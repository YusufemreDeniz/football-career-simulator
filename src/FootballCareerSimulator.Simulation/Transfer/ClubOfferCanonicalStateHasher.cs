using System.Text;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Simulation.Transfer;

public static class ClubOfferCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<ClubOffer> offers)
    {
        ArgumentNullException.ThrowIfNull(offers);

        var builder = new StringBuilder("ClubOffers=");
        foreach (var offer in offers.OrderBy(o => o.OfferId.Value))
        {
            builder.Append("O=").Append(offer.OfferId.Value)
                .Append(";P=").Append(offer.ProcessId.Value)
                .Append(";R=").Append(offer.Round)
                .Append(";F=").Append(offer.OfferedFee)
                .Append(";S=").Append((int)offer.Status)
                .Append(";D=").Append(offer.SubmittedOn.DayNumber)
                .Append('|');
        }

        return builder.ToString();
    }
}
