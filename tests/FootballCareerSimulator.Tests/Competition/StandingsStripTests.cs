using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class StandingsStripTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private static CompetitionModule CreateModuleWithFixtures()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 3);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var module = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);

        module.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            module.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, club));
        }

        module.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        module.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, Day.DayNumber, StartingFixtureId: 1));
        return module;
    }

    [Fact]
    public void GetStandingsStrip_MarksManagedClub_WhenInsideTop()
    {
        var module = CreateModuleWithFixtures();
        // Force club 1 to the top by accepting results with club 1 wins where possible.
        foreach (var fixture in module.Queries.GetSeasonFixtures(1).Take(10))
        {
            var homeWins = fixture.HomeClubId == 1;
            var awayWins = fixture.AwayClubId == 1;
            var score = homeWins
                ? new MatchScore(2, 0)
                : awayWins
                    ? new MatchScore(0, 2)
                    : new MatchScore(1, 1);
            module.Store.League.AcceptFixtureResult(
                new SeasonId(1),
                new FixtureId(fixture.FixtureId),
                score,
                GameDate.FromDayNumber(fixture.ScheduledDayNumber));
        }

        var strip = module.Queries.GetStandingsStrip(1, managedClubId: 1, topCount: 5);
        Assert.Contains(strip.Entries, entry => entry.IsManaged && entry.ClubId == 1);
        Assert.False(strip.ManagedOutsideTop);
        Assert.True(strip.Entries.Count <= 5);
        Assert.Equal(1, strip.Entries.Count(entry => entry.IsManaged));
    }

    [Fact]
    public void GetStandingsStrip_AppendsManagedClub_WhenOutsideTop()
    {
        var module = CreateModuleWithFixtures();
        // Prefer wins for other clubs; leave club 1 weak.
        foreach (var fixture in module.Queries.GetSeasonFixtures(1).Take(30))
        {
            MatchScore score;
            if (fixture.HomeClubId == 1)
            {
                score = new MatchScore(0, 2);
            }
            else if (fixture.AwayClubId == 1)
            {
                score = new MatchScore(2, 0);
            }
            else
            {
                score = new MatchScore(2, 0);
            }

            module.Store.League.AcceptFixtureResult(
                new SeasonId(1),
                new FixtureId(fixture.FixtureId),
                score,
                GameDate.FromDayNumber(fixture.ScheduledDayNumber));
        }

        var full = module.Queries.GetStandings(1);
        var managedRank = full.Select((entry, index) => (entry.ClubId, Rank: index + 1))
            .First(entry => entry.ClubId == 1)
            .Rank;
        Assert.True(managedRank > 5, $"Expected club 1 outside top 5, got rank {managedRank}.");

        var strip = module.Queries.GetStandingsStrip(1, managedClubId: 1, topCount: 5);
        Assert.True(strip.ManagedOutsideTop);
        Assert.Equal(6, strip.Entries.Count);
        Assert.Equal(1, strip.Entries[^1].ClubId);
        Assert.True(strip.Entries[^1].IsManaged);
        Assert.Equal(managedRank, strip.Entries[^1].Rank);
        Assert.DoesNotContain(strip.Entries.Take(5), entry => entry.IsManaged);
    }

    [Fact]
    public void GetStandingsStrip_Empty_WhenNoResults()
    {
        var module = CreateModuleWithFixtures();
        var strip = module.Queries.GetStandingsStrip(1, managedClubId: 1);
        Assert.Empty(strip.Entries);
        Assert.False(strip.ManagedOutsideTop);
    }
}
