using System.Text;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Simulation.TeamPreparation;

public static class TacticPlanCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<TacticPlan> plans)
    {
        ArgumentNullException.ThrowIfNull(plans);

        var builder = new StringBuilder("TacticPlans=");
        foreach (var plan in plans.OrderBy(p => p.ClubId.Value))
        {
            builder.Append("C=").Append(plan.ClubId.Value)
                .Append(";F=").Append((int)plan.Formation)
                .Append(";A=").Append((int)plan.Approach)
                .Append(";U=").Append(plan.LastUpdatedOn.DayNumber)
                .Append('|');
        }

        return builder.ToString();
    }
}
