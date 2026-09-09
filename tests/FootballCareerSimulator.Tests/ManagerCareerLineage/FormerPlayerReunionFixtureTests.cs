using FootballCareerSimulator.Application.ManagerCareer.Services;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.ManagerCareerLineage;

/// <summary>
/// Entegrasyon testi: Eski oyuncusuyla rakip menajer olarak karşılaşan kullanıcının anlatı üretimi.
/// docs/20 §13.21 — FormerPlayerReunionFixtureTests
/// </summary>
public sealed class FormerPlayerReunionFixtureTests
{
    private static readonly ManagerId KullanıcıMenajer = new(1);
    private static readonly ManagerId EskiOyuncuMenajer = new(201);
    private static readonly ManagerId RakipMenajerId = new(99);
    private static readonly GameDate GeçişGünü = GameDate.FromCalendarDate(2028, 6, 15);

    [Fact]
    public void EskiKaptan_RakipMenajere_DönüştüğündeÖzelRozet_ÜretilirDigest()
    {
        var servis = new CoachingLineageService();

        // Eski kaptan emekli olarak menajer oldu, akıl hocası kullanıcı menajeriydi
        servis.TransitionPlayerToManager(
            newManagerId:       EskiOyuncuMenajer,
            originPlayerId:     1001,
            fullName:           "Emre Kaptan",
            mentorManagerId:    KullanıcıMenajer,
            mentorManagerName:  "Yusuf Deniz",
            favoredFormation:   Formation.F433,
            leadershipScore:    88,
            day:                GeçişGünü);

        var digest = servis.BuildDigest(EskiOyuncuMenajer, KullanıcıMenajer);

        // "Eski Öğrenciniz" rozeti üretilmeli
        Assert.True(digest.IsFormerPupilOfUser);
        Assert.Equal("Eski Öğrenciniz", digest.PedigreeBadge);
    }

    [Fact]
    public void EskiKaptan_KullanıcıylaKarşılaştığında_AnlatıÖzeti_IçerirMentorAdı()
    {
        var servis = new CoachingLineageService();

        servis.TransitionPlayerToManager(
            newManagerId:       EskiOyuncuMenajer,
            originPlayerId:     1002,
            fullName:           "Ahmet Yıldız",
            mentorManagerId:    KullanıcıMenajer,
            mentorManagerName:  "Kullanıcı Menajer",
            favoredFormation:   Formation.F352,
            leadershipScore:    82,
            day:                GeçişGünü);

        var digest = servis.BuildDigest(EskiOyuncuMenajer, KullanıcıMenajer);

        // Anlatı özeti kullanıcı menajerinin tedrisatına atıfta bulunmalı
        Assert.Contains("sizin tedrisatınızdan geçmiş", digest.NarrativeSummary);
        Assert.Contains("Ahmet Yıldız", digest.NarrativeSummary);
    }

    [Fact]
    public void BaşkaMenajereAtfedilmiş_EskiOyuncu_FarklıEkolRozeti_Alır()
    {
        var servis = new CoachingLineageService();
        var farklıMentor = new ManagerId(50);

        servis.TransitionPlayerToManager(
            newManagerId:       EskiOyuncuMenajer,
            originPlayerId:     1003,
            fullName:           "Mehmet Lider",
            mentorManagerId:    farklıMentor,
            mentorManagerName:  "Fatih Terim",
            favoredFormation:   Formation.F352,
            leadershipScore:    91,
            day:                GeçişGünü);

        var digest = servis.BuildDigest(EskiOyuncuMenajer, KullanıcıMenajer);

        // Başka mentordan gelen menajer: "... Ekolü" rozeti almalı
        Assert.False(digest.IsFormerPupilOfUser);
        Assert.Equal("Fatih Terim Ekolü", digest.PedigreeBadge);
        Assert.Contains("Fatih Terim'ın taktiksel mirasını", digest.NarrativeSummary);
    }

    [Fact]
    public void EmekliOyuncu_GeçerliZeminDeğerleri_Aktarılır()
    {
        var servis = new CoachingLineageService();

        var soykütük = servis.TransitionPlayerToManager(
            newManagerId:       EskiOyuncuMenajer,
            originPlayerId:     1004,
            fullName:           "Test Kaptan",
            mentorManagerId:    KullanıcıMenajer,
            mentorManagerName:  "Test Mentor",
            favoredFormation:   Formation.F442,
            leadershipScore:    75,
            day:                GeçişGünü);

        // Pedigree alanları eksiksiz aktarılmış olmalı
        Assert.Equal(EskiOyuncuMenajer, soykütük.ManagerId);
        Assert.Equal(1004, soykütük.OriginPlayerId);
        Assert.Equal("Test Kaptan", soykütük.FullName);
        Assert.Equal(KullanıcıMenajer, soykütük.MentoredByManagerId);
        Assert.Equal(Formation.F442, soykütük.FavoredFormation);
        Assert.Equal(75, soykütük.LeadershipReputation);
        Assert.True(soykütük.HasMentor);
    }

    [Fact]
    public void GeçersizOriginPlayerId_Sıfır_InvariantViolationFırlatır()
    {
        var servis = new CoachingLineageService();

        Assert.Throws<ManagerCareerInvariantViolationException>(() =>
            servis.TransitionPlayerToManager(
                newManagerId:      EskiOyuncuMenajer,
                originPlayerId:    0,  // Geçersiz — domain invariantını bozar
                fullName:          "Hatalı Kayıt",
                mentorManagerId:   KullanıcıMenajer,
                mentorManagerName: "Herhangi Biri",
                favoredFormation:  Formation.F442,
                leadershipScore:   50,
                day:               GeçişGünü));
    }
}
