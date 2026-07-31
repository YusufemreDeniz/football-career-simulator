using FootballCareerSimulator.Application.CareerHub.Queries;
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
        var world = WorldCalendarModule.Create(Start, rootSeed: 11);
        var competition = new LeagueCompetition(new CompetitionId(1));
        var manager = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "Teknik Direktör",
            new ClubId(1),
            world.TimelineStore.Timeline.CurrentDate,
            clubSportiveStrength: 50);
        var path = Path.Combine(_tempDirectory, "hub.db");
        var hub = HubNarrativeUiState.Compose(
            weekStoryClosureBeat: "Dönenler işe yaradı — Kurt",
            weekStoryDismissOnNextAdvance: true,
            cleanXiNames: ["Tolga Kurt", "Ali Yılmaz"],
            injuryClearedNames: ["Tolga Kurt"]);

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

        var loaded = _persistence.Load(path);
        Assert.Equal(40, loaded.SchemaVersion);
        Assert.NotNull(loaded.HubNarrativeUiState);
        Assert.Equal("Dönenler işe yaradı — Kurt", loaded.HubNarrativeUiState!.WeekStoryClosureBeat);
        Assert.True(loaded.HubNarrativeUiState.WeekStoryDismissOnNextAdvance);
        Assert.Equal(2, loaded.HubNarrativeUiState.CleanXiNames.Count);
        Assert.Contains("Tolga Kurt", loaded.HubNarrativeUiState.CleanXiNames);
        Assert.Contains("Ali Yılmaz", loaded.HubNarrativeUiState.CleanXiNames);
        Assert.Equal(["Tolga Kurt"], loaded.HubNarrativeUiState.InjuryClearedNames);
    }
}
