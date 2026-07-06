using System.Security.Cryptography;
using System.Text;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
using ManagerCareerState = FootballCareerSimulator.Domain.ManagerCareer.ManagerCareer;
using FootballCareerSimulator.Simulation.ClubGovernance;
using FootballCareerSimulator.Simulation.Competition;
using FootballCareerSimulator.Simulation.ManagerCareer;
using FootballCareerSimulator.Simulation.WorldCalendar;

namespace FootballCareerSimulator.Simulation.Career;

public static class CareerCanonicalStateHasher
{
    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(league);
        ArgumentNullException.ThrowIfNull(clubRegistry);
        ArgumentNullException.ThrowIfNull(managerCareer);

        var canonicalText = string.Concat(
            WorldTimelineCanonicalStateHasher.BuildCanonicalText(timeline),
            "|",
            CompetitionCanonicalStateHasher.BuildCanonicalText(league),
            "|",
            ClubRegistryCanonicalStateHasher.BuildCanonicalText(clubRegistry),
            "|",
            ManagerCareerCanonicalStateHasher.BuildCanonicalText(managerCareer));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText));
        return Convert.ToHexString(hashBytes);
    }
}
