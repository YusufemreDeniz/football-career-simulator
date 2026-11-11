using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public class WorldTimelineTests
{
    private static readonly GameDate SeasonStart = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void AdvanceOneDay_UpdatesDateAndRaisesDomainEvents()
    {
        var timeline = WorldTimeline.Create(SeasonStart);

        var result = timeline.AdvanceOneDay();

        Assert.Equal(GameDate.FromCalendarDate(2026, 7, 2), timeline.CurrentDate);
        Assert.Equal(new SimulationStepId(1), timeline.LastCommittedStepId);
        Assert.Equal(SeasonStart, result.PreviousDate);
        Assert.Equal(3, result.RaisedEvents.Count);
        Assert.Contains(result.RaisedEvents, e => e is GameDayStarted);
        Assert.Contains(result.RaisedEvents, e => e is GameDayCompleted);
        Assert.Contains(result.RaisedEvents, e => e is GameTimeAdvanced);
    }

    [Fact]
    public void AdvanceTo_MovesAcrossMultipleDaysWithDistinctStepIds()
    {
        var timeline = WorldTimeline.Create(SeasonStart);
        var target = GameDate.FromCalendarDate(2026, 7, 4);

        var result = timeline.AdvanceTo(target);

        Assert.Equal(target, timeline.CurrentDate);
        Assert.Equal(new SimulationStepId(3), timeline.LastCommittedStepId);
        Assert.Equal(new SimulationStepId(1), result.FirstCommittedStepId);
        Assert.Equal(9, result.RaisedEvents.Count);
        Assert.Equal(3, result.RaisedEvents.OfType<GameTimeAdvanced>().Count());
    }

    [Fact]
    public void AdvanceTo_RejectsPastOrSameDate()
    {
        var timeline = WorldTimeline.Create(SeasonStart);

        Assert.Throws<WorldCalendarInvariantViolationException>(() => timeline.AdvanceTo(SeasonStart));
        Assert.Throws<WorldCalendarInvariantViolationException>(() =>
            timeline.AdvanceTo(GameDate.FromCalendarDate(2026, 6, 30)));
    }

    [Fact]
    public void OpenPlanningPeriod_RaisesStartedEventAndTracksActivePeriod()
    {
        var timeline = WorldTimeline.Create(SeasonStart);
        var periodId = new PlanningPeriodId(1);

        var period = timeline.OpenPlanningPeriod(periodId, SeasonStart);

        Assert.Equal(PlanningPeriodStatus.Open, period.Status);
        Assert.Same(period, timeline.ActivePlanningPeriod);
        Assert.Contains(timeline.UncommittedEvents, e => e is PlanningPeriodStarted started && started.PlanningPeriodId == periodId);
    }

    [Fact]
    public void CompleteActivePlanningPeriod_TransitionsToCompletedAndCannotCompleteTwice()
    {
        var timeline = WorldTimeline.Create(SeasonStart);
        timeline.OpenPlanningPeriod(new PlanningPeriodId(1), SeasonStart);

        var completed = timeline.CompleteActivePlanningPeriod();

        Assert.Equal(PlanningPeriodStatus.Completed, completed.Status);
        Assert.Equal(SeasonStart, completed.CompletedAt);
        Assert.Throws<WorldCalendarInvariantViolationException>(() => timeline.CompleteActivePlanningPeriod());
    }

    [Fact]
    public void OpenPlanningPeriod_RejectsSecondActivePeriod()
    {
        var timeline = WorldTimeline.Create(SeasonStart);
        timeline.OpenPlanningPeriod(new PlanningPeriodId(1), SeasonStart);

        Assert.Throws<WorldCalendarInvariantViolationException>(() =>
            timeline.OpenPlanningPeriod(new PlanningPeriodId(2), SeasonStart));
    }

    [Fact]
    public void OpenPlanningPeriod_AllowsNewPeriodAfterCompletion()
    {
        var timeline = WorldTimeline.Create(SeasonStart);
        timeline.OpenPlanningPeriod(new PlanningPeriodId(1), SeasonStart);
        timeline.CompleteActivePlanningPeriod();

        var next = timeline.OpenPlanningPeriod(new PlanningPeriodId(2), SeasonStart);

        Assert.Equal(new PlanningPeriodId(2), next.Id);
        Assert.Equal(PlanningPeriodStatus.Open, next.Status);
    }
}
