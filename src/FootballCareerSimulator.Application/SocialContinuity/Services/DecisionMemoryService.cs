using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// DecisionRequest Answered/Expired → Relationship Memory (oyuncu → menajer; idempotent).
/// </summary>
public sealed class DecisionMemoryService
{
    private readonly IMemoryStore _store;

    public DecisionMemoryService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int RecordPlayingTimeOutcome(DecisionRequest request, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Kind != DecisionRequestKind.PlayingTimeRequest)
        {
            return 0;
        }

        if (request.Status is not (
            DecisionRequestStatus.Answered
            or DecisionRequestStatus.Expired))
        {
            return 0;
        }

        var outcomeKey = request.Status == DecisionRequestStatus.Expired
            ? "Expired"
            : request.SelectedOptionCode!;
        var sourceKey = MemoryRecord.BuildDecisionPlayingTimeOutcomeSourceKey(
            request.DecisionRequestId,
            outcomeKey);
        var remembering = new ActorRef(ActorKind.Player, request.SubjectPlayerId.Value);
        if (_store.Memories.Any(m =>
                m.SourceEventKey == sourceKey
                && m.RememberingActor == remembering
                && m.RuleId == MemoryRecord.DecisionPlayingTimeAnswerRuleId
                && m.RuleVersion == MemoryRecord.DecisionPlayingTimeAnswerRuleVersion))
        {
            return 0;
        }

        var nextId = _store.Memories.Count == 0
            ? 1L
            : _store.Memories.Max(m => m.MemoryId.Value) + 1;
        _store.Upsert(MemoryRecord.CreateDecisionPlayingTimeOutcome(
            new MemoryId(nextId),
            request,
            day));
        return 1;
    }
}
