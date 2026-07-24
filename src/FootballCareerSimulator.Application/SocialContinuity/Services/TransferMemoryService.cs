using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// TransferCompleted → Transfer Memory (oyuncu + ilgili menajer; idempotent).
/// İlişki / diyalog / satış talebi hafızası yok.
/// </summary>
public sealed class TransferMemoryService
{
    private readonly IMemoryStore _store;

    public TransferMemoryService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int RecordCompleted(
        TransferProcess process,
        GameDate day,
        ActorRef? involvedManager = null)
    {
        ArgumentNullException.ThrowIfNull(process);

        var created = 0;
        var player = new ActorRef(ActorKind.Player, process.PlayerId.Value);
        created += TryCreate(player, process, day) ? 1 : 0;

        if (involvedManager is { } manager
            && manager.Kind == ActorKind.Manager
            && TryCreate(manager, process, day))
        {
            created++;
        }

        return created;
    }

    private bool TryCreate(ActorRef rememberingActor, TransferProcess process, GameDate day)
    {
        var sourceKey = MemoryRecord.BuildTransferCompletedSourceKey(process.ProcessId);
        var exists = _store.Memories.Any(m =>
            m.SourceEventKey == sourceKey
            && m.RememberingActor == rememberingActor
            && m.RuleId == MemoryRecord.TransferCompletedRuleId
            && m.RuleVersion == MemoryRecord.TransferCompletedRuleVersion);

        if (exists)
        {
            return false;
        }

        var nextId = _store.Memories.Count == 0
            ? 1L
            : _store.Memories.Max(m => m.MemoryId.Value) + 1;

        _store.Upsert(MemoryRecord.CreateTransferCompleted(
            new MemoryId(nextId),
            rememberingActor,
            process.ProcessId,
            day,
            process.IsFreeAgent));
        return true;
    }
}
