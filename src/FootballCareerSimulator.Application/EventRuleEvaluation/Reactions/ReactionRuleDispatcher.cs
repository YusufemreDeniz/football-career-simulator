using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;

/// <summary>
/// Applied commit'ler için RuleId sırasıyla reaction çalıştırır; duplicate effect'i atlar.
/// </summary>
public sealed class ReactionRuleDispatcher
{
    public const string ReactionConsumerId = "Reaction";

    private readonly EventEffectIdempotencyGate _gate;
    private readonly IReadOnlyList<IReactionRule> _rules;

    public ReactionRuleDispatcher(
        EventEffectIdempotencyGate gate,
        IEnumerable<IReactionRule> rules)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _rules = (rules ?? throw new ArgumentNullException(nameof(rules)))
            .OrderBy(rule => rule.RuleId, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<ReactionIntent> Dispatch(
        IReadOnlyList<(WorldCalendarDomainEvent DomainEvent, EvaluatedWorldCalendarEffect Effect)> evaluated)
    {
        ArgumentNullException.ThrowIfNull(evaluated);

        var intents = new List<ReactionIntent>();
        foreach (var (domainEvent, effect) in evaluated)
        {
            if (effect.Status != EventEffectApplicationStatus.Applied)
            {
                continue;
            }

            foreach (var rule in _rules)
            {
                var key = EventEffectProcessingKey.ForConsumerEffect(
                    ReactionConsumerId,
                    effect.Envelope.EventId,
                    rule.RuleId);
                if (_gate.TryApply(key) == EventEffectApplicationStatus.Duplicate)
                {
                    continue;
                }

                intents.AddRange(rule.React(domainEvent, effect));
            }
        }

        return intents;
    }
}
