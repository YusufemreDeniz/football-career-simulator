using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Simulation.TeamPreparation;

/// <summary>
/// Formasyon + taktik yaklaşımdan sade maç gücü modifier'ı (toplam clamp -1..+4).
/// </summary>
public static class MvpTacticMatchModifier
{
    public static int ComputeApproachModifier(TacticPlan? plan)
    {
        if (plan is null)
        {
            return 0;
        }

        return plan.Approach switch
        {
            TacticalApproach.Attacking => 2,
            TacticalApproach.Defensive => 1,
            TacticalApproach.Balanced => 0,
            _ => 0,
        };
    }

    public static int ComputeFormationModifier(TacticPlan? plan)
    {
        if (plan is null)
        {
            return 0;
        }

        return plan.Formation switch
        {
            Formation.F433 => 1,
            Formation.F352 => 1,
            Formation.F442 => 0,
            _ => 0,
        };
    }

    public static int ComputeTacticModifier(TacticPlan? plan)
    {
        if (plan is null)
        {
            return 0;
        }

        return Math.Clamp(
            ComputeApproachModifier(plan) + ComputeFormationModifier(plan),
            -1,
            4);
    }
}
