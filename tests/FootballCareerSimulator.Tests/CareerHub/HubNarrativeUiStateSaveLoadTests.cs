using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class HubNarrativeUiStateSaveLoadTests : IDisposable
{
    private static readonly GameDate Start = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory;
    private readonly CareerSqlitePersistence _persistence = new();

    public HubNarrativeUiStateSaveLoadTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "fcs-hub-narrative", Guid.NewGuid().ToString("N"));
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

    [Fact]
    public void SaveAndLoad_PreservesWeekStoryAndCleanXiBridge()
    {
        var path = Path.Combine(_tempDirectory, "hub.db");
        var hub = HubNarrativeUiState.Compose(
            weekStoryClosureBeat: "Dönenler işe yaradı — Kurt",
            weekStoryDismissOnNextAdvance: true,
            cleanXiNames: ["Tolga Kurt", "Ali Yılmaz"],
            injuryClearedNames: ["Tolga Kurt"],
            matchupPlanHistory:
            [
                NotebookEntry(10, "A FK"),
                NotebookEntry(11, "B FK"),
                NotebookEntry(12, "C FK"),
                NotebookEntry(13, "D FK"),
            ]);

        Save(path, hub);

        var loaded = _persistence.Load(path);
        Assert.Equal(45, loaded.SchemaVersion);
        Assert.NotNull(loaded.HubNarrativeUiState);
        Assert.Equal("Dönenler işe yaradı — Kurt", loaded.HubNarrativeUiState!.WeekStoryClosureBeat);
        Assert.True(loaded.HubNarrativeUiState.WeekStoryDismissOnNextAdvance);
        Assert.Equal(2, loaded.HubNarrativeUiState.CleanXiNames.Count);
        Assert.Contains("Tolga Kurt", loaded.HubNarrativeUiState.CleanXiNames);
        Assert.Contains("Ali Yılmaz", loaded.HubNarrativeUiState.CleanXiNames);
        Assert.Equal(["Tolga Kurt"], loaded.HubNarrativeUiState.InjuryClearedNames);
        Assert.Equal(3, loaded.HubNarrativeUiState.MatchupPlanHistory.Count);
        Assert.Equal(
            [11, 12, 13],
            loaded.HubNarrativeUiState.MatchupPlanHistory.Select(entry => entry.DayNumber));
        var latest = loaded.HubNarrativeUiState.MatchupPlanHistory[^1];
        Assert.Equal("D FK", latest.OpponentName);
        Assert.Equal(OpponentThreatKind.ProductiveAttack, latest.ThreatKind);
        Assert.Equal(MatchupPlanSignal.Risk, latest.PlanSignal);
        Assert.Equal(MatchupPlanOutcomeSignal.Warning, latest.OutcomeSignal);
    }

    [Fact]
    public void Load_V40Save_MigratesWithEmptyNotebook()
    {
        var path = Path.Combine(_tempDirectory, "hub-v40.db");
        Save(path, HubNarrativeUiState.Empty);

        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                DROP TABLE MatchupPlanNotebookState;
                UPDATE ProductionSaveManifest SET SchemaVersion = 40;
                """;
            command.ExecuteNonQuery();
        }

        var loaded = _persistence.Load(path);

        Assert.True(loaded.WasMigrated);
        Assert.Equal(45, loaded.SchemaVersion);
        Assert.Empty(loaded.HubNarrativeUiState!.MatchupPlanHistory);
    }

    private void Save(string path, HubNarrativeUiState hub)
    {
        var world = WorldCalendarModule.Create(Start, rootSeed: 11);
        var competition = new LeagueCompetition(new CompetitionId(1));
        var manager = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "Teknik Direktör",
            new ClubId(1),
            world.TimelineStore.Timeline.CurrentDate,
            clubSportiveStrength: 50);
        _persistence.Save(
            path,
            world.TimelineStore.Timeline,
            competition,
            LeagueClubRegistry.CreateMvpLeague(),
            manager,
            Array.Empty<Domain.TeamPreparation.MatchSelection>(),
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
            Array.Empty<Domain.SocialContinuity.MemoryRecord>(),
            hubNarrativeUiState: hub);
    }

    private static MatchupPlanNotebookEntry NotebookEntry(int day, string opponent) =>
        MatchupPlanNotebookEntry.Compose(
            day,
            opponent,
            "Seçim: 4-3-3 · Hücum",
            OpponentThreatKind.ProductiveAttack,
            MatchupPlanSignal.Risk,
            MatchupPlanOutcomeSignal.Warning,
            "Eşleşme riski giderilemedi.");
}
