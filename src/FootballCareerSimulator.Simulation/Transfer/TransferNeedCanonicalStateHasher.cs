using System.Text;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Simulation.Transfer;

public static class TransferNeedCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<TransferNeed> needs)
    {
        ArgumentNullException.ThrowIfNull(needs);

        var builder = new StringBuilder("TransferNeeds=");
        foreach (var need in needs.OrderBy(n => n.NeedId.Value))
        {
            builder.Append("N=").Append(need.NeedId.Value)
                .Append(";C=").Append(need.ClubId.Value)
                .Append(";K=").Append((int)need.Kind)
                .Append(";S=").Append((int)need.Status)
                .Append(";P=").Append(need.Priority)
                .Append(";R=").Append(need.ReasonCode)
                .Append(";I=").Append(need.IdentifiedOn.DayNumber)
                .Append(";X=").Append(need.ClosedOn?.DayNumber.ToString() ?? "-")
                .Append('|');
        }

        return builder.ToString();
    }
}
