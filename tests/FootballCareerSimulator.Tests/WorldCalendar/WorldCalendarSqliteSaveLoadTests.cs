using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure;
using FootballCareerSimulator.Infrastructure.WorldCalendar;
using FootballCareerSimulator.Simulation.Career;
using FootballCareerSimulator.Simulation.WorldCalendar;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public sealed class WorldCalendarSqliteSaveLoadTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly WorldCalendarSqlitePersistence _persistence = new();

    public WorldCalendarSqliteSaveLoadTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "fcs-worldcalendar-save-tests", Guid.NewGuid().ToString("N"));
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

    private static LeagueCompetition EmptyLeague() => new(new CompetitionId(1));

    private static LeagueClubRegistry DefaultClubs() => LeagueClubRegistry.CreateMvpLeague();

    private static Domain.ManagerCareer.ManagerCareer DefaultManager(GameDate startDate) =>
        Domain.ManagerCareer.ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "Teknik Direktör",
            new ClubId(1),
            startDate,
            clubSportiveStrength: 50);

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesCanonicalState()
    {
        var startDate = GameDate.FromCalendarDate(2026, 7, 1);
        var module = WorldCalendarModule.Create(startDate, rootSeed: 42);
        module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                GameDate.FromCalendarDate(2026, 7, 10).DayNumber));

        var path = GetSavePath("roundtrip.db");
        var timeline = module.TimelineStore.Timeline;
        var expectedHash = CareerCanonicalStateHasher.ComputeHash(
            timeline, EmptyLeague(), DefaultClubs(), DefaultManager(timeline.CurrentDate));

        _persistence.Save(path, timeline);
        var loaded = _persistence.Load(path);

        Assert.False(loaded.WasMigrated);
        Assert.Equal(
            expectedHash,
            CareerCanonicalStateHasher.ComputeHash(
                loaded.Timeline, EmptyLeague(), DefaultClubs(), DefaultManager(loaded.Timeline.CurrentDate)));
        Assert.Equal(timeline.CurrentDate, loaded.Timeline.CurrentDate);
        Assert.Equal(timeline.LastCommittedStepId, loaded.Timeline.LastCommittedStepId);
        Assert.Equal(timeline.RngDrawCount, loaded.Timeline.RngDrawCount);
    }

    [Fact]
    public void SaveAndLoad_WithPlanningPeriod_PreservesActivePeriod()
    {
        var startDate = GameDate.FromCalendarDate(2026, 7, 1);
        var module = WorldCalendarModule.Create(startDate, rootSeed: 7);
        module.TimelineStore.Timeline.OpenPlanningPeriod(new PlanningPeriodId(3), startDate);

        var path = GetSavePath("planning.db");
        _persistence.Save(path, module.TimelineStore.Timeline);
        var loaded = _persistence.Load(path);

        Assert.NotNull(loaded.Timeline.ActivePlanningPeriod);
        Assert.Equal(3, loaded.Timeline.ActivePlanningPeriod.Id.Value);
        Assert.Equal(PlanningPeriodStatus.Open, loaded.Timeline.ActivePlanningPeriod.Status);
    }

    [Fact]
    public void Load_LegacyProductionV1Save_MigratesToCurrentVersion()
    {
        var startDate = GameDate.FromCalendarDate(2026, 7, 1);
        var timeline = WorldTimeline.Rehydrate(
            startDate.AddDays(5),
            new SimulationStepId(5),
            rootSeed: 9,
            rngVersion: "1",
            rngDrawCount: 2,
            activePlanningPeriod: null);
        var hash = WorldTimelineCanonicalStateHasher.ComputeHash(timeline);
        var path = GetSavePath("legacy-v1.db");

        LegacyProductionWorldCalendarSaveFixture.CreateV1File(
            path,
            timeline.CurrentDate.DayNumber,
            timeline.LastCommittedStepId.Value,
            timeline.RootSeed,
            timeline.RngVersion,
            timeline.RngDrawCount,
            hash);

        var loaded = _persistence.Load(path);

        Assert.True(loaded.WasMigrated);
        Assert.Equal(timeline.CurrentDate, loaded.Timeline.CurrentDate);
        Assert.Equal(timeline.LastCommittedStepId, loaded.Timeline.LastCommittedStepId);
        Assert.Equal(
            CareerCanonicalStateHasher.ComputeHash(
                timeline, EmptyLeague(), DefaultClubs(), DefaultManager(timeline.CurrentDate)),
            CareerCanonicalStateHasher.ComputeHash(
                loaded.Timeline, EmptyLeague(), DefaultClubs(), DefaultManager(loaded.Timeline.CurrentDate)));
        Assert.True(File.Exists(path + ".bak"));
    }

    [Fact]
    public void Load_SpikePlaceholderSave_IsRejected()
    {
        var path = GetSavePath("spike.db");
        LegacySpikeSaveFixture.CreateMinimalFile(path);

        Assert.Throws<UnsupportedLegacySpikeSaveException>(() => _persistence.Load(path));
    }

    [Fact]
    public void Load_TamperedHash_IsRejected()
    {
        var startDate = GameDate.FromCalendarDate(2026, 7, 1);
        var module = WorldCalendarModule.Create(startDate);
        var path = GetSavePath("tampered.db");

        _persistence.Save(path, module.TimelineStore.Timeline);

        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE ProductionSaveManifest SET CanonicalStateHash = 'BAD';";
            command.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();

        Assert.Throws<SaveCorruptionException>(() => _persistence.Load(path));
    }
}
