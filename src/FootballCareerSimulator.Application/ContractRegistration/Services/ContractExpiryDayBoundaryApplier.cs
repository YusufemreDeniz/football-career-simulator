using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Queries;
using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.ContractRegistration.Services;

/// <summary>
/// DayBoundaryObserved reaction → owner ContractRegistration.ExpireDueContracts.
/// </summary>
public sealed class ContractExpiryDayBoundaryApplier : IContractExpiryDayBoundaryPort
{
    public const string ConsumerId = "ContractRegistration";
    public const string EffectType = "ExpireDueContracts";

    private readonly ContractRegistrationService _registration;
    private readonly EventEffectIdempotencyGate _gate;

    public ContractExpiryDayBoundaryApplier(
        ContractRegistrationService registration,
        EventEffectIdempotencyGate gate)
    {
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    public FreeAgencyExpiryResult ExpireDueContracts(GameDate day) =>
        _registration.ExpireDueContracts(day);

    public FreeAgencyExpiryResult ApplyFromReactions(IReadOnlyList<ReactionIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(intents);

        var expired = 0;
        var clubs = new HashSet<long>();
        var freeAgents = new List<long>();
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

            var outcome = _registration.ExpireDueContracts(
                GameDate.FromDayNumber(intent.OccurredAtDayNumber));
            if (outcome.ExpiredCount > 0)
            {
                // Sözleşme bitişi sezon ortasında da olabilir. İnce bir kulübü
                // oynanabilir 18 kişilik tabanın altına düşürmeden serbest
                // oyuncu çeşitliliğini mümkün olduğunca koru.
                _registration.RestorePopulationContinuity(
                    GameDate.FromDayNumber(intent.OccurredAtDayNumber));
            }
            expired += outcome.ExpiredCount;
            foreach (var clubId in outcome.AffectedClubIds)
            {
                clubs.Add(clubId);
            }

            freeAgents.AddRange(outcome.FreeAgentPlayerIds.Where(playerId =>
                _registration.IsFreeAgent(
                    new FootballCareerSimulator.Domain.PlayerCareer.PlayerId(playerId))));
            applied = true;
        }

        return applied
            ? new FreeAgencyExpiryResult(
                expired,
                clubs.OrderBy(id => id).ToArray(),
                freeAgents.Distinct().OrderBy(id => id).ToArray())
            : new FreeAgencyExpiryResult(0, Array.Empty<long>(), Array.Empty<long>());
    }
}
