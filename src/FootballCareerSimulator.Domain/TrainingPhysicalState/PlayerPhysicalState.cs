using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.TrainingPhysicalState;

/// <summary>
/// Slot bazlı yorgunluk/fitness/sakatlık (gerçek PlayerId yok; MatchSelection slot modeli ile hizalı).
/// </summary>
public sealed class PlayerPhysicalState
{
    public const int MinLevel = 0;
    public const int MaxLevel = 100;
    public const int DefaultFatigue = 15;
    public const int DefaultFitness = 80;

    private PlayerPhysicalState(
        ClubId clubId,
        int slotIndex,
        int fatigue,
        int fitness,
        InjurySeverity injurySeverity,
        int? injuredUntilDayNumber)
    {
        ClubId = clubId;
        SlotIndex = slotIndex;
        Fatigue = fatigue;
        Fitness = fitness;
        InjurySeverity = injurySeverity;
        InjuredUntilDayNumber = injuredUntilDayNumber;
    }

    public ClubId ClubId { get; }

    public int SlotIndex { get; }

    public int Fatigue { get; }

    public int Fitness { get; }

    public InjurySeverity InjurySeverity { get; }

    public int? InjuredUntilDayNumber { get; }

    public bool IsInjured => InjurySeverity != InjurySeverity.None;

    public static PlayerPhysicalState CreateRested(ClubId clubId, int slotIndex)
    {
        EnsureSlot(slotIndex);
        return new PlayerPhysicalState(
            clubId,
            slotIndex,
            DefaultFatigue,
            DefaultFitness,
            InjurySeverity.None,
            injuredUntilDayNumber: null);
    }

    public static PlayerPhysicalState Rehydrate(
        ClubId clubId,
        int slotIndex,
        int fatigue,
        int fitness,
        InjurySeverity injurySeverity = InjurySeverity.None,
        int? injuredUntilDayNumber = null)
    {
        EnsureSlot(slotIndex);
        EnsureLevel(fatigue, nameof(fatigue));
        EnsureLevel(fitness, nameof(fitness));
        EnsureInjury(injurySeverity, injuredUntilDayNumber);
        return new PlayerPhysicalState(
            clubId,
            slotIndex,
            fatigue,
            fitness,
            injurySeverity,
            injuredUntilDayNumber);
    }

    public PlayerPhysicalState WithLevels(int fatigue, int fitness)
    {
        EnsureLevel(fatigue, nameof(fatigue));
        EnsureLevel(fitness, nameof(fitness));
        return new PlayerPhysicalState(
            ClubId,
            SlotIndex,
            fatigue,
            fitness,
            InjurySeverity,
            InjuredUntilDayNumber);
    }

    public PlayerPhysicalState WithInjury(InjurySeverity severity, GameDate availableAfter)
    {
        if (severity == InjurySeverity.None)
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                "Use ClearInjury to remove an injury.");
        }

        EnsureInjury(severity, availableAfter.DayNumber);
        return new PlayerPhysicalState(
            ClubId,
            SlotIndex,
            Fatigue,
            Fitness,
            severity,
            availableAfter.DayNumber);
    }

    public PlayerPhysicalState ClearInjury() =>
        new(ClubId, SlotIndex, Fatigue, Fitness, InjurySeverity.None, null);

    public PlayerPhysicalState RecoverIfDue(GameDate day)
    {
        if (!IsInjured)
        {
            return this;
        }

        if (InjuredUntilDayNumber is int until && day.DayNumber > until)
        {
            return ClearInjury();
        }

        return this;
    }

    public bool IsAvailableOn(GameDate day) =>
        !IsInjured
        || InjuredUntilDayNumber is int until && day.DayNumber > until;

    public AvailabilityStatus GetAvailability(GameDate day) =>
        IsAvailableOn(day) ? AvailabilityStatus.Available : AvailabilityStatus.Unavailable;

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
