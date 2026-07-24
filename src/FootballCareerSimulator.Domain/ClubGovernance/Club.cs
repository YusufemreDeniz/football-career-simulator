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
    public const int TransferBudgetPerStrengthPoint = 100_000;
    public const int WageBudgetPerStrengthPoint = 5_000;

    private Club(
        ClubId id,
        string displayName,
        ClubCode code,
        int sportiveStrength,
        int transferBudgetLimit,
        int reservedTransferFunds,
        int spentTransferFunds,
        int wageBudgetLimit,
        int reservedWeeklyWage)
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

    public static int DefaultTransferBudgetLimit(int sportiveStrength) =>
        sportiveStrength * TransferBudgetPerStrengthPoint;

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

    public static Club Create(ClubId id, string displayName, ClubCode code, int sportiveStrength)
    {
        ValidateIdentity(displayName, sportiveStrength);
        return new Club(
            id,
            displayName.Trim(),
            code,
            sportiveStrength,
            DefaultTransferBudgetLimit(sportiveStrength),
            reservedTransferFunds: 0,
            spentTransferFunds: 0,
            DefaultWageBudgetLimit(sportiveStrength),
            reservedWeeklyWage: 0);
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
        int reservedWeeklyWage)
    {
        ValidateIdentity(displayName, sportiveStrength);
        ValidateTransferFunds(transferBudgetLimit, reservedTransferFunds, spentTransferFunds);
        ValidateWageFunds(wageBudgetLimit, reservedWeeklyWage);
        return new Club(
            id,
            displayName.Trim(),
            code,
            sportiveStrength,
            transferBudgetLimit,
            reservedTransferFunds,
            spentTransferFunds,
            wageBudgetLimit,
            reservedWeeklyWage);
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
            ReservedWeeklyWage);
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
            reservedWage);
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
