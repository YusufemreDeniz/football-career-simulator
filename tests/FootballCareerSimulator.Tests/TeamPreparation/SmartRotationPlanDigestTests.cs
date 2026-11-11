using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.TeamPreparation;
using Xunit;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class SmartRotationPlanDigestTests
{
    [Fact]
    public void Compose_RotatesFatiguedStarter_AndRetainsPromisedPlayer()
    {
        var candidates = new List<SmartRotationCandidate>
        {
            new(0, "Kaleci As", "GK", MvpSquadPositionGroup.Goalkeeper, MvpSquadPositionRole.Goalkeeper, 82, 92, 10, true, false),
            new(1, "Kaleci Yedek", "GK", MvpSquadPositionGroup.Goalkeeper, MvpSquadPositionRole.Goalkeeper, 75, 95, 5, true, false),
            new(2, "Sag Bek As", "RB", MvpSquadPositionGroup.Defender, MvpSquadPositionRole.RightBack, 80, 90, 15, true, false),
            new(3, "Stoper 1", "CB", MvpSquadPositionGroup.Defender, MvpSquadPositionRole.CentreBack, 81, 88, 18, true, false),
            new(4, "Stoper 2", "CB", MvpSquadPositionGroup.Defender, MvpSquadPositionRole.CentreBack, 79, 89, 14, true, false),
            new(5, "Sol Bek As", "LB", MvpSquadPositionGroup.Defender, MvpSquadPositionRole.LeftBack, 78, 91, 12, true, false),
            new(6, "Sag Kanat Yorgun", "RM", MvpSquadPositionGroup.Midfielder, MvpSquadPositionRole.RightMidfielder, 82, 54, 78, true, false),
            new(7, "Orta Saha 1", "CM", MvpSquadPositionGroup.Midfielder, MvpSquadPositionRole.CentralMidfielder, 80, 90, 14, true, false),
            new(8, "Orta Saha Sozlu", "CM", MvpSquadPositionGroup.Midfielder, MvpSquadPositionRole.CentralMidfielder, 77, 84, 24, true, true),
            new(9, "Sol Kanat As", "LM", MvpSquadPositionGroup.Midfielder, MvpSquadPositionRole.LeftMidfielder, 80, 90, 15, true, false),
            new(10, "Forvet 1", "ST", MvpSquadPositionGroup.Forward, MvpSquadPositionRole.Striker, 83, 91, 12, true, false),
            new(11, "Forvet 2", "ST", MvpSquadPositionGroup.Forward, MvpSquadPositionRole.Striker, 81, 89, 16, true, false),
            new(12, "Sag Kanat Zinde", "RM", MvpSquadPositionGroup.Midfielder, MvpSquadPositionRole.RightMidfielder, 78, 96, 8, true, false),
            new(13, "Orta Saha Yedek", "CM", MvpSquadPositionGroup.Midfielder, MvpSquadPositionRole.CentralMidfielder, 78, 88, 18, true, false),
            new(14, "Sakat Oyuncu", "ST", MvpSquadPositionGroup.Forward, MvpSquadPositionRole.Striker, 90, 40, 80, false, false),
        };

        var currentXi = new[] { 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        var plan = SmartRotationPlanDigest.Compose(Formation.F442, candidates, currentXi);

        Assert.True(plan.IsValid);
        Assert.Equal(11, plan.StartingSlotIndices.Count);
        Assert.Equal(0, plan.StartingSlotIndices[0]);
        Assert.DoesNotContain(14, plan.StartingSlotIndices);
        Assert.DoesNotContain(14, plan.BenchSlotIndices);
        Assert.Contains(12, plan.StartingSlotIndices);
        Assert.DoesNotContain(6, plan.StartingSlotIndices);
        Assert.Contains(8, plan.StartingSlotIndices);
        Assert.Equal(1, plan.RestedFatiguedCount);
        Assert.Equal(1, plan.PromisedStarterCount);
        Assert.Contains(plan.SwapExplanationLines, line => line.Contains("Sag Kanat Yorgun", StringComparison.Ordinal));
        Assert.Contains(plan.SwapExplanationLines, line => line.Contains("Orta Saha Sozlu", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_ReturnsInvalid_WhenFewerThanElevenAvailable()
    {
        var candidates = Enumerable.Range(0, 10)
            .Select(i => new SmartRotationCandidate(
                i,
                $"Oyuncu {i}",
                "CM",
                MvpSquadPositionGroup.Midfielder,
                MvpSquadPositionRole.CentralMidfielder,
                75,
                90,
                10,
                true,
                false))
            .ToArray();

        var plan = SmartRotationPlanDigest.Compose(Formation.F442, candidates);

        Assert.False(plan.IsValid);
        Assert.Empty(plan.StartingSlotIndices);
    }
}
