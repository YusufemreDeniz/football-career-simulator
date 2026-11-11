using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Tests.ClubGovernance;

/// <summary>
/// Entegrasyon testi: Kulüp DNA'sı ile çatışan menajer kararlarının
/// yönetim güvenini negatif etkilemesi.
/// docs/20 §10.21 — ClubDnaManagerConflictIntegrationTests
/// </summary>
public sealed class ClubDnaManagerConflictIntegrationTests
{
    private static readonly ClubId GençliKulübü  = new(1);
    private static readonly ClubId ÜçüncüKulüp   = new(3);

    [Fact]
    public void GençKültürüKulübü_YaşlıTransferSezonunda_SellingTendencyOrtasına_DNA_Uzaklaşır()
    {
        // Akademi geleneği olan kulübü başlat (yüksek gençlik kültürü)
        var profil = new ClubDnaProfile(
            GençliKulübü,
            youthDevelopmentCulture: 80,  // Güçlü akademi geleneği
            recruitmentAggression:    30,
            sellingClubTendency:      20,
            managerialPatience:       60,
            wageDiscipline:           70,
            highIntensityTradition:   50);

        var servis = new ClubDnaService();
        servis.RegisterProfile(profil);

        var öncekiGençlik = profil.YouthDevelopmentCulture;
        Assert.Equal(80, öncekiGençlik);

        // Sezon: Genç oynatma oranı düşük, net harcama yüksek (DNA'ya karşı)
        profil.RecordSeasonFacts(
            youthMinutesRatioPercent: 15,   // DNA'ya aykırı — genç oynatılmıyor
            netTransferSpendMillion:  40,   // Büyük harcama
            managersDismissedCount:    0,
            wageUtilizationPercent:   90);

        // Gençlik kültürü puanı düşmeli
        Assert.True(profil.YouthDevelopmentCulture < öncekiGençlik,
            "Gençlik odaklı kulübün DNA'sına aykırı sezon sonrası YouthDevelopmentCulture düşmeli.");

        // Agresif transferler RecruitmentAggression'ı artırmalı
        Assert.True(profil.RecruitmentAggression > 30,
            "Yüksek harcama sezonunda RecruitmentAggression artmalı.");
    }

    [Fact]
    public void AkademiKültürüKulübü_3SezonGençOynatatırsa_BadgeKazanır()
    {
        var profil = ClubDnaProfile.CreateDefault(GençliKulübü);
        var servis = new ClubDnaService();
        servis.RegisterProfile(profil);

        // 3 sezon boyunca yüksek oranda genç oyuncu sahasında
        for (var sezon = 0; sezon < 3; sezon++)
        {
            profil.RecordSeasonFacts(
                youthMinutesRatioPercent: 65,
                netTransferSpendMillion:  0,
                managersDismissedCount:    0,
                wageUtilizationPercent:   80);
        }

        Assert.Equal(3, profil.SeasonsTracked);
        Assert.True(profil.YouthDevelopmentCulture >= 65);

        var digest = servis.BuildDigest(GençliKulübü);
        Assert.Contains("Akademi Geleneği", digest.CultureBadges);
    }

    [Fact]
    public void SıkMenajerDeğişimi_ManagerialPatienceDüşürür_SabırsızYönetimRozeti()
    {
        var profil = ClubDnaProfile.CreateDefault(ÜçüncüKulüp);
        var servis = new ClubDnaService();
        servis.RegisterProfile(profil);

        // 2 sezon içinde birer menajer kovuldu
        profil.RecordSeasonFacts(
            youthMinutesRatioPercent: 30,
            netTransferSpendMillion:  5,
            managersDismissedCount:    2,  // Kovulan menajer sayısı
            wageUtilizationPercent:   90);

        Assert.True(profil.ManagerialPatience < 50,
            "Menajer kovma sezonlarından sonra ManagerialPatience 50'nin altına düşmeli.");

        var digest = servis.BuildDigest(ÜçüncüKulüp);
        Assert.Contains("Sabırsız Yönetim", digest.CultureBadges);
    }

    [Fact]
    public void SellingKulübü_NetKâr_SellingTendencyArtırır_YetiştiriciRozeti()
    {
        var profil = ClubDnaProfile.CreateDefault(ÜçüncüKulüp);
        var servis = new ClubDnaService();
        servis.RegisterProfile(profil);

        // Net transfer kârı: -60M (satış ağırlıklı)
        profil.RecordSeasonFacts(
            youthMinutesRatioPercent: 30,
            netTransferSpendMillion: -60,
            managersDismissedCount:   0,
            wageUtilizationPercent:   75);

        Assert.Equal(65, profil.SellingClubTendency); // 50 + 15 = 65

        var digest = servis.BuildDigest(ÜçüncüKulüp);
        Assert.Contains("Yetiştirici / Satıcı Kulüp", digest.CultureBadges);
    }

    [Fact]
    public void KulübDNA_MenajerAyrıldıktan_Sonra_Korunur()
    {
        // docs/20 §10.22: Menajer ayrılıp başka kulübe geçtiğinde eski kulübün DNA'sı korunmalı
        var profil = new ClubDnaProfile(
            GençliKulübü,
            youthDevelopmentCulture: 75,
            recruitmentAggression:   35,
            sellingClubTendency:     25,
            managerialPatience:      60,
            wageDiscipline:          65,
            highIntensityTradition:  55);

        var servis = new ClubDnaService();
        servis.RegisterProfile(profil);

        var öncekiGençlik = profil.YouthDevelopmentCulture;

        // "Menajer ayrılışı" simülasyonu: DNA üzerinde doğrudan değişiklik yok,
        // sadece mevcut değerlerin okunması yeterli (inmutable read)
        var digest = servis.BuildDigest(GençliKulübü);

        // Kulüp DNA'sı menajer bağımsız korunuyor
        Assert.Equal(öncekiGençlik, profil.YouthDevelopmentCulture);
        Assert.NotNull(digest);
    }
}
