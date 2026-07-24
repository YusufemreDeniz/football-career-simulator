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

    public int RecordOutcome(DecisionRequest request, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Kind switch
        {
            DecisionRequestKind.PlayingTimeRequest => RecordPlayingTimeOutcome(request, day),
            DecisionRequestKind.StartingOpportunityRequest => RecordStartingOpportunityOutcome(request, day),
            _ => 0,
        };
    }

    public int RecordPlayingTimeOutcome(DecisionRequest request, GameDate day) =>
        RecordTypedOutcome(
            request,
            DecisionRequestKind.PlayingTimeRequest,
            MemoryRecord.DecisionPlayingTimeAnswerRuleId,
            MemoryRecord.DecisionPlayingTimeAnswerRuleVersion,
            MemoryRecord.BuildDecisionPlayingTimeOutcomeSourceKey,
            MemoryRecord.CreateDecisionPlayingTimeOutcome,
            day);

    public int RecordStartingOpportunityOutcome(DecisionRequest request, GameDate day) =>
        RecordTypedOutcome(
            request,
            DecisionRequestKind.StartingOpportunityRequest,
            MemoryRecord.DecisionStartingOpportunityAnswerRuleId,
            MemoryRecord.DecisionStartingOpportunityAnswerRuleVersion,
            MemoryRecord.BuildDecisionStartingOpportunityOutcomeSourceKey,
            MemoryRecord.CreateDecisionStartingOpportunityOutcome,
            day);

    private int RecordTypedOutcome(
        DecisionRequest request,
        DecisionRequestKind expectedKind,
        string ruleId,
        int ruleVersion,
        Func<DecisionRequestId, string, string> sourceKeyBuilder,
        Func<MemoryId, DecisionRequest, GameDate, MemoryRecord> factory,
        GameDate day)
    {
        if (request.Kind != expectedKind)
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
        var sourceKey = sourceKeyBuilder(request.DecisionRequestId, outcomeKey);
        var remembering = new ActorRef(ActorKind.Player, request.SubjectPlayerId.Value);
        if (_store.Memories.Any(m =>
                m.SourceEventKey == sourceKey
                && m.RememberingActor == remembering
                && m.RuleId == ruleId
                && m.RuleVersion == ruleVersion))
        {
            return 0;
        }

        var nextId = _store.Memories.Count == 0
            ? 1L
            : _store.Memories.Max(m => m.MemoryId.Value) + 1;
        _store.Upsert(factory(new MemoryId(nextId), request, day));
        return 1;
    }
}
