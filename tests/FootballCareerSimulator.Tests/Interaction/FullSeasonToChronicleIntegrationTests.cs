using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

/// <summary>
/// Entegrasyon testi: 1 sezonluk tam simülasyonun ardından kariyer belgeselinin
/// gerçek olaylarla birebir eşleştiğinin ve deterministik üretildiğinin doğrulanması.
/// docs/20 §15.21 — FullSeasonToChronicleIntegrationTests
/// </summary>
public sealed class FullSeasonToChronicleIntegrationTests
{
    // Sabit tarihler — determinizm testlerinde sabitleme zorunlu
    private static readonly GameDate SezonBaşı      = GameDate.FromCalendarDate(2026, 8, 10);
    private static readonly GameDate DerbiGünü       = GameDate.FromCalendarDate(2026, 11, 22);
    private static readonly GameDate KışKriziGünü    = GameDate.FromCalendarDate(2027, 1, 15);
    private static readonly GameDate ŞampiyonlukGünü = GameDate.FromCalendarDate(2027, 5, 18);

    [Fact]
    public void TamSezon_KritikOlaylar_BelgeseldeTutarlıYansır()
    {
        var derleyici = new CareerChronicleCompiler();

        // Derbi zaferi kaydedildi
        derleyici.RecordFact(2026, new NarrativeFactRecord(
            factId:              "DerbiZaferi_2026",
            milestoneType:       "DerbyWin",
            importanceScore:     90,
            involvedActorNames:  new[] { "Galatasaray", "Fenerbahçe" },
            occurredOn:          DerbiGünü,
            narrativeSnippet:    "Tarihi derbi zaferinde 3 gol bulundu."));

        // Kış transferi krizi kaydedildi
        derleyici.RecordFact(2026, new NarrativeFactRecord(
            factId:              "TransferKrizi_2027",
            milestoneType:       "TransferConflict",
            importanceScore:     65,
            involvedActorNames:  new[] { "Yabancı Kulüp", "Galatasaray" },
            occurredOn:          KışKriziGünü,
            narrativeSnippet:    "Kış döneminde kilit oyuncu transfer baskısına direnildi."));

        // Şampiyonluk anı kaydedildi (en yüksek önem puanı)
        derleyici.RecordFact(2026, new NarrativeFactRecord(
            factId:              "ŞampiyonlukAnı_2027",
            milestoneType:       "TitleWon",
            importanceScore:     100,
            involvedActorNames:  new[] { "Galatasaray" },
            occurredOn:          ŞampiyonlukGünü,
            narrativeSnippet:    "Son haftada mucizevi bir galibiyetle şampiyonluk mührü vuruldu."));

        // Act: Sezon belgeseli derle
        var belgesel = derleyici.CompileSeasonChronicle(
            seasonYear:          2026,
            clubName:            "Galatasaray",
            finalLeaguePosition: 1,
            wonTitle:            true);

        // Assert: Başlık doğru belirlenmeli
        Assert.Contains("Şampiyonluk Masalı", belgesel.SeasonTitle);
        Assert.True(belgesel.WonTitle);
        Assert.Equal(1, belgesel.FinalLeaguePosition);

        // Dönüm noktaları önem sırasına göre sıralanmalı (100 → 90 → 65)
        Assert.Equal(3, belgesel.KeyMilestones.Count);
        Assert.Equal("ŞampiyonlukAnı_2027", belgesel.KeyMilestones[0].FactId);
        Assert.Equal("DerbiZaferi_2026",    belgesel.KeyMilestones[1].FactId);

        // Anlatı metninde en önemli 3 dönüm noktası geçmeli
        Assert.Contains("mucizevi bir galibiyetle",    belgesel.DramaticArcNarrative);
        Assert.Contains("Tarihi derbi zaferinde",      belgesel.DramaticArcNarrative);
    }

    [Fact]
    public void BelgeseldeDeterminizm_AynıGirdiler_AynıMetin()
    {
        var derleyici1 = new CareerChronicleCompiler();
        var derleyici2 = new CareerChronicleCompiler();

        var ortakOlay = new NarrativeFactRecord(
            factId:             "EşsizOlay_1",
            milestoneType:      "YouthBreakthrough",
            importanceScore:    75,
            involvedActorNames: new[] { "Altyapı Oyuncusu" },
            occurredOn:         DerbiGünü,
            narrativeSnippet:   "Altyapıdan yetişen genç oyuncu sezonun sürprizine dönüştü.");

        derleyici1.RecordFact(2026, ortakOlay);
        derleyici2.RecordFact(2026, ortakOlay);

        var belgesel1 = derleyici1.CompileSeasonChronicle(2026, "Beşiktaş", 3, false);
        var belgesel2 = derleyici2.CompileSeasonChronicle(2026, "Beşiktaş", 3, false);

        // Aynı girdilerle tamamen aynı çıktı üretilmeli (determinizm)
        Assert.Equal(belgesel1.SeasonTitle,           belgesel2.SeasonTitle);
        Assert.Equal(belgesel1.DramaticArcNarrative,  belgesel2.DramaticArcNarrative);
        Assert.Equal(belgesel1.SeasonVerdict,          belgesel2.SeasonVerdict);
        Assert.Equal(belgesel1.KeyMilestones.Count,   belgesel2.KeyMilestones.Count);
    }

    [Fact]
    public void HiçOlayYaşanmamış_Sessiz_Sezon_AlternatifŞablonÜretilir()
    {
        // docs/20 §15.19: Sakin sezon → istikrar odaklı alternatif şablon
        var derleyici = new CareerChronicleCompiler();

        var belgesel = derleyici.CompileSeasonChronicle(
            seasonYear:          2027,
            clubName:            "Kasımpaşa",
            finalLeaguePosition: 10,
            wonTitle:            false);

        // Hiç milestone olmasa da anlatı tutarlı üretilmeli
        Assert.NotNull(belgesel.DramaticArcNarrative);
        Assert.NotEmpty(belgesel.DramaticArcNarrative);
        Assert.Contains("dengeli bir grafik çizildi", belgesel.DramaticArcNarrative);
        Assert.Contains("Mücadele Sezonu", belgesel.SeasonTitle);
    }

    [Fact]
    public void İkiSezon_BağımsızBelgesel_KarışmakzıÜretilir()
    {
        var derleyici = new CareerChronicleCompiler();

        // 1. sezon: Şampiyonluk
        derleyici.RecordFact(2026, new NarrativeFactRecord(
            "Ş1", "TitleWon", 100, new[] { "Galatasaray" },
            GameDate.FromCalendarDate(2027, 5, 18),
            "Şampiyonluk."));

        // 2. sezon: İkinci lig bitiş
        derleyici.RecordFact(2027, new NarrativeFactRecord(
            "A1", "CupFinal", 80, new[] { "Galatasaray" },
            GameDate.FromCalendarDate(2028, 5, 20),
            "Kupa finaline ulaşıldı."));

        var sezon1 = derleyici.CompileSeasonChronicle(2026, "Galatasaray", 1, wonTitle: true);
        var sezon2 = derleyici.CompileSeasonChronicle(2027, "Galatasaray", 2, wonTitle: false);

        // İki sezonun belgeselleri birbirinden bağımsız ve farklı içerikler taşımalı
        Assert.NotEqual(sezon1.SeasonTitle, sezon2.SeasonTitle);
        Assert.Single(sezon1.KeyMilestones);
        Assert.Single(sezon2.KeyMilestones);
        Assert.Equal("Ş1", sezon1.KeyMilestones[0].FactId);
        Assert.Equal("A1", sezon2.KeyMilestones[0].FactId);
    }
}
