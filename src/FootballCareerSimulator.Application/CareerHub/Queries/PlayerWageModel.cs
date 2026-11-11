namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed class PlayerWageBalanceProfile
{
    // SM25 native tabanı £50'dir; üretim profili aşağıda bilinçli bir simülatör denge tabanı kullanır.
    public const int Sm25NativeMinimumWeeklyWage = 50;
    public int MinimumAbility { get; init; } = 40;
    public int MaximumAbility { get; init; } = 99;
    public int MinimumWeeklyWage { get; init; } = 100;
    // SM25 native zincirinde global üst tavan yoktur; bu değer simülatör güvenlik/denge kuralıdır.
    public int MaximumWeeklyWage { get; init; } = 1_000_000;
    public double BaseWageReferenceScale { get; init; } = 650_000.0;
    public double AbilityExponent { get; init; } = 10.0;
    public double NonPeakedCurrentAbilityWeight { get; init; } = 0.85;
    public double NonPeakedPotentialWeight { get; init; } = 0.15;
    public double ClubBenchmarkRating { get; init; } = 92.0;
    public double LeagueBenchmarkRating { get; init; } = 88.0;
    public double TeamFitBase { get; init; } = 0.50;
    public double TeamFitWeight { get; init; } = 0.50;
    public double MinimumTierMultiplier { get; init; } = 0.40;
    public double MaximumTierMultiplier { get; init; } = 1.30;
    public double ContractLengthYearScale { get; init; } = 0.025; // +2.5% per additional year beyond 1
    public int MinimumContractYears { get; init; } = 1;
    public int MaximumContractYears { get; init; } = 5;
    public double MinimumNegotiationPercentage { get; init; } = -35.0;
    public double MaximumNegotiationPercentage { get; init; } = 35.0;
    public double MinimumSquadRoleFactor { get; init; } = 0.70;
    public double MaximumSquadRoleFactor { get; init; } = 1.30;
    public double MinimumMarketStatusFactor { get; init; } = 0.80;
    public double MaximumMarketStatusFactor { get; init; } = 1.25;
    public int SuggestedContractYears { get; init; } = 3;
    public int SuggestedPeakedAge { get; init; } = 30;
    public double KeyPlayerRoleFactor { get; init; } = 1.25;
    public double StartingRoleFactor { get; init; } = 1.10;
    public double RotationRoleFactor { get; init; } = 1.00;
    public double ProspectRoleFactor { get; init; } = 0.80;
    public double FreeAgentMarketStatusFactor { get; init; } = 1.20;
    public int RoundingThresholdSmall { get; init; } = 1_000;
    public int RoundingThresholdMedium { get; init; } = 10_000;
    public int RoundingThresholdLarge { get; init; } = 50_000;
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
    double ManagerTraitPercentage = 0.0,
    double StaffNegotiatorPercentage = 0.0,
    double SquadRoleFactor = 1.0,
    double MarketStatusFactor = 1.0);

public sealed record PlayerWageBreakdown(
    int WeeklyWage,
    long AnnualWage,
    double RawWeeklyWage,
    int RoundedWeeklyWage,
    bool MinimumApplied,
    bool MaximumApplied,
    double BaseWage,
    double TeamFitMultiplier,
    double ClubTierMultiplier,
    double LeagueTierMultiplier,
    double ContractLengthMultiplier,
    double ManagerTraitMultiplier,
    double StaffNegotiatorMultiplier,
    double SquadRoleMultiplier,
    double MarketStatusMultiplier);

public static class PlayerWageModel
{
    /// <summary>
    /// Sözleşme kabul eşiğinin ve scout ekranındaki önerinin ortak tabanı: kadro rolü nötr (rotasyon),
    /// kulüp ve lig gücü olarak alıcı kulübün sportif gücü kullanılır. Böylece arayüzün önerdiği ücret
    /// hiçbir zaman oyuncunun kabul eşiğinin altında kalmaz.
    /// </summary>
    public static PlayerWageBreakdown EvaluateContractBasis(
        int currentAbility,
        int potentialAbility,
        int age,
        int buyingClubStrength,
        int contractYears,
        bool isFreeAgent,
        PlayerWageBalanceProfile? profile = null)
    {
        profile ??= PlayerWageBalanceProfile.Default;
        return Evaluate(
            new PlayerWageCalculationInput(
                currentAbility,
                potentialAbility,
                age,
                buyingClubStrength,
                buyingClubStrength,
                ContractLengthYears: contractYears,
                IsPeaked: age >= profile.SuggestedPeakedAge || potentialAbility <= currentAbility,
                SquadRoleFactor: profile.RotationRoleFactor,
                MarketStatusFactor: isFreeAgent ? profile.FreeAgentMarketStatusFactor : 1.0),
            profile);
    }

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

        var clubTier = Math.Clamp(
            clubRating / profile.ClubBenchmarkRating,
            profile.MinimumTierMultiplier,
            profile.MaximumTierMultiplier);
        var leagueTier = Math.Clamp(
            leagueRating / profile.LeagueBenchmarkRating,
            profile.MinimumTierMultiplier,
            profile.MaximumTierMultiplier);

        // Contract length modifier (+2.5% per year after year 1)
        var clampedContractYears = Math.Clamp(
            input.ContractLengthYears,
            profile.MinimumContractYears,
            profile.MaximumContractYears);
        var contractLengthMultiplier = 1.0
            + (clampedContractYears - profile.MinimumContractYears) * profile.ContractLengthYearScale;

        // Negotiation and contract-context modifiers are explicit so callers can audit each source.
        var managerTraitPercentage = Math.Clamp(
            input.ManagerTraitPercentage,
            profile.MinimumNegotiationPercentage,
            profile.MaximumNegotiationPercentage);
        var staffNegotiatorPercentage = Math.Clamp(
            input.StaffNegotiatorPercentage,
            profile.MinimumNegotiationPercentage,
            profile.MaximumNegotiationPercentage);
        var managerTraitMultiplier = 1.0 + managerTraitPercentage / 100.0;
        var staffNegotiatorMultiplier = 1.0 + staffNegotiatorPercentage / 100.0;
        var squadRoleMultiplier = Math.Clamp(
            input.SquadRoleFactor,
            profile.MinimumSquadRoleFactor,
            profile.MaximumSquadRoleFactor);
        var marketStatusMultiplier = Math.Clamp(
            input.MarketStatusFactor,
            profile.MinimumMarketStatusFactor,
            profile.MaximumMarketStatusFactor);

        // Raw wage composition
        var rawWage = baseWage
            * teamFit
            * clubTier
            * leagueTier
            * contractLengthMultiplier
            * managerTraitMultiplier
            * staffNegotiatorMultiplier
            * squadRoleMultiplier
            * marketStatusMultiplier;

        var roundedWage = RoundWage(rawWage, profile);
        var weeklyWage = Math.Clamp(
            roundedWage,
            profile.MinimumWeeklyWage,
            profile.MaximumWeeklyWage);
        var annualWage = (long)weeklyWage * 52;

        return new PlayerWageBreakdown(
            weeklyWage,
            annualWage,
            rawWage,
            roundedWage,
            roundedWage < profile.MinimumWeeklyWage,
            roundedWage > profile.MaximumWeeklyWage,
            baseWage,
            teamFit,
            clubTier,
            leagueTier,
            contractLengthMultiplier,
            managerTraitMultiplier,
            staffNegotiatorMultiplier,
            squadRoleMultiplier,
            marketStatusMultiplier);
    }

    public static int RoundWage(double wage, PlayerWageBalanceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ValidateProfile(profile);

        var step = wage switch
        {
            < 0 => throw new ArgumentOutOfRangeException(nameof(wage)),
            _ when !double.IsFinite(wage) => throw new ArgumentOutOfRangeException(nameof(wage)),
            _ when wage < profile.RoundingThresholdSmall => profile.RoundingStepSmall,
            _ when wage < profile.RoundingThresholdMedium => profile.RoundingStepMedium,
            _ when wage < profile.RoundingThresholdLarge => profile.RoundingStepLarge,
            _ => profile.RoundingStepElite
        };

        var rounded = Math.Round(wage / step, MidpointRounding.AwayFromZero) * step;
        return (int)Math.Min(int.MaxValue, rounded);
    }

    private static void ValidateInput(PlayerWageCalculationInput input)
    {
        if (input.Age < 14 || input.Age > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(input.Age), "Player age must be between 14 and 50.");
        }

        if (!double.IsFinite(input.ManagerTraitPercentage)
            || !double.IsFinite(input.StaffNegotiatorPercentage)
            || !double.IsFinite(input.SquadRoleFactor)
            || !double.IsFinite(input.MarketStatusFactor))
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
            || !double.IsFinite(profile.BaseWageReferenceScale)
            || profile.AbilityExponent <= 0
            || !double.IsFinite(profile.AbilityExponent)
            || profile.NonPeakedCurrentAbilityWeight < 0
            || profile.NonPeakedPotentialWeight < 0
            || !double.IsFinite(profile.NonPeakedCurrentAbilityWeight)
            || !double.IsFinite(profile.NonPeakedPotentialWeight)
            || Math.Abs(
                profile.NonPeakedCurrentAbilityWeight
                + profile.NonPeakedPotentialWeight
                - 1.0) > 0.000001
            || profile.ClubBenchmarkRating <= 0
            || profile.LeagueBenchmarkRating <= 0
            || !double.IsFinite(profile.ClubBenchmarkRating)
            || !double.IsFinite(profile.LeagueBenchmarkRating)
            || profile.TeamFitBase < 0
            || profile.TeamFitWeight < 0
            || !double.IsFinite(profile.TeamFitBase)
            || !double.IsFinite(profile.TeamFitWeight)
            || profile.MinimumTierMultiplier <= 0
            || profile.MaximumTierMultiplier < profile.MinimumTierMultiplier
            || !double.IsFinite(profile.MinimumTierMultiplier)
            || !double.IsFinite(profile.MaximumTierMultiplier)
            || profile.ContractLengthYearScale < 0
            || !double.IsFinite(profile.ContractLengthYearScale)
            || profile.MinimumContractYears <= 0
            || profile.MaximumContractYears < profile.MinimumContractYears
            || profile.MinimumNegotiationPercentage <= -100
            || profile.MaximumNegotiationPercentage < profile.MinimumNegotiationPercentage
            || !double.IsFinite(profile.MinimumNegotiationPercentage)
            || !double.IsFinite(profile.MaximumNegotiationPercentage)
            || profile.MinimumSquadRoleFactor <= 0
            || profile.MaximumSquadRoleFactor < profile.MinimumSquadRoleFactor
            || profile.MinimumMarketStatusFactor <= 0
            || profile.MaximumMarketStatusFactor < profile.MinimumMarketStatusFactor
            || !double.IsFinite(profile.MinimumSquadRoleFactor)
            || !double.IsFinite(profile.MaximumSquadRoleFactor)
            || !double.IsFinite(profile.MinimumMarketStatusFactor)
            || !double.IsFinite(profile.MaximumMarketStatusFactor)
            || profile.SuggestedContractYears < profile.MinimumContractYears
            || profile.SuggestedContractYears > profile.MaximumContractYears
            || profile.SuggestedPeakedAge < 14
            || profile.SuggestedPeakedAge > 50
            || profile.KeyPlayerRoleFactor < profile.MinimumSquadRoleFactor
            || profile.KeyPlayerRoleFactor > profile.MaximumSquadRoleFactor
            || profile.StartingRoleFactor < profile.MinimumSquadRoleFactor
            || profile.StartingRoleFactor > profile.MaximumSquadRoleFactor
            || profile.RotationRoleFactor < profile.MinimumSquadRoleFactor
            || profile.RotationRoleFactor > profile.MaximumSquadRoleFactor
            || profile.ProspectRoleFactor < profile.MinimumSquadRoleFactor
            || profile.ProspectRoleFactor > profile.MaximumSquadRoleFactor
            || profile.FreeAgentMarketStatusFactor < profile.MinimumMarketStatusFactor
            || profile.FreeAgentMarketStatusFactor > profile.MaximumMarketStatusFactor
            || !double.IsFinite(profile.KeyPlayerRoleFactor)
            || !double.IsFinite(profile.StartingRoleFactor)
            || !double.IsFinite(profile.RotationRoleFactor)
            || !double.IsFinite(profile.ProspectRoleFactor)
            || !double.IsFinite(profile.FreeAgentMarketStatusFactor)
            || profile.RoundingThresholdSmall <= 0
            || profile.RoundingThresholdMedium <= profile.RoundingThresholdSmall
            || profile.RoundingThresholdLarge <= profile.RoundingThresholdMedium
            || profile.RoundingStepSmall <= 0
            || profile.RoundingStepMedium <= 0
            || profile.RoundingStepLarge <= 0
            || profile.RoundingStepElite <= 0)
        {
            throw new ArgumentException("Player wage balance profile contains invalid scalar values.", nameof(profile));
        }
    }
}
