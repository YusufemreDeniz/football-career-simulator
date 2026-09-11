using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class PlayerWageModelTests
{
    [Fact]
    public void Wage_DoesNotDecreaseAsCurrentAbilityIncreases()
    {
        var wages = Enumerable.Range(45, 54)
            .Select(ability => Evaluate(ability, ability, age: 25).WeeklyWage)
            .ToArray();

        Assert.All(wages.Zip(wages.Skip(1)), pair => Assert.True(pair.First <= pair.Second));
    }

    [Fact]
    public void EliteAbility_CommandsSignificantWagePremium()
    {
        var midtier = Evaluate(70, 70, age: 25, clubRating: 72, leagueRating: 70).WeeklyWage;
        var elite = Evaluate(94, 94, age: 25, clubRating: 92, leagueRating: 90).WeeklyWage;

        Assert.True(elite >= midtier * 10, $"Expected elite wage premium. Midtier={midtier}, Elite={elite}");
    }

    [Fact]
    public void HighPotentialProspect_CommandsHigherWageThanFlatPlayer()
    {
        var flat = Evaluate(68, 68, age: 19, isPeaked: false).WeeklyWage;
        var prospect = Evaluate(68, 88, age: 19, isPeaked: false).WeeklyWage;

        Assert.True(prospect > flat, $"Prospect wage {prospect} should exceed flat wage {flat}.");
    }

    [Fact]
    public void PeakedPlayer_IgnoresPotentialDifferences()
    {
        var peakedBaseline = Evaluate(80, 80, age: 29, isPeaked: true).WeeklyWage;
        var peakedWithHighPot = Evaluate(80, 95, age: 29, isPeaked: true).WeeklyWage;

        Assert.Equal(peakedBaseline, peakedWithHighPot);
    }

    [Fact]
    public void HigherTierClub_OffersHigherWagesForSamePlayer()
    {
        var lowerTier = Evaluate(78, 80, age: 24, clubRating: 65, leagueRating: 65).WeeklyWage;
        var eliteTier = Evaluate(78, 80, age: 24, clubRating: 90, leagueRating: 88).WeeklyWage;

        Assert.True(eliteTier > lowerTier, $"Elite club wage {eliteTier} should be higher than lower tier {lowerTier}.");
    }

    [Fact]
    public void LongerContract_GraduallyIncreasesWeeklyWageDemand()
    {
        var oneYear = Evaluate(82, 85, age: 25, contractYears: 1).WeeklyWage;
        var fiveYear = Evaluate(82, 85, age: 25, contractYears: 5).WeeklyWage;

        Assert.True(fiveYear > oneYear, $"5-year contract wage {fiveYear} should exceed 1-year wage {oneYear}.");
    }

    [Fact]
    public void NegotiationDiscount_ReducesWageDemand()
    {
        var standard = Evaluate(85, 88, age: 26, discount: 0.0).WeeklyWage;
        var discounted = Evaluate(85, 88, age: 26, discount: 0.15).WeeklyWage;

        Assert.True(discounted < standard, $"Discounted wage {discounted} should be lower than standard {standard}.");
    }

    [Fact]
    public void MinimumWageFloor_IsStrictlyPreservedForLowestInputs()
    {
        var result = Evaluate(40, 40, age: 16, clubRating: 40, leagueRating: 40, discount: 0.35);

        Assert.True(result.WeeklyWage >= PlayerWageBalanceProfile.Default.MinimumWeeklyWage);
    }

    [Fact]
    public void Rounding_AdheresToDefinedStepProfile()
    {
        var profile = PlayerWageBalanceProfile.Default;
        var smallWage = PlayerWageModel.RoundWage(450.0, profile);
        var mediumWage = PlayerWageModel.RoundWage(4_320.0, profile);
        var largeWage = PlayerWageModel.RoundWage(27_340.0, profile);
        var eliteWage = PlayerWageModel.RoundWage(184_750.0, profile);

        Assert.Equal(0, smallWage % profile.RoundingStepSmall);
        Assert.Equal(0, mediumWage % profile.RoundingStepMedium);
        Assert.Equal(0, largeWage % profile.RoundingStepLarge);
        Assert.Equal(0, eliteWage % profile.RoundingStepElite);
    }

    [Fact]
    public void InvalidInputs_ThrowExpectedExceptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Evaluate(75, 80, age: 12));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Evaluate(75, 80, age: 65));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PlayerWageModel.Evaluate(new PlayerWageCalculationInput(75, 80, 24, 75, 75, SquadRoleFactor: double.NaN)));
    }

    [Fact]
    public void Evaluation_IsPureAndDeterministic()
    {
        var first = Evaluate(84, 88, age: 24, clubRating: 82, leagueRating: 80);
        var second = Evaluate(84, 88, age: 24, clubRating: 82, leagueRating: 80);

        Assert.Equal(first.WeeklyWage, second.WeeklyWage);
        Assert.Equal(first.AnnualWage, second.AnnualWage);
        Assert.Equal(first.BaseWage, second.BaseWage);
    }

    private static PlayerWageBreakdown Evaluate(
        int ability,
        int potential,
        int age,
        int clubRating = 75,
        int leagueRating = 75,
        int contractYears = 3,
        bool isPeaked = false,
        double discount = 0.0) =>
        PlayerWageModel.Evaluate(
            new PlayerWageCalculationInput(
                ability,
                potential,
                age,
                clubRating,
                leagueRating,
                contractYears,
                isPeaked,
                discount));
}
