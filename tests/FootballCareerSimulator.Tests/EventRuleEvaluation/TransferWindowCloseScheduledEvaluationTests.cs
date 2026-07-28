using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.EventRuleEvaluation;

public sealed class TransferWindowCloseScheduledEvaluationTests
{
    [Fact]
    public void Advance_OnClosesOnDay_SchedulesAndClosesTransferWindow()
    {
        var start = GameDate.FromCalendarDate(2026, 7, 1);
        var closesOn = GameDate.FromCalendarDate(2026, 7, 3);
        var module = WorldCalendarModule.Create(start, rootSeed: 19);

        module.CloseTransferWindow.Handle(new CloseTransferWindowCommand(Guid.NewGuid()));
        module.OpenTransferWindow.Handle(
            new OpenTransferWindowCommand(Guid.NewGuid(), closesOn.DayNumber));
        Assert.True(module.TimelineStore.Timeline.TransferWindow.IsOpen);

        var result = module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), closesOn.DayNumber));

        Assert.True(result.Succeeded);
        Assert.True(result.ReactionIntentCount > 0);
        Assert.Contains(
            ObserveGameDayStartedReactionRule.IntentTypeCode,
            result.ReactionIntentTypeCodes);
        Assert.Equal(1, result.ScheduledEvaluationCount);
        Assert.Equal(1, result.DueEvaluationsProcessed);
        Assert.Equal(1, result.TransferWindowsClosedBySchedule);
        Assert.False(module.TimelineStore.Timeline.TransferWindow.IsOpen);

        var completed = module.EventRuleEvaluation!.ScheduledEvaluationStore.Items
            .Single(item => item.EvaluationTypeCode
                == TransferWindowCloseReactionScheduler.CloseTransferWindowEvaluationType);
        Assert.Equal(ScheduledEvaluationStatus.Completed, completed.Status);
        Assert.Equal(closesOn.DayNumber, completed.DueDayNumber);
    }

    [Fact]
    public void Advance_BeforeClosesOn_DoesNotCloseWindow()
    {
        var start = GameDate.FromCalendarDate(2026, 7, 1);
        var closesOn = GameDate.FromCalendarDate(2026, 7, 10);
        var module = WorldCalendarModule.Create(start, rootSeed: 21);

        module.CloseTransferWindow.Handle(new CloseTransferWindowCommand(Guid.NewGuid()));
        module.OpenTransferWindow.Handle(
            new OpenTransferWindowCommand(Guid.NewGuid(), closesOn.DayNumber));

        var result = module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                GameDate.FromCalendarDate(2026, 7, 2).DayNumber));

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ScheduledEvaluationCount);
        Assert.Equal(0, result.TransferWindowsClosedBySchedule);
        Assert.True(module.TimelineStore.Timeline.TransferWindow.IsOpen);
    }
}
