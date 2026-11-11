using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public sealed class StubTimeAdvanceBlockerSource : ITimeAdvanceBlockerSource
{
    private readonly IReadOnlyList<TimeAdvanceBlockerDescriptor> _blockers;

    public StubTimeAdvanceBlockerSource(string sourceContext, params TimeAdvanceBlockerDescriptor[] blockers)
    {
        SourceContext = sourceContext;
        _blockers = blockers;
    }

    public string SourceContext { get; }

    public IReadOnlyList<TimeAdvanceBlockerDescriptor> GetActiveBlockers() => _blockers;
}

public class AdvanceSimulationTimeHandlerTests
{
    private static readonly GameDate StartDate = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void Handle_AdvancesTimelineAndReturnsPrimitiveReadModelFields()
    {
        var module = WorldCalendarModule.Create(StartDate);
        var target = GameDate.FromCalendarDate(2026, 7, 3);

        var result = module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), target.DayNumber));

        Assert.True(result.Succeeded);
        Assert.False(result.WasBlocked);
        Assert.Equal(StartDate.DayNumber, result.PreviousDayNumber);
        Assert.Equal(target.DayNumber, result.NewDayNumber);
        Assert.Contains(nameof(GameTimeAdvanced), result.RaisedEventTypes);
        Assert.Equal(target.DayNumber, module.Queries.GetCurrentGameDate().DayNumber);
    }

    [Fact]
    public void Handle_WithHardBlocker_DoesNotAdvanceTimeline()
    {
        var blocker = new StubTimeAdvanceBlockerSource(
            "TeamPreparation",
            new TimeAdvanceBlockerDescriptor("MissingMatchSquad", "SquadIncomplete", IsHardBlocker: true));

        var module = WorldCalendarModule.Create(StartDate, blockerSources: [blocker]);
        var target = GameDate.FromCalendarDate(2026, 7, 2);

        var result = module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), target.DayNumber));

        Assert.False(result.Succeeded);
        Assert.True(result.WasBlocked);
        Assert.Single(result.Blockers);
        Assert.Equal("TeamPreparation", result.Blockers[0].SourceContext);
        Assert.Equal(StartDate.DayNumber, module.Queries.GetCurrentGameDate().DayNumber);
    }

    [Fact]
    public void Handle_SameCommandId_IsIdempotent()
    {
        var module = WorldCalendarModule.Create(StartDate);
        var commandId = Guid.NewGuid();
        var target = GameDate.FromCalendarDate(2026, 7, 2);
        var command = new AdvanceSimulationTimeCommand(commandId, target.DayNumber);

        var first = module.AdvanceSimulationTime.Handle(command);
        var second = module.AdvanceSimulationTime.Handle(command);

        Assert.Equal(first, second);
        Assert.Equal(target.DayNumber, module.Queries.GetCurrentGameDate().DayNumber);
    }

    [Fact]
    public void GetTimeAdvanceEligibility_ReflectsActiveBlockers()
    {
        var blocker = new StubTimeAdvanceBlockerSource(
            "InteractionNarrative",
            new TimeAdvanceBlockerDescriptor("PendingDecision", "BoardMeetingRequired", IsHardBlocker: true));

        var module = WorldCalendarModule.Create(StartDate, blockerSources: [blocker]);

        var eligibility = module.Queries.GetTimeAdvanceEligibility();

        Assert.False(eligibility.CanAdvance);
        Assert.Equal(StartDate.DayNumber, eligibility.CurrentDayNumber);
        Assert.Equal("InteractionNarrative", eligibility.Blockers[0].SourceContext);
    }

    [Fact]
    public void GetCurrentPlanningPeriod_ReturnsNullWhenNoActivePeriod()
    {
        var module = WorldCalendarModule.Create(StartDate);

        Assert.Null(module.Queries.GetCurrentPlanningPeriod());
    }
}
