using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// ManagerDismissed / ManagerHired → Career Memory (menajer; idempotent).
/// İlişki / diyalog / board aktör hafızası yok.
/// </summary>
public sealed class CareerMemoryService
{
    private readonly IMemoryStore _store;

    public CareerMemoryService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int RecordDismissal(
        ManagerId managerId,
        ClubId clubId,
        FixtureId causationFixtureId,
        GameDate day)
    {
        var remembering = new ActorRef(ActorKind.Manager, managerId.Value);
        var sourceKey = MemoryRecord.BuildManagerDismissedSourceKey(causationFixtureId, managerId);
        if (Exists(sourceKey, remembering, MemoryRecord.ManagerDismissedRuleId, MemoryRecord.ManagerDismissedRuleVersion))
        {
            return 0;
        }

        _store.Upsert(MemoryRecord.CreateManagerDismissed(
            NextId(),
            managerId,
            clubId,
            causationFixtureId,
            day));
        return 1;
    }

    public int RecordHiring(
        ManagerId managerId,
        ClubId clubId,
        JobOfferId offerId,
        GameDate day)
    {
        var remembering = new ActorRef(ActorKind.Manager, managerId.Value);
        var sourceKey = MemoryRecord.BuildManagerHiredSourceKey(offerId);
        if (Exists(sourceKey, remembering, MemoryRecord.ManagerHiredRuleId, MemoryRecord.ManagerHiredRuleVersion))
        {
            return 0;
        }

        _store.Upsert(MemoryRecord.CreateManagerHired(
            NextId(),
            managerId,
            clubId,
            offerId,
            day));
        return 1;
    }

    private bool Exists(string sourceKey, ActorRef remembering, string ruleId, int ruleVersion) =>
        _store.Memories.Any(m =>
            m.SourceEventKey == sourceKey
            && m.RememberingActor == remembering
            && m.RuleId == ruleId
            && m.RuleVersion == ruleVersion);

    private MemoryId NextId()
    {
        var next = _store.Memories.Count == 0
            ? 1L
            : _store.Memories.Max(m => m.MemoryId.Value) + 1;
        return new MemoryId(next);
    }
}
