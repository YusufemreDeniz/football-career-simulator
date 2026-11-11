using FootballCareerSimulator.Application.Competition.Queries;
using Xunit;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class StadiumAtmosphereDigestTests
{
    [Fact]
    public void HomeTopZone_EnthusiasticCrowd()
    {
        var digest = StadiumAtmosphereDigest.Compose(isHome: true, managedRank: 1, clubCount: 10);

        Assert.Equal(StadiumAtmosphereDigest.Brand, digest.BrandTitle);
        Assert.Contains("ev gecesi", digest.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("adını söylüyor", digest.CrowdLine, StringComparison.Ordinal);
    }

    [Fact]
    public void HomeBottomZone_TenseCrowd()
    {
        var digest = StadiumAtmosphereDigest.Compose(isHome: true, managedRank: 9, clubCount: 10);

        Assert.Contains("sabır sınanacak", digest.CrowdLine, StringComparison.Ordinal);
    }

    [Fact]
    public void HomeMidTable_ExpectsControl()
    {
        var digest = StadiumAtmosphereDigest.Compose(isHome: true, managedRank: 5, clubCount: 10);

        Assert.Contains("kontrolünü senden bekliyor", digest.CrowdLine, StringComparison.Ordinal);
    }

    [Fact]
    public void HomeNoRank_UsesNeutralCrowd()
    {
        var digest = StadiumAtmosphereDigest.Compose(isHome: true, managedRank: null, clubCount: 10);

        Assert.Contains("kontrolünü senden bekliyor", digest.CrowdLine, StringComparison.Ordinal);
    }

    [Fact]
    public void AwayTopZone_HostCrowdTargetsContender()
    {
        var digest = StadiumAtmosphereDigest.Compose(isHome: false, managedRank: 2, clubCount: 12);

        Assert.Contains("deplasman gecesi", digest.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("zirve yarışındaki misafiri", digest.CrowdLine, StringComparison.Ordinal);
    }

    [Fact]
    public void AwayBottomZone_HostCrowdSensesWeakness()
    {
        var digest = StadiumAtmosphereDigest.Compose(isHome: false, managedRank: 11, clubCount: 12);

        Assert.Contains("kırılganlık görüyor", digest.CrowdLine, StringComparison.Ordinal);
    }

    [Fact]
    public void AwayMidTable_NeedsEarlyComposure()
    {
        var digest = StadiumAtmosphereDigest.Compose(isHome: false, managedRank: 6, clubCount: 12);

        Assert.Contains("erken tutunman", digest.CrowdLine, StringComparison.Ordinal);
    }

    [Fact]
    public void SmallLeague_TopZoneUsesHalfRule()
    {
        var digest = StadiumAtmosphereDigest.Compose(isHome: true, managedRank: 2, clubCount: 4);

        Assert.Contains("adını söylüyor", digest.CrowdLine, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(11, 10)]
    [InlineData(1, 0)]
    public void InvalidStanding_UsesNeutralCrowd(int managedRank, int clubCount)
    {
        var digest = StadiumAtmosphereDigest.Compose(
            isHome: true,
            managedRank,
            clubCount);

        Assert.Contains("kontrolünü senden bekliyor", digest.CrowdLine, StringComparison.Ordinal);
    }
}
