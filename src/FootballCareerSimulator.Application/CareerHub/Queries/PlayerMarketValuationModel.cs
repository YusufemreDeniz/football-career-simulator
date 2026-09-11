namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed record ValuationCurvePoint(double Input, double Multiplier);

public sealed class PlayerValuationBalanceProfile
{
    public int MinimumAbility { get; init; } = 40;
    public int MaximumAbility { get; init; } = 99;
    public int MinimumMarketValue { get; init; } = 250_000;
    public int MaximumMarketValue { get; init; } = 160_000_000;
    public int RoundingStep { get; init; } = 50_000;
    public double AbilityExponent { get; init; } = 4.6;
    public double PotentialPremiumScale { get; init; } = 1.0;
    public double SourceClubContextMinimum { get; init; } = 0.90;
    public double SourceClubContextMaximum { get; init; } = 1.12;
    public double FormImpactLimit { get; init; } = 0.08;
    public double MoraleImpactLimit { get; init; } = 0.04;
    public double MinutesImpactLimit { get; init; } = 0.04;
    public double TransferListedMultiplier { get; init; } = 0.95;
    public double TransferRequestedMultiplier { get; init; } = 0.90;
    public double BuyerOpeningOfferFactor { get; init; } = 0.90;

    public IReadOnlyList<ValuationCurvePoint> AgeMultiplierCurve { get; init; } =
    [
        new(16, 0.78),
        new(18, 0.92),
        new(21, 1.10),
        new(24, 1.16),
        new(27, 1.08),
        new(30, 0.90),
        new(33, 0.68),
        new(36, 0.46),
        new(40, 0.25),
    ];

    public IReadOnlyList<ValuationCurvePoint> PotentialWeightCurve { get; init; } =
    [
        new(16, 1.00),
        new(18, 0.95),
        new(21, 0.78),
        new(24, 0.50),
        new(27, 0.25),
        new(30, 0.08),
        new(33, 0.00),
    ];

    public IReadOnlyList<ValuationCurvePoint> ContractMultiplierCurve { get; init; } =
    [
        new(0.0, 0.55),
        new(0.5, 0.66),
        new(1.0, 0.78),
        new(2.0, 1.00),
        new(3.0, 1.08),
        new(4.0, 1.12),
        new(5.0, 1.14),
    ];

    public static PlayerValuationBalanceProfile Default { get; } = new();
}

public sealed record PlayerMarketValuationInput(
    int CurrentAbility,
    int PotentialAbility,
    int Age,
    double ContractYearsRemaining,
    int SourceClubStrength,
    double LeagueMarketFactor = 1.0,
    double PositionScarcityFactor = 1.0,
    double RecentForm = 0.0,
    double Morale = 0.5,
    double MinutesShare = 0.5,
    bool IsTransferListed = false,
    bool HasRequestedTransfer = false,
    bool IsFreeAgent = false);

public sealed record PlayerMarketValuationBreakdown(
    int MarketValue,
    double AbilityValue,
    double PotentialMultiplier,
    double AgeMultiplier,
    double ContractMultiplier,
    double MarketContextMultiplier,
    double ShortTermMultiplier);

public static class PlayerMarketValuationModel
{
    public static PlayerMarketValuationBreakdown Evaluate(
        PlayerMarketValuationInput input,
        PlayerValuationBalanceProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        profile ??= PlayerValuationBalanceProfile.Default;
        ValidateInput(input);
        Validate(profile);

        var ability = Math.Clamp(input.CurrentAbility, profile.MinimumAbility, profile.MaximumAbility);
        var potential = Math.Clamp(input.PotentialAbility, ability, profile.MaximumAbility);
        var abilityRange = profile.MaximumAbility - profile.MinimumAbility;
        var normalizedAbility = (ability - profile.MinimumAbility) / (double)abilityRange;
        var abilityValue = profile.MinimumMarketValue
            + (profile.MaximumMarketValue - profile.MinimumMarketValue)
            * Math.Pow(normalizedAbility, profile.AbilityExponent);

        var potentialGap = (potential - ability) / (double)abilityRange;
        var potentialWeight = Interpolate(profile.PotentialWeightCurve, input.Age);
        var potentialMultiplier = 1.0 + potentialGap * profile.PotentialPremiumScale * potentialWeight;
        var ageMultiplier = Interpolate(profile.AgeMultiplierCurve, input.Age);
        var contractMultiplier = input.IsFreeAgent
            ? 0.0
            : Interpolate(profile.ContractMultiplierCurve, Math.Clamp(input.ContractYearsRemaining, 0.0, 5.0));

        var normalizedClubStrength = Math.Clamp(
            (input.SourceClubStrength - profile.MinimumAbility) / (double)abilityRange,
            0.0,
            1.0);
        var sourceClubMultiplier = profile.SourceClubContextMinimum
            + normalizedClubStrength * (profile.SourceClubContextMaximum - profile.SourceClubContextMinimum);
        var marketContextMultiplier = sourceClubMultiplier
            * Math.Clamp(input.LeagueMarketFactor, 0.70, 1.30)
            * Math.Clamp(input.PositionScarcityFactor, 0.85, 1.20)
            * (input.IsTransferListed ? profile.TransferListedMultiplier : 1.0)
            * (input.HasRequestedTransfer ? profile.TransferRequestedMultiplier : 1.0);

        var shortTermMultiplier = 1.0
            + Math.Clamp(input.RecentForm, -1.0, 1.0) * profile.FormImpactLimit
            + ((Math.Clamp(input.Morale, 0.0, 1.0) - 0.5) * 2.0) * profile.MoraleImpactLimit
            + ((Math.Clamp(input.MinutesShare, 0.0, 1.0) - 0.5) * 2.0) * profile.MinutesImpactLimit;

        var rawValue = abilityValue
            * potentialMultiplier
            * ageMultiplier
            * contractMultiplier
            * marketContextMultiplier
            * shortTermMultiplier;
        var marketValue = input.IsFreeAgent
            ? 0
            : RoundTo(
                (int)Math.Round(
                    Math.Clamp(rawValue, profile.MinimumMarketValue, profile.MaximumMarketValue),
                    MidpointRounding.AwayFromZero),
                profile.RoundingStep);

        return new PlayerMarketValuationBreakdown(
            marketValue,
            abilityValue,
            potentialMultiplier,
            ageMultiplier,
            contractMultiplier,
            marketContextMultiplier,
            shortTermMultiplier);
    }

    private static double Interpolate(IReadOnlyList<ValuationCurvePoint> curve, double input)
    {
        if (input <= curve[0].Input)
        {
            return curve[0].Multiplier;
        }

        for (var index = 1; index < curve.Count; index++)
        {
            var upper = curve[index];
            if (input > upper.Input)
            {
                continue;
            }

            var lower = curve[index - 1];
            var progress = (input - lower.Input) / (upper.Input - lower.Input);
            return lower.Multiplier + (upper.Multiplier - lower.Multiplier) * progress;
        }

        return curve[^1].Multiplier;
    }

    private static int RoundTo(int value, int step) =>
        Math.Max(step, (int)Math.Round(value / (double)step, MidpointRounding.AwayFromZero) * step);

    private static void Validate(PlayerValuationBalanceProfile profile)
    {
        if (profile.MaximumAbility <= profile.MinimumAbility
            || profile.MinimumMarketValue <= 0
            || profile.MaximumMarketValue < profile.MinimumMarketValue
            || profile.RoundingStep <= 0
            || profile.AbilityExponent <= 0
            || profile.PotentialPremiumScale < 0
            || !double.IsFinite(profile.AbilityExponent)
            || !double.IsFinite(profile.PotentialPremiumScale)
            || !IsPositiveFinite(profile.SourceClubContextMinimum)
            || !IsPositiveFinite(profile.SourceClubContextMaximum)
            || profile.SourceClubContextMaximum < profile.SourceClubContextMinimum
            || !IsNonNegativeFinite(profile.FormImpactLimit)
            || !IsNonNegativeFinite(profile.MoraleImpactLimit)
            || !IsNonNegativeFinite(profile.MinutesImpactLimit)
            || !IsPositiveFinite(profile.TransferListedMultiplier)
            || !IsPositiveFinite(profile.TransferRequestedMultiplier)
            || !IsPositiveFinite(profile.BuyerOpeningOfferFactor))
        {
            throw new ArgumentException("Player valuation balance profile contains invalid scalar values.", nameof(profile));
        }

        ValidateCurve(profile.AgeMultiplierCurve, nameof(profile.AgeMultiplierCurve));
        ValidateCurve(profile.PotentialWeightCurve, nameof(profile.PotentialWeightCurve));
        ValidateCurve(profile.ContractMultiplierCurve, nameof(profile.ContractMultiplierCurve));
    }

    private static void ValidateInput(PlayerMarketValuationInput input)
    {
        if (!double.IsFinite(input.ContractYearsRemaining)
            || !double.IsFinite(input.LeagueMarketFactor)
            || !double.IsFinite(input.PositionScarcityFactor)
            || !double.IsFinite(input.RecentForm)
            || !double.IsFinite(input.Morale)
            || !double.IsFinite(input.MinutesShare))
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Player valuation inputs must be finite.");
        }
    }

    private static bool IsPositiveFinite(double value) => double.IsFinite(value) && value > 0;

    private static bool IsNonNegativeFinite(double value) => double.IsFinite(value) && value >= 0;

    private static void ValidateCurve(IReadOnlyList<ValuationCurvePoint> curve, string name)
    {
        if (curve.Count == 0)
        {
            throw new ArgumentException($"{name} cannot be empty.");
        }

        for (var index = 0; index < curve.Count; index++)
        {
            if (!double.IsFinite(curve[index].Input)
                || !double.IsFinite(curve[index].Multiplier)
                || curve[index].Multiplier < 0
                || (index > 0 && curve[index].Input <= curve[index - 1].Input))
            {
                throw new ArgumentException($"{name} must contain finite, non-negative multipliers with strictly increasing inputs.");
            }
        }
    }
}
