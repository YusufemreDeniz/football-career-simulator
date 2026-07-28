using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class SeasonTransitionTests
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private static CompetitionModule CreateCareerModule()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 5);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        return CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
    }

    private static void SetupActiveSeason(CompetitionModule module, long seasonId)
    {
        module.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            module.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }

        module.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        module.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                FirstMatchday.DayNumber,
                StartingFixtureId: 1));
    }

    private static void AcceptAllFixtures(CompetitionModule module, long seasonId)
    {
        foreach (var fixture in module.Queries.GetSeasonFixtures(seasonId))
        {
            module.Store.League.AcceptFixtureResult(
                new SeasonId(seasonId),
                new FixtureId(fixture.FixtureId),
                new MatchScore(1, 0),
                GameDate.FromDayNumber(fixture.ScheduledDayNumber));
        }
    }

    [Fact]
    public void GetSeasonProgress_CanComplete_OnlyWhenAllFixturesAccepted()
    {
        var module = CreateCareerModule();
        SetupActiveSeason(module, seasonId: 1);

        var early = module.Queries.GetSeasonProgress(1)!;
        Assert.False(early.CanComplete);
        Assert.False(early.CanArchive);

        AcceptAllFixtures(module, seasonId: 1);

        var ready = module.Queries.GetSeasonProgress(1)!;
        Assert.True(ready.CanComplete);
        Assert.False(ready.CanArchive);
        Assert.Equal(ready.TotalFixtureCount, ready.AcceptedFixtureCount);
    }

    [Fact]
    public void CompleteSeason_FailsWhileFixturesPending()
    {
        var module = CreateCareerModule();
        SetupActiveSeason(module, seasonId: 1);

        Assert.ThrowsAny<Exception>(() =>
            module.CompleteSeason.Handle(
                new CompleteSeasonCommand(Guid.NewGuid(), 1, FirstMatchday.DayNumber)));
    }

    [Fact]
    public void CompleteArchiveStartNext_CreatesActiveSeasonTwo()
    {
        var module = CreateCareerModule();
        SetupActiveSeason(module, seasonId: 1);
        AcceptAllFixtures(module, seasonId: 1);

        module.CompleteSeason.Handle(
            new CompleteSeasonCommand(Guid.NewGuid(), 1, FirstMatchday.AddDays(200).DayNumber));
        Assert.True(module.Queries.GetSeasonProgress(1)!.CanArchive);

        module.ArchiveSeason.Handle(
            new ArchiveSeasonCommand(Guid.NewGuid(), 1, FirstMatchday.AddDays(210).DayNumber));
        Assert.Null(module.Queries.GetCurrentSeason());

        const long nextSeasonId = 2;
        var startDay = FirstMatchday.AddDays(220).DayNumber;
        module.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), nextSeasonId, startDay));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            module.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), nextSeasonId, club));
        }

        module.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), nextSeasonId, startDay));
        module.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                nextSeasonId,
                startDay,
                StartingFixtureId: 1));

        var current = module.Queries.GetCurrentSeason();
        Assert.NotNull(current);
        Assert.Equal(2, current.SeasonId);
        Assert.Equal(nameof(SeasonStatus.Active), current.Status);
        Assert.True(current.FixtureCount > 0);
        Assert.Equal(SeasonStatus.Archived, module.Store.League.Seasons.Single(s => s.SeasonId.Value == 1).Status);
    }
}
