using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

/// <summary>
/// Haftalık planı slot fiziksel state'ine deterministik uygular.
/// </summary>
public static class MvpTrainingLoadApplier
{
    public static IReadOnlyList<PlayerPhysicalState> ApplyPlanToSquad(WeeklyTrainingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var states = new PlayerPhysicalState[MatchSelection.MaxSquadSlot - MatchSelection.MinSquadSlot + 1];
        for (var slot = MatchSelection.MinSquadSlot; slot <= MatchSelection.MaxSquadSlot; slot++)
        {
            var rested = PlayerPhysicalState.CreateRested(plan.ClubId, slot);
            states[slot - MatchSelection.MinSquadSlot] = ApplyToPlayer(rested, plan);
        }

        return states;
    }

    public static PlayerPhysicalState ApplyToPlayer(PlayerPhysicalState current, WeeklyTrainingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(plan);

        if (current.ClubId != plan.ClubId)
        {
            throw new ArgumentException("Physical state club must match training plan club.", nameof(current));
        }

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

        var fatigue = current.Fatigue + intensityLoad - restRelief;
        var fitness = current.Fitness;

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
            default:
                fitness += 3;
                break;
        }

        fatigue = Math.Clamp(fatigue, PlayerPhysicalState.MinLevel, PlayerPhysicalState.MaxLevel);
        fitness = Math.Clamp(fitness, PlayerPhysicalState.MinLevel, PlayerPhysicalState.MaxLevel);
        return current.WithLevels(fatigue, fitness);
    }
}
