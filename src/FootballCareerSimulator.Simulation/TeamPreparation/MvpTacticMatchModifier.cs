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
            ComputeApproachModifier(plan)
            + ComputeFormationModifier(plan)
            + ComputeInstructionModifier(plan),
            -3,
            5);
    }

    public static int ComputeInstructionModifier(TacticPlan? plan)
    {
        if (plan is null)
        {
            return 0;
        }

        var score = 0;
        score += plan.Pressing switch
        {
            PressingIntensity.HighPress when plan.Approach == TacticalApproach.Attacking => 1,
            PressingIntensity.LowBlock when plan.Approach == TacticalApproach.Defensive => 1,
            PressingIntensity.HighPress when plan.Approach == TacticalApproach.Defensive => -1,
            _ => 0,
        };
        score += plan.DefensiveLine switch
        {
            DefensiveLine.High when plan.Formation is Formation.F433 or Formation.F352 => 1,
            DefensiveLine.Deep when plan.Approach == TacticalApproach.Defensive => 1,
            DefensiveLine.Deep when plan.Approach == TacticalApproach.Attacking => -1,
            _ => 0,
        };
        score += plan.PassingStyle switch
        {
            PassingStyle.Direct when plan.Formation == Formation.F442 => 1,
            PassingStyle.Short when plan.Formation == Formation.F433 => 1,
            PassingStyle.Short when plan.Formation == Formation.F352 => -1,
            _ => 0,
        };
        return Math.Clamp(score, -2, 2);
    }
}
