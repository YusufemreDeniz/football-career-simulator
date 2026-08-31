using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Simulation.Career;
using FootballCareerSimulator.Simulation.Competition;
using FootballCareerSimulator.Simulation.WorldCalendar;
using FootballCareerSimulator.Tests.WorldCalendar;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Career;

public sealed class CareerSqliteSaveLoadTests : IDisposable
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory;
    private readonly CareerSqlitePersistence _persistence = new();

    public CareerSqliteSaveLoadTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "fcs-career-save-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private string GetSavePath(string name) => Path.Combine(_tempDirectory, name);

    private static void RegisterFullLeague(CompetitionModule module, long seasonId)
    {
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            module.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }
    }

    private static Domain.ManagerCareer.ManagerCareer DefaultManager(GameDate startDate) =>
        Domain.ManagerCareer.ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "Teknik Direktör",
            new ClubId(1),
            startDate,
            clubSportiveStrength: 50);

    private static (WorldCalendarModule World, CompetitionModule Competition) CreateCareerState()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 42);
        world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                GameDate.FromCalendarDate(2026, 7, 15).DayNumber));

        var competition = CompetitionModule.CreateNewLeague();
        const long seasonId = 1;

        competition.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        RegisterFullLeague(competition, seasonId);
        competition.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                FirstMatchday.DayNumber,
                StartingFixtureId: 1));

        return (world, competition);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesTimelineAndCompetition()
    {
        var (world, competition) = CreateCareerState();
        var timeline = world.TimelineStore.Timeline;
        var league = competition.Store.League;
        var clubs = LeagueClubRegistry.CreateMvpLeague();
        var manager = DefaultManager(timeline.CurrentDate);
        var path = GetSavePath("career-roundtrip.db");
        var selections = Array.Empty<FootballCareerSimulator.Domain.TeamPreparation.MatchSelection>();
        var expectedHash = CareerCanonicalStateHasher.ComputeHash(
            timeline, league, clubs, manager, selections);

        _persistence.Save(
            path,
            timeline,
            league,
            clubs,
            manager,
            selections,
            Array.Empty<Domain.TrainingPhysicalState.WeeklyTrainingPlan>(),
            Array.Empty<Domain.TrainingPhysicalState.PlayerPhysicalState>(),
            Array.Empty<Domain.PlayerCareer.PlayerCareer>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<Domain.TeamPreparation.ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<Domain.TeamPreparation.TacticPlan>(),
            Array.Empty<Domain.Transfer.TransferNeed>(),
            Array.Empty<Domain.Transfer.ShortlistEntry>(),
            Array.Empty<Domain.Transfer.TransferTarget>(),
            Array.Empty<Domain.Transfer.TransferProcess>(),
            Array.Empty<Domain.Transfer.ClubOffer>(),
            Array.Empty<Domain.Transfer.PlayerContractProposal>(),
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>());
        var loaded = _persistence.Load(path);

        Assert.False(loaded.WasMigrated);
        Assert.Equal(
            expectedHash,
            CareerCanonicalStateHasher.ComputeHash(
                loaded.Timeline,
                loaded.League,
                loaded.ClubRegistry,
                loaded.ManagerCareer,
                loaded.MatchSelections,
                loaded.TrainingPlans,
                loaded.PhysicalStates,
                loaded.PlayerCareers,
                loaded.Contracts,
                loaded.ClubSquads,
                loaded.FreeAgents,
                loaded.TacticPlans,
                loaded.TransferNeeds,
                loaded.ShortlistEntries,
                loaded.TransferTargets,
                loaded.TransferProcesses,
                loaded.ClubOffers));
        Assert.Equal(timeline.CurrentDate, loaded.Timeline.CurrentDate);
        Assert.Equal(
            CompetitionMvpConstraints.TotalLeagueFixtures,
            loaded.League.Seasons.Single().Fixtures.Count);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesFinanceLedgerAndDualPhaseTactic()
    {
        var (world, competition) = CreateCareerState();
        var clubId = new ClubId(1);
        var entry = ClubLedgerEntry.Create(
            new ClubLedgerEntryId("fixture:1:ticket"),
            clubId,
            world.TimelineStore.Timeline.CurrentDate,
            ClubLedgerCategory.MatchdayTicketRevenue,
            750_000,
            "İç saha bilet geliri");
        var ledger = ClubFinanceLedger.Restore(clubId, 10_000_000, [entry]);
        var phasePlan = DualPhaseTacticPlan.Set(
            clubId,
            Formation.F433,
            Formation.F442,
            TacticalPhaseRole.WideOverloads,
            TacticalPhaseRole.CompactBlock,
            world.TimelineStore.Timeline.CurrentDate);
        var path = GetSavePath("career-v47-roundtrip.db");

        _persistence.Save(
            path,
            world.TimelineStore.Timeline,
            competition.Store.League,
            LeagueClubRegistry.CreateMvpLeague(),
            DefaultManager(world.TimelineStore.Timeline.CurrentDate),
            [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [],
            clubFinanceLedgers: [ledger],
            dualPhaseTacticPlans: [phasePlan]);

        var loaded = _persistence.Load(path);

        Assert.Equal(47, loaded.SchemaVersion);
        var loadedLedger = Assert.Single(loaded.ClubFinanceLedgers!);
        Assert.Equal(10_750_000, loadedLedger.Balance);
        Assert.Equal("fixture:1:ticket", Assert.Single(loadedLedger.Entries).Id.Value);
        var loadedPhase = Assert.Single(loaded.DualPhaseTacticPlans!);
        Assert.Equal(Formation.F433, loadedPhase.InPossessionFormation);
        Assert.Equal(Formation.F442, loadedPhase.OutOfPossessionFormation);
        Assert.Equal(TacticalPhaseRole.CompactBlock, loadedPhase.OutOfPossessionRole);
    }

    [Fact]
    public void Load_LegacyV2Save_MigratesToV3WithEmptyCompetition()
    {
        var timeline = WorldTimeline.Rehydrate(
            PreseasonStart.AddDays(5),
            new SimulationStepId(5),
            rootSeed: 9,
            rngVersion: "1",
            rngDrawCount: 2,
            activePlanningPeriod: null);
        var hash = WorldTimelineCanonicalStateHasher.ComputeHash(timeline);
        var path = GetSavePath("legacy-v2.db");

        LegacyProductionWorldCalendarSaveFixture.CreateV2File(
            path,
            timeline.CurrentDate.DayNumber,
            timeline.LastCommittedStepId.Value,
            timeline.RootSeed,
            timeline.RngVersion,
            timeline.RngDrawCount,
            hash);

        var loaded = _persistence.Load(path);

        Assert.True(loaded.WasMigrated);
        Assert.Equal(47, loaded.SchemaVersion);
        Assert.Empty(loaded.League.Seasons);
        Assert.Equal(1, loaded.League.CompetitionId.Value);
        Assert.Equal(
            CareerCanonicalStateHasher.ComputeHash(
                timeline,
                loaded.League,
                loaded.ClubRegistry,
                loaded.ManagerCareer,
                loaded.MatchSelections),
            CareerCanonicalStateHasher.ComputeHash(
                loaded.Timeline,
                loaded.League,
                loaded.ClubRegistry,
                loaded.ManagerCareer,
                loaded.MatchSelections));
        Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, loaded.ClubRegistry.Clubs.Count);
        Assert.Empty(loaded.MatchSelections);
        Assert.True(File.Exists(path + ".bak"));
    }
}
