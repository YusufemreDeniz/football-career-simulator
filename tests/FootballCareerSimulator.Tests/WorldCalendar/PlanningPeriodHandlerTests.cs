using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public class PlanningPeriodHandlerTests
{
    private static readonly GameDate StartDate = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void OpenPlanningPeriod_CreatesActivePeriod()
    {
        var module = WorldCalendarModule.Create(StartDate);

        var result = module.OpenPlanningPeriod.Handle(
            new OpenPlanningPeriodCommand(Guid.NewGuid(), 1, StartDate.DayNumber));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.PlanningPeriodId);
        Assert.Equal(nameof(PlanningPeriodStatus.Open), result.Status);
        Assert.Contains(nameof(PlanningPeriodStarted), result.RaisedEventTypes);

        var period = module.Queries.GetCurrentPlanningPeriod();
        Assert.NotNull(period);
        Assert.Equal(1, period.PlanningPeriodId);
        Assert.Equal(nameof(PlanningPeriodStatus.Open), period.Status);
    }

    [Fact]
    public void CompletePlanningPeriod_ClosesActivePeriod()
    {
        var module = WorldCalendarModule.Create(StartDate);
        module.OpenPlanningPeriod.Handle(
            new OpenPlanningPeriodCommand(Guid.NewGuid(), 1, StartDate.DayNumber));

        var result = module.CompletePlanningPeriod.Handle(
            new CompletePlanningPeriodCommand(Guid.NewGuid()));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.PlanningPeriodId);
        Assert.Equal(nameof(PlanningPeriodStatus.Completed), result.Status);
        Assert.Contains(nameof(PlanningPeriodCompleted), result.RaisedEventTypes);
        Assert.Null(module.Queries.GetCurrentPlanningPeriod());
    }

    [Fact]
    public void OpenPlanningPeriod_RejectsSecondActivePeriod()
    {
        var module = WorldCalendarModule.Create(StartDate);
        module.OpenPlanningPeriod.Handle(
            new OpenPlanningPeriodCommand(Guid.NewGuid(), 1, StartDate.DayNumber));

        Assert.Throws<WorldCalendarInvariantViolationException>(() =>
            module.OpenPlanningPeriod.Handle(
                new OpenPlanningPeriodCommand(Guid.NewGuid(), 2, StartDate.DayNumber)));
    }

    [Fact]
    public void OpenPlanningPeriod_SameCommandId_IsIdempotent()
    {
        var module = WorldCalendarModule.Create(StartDate);
        var commandId = Guid.NewGuid();
        var command = new OpenPlanningPeriodCommand(commandId, 1, StartDate.DayNumber);

        var first = module.OpenPlanningPeriod.Handle(command);
        var second = module.OpenPlanningPeriod.Handle(command);

        Assert.Equal(first, second);
    }

    [Fact]
    public void SaveAndLoad_PreservesOpenPlanningPeriod()
    {
        var persistence = new Infrastructure.WorldCalendar.WorldCalendarSqlitePersistence();
        var module = WorldCalendarModule.CreateNewGame(persistence: persistence);
        var currentDay = module.Queries.GetCurrentGameDate().DayNumber;
        module.OpenPlanningPeriod.Handle(
            new OpenPlanningPeriodCommand(Guid.NewGuid(), 7, currentDay));

        var path = Path.Combine(Path.GetTempPath(), $"fcs-planning-{Guid.NewGuid():N}.db");
        try
        {
            module.GameSession!.Save(path);
            module.CompletePlanningPeriod.Handle(new CompletePlanningPeriodCommand(Guid.NewGuid()));
            module.GameSession.Load(path);

            var period = module.Queries.GetCurrentPlanningPeriod();
            Assert.NotNull(period);
            Assert.Equal(7, period.PlanningPeriodId);
            Assert.Equal(nameof(PlanningPeriodStatus.Open), period.Status);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
