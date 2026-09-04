using FootballCareerSimulator.Application.Competition.Queries;
using Xunit;

namespace FootballCareerSimulator.Tests.Competition;

public class CaptainReactionDigestTests
{
    [Fact]
    public void NoManagedMatch_ReturnsNull()
    {
        var digest = CaptainReactionDigest.Compose(null, dismissed: false);

        Assert.Null(digest);
    }

    [Fact]
    public void BigWin_CaptainOwnsTheNight()
    {
        var digest = CaptainReactionDigest.Compose(4, dismissed: false);

        Assert.NotNull(digest);
        Assert.Equal(CaptainReactionDigest.Brand, digest!.BrandTitle);
        Assert.Contains("saha bizimdi", digest.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void NarrowWin_ReliefWithWarning()
    {
        var digest = CaptainReactionDigest.Compose(1, dismissed: false);

        Assert.NotNull(digest);
        Assert.Contains("sallanarak", digest!.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Draw_UnfinishedBusiness()
    {
        var digest = CaptainReactionDigest.Compose(0, dismissed: false);

        Assert.NotNull(digest);
        Assert.Contains("İş bitmedi", digest!.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void NarrowLoss_CaptainTakesBlame()
    {
        var digest = CaptainReactionDigest.Compose(-2, dismissed: false);

        Assert.NotNull(digest);
        Assert.Contains("kayıp bizim", digest!.VoiceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void HeavyLoss_SilenceInTheRoom()
    {
        var digest = CaptainReactionDigest.Compose(-4, dismissed: false);

        Assert.NotNull(digest);
        Assert.Contains("sessizlik", digest!.VoiceLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dismissed_EmptyCorridorEvenAfterWin()
    {
        var digest = CaptainReactionDigest.Compose(3, dismissed: true);

        Assert.NotNull(digest);
        Assert.Contains("koltuk gitti", digest!.VoiceLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Kaptan:", digest!.VoiceLine, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(-5)]
    public void MarginBoundary_HeavyLossZone(int margin)
    {
        var digest = CaptainReactionDigest.Compose(margin, dismissed: false);

        Assert.NotNull(digest);
        Assert.Contains("sessizlik", digest!.VoiceLine, StringComparison.OrdinalIgnoreCase);
    }
}
