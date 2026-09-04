using System.Text;
using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Simulation.Interaction;

public static class DialogueSessionCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<DialogueSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        var builder = new StringBuilder();
        foreach (var session in sessions.OrderBy(s => s.DialogueSessionId.Value))
        {
            builder.Append("DialogueSessionId=").Append(session.DialogueSessionId.Value).Append(';');
            builder.Append("SourceDecisionRequestId=")
                .Append(session.SourceDecisionRequestId.Value)
                .Append(';');
            builder.Append("DialogueTypeCode=").Append(session.DialogueTypeCode).Append(';');
            builder.Append("ManagerId=").Append(session.ManagerId.Value).Append(';');
            builder.Append("PrimaryParticipantPlayerId=")
                .Append(session.PrimaryParticipantPlayerId.Value)
                .Append(';');
            builder.Append("CreatedOn=").Append(session.CreatedOn.DayNumber).Append(';');
            builder.Append("DeadlineOn=")
                .Append(session.DeadlineOn?.DayNumber.ToString() ?? string.Empty)
                .Append(';');
            builder.Append("Status=").Append((int)session.Status).Append(';');
            builder.Append("AvailableOptionCodes=")
                .Append(string.Join(',', session.AvailableOptionCodes))
                .Append(';');
            builder.Append("SelectedOptionCode=")
                .Append(session.SelectedOptionCode ?? string.Empty)
                .Append(';');
            builder.Append("ResolvedOn=")
                .Append(session.ResolvedOn?.DayNumber.ToString() ?? string.Empty)
                .Append('|');
        }

        return builder.ToString();
    }
}
