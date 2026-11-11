namespace FootballCareerSimulator.Domain.Competition;

using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.Shared;

public sealed class SeasonStandings
{
    private SeasonStandings(IReadOnlyList<StandingEntry> entries)
    {
        Entries = entries;
    }

    public IReadOnlyList<StandingEntry> Entries { get; }

    public static SeasonStandings Empty { get; } = new(Array.Empty<StandingEntry>());

    public static SeasonStandings Rebuild(
        IEnumerable<ClubId> participants,
        IEnumerable<Fixture> fixtures)
    {
        var entries = participants
            .Distinct()
            .ToDictionary(participant => participant, participant => new StandingEntry(participant));

        foreach (var fixture in fixtures.Where(fixture => fixture.Status == FixtureStatus.ResultAccepted))
        {
            if (fixture.HomeGoals is not int homeGoals || fixture.AwayGoals is not int awayGoals)
            {
                throw new CompetitionInvariantViolationException(
                    $"Fixture {fixture.Id.Value} is marked accepted without a score.");
            }

            var score = new MatchScore(homeGoals, awayGoals);
            entries[fixture.HomeClubId].ApplyResult(isHomeClub: true, score);
            entries[fixture.AwayClubId].ApplyResult(isHomeClub: false, score);
        }

        var ordered = entries.Values
            .OrderByDescending(entry => entry.Points.Value)
            .ThenByDescending(entry => entry.GoalDifference)
            .ThenByDescending(entry => entry.GoalsFor)
            .ThenBy(entry => entry.ClubId.Value)
            .ToArray();

        return new SeasonStandings(ordered);
    }
}
