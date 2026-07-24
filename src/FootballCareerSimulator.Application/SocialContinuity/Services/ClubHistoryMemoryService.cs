using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Kulüp geçmişi hafızası: menajer ayrılış/dönüş + oyuncu transfer ayrılış/katılış (idempotent).
/// Eski kulüple karşılaşma ve ilişki mutasyonu yok.
/// </summary>
public sealed class ClubHistoryMemoryService
{
    private readonly IMemoryStore _store;

    public ClubHistoryMemoryService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int RecordManagerLeftDismissed(
        ManagerId managerId,
        ClubId clubId,
        FixtureId causationFixtureId,
        GameDate day)
    {
        var remembering = new ActorRef(ActorKind.Manager, managerId.Value);
        var sourceKey = MemoryRecord.BuildClubHistoryLeftDismissedSourceKey(causationFixtureId, managerId);
        if (Exists(
                sourceKey,
                remembering,
                MemoryRecord.ClubHistoryLeftDismissedRuleId,
                MemoryRecord.ClubHistoryLeftDismissedRuleVersion))
        {
            return 0;
        }

        _store.Upsert(MemoryRecord.CreateClubHistoryLeftDismissed(
            NextId(),
            managerId,
            clubId,
            causationFixtureId,
            day));
        return 1;
    }

    public int RecordManagerReturned(
        ManagerId managerId,
        ClubId clubId,
        JobOfferId offerId,
        GameDate day)
    {
        var remembering = new ActorRef(ActorKind.Manager, managerId.Value);
        var sourceKey = MemoryRecord.BuildClubHistoryReturnedSourceKey(offerId);
        if (Exists(
                sourceKey,
                remembering,
                MemoryRecord.ClubHistoryReturnedRuleId,
                MemoryRecord.ClubHistoryReturnedRuleVersion))
        {
            return 0;
        }

        _store.Upsert(MemoryRecord.CreateClubHistoryReturned(
            NextId(),
            managerId,
            clubId,
            offerId,
            day));
        return 1;
    }

    public int RecordPlayerTransferClubs(TransferProcess process, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(process);

        var created = 0;
        if (process.SellingClubId is { } selling)
        {
            created += TryCreatePlayerLeft(process.PlayerId, selling, process.ProcessId, day) ? 1 : 0;
        }

        created += TryCreatePlayerJoined(
            process.PlayerId,
            process.BuyingClubId,
            process.ProcessId,
            day,
            process.IsFreeAgent)
            ? 1
            : 0;
        return created;
    }

    private bool TryCreatePlayerLeft(
        PlayerId playerId,
        ClubId sellingClubId,
        TransferProcessId processId,
        GameDate day)
    {
        var remembering = new ActorRef(ActorKind.Player, playerId.Value);
        var sourceKey = MemoryRecord.BuildClubHistoryLeftTransferSourceKey(processId);
        if (Exists(
                sourceKey,
                remembering,
                MemoryRecord.ClubHistoryLeftTransferRuleId,
                MemoryRecord.ClubHistoryLeftTransferRuleVersion))
        {
            return false;
        }

        _store.Upsert(MemoryRecord.CreateClubHistoryLeftTransfer(
            NextId(),
            playerId,
            sellingClubId,
            processId,
            day));
        return true;
    }

    private bool TryCreatePlayerJoined(
        PlayerId playerId,
        ClubId buyingClubId,
        TransferProcessId processId,
        GameDate day,
        bool isFreeAgent)
    {
        var remembering = new ActorRef(ActorKind.Player, playerId.Value);
        var sourceKey = MemoryRecord.BuildClubHistoryJoinedTransferSourceKey(processId);
        if (Exists(
                sourceKey,
                remembering,
                MemoryRecord.ClubHistoryJoinedTransferRuleId,
                MemoryRecord.ClubHistoryJoinedTransferRuleVersion))
        {
            return false;
        }

        _store.Upsert(MemoryRecord.CreateClubHistoryJoinedTransfer(
            NextId(),
            playerId,
            buyingClubId,
            processId,
            day,
            isFreeAgent));
        return true;
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
