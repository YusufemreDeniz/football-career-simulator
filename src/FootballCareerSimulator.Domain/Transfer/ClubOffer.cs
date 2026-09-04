using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// Kulüpler arası transfer ücreti teklifi (iskelet — finansal rezervasyon yok).
/// </summary>
public sealed class ClubOffer
{
    public const int MinFee = 1;
    public const int MaxFee = 100_000_000;

    private ClubOffer(
        ClubOfferId offerId,
        TransferProcessId processId,
        int round,
        int offeredFee,
        ClubOfferStatus status,
        GameDate submittedOn)
    {
        OfferId = offerId;
        ProcessId = processId;
        Round = round;
        OfferedFee = offeredFee;
        Status = status;
        SubmittedOn = submittedOn;
    }

    public ClubOfferId OfferId { get; }

    public TransferProcessId ProcessId { get; }

    public int Round { get; }

    public int OfferedFee { get; }

    public ClubOfferStatus Status { get; }

    public GameDate SubmittedOn { get; }

    public bool IsPending => Status == ClubOfferStatus.Pending;

    public static ClubOffer Submit(
        ClubOfferId offerId,
        TransferProcessId processId,
        int round,
        int offeredFee,
        GameDate day)
    {
        Validate(round, offeredFee);
        return new ClubOffer(
            offerId,
            processId,
            round,
            offeredFee,
            ClubOfferStatus.Pending,
            day);
    }

    public static ClubOffer Rehydrate(
        ClubOfferId offerId,
        TransferProcessId processId,
        int round,
        int offeredFee,
        ClubOfferStatus status,
        GameDate submittedOn)
    {
        Validate(round, offeredFee);
        if (!Enum.IsDefined(status))
        {
            throw new TransferInvariantViolationException($"Unknown club offer status: {status}.");
        }

        return new ClubOffer(offerId, processId, round, offeredFee, status, submittedOn);
    }

    public ClubOffer Accept() =>
        Status == ClubOfferStatus.Accepted
            ? this
            : EnsurePendingTransition(ClubOfferStatus.Accepted);

    public ClubOffer Reject() =>
        Status == ClubOfferStatus.Rejected
            ? this
            : EnsurePendingTransition(ClubOfferStatus.Rejected);

    public ClubOffer Supersede() =>
        Status == ClubOfferStatus.Superseded
            ? this
            : EnsurePendingTransition(ClubOfferStatus.Superseded);

    private ClubOffer EnsurePendingTransition(ClubOfferStatus next)
    {
        if (Status != ClubOfferStatus.Pending)
        {
            throw new TransferInvariantViolationException(
                $"Offer #{OfferId.Value} is {Status} and cannot become {next}.");
        }

        return new ClubOffer(OfferId, ProcessId, Round, OfferedFee, next, SubmittedOn);
    }

    private static void Validate(int round, int offeredFee)
    {
        if (round <= 0)
        {
            throw new TransferInvariantViolationException("Offer round must be positive.");
        }

        if (offeredFee is < MinFee or > MaxFee)
        {
            throw new TransferInvariantViolationException(
                $"Offered fee must be between {MinFee} and {MaxFee}.");
        }
    }
}
