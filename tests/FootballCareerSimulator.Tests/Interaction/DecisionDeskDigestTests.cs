using FootballCareerSimulator.Application.Interaction.Queries;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DecisionDeskDigestTests
{
    [Fact]
    public void Clear_WhenNoOpenRequests()
    {
        var digest = DecisionDeskDigest.Compose(
            new PendingDecisionsReadModel(0, Array.Empty<DecisionRequestLineReadModel>()),
            currentDayNumber: 10);

        Assert.False(digest.HasOpenDecision);
        Assert.Equal("Masada", digest.BrandTitle);
        Assert.Equal("Masada bekleyen yok — günün işine bak.", digest.Headline);
        Assert.Contains("Masada bekleyen yok", digest.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void HardPress_LeadsWithBlockedHeadline()
    {
        var pending = new PendingDecisionsReadModel(
            1,
            [
                new DecisionRequestLineReadModel(
                    DecisionRequestId: 7,
                    KindName: "Kritik basın sorusu",
                    SubjectPlayerId: 42,
                    ClubId: 1,
                    StatusName: "Open",
                    IsHardBlocker: true,
                    OpenedDayNumber: 8,
                    DeadlineDayNumber: 12,
                    SelectedOptionCode: null),
            ]);

        var digest = DecisionDeskDigest.Compose(
            pending,
            currentDayNumber: 10,
            subjectPlayerName: "Ahmet Yılmaz");

        Assert.True(digest.HasOpenDecision);
        Assert.True(digest.IsHardBlocker);
        Assert.Equal("Masada (zorunlu)", digest.BrandTitle);
        Assert.Equal("Basın kapıda — cevap vermeden ilerleyemezsin.", digest.Headline);
        Assert.Contains("ZORUNLU", digest.SupportingLine, StringComparison.Ordinal);
        Assert.Contains("Ahmet Yılmaz", digest.SupportingLine, StringComparison.Ordinal);
        Assert.DoesNotContain("oyuncu#", digest.SupportingLine, StringComparison.Ordinal);
        Assert.Equal(7, digest.DecisionRequestId);
    }

    [Fact]
    public void BoardDemand_HidesSentinelPlayerId()
    {
        var pending = new PendingDecisionsReadModel(
            2,
            [
                new DecisionRequestLineReadModel(
                    3,
                    "Yönetim talebi",
                    SubjectPlayerId: 9_000_000_001L,
                    ClubId: 1,
                    StatusName: "Open",
                    IsHardBlocker: false,
                    OpenedDayNumber: 5,
                    DeadlineDayNumber: 11,
                    SelectedOptionCode: null),
                new DecisionRequestLineReadModel(
                    4,
                    "Forma süresi talebi",
                    SubjectPlayerId: 9,
                    ClubId: 1,
                    StatusName: "Open",
                    IsHardBlocker: false,
                    OpenedDayNumber: 5,
                    DeadlineDayNumber: 20,
                    SelectedOptionCode: null),
            ]);

        var digest = DecisionDeskDigest.Compose(pending, currentDayNumber: 10);

        Assert.Equal("Yönetim masaya oturdu.", digest.Headline);
        Assert.DoesNotContain("oyuncu#", digest.SupportingLine, StringComparison.Ordinal);
        Assert.Contains("+1 kuyrukta", digest.SupportingLine, StringComparison.Ordinal);
        Assert.Contains("Yarın son", digest.SupportingLine, StringComparison.Ordinal);
    }

    [Fact]
    public void SoftPlayingTime_UsesCalmHeadline()
    {
        var pending = new PendingDecisionsReadModel(
            1,
            [
                new DecisionRequestLineReadModel(
                    1,
                    "Forma süresi talebi",
                    SubjectPlayerId: 5,
                    ClubId: 1,
                    StatusName: "Open",
                    IsHardBlocker: false,
                    OpenedDayNumber: 1,
                    DeadlineDayNumber: 1,
                    SelectedOptionCode: null),
            ]);

        var digest = DecisionDeskDigest.Compose(pending, currentDayNumber: 3);
        Assert.Equal("Forma süresi talebi bekliyor.", digest.Headline);
        Assert.Contains("Son gün", digest.SupportingLine, StringComparison.Ordinal);
    }

    [Fact]
    public void TransferWithBrokenPromiseCausality_UsesExitHeadline()
    {
        var pending = new PendingDecisionsReadModel(
            1,
            [
                new DecisionRequestLineReadModel(
                    9,
                    "Transfer isteği",
                    SubjectPlayerId: 12,
                    ClubId: 1,
                    StatusName: "Open",
                    IsHardBlocker: true,
                    OpenedDayNumber: 8,
                    DeadlineDayNumber: 14,
                    SelectedOptionCode: null),
            ]);

        var digest = DecisionDeskDigest.Compose(
            pending,
            currentDayNumber: 10,
            causalityLine: "Söz #2 bozuldu · güven düşük");

        Assert.Equal("Söz kırıldı — oyuncu ayrılmak istiyor.", digest.Headline);
        Assert.Contains("Söz #2 bozuldu", digest.SupportingLine, StringComparison.Ordinal);
        Assert.Equal("Söz #2 bozuldu · güven düşük", digest.CausalityLine);
    }
}
