using System.Text;
using FootballCareerSimulator.Domain.ClubGovernance;

namespace FootballCareerSimulator.Simulation.ClubGovernance;

public static class ClubRegistryCanonicalStateHasher
{
    public static string BuildCanonicalText(LeagueClubRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var builder = new StringBuilder();
        builder.Append("ClubCount=").Append(registry.Clubs.Count).Append(';');

        foreach (var club in registry.Clubs.OrderBy(club => club.Id.Value))
        {
            builder.Append("ClubId=").Append(club.Id.Value).Append(';');
            builder.Append("DisplayName=").Append(club.DisplayName).Append(';');
            builder.Append("Code=").Append(club.Code.Value).Append(';');
            builder.Append("SportiveStrength=").Append(club.SportiveStrength).Append(';');
        }

        return builder.ToString();
    }
}
