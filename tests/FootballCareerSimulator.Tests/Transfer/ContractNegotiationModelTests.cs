using FootballCareerSimulator.Application.Transfer.Services;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class ContractNegotiationModelTests
{
    [Theory]
    [InlineData(9_999, ContractNegotiationRejectionReason.Wages)]
    [InlineData(10_000, ContractNegotiationRejectionReason.Accepted)]
    [InlineData(12_000, ContractNegotiationRejectionReason.Accepted)]
    public void WageThreshold_BelowEqualAndAbove_ReturnExpectedReason(
        int offeredWage,
        ContractNegotiationRejectionReason expected)
    {
        var result = Evaluate(DefaultDemand(), Offer(weeklyWage: offeredWage));

        Assert.Equal(expected, result.Reason);
    }

    [Fact]
    public void WageAtHalfDemand_IsInsultAndTerminatesImmediately()
    {
        var result = Evaluate(DefaultDemand(), Offer(weeklyWage: 5_000));

        Assert.Equal(ContractNegotiationRejectionReason.Insulted, result.Reason);
        Assert.True(result.State.IsTerminated);
    }

    [Fact]
    public void TransferWanted_RenewalRequiresDoubleDemandedWage()
    {
        var demand = DefaultDemand();

        var ordinary = Evaluate(demand, Offer(weeklyWage: 10_000));
        var wantsTransfer = Evaluate(
            demand,
            Offer(weeklyWage: 10_000),
            isTransferWanted: true);
        var retained = Evaluate(
            demand,
            Offer(weeklyWage: 20_000),
            isTransferWanted: true);

        Assert.True(ordinary.Accepted);
        Assert.Equal(ContractNegotiationRejectionReason.Wages, wantsTransfer.Reason);
        Assert.True(retained.Accepted);
    }

    [Theory]
    [InlineData(19_999, ContractNegotiationRejectionReason.SigningFee)]
    [InlineData(20_000, ContractNegotiationRejectionReason.Accepted)]
    [InlineData(25_000, ContractNegotiationRejectionReason.Accepted)]
    public void SimulatorSigningFeeThreshold_BelowEqualAndAbove_ReturnExpectedReason(
        int signingFee,
        ContractNegotiationRejectionReason expected)
    {
        var result = Evaluate(
            DefaultDemand(),
            Offer(signingFee: signingFee),
            profile: ContractNegotiationProfile.SimulatorBalance);

        Assert.Equal(expected, result.Reason);
    }

    [Fact]
    public void NativeParity_DoesNotUseSigningFeeAsAcceptanceGate()
    {
        var result = Evaluate(DefaultDemand(), Offer(signingFee: 0));

        Assert.True(result.Accepted);
    }

    [Fact]
    public void NativeParity_DoesNotUseBonusAsAcceptanceGate()
    {
        var result = Evaluate(DefaultDemand(), Offer(simulatorBonus: 0));

        Assert.True(result.Accepted);
    }

    [Theory]
    [InlineData(4_999, ContractNegotiationRejectionReason.Bonuses)]
    [InlineData(5_000, ContractNegotiationRejectionReason.Accepted)]
    [InlineData(7_500, ContractNegotiationRejectionReason.Accepted)]
    public void SimulatorBonusThreshold_BelowEqualAndAbove_ReturnExpectedReason(
        int bonus,
        ContractNegotiationRejectionReason expected)
    {
        var result = Evaluate(
            DefaultDemand(),
            Offer(signingFee: 20_000, simulatorBonus: bonus),
            profile: ContractNegotiationProfile.SimulatorBalance);

        Assert.Equal(expected, result.Reason);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void ContractLength_NativeBoundariesAreAccepted(int years)
    {
        var demand = DefaultDemand() with
        {
            DesiredContractYears = years,
            MinimumContractYears = 1,
            MaximumContractYears = 5
        };

        var result = Evaluate(demand, Offer(contractYears: years));

        Assert.True(result.Accepted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ContractLength_OutsideRepresentableBoundaryIsRejectedAsInvalidInput(int years)
    {
        Assert.Throws<ArgumentException>(() =>
            Evaluate(DefaultDemand(), Offer(contractYears: years)));
    }

    [Fact]
    public void ContractLength_PlayerMinimumAndAgeMaximumProduceDistinctReasons()
    {
        var demand = DefaultDemand() with
        {
            MinimumContractYears = 2,
            MaximumContractYears = 3
        };

        Assert.Equal(
            ContractNegotiationRejectionReason.ContractTooShort,
            Evaluate(demand, Offer(contractYears: 1)).Reason);
        Assert.Equal(
            ContractNegotiationRejectionReason.ContractTooLong,
            Evaluate(demand, Offer(contractYears: 4)).Reason);
    }

    [Theory]
    [InlineData(ContractSquadStatus.FirstTeam, ContractNegotiationRejectionReason.Accepted)]
    [InlineData(ContractSquadStatus.Rotation, ContractNegotiationRejectionReason.Accepted)]
    [InlineData(ContractSquadStatus.Backup, ContractNegotiationRejectionReason.SquadStatus)]
    public void SquadStatus_HigherEqualAndLowerUseNativeOrdering(
        ContractSquadStatus offered,
        ContractNegotiationRejectionReason expected)
    {
        var result = Evaluate(DefaultDemand(), Offer(squadStatus: offered));

        Assert.Equal(expected, result.Reason);
    }

    [Fact]
    public void SquadStatus_OneStepReductionCanBeAcceptedByPlayerOverride()
    {
        var result = Evaluate(
            DefaultDemand(),
            Offer(squadStatus: ContractSquadStatus.Backup),
            acceptsOneStepLowerSquadStatus: true);

        Assert.True(result.Accepted);
    }

    [Fact]
    public void Patience_IsReducedBeforeEveryOfferAndThirdOrdinaryRejectionTerminates()
    {
        var demand = DefaultDemand();
        var offer = Offer(weeklyWage: 9_000);
        var state = ContractNegotiationState.Start(
            isTransferWanted: false,
            hasActiveConcerns: false);

        var first = Evaluate(demand, offer, state: state);
        var second = Evaluate(demand, offer, state: first.State);
        var third = Evaluate(demand, offer, state: second.State);

        Assert.Equal(3.0, first.State.CurrentPatience);
        Assert.Equal(1.0, second.State.CurrentPatience);
        Assert.Equal(-1.0, third.State.CurrentPatience);
        Assert.False(first.State.IsTerminated);
        Assert.False(second.State.IsTerminated);
        Assert.True(third.State.IsTerminated);
        Assert.Equal(3, third.State.Round);
        Assert.Equal(6, third.State.NativeNegotiationCount);
    }

    [Fact]
    public void StartingPatience_DependsOnTransferWishAndConcerns()
    {
        Assert.Equal(0.0, ContractNegotiationState.Start(true, false).StartingPatience);
        Assert.Equal(2.0, ContractNegotiationState.Start(false, true).StartingPatience);
        Assert.Equal(5.0, ContractNegotiationState.Start(false, false).StartingPatience);
    }

    [Fact]
    public void TerminatedNegotiation_CannotEvaluateAnotherOffer()
    {
        var ended = ContractNegotiationState.Start(false, false) with { IsTerminated = true };

        Assert.Throws<InvalidOperationException>(() =>
            Evaluate(DefaultDemand(), Offer(), state: ended));
    }

    [Fact]
    public void PatienceMatrices_PreserveNativeLengthTypoAndRolePenalty()
    {
        var demand = DefaultDemand() with
        {
            DesiredContractYears = 2,
            DesiredSquadStatus = ContractSquadStatus.KeyPlayer
        };
        var result = Evaluate(
            demand,
            Offer(
                weeklyWage: 9_000,
                contractYears: 1,
                squadStatus: ContractSquadStatus.Backup));

        Assert.Equal(0.0, result.ContractLengthPatiencePenalty);
        Assert.Equal(5.0, result.SquadStatusPatiencePenalty);
    }

    [Fact]
    public void DemandGeneration_SamePlayerGameSeedAndInputIsStable()
    {
        var input = DemandInput(playerId: 42, gameSeed: 700);

        var first = PlayerContractDemandModel.Generate(input);
        var second = PlayerContractDemandModel.Generate(input);

        Assert.Equal(first, second);
    }

    [Fact]
    public void DemandGeneration_DifferentSeedChangesAValidRandomizedResult()
    {
        var first = PlayerContractDemandModel.Generate(DemandInput(42, 700));
        var second = PlayerContractDemandModel.Generate(DemandInput(42, 701));

        Assert.NotEqual(first.DisplayedWeeklyWage, second.DisplayedWeeklyWage);
        Assert.InRange(first.WeeklyWage, 10_000, 11_000);
        Assert.InRange(second.WeeklyWage, 10_000, 11_000);
        Assert.InRange(first.DisplayedWeeklyWage, 11_000, 12_500);
        Assert.InRange(second.DisplayedWeeklyWage, 11_000, 12_500);
    }

    [Fact]
    public void SigningFee_FreeAgentUnder23IncludesTwentyPercentCompensation()
    {
        var breakdown = PlayerContractDemandModel.CalculateSigningFee(
            DemandInput(42, 700) with
            {
                PlayerValue = 1_000_000,
                Age = 22,
                IsFreeAgent = true,
                IsTargetUserClub = false
            });

        Assert.Equal(100_000, breakdown.PlayerFee);
        Assert.Equal(200_000, breakdown.CompensationFee);
        Assert.Equal(300_000, breakdown.SigningFee);
    }

    [Fact]
    public void SigningFee_UserClubAppliesTransferNegotiatorAndTraitSeparately()
    {
        var breakdown = PlayerContractDemandModel.CalculateSigningFee(
            DemandInput(42, 700) with
            {
                PlayerValue = 1_000_000,
                IsTargetUserClub = true,
                TransferNegotiatorMultiplier = 0.90,
                SigningFeeTraitPercentage = -10.0
            });

        Assert.Equal(90_000, breakdown.PlayerFee);
        Assert.Equal(81_000, breakdown.SigningFee);
        Assert.Equal(0.90, breakdown.StaffMultiplier);
        Assert.Equal(0.90, breakdown.TraitMultiplier);
    }

    [Theory]
    [InlineData(1, 1.000)]
    [InlineData(2, 1.025)]
    [InlineData(3, 1.050)]
    [InlineData(4, 1.075)]
    [InlineData(5, 1.100)]
    public void CalculateLengthMultiplier_MatchesNativeArm64Formula(int years, double expected)
    {
        var actual = PlayerContractDemandModel.CalculateLengthMultiplier(years);
        Assert.Equal(expected, actual, precision: 4);
    }

    [Fact]
    public void CalculateBaseContractWage_ClampsToFiftyPoundsMinimum()
    {
        var result = PlayerContractDemandModel.CalculateBaseContractWage(
            baseWageForClub: 10,
            contractYears: 1);

        Assert.Equal(50, result);
    }

    [Fact]
    public void CalculateBaseContractWage_AppliesLengthMultiplierAndTraits()
    {
        // 10,000 base, 5 years (+10%) -> 11,000
        var standard = PlayerContractDemandModel.CalculateBaseContractWage(
            baseWageForClub: 10_000,
            contractYears: 5);
        Assert.Equal(11_000, standard);

        // 10,000 base, 5 years (+10%), -5% HardBargain (Transfer) -> 10,450
        var withTrait = PlayerContractDemandModel.CalculateBaseContractWage(
            baseWageForClub: 10_000,
            contractYears: 5,
            isRenewal: false,
            isTargetUserClub: true,
            managerWageTraitPercentage: -5.0);
        Assert.Equal(10_450, withTrait);
    }

    [Fact]
    public void CalculateAcceptableWage_UnwillingToRenew_AddsFiftyPercentPenalty()
    {
        // 10,000 base, 1 year -> 10,000
        var willing = PlayerContractDemandModel.CalculateAcceptableWage(
            baseWageForClub: 10_000,
            contractYears: 1,
            isRenewal: true,
            unwillingToRenew: false);
        Assert.Equal(10_000, willing);

        // 10,000 base, 1 year, unwilling -> 10,000 + 5,000 = 15,000 (+50%)
        var unwilling = PlayerContractDemandModel.CalculateAcceptableWage(
            baseWageForClub: 10_000,
            contractYears: 1,
            isRenewal: true,
            unwillingToRenew: true);
        Assert.Equal(15_000, unwilling);
    }

    [Fact]
    public void CalculateRequestedWageAfterRejection_DeterministicSeedAndRange()
    {
        var wage = 20_000;
        var first = PlayerContractDemandModel.CalculateRequestedWageAfterRejection(wage, playerId: 101, clubId: 5);
        var second = PlayerContractDemandModel.CalculateRequestedWageAfterRejection(wage, playerId: 101, clubId: 5);

        // Same inputs must produce exact same result (seeded RNG parity)
        Assert.Equal(first, second);
        Assert.InRange(first, 20_000, 22_000); // 1.0 to 1.1x
    }

    [Fact]
    public void CalculateDisplayedWage_DeterministicSeedAndStepRounding()
    {
        var baseWage = 20_000;
        var displayed = PlayerContractDemandModel.CalculateDisplayedWage(baseWage, playerId: 101, gameSeed: 999);

        // Native range is 1.10x to 1.25x
        Assert.InRange(displayed, 22_000, 25_000);
        // Must be rounded to nearest 100 (Extensions.Round(..., 100))
        Assert.Equal(0, displayed % 100);
    }

    [Fact]
    public void DemandedWage_RepeatedRejectionWithoutChangingParameters_RemainsStable()
    {
        var acceptable = 15_000;
        var demand1 = PlayerContractDemandModel.CalculateRequestedWageAfterRejection(acceptable, 77, 12);
        var demand2 = PlayerContractDemandModel.CalculateRequestedWageAfterRejection(acceptable, 77, 12);

        Assert.Equal(demand1, demand2);
    }

    [Fact]
    public void DemandedWage_IncreasesWhenContractLengthIncreases()
    {
        var wage1Yr = PlayerContractDemandModel.CalculateAcceptableWage(10_000, 1);
        var wage3Yr = PlayerContractDemandModel.CalculateAcceptableWage(10_000, 3);
        var wage5Yr = PlayerContractDemandModel.CalculateAcceptableWage(10_000, 5);

        Assert.True(wage1Yr < wage3Yr);
        Assert.True(wage3Yr < wage5Yr);
        Assert.Equal(10_000, wage1Yr);
        Assert.Equal(10_500, wage3Yr);
        Assert.Equal(11_000, wage5Yr);
    }

    private static ContractNegotiationEvaluation Evaluate(
        PlayerContractDemand demand,
        ContractOffer offer,
        bool isTransferWanted = false,
        bool acceptsOneStepLowerSquadStatus = false,
        ContractNegotiationProfile? profile = null,
        ContractNegotiationState? state = null) => ContractOfferEvaluator.Evaluate(
            demand,
            offer,
            state ?? ContractNegotiationState.Start(false, false),
            isTransferWanted,
            acceptsOneStepLowerSquadStatus,
            profile);

    private static PlayerContractDemand DefaultDemand() => new(
        WeeklyWage: 10_000,
        DisplayedWeeklyWage: 12_000,
        SigningFee: 20_000,
        SimulatorBonus: 5_000,
        DesiredContractYears: 3,
        MinimumContractYears: 1,
        MaximumContractYears: 5,
        DesiredSquadStatus: ContractSquadStatus.Rotation);

    private static ContractOffer Offer(
        int weeklyWage = 10_000,
        int signingFee = 20_000,
        int contractYears = 3,
        ContractSquadStatus squadStatus = ContractSquadStatus.Rotation,
        int simulatorBonus = 5_000) => new(
            weeklyWage,
            signingFee,
            contractYears,
            squadStatus,
            simulatorBonus);

    private static PlayerContractDemandInput DemandInput(int playerId, int gameSeed) => new(
        playerId,
        gameSeed,
        BaseAcceptableWeeklyWage: 10_000,
        PlayerValue: 1_000_000,
        Age: 26,
        PlayerRating: 75,
        TargetClubAverageRating: 72,
        DesiredContractYears: 3,
        MinimumContractYears: 1,
        DesiredSquadStatus: ContractSquadStatus.Rotation);

    [Fact]
    public void ManagerNegotiationSkill_DiscountsDemandedWage()
    {
        var inputWithoutSkill = new PlayerContractDemandInput(
            PlayerId: 101,
            GameSeed: 42,
            BaseAcceptableWeeklyWage: 20_000,
            PlayerValue: 1_000_000,
            Age: 25,
            PlayerRating: 75,
            TargetClubAverageRating: 75,
            DesiredContractYears: 3,
            MinimumContractYears: 1,
            DesiredSquadStatus: ContractSquadStatus.FirstTeam,
            ManagerWageTraitPercentage: 0.0);

        var inputWithSkill = inputWithoutSkill with { ManagerWageTraitPercentage = 0.15 };

        var demandStandard = PlayerContractDemandModel.Generate(inputWithoutSkill);
        var demandDiscounted = PlayerContractDemandModel.Generate(inputWithSkill);

        Assert.True(demandDiscounted.WeeklyWage < demandStandard.WeeklyWage,
            $"Expected discounted wage ({demandDiscounted.WeeklyWage}) < standard ({demandStandard.WeeklyWage})");
    }
}
