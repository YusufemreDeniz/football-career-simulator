using System.Text;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Simulation.TeamPreparation;

public static class ClubSquadCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<ClubSquad> squads)
    {
        ArgumentNullException.ThrowIfNull(squads);

        var builder = new StringBuilder("ClubSquads=");
        foreach (var squad in squads.OrderBy(s => s.ClubId.Value))
        {
            builder.Append("Club=").Append(squad.ClubId.Value).Append(';');
            foreach (var member in squad.Members.OrderBy(m => m.SlotIndex))
            {
                builder.Append("P=").Append(member.PlayerId.Value)
                    .Append(";S=").Append(member.SlotIndex)
                    .Append(";J=").Append(member.JoinedOn.DayNumber)
                    .Append('|');
            }

            builder.Append('#');
        }

        return builder.ToString();
    }
}
