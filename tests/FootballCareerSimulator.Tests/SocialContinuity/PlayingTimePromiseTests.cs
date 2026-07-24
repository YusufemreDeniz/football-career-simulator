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

public sealed class PlayingTimePromiseTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-playing-time",
        Guid.NewGuid().ToString("N"));

    public PlayingTimePromiseTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void BenchOnlyAppearance_AdvancesPlayingTime_ButNotStartingOpportunity()
    {
        var social = SocialContinuityModule.Create();
        var playerId = new PlayerId(1001);
        social.StartingOpportunity.Create(
            new ManagerId(1),
            playerId,
            new ClubId(1),
            targetStarts: 2,
            deadlineOn: Day.AddDays(30),
            createdOn: Day);
        social.PlayingTime.Create(
            new ManagerId(1),
            playerId,
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(30),
            createdOn: Day);

        social.PlayingTime.RecordAppearancesForPlayers(
            new FixtureId(1),
            new ClubId(1),
            [playerId],
            Day);

        var playing = social.PromiseStore.Promises.Single(p => p.Kind == PromiseKind.PlayingTime);
        var starting = social.PromiseStore.Promises.Single(p => p.Kind == PromiseKind.StartingOpportunity);
        Assert.Equal(1, playing.StartsGiven);
        Assert.Equal(0, starting.StartsGiven);
    }

    [Fact]
    public void Deadline_BreaksPlayingTimeWhenMissed()
    {
        var social = SocialContinuityModule.Create();
        social.PlayingTime.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetAppearances: 3,
            deadlineOn: Day.AddDays(7),
            createdOn: Day);

        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(7));

        Assert.Equal(
            PromiseStatus.Broken,
            social.PromiseStore.Promises.Single(p => p.Kind == PromiseKind.PlayingTime).Status);
        Assert.Equal(2, social.MemoryStore.Memories.Count(m => m.Category == MemoryCategory.Promise));
        Assert.Equal(1, social.MemoryStore.Memories.Count(m => m.Category == MemoryCategory.Trust));
    }

    [Fact]
    public void PlayFixtureMatch_BenchSlotCountsTowardPlayingTime()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 33);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var selectionStore = new InMemoryMatchSelectionStore();
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore,
            selectionMemory: social.SelectionMemory,
            playingTimePromises: social.PlayingTime);
        var teamPrep = TeamPreparationModule.Create(competition.Store, manager.Store, selectionStore);

        var benchPlayerId = PlayerId.FromClubSlot(1, MatchSelection.StartingXiSize);
        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            benchPlayerId,
            new ClubId(1),
            targetAppearances: 1,
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

        var promise = social.PromiseStore.Promises.Single(p => p.Kind == PromiseKind.PlayingTime);
        Assert.Equal(1, promise.StartsGiven);
        Assert.Equal(PromiseStatus.Fulfilled, promise.Status);
    }

    [Fact]
    public void SaveLoad_PreservesPlayingTimePromise()
    {
        var social = SocialContinuityModule.Create();
        social.PlayingTime.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetAppearances: 6,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);
        social.PlayingTime.RecordAppearancesForPlayers(
            new FixtureId(2),
            new ClubId(1),
            [new PlayerId(1001)],
            Day);

        var world = WorldCalendarModule.Create(Day, rootSeed: 2);
        var path = Path.Combine(_tempDirectory, "playing-time.db");
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
        Assert.Equal(35, loaded.SchemaVersion);
        Assert.Single(loaded.Promises);
        Assert.Equal(PromiseKind.PlayingTime, loaded.Promises[0].Kind);
        Assert.Equal(6, loaded.Promises[0].TargetStarts);
        Assert.Equal(1, loaded.Promises[0].StartsGiven);
    }
}
