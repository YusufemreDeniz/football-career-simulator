using System.Text;
using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Simulation.Interaction;

public static class DecisionRequestCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<DecisionRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var builder = new StringBuilder();
        foreach (var request in requests.OrderBy(r => r.DecisionRequestId.Value))
        {
            builder.Append("DecisionRequestId=").Append(request.DecisionRequestId.Value).Append(';');
            builder.Append("Kind=").Append((int)request.Kind).Append(';');
            builder.Append("ManagerId=").Append(request.ManagerId.Value).Append(';');
            builder.Append("SubjectPlayerId=").Append(request.SubjectPlayerId.Value).Append(';');
            builder.Append("ClubId=").Append(request.ClubId.Value).Append(';');
            builder.Append("OpenedOn=").Append(request.OpenedOn.DayNumber).Append(';');
            builder.Append("DeadlineOn=").Append(request.DeadlineOn.DayNumber).Append(';');
            builder.Append("Status=").Append((int)request.Status).Append(';');
            builder.Append("IsHardBlocker=").Append(request.IsHardBlocker ? "1" : "0").Append(';');
            builder.Append("SelectedOptionCode=")
                .Append(request.SelectedOptionCode ?? string.Empty)
                .Append(';');
            builder.Append("ResolvedOn=")
                .Append(request.ResolvedOn?.DayNumber.ToString() ?? string.Empty)
                .Append('|');
        }

        return builder.ToString();
    }
}
