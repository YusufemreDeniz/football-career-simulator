using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class StartingOpportunityPromiseTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-promise-tests",
        Guid.NewGuid().ToString("N"));

    public StartingOpportunityPromiseTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Create_StartsActive_AndDuplicateRejected()
    {
        var social = SocialContinuityModule.Create();
        var promise = social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 3,
            deadlineOn: Day.AddDays(30),
            createdOn: Day);

        Assert.Equal(PromiseStatus.Active, promise.Status);
        Assert.Equal(PromiseKind.StartingOpportunity, promise.Kind);
        Assert.Equal(0, promise.StartsGiven);

        Assert.Throws<SocialContinuityInvariantViolationException>(() =>
            social.StartingOpportunity.Create(
                new ManagerId(1),
                new PlayerId(1001),
                new ClubId(1),
                targetStarts: 2,
                deadlineOn: Day.AddDays(20),
                createdOn: Day));
    }

    [Fact]
    public void RecordStarts_IsIdempotentPerFixture_AndFulfillsEarly()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 2,
            deadlineOn: Day.AddDays(30),
            createdOn: Day);

        social.StartingOpportunity.RecordStartsForPlayers(
            new FixtureId(10),
            new ClubId(1),
            [new PlayerId(1001)],
            Day);
        social.StartingOpportunity.RecordStartsForPlayers(
            new FixtureId(10),
            new ClubId(1),
            [new PlayerId(1001)],
            Day);

        var afterFirst = social.PromiseStore.Promises.Single();
        Assert.Equal(1, afterFirst.StartsGiven);
        Assert.Equal(PromiseStatus.Active, afterFirst.Status);

        social.StartingOpportunity.RecordStartsForPlayers(
            new FixtureId(11),
            new ClubId(1),
            [new PlayerId(1001)],
            Day.AddDays(7));

        var fulfilled = social.PromiseStore.Promises.Single();
        Assert.Equal(2, fulfilled.StartsGiven);
        Assert.Equal(PromiseStatus.Fulfilled, fulfilled.Status);
    }

    [Fact]
    public void EvaluateDeadline_BreaksWhenTargetMissed()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 3,
            deadlineOn: Day.AddDays(10),
            createdOn: Day);

        var resolved = social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(10));
        Assert.Equal(1, resolved);
        Assert.Equal(PromiseStatus.Broken, social.PromiseStore.Promises.Single().Status);
    }

    [Fact]
    public void PlayFixtureMatch_AdvancesStartingOpportunityPromise()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 11);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var selectionStore = new InMemoryMatchSelectionStore();
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore,
            startingOpportunityPromises: social.StartingOpportunity);
        var teamPrep = TeamPreparationModule.Create(competition.Store, manager.Store, selectionStore);

        var playerId = PlayerId.FromClubSlot(1, 0);
        social.StartingOpportunity.Create(
            manager.Store.Career.ManagerId,
            playerId,
            new ClubId(1),
            targetStarts: 1,
            deadlineOn: Day.AddDays(40),
            createdOn: Day);

        const long seasonId = 1;
        competition.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, Day.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), seasonId, Day.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                Day.DayNumber,
                StartingFixtureId: 1));

        var fixture = competition.Queries.GetSeasonFixtures(seasonId)
            .First(f => f.HomeClubId == 1 || f.AwayClubId == 1);

        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixture.FixtureId, ClubId: 1));

        competition.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(
                Guid.NewGuid(),
                seasonId,
                fixture.FixtureId,
                Day.DayNumber));

        var promise = social.PromiseStore.Promises.Single();
        Assert.Equal(1, promise.StartsGiven);
        Assert.Equal(PromiseStatus.Fulfilled, promise.Status);
    }

    [Fact]
    public void SaveLoad_PreservesPromiseAtSchemaV31()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 4,
            deadlineOn: Day.AddDays(21),
            createdOn: Day);
        social.StartingOpportunity.RecordStartsForPlayers(
            new FixtureId(5),
            new ClubId(1),
            [new PlayerId(1001)],
            Day);

        var world = WorldCalendarModule.Create(Day, rootSeed: 5);
        var path = Path.Combine(_tempDirectory, "promise.db");
        new CareerSqlitePersistence().Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            ClubGovernanceModule.CreateMvpLeague().Store.Registry,
            ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1).Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<Domain.TrainingPhysicalState.WeeklyTrainingPlan>(),
            Array.Empty<Domain.TrainingPhysicalState.PlayerPhysicalState>(),
            Array.Empty<PlayerCareerAggregate>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<TacticPlan>(),
            Array.Empty<Domain.Transfer.TransferNeed>(),
            Array.Empty<Domain.Transfer.ShortlistEntry>(),
            Array.Empty<Domain.Transfer.TransferTarget>(),
            Array.Empty<Domain.Transfer.TransferProcess>(),
            Array.Empty<Domain.Transfer.ClubOffer>(),
            Array.Empty<Domain.Transfer.PlayerContractProposal>(),
            social.PromiseStore.Promises,
            social.MemoryStore.Memories);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(48, loaded.SchemaVersion);
        Assert.Single(loaded.Promises);
        Assert.Equal(4, loaded.Promises[0].TargetStarts);
        Assert.Equal(1, loaded.Promises[0].StartsGiven);
        Assert.Contains(5L, loaded.Promises[0].CountedFixtureIds);
        Assert.Equal(PromiseStatus.Active, loaded.Promises[0].Status);
    }
}
