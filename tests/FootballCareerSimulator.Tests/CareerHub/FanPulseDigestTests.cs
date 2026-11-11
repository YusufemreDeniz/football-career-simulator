using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class FanPulseDigestTests
{
    [Fact]
    public void Compose_StrongFormAndLeagueLeadProducePositivePulse()
    {
        var digest = FanPulseDigest.Compose(
            "Anadolu SK",
            rank: 1,
            clubCount: 18,
            recentPoints: 13,
            recentMatchCount: 5,
            goalDifference: 12,
            lastGoalMargin: 3,
            managerReputation: 72,
            boardConfidence: 80);

        Assert.InRange(digest.ApprovalRating, 80, 99);
        Assert.Equal("YÜKSELİYOR", digest.Trend);
        Assert.Equal(3, digest.Posts.Count);
        Assert.All(digest.Posts, post => Assert.Equal("#AnadoluSK", post.Tag));
    }

    [Fact]
    public void Compose_HeavyLossAndBottomRankProduceCriticalPulse()
    {
        var digest = FanPulseDigest.Compose(
            "Anadolu SK",
            rank: 18,
            clubCount: 18,
            recentPoints: 1,
            recentMatchCount: 5,
            goalDifference: -14,
            lastGoalMargin: -4,
            managerReputation: 35,
            boardConfidence: 25);

        Assert.InRange(digest.ApprovalRating, 5, 35);
        Assert.Equal("DÜŞÜYOR", digest.Trend);
        Assert.Contains(digest.Posts, post => !post.IsPositive);
    }
}
