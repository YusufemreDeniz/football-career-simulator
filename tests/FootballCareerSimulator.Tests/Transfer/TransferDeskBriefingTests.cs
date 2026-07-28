using FootballCareerSimulator.Application.Transfer.Queries;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class TransferDeskBriefingTests
{
    [Fact]
    public void ClosedWindow_WithSaleCandidate_PointsToOpenWindow()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: false,
            windowStatusName: "Kapalı",
            windowClosesOnDayNumber: null,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: 2_000_000,
            budgetSpent: 500_000,
            squadFull: true,
            saleCandidatePlayerId: 1024);

        Assert.True(desk.IsEmployed);
        Assert.True(desk.DemandsAttention);
        Assert.Equal(TransferDeskBriefing.Brand, desk.BrandTitle);
        Assert.Contains("Pencere kapalı", desk.Headline, StringComparison.Ordinal);
        Assert.Contains("Pencere Aç", desk.AdviceLine, StringComparison.Ordinal);
        Assert.Contains(desk.BeatLines, b => b.Contains("#1024", StringComparison.Ordinal));
        Assert.Contains("Öneri:", desk.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void OpenWindow_FullSquad_PointsToSale()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            windowStatusName: "Açık",
            windowClosesOnDayNumber: 40,
            openNeedCount: 1,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: 1_000_000,
            budgetSpent: 0,
            squadFull: true,
            saleCandidatePlayerId: 2001);

        Assert.Contains("Kadro dolu", desk.Headline, StringComparison.Ordinal);
        Assert.Contains("Satışa Çıkar", desk.AdviceLine, StringComparison.Ordinal);
        Assert.True(desk.DemandsAttention);
        Assert.Contains(desk.BeatLines, b => b.Contains("kapanış gün 40", StringComparison.Ordinal));
    }

    [Fact]
    public void ActiveProcess_BeatsCalmAdvice()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            50,
            openNeedCount: 2,
            openExitNeedCount: 0,
            listedTargetCount: 1,
            activeProcessCount: 1,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: 10);

        Assert.Contains("Aktif süreç", desk.Headline, StringComparison.Ordinal);
        Assert.Contains("onay", desk.AdviceLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unemployed_ClosesDesk()
    {
        var desk = TransferDeskBriefing.Unemployed();
        Assert.False(desk.IsEmployed);
        Assert.Contains("kapalı", desk.Headline, StringComparison.OrdinalIgnoreCase);
    }
}
