namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed class PlayerWageBalanceProfile
{
    public int MinimumAbility { get; init; } = 40;
    public int MaximumAbility { get; init; } = 99;
    public int MinimumWeeklyWage { get; init; } = 100;
    public int MaximumWeeklyWage { get; init; } = 1_000_000;
    public double BaseWageReferenceScale { get; init; } = 650_000.0;
    public double AbilityExponent { get; init; } = 10.0;
    public double NonPeakedCurrentAbilityWeight { get; init; } = 0.85;
    public double NonPeakedPotentialWeight { get; init; } = 0.15;
    public double ClubBenchmarkRating { get; init; } = 92.0;
    public double LeagueBenchmarkRating { get; init; } = 88.0;
    public double TeamFitBase { get; init; } = 0.50;
    public double TeamFitWeight { get; init; } = 0.50;
    public double ContractLengthYearScale { get; init; } = 0.025; // +2.5% per additional year beyond 1
    public int RoundingStepSmall { get; init; } = 25;      // For wages < 1,000
    public int RoundingStepMedium { get; init; } = 100;    // For wages < 10,000
    public int RoundingStepLarge { get; init; } = 500;     // For wages < 50,000
    public int RoundingStepElite { get; init; } = 1_000;   // For wages >= 50,000

    public static PlayerWageBalanceProfile Default { get; } = new();
}

public sealed record PlayerWageCalculationInput(
    int CurrentAbility,
    int PotentialAbility,
    int Age,
    int ClubAverageRating,
    int LeagueRating,
    int ContractLengthYears = 3,
    bool IsPeaked = false,
    double NegotiationDiscount = 0.0,
    double SquadRoleFactor = 1.0);

public sealed record PlayerWageBreakdown(
    int WeeklyWage,
    long AnnualWage,
    double BaseWage,
    double TeamFitMultiplier,
    double ClubTierMultiplier,
    double LeagueTierMultiplier,
    double ContractLengthMultiplier,
    double NegotiationDiscountMultiplier);

public static class PlayerWageModel
{
    public static PlayerWageBreakdown Evaluate(
        PlayerWageCalculationInput input,
        PlayerWageBalanceProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        profile ??= PlayerWageBalanceProfile.Default;
        ValidateInput(input);
        ValidateProfile(profile);

        var ability = Math.Clamp(input.CurrentAbility, profile.MinimumAbility, profile.MaximumAbility);
        var potential = Math.Clamp(input.PotentialAbility, ability, profile.MaximumAbility);
        var abilityRange = profile.MaximumAbility - profile.MinimumAbility;

        var normalizedAbility = (ability - profile.MinimumAbility) / (double)abilityRange;
        var normalizedPotential = (potential - profile.MinimumAbility) / (double)abilityRange;

        var abilityFactor = Math.Pow(normalizedAbility, profile.AbilityExponent);
        var potentialFactor = Math.Pow(normalizedPotential, profile.AbilityExponent);

        // Base wage calculation (peaked uses ability only, non-peaked blends ability + potential)
        var compositeFactor = input.IsPeaked
            ? abilityFactor
            : (profile.NonPeakedCurrentAbilityWeight * abilityFactor
               + profile.NonPeakedPotentialWeight * potentialFactor);

        var baseWage = profile.MinimumWeeklyWage
            + (profile.BaseWageReferenceScale - profile.MinimumWeeklyWage) * compositeFactor;

        // Team context & tier modifiers
        var clubRating = Math.Max(profile.MinimumAbility, input.ClubAverageRating);
        var leagueRating = Math.Max(profile.MinimumAbility, input.LeagueRating);

        var teamFit = profile.TeamFitBase
            + profile.TeamFitWeight * ((double)ability / clubRating);

        var clubTier = Math.Clamp(clubRating / profile.ClubBenchmarkRating, 0.40, 1.30);
        var leagueTier = Math.Clamp(leagueRating / profile.LeagueBenchmarkRating, 0.40, 1.30);

        // Contract length modifier (+2.5% per year after year 1)
        var clampedContractYears = Math.Clamp(input.ContractLengthYears, 1, 5);
        var contractLengthMultiplier = 1.0 + (clampedContractYears - 1) * profile.ContractLengthYearScale;

        // Negotiation discounts (manager traits, staff negotiator, role)
        var clampedDiscount = Math.Clamp(input.NegotiationDiscount, 0.0, 0.35);
        var roleFactor = Math.Clamp(input.SquadRoleFactor, 0.70, 1.30);
        var negotiationMultiplier = (1.0 - clampedDiscount) * roleFactor;

        // Raw wage composition
        var rawWage = baseWage
            * teamFit
            * clubTier
            * leagueTier
            * contractLengthMultiplier
            * negotiationMultiplier;

        var clampedWage = Math.Clamp(rawWage, profile.MinimumWeeklyWage, profile.MaximumWeeklyWage);
        var weeklyWage = RoundWage(clampedWage, profile);
        var annualWage = (long)weeklyWage * 52;

        return new PlayerWageBreakdown(
            weeklyWage,
            annualWage,
            baseWage,
            teamFit,
            clubTier,
            leagueTier,
            contractLengthMultiplier,
            negotiationMultiplier);
    }

    public static int RoundWage(double wage, PlayerWageBalanceProfile profile)
    {
        var step = wage switch
        {
            < 1_000 => profile.RoundingStepSmall,
            < 10_000 => profile.RoundingStepMedium,
            < 50_000 => profile.RoundingStepLarge,
            _ => profile.RoundingStepElite
        };

        var rounded = (int)Math.Round(wage / step, MidpointRounding.AwayFromZero) * step;
        return Math.Max(profile.MinimumWeeklyWage, rounded);
    }

    private static void ValidateInput(PlayerWageCalculationInput input)
    {
        if (input.Age < 14 || input.Age > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(input.Age), "Player age must be between 14 and 50.");
        }

        if (!double.IsFinite(input.NegotiationDiscount)
            || !double.IsFinite(input.SquadRoleFactor))
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Player wage calculation inputs must be finite.");
        }
    }

    private static void ValidateProfile(PlayerWageBalanceProfile profile)
    {
        if (profile.MaximumAbility <= profile.MinimumAbility
            || profile.MinimumWeeklyWage <= 0
            || profile.MaximumWeeklyWage < profile.MinimumWeeklyWage
            || profile.BaseWageReferenceScale <= profile.MinimumWeeklyWage
            || profile.AbilityExponent <= 0
            || !double.IsFinite(profile.AbilityExponent)
            || profile.ClubBenchmarkRating <= 0
            || profile.LeagueBenchmarkRating <= 0
            || profile.RoundingStepSmall <= 0
            || profile.RoundingStepMedium <= 0
            || profile.RoundingStepLarge <= 0
            || profile.RoundingStepElite <= 0)
        {
            throw new ArgumentException("Player wage balance profile contains invalid scalar values.", nameof(profile));
        }
    }
}
