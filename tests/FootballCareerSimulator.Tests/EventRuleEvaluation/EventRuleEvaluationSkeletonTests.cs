using FootballCareerSimulator.Application.EventRuleEvaluation.Composition;
using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
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

        Assert.Equal(2, first.Effects.Count);
        Assert.All(first.Effects, e => Assert.Equal(EventEffectApplicationStatus.Applied, e.Status));
        Assert.Equal(first.Effects[0].Envelope.EventId, second.Effects[0].Envelope.EventId);
        Assert.Equal(first.Effects[1].Envelope.EventId, second.Effects[1].Envelope.EventId);
        Assert.Equal(correlation, first.Effects[0].Envelope.CorrelationId);
        Assert.Null(first.Effects[0].Envelope.CausationId);
        Assert.Equal(first.Effects[0].Envelope.EventId, first.Effects[1].Envelope.CausationId);
        Assert.Contains(
            first.ReactionIntents,
            intent => intent.IntentTypeCode == ObserveGameDayStartedReactionRule.IntentTypeCode);
    }

    [Fact]
    public void Evaluate_SameEventsTwice_MarksDuplicates_AndSkipsReactions()
    {
        var module = EventRuleEvaluationModule.Create();
        var events = new WorldCalendarDomainEvent[]
        {
            new GameDayStarted(new SimulationStepId(3), Day),
        };
        var correlation = Guid.NewGuid();

        var first = module.WorldCalendarEvaluation.Evaluate(events, rootSeed: 3, correlation);
        var second = module.WorldCalendarEvaluation.Evaluate(events, rootSeed: 3, correlation);

        Assert.Equal(EventEffectApplicationStatus.Applied, first.Effects[0].Status);
        Assert.Equal(EventEffectApplicationStatus.Duplicate, second.Effects[0].Status);
        Assert.Equal(first.Effects[0].Envelope.EventId, second.Effects[0].Envelope.EventId);
        Assert.Single(first.ReactionIntents);
        Assert.Empty(second.ReactionIntents);
    }
}

public sealed class ObserveGameDayStartedReactionRuleTests
{
    [Fact]
    public void Advance_EmitsDayBoundaryObservedIntents()
    {
        var start = GameDate.FromCalendarDate(2026, 7, 1);
        var module = WorldCalendarModule.Create(start, rootSeed: 11);
        var target = GameDate.FromCalendarDate(2026, 7, 3);

        var result = module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), target.DayNumber));

        Assert.True(result.Succeeded);
        Assert.True(result.ReactionIntentCount > 0);
        Assert.Contains(
            ObserveGameDayStartedReactionRule.IntentTypeCode,
            result.ReactionIntentTypeCodes);
        Assert.Equal(
            result.RaisedEventTypes.Count(t => t == nameof(GameDayStarted)),
            result.ReactionIntentCount);
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
