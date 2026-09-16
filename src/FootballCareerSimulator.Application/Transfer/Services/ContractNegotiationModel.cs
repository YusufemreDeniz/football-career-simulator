namespace FootballCareerSimulator.Application.Transfer.Services;

public enum ContractSquadStatus
{
    None = 0,
    KeyPlayer = 1,
    FirstTeam = 2,
    Rotation = 3,
    Backup = 4,
    HotProspect = 5,
    Youngster = 6,
    NotNeeded = 7
}

public enum ContractNegotiationRejectionReason
{
    ContractTooLong = 0,
    ContractTooShort = 1,
    Wages = 2,
    Bonuses = 3,
    SquadStatus = 4,
    SigningFee = 5,
    Negotiations = 6,
    Insulted = 7,
    OtherOffer = 8,
    TookTooLong = 9,
    Accepted = 10
}

public sealed record ContractOffer(
    int WeeklyWage,
    int SigningFee,
    int ContractYears,
    ContractSquadStatus SquadStatus,
    int SimulatorBonus = 0);

public sealed record PlayerContractDemand(
    int WeeklyWage,
    int DisplayedWeeklyWage,
    int SigningFee,
    int SimulatorBonus,
    int DesiredContractYears,
    int MinimumContractYears,
    int MaximumContractYears,
    ContractSquadStatus DesiredSquadStatus);

public sealed record PlayerContractDemandInput(
    int PlayerId,
    int GameSeed,
    int BaseAcceptableWeeklyWage,
    int PlayerValue,
    int Age,
    int PlayerRating,
    int TargetClubAverageRating,
    int DesiredContractYears,
    int MinimumContractYears,
    ContractSquadStatus DesiredSquadStatus,
    bool IsRenewal = false,
    bool IsFreeAgent = false,
    bool IsTargetUserClub = false,
    double TransferNegotiatorMultiplier = 0.90,
    double SigningFeeTraitPercentage = 0.0,
    int SimulatorBonusDemand = 0,
    int ClubId = 0,
    bool UnwillingToRenew = false,
    double ManagerWageTraitPercentage = 0.0);

public sealed record SigningFeeBreakdown(
    int SigningFee,
    int PlayerFee,
    int CompensationFee,
    double PlayerFeeFactor,
    double StaffMultiplier,
    double TraitMultiplier);

public sealed class ContractNegotiationProfile
{
    public double RequestedWageMinimumMultiplier { get; init; } = 1.00;
    public double RequestedWageMaximumMultiplier { get; init; } = 1.10;
    public double DisplayedWageMinimumMultiplier { get; init; } = 1.10;
    public double DisplayedWageMaximumMultiplier { get; init; } = 1.25;
    public bool EnforceSigningFeeThreshold { get; init; }
    public bool EnforceSimulatorBonusThreshold { get; init; }

    public IReadOnlyList<IReadOnlyList<double>> ContractLengthPatiencePenalties { get; init; } =
    [
        [0.0, 0.5, 1.0, 1.5, 1.5],
        // Native JSON'daki OneYears/OneYear yazım farkı nedeniyle ilk değer fiilen 0,0'dır.
        [0.0, 0.0, 0.5, 1.0, 1.5],
        [1.0, 0.5, 0.0, 0.5, 1.0],
        [1.5, 1.0, 0.5, 0.0, 0.5],
        [1.5, 1.5, 1.0, 0.5, 0.0]
    ];

    public IReadOnlyList<IReadOnlyList<double>> SquadStatusPatiencePenalties { get; init; } =
    [
        [0.0, 1.0, 1.5, 5.0, 5.0, 5.0],
        [0.0, 0.0, 1.0, 1.5, 5.0, 5.0],
        [0.0, 0.0, 0.0, 1.0, 1.5, 5.0],
        [0.0, 0.0, 0.0, 0.0, 1.0, 1.5],
        [0.0, 0.0, 0.0, 0.0, 0.0, 1.0],
        [0.0, 0.0, 0.0, 0.0, 0.0, 0.0]
    ];

    public static ContractNegotiationProfile Sm25NativeParity { get; } = new();

    public static ContractNegotiationProfile SimulatorBalance { get; } = new()
    {
        EnforceSigningFeeThreshold = true,
        EnforceSimulatorBonusThreshold = true
    };
}

public sealed record ContractNegotiationState(
    double StartingPatience,
    double CurrentPatience,
    int Round,
    bool IsTerminated,
    int NativeNegotiationCount = 0)
{
    public static ContractNegotiationState Start(bool isTransferWanted, bool hasActiveConcerns)
    {
        var startingPatience = isTransferWanted ? 0.0 : hasActiveConcerns ? 2.0 : 5.0;
        return new ContractNegotiationState(startingPatience, startingPatience, 0, false);
    }
}

public sealed record ContractNegotiationEvaluation(
    ContractNegotiationRejectionReason Reason,
    ContractNegotiationState State,
    double WagePatiencePenalty,
    double ContractLengthPatiencePenalty,
    double SquadStatusPatiencePenalty)
{
    public bool Accepted => Reason == ContractNegotiationRejectionReason.Accepted;
    public bool CanContinue => !State.IsTerminated;
}

public static class PlayerContractDemandModel
{
    /// <summary>
    /// SM25 Native: LengthMultiplier = 1.0 + (contractLength - 1) / 40.0
    /// VA: 0x02FBFCB4. Her ilave sözleşme yılı haftalık ücret talebini %2.5 artırır.
    /// 1 yıl: 1.000, 2 yıl: 1.025, 3 yıl: 1.050, 4 yıl: 1.075, 5 yıl: 1.100.
    /// </summary>
    public static double CalculateLengthMultiplier(int contractYears)
    {
        var clampedYears = Math.Clamp(contractYears, 1, 5);
        return 1.0 + (clampedYears - 1) / 40.0;
    }

    /// <summary>
    /// SM25 Native: PlayerContract.GenerateNewContractWages (VA: 0x02FBFCD4)
    /// Taban kulüp ücreti, süre çarpanı, menajer özellikleri (HardBargain / ToughNegotiator) ve £50 taban sınırını uygular.
    /// </summary>
    public static int CalculateBaseContractWage(
        int baseWageForClub,
        int contractYears,
        bool isRenewal = false,
        bool isTargetUserClub = false,
        double managerWageTraitPercentage = 0.0)
    {
        var lengthMult = CalculateLengthMultiplier(contractYears);
        var lengthWage = (int)(baseWageForClub * lengthMult);

        // Native: Trait 38 (HardBargain) for transfer (VA: 0x02FBFDF8), Trait 36 (ToughNegotiator) for renewal (VA: 0x02FBFEC4)
        if (isTargetUserClub && Math.Abs(managerWageTraitPercentage) > 1e-6)
        {
            var modifier = 1.0 + (managerWageTraitPercentage / 100.0);
            lengthWage = (int)Math.Max(0, Math.Round(lengthWage * modifier));
        }

        // Native £50 kesin minimum floor (VA: 0x02FBFF48: mov w1, #0x32; bl Math.Max)
        return Math.Max(50, lengthWage);
    }

    /// <summary>
    /// SM25 Native: PlayerData.GetAcceptableNewContractWage (VA: 0x02FC3E9C)
    /// Eğer oyuncu yönetilen kulübe aitse ve sözleşme yenilemek istemiyorsa %50 ceza/artış eklenir:
    /// wage = wage + (wage / 2) (VA: 0x02FC3F60).
    /// </summary>
    public static int CalculateAcceptableWage(
        int baseWageForClub,
        int contractYears,
        bool isRenewal = false,
        bool isTargetUserClub = false,
        bool unwillingToRenew = false,
        double managerWageTraitPercentage = 0.0)
    {
        var wage = CalculateBaseContractWage(
            baseWageForClub,
            contractYears,
            isRenewal,
            isTargetUserClub,
            managerWageTraitPercentage);

        if (isRenewal && unwillingToRenew)
        {
            wage += wage / 2; // +50% native ceza
        }

        return Math.Max(50, wage);
    }

    /// <summary>
    /// SM25 Native: PlayerData.GetRequestedNewContractWage (VA: 0x02FC3F80)
    /// Reddedilme sonrası yeni talep edilen ücret.
    /// Seed = PlayerId + ClubId (VA: 0x02FC3A9C).
    /// Multiplier = RNG.GetRand(1.0, 1.1) (VA: 0x02FC3AC0..0x02FC3AC8).
    /// Yuvarlama: MonetaryHelpers.RoundMonetaryValue (VA: 0x02FC3FA8).
    /// </summary>
    public static int CalculateRequestedWageAfterRejection(
        int acceptableWage,
        int playerId,
        int clubId,
        ContractNegotiationProfile? profile = null)
    {
        profile ??= ContractNegotiationProfile.Sm25NativeParity;
        var seed = unchecked(playerId + clubId);
        var multiplier = NextDouble(
            seed,
            profile.RequestedWageMinimumMultiplier,
            profile.RequestedWageMaximumMultiplier);
        return RoundMonetaryValue(acceptableWage * multiplier);
    }

    /// <summary>
    /// SM25 Native: ContractInfo.GetNegotiationWageForUserManager (VA: 0x03027D28)
    /// UI modal ilk açıldığında ekranda gösterilen talep edilen ücret.
    /// Seed = PlayerId + GameSeed (VA: 0x03027DD4).
    /// Multiplier = RNG.GetRand(1.10, 1.25) (VA: 0x03027DF8..0x03027E08).
    /// Yuvarlama: Extensions.Round(randWage, 100) (VA: 0x03027E1C: mov w0, #0x64).
    /// </summary>
    public static int CalculateDisplayedWage(
        int baseWage,
        int playerId,
        int gameSeed,
        ContractNegotiationProfile? profile = null)
    {
        profile ??= ContractNegotiationProfile.Sm25NativeParity;
        var seed = unchecked(playerId + gameSeed);
        var multiplier = NextDouble(
            seed,
            profile.DisplayedWageMinimumMultiplier,
            profile.DisplayedWageMaximumMultiplier);
        return RoundToStep(baseWage * multiplier, 100);
    }

    public static PlayerContractDemand Generate(
        PlayerContractDemandInput input,
        ContractNegotiationProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        profile ??= ContractNegotiationProfile.Sm25NativeParity;
        Validate(input, profile);

        var seed = unchecked(input.PlayerId + input.GameSeed);
        var requestedMultiplier = NextDouble(
            seed,
            profile.RequestedWageMinimumMultiplier,
            profile.RequestedWageMaximumMultiplier);
        var displayedMultiplier = NextDouble(
            unchecked(seed ^ (int)0x9E3779B9u),
            profile.DisplayedWageMinimumMultiplier,
            profile.DisplayedWageMaximumMultiplier);

        if (input.ManagerWageTraitPercentage > 0.0)
        {
            var discountFactor = Math.Clamp(1.0 - input.ManagerWageTraitPercentage, 0.70, 1.0);
            requestedMultiplier *= discountFactor;
            displayedMultiplier *= discountFactor;
        }

        var signingFee = CalculateSigningFee(input);

        return new PlayerContractDemand(
            RoundMonetaryValue(input.BaseAcceptableWeeklyWage * requestedMultiplier),
            RoundToStep(input.BaseAcceptableWeeklyWage * displayedMultiplier, 100),
            signingFee.SigningFee,
            input.SimulatorBonusDemand,
            input.DesiredContractYears,
            input.MinimumContractYears,
            input.Age < 32 ? 5 : 3,
            input.DesiredSquadStatus);
    }

    public static SigningFeeBreakdown CalculateSigningFee(PlayerContractDemandInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.PlayerValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input.PlayerValue));
        }

        var seed = unchecked(input.PlayerId + input.GameSeed);
        var factor = input.IsRenewal
            ? input.PlayerRating > input.TargetClubAverageRating
                ? NextDouble(seed, 0.05, 0.10)
                : NextDouble(seed, 0.025, 0.05)
            : input.IsFreeAgent
                ? input.Age < 23 ? 0.10 : 0.25
                : 0.10;

        var playerFee = RoundMonetaryValue(input.PlayerValue * factor);
        var staffMultiplier = input.IsTargetUserClub
            ? input.TransferNegotiatorMultiplier
            : 1.0;
        if (input.IsTargetUserClub)
        {
            playerFee = RoundMonetaryValue(playerFee * staffMultiplier);
        }

        var compensationFee = input.IsFreeAgent && input.Age <= 23
            ? RoundMonetaryValue(input.PlayerValue * 0.20)
            : 0;
        var traitMultiplier = input.IsTargetUserClub
            ? Math.Max(0.0, 1.0 + input.SigningFeeTraitPercentage / 100.0)
            : 1.0;
        var total = Math.Max(0.0, (playerFee + compensationFee) * traitMultiplier);

        return new SigningFeeBreakdown(
            (int)Math.Min(int.MaxValue, total),
            playerFee,
            compensationFee,
            factor,
            staffMultiplier,
            traitMultiplier);
    }

    public static double NextDouble(int seed, double minimum, double maximum)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }

        var value = unchecked((uint)seed + 0x9E3779B9u);
        value = (value ^ (value >> 16)) * 0x85EBCA6Bu;
        value = (value ^ (value >> 13)) * 0xC2B2AE35u;
        value ^= value >> 16;
        var unit = value / ((double)uint.MaxValue + 1.0);
        return minimum + (maximum - minimum) * unit;
    }

    public static int RoundMonetaryValue(double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        var step = value switch
        {
            < 250.0 => 10,
            < 1_000.0 => 25,
            < 5_000.0 => 50,
            < 10_000.0 => 100,
            < 50_000.0 => 500,
            < 100_000.0 => 1_000,
            < 5_000_000.0 => 5_000,
            < 10_000_000.0 => 100_000,
            < 50_000_000.0 => 500_000,
            _ => 1_000_000
        };
        return RoundToStep(value, step);
    }

    private static int RoundToStep(double value, int step)
    {
        var rounded = Math.Round(value / step, MidpointRounding.ToEven) * step;
        return (int)Math.Min(int.MaxValue, rounded);
    }

    private static void Validate(
        PlayerContractDemandInput input,
        ContractNegotiationProfile profile)
    {
        if (input.BaseAcceptableWeeklyWage <= 0
            || input.PlayerValue < 0
            || input.Age is < 14 or > 50
            || input.PlayerRating is < 0 or > 100
            || input.TargetClubAverageRating is < 0 or > 100
            || input.DesiredContractYears is < 1 or > 5
            || input.MinimumContractYears is < 1 or > 5
            || input.DesiredContractYears < input.MinimumContractYears
            || input.TransferNegotiatorMultiplier < 0
            || !double.IsFinite(input.TransferNegotiatorMultiplier)
            || input.SigningFeeTraitPercentage < -100
            || !double.IsFinite(input.SigningFeeTraitPercentage)
            || profile.RequestedWageMinimumMultiplier <= 0
            || profile.RequestedWageMaximumMultiplier < profile.RequestedWageMinimumMultiplier
            || profile.DisplayedWageMinimumMultiplier <= 0
            || profile.DisplayedWageMaximumMultiplier < profile.DisplayedWageMinimumMultiplier)
        {
            throw new ArgumentException("Contract demand input or profile is invalid.");
        }
    }
}

public static class ContractOfferEvaluator
{
    public static ContractNegotiationEvaluation Evaluate(
        PlayerContractDemand demand,
        ContractOffer offer,
        ContractNegotiationState state,
        bool isTransferWanted = false,
        bool acceptsOneStepLowerSquadStatus = false,
        ContractNegotiationProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(demand);
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(state);
        profile ??= ContractNegotiationProfile.Sm25NativeParity;
        Validate(demand, offer, state, profile);

        var wagePenalty = WagePatiencePenalty(offer.WeeklyWage, demand.WeeklyWage);
        var lengthPenalty = GetLengthPenalty(
            profile,
            demand.DesiredContractYears,
            offer.ContractYears);
        var statusPenalty = GetStatusPenalty(
            profile,
            demand.DesiredSquadStatus,
            offer.SquadStatus);
        var patience = state.CurrentPatience - 1.0 - wagePenalty - lengthPenalty - statusPenalty;

        var reason = EvaluateReason(
            demand,
            offer,
            isTransferWanted,
            acceptsOneStepLowerSquadStatus,
            profile);
        var terminated = reason is ContractNegotiationRejectionReason.Accepted
            or ContractNegotiationRejectionReason.Insulted
            || patience <= 0.0;
        // ConfirmContractOffer/AddNegotiation her gönderimde +1; TryAccept ret dalında ayrıca +1 yazar.
        var nativeNegotiationCount = state.NativeNegotiationCount
            + (reason == ContractNegotiationRejectionReason.Accepted ? 1 : 2);
        var nextState = state with
        {
            CurrentPatience = patience,
            Round = state.Round + 1,
            IsTerminated = terminated,
            NativeNegotiationCount = nativeNegotiationCount
        };

        return new ContractNegotiationEvaluation(
            reason,
            nextState,
            wagePenalty,
            lengthPenalty,
            statusPenalty);
    }

    private static ContractNegotiationRejectionReason EvaluateReason(
        PlayerContractDemand demand,
        ContractOffer offer,
        bool isTransferWanted,
        bool acceptsOneStepLowerSquadStatus,
        ContractNegotiationProfile profile)
    {
        if (offer.WeeklyWage * 2L <= demand.WeeklyWage)
        {
            return ContractNegotiationRejectionReason.Insulted;
        }

        var wageThreshold = isTransferWanted
            ? demand.WeeklyWage * 2L
            : demand.WeeklyWage;
        if (offer.WeeklyWage < wageThreshold)
        {
            return ContractNegotiationRejectionReason.Wages;
        }

        if (!WillAcceptSquadStatus(
                offer.SquadStatus,
                demand.DesiredSquadStatus,
                acceptsOneStepLowerSquadStatus))
        {
            return ContractNegotiationRejectionReason.SquadStatus;
        }

        if (offer.ContractYears < demand.MinimumContractYears)
        {
            return ContractNegotiationRejectionReason.ContractTooShort;
        }

        if (offer.ContractYears > demand.MaximumContractYears)
        {
            return ContractNegotiationRejectionReason.ContractTooLong;
        }

        // Aşağıdaki iki kapı SM25 ContractInfo içinde yoktur; yalnız simülatör denge profilidir.
        if (profile.EnforceSigningFeeThreshold && offer.SigningFee < demand.SigningFee)
        {
            return ContractNegotiationRejectionReason.SigningFee;
        }

        if (profile.EnforceSimulatorBonusThreshold && offer.SimulatorBonus < demand.SimulatorBonus)
        {
            return ContractNegotiationRejectionReason.Bonuses;
        }

        return ContractNegotiationRejectionReason.Accepted;
    }

    private static bool WillAcceptSquadStatus(
        ContractSquadStatus offered,
        ContractSquadStatus desired,
        bool acceptsOneStepLower)
    {
        if (offered is ContractSquadStatus.None or ContractSquadStatus.NotNeeded
            || desired is ContractSquadStatus.None or ContractSquadStatus.NotNeeded)
        {
            return offered == desired;
        }

        var difference = (int)offered - (int)desired;
        return difference <= 0 || acceptsOneStepLower && difference == 1;
    }

    private static double WagePatiencePenalty(int offered, int demanded)
    {
        var ratio = offered / (double)demanded;
        return ratio >= 1.0 ? 0.0
            : ratio >= 0.75 ? 1.0
            : ratio >= 0.50 ? 1.5
            : 3.0;
    }

    private static double GetLengthPenalty(
        ContractNegotiationProfile profile,
        int desired,
        int offered) => profile.ContractLengthPatiencePenalties[desired - 1][offered - 1];

    private static double GetStatusPenalty(
        ContractNegotiationProfile profile,
        ContractSquadStatus desired,
        ContractSquadStatus offered)
    {
        if (desired is < ContractSquadStatus.KeyPlayer or > ContractSquadStatus.Youngster
            || offered is < ContractSquadStatus.KeyPlayer or > ContractSquadStatus.Youngster)
        {
            return 0.0;
        }

        return profile.SquadStatusPatiencePenalties[(int)desired - 1][(int)offered - 1];
    }

    private static void Validate(
        PlayerContractDemand demand,
        ContractOffer offer,
        ContractNegotiationState state,
        ContractNegotiationProfile profile)
    {
        if (state.IsTerminated)
        {
            throw new InvalidOperationException("The contract negotiation has already terminated.");
        }

        if (demand.WeeklyWage <= 0
            || demand.SigningFee < 0
            || demand.SimulatorBonus < 0
            || demand.DesiredContractYears is < 1 or > 5
            || demand.MinimumContractYears is < 1 or > 5
            || demand.MaximumContractYears is < 1 or > 5
            || demand.MaximumContractYears < demand.MinimumContractYears
            || offer.WeeklyWage < 0
            || offer.SigningFee < 0
            || offer.SimulatorBonus < 0
            || offer.ContractYears is < 1 or > 5
            || profile.ContractLengthPatiencePenalties.Count != 5
            || profile.ContractLengthPatiencePenalties.Any(row => row.Count != 5)
            || profile.SquadStatusPatiencePenalties.Count != 6
            || profile.SquadStatusPatiencePenalties.Any(row => row.Count != 6))
        {
            throw new ArgumentException("Contract demand, offer, state, or profile is invalid.");
        }
    }
}
