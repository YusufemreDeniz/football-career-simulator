using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.Transfer.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Club Offer iskeleti: teklif / karşı teklif / kabul / ret. Financial approval yok.
/// </summary>
public sealed class ClubOfferService
{
    private readonly IClubOfferStore _offerStore;
    private readonly ITransferProcessStore _processStore;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly ITransferWindowQuery _transferWindow;

    public ClubOfferService(
        IClubOfferStore offerStore,
        ITransferProcessStore processStore,
        IManagerCareerStore managerCareerStore,
        ITransferWindowQuery? transferWindow = null)
    {
        _offerStore = offerStore ?? throw new ArgumentNullException(nameof(offerStore));
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _transferWindow = transferWindow ?? AlwaysOpenTransferWindowQuery.Instance;
    }

    public ClubOffer SubmitClubOffer(TransferProcessId processId, int offeredFee, GameDate day)
    {
        EnsureTransferWindowOpen();
        var process = RequireProcess(processId);
        EnsureManagerOfBuyingClub(process.BuyingClubId);

        if (process.IsFreeAgent)
        {
            throw new TransferInvariantViolationException(
                "Free-agent process cannot submit a club offer.");
        }

        if (process.Status == TransferProcessStatus.SportingApproved)
        {
            process = process.EnterClubNegotiation();
            _processStore.Upsert(process);
        }
        else if (process.Status != TransferProcessStatus.ClubNegotiation)
        {
            throw new TransferInvariantViolationException(
                "Club offer requires sporting approval or an open club negotiation.");
        }

        if (_offerStore.GetForProcess(processId).Any(o => o.IsPending))
        {
            throw new TransferInvariantViolationException(
                "A pending club offer already exists; accept, reject, or counter it first.");
        }

        var round = _offerStore.GetForProcess(processId).Select(o => o.Round).DefaultIfEmpty(0).Max() + 1;
        var maxId = _offerStore.Offers.Select(o => o.OfferId.Value).DefaultIfEmpty(0).Max();
        var offer = ClubOffer.Submit(new ClubOfferId(maxId + 1), processId, round, offeredFee, day);
        _offerStore.Upsert(offer);
        return offer;
    }

    public ClubOffer AcceptPendingOffer(TransferProcessId processId)
    {
        var process = RequireProcess(processId);
        EnsureManagerOfBuyingClub(process.BuyingClubId);
        if (!process.IsInClubNegotiation)
        {
            throw new TransferInvariantViolationException("No active club negotiation to accept.");
        }

        var pending = RequirePending(processId);
        var accepted = pending.Accept();
        _offerStore.Upsert(accepted);
        _processStore.Upsert(process.ReachClubAgreement());
        return accepted;
    }

    public ClubOffer RejectPendingOffer(TransferProcessId processId)
    {
        var process = RequireProcess(processId);
        EnsureManagerOfBuyingClub(process.BuyingClubId);
        if (!process.IsInClubNegotiation)
        {
            throw new TransferInvariantViolationException("No active club negotiation to reject.");
        }

        var pending = RequirePending(processId);
        var rejected = pending.Reject();
        _offerStore.Upsert(rejected);
        return rejected;
    }

    public ClubOffer CounterPendingOffer(TransferProcessId processId, int offeredFee, GameDate day)
    {
        EnsureTransferWindowOpen();
        var process = RequireProcess(processId);
        EnsureManagerOfBuyingClub(process.BuyingClubId);
        if (!process.IsInClubNegotiation)
        {
            throw new TransferInvariantViolationException("No active club negotiation to counter.");
        }

        var pending = RequirePending(processId);
        _offerStore.Upsert(pending.Supersede());

        var round = pending.Round + 1;
        var maxId = _offerStore.Offers.Select(o => o.OfferId.Value).DefaultIfEmpty(0).Max();
        var counter = ClubOffer.Submit(new ClubOfferId(maxId + 1), processId, round, offeredFee, day);
        _offerStore.Upsert(counter);
        return counter;
    }

    private ClubOffer RequirePending(TransferProcessId processId) =>
        _offerStore.GetForProcess(processId).LastOrDefault(o => o.IsPending)
        ?? throw new TransferInvariantViolationException(
            $"No pending club offer for process #{processId.Value}.");

    private TransferProcess RequireProcess(TransferProcessId processId) =>
        _processStore.Get(processId)
        ?? throw new TransferInvariantViolationException($"Transfer process #{processId.Value} not found.");

    private void EnsureManagerOfBuyingClub(ClubId buyingClubId)
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId }
            || clubId.Value != buyingClubId.Value)
        {
            throw new TransferInvariantViolationException(
                "Only the employed manager of the buying club can manage club offers.");
        }
    }

    private void EnsureTransferWindowOpen()
    {
        if (!_transferWindow.IsOpen)
        {
            throw new TransferInvariantViolationException(
                "Transfer window is closed; cannot submit a club offer.");
        }
    }
}
