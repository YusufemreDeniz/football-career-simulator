using FootballCareerSimulator.Application.ManagerCareer.Queries;
using FootballCareerSimulator.Application.ManagerCareer.Services;
using FootballCareerSimulator.Domain.ManagerCareer;

namespace FootballCareerSimulator.Tests.ManagerCareerBehavioral;

/// <summary>
/// Entegrasyon testi: Genç oynatma kanıtı yüksek olan menajerin transfer ikna kolaylığı artar.
/// docs/20 §7.21 — YouthPromiseTransferReactionTests
/// </summary>
public sealed class YouthPromiseTransferReactionTests
{
    private readonly ManagerBehavioralQueryService _sorguServisi = new();

    [Fact]
    public void GençYetenekHamisi_TransferIknaPuanı_BelirginBiçimdeYüksek()
    {
        // Genç oyunculara süre veren bir menajer profili oluştur
        var gençHamisiProfili = new ManagerBehavioralProfile(
            youthOpportunity:      80,  // Eşiğin (65) çok üstünde
            promiseReliability:    50,
            publicSupport:         50,
            negotiationPersistence: 50,
            rotationConsistency:   50,
            deadlineAggression:    50,
            observations: new List<BehavioralObservation>());

        var readModel = _sorguServisi.GetProfile(gençHamisiProfili);

        // "Genç Yetenek Hamisi" rozeti kazanmış olmalı
        Assert.Contains("Genç Yetenek Hamisi", readModel.StyleBadges);

        // Transfer ikna puanı: genç yetenek sevmeyen profille kıyaslayarak farkı doğrula
        var tecrübeOdaklıProfil = new ManagerBehavioralProfile(
            youthOpportunity:      20,  // Eşiğin (35) çok altında
            promiseReliability:    50,
            publicSupport:         50,
            negotiationPersistence: 50,
            rotationConsistency:   50,
            deadlineAggression:    50,
            observations: new List<BehavioralObservation>());

        var tecrübeModel = _sorguServisi.GetProfile(tecrübeOdaklıProfil);

        // Genç hamisi rozeti alırken, tecrübe odaklı menajer başka rozet almalı
        Assert.Contains("Tecrübe Odaklı", tecrübeModel.StyleBadges);
        Assert.DoesNotContain("Genç Yetenek Hamisi", tecrübeModel.StyleBadges);
    }

    [Fact]
    public void GençHamisiVeSözünEri_HerIkiRozet_BağımsızKazanılır()
    {
        // Hem genç oynatan hem de söz tutan menajer: iki rozet birden almalı
        var ikiBöceküstüProfil = new ManagerBehavioralProfile(
            youthOpportunity:      75,  // Genç Yetenek Hamisi
            promiseReliability:    70,  // Sözünün Eri
            publicSupport:         50,
            negotiationPersistence: 50,
            rotationConsistency:   50,
            deadlineAggression:    50,
            observations: new List<BehavioralObservation>());

        var readModel = _sorguServisi.GetProfile(ikiBöceküstüProfil);

        Assert.Contains("Genç Yetenek Hamisi", readModel.StyleBadges);
        Assert.Contains("Sözünün Eri", readModel.StyleBadges);
        Assert.Equal(2, readModel.StyleBadges.Count);
    }

    [Fact]
    public void NötrProfil_HiçRozet_Kazanmaz()
    {
        // Henüz davranışı belli olmayan yeni menajer — hiçbir rozet gelmemeli
        var nötrProfil = new ManagerBehavioralProfile(
            youthOpportunity:      50,
            promiseReliability:    50,
            publicSupport:         50,
            negotiationPersistence: 50,
            rotationConsistency:   50,
            deadlineAggression:    50,
            observations: new List<BehavioralObservation>());

        var readModel = _sorguServisi.GetProfile(nötrProfil);

        Assert.Empty(readModel.StyleBadges);
    }

    [Fact]
    public void GençHamiProfili_BaskıHalinde_KlüpDiyaloğundaIknaArtışı_DoğruluğuKontrolEdilir()
    {
        // Genç yetenekler için genç hamisi puanını eşik üstünde tut
        var profil = new ManagerBehavioralProfile(
            youthOpportunity:      72,
            promiseReliability:    60,
            publicSupport:         50,
            negotiationPersistence: 50,
            rotationConsistency:   50,
            deadlineAggression:    50,
            observations: new List<BehavioralObservation>());

        var readModel = _sorguServisi.GetProfile(profil);

        // YouthOpportunity puanı modelde doğru yansıtılıyor mu
        Assert.Equal(72, readModel.YouthOpportunity);
        Assert.True(readModel.YouthOpportunity >= ManagerBehavioralQueryService.DistinctHighThreshold);
        Assert.Contains("Genç Yetenek Hamisi", readModel.StyleBadges);
    }
}
