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
        Assert.Equal(TransferNextStep.ReasonOpenWindow, desk.NextStep!.ReasonCode);
        Assert.Equal(TransferNextStep.ActionOpenTransferWindow, desk.NextStep.ActionCode);
        Assert.Contains(desk.BeatLines, b => b.Contains("kenar oyuncu", StringComparison.Ordinal));
        Assert.Contains("Öneri:", desk.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void OpenWindow_FullSquad_DoesNotForceNamedSale()
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
            saleCandidatePlayerId: 2001,
            currentDayNumber: 30);

        Assert.Contains("Kadro dolu", desk.Headline, StringComparison.Ordinal);
        Assert.Contains("Kadro", desk.Headline, StringComparison.Ordinal);
        Assert.Contains("Satışa Çıkar", desk.AdviceLine, StringComparison.Ordinal);
        Assert.False(desk.DemandsAttention);
        Assert.NotEqual(TransferNextStep.ReasonSellFringe, desk.NextStep?.ReasonCode);
        Assert.Equal(TransferNextStep.ReasonPickTarget, desk.NextStep!.ReasonCode);
        Assert.Contains(desk.BeatLines, b => b.Contains("kapanış", StringComparison.Ordinal));
        Assert.DoesNotContain(desk.ToDisplayText(), "kapanış gün", StringComparison.Ordinal);
    }

    [Fact]
    public void ClosingCritical_RaisesWindowPressureHeadline()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: 42,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: 1_000_000,
            budgetSpent: 0,
            squadFull: true,
            saleCandidatePlayerId: 88,
            currentDayNumber: 40);

        Assert.False(desk.DemandsAttention);
        Assert.Contains("kapanıyor", desk.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(desk.BeatLines, b => b.Contains("kritik", StringComparison.Ordinal));
        Assert.Equal(TransferNextStep.ActionScanNeeds, desk.NextStep!.ActionCode);
        Assert.DoesNotContain("Satışa Çıkar —", desk.NextStep.ButtonLabel, StringComparison.Ordinal);
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
        Assert.Equal(TransferNextStep.ReasonAdvanceProcess, desk.NextStep!.ReasonCode);
    }

    [Fact]
    public void Unemployed_ClosesDesk()
    {
        var desk = TransferDeskBriefing.Unemployed();
        Assert.False(desk.IsEmployed);
        Assert.Contains("kapalı", desk.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Null(desk.NextStep);
        Assert.Null(desk.WindowRhythmLine);
    }

    [Fact]
    public void OpenWindow_NoCloseInfo_CalmRhythmLine()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: null,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null);

        Assert.False(desk.DemandsAttention);
        Assert.Equal(
            "Pencere açık — transfer masası çalışıyor.",
            desk.WindowRhythmLine);
    }

    [Fact]
    public void OpenWindow_ClosingInTwoDays_CriticalRhythmLine()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: 42,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null,
            currentDayNumber: 40);

        Assert.Equal(
            "Pencere 2 gün içinde kapanıyor — masaya bak.",
            desk.WindowRhythmLine);
    }

    [Fact]
    public void OpenWindow_ClosingToday_LastDayRhythmLine()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: 40,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null,
            currentDayNumber: 40);

        Assert.Equal(
            "Pencere bugün kapanıyor — işi bitir.",
            desk.WindowRhythmLine);
    }

    [Fact]
    public void ClosedWindow_OneDayAgo_ClosingRhythmLine()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: false,
            "Kapalı",
            windowClosesOnDayNumber: 39,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null,
            currentDayNumber: 40);

        Assert.Equal(
            "Pencere kapandı — kadro bu haliyle ilerliyor.",
            desk.WindowRhythmLine);
    }

    [Fact]
    public void ClosedWindow_LongAgo_NoRhythmLine()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: false,
            "Kapalı",
            windowClosesOnDayNumber: 30,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null,
            currentDayNumber: 40);

        Assert.Null(desk.WindowRhythmLine);
    }

    [Fact]
    public void PromiseExitPressure_DemandsAttention_AndPointsToDesk()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: 60,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: 1_000_000,
            budgetSpent: 0,
            squadFull: false,
            saleCandidatePlayerId: null,
            currentDayNumber: 40,
            promiseExitPressurePlayerId: 77,
            promiseExitPressureHint: "Söz #2 bozuldu · güven düşük");

        Assert.True(desk.DemandsAttention);
        Assert.Contains("Söz kırılması", desk.Headline, StringComparison.Ordinal);
        Assert.Contains("kenar oyuncu", desk.Headline, StringComparison.Ordinal);
        Assert.Contains("Masada Cevapla", desk.AdviceLine, StringComparison.Ordinal);
        Assert.Equal(TransferNextStep.ReasonPromiseExit, desk.NextStep!.ReasonCode);
        Assert.Equal(TransferNextStep.TargetToday, desk.NextStep.TargetPageCode);
        Assert.Equal("Masada Cevapla", desk.NextStep.ButtonLabel);
        Assert.Contains(desk.BeatLines, b => b.Contains("Söz baskısı", StringComparison.Ordinal));
        Assert.Contains(desk.BeatLines, b => b.Contains("güven düşük", StringComparison.Ordinal));
    }

    [Fact]
    public void PromiseExitPressure_YieldsToPendingOffers()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            60,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 1,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null,
            currentDayNumber: 40,
            promiseExitPressurePlayerId: 77,
            promiseExitPressureHint: "Söz #2 bozuldu");

        Assert.Equal(TransferNextStep.ReasonAnswerOffers, desk.NextStep!.ReasonCode);
        Assert.Contains(desk.BeatLines, b => b.Contains("Söz baskısı", StringComparison.Ordinal));
    }

    [Fact]
    public void IdleOpenWindow_ScanNeeds_SoftGuideWithoutAttentionPressure()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: null,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null);

        Assert.Equal(TransferNextStep.ReasonScanNeeds, desk.NextStep!.ReasonCode);
        Assert.Equal(TransferNextStep.ActionScanNeeds, desk.NextStep.ActionCode);
        Assert.False(desk.DemandsAttention);
    }

    [Fact]
    public void OpenNeedsWithoutTarget_PickTargetIsExecutable()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: null,
            openNeedCount: 1,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null);

        Assert.Equal(TransferNextStep.ReasonPickTarget, desk.NextStep!.ReasonCode);
        Assert.Equal(TransferNextStep.ActionPickTarget, desk.NextStep.ActionCode);
        Assert.False(desk.DemandsAttention);
    }

    [Fact]
    public void ListedTarget_StartProcessIsExecutable()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: null,
            openNeedCount: 1,
            openExitNeedCount: 0,
            listedTargetCount: 1,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null);

        Assert.Equal(TransferNextStep.ReasonStartProcess, desk.NextStep!.ReasonCode);
        Assert.Equal(TransferNextStep.ActionStartProcess, desk.NextStep.ActionCode);
        Assert.True(desk.DemandsAttention);
    }

    [Fact]
    public void ActiveProcess_AdvanceProcessIsExecutable()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: null,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 1,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: null);

        Assert.Equal(TransferNextStep.ReasonAdvanceProcess, desk.NextStep!.ReasonCode);
        Assert.Equal(TransferNextStep.ActionAdvanceProcess, desk.NextStep.ActionCode);
    }

    [Fact]
    public void NamedSaleCandidate_ReplacesInternalIdOnPlayerFacingCopy()
    {
        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            windowStatusName: "Açık",
            windowClosesOnDayNumber: 40,
            openNeedCount: 0,
            openExitNeedCount: 0,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: 1_000_000,
            budgetSpent: 0,
            squadFull: true,
            saleCandidatePlayerId: 17025,
            currentDayNumber: 30,
            saleCandidatePlayerName: "Rayan Raveloson",
            saleCandidateDetail: "Rayan Raveloson (DOS · GÜÇ 70)");

        Assert.Contains("Kadro dolu", desk.Headline, StringComparison.Ordinal);
        Assert.Contains("Rayan Raveloson (DOS · GÜÇ 70)", desk.ToDisplayText(), StringComparison.Ordinal);
        Assert.NotEqual(TransferNextStep.ReasonSellFringe, desk.NextStep?.ReasonCode);
        Assert.DoesNotContain("#17025", desk.ToDisplayText(), StringComparison.Ordinal);
        Assert.DoesNotContain("#17025", desk.ToSummaryText(), StringComparison.Ordinal);
        Assert.Equal(
            "Kadro dolu — satılacak oyuncuyu Kadro'dan seç.\nKadro dosyasında Satışa Çıkar veya Yer Aç ile slot aç.",
            desk.ToSummaryText());
    }
}
