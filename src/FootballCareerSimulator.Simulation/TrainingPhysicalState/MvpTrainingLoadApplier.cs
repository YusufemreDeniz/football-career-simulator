using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

/// <summary>
/// Haftalık planı antrenman günlerinde uygular; maç günü yükü basmaz; ghost slot yok.
/// </summary>
public static class MvpTrainingLoadApplier
{
    /// <summary>Haftada antrenman günü sayısı (maç günü + dinlenme günü ayrı).</summary>
    public const int TrainingLoadDaysPerWeek = 5;

    /// <summary>Geriye uyum alias.</summary>
    public const int TrainingDaysPerWeek = TrainingLoadDaysPerWeek;

    /// <summary>
    /// Haftanın her takvim günü antrenman günü değildir: Pzt–Cum yük; Cmt–Paz dinlenme.
    /// Maç günü ayrıca düşük yük uygular.
    /// </summary>
    public static bool IsCalendarTrainingDay(GameDate day)
    {
        var dow = DateOnly.FromDayNumber(day.DayNumber).DayOfWeek;
        return dow is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
    }

    public static IReadOnlyList<PlayerPhysicalState> ApplyDailyTick(
        WeeklyTrainingPlan plan,
        GameDate day,
        int rootSeed,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>? existing = null,
        IReadOnlyList<int>? occupiedSlots = null,
        bool isMatchDay = false)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var slots = ResolveOccupiedSlots(occupiedSlots);
        var states = new List<PlayerPhysicalState>(slots.Count);
        foreach (var slot in slots)
        {
            var current = existing is not null
                && existing.TryGetValue((plan.ClubId.Value, slot), out var prior)
                    ? prior.RecoverIfDue(day)
                    : PlayerPhysicalState.CreateRested(plan.ClubId, slot);

            if (!current.IsAvailableOn(day))
            {
                states.Add(current);
                continue;
            }

            if (isMatchDay || !IsCalendarTrainingDay(day))
            {
                states.Add(ApplyRestOrMatchDayRecovery(current, plan, isMatchDay));
                continue;
            }

            var loaded = ApplyDailyToPlayer(current, plan);
            states.Add(
                MvpInjuryRiskEvaluator.MaybeInjureFromTraining(
                    loaded,
                    plan,
                    rootSeed,
                    day,
                    dailyTick: true));
        }

        return MergeWithPreservedSlots(plan.ClubId, states, existing, slots);
    }

    /// <summary>
    /// PlayerId tabanlı günlük tick — squad üyeleri üzerinden.
    /// </summary>
    public static IReadOnlyList<PlayerPhysicalState> ApplyDailyTickToMembers(
        WeeklyTrainingPlan plan,
        GameDate day,
        int rootSeed,
        IReadOnlyList<(PlayerId PlayerId, int SlotIndex)> members,
        IReadOnlyDictionary<long, PlayerPhysicalState>? existingByPlayer = null,
        bool isMatchDay = false)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(members);

        var result = new List<PlayerPhysicalState>(members.Count);
        foreach (var member in members)
        {
            var current = existingByPlayer is not null
                && existingByPlayer.TryGetValue(member.PlayerId.Value, out var prior)
                    ? prior.RecoverIfDue(day).WithLocation(plan.ClubId, member.SlotIndex)
                    : PlayerPhysicalState.CreateRested(member.PlayerId, plan.ClubId, member.SlotIndex);

            if (!current.IsAvailableOn(day))
            {
                result.Add(current);
                continue;
            }

            if (isMatchDay || !IsCalendarTrainingDay(day))
            {
                result.Add(ApplyRestOrMatchDayRecovery(current, plan, isMatchDay));
                continue;
            }

            var loaded = ApplyDailyToPlayer(current, plan);
            result.Add(
                MvpInjuryRiskEvaluator.MaybeInjureFromTraining(
                    loaded,
                    plan,
                    rootSeed,
                    day,
                    dailyTick: true));
        }

        return result;
    }

    /// <summary>Maç günü veya hafta sonu: antrenman sakatlığı yok; dinlenme/toparlanma.</summary>
    public static PlayerPhysicalState ApplyRestOrMatchDayRecovery(
        PlayerPhysicalState current,
        WeeklyTrainingPlan plan,
        bool isMatchDay)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(plan);

        var fatigueRelief = isMatchDay
            ? 1
            : plan.RestApproach switch
            {
                RestApproach.Light => 2,
                RestApproach.Normal => 4,
                RestApproach.Heavy => 8,
                _ => 4,
            };
        if (!isMatchDay && plan.Focus == TrainingFocus.Recovery)
        {
            fatigueRelief += 2;
        }

        var fitnessBump = !isMatchDay && plan.RestApproach == RestApproach.Heavy ? 1 : 0;
        return current.WithLevels(
            Math.Clamp(current.Fatigue - fatigueRelief, PlayerPhysicalState.MinLevel, PlayerPhysicalState.MaxLevel),
            Math.Clamp(current.Fitness + fitnessBump, PlayerPhysicalState.MinLevel, PlayerPhysicalState.MaxLevel));
    }

    /// <summary>
    /// Eski tek-seferlik haftalık uygulama (test / geriye dönük). Üretim yolu günlük tick'tir.
    /// </summary>
    public static IReadOnlyList<PlayerPhysicalState> ApplyPlanToSquad(
        WeeklyTrainingPlan plan,
        int rootSeed,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>? existing = null,
        IReadOnlyList<int>? occupiedSlots = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var slots = ResolveOccupiedSlots(occupiedSlots);
        var states = new List<PlayerPhysicalState>(slots.Count);
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var current = existing is not null
                && existing.TryGetValue((plan.ClubId.Value, slot), out var prior)
                    ? prior.RecoverIfDue(plan.SetAt)
                    : PlayerPhysicalState.CreateRested(plan.ClubId, slot);

            if (!current.IsAvailableOn(plan.SetAt))
            {
                states.Add(current);
                continue;
            }

            var loaded = ApplyToPlayer(current, plan);
            states.Add(
                MvpInjuryRiskEvaluator.MaybeInjureFromTraining(loaded, plan, rootSeed, plan.SetAt));
        }

        return MergeWithPreservedSlots(plan.ClubId, states, existing, slots);
    }

    public static PlayerPhysicalState ApplyDailyToPlayer(PlayerPhysicalState current, WeeklyTrainingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(plan);

        if (current.ClubId != plan.ClubId)
        {
            throw new ArgumentException("Physical state club must match training plan club.", nameof(current));
        }

        var weekly = ComputeWeeklyDeltas(plan);
        var fatigueDelta = DivideAcrossWeek(weekly.FatigueDelta);
        var fitnessDelta = DivideAcrossWeek(weekly.FitnessDelta);
        var fatigue = Math.Clamp(
            current.Fatigue + fatigueDelta,
            PlayerPhysicalState.MinLevel,
            PlayerPhysicalState.MaxLevel);
        var fitness = Math.Clamp(
            current.Fitness + fitnessDelta,
            PlayerPhysicalState.MinLevel,
            PlayerPhysicalState.MaxLevel);
        return current.WithLevels(fatigue, fitness);
    }

    public static PlayerPhysicalState ApplyToPlayer(PlayerPhysicalState current, WeeklyTrainingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(plan);

        if (current.ClubId != plan.ClubId)
        {
            throw new ArgumentException("Physical state club must match training plan club.", nameof(current));
        }

        var weekly = ComputeWeeklyDeltas(plan);
        var fatigue = Math.Clamp(
            current.Fatigue + weekly.FatigueDelta,
            PlayerPhysicalState.MinLevel,
            PlayerPhysicalState.MaxLevel);
        var fitness = Math.Clamp(
            current.Fitness + weekly.FitnessDelta,
            PlayerPhysicalState.MinLevel,
            PlayerPhysicalState.MaxLevel);
        return current.WithLevels(fatigue, fitness);
    }

    public static IReadOnlyList<PlayerPhysicalState> RecoverClubToDate(
        ClubId clubId,
        GameDate day,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot)
    {
        ArgumentNullException.ThrowIfNull(physicalBySlot);

        return physicalBySlot.Values
            .Where(state => state.ClubId == clubId)
            .Select(state => state.RecoverIfDue(day))
            .OrderBy(state => state.SlotIndex ?? int.MaxValue)
            .ThenBy(state => state.PlayerId.Value)
            .ToArray();
    }

    private static (int FatigueDelta, int FitnessDelta) ComputeWeeklyDeltas(WeeklyTrainingPlan plan)
    {
        var intensityLoad = plan.Intensity switch
        {
            TrainingIntensity.Low => 12,
            TrainingIntensity.Medium => 28,
            TrainingIntensity.High => 48,
            _ => 28,
        };

        var restRelief = plan.RestApproach switch
        {
            RestApproach.Light => 4,
            RestApproach.Normal => 16,
            RestApproach.Heavy => 30,
            _ => 16,
        };

        var fatigue = intensityLoad - restRelief;
        var fitness = 0;

        switch (plan.Focus)
        {
            case TrainingFocus.Fitness:
                fitness += 6;
                fatigue += 2;
                break;
            case TrainingFocus.Recovery:
                fatigue -= 8;
                fitness += 3;
                break;
            case TrainingFocus.Tactical:
                fitness += 3;
                fatigue -= 1;
                break;
            default:
                fitness += 3;
                break;
        }

        return (fatigue, fitness);
    }

    private static int DivideAcrossWeek(int weeklyDelta) =>
        (int)Math.Round(weeklyDelta / (double)TrainingLoadDaysPerWeek, MidpointRounding.AwayFromZero);

    private static IReadOnlyList<int> ResolveOccupiedSlots(IReadOnlyList<int>? occupiedSlots)
    {
        if (occupiedSlots is { Count: > 0 })
        {
            return occupiedSlots
                .Where(slot => slot is >= MatchSelection.MinSquadSlot and <= MatchSelection.MaxSquadSlot)
                .Distinct()
                .OrderBy(slot => slot)
                .ToArray();
        }

        return Enumerable.Range(
                MatchSelection.MinSquadSlot,
                MatchSelection.MaxSquadSlot - MatchSelection.MinSquadSlot + 1)
            .ToArray();
    }

    private static IReadOnlyList<PlayerPhysicalState> MergeWithPreservedSlots(
        ClubId clubId,
        IReadOnlyList<PlayerPhysicalState> updated,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>? existing,
        IReadOnlyList<int> touchedSlots)
    {
        var touched = touchedSlots.ToHashSet();
        var bySlot = updated
            .Where(state => state.SlotIndex is int)
            .ToDictionary(state => state.SlotIndex!.Value);
        if (existing is not null)
        {
            foreach (var state in existing.Values.Where(s => s.ClubId == clubId && s.SlotIndex is int))
            {
                var slot = state.SlotIndex!.Value;
                if (!touched.Contains(slot))
                {
                    bySlot[slot] = state;
                }
            }
        }

        return bySlot.Values.OrderBy(state => state.SlotIndex).ToArray();
    }
}
