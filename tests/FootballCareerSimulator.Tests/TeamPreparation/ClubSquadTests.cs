using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class ClubSquadTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-club-squad",
        Guid.NewGuid().ToString("N"));

    public ClubSquadTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SyncFromActiveContracts_BuildsMembershipFromContracts()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 5);
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        var contracts = ContractRegistrationModule.Create(
            players.Store,
            manager.Store,
            world.TimelineStore);
        players = PlayerCareerModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            players.Store,
            contracts.Registration);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            contractStore: contracts.Store,
            playerCareerStore: players.Store);

        players.Development.EnsureClub(new ClubId(1), world.TimelineStore.Timeline.RootSeed, Day);
        var squad = teamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);

        Assert.Equal(25, squad.Members.Count);
        Assert.Equal(25, teamPrep.SquadQueries.GetClubSquad(1, world.TimelineStore.Timeline.RootSeed).Count);
        Assert.True(squad.ContainsSlot(0));
        Assert.True(squad.ContainsPlayer(PlayerId.FromClubSlot(1, 0)));
    }

    [Fact]
    public void Sync_RemovesMembersWhenContractExpires()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 3);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        var contracts = ContractRegistrationModule.Create(
            players.Store,
            manager.Store,
            world.TimelineStore);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            contractStore: contracts.Store,
            playerCareerStore: players.Store);

        var playerId = PlayerId.FromClubSlot(1, 0);
        players.Store.Upsert(Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1),
            slotIndex: 0,
            currentAbility: 55,
            potentialAbility: 70,
            birthYear: 2001));
        contracts.Store.Upsert(PlayerContract.Activate(
            playerId,
            new ClubId(1),
            Day,
            GameDate.FromCalendarDate(2026, 7, 2),
            weeklyWage: 900));

        teamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);
        Assert.Single(teamPrep.SquadStore.Get(new ClubId(1))!.Members);

        contracts.Registration.ExpireDueContracts(GameDate.FromCalendarDate(2026, 7, 3));
        var after = teamPrep.ClubSquad.SyncFromActiveContracts(
            new ClubId(1),
            GameDate.FromCalendarDate(2026, 7, 3));

        Assert.Empty(after.Members);
    }

    [Fact]
    public void ApproveSelection_RejectsSlotOutsideClubSquad()
    {
        var clubId = new ClubId(1);
        var squad = ClubSquad.Empty(clubId)
            .EnsureMember(PlayerId.FromClubSlot(1, 0), 0, Day)
            .EnsureMember(PlayerId.FromClubSlot(1, 1), 1, Day);

        Assert.Throws<TeamPreparationInvariantViolationException>(() =>
            MatchSelection.ApproveDefault(new FixtureId(1), clubId, squad));
    }

    [Fact]
    public void SaveLoad_PreservesClubSquadAtSchemaV16()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 7);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        var contracts = ContractRegistrationModule.Create(
            players.Store,
            manager.Store,
            world.TimelineStore);
        players = PlayerCareerModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            players.Store,
            contracts.Registration);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            contractStore: contracts.Store,
            playerCareerStore: players.Store);

        players.Development.EnsureClub(new ClubId(1), world.TimelineStore.Timeline.RootSeed, Day);
        teamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "squad.db");
        persistence.Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            clubs.Store.Registry,
            manager.Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<WeeklyTrainingPlan>(),
            Array.Empty<PlayerPhysicalState>(),
            players.Store.Careers,
            contracts.Store.Contracts,
            teamPrep.SquadStore.Squads,
            Array.Empty<PlayerFreeAgency>());

        var loaded = persistence.Load(path);
        Assert.Equal(17, loaded.SchemaVersion);
        Assert.Single(loaded.ClubSquads);
        Assert.Equal(25, loaded.ClubSquads[0].Members.Count);
        Assert.Equal(1, loaded.ClubSquads[0].ClubId.Value);
    }
}
