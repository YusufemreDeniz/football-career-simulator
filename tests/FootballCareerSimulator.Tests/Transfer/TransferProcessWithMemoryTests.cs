using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Tests.Transfer;

/// <summary>
/// Entegrasyon testi: Geçmiş pazarlık hafızasının yeni transfer sürecindeki karşı teklif tutarını deterministik etkisi.
/// docs/20 §14.21 — TransferProcessWithMemoryTests
/// </summary>
public sealed class TransferProcessWithMemoryTests
{
    private static readonly ClubId SatıcıKulüp  = new(1);
    private static readonly ClubId RakipKulüp   = new(2);
    private const long MenajerId = 99;

    [Fact]
    public void GeçmişDüşükTeklifler_SatıcıKulübüSabırsızlaştırır()
    {
        var servis = new TransferNegotiationMemoryService();

        // Üç ardışık düşük teklif: piyasa değerinin %40'ı
        servis.RecordOfferObservation(SatıcıKulüp, MenajerId,
            offeredFee: 4_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: false);
        servis.RecordOfferObservation(SatıcıKulüp, MenajerId,
            offeredFee: 4_500_000, estimatedMarketValue: 10_000_000, isDeadlineDay: false);
        servis.RecordOfferObservation(SatıcıKulüp, MenajerId,
            offeredFee: 4_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: false);

        var digest = servis.BuildDigest(SatıcıKulüp, MenajerId);

        // Açılış teklif agresifliği yükselmeli
        Assert.True(digest.OpeningBidAggressionScore > 50,
            "Sürekli düşük teklif sonrası OpeningBidAggression eşiği geçmeli.");

        // "Düşük Açılış Teklifi Alışkanlığı" rozeti kazanılmış olmalı
        Assert.Contains("Düşük Açılış Teklifi Alışkanlığı", digest.StyleIndicators);
    }

    [Fact]
    public void RakipKulüp_SatıcıKulübünHafızasınıGöremez_EpistemikSınır()
    {
        var servis = new TransferNegotiationMemoryService();

        // Satıcı kulübüyle 3 düşük teklif geçmişi oluştur
        servis.RecordOfferObservation(SatıcıKulüp, MenajerId,
            offeredFee: 3_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: false);
        servis.RecordOfferObservation(SatıcıKulüp, MenajerId,
            offeredFee: 3_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: false);

        var satıcıDigest = servis.BuildDigest(SatıcıKulüp, MenajerId);
        var rakipDigest  = servis.BuildDigest(RakipKulüp, MenajerId);

        // Satıcı kulüp menajeri tanıyor
        Assert.Contains("Düşük Açılış Teklifi Alışkanlığı", satıcıDigest.StyleIndicators);

        // Rakip kulüp bu menajerle hiç pazarlık yapmadı → nötr (omniscience yasak!)
        Assert.Equal(NegotiationBehaviorDossier.DefaultNeutralScore, rakipDigest.OpeningBidAggressionScore);
        Assert.Empty(rakipDigest.StyleIndicators);
    }

    [Fact]
    public void SonGünPanikTeklifi_İkiKez_DeadlineDesperationRozeti_Kazanılır()
    {
        var servis = new TransferNegotiationMemoryService();

        // İki son gün panik teklifi: 50 + 12 + 12 = 74 → rozetŞartı karşılanır
        servis.RecordOfferObservation(SatıcıKulüp, MenajerId,
            offeredFee: 15_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: true);
        servis.RecordOfferObservation(SatıcıKulüp, MenajerId,
            offeredFee: 14_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: true);

        var digest = servis.BuildDigest(SatıcıKulüp, MenajerId);

        Assert.Equal(74, digest.DeadlineDesperationScore); // 50 + 12 + 12
        Assert.Contains("Son Gün Panik Teklifçisi", digest.StyleIndicators);
    }

    [Fact]
    public void MasadanÇekilme_VeYenideGelme_HerIkiRozet_BağımsızÇalışır()
    {
        var servis = new TransferNegotiationMemoryService();

        // 2 kez masadan çekildi, 2 kez aynı oyuncuya tekrar geri döndü
        servis.RecordWalkaway(SatıcıKulüp, MenajerId);
        servis.RecordWalkaway(SatıcıKulüp, MenajerId);
        servis.RecordRepeatedInquiry(SatıcıKulüp, MenajerId);
        servis.RecordRepeatedInquiry(SatıcıKulüp, MenajerId);

        var digest = servis.BuildDigest(SatıcıKulüp, MenajerId);

        // Her iki boyut rozetleri bağımsız kazanılmış olmalı
        Assert.Contains("Masayı Terk Etmeye Yatkın", digest.StyleIndicators);
        Assert.Contains("Hedefinde İnatçı", digest.StyleIndicators);
        Assert.True(digest.WalkawayTendencyScore > 50);
        Assert.True(digest.PersistenceIndexScore > 50);
    }

    [Fact]
    public void Skor_UstSınırı_Aşmaz()
    {
        // docs/20 §14 — Puan bant sınırı: MaxScore = 100
        var dossier = new NegotiationBehaviorDossier(
            SatıcıKulüp, MenajerId,
            openingBidAggression: 98,
            deadlineDesperation:  98,
            walkawayTendency:      5,
            persistenceIndex:      5);

        dossier.RecordBid(offeredFee: 1_000_000, estimatedMarketValue: 10_000_000, isDeadlineDay: true);

        // Clamp: 100'ü geçemez
        Assert.Equal(NegotiationBehaviorDossier.MaxScore, dossier.OpeningBidAggression);
    }
}
