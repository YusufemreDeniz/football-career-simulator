using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// DayBoundaryObserved reaction → owner EvaluateDeadlines (PlayingTime + StartingOpportunity)
/// (+ isteğe bağlı PromiseBroken karar tetikleyicisi).
/// </summary>
public sealed class PromiseDeadlineDayBoundaryApplier
{
    public const string ConsumerId = "SocialContinuity";
    public const string EffectType = "EvaluatePromiseDeadlines";

    private readonly StartingOpportunityPromiseService _startingOpportunity;
    private readonly EventEffectIdempotencyGate _gate;
    private readonly PromiseBrokenDecisionTrigger? _promiseBroken;

    public PromiseDeadlineDayBoundaryApplier(
        StartingOpportunityPromiseService startingOpportunity,
        EventEffectIdempotencyGate gate,
        PromiseBrokenDecisionTrigger? promiseBroken = null)
    {
        _startingOpportunity = startingOpportunity
            ?? throw new ArgumentNullException(nameof(startingOpportunity));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _promiseBroken = promiseBroken;
    }

    public PromiseDeadlineDayBoundaryOutcome ApplyFromReactions(IReadOnlyList<ReactionIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(intents);

        var resolved = 0;
        var crises = 0;
        var applied = false;

        foreach (var intent in intents
                     .Where(i => string.Equals(
                         i.IntentTypeCode,
                         ObserveGameDayStartedReactionRule.IntentTypeCode,
                         StringComparison.Ordinal))
                     .OrderBy(i => i.OccurredAtDayNumber)
                     .ThenBy(i => i.SourceEventId))
        {
            var key = EventEffectProcessingKey.ForConsumerEffect(
                ConsumerId,
                intent.SourceEventId,
                EffectType);
            if (_gate.TryApply(key) == EventEffectApplicationStatus.Duplicate)
            {
                continue;
            }

            var day = GameDate.FromDayNumber(intent.OccurredAtDayNumber);
            resolved += _startingOpportunity.EvaluateDeadlines(
                day,
                promise =>
                {
                    if (_promiseBroken?.TryOpenAfterBroken(promise, day) is not null)
                    {
                        crises++;
                    }
                });
            applied = true;
        }

        return applied
            ? new PromiseDeadlineDayBoundaryOutcome(resolved, crises)
            : new PromiseDeadlineDayBoundaryOutcome(0, 0);
    }
}

public sealed record PromiseDeadlineDayBoundaryOutcome(
    int ResolvedCount,
    int CrisisOpenedCount);
