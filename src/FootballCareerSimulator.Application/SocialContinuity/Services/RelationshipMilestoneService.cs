using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Sınırlı Relationship Milestone: Trust band geçişi → Relationship Memory (idempotent).
/// </summary>
public sealed class RelationshipMilestoneService
{
    private readonly IMemoryStore _store;

    public RelationshipMilestoneService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int EvaluateTrustBandChange(
        RelationshipRecord previous,
        RelationshipRecord next,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(next);

        if (previous.RelationshipId != next.RelationshipId)
        {
            throw new SocialContinuityInvariantViolationException(
                "Trust-band milestone requires the same relationship identity.");
        }

        var fromBand = RelationshipDimensionBands.FromValue(previous.Trust);
        var toBand = RelationshipDimensionBands.FromValue(next.Trust);
        if (fromBand == toBand)
        {
            return 0;
        }

        var sourceKey = MemoryRecord.BuildRelationshipTrustBandSourceKey(
            next.RelationshipId,
            fromBand,
            toBand);
        var remembering = next.Observer;
        if (_store.Memories.Any(m =>
                m.SourceEventKey == sourceKey
                && m.RememberingActor == remembering
                && m.RuleId == MemoryRecord.RelationshipTrustBandRuleId
                && m.RuleVersion == MemoryRecord.RelationshipTrustBandRuleVersion))
        {
            return 0;
        }

        var nextId = _store.Memories.Count == 0
            ? 1L
            : _store.Memories.Max(m => m.MemoryId.Value) + 1;

        _store.Upsert(MemoryRecord.CreateRelationshipTrustBandMilestone(
            new MemoryId(nextId),
            next,
            fromBand,
            toBand,
            day));
        return 1;
    }
}
