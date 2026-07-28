using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// TransferWindowOpenedObserved reaction → owner AiClubTransferSimulationService.RunWindowTick.
/// </summary>
public sealed class TransferWindowOpenedConsequenceApplier
{
    public const string ConsumerId = "Transfer";
    public const string EffectType = "RunWindowTick";

    private readonly AiClubTransferSimulationService _aiSimulation;
    private readonly EventEffectIdempotencyGate _gate;

    public TransferWindowOpenedConsequenceApplier(
        AiClubTransferSimulationService aiSimulation,
        EventEffectIdempotencyGate gate)
    {
        _aiSimulation = aiSimulation ?? throw new ArgumentNullException(nameof(aiSimulation));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    public AiClubTransferTickOutcome ApplyFromReactions(
        IReadOnlyList<ReactionIntent> intents,
        int worldSeed)
    {
        ArgumentNullException.ThrowIfNull(intents);

        var completed = 0;
        var attempted = 0;
        var applied = false;

        foreach (var intent in intents
                     .Where(i => string.Equals(
                         i.IntentTypeCode,
                         ObserveTransferWindowOpenedReactionRule.IntentTypeCode,
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

            var outcome = _aiSimulation.RunWindowTick(
                GameDate.FromDayNumber(intent.OccurredAtDayNumber),
                worldSeed);
            completed += outcome.CompletedCount;
            attempted += outcome.AttemptedClubCount;
            applied = true;
        }

        return applied
            ? new AiClubTransferTickOutcome(completed, attempted)
            : new AiClubTransferTickOutcome(0, 0);
    }
}
