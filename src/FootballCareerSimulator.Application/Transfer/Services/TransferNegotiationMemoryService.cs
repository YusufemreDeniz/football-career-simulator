using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Kulüplerin menajerler hakkındaki transfer müzakere hafızasını yöneten ve epistemik izolasyonu sağlayan servis (Yenilik 9).
/// </summary>
public sealed class TransferNegotiationMemoryService
{
    private readonly Dictionary<(ClubId ClubId, long ManagerId), NegotiationBehaviorDossier> _dossiers = new();

    public NegotiationBehaviorDossier GetOrCreateDossier(ClubId observingClubId, long managerId)
    {
        var key = (observingClubId, managerId);
        if (!_dossiers.TryGetValue(key, out var dossier))
        {
            dossier = NegotiationBehaviorDossier.CreateDefault(observingClubId, managerId);
            _dossiers[key] = dossier;
        }

        return dossier;
    }

    public void RecordOfferObservation(
        ClubId sellerClubId,
        long managerId,
        int offeredFee,
        int estimatedMarketValue,
        bool isDeadlineDay)
    {
        var dossier = GetOrCreateDossier(sellerClubId, managerId);
        dossier.RecordBid(offeredFee, estimatedMarketValue, isDeadlineDay);
    }

    public void RecordWalkaway(ClubId sellerClubId, long managerId)
    {
        var dossier = GetOrCreateDossier(sellerClubId, managerId);
        dossier.RecordWalkaway();
    }

    public void RecordRepeatedInquiry(ClubId sellerClubId, long managerId)
    {
        var dossier = GetOrCreateDossier(sellerClubId, managerId);
        dossier.RecordRepeatedInquiry();
    }

    public NegotiationIntelDigest BuildDigest(ClubId observingClubId, long managerId)
    {
        var dossier = GetOrCreateDossier(observingClubId, managerId);
        var indicators = new List<string>();

        if (dossier.OpeningBidAggression >= 65)
        {
            indicators.Add("Düşük Açılış Teklifi Alışkanlığı");
        }

        if (dossier.DeadlineDesperation >= 65)
        {
            indicators.Add("Son Gün Panik Teklifçisi");
        }

        if (dossier.WalkawayTendency >= 65)
        {
            indicators.Add("Masayı Terk Etmeye Yatkın");
        }

        if (dossier.PersistenceIndex >= 65)
        {
            indicators.Add("Hedefinde İnatçı");
        }

        var summary = indicators.Count > 0
            ? string.Join(", ", indicators)
            : "Standart / Henüz Bilinmeyen Müzakere Tarzı";

        return new NegotiationIntelDigest(
            observingClubId,
            managerId,
            summary,
            indicators,
            dossier.OpeningBidAggression,
            dossier.DeadlineDesperation,
            dossier.WalkawayTendency,
            dossier.PersistenceIndex);
    }
}
