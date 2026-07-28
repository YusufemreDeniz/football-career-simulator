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
            remainingOpenCount: 0);

        Assert.Equal("Masada", narrative.BrandTitle);
        Assert.Equal("Söz verdin — forma süresi hesabı başladı.", narrative.Headline);
        Assert.Equal("Seçimin: Forma süresi sözü ver", narrative.ChoiceLine);
        Assert.Contains(narrative.BeatLines, b => b.Contains("oyuncu#12", StringComparison.Ordinal));
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
}
