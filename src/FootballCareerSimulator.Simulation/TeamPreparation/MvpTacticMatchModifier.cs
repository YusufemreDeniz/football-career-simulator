using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Simulation.TeamPreparation;

/// <summary>
/// Taktik yaklaşımından sade maç gücü modifier'ı (-1..+2).
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
}
