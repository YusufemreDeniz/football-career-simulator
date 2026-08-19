using FootballCareerSimulator.Application.Competition.Queries;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class FormStreakVerdictDigestTests
{
    [Fact]
    public void WinningStreakAndWin_ExtendsTheRun()
    {
        var verdict = FormStreakVerdictDigest.Compose(
            DressingRoomEchoDigest.MomentumWinningStreak,
            managedGoalMargin: 2);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.WinningExtended, verdict!.VerdictCode);
        Assert.Contains("dördüncü galibiyet", verdict.Headline, StringComparison.Ordinal);
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
            managedGoalMargin: -2);

        Assert.NotNull(verdict);
        Assert.Equal(FormStreakVerdictDigest.LosingDeepened, verdict!.VerdictCode);
        Assert.Contains("dördüncü mağlubiyet", verdict.Headline, StringComparison.Ordinal);
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
}
