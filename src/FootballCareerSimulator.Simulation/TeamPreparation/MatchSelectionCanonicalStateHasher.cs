using System.Text;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Simulation.TeamPreparation;

public static class MatchSelectionCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<MatchSelection> selections)
    {
        ArgumentNullException.ThrowIfNull(selections);

        var builder = new StringBuilder("MatchSelections=");
        foreach (var selection in selections
                     .OrderBy(s => s.FixtureId.Value)
                     .ThenBy(s => s.ClubId.Value))
        {
            builder.Append("F=").Append(selection.FixtureId.Value)
                .Append(";C=").Append(selection.ClubId.Value)
                .Append(";S=").Append((int)selection.Status)
                .Append(";XI=").Append(string.Join(',', selection.StartingSlotIndices))
                .Append(";B=").Append(string.Join(',', selection.BenchSlotIndices))
                .Append('|');
        }

        return builder.ToString();
    }
}
