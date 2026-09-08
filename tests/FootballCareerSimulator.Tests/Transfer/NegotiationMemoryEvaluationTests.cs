using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class NegotiationMemoryEvaluationTests
{
    private static readonly ClubId ClubA = new(1);
    private static readonly ClubId ClubB = new(2);
    private const long ManagerId = 42;

    [Fact]
    public void NegotiationBehaviorDossier_InitialState_IsNeutral()
    {
        var dossier = NegotiationBehaviorDossier.CreateDefault(ClubA, ManagerId);
        var service = new TransferNegotiationMemoryService();

        Assert.Equal(NegotiationBehaviorDossier.DefaultNeutralScore, dossier.OpeningBidAggression);
        Assert.Equal(NegotiationBehaviorDossier.DefaultNeutralScore, dossier.DeadlineDesperation);
        Assert.Equal(NegotiationBehaviorDossier.DefaultNeutralScore, dossier.WalkawayTendency);
        Assert.Equal(NegotiationBehaviorDossier.DefaultNeutralScore, dossier.PersistenceIndex);
        Assert.Equal(0, dossier.TotalInteractionsCount);

        var digest = service.BuildDigest(ClubA, ManagerId);
        Assert.Equal("Standart / Henüz Bilinmeyen Müzakere Tarzı", digest.MarketReputationSummary);
        Assert.Empty(digest.StyleIndicators);
    }

    [Fact]
    public void NegotiationBehaviorDossier_LowballBids_RaiseOpeningBidAggressionAndAwardBadge()
    {
        var service = new TransferNegotiationMemoryService();

        // 2 lowball bids (offering 5M and 4M for a 10M valued player: 50% and 40%)
        service.RecordOfferObservation(ClubA, ManagerId, offeredFee: 5_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: false);
        service.RecordOfferObservation(ClubA, ManagerId, offeredFee: 4_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: false);

        var digest = service.BuildDigest(ClubA, ManagerId);

        Assert.Equal(66, digest.OpeningBidAggressionScore); // 50 + 8 + 8 = 66
        Assert.Contains("Düşük Açılış Teklifi Alışkanlığı", digest.StyleIndicators);
    }

    [Fact]
    public void NegotiationBehaviorDossier_DeadlineOverpay_RaisesDeadlineDesperation()
    {
        var service = new TransferNegotiationMemoryService();

        // Panic deadline offer (offering 15M for a 10M player on deadline day: 150%)
        service.RecordOfferObservation(ClubA, ManagerId, offeredFee: 15_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: true);
        service.RecordOfferObservation(ClubA, ManagerId, offeredFee: 14_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: true);

        var digest = service.BuildDigest(ClubA, ManagerId);

        Assert.Equal(74, digest.DeadlineDesperationScore); // 50 + 12 + 12 = 74
        Assert.Contains("Son Gün Panik Teklifçisi", digest.StyleIndicators);
    }

    [Fact]
    public void NegotiationBehaviorDossier_WalkawayAndPersistence_TrackCorrectly()
    {
        var service = new TransferNegotiationMemoryService();

        service.RecordWalkaway(ClubA, ManagerId);
        service.RecordWalkaway(ClubA, ManagerId);
        service.RecordRepeatedInquiry(ClubA, ManagerId);
        service.RecordRepeatedInquiry(ClubA, ManagerId);

        var digest = service.BuildDigest(ClubA, ManagerId);

        Assert.Equal(70, digest.WalkawayTendencyScore); // 50 + 20 = 70
        Assert.Equal(66, digest.PersistenceIndexScore); // 50 + 16 = 66
        Assert.Contains("Masayı Terk Etmeye Yatkın", digest.StyleIndicators);
        Assert.Contains("Hedefinde İnatçı", digest.StyleIndicators);
    }

    [Fact]
    public void TransferNegotiationMemoryService_EpistemicIsolation_ClubBCannotSeeClubADossier()
    {
        var service = new TransferNegotiationMemoryService();

        // Club A observes multiple lowball bids
        service.RecordOfferObservation(ClubA, ManagerId, offeredFee: 3_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: false);
        service.RecordOfferObservation(ClubA, ManagerId, offeredFee: 3_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: false);

        var clubADigest = service.BuildDigest(ClubA, ManagerId);
        var clubBDigest = service.BuildDigest(ClubB, ManagerId);

        // Club A knows the manager lowballs
        Assert.Equal(66, clubADigest.OpeningBidAggressionScore);
        Assert.Contains("Düşük Açılış Teklifi Alışkanlığı", clubADigest.StyleIndicators);

        // Club B has NEVER negotiated with the manager, has no omniscience!
        Assert.Equal(NegotiationBehaviorDossier.DefaultNeutralScore, clubBDigest.OpeningBidAggressionScore);
        Assert.Empty(clubBDigest.StyleIndicators);
        Assert.Equal("Standart / Henüz Bilinmeyen Müzakere Tarzı", clubBDigest.MarketReputationSummary);
    }

    [Fact]
    public void NegotiationBehaviorDossier_ScoreClamping_DoesNotExceedBounds()
    {
        var dossier = new NegotiationBehaviorDossier(
            ClubA,
            ManagerId,
            openingBidAggression: 95,
            deadlineDesperation: 95,
            walkawayTendency: 5,
            persistenceIndex: 5);

        dossier.RecordBid(offeredFee: 1_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: true);

        Assert.Equal(NegotiationBehaviorDossier.MaxScore, dossier.OpeningBidAggression);
    }
}
