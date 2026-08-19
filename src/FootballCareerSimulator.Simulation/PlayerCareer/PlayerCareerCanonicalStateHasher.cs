using System.Text;
using FootballCareerSimulator.Domain.PlayerCareer;

namespace FootballCareerSimulator.Simulation.PlayerCareer;

public static class PlayerCareerCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<Domain.PlayerCareer.PlayerCareer> careers)
    {
        ArgumentNullException.ThrowIfNull(careers);

        var builder = new StringBuilder("PlayerCareers=");
        foreach (var career in careers
                     .OrderBy(c => c.Id.Value)
                     .ThenBy(c => c.OriginClubId.Value)
                     .ThenBy(c => c.SlotIndex))
        {
            builder.Append("Id=").Append(career.Id.Value)
                .Append(";C=").Append(career.OriginClubId.Value)
                .Append(";S=").Append(career.SlotIndex)
                .Append(";CA=").Append(career.CurrentAbility)
                .Append(";PA=").Append(career.PotentialAbility)
                .Append(";DP=").Append(career.DevelopmentPoints)
                .Append(";D=").Append(career.LastDevelopedOn?.DayNumber.ToString() ?? "-")
                .Append(";BY=").Append(career.BirthYear)
                .Append(";AY=").Append(career.LastAgedCalendarYear?.ToString() ?? "-")
                .Append(";LS=").Append((int)career.LifecycleStatus)
                .Append(";RD=").Append(career.RetiredOn?.DayNumber.ToString() ?? "-")
                .Append(";RR=").Append(career.RetirementReason is PlayerRetirementReason reason ? ((int)reason).ToString() : "-")
                .Append(";G=").Append(career.Generation)
                .Append('|');
        }

        return builder.ToString();
    }
}
