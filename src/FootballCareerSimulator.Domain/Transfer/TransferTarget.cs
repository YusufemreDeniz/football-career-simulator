using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// Belirli bir Transfer Need için aday. Process / Offer / Approval açmaz.
/// </summary>
public sealed class TransferTarget
{
    private TransferTarget(
        TransferTargetId targetId,
        TransferNeedId needId,
        ClubId clubId,
        PlayerId playerId,
        ShortlistEntryId? shortlistEntryId,
        TransferTargetStatus status,
        GameDate listedOn,
        GameDate? droppedOn)
    {
        TargetId = targetId;
        NeedId = needId;
        ClubId = clubId;
        PlayerId = playerId;
        ShortlistEntryId = shortlistEntryId;
        Status = status;
        ListedOn = listedOn;
        DroppedOn = droppedOn;
    }

    public TransferTargetId TargetId { get; }

    public TransferNeedId NeedId { get; }

    public ClubId ClubId { get; }

    public PlayerId PlayerId { get; }

    public ShortlistEntryId? ShortlistEntryId { get; }

    public TransferTargetStatus Status { get; }

    public GameDate ListedOn { get; }

    public GameDate? DroppedOn { get; }

    public bool IsListed => Status == TransferTargetStatus.Listed;

    public static TransferTarget List(
        TransferTargetId targetId,
        TransferNeedId needId,
        ClubId clubId,
        PlayerId playerId,
        ShortlistEntryId? shortlistEntryId,
        GameDate day)
    {
        return new TransferTarget(
            targetId,
            needId,
            clubId,
            playerId,
            shortlistEntryId,
            TransferTargetStatus.Listed,
            day,
            droppedOn: null);
    }

    public static TransferTarget Rehydrate(
        TransferTargetId targetId,
        TransferNeedId needId,
        ClubId clubId,
        PlayerId playerId,
        ShortlistEntryId? shortlistEntryId,
        TransferTargetStatus status,
        GameDate listedOn,
        GameDate? droppedOn)
    {
        if (!Enum.IsDefined(status))
        {
            throw new TransferInvariantViolationException($"Unknown transfer target status: {status}.");
        }

        if (status == TransferTargetStatus.Dropped && droppedOn is null)
        {
            throw new TransferInvariantViolationException("Dropped transfer target requires DroppedOn.");
        }

        if (status == TransferTargetStatus.Listed && droppedOn is not null)
        {
            throw new TransferInvariantViolationException("Listed transfer target cannot have DroppedOn.");
        }

        return new TransferTarget(
            targetId,
            needId,
            clubId,
            playerId,
            shortlistEntryId,
            status,
            listedOn,
            droppedOn);
    }

    public TransferTarget Drop(GameDate day)
    {
        if (Status == TransferTargetStatus.Dropped)
        {
            return this;
        }

        return new TransferTarget(
            TargetId,
            NeedId,
            ClubId,
            PlayerId,
            ShortlistEntryId,
            TransferTargetStatus.Dropped,
            ListedOn,
            day);
    }
}
