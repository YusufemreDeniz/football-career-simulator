using System.Text;
using FootballCareerSimulator.Domain.Discipline;

namespace FootballCareerSimulator.Simulation.Discipline;

public static class DisciplinaryActionCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<DisciplinaryAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        var builder = new StringBuilder();
        foreach (var action in actions.OrderBy(a => a.DisciplinaryActionId.Value))
        {
            builder.Append("DisciplinaryActionId=").Append(action.DisciplinaryActionId.Value).Append(';');
            builder.Append("Kind=").Append((int)action.Kind).Append(';');
            builder.Append("ManagerId=").Append(action.ManagerId.Value).Append(';');
            builder.Append("SubjectPlayerId=").Append(action.SubjectPlayerId.Value).Append(';');
            builder.Append("ClubId=").Append(action.ClubId.Value).Append(';');
            builder.Append("SourceDecisionRequestId=")
                .Append(action.SourceDecisionRequestId?.Value.ToString() ?? string.Empty)
                .Append(';');
            builder.Append("AppliedOn=").Append(action.AppliedOn.DayNumber).Append('|');
        }

        return builder.ToString();
    }
}
