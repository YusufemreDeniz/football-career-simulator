using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class PlayerMarketValuationModelTests
{
    [Fact]
    public void Value_DoesNotDecreaseAsCurrentAbilityIncreases()
    {
        var values = Enumerable.Range(40, 60)
            .Select(ability => Evaluate(ability, ability, age: 25).MarketValue)
            .ToArray();

        Assert.All(values.Zip(values.Skip(1)), pair => Assert.True(pair.First <= pair.Second));
    }

    [Fact]
    public void EliteAbility_CreatesAVisibleScarcityPremium()
    {
        var regular = Evaluate(70, 70, age: 25).MarketValue;
        var elite = Evaluate(94, 94, age: 25).MarketValue;

        Assert.True(elite >= regular * 5, $"Expected elite premium, got regular={regular}, elite={elite}.");
    }

    [Fact]
    public void YoungHighPotentialPlayer_IsWorthMoreAtTheSameAbility()
    {
        var baseline = Evaluate(68, 68, age: 19).MarketValue;
        var prospect = Evaluate(68, 90, age: 19).MarketValue;

        Assert.True(prospect > baseline);
    }

    [Fact]
    public void PotentialInfluence_FadesWithAge()
    {
        var youngGap = Evaluate(68, 90, age: 18).MarketValue - Evaluate(68, 68, age: 18).MarketValue;
        var veteranGap = Evaluate(68, 90, age: 34).MarketValue - Evaluate(68, 68, age: 34).MarketValue;

        Assert.True(youngGap > veteranGap);
        Assert.Equal(0, veteranGap);
    }

    [Fact]
    public void ExpiringContract_ReducesMarketValue()
    {
        var expiring = Evaluate(82, 86, age: 25, contractYears: 0.25).MarketValue;
        var protectedContract = Evaluate(82, 86, age: 25, contractYears: 4).MarketValue;

        Assert.True(expiring < protectedContract);
    }

    [Fact]
    public void ShortTermSignals_AreBoundedAndDoNotOvertakeIntrinsicValue()
    {
        var low = Evaluate(80, 84, age: 26, form: -1, morale: 0, minutes: 0);
        var high = Evaluate(80, 84, age: 26, form: 1, morale: 1, minutes: 1);

        Assert.InRange(low.ShortTermMultiplier, 0.80, 1.00);
        Assert.InRange(high.ShortTermMultiplier, 1.00, 1.20);
        Assert.True(high.MarketValue < low.MarketValue * 1.5);
    }

    [Fact]
    public void InvalidGameplayInputs_AreClampedToFiniteNonNegativeResult()
    {
        var value = Evaluate(-500, 500, age: 500, contractYears: -20, form: -50, morale: 50, minutes: -50);

        Assert.InRange(value.MarketValue, 250_000, PlayerValuationBalanceProfile.Default.MaximumMarketValue);
        Assert.True(double.IsFinite(value.AbilityValue));
        Assert.True(double.IsFinite(value.MarketContextMultiplier));
        Assert.True(double.IsFinite(value.ShortTermMultiplier));
    }

    [Fact]
    public void NonFiniteInput_IsRejected()
    {
        var input = Input(80, 85, age: 25) with { LeagueMarketFactor = double.NaN };

        Assert.Throws<ArgumentOutOfRangeException>(() => PlayerMarketValuationModel.Evaluate(input));
    }

    [Fact]
    public void FreeAgent_HasNoTransferMarketValueButKeepsIntrinsicBreakdown()
    {
        var value = Evaluate(82, 84, age: 28, isFreeAgent: true);

        Assert.Equal(0, value.MarketValue);
        Assert.True(value.AbilityValue > 0);
        Assert.Equal(0, value.ContractMultiplier);
    }

    [Fact]
    public void CustomProfile_ChangesBalanceWithoutChangingCalculatorCode()
    {
        var conservative = new PlayerValuationBalanceProfile
        {
            MaximumMarketValue = 60_000_000,
            AbilityExponent = 6.0,
        };

        var defaultValue = Evaluate(85, 88, age: 25).MarketValue;
        var customValue = PlayerMarketValuationModel.Evaluate(
            Input(85, 88, age: 25),
            conservative).MarketValue;

        Assert.True(customValue < defaultValue);
        Assert.InRange(customValue, conservative.MinimumMarketValue, conservative.MaximumMarketValue);
    }

    private static PlayerMarketValuationBreakdown Evaluate(
        int ability,
        int potential,
        int age,
        double contractYears = 3,
        double form = 0,
        double morale = 0.5,
        double minutes = 0.5,
        bool isFreeAgent = false) =>
        PlayerMarketValuationModel.Evaluate(
            Input(ability, potential, age, contractYears, form, morale, minutes, isFreeAgent));

    private static PlayerMarketValuationInput Input(
        int ability,
        int potential,
        int age,
        double contractYears = 3,
        double form = 0,
        double morale = 0.5,
        double minutes = 0.5,
        bool isFreeAgent = false) =>
        new(
            ability,
            potential,
            age,
            contractYears,
            SourceClubStrength: 75,
            RecentForm: form,
            Morale: morale,
            MinutesShare: minutes,
            IsFreeAgent: isFreeAgent);
}
