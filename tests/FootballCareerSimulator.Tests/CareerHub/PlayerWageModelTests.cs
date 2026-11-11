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
    public void ManagerTrait_ReducesWageDemandWhenEnabled()
    {
        var standard = Evaluate(85, 88, age: 26).WeeklyWage;
        var discounted = Evaluate(85, 88, age: 26, managerTraitPercentage: -15).WeeklyWage;

        Assert.True(discounted < standard, $"Discounted wage {discounted} should be lower than standard {standard}.");
    }

    [Fact]
    public void StaffNegotiator_ReducesWageDemandWhenEnabled()
    {
        var standard = Evaluate(85, 88, age: 26).WeeklyWage;
        var discounted = Evaluate(85, 88, age: 26, staffNegotiatorPercentage: -10).WeeklyWage;

        Assert.True(discounted < standard, $"Staff-adjusted wage {discounted} should be lower than standard {standard}.");
    }

    [Fact]
    public void MinimumWageFloor_IsStrictlyPreservedForLowestInputs()
    {
        var result = Evaluate(
            40,
            40,
            age: 16,
            clubRating: 40,
            leagueRating: 40,
            managerTraitPercentage: -35,
            staffNegotiatorPercentage: -35);

        Assert.Equal(PlayerWageBalanceProfile.Default.MinimumWeeklyWage, result.WeeklyWage);
        Assert.True(result.MinimumApplied);
    }

    [Fact]
    public void NativeMinimumAndSimulatorBalanceMinimum_AreExplicitlySeparated()
    {
        Assert.Equal(50, PlayerWageBalanceProfile.Sm25NativeMinimumWeeklyWage);
        Assert.Equal(100, PlayerWageBalanceProfile.Default.MinimumWeeklyWage);
    }

    [Fact]
    public void MaximumWageCap_IsAppliedAfterRounding()
    {
        var profile = new PlayerWageBalanceProfile { MaximumWeeklyWage = 100_250 };
        var result = PlayerWageModel.Evaluate(
            new PlayerWageCalculationInput(
                99,
                99,
                25,
                99,
                99,
                ContractLengthYears: 5,
                IsPeaked: true,
                ManagerTraitPercentage: 35,
                StaffNegotiatorPercentage: 35,
                SquadRoleFactor: 1.30,
                MarketStatusFactor: 1.25),
            profile);

        Assert.Equal(profile.MaximumWeeklyWage, result.WeeklyWage);
        Assert.True(result.MaximumApplied);
        Assert.True(result.RoundedWeeklyWage > result.WeeklyWage);
    }

    [Fact]
    public void MaximumWageCap_PreservesJustBelowAndClampsJustAboveBoundary()
    {
        var input = new PlayerWageCalculationInput(92, 92, 27, 70, 68, IsPeaked: true);
        var justBelow = PlayerWageModel.Evaluate(
            input,
            new PlayerWageBalanceProfile { MaximumWeeklyWage = 132_000 });
        var justAbove = PlayerWageModel.Evaluate(
            input,
            new PlayerWageBalanceProfile { MaximumWeeklyWage = 130_000 });

        Assert.Equal(131_000, justBelow.WeeklyWage);
        Assert.False(justBelow.MaximumApplied);
        Assert.Equal(130_000, justAbove.WeeklyWage);
        Assert.True(justAbove.MaximumApplied);
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
        Assert.Equal(1_000, PlayerWageModel.RoundWage(987.5, profile));
        Assert.Equal(1_100, PlayerWageModel.RoundWage(1_050.0, profile));
        Assert.Equal(10_500, PlayerWageModel.RoundWage(10_250.0, profile));
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

        Assert.Throws<ArgumentException>(() =>
            PlayerWageModel.Evaluate(
                new PlayerWageCalculationInput(75, 80, 24, 75, 75),
                new PlayerWageBalanceProfile
                {
                    NonPeakedCurrentAbilityWeight = 0.9,
                    NonPeakedPotentialWeight = 0.2,
                }));
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
        double managerTraitPercentage = 0.0,
        double staffNegotiatorPercentage = 0.0,
        double squadRoleFactor = 1.0,
        double marketStatusFactor = 1.0) =>
        PlayerWageModel.Evaluate(
            new PlayerWageCalculationInput(
                ability,
                potential,
                age,
                clubRating,
                leagueRating,
                contractYears,
                isPeaked,
                managerTraitPercentage,
                staffNegotiatorPercentage,
                squadRoleFactor,
                marketStatusFactor));
}
