using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Infrastructure;

public sealed class InMemoryClubOfferStore : IClubOfferStore
{
    private readonly Dictionary<long, ClubOffer> _byId = new();

    public IReadOnlyList<ClubOffer> Offers =>
        _byId.Values.OrderBy(o => o.OfferId.Value).ToArray();

    public ClubOffer? Get(ClubOfferId offerId) =>
        _byId.TryGetValue(offerId.Value, out var offer) ? offer : null;

    public IReadOnlyList<ClubOffer> GetForProcess(TransferProcessId processId) =>
        _byId.Values
            .Where(o => o.ProcessId.Value == processId.Value)
            .OrderBy(o => o.Round)
            .ThenBy(o => o.OfferId.Value)
            .ToArray();

    public void Upsert(ClubOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        _byId[offer.OfferId.Value] = offer;
    }

    public void ReplaceAll(IEnumerable<ClubOffer> offers)
    {
        ArgumentNullException.ThrowIfNull(offers);
        _byId.Clear();
        foreach (var offer in offers)
        {
            _byId[offer.OfferId.Value] = offer;
        }
    }
}
