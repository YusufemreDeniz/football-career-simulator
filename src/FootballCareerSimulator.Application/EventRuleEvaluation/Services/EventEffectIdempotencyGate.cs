using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Services;

/// <summary>
/// Consumer effect'i bir kez uygular; tekrarında Duplicate döner. Business state değiştirmez.
/// </summary>
public sealed class EventEffectIdempotencyGate
{
    public const string WorldCalendarConsumerId = "WorldCalendar";
    public const string CommitEffectType = "Commit";

    private readonly IEventEffectIdempotencyRegistry _registry;

    public EventEffectIdempotencyGate(IEventEffectIdempotencyRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public EventEffectApplicationStatus TryApply(EventEffectProcessingKey key)
    {
        return _registry.TryAdd(key)
            ? EventEffectApplicationStatus.Applied
            : EventEffectApplicationStatus.Duplicate;
    }

    public EventEffectApplicationStatus TryApplyCommit(Guid eventId) =>
        TryApply(EventEffectProcessingKey.ForConsumerEffect(
            WorldCalendarConsumerId,
            eventId,
            CommitEffectType));
}
