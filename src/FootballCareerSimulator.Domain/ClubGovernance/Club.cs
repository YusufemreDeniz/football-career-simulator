namespace FootballCareerSimulator.Domain.ClubGovernance;

using FootballCareerSimulator.Domain.Shared;

/// <summary>
/// Kulüp kimliği + minimal transfer/maaş bütçe rezervasyonu (docs/08 §32).
/// Muhasebe/ledger yok; Transfer context bütçeyi doğrudan yazamaz.
/// </summary>
public sealed class Club
{
    public const int MinSportiveStrength = 1;
    public const int MaxSportiveStrength = 100;
    public const int BaseTransferBudget = 5_000_000;
    public const int TransferBudgetGrowthFactor = 50_000;
    public const int WageBudgetPerStrengthPoint = 5_000;
    public const int MinFacilityLevel = 1;
    public const int MaxFacilityLevel = 5;

    private Club(
        ClubId id,
        string displayName,
        ClubCode code,
        int sportiveStrength,
        int transferBudgetLimit,
        int reservedTransferFunds,
        int spentTransferFunds,
        int wageBudgetLimit,
        int reservedWeeklyWage,
        int facilityLevel = 1)
    {
        Id = id;
        DisplayName = displayName;
        Code = code;
        SportiveStrength = sportiveStrength;
        TransferBudgetLimit = transferBudgetLimit;
        ReservedTransferFunds = reservedTransferFunds;
        SpentTransferFunds = spentTransferFunds;
        WageBudgetLimit = wageBudgetLimit;
        ReservedWeeklyWage = reservedWeeklyWage;
        FacilityLevel = facilityLevel;
    }

    public ClubId Id { get; }

    public string DisplayName { get; }

    public ClubCode Code { get; }

    public int SportiveStrength { get; }

    public int TransferBudgetLimit { get; }

    public int ReservedTransferFunds { get; }

    public int SpentTransferFunds { get; }

    public int AvailableTransferFunds =>
        TransferBudgetLimit - ReservedTransferFunds - SpentTransferFunds;

    public int WageBudgetLimit { get; }

    public int ReservedWeeklyWage { get; }

    public int FacilityLevel { get; }

    /// <summary>
    /// Tesis seviyesine göre antrenman verim çarpanı (1.0x - 1.4x).
    /// </summary>
    public double FacilityTrainingMultiplier => 1.0 + (FacilityLevel - 1) * 0.1;

    /// <summary>
    /// Altyapı tesis seviyesine göre genç oyuncu yetenek tavanı/gelişim bonusu.
    /// </summary>
    public int FacilityYouthBonus => (FacilityLevel - 1) * 2;

    public static int DefaultFacilityLevel(int sportiveStrength) =>
        sportiveStrength switch
        {
            >= 80 => 4,
            >= 70 => 3,
            >= 50 => 2,
            _ => 1,
        };

    public static int FacilityUpgradeCost(int currentLevel) =>
        currentLevel switch
        {
            1 => 2_500_000,
            2 => 5_000_000,
            3 => 10_000_000,
            4 => 20_000_000,
            _ => 0,
        };

    public static int DefaultTransferBudgetLimit(int sportiveStrength)
    {
        var competitiveStrength = Math.Max(0, sportiveStrength - 50);
        return BaseTransferBudget
            + competitiveStrength * competitiveStrength * TransferBudgetGrowthFactor;
    }

    public static int DefaultWageBudgetLimit(int sportiveStrength) =>
        sportiveStrength * WageBudgetPerStrengthPoint;

    public int AvailableWeeklyWageHeadroom(int committedWeeklyWage)
    {
        if (committedWeeklyWage < 0)
        {
            throw new ClubGovernanceInvariantViolationException(
                "Committed weekly wage cannot be negative.");
        }

        return WageBudgetLimit - committedWeeklyWage - ReservedWeeklyWage;
    }

    public Club UpgradeFacilities()
    {
        if (FacilityLevel >= MaxFacilityLevel)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Facilities are already at maximum level ({MaxFacilityLevel}).");
        }

        var cost = FacilityUpgradeCost(FacilityLevel);
        if (AvailableTransferFunds < cost)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Insufficient transfer budget for facility upgrade: need {cost}, available {AvailableTransferFunds}.");
        }

        ValidateTransferFunds(TransferBudgetLimit, ReservedTransferFunds, SpentTransferFunds + cost);
        return new Club(
            Id,
            DisplayName,
            Code,
            SportiveStrength,
            TransferBudgetLimit,
            ReservedTransferFunds,
            SpentTransferFunds + cost,
            WageBudgetLimit,
            ReservedWeeklyWage,
            FacilityLevel + 1);
    }

    public static Club Create(
        ClubId id,
        string displayName,
        ClubCode code,
        int sportiveStrength,
        int? initialFacilityLevel = null)
    {
        ValidateIdentity(displayName, sportiveStrength);
        var facility = initialFacilityLevel ?? DefaultFacilityLevel(sportiveStrength);
        ValidateFacilityLevel(facility);
        return new Club(
            id,
            displayName.Trim(),
            code,
            sportiveStrength,
            DefaultTransferBudgetLimit(sportiveStrength),
            reservedTransferFunds: 0,
            spentTransferFunds: 0,
            DefaultWageBudgetLimit(sportiveStrength),
            reservedWeeklyWage: 0,
            facility);
    }

    public static Club Rehydrate(
        ClubId id,
        string displayName,
        ClubCode code,
        int sportiveStrength,
        int transferBudgetLimit,
        int reservedTransferFunds,
        int spentTransferFunds,
        int wageBudgetLimit,
        int reservedWeeklyWage,
        int facilityLevel = 1)
    {
        ValidateIdentity(displayName, sportiveStrength);
        ValidateTransferFunds(transferBudgetLimit, reservedTransferFunds, spentTransferFunds);
        ValidateWageFunds(wageBudgetLimit, reservedWeeklyWage);
        ValidateFacilityLevel(facilityLevel);
        return new Club(
            id,
            displayName.Trim(),
            code,
            sportiveStrength,
            transferBudgetLimit,
            reservedTransferFunds,
            spentTransferFunds,
            wageBudgetLimit,
            reservedWeeklyWage,
            facilityLevel);
    }

    public Club ReserveTransferFunds(int amount)
    {
        if (amount < 0)
        {
            throw new ClubGovernanceInvariantViolationException("Reserve amount cannot be negative.");
        }

        if (amount == 0)
        {
            return this;
        }

        if (amount > AvailableTransferFunds)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Insufficient transfer budget: need {amount}, available {AvailableTransferFunds}.");
        }

        return WithTransferFunds(ReservedTransferFunds + amount, SpentTransferFunds);
    }

    public Club ReleaseTransferReservation(int amount)
    {
        if (amount < 0)
        {
            throw new ClubGovernanceInvariantViolationException("Release amount cannot be negative.");
        }

        if (amount == 0)
        {
            return this;
        }

        if (amount > ReservedTransferFunds)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Cannot release {amount}; only {ReservedTransferFunds} is reserved.");
        }

        return WithTransferFunds(ReservedTransferFunds - amount, SpentTransferFunds);
    }

    public Club ApplyReservedTransferSpend(int amount)
    {
        if (amount < 0)
        {
            throw new ClubGovernanceInvariantViolationException("Spend amount cannot be negative.");
        }

        if (amount == 0)
        {
            return this;
        }

        if (amount > ReservedTransferFunds)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Cannot apply {amount}; only {ReservedTransferFunds} is reserved.");
        }

        return WithTransferFunds(ReservedTransferFunds - amount, SpentTransferFunds + amount);
    }

    public Club ReserveWeeklyWage(int amount, int committedWeeklyWage)
    {
        if (amount < 0)
        {
            throw new ClubGovernanceInvariantViolationException("Wage reserve amount cannot be negative.");
        }

        if (amount == 0)
        {
            return this;
        }

        var headroom = AvailableWeeklyWageHeadroom(committedWeeklyWage);
        if (amount > headroom)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Insufficient wage budget: need {amount}, available {headroom}.");
        }

        return WithWageReservation(ReservedWeeklyWage + amount);
    }

    public Club ReleaseWeeklyWageReservation(int amount)
    {
        if (amount < 0)
        {
            throw new ClubGovernanceInvariantViolationException("Wage release amount cannot be negative.");
        }

        if (amount == 0)
        {
            return this;
        }

        if (amount > ReservedWeeklyWage)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Cannot release wage {amount}; only {ReservedWeeklyWage} is reserved.");
        }

        return WithWageReservation(ReservedWeeklyWage - amount);
    }

    public Club CreditTransferPrize(int amount)
    {
        if (amount < 0)
        {
            throw new ClubGovernanceInvariantViolationException("Prize amount cannot be negative.");
        }

        return new Club(
            Id,
            DisplayName,
            Code,
            SportiveStrength,
            TransferBudgetLimit + amount,
            ReservedTransferFunds,
            SpentTransferFunds,
            WageBudgetLimit,
            ReservedWeeklyWage,
            FacilityLevel);
    }

    private Club WithTransferFunds(int reserved, int spent)
    {
        ValidateTransferFunds(TransferBudgetLimit, reserved, spent);
        return new Club(
            Id,
            DisplayName,
            Code,
            SportiveStrength,
            TransferBudgetLimit,
            reserved,
            spent,
            WageBudgetLimit,
            ReservedWeeklyWage,
            FacilityLevel);
    }

    private Club WithWageReservation(int reservedWage)
    {
        ValidateWageFunds(WageBudgetLimit, reservedWage);
        return new Club(
            Id,
            DisplayName,
            Code,
            SportiveStrength,
            TransferBudgetLimit,
            ReservedTransferFunds,
            SpentTransferFunds,
            WageBudgetLimit,
            reservedWage,
            FacilityLevel);
    }

    private static void ValidateFacilityLevel(int level)
    {
        if (level is < MinFacilityLevel or > MaxFacilityLevel)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Facility level must be between {MinFacilityLevel} and {MaxFacilityLevel}.");
        }
    }

    private static void ValidateIdentity(string displayName, int sportiveStrength)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ClubGovernanceInvariantViolationException("Club display name cannot be empty.");
        }

        if (sportiveStrength is < MinSportiveStrength or > MaxSportiveStrength)
        {
            throw new ClubGovernanceInvariantViolationException(
                $"Sportive strength must be between {MinSportiveStrength} and {MaxSportiveStrength}.");
        }
    }

    private static void ValidateTransferFunds(int limit, int reserved, int spent)
    {
        if (limit < 0 || reserved < 0 || spent < 0)
        {
            throw new ClubGovernanceInvariantViolationException(
                "Transfer budget values cannot be negative.");
        }

        if (reserved + spent > limit)
        {
            throw new ClubGovernanceInvariantViolationException(
                "Reserved + spent transfer funds cannot exceed the budget limit.");
        }
    }

    private static void ValidateWageFunds(int limit, int reserved)
    {
        if (limit < 0 || reserved < 0)
        {
            throw new ClubGovernanceInvariantViolationException(
                "Wage budget values cannot be negative.");
        }

        if (reserved > limit)
        {
            throw new ClubGovernanceInvariantViolationException(
                "Reserved weekly wage cannot exceed the wage budget limit.");
        }
    }
}
