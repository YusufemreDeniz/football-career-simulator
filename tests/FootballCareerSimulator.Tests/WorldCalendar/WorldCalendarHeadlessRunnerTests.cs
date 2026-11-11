using FootballCareerSimulator.Application.WorldCalendar;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.WorldCalendar.Events;
using FootballCareerSimulator.Simulation.WorldCalendar;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public class WorldCalendarHeadlessRunnerTests
{
    [Fact]
    public void Run_TenSeasons_CompletesWithoutException()
    {
        var report = WorldCalendarHeadlessRunner.Run(seed: 42, seasonCount: 10);

        Assert.Equal(10, report.SeasonCount);
        Assert.Equal(3_650, report.SimulatedDayCount);
        Assert.True(report.FinalDayNumber > GameDate.FromCalendarDate(2026, 7, 1).DayNumber);
        Assert.False(string.IsNullOrWhiteSpace(report.CanonicalStateHash));
        Assert.True(report.CommittedEventCount > 0);
    }

    [Fact]
    public void Run_SameSeed_ProducesSameCanonicalHash()
    {
        var first = WorldCalendarHeadlessRunner.Run(seed: 42, seasonCount: 10);
        var second = WorldCalendarHeadlessRunner.Run(seed: 42, seasonCount: 10);

        Assert.Equal(first.CanonicalStateHash, second.CanonicalStateHash);
        Assert.Equal(first.FinalDayNumber, second.FinalDayNumber);
    }

    [Fact]
    public void EventCommitment_UsesDeterministicEventIdFactory()
    {
        var domainEvent = new GameTimeAdvanced(
            new SimulationStepId(7),
            GameDate.FromCalendarDate(2026, 7, 8),
            GameDate.FromCalendarDate(2026, 7, 7));

        var committed = WorldCalendarEventCommitment.Commit(
            domainEvent,
            correlationId: DeterministicGuidFactory.Create(42, 7),
            causationId: null,
            (_, sequence) => DeterministicGuidFactory.Create(42, sequence));

        Assert.Equal(DeterministicGuidFactory.Create(42, 7), committed.EventId);
        Assert.Equal(new SimulationStepId(7), committed.SimulationStepId);
    }
}
