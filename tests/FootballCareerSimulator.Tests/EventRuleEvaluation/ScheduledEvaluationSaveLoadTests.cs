using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
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

public sealed class ScheduledEvaluationSaveLoadTests : IDisposable
{
    private static readonly GameDate Start = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory;
    private readonly CareerSqlitePersistence _persistence = new();

    public ScheduledEvaluationSaveLoadTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "fcs-schedule-save-tests", Guid.NewGuid().ToString("N"));
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
    public void SaveAndLoad_PreservesPendingScheduledEvaluations()
    {
        var world = WorldCalendarModule.Create(Start, rootSeed: 42);
        var closesOn = GameDate.FromCalendarDate(2026, 7, 5);
        world.CloseTransferWindow.Handle(new CloseTransferWindowCommand(Guid.NewGuid()));
        world.OpenTransferWindow.Handle(new OpenTransferWindowCommand(Guid.NewGuid(), closesOn.DayNumber));

        // Schedule without closing: advance to day before closesOn so intent schedules... 
        // Actually schedule only when day >= closesOn. Seed a pending item directly.
        var pending = ScheduledEvaluation.CreatePending(
            new ScheduledEvaluationId(7),
            TransferWindowCloseReactionScheduler.CloseTransferWindowEvaluationType,
            closesOn.DayNumber,
            Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
        world.EventRuleEvaluation!.ScheduledEvaluationStore.ReplaceAll([pending]);

        var competition = CompetitionModuleCreateEmpty();
        var path = Path.Combine(_tempDirectory, "scheduled.db");
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
            scheduledEvaluations: [pending]);

        var loaded = _persistence.Load(path);
        Assert.Equal(42, loaded.SchemaVersion);
        Assert.NotNull(loaded.ScheduledEvaluations);
        Assert.Single(loaded.ScheduledEvaluations!);
        var restored = loaded.ScheduledEvaluations![0];
        Assert.Equal(7, restored.Id.Value);
        Assert.Equal(TransferWindowCloseReactionScheduler.CloseTransferWindowEvaluationType, restored.EvaluationTypeCode);
        Assert.Equal(closesOn.DayNumber, restored.DueDayNumber);
        Assert.Equal(ScheduledEvaluationStatus.Pending, restored.Status);
        Assert.Equal(pending.SourceEventId, restored.SourceEventId);

        world.EventRuleEvaluation.ScheduledEvaluationStore.Clear();
        world.EventRuleEvaluation.ScheduledEvaluationStore.ReplaceAll(loaded.ScheduledEvaluations);
        Assert.Single(world.EventRuleEvaluation.ScheduledEvaluationStore.GetPendingDueThrough(closesOn.DayNumber));
    }

    private static LeagueCompetition CompetitionModuleCreateEmpty() =>
        new(new CompetitionId(1));
}
