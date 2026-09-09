using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

/// <summary>
/// Entegrasyon testi: Oyuncunun şampiyon taktiğine karşı sonraki sezon
/// rakiplerin antitez taktik seçme sıklığının arttığının doğrulanması.
/// docs/20 §9.21 — AiTacticalAdaptationIntegrationTests
/// </summary>
public sealed class AiTacticalAdaptationIntegrationTests
{
    private const string LigKodu = "TUR_S1";

    [Fact]
    public void Sezon1_4x33Hakimiyeti_Sezon2_KarşıTaktikÖnerir()
    {
        var servis = new TacticalEcosystemService();

        // Sezon 1: 4-3-3 hücum yaklaşımıyla 6 maç baskın (şampiyon taktik)
        for (var i = 0; i < 6; i++)
        {
            servis.RecordMatchTactics(
                LigKodu, 2026,
                Formation.F433, TacticalApproach.Attacking,
                Formation.F442, TacticalApproach.Balanced);
        }

        var sezon1Snapshot = servis.CompileSnapshot(LigKodu, 2026);

        // Sezon 1: 4-3-3 hâkim taktik olarak belirlenmeli
        Assert.Equal(Formation.F433, sezon1Snapshot.DominantFormation);
        Assert.Equal(TacticalApproach.Attacking, sezon1Snapshot.DominantApproach);

        // Sezon 2: Karşı taktik önerisi doğru belirlenmeli (antitez: 3-5-2 defans)
        var digest = servis.BuildDigest(LigKodu, 2026);
        Assert.Equal(Formation.F352, digest.RecommendedCounterFormation);
        Assert.Equal(TacticalApproach.Defensive, digest.RecommendedCounterApproach);

        // Bültenler 4-3-3'e karşı öneri içermeli
        Assert.NotEmpty(digest.TacticalBulletins);
        Assert.Contains(digest.TacticalBulletins, b => b.Contains("F352"));
    }

    [Fact]
    public void DengeliLig_HiçbirTaktiğin_AçıkÜstünlüğü_Olmadığında_Dengeli_MetaDöner()
    {
        // docs/20 §9.19: Dengeli ligde AI kendi geleneksel formasyonunda kalır
        var servis = new TacticalEcosystemService();

        // Her formasyondan eşit sayıda maç — denge durumu
        servis.RecordMatchTactics(LigKodu, 2027, Formation.F433, TacticalApproach.Balanced, Formation.F442, TacticalApproach.Balanced);
        servis.RecordMatchTactics(LigKodu, 2027, Formation.F442, TacticalApproach.Balanced, Formation.F352, TacticalApproach.Balanced);
        servis.RecordMatchTactics(LigKodu, 2027, Formation.F352, TacticalApproach.Balanced, Formation.F433, TacticalApproach.Balanced);

        var snapshot = servis.CompileSnapshot(LigKodu, 2027);

        // Dengeli meta: baskın yaklaşım Balanced olmalı
        Assert.Equal(TacticalApproach.Balanced, snapshot.DominantApproach);
    }

    [Fact]
    public void ÜstünTaktikDeğişti_MetaGüncellenir()
    {
        // Sezon 1: 4-3-3 hâkim (2 maç)
        var servis = new TacticalEcosystemService();
        servis.RecordMatchTactics(LigKodu, 2026, Formation.F433, TacticalApproach.Attacking, Formation.F442, TacticalApproach.Balanced);
        servis.RecordMatchTactics(LigKodu, 2026, Formation.F433, TacticalApproach.Attacking, Formation.F442, TacticalApproach.Balanced);

        var sezon1Snap = servis.CompileSnapshot(LigKodu, 2026);
        Assert.Equal(Formation.F433, sezon1Snap.DominantFormation);

        // Sezon 2 (yeni yıl): 3-5-2 baskın — 4 maçta hem ev hem deplasman F352 kullananlar baskın
        // Sezon2 gözlem: F352×8, F433×4 → F352 %67 share
        servis.RecordMatchTactics(LigKodu, 2027, Formation.F352, TacticalApproach.Defensive, Formation.F352, TacticalApproach.Defensive);
        servis.RecordMatchTactics(LigKodu, 2027, Formation.F352, TacticalApproach.Defensive, Formation.F352, TacticalApproach.Defensive);
        servis.RecordMatchTactics(LigKodu, 2027, Formation.F352, TacticalApproach.Defensive, Formation.F433, TacticalApproach.Attacking);
        servis.RecordMatchTactics(LigKodu, 2027, Formation.F352, TacticalApproach.Defensive, Formation.F433, TacticalApproach.Attacking);

        // Sezon 2 meta DEĞİŞMELİ — 3-5-2 hâkim (yıl 2027 bağımsız hesaplanır)
        var sezon2Snap = servis.CompileSnapshot(LigKodu, 2027);
        Assert.Equal(Formation.F352, sezon2Snap.DominantFormation);
    }

    [Fact]
    public void TaktikDigest_BültenMetni_FigüratifAdıçerir()
    {
        var servis = new TacticalEcosystemService();

        servis.RecordMatchTactics(LigKodu, 2026, Formation.F433, TacticalApproach.Attacking, Formation.F433, TacticalApproach.Attacking);

        var digest = servis.BuildDigest(LigKodu, 2026);

        // Digest meta adı anlamlı bir içerik taşımalı
        Assert.NotNull(digest.PrevailingMetaName);
        Assert.NotEmpty(digest.PrevailingMetaName);
        Assert.Contains("4-3-3", digest.PrevailingMetaName);
    }
}
