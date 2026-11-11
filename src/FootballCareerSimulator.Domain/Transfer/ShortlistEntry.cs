using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// İzleme kaydı. Transfer Process veya Approval taşımaz.
/// </summary>
public sealed class ShortlistEntry
{
    public const int MinPriority = 1;
    public const int MaxPriority = 5;

    private ShortlistEntry(
        ShortlistEntryId entryId,
        ClubId clubId,
        PlayerId playerId,
        TransferNeedId? needId,
        int priority,
        ShortlistEntryStatus status,
        GameDate addedOn,
        GameDate? archivedOn)
    {
        EntryId = entryId;
        ClubId = clubId;
        PlayerId = playerId;
        NeedId = needId;
        Priority = priority;
        Status = status;
        AddedOn = addedOn;
        ArchivedOn = archivedOn;
    }

    public ShortlistEntryId EntryId { get; }

    public ClubId ClubId { get; }

    public PlayerId PlayerId { get; }

    public TransferNeedId? NeedId { get; }

    public int Priority { get; }

    public ShortlistEntryStatus Status { get; }

    public GameDate AddedOn { get; }

    public GameDate? ArchivedOn { get; }

    public bool IsActive => Status == ShortlistEntryStatus.Active;

    public static ShortlistEntry Add(
        ShortlistEntryId entryId,
        ClubId clubId,
        PlayerId playerId,
        TransferNeedId? needId,
        int priority,
        GameDate day)
    {
        Validate(priority);
        return new ShortlistEntry(
            entryId,
            clubId,
            playerId,
            needId,
            priority,
            ShortlistEntryStatus.Active,
            day,
            archivedOn: null);
    }

    public static ShortlistEntry Rehydrate(
        ShortlistEntryId entryId,
        ClubId clubId,
        PlayerId playerId,
        TransferNeedId? needId,
        int priority,
        ShortlistEntryStatus status,
        GameDate addedOn,
        GameDate? archivedOn)
    {
        Validate(priority);
        if (!Enum.IsDefined(status))
        {
            throw new TransferInvariantViolationException($"Unknown shortlist status: {status}.");
        }

        if (status == ShortlistEntryStatus.Archived && archivedOn is null)
        {
            throw new TransferInvariantViolationException("Archived shortlist entry requires ArchivedOn.");
        }

        if (status == ShortlistEntryStatus.Active && archivedOn is not null)
        {
            throw new TransferInvariantViolationException("Active shortlist entry cannot have ArchivedOn.");
        }

        return new ShortlistEntry(
            entryId,
            clubId,
            playerId,
            needId,
            priority,
            status,
            addedOn,
            archivedOn);
    }

    public ShortlistEntry Archive(GameDate day)
    {
        if (Status == ShortlistEntryStatus.Archived)
        {
            return this;
        }

        return new ShortlistEntry(
            EntryId,
            ClubId,
            PlayerId,
            NeedId,
            Priority,
            ShortlistEntryStatus.Archived,
            AddedOn,
            day);
    }

    private static void Validate(int priority)
    {
        if (priority is < MinPriority or > MaxPriority)
        {
            throw new TransferInvariantViolationException(
                $"Priority must be between {MinPriority} and {MaxPriority}.");
        }
    }
}
