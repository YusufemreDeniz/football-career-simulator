using FootballCareerSimulator.Application.TeamPreparation.Queries;
using Xunit;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class PreMatchReadinessChecklistDigestTests
{
    [Fact]
    public void Compose_ReturnsFullyReady_WhenAllCriteriaSatisfied()
    {
        var digest = PreMatchReadinessChecklistDigest.Compose(
            hasUpcomingMatch: true,
            isSelectionApproved: true,
            startingXiCount: 11,
            unavailableInXiCount: 0,
            fatiguedInXiCount: 0,
            hasTacticPlan: true,
            hasTrainingPlan: true,
            activePromiseStarterCount: 2,
            activePromiseBenchedCount: 0);

        Assert.True(digest.HasUpcomingMatch);
        Assert.True(digest.IsFullyReady);
        Assert.Equal(5, digest.ReadyCount);
        Assert.Equal(5, digest.TotalCount);
        Assert.Contains("5/5", digest.SummaryBadgeText, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_FlagsWarnings_WhenUnapprovedOrFatiguedOrPromiseBenched()
    {
        var digest = PreMatchReadinessChecklistDigest.Compose(
            hasUpcomingMatch: true,
            isSelectionApproved: false,
            startingXiCount: 11,
            unavailableInXiCount: 1,
            fatiguedInXiCount: 2,
            hasTacticPlan: true,
            hasTrainingPlan: false,
            activePromiseStarterCount: 0,
            activePromiseBenchedCount: 1);

        Assert.True(digest.HasUpcomingMatch);
        Assert.False(digest.IsFullyReady);
        Assert.Equal(0, digest.ReadyCount);
        Assert.Equal(5, digest.TotalCount);
        Assert.Contains("0/5", digest.SummaryBadgeText, StringComparison.Ordinal);
        Assert.Contains("sözü verilen oyuncu", digest.ToCompactText(), StringComparison.Ordinal);
    }
}
