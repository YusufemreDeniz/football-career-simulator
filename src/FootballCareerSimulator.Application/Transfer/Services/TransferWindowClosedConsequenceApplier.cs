using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// TransferWindowClosedObserved reaction → owner TransferWindowCloseService (expire/carry).
/// </summary>
public sealed class TransferWindowClosedConsequenceApplier : ITransferWindowClosedConsequencePort
{
    public const string ConsumerId = "Transfer";
    public const string EffectType = "ApplyWindowClosed";

    private readonly TransferWindowCloseService _windowClose;
    private readonly EventEffectIdempotencyGate _gate;

    public TransferWindowClosedConsequenceApplier(
        TransferWindowCloseService windowClose,
        EventEffectIdempotencyGate gate)
    {
        _windowClose = windowClose ?? throw new ArgumentNullException(nameof(windowClose));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    public TransferWindowCloseOutcome ApplyWindowClosed(GameDate day) =>
        _windowClose.ApplyWindowClosed(day);

    public TransferWindowCloseOutcome ApplyFromReactions(IReadOnlyList<ReactionIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(intents);

        var expired = 0;
        var carried = 0;
        var applied = false;

        foreach (var intent in intents
                     .Where(i => string.Equals(
                         i.IntentTypeCode,
                         ObserveTransferWindowClosedReactionRule.IntentTypeCode,
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

            var outcome = _windowClose.ApplyWindowClosed(
                GameDate.FromDayNumber(intent.OccurredAtDayNumber));
            expired += outcome.ExpiredCount;
            carried += outcome.CarriedCount;
            applied = true;
        }

        return applied
            ? new TransferWindowCloseOutcome(expired, carried)
            : new TransferWindowCloseOutcome(0, 0);
    }
}
