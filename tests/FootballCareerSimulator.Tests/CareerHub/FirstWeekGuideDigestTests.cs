using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class FirstWeekGuideDigestTests
{
    [Fact]
    public void Compose_RoutesFirstWeekAcrossRealHubPages()
    {
        var pages = Enumerable.Range(0, FirstWeekGuideDigest.TotalSteps)
            .Select(index => FirstWeekGuideDigest.Compose(true, index, daysSinceCareerStart: 2))
            .Select(digest => digest.CurrentStep!.TargetPageCode)
            .ToArray();

        Assert.Equal(7, pages.Length);
        Assert.Contains(FirstWeekGuideDigest.PageClub, pages);
        Assert.Contains(FirstWeekGuideDigest.PagePrep, pages);
        Assert.Contains(FirstWeekGuideDigest.PageTransfer, pages);
        Assert.Contains(FirstWeekGuideDigest.PageWorld, pages);
        Assert.Equal(FirstWeekGuideDigest.PageToday, pages[^1]);
    }

    [Theory]
    [InlineData(false, 0, 1)]
    [InlineData(true, 0, 8)]
    [InlineData(true, 7, 1)]
    public void Compose_HidesWhenDisabledPastFirstWeekOrComplete(bool enabled, int step, int days)
    {
        var digest = FirstWeekGuideDigest.Compose(enabled, step, days);

        Assert.False(digest.IsVisible);
        Assert.Null(digest.CurrentStep);
    }
}
