using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.EventRuleEvaluation;

public sealed class EventEffectIdempotencySaveLoadTests : IDisposable
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory;
    private readonly CareerSqlitePersistence _persistence = new();

    public EventEffectIdempotencySaveLoadTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "fcs-effect-save-tests", Guid.NewGuid().ToString("N"));
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
    public void SaveAndLoad_PreservesEventEffectProcessingKeys()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 42);
        var advance = world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                GameDate.FromCalendarDate(2026, 7, 4).DayNumber));
        Assert.True(advance.AppliedEffectCount > 0);

        var keys = world.EventRuleEvaluation!.Registry.SnapshotKeys();
        Assert.NotEmpty(keys);

        var competition = CompetitionModule.CreateNewLeague();
        competition.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), 1, PreseasonStart.DayNumber));

        var path = Path.Combine(_tempDirectory, "effect-keys.db");
        var manager = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "Teknik Direktör",
            new ClubId(1),
            world.TimelineStore.Timeline.CurrentDate,
            clubSportiveStrength: 50);

        _persistence.Save(
            path,
            world.TimelineStore.Timeline,
            competition.Store.League,
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
            eventEffectProcessingKeys: keys);

        var loaded = _persistence.Load(path);
        Assert.Equal(46, loaded.SchemaVersion);
        Assert.Equal(keys, loaded.EventEffectProcessingKeys);

        var registry = world.EventRuleEvaluation.Registry;
        registry.Clear();
        registry.ReplaceAll(loaded.EventEffectProcessingKeys!);
        Assert.Equal(
            EventEffectApplicationStatus.Duplicate,
            world.EventRuleEvaluation.Gate.TryApply(new EventEffectProcessingKey(keys[0])));
    }
}
