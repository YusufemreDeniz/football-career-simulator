using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class MobileRuntimeTelemetryDigestTests
{
    [Fact]
    public void Compose_RequiresThirtySecondsOfEvidence()
    {
        var digest = MobileRuntimeTelemetryDigest.Compose(
            12,
            720,
            1d / 60,
            0.019,
            0.032,
            2,
            24 * 1024 * 1024);

        Assert.Equal(MobileRuntimeTelemetryDigest.WarmingUp, digest.VerdictCode);
        Assert.False(digest.HasEnoughEvidence);
    }

    [Fact]
    public void Compose_AcceptsStableRealDeviceFrameBudget()
    {
        var digest = MobileRuntimeTelemetryDigest.Compose(
            60,
            3600,
            1d / 60,
            0.020,
            0.041,
            20,
            48 * 1024 * 1024);

        Assert.Equal(MobileRuntimeTelemetryDigest.Ready, digest.VerdictCode);
        Assert.True(digest.MeetsFrameBudget);
        Assert.InRange(digest.AverageFps, 59.9, 60.1);
        Assert.Contains("FPS", digest.DetailLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_FlagsSlowTailFrames()
    {
        var digest = MobileRuntimeTelemetryDigest.Compose(
            60,
            3000,
            1d / 52,
            0.031,
            0.120,
            120,
            64 * 1024 * 1024);

        Assert.Equal(MobileRuntimeTelemetryDigest.Review, digest.VerdictCode);
        Assert.False(digest.MeetsFrameBudget);
        Assert.True(digest.HitchPercent > 2);
    }
}
