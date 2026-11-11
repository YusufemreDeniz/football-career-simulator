using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Ports;

public interface IClubOfferStore
{
    IReadOnlyList<ClubOffer> Offers { get; }

    ClubOffer? Get(ClubOfferId offerId);

    IReadOnlyList<ClubOffer> GetForProcess(TransferProcessId processId);

    void Upsert(ClubOffer offer);

    void ReplaceAll(IEnumerable<ClubOffer> offers);
}
