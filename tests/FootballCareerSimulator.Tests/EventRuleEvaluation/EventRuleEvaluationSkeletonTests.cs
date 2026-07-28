using FootballCareerSimulator.Application.EventRuleEvaluation.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.WorldCalendar.Events;
using FootballCareerSimulator.Simulation.WorldCalendar;

namespace FootballCareerSimulator.Tests.EventRuleEvaluation;

public sealed class EventEffectIdempotencyGateTests
{
    [Fact]
    public void TryApply_SameKey_SecondCallIsDuplicate()
    {
        var module = EventRuleEvaluationModule.Create();
        var key = EventEffectProcessingKey.ForConsumerEffect("WorldCalendar", Guid.NewGuid(), "Commit");

        Assert.Equal(EventEffectApplicationStatus.Applied, module.Gate.TryApply(key));
        Assert.Equal(EventEffectApplicationStatus.Duplicate, module.Gate.TryApply(key));
        Assert.Equal(1, module.Registry.Count);
    }
}

public sealed class WorldCalendarEventEvaluationServiceTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void Evaluate_IsDeterministic_AndChainsCausation()
    {
        var module = EventRuleEvaluationModule.Create();
        var events = new WorldCalendarDomainEvent[]
        {
            new GameDayStarted(new SimulationStepId(1), Day),
            new GameTimeAdvanced(new SimulationStepId(1), Day, Day),
        };
        var correlation = DeterministicGuidFactory.Create(7, 1);

        var first = module.WorldCalendarEvaluation.Evaluate(events, rootSeed: 7, correlation);
        var secondModule = EventRuleEvaluationModule.Create();
        var second = secondModule.WorldCalendarEvaluation.Evaluate(events, rootSeed: 7, correlation);

        Assert.Equal(2, first.Count);
        Assert.All(first, e => Assert.Equal(EventEffectApplicationStatus.Applied, e.Status));
        Assert.Equal(first[0].Envelope.EventId, second[0].Envelope.EventId);
        Assert.Equal(first[1].Envelope.EventId, second[1].Envelope.EventId);
        Assert.Equal(correlation, first[0].Envelope.CorrelationId);
        Assert.Null(first[0].Envelope.CausationId);
        Assert.Equal(first[0].Envelope.EventId, first[1].Envelope.CausationId);
    }

    [Fact]
    public void Evaluate_SameEventsTwice_MarksDuplicates()
    {
        var module = EventRuleEvaluationModule.Create();
        var events = new WorldCalendarDomainEvent[]
        {
            new GameDayCompleted(new SimulationStepId(3), Day),
        };
        var correlation = Guid.NewGuid();

        var first = module.WorldCalendarEvaluation.Evaluate(events, rootSeed: 3, correlation);
        var second = module.WorldCalendarEvaluation.Evaluate(events, rootSeed: 3, correlation);

        Assert.Equal(EventEffectApplicationStatus.Applied, first[0].Status);
        Assert.Equal(EventEffectApplicationStatus.Duplicate, second[0].Status);
        Assert.Equal(first[0].Envelope.EventId, second[0].Envelope.EventId);
    }
}

public sealed class AdvanceSimulationTimeEventEvaluationTests
{
    [Fact]
    public void Advance_AppliesWorldCalendarEffects()
    {
        var start = GameDate.FromCalendarDate(2026, 7, 1);
        var module = WorldCalendarModule.Create(start, rootSeed: 11);
        var target = GameDate.FromCalendarDate(2026, 7, 2);

        var result = module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), target.DayNumber));

        Assert.True(result.Succeeded);
        Assert.True(result.RaisedEventTypes.Count > 0);
        Assert.Equal(result.RaisedEventTypes.Count, result.AppliedEffectCount);
        Assert.Equal(0, result.DuplicateEffectCount);
        Assert.True(module.EventRuleEvaluation!.Registry.Count >= result.AppliedEffectCount);
    }
}
