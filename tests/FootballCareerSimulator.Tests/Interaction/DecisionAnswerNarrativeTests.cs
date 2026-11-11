using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DecisionAnswerNarrativeTests
{
    [Fact]
    public void GrantPlayingTime_LeadsWithPromiseHeadline()
    {
        var narrative = DecisionAnswerNarrative.Compose(
            kindName: "Forma süresi talebi",
            optionCode: DecisionRequest.OptionGrantPlayingTimePromise,
            optionDisplayText: "Forma süresi sözü ver",
            subjectPlayerId: 12,
            wasHardBlocker: false,
            remainingOpenCount: 0,
            subjectPlayerName: "Ali Veli");

        Assert.Equal("Masada", narrative.BrandTitle);
        Assert.Equal("Söz verdin — forma süresi hesabı başladı.", narrative.Headline);
        Assert.Equal("Seçimin: Forma süresi sözü ver", narrative.ChoiceLine);
        Assert.Contains(narrative.BeatLines, b => b.Contains("Konu: Ali Veli", StringComparison.Ordinal));
        Assert.DoesNotContain(narrative.BeatLines, b => b.Contains("oyuncu#", StringComparison.Ordinal));
        Assert.Contains(narrative.BeatLines, b => b.Contains("hafızasına", StringComparison.Ordinal));
        Assert.Contains(narrative.BeatLines, b => b.Contains("bekleyen kalmadı", StringComparison.Ordinal));
    }

    [Fact]
    public void HardBlockerCleared_AddsTimeBeat()
    {
        var narrative = DecisionAnswerNarrative.Compose(
            "Kritik basın sorusu",
            DecisionRequest.OptionPubliclyDefend,
            "Oyuncuyu kamuya savun",
            subjectPlayerId: 3,
            wasHardBlocker: true,
            remainingOpenCount: 2);

        Assert.Equal("Kamuya savundun — basın bunu duyacak.", narrative.Headline);
        Assert.Contains(narrative.BeatLines, b => b.Contains("Zorunlu engel kalktı", StringComparison.Ordinal));
        Assert.Contains(narrative.BeatLines, b => b.Contains("2 dosya", StringComparison.Ordinal));

        var text = narrative.ToStatusMessage();
        Assert.Contains("Masada", text, StringComparison.Ordinal);
        Assert.Contains("Seçimin:", text, StringComparison.Ordinal);
        Assert.Contains("· ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Refuse_UsesTensionHeadline()
    {
        var narrative = DecisionAnswerNarrative.Compose(
            "İlk 11 fırsatı talebi",
            DecisionRequest.OptionRefuse,
            "Talebi reddet",
            subjectPlayerId: 8,
            wasHardBlocker: false,
            remainingOpenCount: 1);

        Assert.Equal("Reddettin — gerilim soğumadı.", narrative.Headline);
        Assert.Contains(narrative.BeatLines, b => b.Contains("hafızaya", StringComparison.Ordinal));
        Assert.Contains(narrative.BeatLines, b => b.Contains("1 dosya", StringComparison.Ordinal));
    }

    [Fact]
    public void BoardDemand_HidesSentinelPlayer()
    {
        var narrative = DecisionAnswerNarrative.Compose(
            "Yönetim talebi",
            DecisionRequest.OptionCounterBoardDemand,
            "Karşı teklif sun",
            subjectPlayerId: 9_000_000_001L,
            wasHardBlocker: false,
            remainingOpenCount: 0);

        Assert.Equal("Masada pazarlık açıldı.", narrative.Headline);
        Assert.DoesNotContain(narrative.BeatLines, b => b.Contains("oyuncu#", StringComparison.Ordinal));
    }

    [Fact]
    public void GrantPlayingTime_WithSittingOutCausality_PointsToXi()
    {
        var narrative = DecisionAnswerNarrative.Compose(
            kindName: "Forma süresi talebi",
            optionCode: DecisionRequest.OptionGrantPlayingTimePromise,
            optionDisplayText: "Forma süresi sözü ver",
            subjectPlayerId: 21,
            wasHardBlocker: true,
            remainingOpenCount: 0,
            nextActionHint: "Sıradaki: Hazırlık / Maç Günü — #21 için XI veya yedek tut.",
            causalityLine: "Son 3 maçta yedek/kadro dışı — forma istiyor");

        Assert.Equal("Yedek birikimine söz verdin — forma hesabı başladı.", narrative.Headline);
        Assert.Contains(narrative.BeatLines, b => b.StartsWith("Neden:", StringComparison.Ordinal));
        Assert.Contains(narrative.BeatLines, b => b.Contains("XI veya yedek", StringComparison.Ordinal));
        Assert.Contains(narrative.BeatLines, b => b.Contains("Sıradaki:", StringComparison.Ordinal));
    }

    [Fact]
    public void RefusePlayingTime_WithSittingOutCausality_KeepsBenchTension()
    {
        var narrative = DecisionAnswerNarrative.Compose(
            "Forma süresi talebi",
            DecisionRequest.OptionRefuse,
            "Talebi reddet",
            subjectPlayerId: 21,
            wasHardBlocker: true,
            remainingOpenCount: 0,
            nextActionHint: "Oyuncu yönetimi — yedek kalma hafızası ve güven satırına bak.",
            causalityLine: "Son 3 maçta yedek/kadro dışı — forma istiyor");

        Assert.Equal("Yedek kalma talebini reddettin — gerilim büyüdü.", narrative.Headline);
        Assert.Contains(
            narrative.BeatLines,
            b => b.Contains("yedek kalma hafızası", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AcknowledgeTransfer_PointsToSellOnTransferDesk()
    {
        var narrative = DecisionAnswerNarrative.Compose(
            "Transfer isteği",
            DecisionRequest.OptionAcknowledgeTransferRequest,
            "Transfer isteğini kabul et",
            subjectPlayerId: 501,
            wasHardBlocker: true,
            remainingOpenCount: 0,
            nextActionHint: "Sıradaki: Transfer → Satışa Çıkar — Kaan Demir",
            subjectPlayerName: "Kaan Demir");

        Assert.Equal("Ayrılma isteğini kabul ettin — satış masası ısındı.", narrative.Headline);
        Assert.Contains(
            narrative.BeatLines,
            b => b.Contains("Satışa Çıkar: Kaan Demir", StringComparison.Ordinal));
        Assert.Contains(
            narrative.BeatLines,
            b => b.Contains("Sıradaki:", StringComparison.Ordinal));
    }

    [Fact]
    public void RefuseTransfer_KeepsBrokenPromiseTensionBeat()
    {
        var narrative = DecisionAnswerNarrative.Compose(
            "Transfer isteği",
            DecisionRequest.OptionRefuse,
            "Talebi reddet",
            subjectPlayerId: 88,
            wasHardBlocker: true,
            remainingOpenCount: 0);

        Assert.Equal("Ayrılmayı reddettin — kırgınlık masada kaldı.", narrative.Headline);
        Assert.Contains(
            narrative.BeatLines,
            b => b.Contains("söz kırığı unutulmadı", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IssueWarning_WithRedCardCausality_NamesTheCard()
    {
        var narrative = DecisionAnswerNarrative.Compose(
            "Disiplin görüşmesi",
            DecisionRequest.OptionIssueWarning,
            "Uyarı ver",
            subjectPlayerId: 12,
            wasHardBlocker: true,
            remainingOpenCount: 0,
            causalityLine: "Kırmızı kart gördü — soyunma odasında konuşma şart",
            subjectPlayerName: "Tolga Kurt");

        Assert.Equal("Kırmızı kart için uyarı yazıldı.", narrative.Headline);
        Assert.Contains(narrative.BeatLines, b => b.StartsWith("Neden:", StringComparison.Ordinal));
        Assert.Contains(
            narrative.BeatLines,
            b => b.Contains("kırmızı kart uyarısını", StringComparison.OrdinalIgnoreCase));
    }
}
