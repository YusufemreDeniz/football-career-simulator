using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.Competition.Services;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation;
namespace FootballCareerSimulator.Tests.Competition;

public sealed class UnplayedFixturesTimeAdvanceBlockerTests
{
    private static readonly GameDate StartDate = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void GetActiveBlockers_WhenDueFixturesUnplayed_ReturnsHardBlocker()
    {
        var competitionStore = new InMemoryLeagueCompetitionStore(new LeagueCompetition(new CompetitionId(1)));
        var timelineStore = new InMemoryWorldTimelineStore(
            WorldTimeline.Create(FirstMatchday, rootSeed: 1, SimulationRandomContext.Version));
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var module = CompetitionModule.CreateForCareerFromStore(
            competitionStore,
            timelineStore,
            clubs.Store);

        SetupSeasonWithFixtures(module, seasonId: 1);

        var blocker = new UnplayedFixturesTimeAdvanceBlockerSource(competitionStore, timelineStore);
        var blockers = blocker.GetActiveBlockers();

        Assert.Single(blockers);
        Assert.True(blockers[0].IsHardBlocker);
        Assert.Equal(UnplayedFixturesTimeAdvanceBlockerSource.BlockerTypeCode, blockers[0].BlockerTypeCode);
    }

    [Fact]
    public void AdvanceSimulationTime_WithDueUnplayedFixtures_IsBlocked()
    {
        var competitionStore = new InMemoryLeagueCompetitionStore(new LeagueCompetition(new CompetitionId(1)));
        var timelineStore = new InMemoryWorldTimelineStore(
            WorldTimeline.Create(FirstMatchday, rootSeed: 1, SimulationRandomContext.Version));
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var module = CompetitionModule.CreateForCareerFromStore(
            competitionStore,
            timelineStore,
            clubs.Store);
        SetupSeasonWithFixtures(module, seasonId: 1);

        var world = WorldCalendarModule.Create(
            FirstMatchday,
            rootSeed: 1,
            blockerSources: [new UnplayedFixturesTimeAdvanceBlockerSource(competitionStore, timelineStore)],
            timelineStore: timelineStore);

        var result = world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                FirstMatchday.DayNumber + 1));

        Assert.True(result.WasBlocked);
        Assert.Equal(FirstMatchday.DayNumber, world.Queries.GetCurrentGameDate().DayNumber);
    }

    private static void SetupSeasonWithFixtures(CompetitionModule module, long seasonId)
    {
        module.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, StartDate.DayNumber));

        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            module.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }

        module.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, StartDate.DayNumber));
        module.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                FirstMatchday.DayNumber,
                StartingFixtureId: 1));
    }
}