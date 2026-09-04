using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

/// <summary>
/// Haftalık planı günlük tick olarak uygular; boş/ghost slotlara yük basmaz.
/// </summary>
public static class MvpTrainingLoadApplier
{
    public const int TrainingDaysPerWeek = 7;

    public static IReadOnlyList<PlayerPhysicalState> ApplyDailyTick(
        WeeklyTrainingPlan plan,
        GameDate day,
        int rootSeed,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>? existing = null,
        IReadOnlyList<int>? occupiedSlots = null)
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
            .OrderBy(state => state.SlotIndex)
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
                fatigue -= 6;
                fitness += 2;
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
        (int)Math.Round(weeklyDelta / (double)TrainingDaysPerWeek, MidpointRounding.AwayFromZero);

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
        var bySlot = updated.ToDictionary(state => state.SlotIndex);
        if (existing is not null)
        {
            foreach (var state in existing.Values.Where(s => s.ClubId == clubId))
            {
                if (!touched.Contains(state.SlotIndex))
                {
                    bySlot[state.SlotIndex] = state;
                }
            }
        }

        return bySlot.Values.OrderBy(state => state.SlotIndex).ToArray();
    }
}
