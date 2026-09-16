namespace FootballCareerSimulator.Tests.TeamPreparation;

using FootballCareerSimulator.Domain.TeamPreparation;
using Xunit;

public sealed class TeamChemistryTests
{
    [Fact]
    public void PerfectStartingXiAndWinStreak_YieldsHighChemistryAndBonusMultiplier()
    {
        var result = TeamChemistryCalculator.Evaluate(
            starterCount: 11,
            naturalPositionMatches: 11,
            averageSquadAge: 26,
            winStreakLength: 4,
            facilityLevel: 4);

        Assert.True(result.ChemistryScore >= 85, $"Expected high chemistry >= 85, got {result.ChemistryScore}");
        Assert.Equal("Mükemmel Uyum", result.ChemistryBand);
        Assert.True(result.PerformanceMultiplier >= 1.05, $"Expected multiplier >= 1.05, got {result.PerformanceMultiplier}");
        Assert.Contains("gözü kapalı", result.NarrativeSummary);
    }

    [Fact]
    public void DisjointedSquadWithZeroNaturalPositionsAndLosingStreak_YieldsLowChemistry()
    {
        var result = TeamChemistryCalculator.Evaluate(
            starterCount: 11,
            naturalPositionMatches: 2,
            averageSquadAge: 35,
            winStreakLength: -3,
            facilityLevel: 1);

        Assert.True(result.ChemistryScore <= 55, $"Expected low chemistry <= 55, got {result.ChemistryScore}");
        Assert.True(result.PerformanceMultiplier <= 1.0, $"Expected multiplier <= 1.0, got {result.PerformanceMultiplier}");
    }

    [Theory]
    [InlineData(11, 11, 26, 10, 5)]
    [InlineData(11, 0, 40, -10, 1)]
    [InlineData(0, 0, 18, 0, 1)]
    public void ChemistryScore_AlwaysClampedBetweenMinAndMax(
        int starterCount,
        int naturalPositions,
        int avgAge,
        int streak,
        int facility)
    {
        var result = TeamChemistryCalculator.Evaluate(
            starterCount,
            naturalPositions,
            avgAge,
            streak,
            facility);

        Assert.InRange(result.ChemistryScore, TeamChemistryCalculator.MinChemistry, TeamChemistryCalculator.MaxChemistry);
        Assert.InRange(result.PerformanceMultiplier, 0.90, 1.10);
    }
}
