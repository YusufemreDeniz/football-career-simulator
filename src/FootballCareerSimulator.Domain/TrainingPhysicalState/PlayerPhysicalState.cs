using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.TrainingPhysicalState;

/// <summary>
/// Futbolcuya bağlı yorgunluk/fitness/sakatlık/yük. Kimlik PlayerId'dir;
/// ClubId+SlotIndex yalnız kadro konumu (denormalize) bilgisidir.
/// </summary>
public sealed class PlayerPhysicalState
{
    public const int MinLevel = 0;
    public const int MaxLevel = 100;
    public const int DefaultFatigue = 15;
    public const int DefaultFitness = 80;
    public const int PostInjuryFatigueBump = 12;
    public const int PostInjuryFitnessPenalty = 18;

    public const string ReasonTrainingLoad = "training_load";
    public const string ReasonMatchLoad = "match_load";
    public const string ReasonAccumulatedWorkload = "accumulated_workload";
    public const string ReasonReturnFromInjury = "return_from_injury";
    public const string ReasonUnexpected = "unexpected";

    private PlayerPhysicalState(
        PlayerId playerId,
        ClubId? clubId,
        int? slotIndex,
        int fatigue,
        int fitness,
        InjurySeverity injurySeverity,
        int? injuredUntilDayNumber,
        int matchMinutesLast7Days,
        int matchMinutesLast14Days,
        int? lastMatchDayNumber,
        string? lastInjuryReasonCode)
    {
        PlayerId = playerId;
        ClubId = clubId;
        SlotIndex = slotIndex;
        Fatigue = fatigue;
        Fitness = fitness;
        InjurySeverity = injurySeverity;
        InjuredUntilDayNumber = injuredUntilDayNumber;
        MatchMinutesLast7Days = matchMinutesLast7Days;
        MatchMinutesLast14Days = matchMinutesLast14Days;
        LastMatchDayNumber = lastMatchDayNumber;
        LastInjuryReasonCode = lastInjuryReasonCode;
    }

    public PlayerId PlayerId { get; }

    /// <summary>Denormalize: aktif kadro kulübü; serbestse null.</summary>
    public ClubId? ClubId { get; }

    /// <summary>Denormalize: maç günü slotu; serbestse null.</summary>
    public int? SlotIndex { get; }

    public int Fatigue { get; }

    public int Fitness { get; }

    public InjurySeverity InjurySeverity { get; }

    public int? InjuredUntilDayNumber { get; }

    public int MatchMinutesLast7Days { get; }

    public int MatchMinutesLast14Days { get; }

    public int? LastMatchDayNumber { get; }

    public string? LastInjuryReasonCode { get; }

    public bool IsInjured => InjurySeverity != InjurySeverity.None;

    public bool HasLocation => ClubId is not null && SlotIndex is int;

    public static PlayerPhysicalState CreateRested(PlayerId playerId, ClubId? clubId = null, int? slotIndex = null)
    {
        EnsureLocation(clubId, slotIndex);
        return new PlayerPhysicalState(
            playerId,
            clubId,
            slotIndex,
            DefaultFatigue,
            DefaultFitness,
            InjurySeverity.None,
            injuredUntilDayNumber: null,
            matchMinutesLast7Days: 0,
            matchMinutesLast14Days: 0,
            lastMatchDayNumber: null,
            lastInjuryReasonCode: null);
    }

    /// <summary>Legacy/test: sentetik PlayerId.FromClubSlot ile rested state.</summary>
    public static PlayerPhysicalState CreateRested(ClubId clubId, int slotIndex) =>
        CreateRested(PlayerId.FromClubSlot(clubId.Value, slotIndex), clubId, slotIndex);

    public static PlayerPhysicalState Rehydrate(
        PlayerId playerId,
        ClubId? clubId,
        int? slotIndex,
        int fatigue,
        int fitness,
        InjurySeverity injurySeverity = InjurySeverity.None,
        int? injuredUntilDayNumber = null,
        int matchMinutesLast7Days = 0,
        int matchMinutesLast14Days = 0,
        int? lastMatchDayNumber = null,
        string? lastInjuryReasonCode = null)
    {
        EnsureLocation(clubId, slotIndex);
        EnsureLevel(fatigue, nameof(fatigue));
        EnsureLevel(fitness, nameof(fitness));
        EnsureInjury(injurySeverity, injuredUntilDayNumber);
        EnsureNonNegative(matchMinutesLast7Days, nameof(matchMinutesLast7Days));
        EnsureNonNegative(matchMinutesLast14Days, nameof(matchMinutesLast14Days));
        return new PlayerPhysicalState(
            playerId,
            clubId,
            slotIndex,
            fatigue,
            fitness,
            injurySeverity,
            injuredUntilDayNumber,
            matchMinutesLast7Days,
            matchMinutesLast14Days,
            lastMatchDayNumber,
            string.IsNullOrWhiteSpace(lastInjuryReasonCode) ? null : lastInjuryReasonCode.Trim());
    }

    /// <summary>Legacy save/test rehydrate keyed by club+slot before PlayerId migration.</summary>
    public static PlayerPhysicalState Rehydrate(
        ClubId clubId,
        int slotIndex,
        int fatigue,
        int fitness,
        InjurySeverity injurySeverity = InjurySeverity.None,
        int? injuredUntilDayNumber = null) =>
        Rehydrate(
            PlayerId.FromClubSlot(clubId.Value, slotIndex),
            clubId,
            slotIndex,
            fatigue,
            fitness,
            injurySeverity,
            injuredUntilDayNumber);

    public PlayerPhysicalState WithLevels(int fatigue, int fitness)
    {
        EnsureLevel(fatigue, nameof(fatigue));
        EnsureLevel(fitness, nameof(fitness));
        return Copy(fatigue: fatigue, fitness: fitness);
    }

    public PlayerPhysicalState WithLocation(ClubId clubId, int slotIndex)
    {
        EnsureSlot(slotIndex);
        return Copy(clubId: clubId, slotIndex: slotIndex);
    }

    public PlayerPhysicalState ClearLocation() =>
        Copy(clubId: null, slotIndex: null, clearLocation: true);

    /// <summary>Geriye uyum: konum güncelle (PlayerId sabit).</summary>
    public PlayerPhysicalState Relocate(ClubId clubId, int slotIndex) =>
        WithLocation(clubId, slotIndex);

    public PlayerPhysicalState WithInjury(
        InjurySeverity severity,
        GameDate availableAfter,
        string? reasonCode = null)
    {
        if (severity == InjurySeverity.None)
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                "Use ClearInjury to remove an injury.");
        }

        EnsureInjury(severity, availableAfter.DayNumber);
        return Copy(
            injurySeverity: severity,
            injuredUntilDayNumber: availableAfter.DayNumber,
            lastInjuryReasonCode: string.IsNullOrWhiteSpace(reasonCode)
                ? ReasonUnexpected
                : reasonCode.Trim());
    }

    public PlayerPhysicalState ClearInjury() =>
        Copy(
            injurySeverity: InjurySeverity.None,
            injuredUntilDayNumber: null,
            clearInjuryUntil: true,
            lastInjuryReasonCode: null,
            clearInjuryReason: true);

    /// <summary>
    /// Sakatlık bitince hemen tam kondisyon verilmez; yeniden sakatlanma riski kalsın.
    /// </summary>
    public PlayerPhysicalState ClearInjuryWithRecoveryDampening()
    {
        var fatigue = Math.Clamp(Fatigue + PostInjuryFatigueBump, MinLevel, MaxLevel);
        var fitness = Math.Clamp(Fitness - PostInjuryFitnessPenalty, MinLevel, MaxLevel);
        return Copy(
            fatigue: fatigue,
            fitness: fitness,
            injurySeverity: InjurySeverity.None,
            injuredUntilDayNumber: null,
            clearInjuryUntil: true,
            lastInjuryReasonCode: ReasonReturnFromInjury);
    }

    public PlayerPhysicalState RecoverIfDue(GameDate day)
    {
        if (!IsInjured)
        {
            return this;
        }

        if (InjuredUntilDayNumber is int until && day.DayNumber > until)
        {
            return ClearInjuryWithRecoveryDampening();
        }

        return this;
    }

    /// <summary>
    /// Maç dakikası kaydı: 7/14 günlük pencereyi basit üstel-azalmalı sayaçla günceller.
    /// </summary>
    public PlayerPhysicalState RecordMatchMinutes(GameDate day, int minutesPlayed)
    {
        if (minutesPlayed < 0)
        {
            throw new TrainingPhysicalStateInvariantViolationException("minutesPlayed cannot be negative.");
        }

        var gap = LastMatchDayNumber is int last
            ? Math.Max(0, day.DayNumber - last)
            : 14;
        var decay7 = DecayMinutes(MatchMinutesLast7Days, gap, windowDays: 7);
        var decay14 = DecayMinutes(MatchMinutesLast14Days, gap, windowDays: 14);
        return Copy(
            matchMinutesLast7Days: Math.Min(900, decay7 + minutesPlayed),
            matchMinutesLast14Days: Math.Min(1800, decay14 + minutesPlayed),
            lastMatchDayNumber: day.DayNumber);
    }

    public int DaysSinceLastMatch(GameDate day) =>
        LastMatchDayNumber is int last
            ? Math.Max(0, day.DayNumber - last)
            : 99;

    public bool HasCongestedFixture(GameDate day) =>
        DaysSinceLastMatch(day) is > 0 and <= 3 && MatchMinutesLast7Days >= 60;

    public static string FatigueBandLabel(int fatigue) =>
        fatigue switch
        {
            <= 24 => "Dinç",
            <= 44 => "Normal",
            <= 64 => "Yorgun",
            <= 79 => "Yüksek Risk",
            _ => "Çok Yorgun",
        };

    public bool IsAvailableOn(GameDate day) =>
        !IsInjured
        || InjuredUntilDayNumber is int until && day.DayNumber > until;

    public AvailabilityStatus GetAvailability(GameDate day) =>
        IsAvailableOn(day) ? AvailabilityStatus.Available : AvailabilityStatus.Unavailable;

    private PlayerPhysicalState Copy(
        ClubId? clubId = null,
        int? slotIndex = null,
        bool clearLocation = false,
        int? fatigue = null,
        int? fitness = null,
        InjurySeverity? injurySeverity = null,
        int? injuredUntilDayNumber = null,
        bool clearInjuryUntil = false,
        int? matchMinutesLast7Days = null,
        int? matchMinutesLast14Days = null,
        int? lastMatchDayNumber = null,
        string? lastInjuryReasonCode = null,
        bool clearInjuryReason = false) =>
        new(
            PlayerId,
            clearLocation ? null : clubId ?? ClubId,
            clearLocation ? null : slotIndex ?? SlotIndex,
            fatigue ?? Fatigue,
            fitness ?? Fitness,
            injurySeverity ?? InjurySeverity,
            clearInjuryUntil ? null : injuredUntilDayNumber ?? InjuredUntilDayNumber,
            matchMinutesLast7Days ?? MatchMinutesLast7Days,
            matchMinutesLast14Days ?? MatchMinutesLast14Days,
            lastMatchDayNumber ?? LastMatchDayNumber,
            clearInjuryReason ? null : lastInjuryReasonCode ?? LastInjuryReasonCode);

    private static int DecayMinutes(int current, int gapDays, int windowDays)
    {
        if (gapDays <= 0 || current <= 0)
        {
            return current;
        }

        if (gapDays >= windowDays)
        {
            return 0;
        }

        return (int)Math.Round(current * (windowDays - gapDays) / (double)windowDays, MidpointRounding.AwayFromZero);
    }

    private static void EnsureLocation(ClubId? clubId, int? slotIndex)
    {
        if (clubId is null && slotIndex is null)
        {
            return;
        }

        if (clubId is null || slotIndex is null)
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                "ClubId and SlotIndex must both be set or both null.");
        }

        EnsureSlot(slotIndex.Value);
    }

    private static void EnsureSlot(int slotIndex)
    {
        if (slotIndex is < MatchSelection.MinSquadSlot or > MatchSelection.MaxSquadSlot)
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                $"Squad slot {slotIndex} is out of range ({MatchSelection.MinSquadSlot}-{MatchSelection.MaxSquadSlot}).");
        }
    }

    private static void EnsureLevel(int value, string name)
    {
        if (value is < MinLevel or > MaxLevel)
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                $"{name} must be between {MinLevel} and {MaxLevel}.");
        }
    }

    private static void EnsureNonNegative(int value, string name)
    {
        if (value < 0)
        {
            throw new TrainingPhysicalStateInvariantViolationException($"{name} cannot be negative.");
        }
    }

    private static void EnsureInjury(InjurySeverity severity, int? injuredUntilDayNumber)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                $"Unknown injury severity: {severity}.");
        }

        if (severity == InjurySeverity.None)
        {
            if (injuredUntilDayNumber is not null)
            {
                throw new TrainingPhysicalStateInvariantViolationException(
                    "Healthy player cannot have an injured-until date.");
            }

            return;
        }

        if (injuredUntilDayNumber is null)
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                "Injured player requires an injured-until day number.");
        }
    }
}
