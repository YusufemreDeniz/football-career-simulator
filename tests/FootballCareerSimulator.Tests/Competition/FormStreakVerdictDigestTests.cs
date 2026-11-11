using FootballCareerSimulator.Application.Competition.Queries;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class FormStreakVerdictDigestTests
{
    [Fact]
    public void WinningStreakAndWin_ExtendsTheRun()
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumWinningStreak,
            managedGoalMargin: 2,
            enteringMomentumLength: 4);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.WinningExtended, verdict!.VerdictCode);
        Assert.Contains("5. galibiyet", verdict.Headline, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WinningStreakWithoutWin_ClosesTheRun(int margin)
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumWinningStreak,
            margin);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.WinningEnded, verdict!.VerdictCode);
        Assert.Contains("sona erdi", verdict.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void LosingStreakAndLoss_DeepensTheCrisis()
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumLosingStreak,
            managedGoalMargin: -2,
            enteringMomentumLength: 5);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.LosingDeepened, verdict!.VerdictCode);
        Assert.Contains("6. mağlubiyet", verdict.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void LosingStreakAndWin_BreaksTheCrisis()
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumLosingStreak,
            managedGoalMargin: 1);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.LosingBroken, verdict!.VerdictCode);
        Assert.Contains("Kriz kırıldı", verdict.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void LosingStreakAndDraw_StopsTheLosingRunWithoutClaimingAWin()
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumLosingStreak,
            managedGoalMargin: 0);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.LosingBroken, verdict!.VerdictCode);
        Assert.Contains("ilk nefes", verdict.Headline, StringComparison.Ordinal);
        Assert.DoesNotContain("galibiyet", verdict.Headline, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(DressingRoomEchoDigest.MomentumMixed, 1)]
    [InlineData(DressingRoomEchoDigest.MomentumWinningStreak, null)]
    public void NoEnteringStreakOrManagedResult_ReturnsNull(string? momentum, int? margin)
    {
        Assert.Null(FormStreakVerdictDigest.Compose(momentum, margin));
    }

    [Fact]
    public void StreakBattleWin_ExtendsOursAndEndsRivals()
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumWinningStreak,
            managedGoalMargin: 1,
            enteringMomentumLength: 5,
            opponentWinningStreakLength: 4);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.StreakBattleWon, verdict!.VerdictCode);
        Assert.Contains("6 galibiyete çıktı", verdict.Headline, StringComparison.Ordinal);
        Assert.Contains("rakibin 4 maçlık serisi bitti", verdict.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void StreakBattleDraw_StopsBothRuns()
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumWinningStreak,
            managedGoalMargin: 0,
            enteringMomentumLength: 5,
            opponentWinningStreakLength: 4);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.StreakBattleDrawn, verdict!.VerdictCode);
        Assert.Contains("senin 5 maçlık", verdict.Headline, StringComparison.Ordinal);
        Assert.Contains("rakibin 4 maçlık", verdict.Headline, StringComparison.Ordinal);
        Assert.Contains("sona erdi", verdict.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void StreakBattleLoss_EndsOursAndExtendsRivals()
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumWinningStreak,
            managedGoalMargin: -1,
            enteringMomentumLength: 5,
            opponentWinningStreakLength: 4);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.StreakBattleLost, verdict!.VerdictCode);
        Assert.Contains("5 maçlık serin bitti", verdict.Headline, StringComparison.Ordinal);
        Assert.Contains("5 galibiyete çıktı", verdict.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void CrisisWin_BreaksBothOpposingRuns()
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumLosingStreak,
            managedGoalMargin: 2,
            enteringMomentumLength: 4,
            opponentWinningStreakLength: 5);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.LosingBroken, verdict!.VerdictCode);
        Assert.Contains("4 yenilgin", verdict.Headline, StringComparison.Ordinal);
        Assert.Contains("5 galibiyeti", verdict.Headline, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, FormStreakVerdictDigest.LosingBroken, "Krizde ilk nefes", "5 galibiyetlik serisi de bitti")]
    [InlineData(-1, FormStreakVerdictDigest.LosingDeepened, "5. mağlubiyet", "serisi 6 galibiyete çıktı")]
    public void CrisisWithoutWin_ReportsBothRuns(
        int margin,
        string expectedCode,
        string managedLine,
        string opponentLine)
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumLosingStreak,
            margin,
            enteringMomentumLength: 4,
            opponentWinningStreakLength: 5);

        Assert.NotNull(verdict);
        Assert.Equal(expectedCode, verdict!.VerdictCode);
        Assert.Contains(managedLine, verdict.Headline, StringComparison.Ordinal);
        Assert.Contains(opponentLine, verdict.Headline, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, FormStreakVerdictDigest.RivalStreakStopped, "serisini bitirdin")]
    [InlineData(0, FormStreakVerdictDigest.RivalStreakStopped, "beraberlikle durdurdun")]
    [InlineData(-1, FormStreakVerdictDigest.RivalStreakExtended, "serisi 6 maça çıktı")]
    public void OpponentOnlyWinningRun_ReceivesAResultVerdict(
        int margin,
        string expectedCode,
        string expectedLine)
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumMixed,
            margin,
            opponentWinningStreakLength: 5);

        Assert.NotNull(verdict);
        Assert.Equal(expectedCode, verdict!.VerdictCode);
        Assert.Contains(expectedLine, verdict.Headline, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, FormStreakVerdictDigest.RivalCrisisDeepened, "Fırsatı değerlendirdin", "6 maça çıktı")]
    [InlineData(0, FormStreakVerdictDigest.RivalCrisisStopped, "beraberlikle durdu", "5 maçlık")]
    [InlineData(-1, FormStreakVerdictDigest.RivalCrisisRevived, "Rakibi ayağa kaldırdın", "sana karşı bitirdi")]
    public void OpponentOnlyLosingRun_ReceivesAResultVerdict(
        int margin,
        string expectedCode,
        string expectedLine,
        string expectedDetail)
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumMixed,
            margin,
            opponentLosingStreakLength: 5);

        Assert.NotNull(verdict);
        Assert.Equal(expectedCode, verdict!.VerdictCode);
        Assert.Contains(expectedLine, verdict.Headline, StringComparison.Ordinal);
        Assert.Contains(expectedDetail, verdict.Headline, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, FormStreakVerdictDigest.FormGapConfirmed, "serin 6 galibiyete", "krizi 5 mağlubiyete")]
    [InlineData(-1, FormStreakVerdictDigest.FormGapReversed, "5 galibiyetlik serin bitti", "4 maçlık mağlubiyet serisini")]
    public void WinningRunAgainstLosingOpponent_SettlesTheFormGap(
        int margin,
        string expectedCode,
        string managedLine,
        string opponentLine)
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumWinningStreak,
            margin,
            enteringMomentumLength: 5,
            opponentLosingStreakLength: 4);

        Assert.NotNull(verdict);
        Assert.Equal(expectedCode, verdict!.VerdictCode);
        Assert.Contains(managedLine, verdict.Headline, StringComparison.Ordinal);
        Assert.Contains(opponentLine, verdict.Headline, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, "Kırılma gecesini kazandın", "4 yenilgin bitti", "6 mağlubiyete çıktı")]
    [InlineData(0, "Kırılma gecesi kilitlendi", "4", "5 maçlık mağlubiyet serisi")]
    [InlineData(-1, "Kırılma gecesini rakip kazandı", "5 mağlubiyete çıktı", "5 maçlık serisini kırdı")]
    public void BothTeamsOnLosingRuns_SettlesTheCrisisNight(
        int margin,
        string expectedHeadline,
        string managedLine,
        string opponentLine)
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumLosingStreak,
            margin,
            enteringMomentumLength: 4,
            opponentLosingStreakLength: 5);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.CrisisDuelSettled, verdict!.VerdictCode);
        Assert.Contains(expectedHeadline, verdict.Headline, StringComparison.Ordinal);
        Assert.Contains(managedLine, verdict.Headline, StringComparison.Ordinal);
        Assert.Contains(opponentLine, verdict.Headline, StringComparison.Ordinal);
    }
}
