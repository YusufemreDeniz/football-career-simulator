using System.Text;
using FootballCareerSimulator.Domain.ContractRegistration;

namespace FootballCareerSimulator.Simulation.ContractRegistration;

public static class FreeAgencyCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<PlayerFreeAgency> freeAgents)
    {
        ArgumentNullException.ThrowIfNull(freeAgents);

        var builder = new StringBuilder("FreeAgents=");
        foreach (var entry in freeAgents.OrderBy(f => f.PlayerId.Value))
        {
            builder.Append("P=").Append(entry.PlayerId.Value)
                .Append(";L=").Append(entry.LastClubId.Value)
                .Append(";D=").Append(entry.BecameFreeAgentOn.DayNumber)
                .Append('|');
        }

        return builder.ToString();
    }
}
