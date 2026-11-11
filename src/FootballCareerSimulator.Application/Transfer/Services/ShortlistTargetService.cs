using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Shortlist + Transfer Target iskeleti. Process / Offer / Approval yok.
/// </summary>
public sealed class ShortlistTargetService
{
    private readonly IShortlistStore _shortlistStore;
    private readonly ITransferTargetStore _targetStore;
    private readonly ITransferNeedStore _needStore;

    public ShortlistTargetService(
        IShortlistStore shortlistStore,
        ITransferTargetStore targetStore,
        ITransferNeedStore needStore)
    {
        _shortlistStore = shortlistStore ?? throw new ArgumentNullException(nameof(shortlistStore));
        _targetStore = targetStore ?? throw new ArgumentNullException(nameof(targetStore));
        _needStore = needStore ?? throw new ArgumentNullException(nameof(needStore));
    }

    public ShortlistEntry AddToShortlist(
        ClubId clubId,
        PlayerId playerId,
        TransferNeedId? needId,
        int priority,
        GameDate day)
    {
        if (needId is TransferNeedId linkedNeed)
        {
            EnsureOpenNeedOwnedByClub(linkedNeed, clubId);
        }

        var existing = _shortlistStore.GetForClub(clubId).FirstOrDefault(e =>
            e.IsActive
            && e.PlayerId.Value == playerId.Value
            && NullableNeedEquals(e.NeedId, needId));
        if (existing is not null)
        {
            return existing;
        }

        var maxId = _shortlistStore.Entries.Select(e => e.EntryId.Value).DefaultIfEmpty(0).Max();
        var entry = ShortlistEntry.Add(
            new ShortlistEntryId(maxId + 1),
            clubId,
            playerId,
            needId,
            priority,
            day);
        _shortlistStore.Upsert(entry);
        return entry;
    }

    public ShortlistEntry ArchiveShortlistEntry(ShortlistEntryId entryId, GameDate day)
    {
        var existing = _shortlistStore.Get(entryId)
            ?? throw new TransferInvariantViolationException($"Shortlist entry #{entryId.Value} not found.");
        var archived = existing.Archive(day);
        _shortlistStore.Upsert(archived);
        return archived;
    }

    public TransferTarget AddTransferTarget(
        TransferNeedId needId,
        PlayerId playerId,
        ShortlistEntryId? shortlistEntryId,
        GameDate day)
    {
        var need = EnsureOpenNeed(needId);

        if (shortlistEntryId is ShortlistEntryId entryId)
        {
            var entry = _shortlistStore.Get(entryId)
                ?? throw new TransferInvariantViolationException(
                    $"Shortlist entry #{entryId.Value} not found.");
            if (!entry.IsActive)
            {
                throw new TransferInvariantViolationException("Shortlist entry is archived.");
            }

            if (entry.ClubId.Value != need.ClubId.Value)
            {
                throw new TransferInvariantViolationException(
                    "Shortlist entry club does not match transfer need club.");
            }

            if (entry.PlayerId.Value != playerId.Value)
            {
                throw new TransferInvariantViolationException(
                    "Shortlist entry player does not match target player.");
            }
        }

        var listed = _targetStore.GetForClub(need.ClubId).FirstOrDefault(t =>
            t.IsListed
            && t.NeedId.Value == needId.Value
            && t.PlayerId.Value == playerId.Value);
        if (listed is not null)
        {
            return listed;
        }

        var maxId = _targetStore.Targets.Select(t => t.TargetId.Value).DefaultIfEmpty(0).Max();
        var target = TransferTarget.List(
            new TransferTargetId(maxId + 1),
            needId,
            need.ClubId,
            playerId,
            shortlistEntryId,
            day);
        _targetStore.Upsert(target);
        return target;
    }

    public TransferTarget PromoteOldestActiveShortlist(ClubId clubId, GameDate day)
    {
        var entry = _shortlistStore.GetForClub(clubId)
            .Where(e => e.IsActive && e.NeedId is not null)
            .OrderBy(e => e.EntryId.Value)
            .FirstOrDefault()
            ?? throw new TransferInvariantViolationException(
                "No active shortlist entry linked to a transfer need.");

        return AddTransferTarget(entry.NeedId!.Value, entry.PlayerId, entry.EntryId, day);
    }

    public TransferTarget DropTransferTarget(TransferTargetId targetId, GameDate day)
    {
        var existing = _targetStore.Get(targetId)
            ?? throw new TransferInvariantViolationException($"Transfer target #{targetId.Value} not found.");
        var dropped = existing.Drop(day);
        _targetStore.Upsert(dropped);
        return dropped;
    }

    /// <summary>
    /// Deterministik iskelet önerisi: başka kulüpten sentetik oyuncu shortlist + hedef.
    /// </summary>
    public TransferTarget SuggestAndListTargetForOldestOpenNeed(ClubId clubId, GameDate day)
    {
        var need = _needStore.GetForClub(clubId).Where(n => n.IsOpen).OrderBy(n => n.NeedId.Value).FirstOrDefault()
            ?? throw new TransferInvariantViolationException("No open transfer need for club.");

        var sourceClubId = clubId.Value == 1 ? 2L : 1L;
        var playerId = PlayerId.FromClubSlot(sourceClubId, slotIndex: 0);
        var entry = AddToShortlist(clubId, playerId, need.NeedId, priority: 3, day);
        return AddTransferTarget(need.NeedId, playerId, entry.EntryId, day);
    }

    private TransferNeed EnsureOpenNeedOwnedByClub(TransferNeedId needId, ClubId clubId)
    {
        var need = EnsureOpenNeed(needId);
        if (need.ClubId.Value != clubId.Value)
        {
            throw new TransferInvariantViolationException(
                "Transfer need does not belong to the requesting club.");
        }

        return need;
    }

    private TransferNeed EnsureOpenNeed(TransferNeedId needId)
    {
        var need = _needStore.Get(needId)
            ?? throw new TransferInvariantViolationException($"Transfer need #{needId.Value} not found.");
        if (!need.IsOpen)
        {
            throw new TransferInvariantViolationException($"Transfer need #{needId.Value} is closed.");
        }

        return need;
    }

    private static bool NullableNeedEquals(TransferNeedId? left, TransferNeedId? right) =>
        left is null && right is null
        || left is TransferNeedId l && right is TransferNeedId r && l.Value == r.Value;
}
