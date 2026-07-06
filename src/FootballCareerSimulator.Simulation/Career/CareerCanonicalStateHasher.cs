using System.Security.Cryptography;
using System.Text;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.Competition;
using FootballCareerSimulator.Simulation.WorldCalendar;

namespace FootballCareerSimulator.Simulation.Career;

public static class CareerCanonicalStateHasher
{
    public static string ComputeHash(WorldTimeline timeline, LeagueCompetition league)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(league);

        var canonicalText = string.Concat(
            WorldTimelineCanonicalStateHasher.BuildCanonicalText(timeline),
            "|",
            CompetitionCanonicalStateHasher.BuildCanonicalText(league));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText));
        return Convert.ToHexString(hashBytes);
    }
}
