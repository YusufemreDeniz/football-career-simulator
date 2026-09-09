using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

/// <summary>
/// Entegrasyon testi: Kaptan güven kaybı → yakın takım arkadaşlarının güvenini dolaylı etkiler.
/// docs/20 §11.21 — CaptainFalloutContagionTests
/// </summary>
public sealed class CaptainFalloutContagionTests
{
    private static readonly ClubId KulübId  = new(1);
    private static readonly GameDate Gün1   = GameDate.FromCalendarDate(2026, 9, 1);
    private static readonly GameDate Gün20  = GameDate.FromCalendarDate(2026, 9, 20);

    [Fact]
    public void KaptanKriziYayılır_ViceCaptainGüveniEtkilenir()
    {
        // Soyunma odası sosyal bağlarını kur
        var servis = new SocialContagionService();
        const long KaptanId     = 10;
        const long ViceCaptainId = 11;

        // Kaptan → ViceCaptan (güçlü bağ, strength 100)
        servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, ViceCaptainId, "ViceCaptain", 100));

        // Kaptan sert kriz yaşadı (20 güven kaybı)
        var etkilenenler = servis.PropagateContagion(KaptanId, trustLoss: 20, Gün1, KulübId);

        // Depth=1: Yalnızca doğrudan bağlı ViceCaptan etkilenmeli
        Assert.Single(etkilenenler);
        Assert.Equal(ViceCaptainId, etkilenenler[0].TargetPlayerId);

        // İkincil güven kaybı: 20 × 1.0 × 0.35 = 7 puan
        Assert.Equal(7, etkilenenler[0].SecondaryTrustDelta);
    }

    [Fact]
    public void KaptanKrizi_30TrustKaybı_4Oyuncuya_SoyunmaOdasıBölünür()
    {
        var servis = new SocialContagionService();
        const long KaptanId = 10;

        // Kaptanın 4 yakın bağı var (maksimum sınır)
        servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, 11, "ViceCaptain", 90));
        servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, 12, "Mentor", 90));
        servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, 13, "AgeCohort", 90));
        servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, 14, "AgeCohort", 90));

        var öncesiDigest = servis.BuildDigest(KulübId);
        Assert.Equal("Kenetlenmiş", öncesiDigest.ClimateStateTurkish);

        // Şiddetli kriz: 30 güven kaybı, 4 takım arkadaşına yayılır
        servis.PropagateContagion(KaptanId, trustLoss: 30, Gün1, KulübId);

        var sonrasıDigest = servis.BuildDigest(KulübId);

        // Soyunma odası iklimi "Bölünmüş" duruma geçmeli
        Assert.Equal("Bölünmüş", sonrasıDigest.ClimateStateTurkish);

        // Gözlemler arasında uyarı mesajı olmalı
        Assert.Contains("Soyunma odasında klikleşme ve gerginlik gözleniyor.", sonrasıDigest.KeyObservations);

        // Sürtüşme skoru artmış olmalı
        Assert.True(sonrasıDigest.FrictionScore >= 44);
    }

    [Fact]
    public void KaptanKrizi_14GünCooldown_IkincietkinlikYayılmaz()
    {
        var servis = new SocialContagionService();
        const long KaptanId    = 20;
        const long ViceCaptain = 21;

        servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, ViceCaptain, "ViceCaptain", 80));

        // Gün 1: İlk kriz yayılır
        var ilkYayılım = servis.PropagateContagion(KaptanId, trustLoss: 20, Gün1, KulübId);
        Assert.Single(ilkYayılım);

        // Gün 10 (cooldown içinde): İkinci kriz yayılmamalı
        var gün10 = GameDate.FromCalendarDate(2026, 9, 10);
        var cooldownYayılım = servis.PropagateContagion(KaptanId, trustLoss: 20, gün10, KulübId);
        Assert.Empty(cooldownYayılım);

        // Gün 20 (cooldown bitti): Üçüncü kriz yeniden yayılır
        var sonYayılım = servis.PropagateContagion(KaptanId, trustLoss: 20, Gün20, KulübId);
        Assert.Single(sonYayılım);
    }

    [Fact]
    public void BeşinciSosyalBağ_InvariantViolation_Fırlatılır()
    {
        // docs/20 §11.17: Oyuncu başına maksimum 4 sparse bağ; 5. bağ reddedilmeli
        var servis = new SocialContagionService();
        const long KaptanId = 30;

        servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, 31, "ViceCaptain", 80));
        servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, 32, "Mentor", 70));
        servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, 33, "AgeCohort", 60));
        servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, 34, "AgeCohort", 60));

        Assert.Throws<SocialContinuityInvariantViolationException>(() =>
            servis.AddInfluenceLink(new SparseInfluenceLink(KaptanId, 35, "AgeCohort", 50)));
    }
}
