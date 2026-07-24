namespace FootballCareerSimulator.Domain.ClubGovernance;

using FootballCareerSimulator.Domain.Shared;

/// <summary>
/// Kulüp kimliği + minimal transfer bütçe rezervasyonu (docs/08 §32).
/// Muhasebe/ledger yok; Transfer context bütçeyi doğrudan yazamaz.
/// </summary>
public sealed class Club
{
    public const int MinSportiveStrength = 1;
    public const int MaxSportiveStrength = 100;
    public const int TransferBudgetPerStrengthPoint = 100_000;

    private Club(
        ClubId id,
        string displayName,
        ClubCode code,
        int sportiveStrength,
        int transferBudgetLimit,
        int reservedTransferFunds,
        int spentTransferFunds)
    {
        Id = id;
        DisplayName = displayName;
        Code = code;
        SportiveStrength = sportiveStrength;
        TransferBudgetLimit = transferBudgetLimit;
        ReservedTransferFunds = reservedTransferFunds;
        SpentTransferFunds = spentTransferFunds;
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

    public static int DefaultTransferBudgetLimit(int sportiveStrength) =>
        sportiveStrength * TransferBudgetPerStrengthPoint;

    public static Club Create(ClubId id, string displayName, ClubCode code, int sportiveStrength)
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

        return new Club(
            id,
            displayName.Trim(),
            code,
            sportiveStrength,
            DefaultTransferBudgetLimit(sportiveStrength),
            reservedTransferFunds: 0,
            spentTransferFunds: 0);
    }

    public static Club Rehydrate(
        ClubId id,
        string displayName,
        ClubCode code,
        int sportiveStrength,
        int transferBudgetLimit,
        int reservedTransferFunds,
        int spentTransferFunds)
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

        ValidateFunds(transferBudgetLimit, reservedTransferFunds, spentTransferFunds);
        return new Club(
            id,
            displayName.Trim(),
            code,
            sportiveStrength,
            transferBudgetLimit,
            reservedTransferFunds,
            spentTransferFunds);
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

        return WithFunds(ReservedTransferFunds + amount, SpentTransferFunds);
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

        return WithFunds(ReservedTransferFunds - amount, SpentTransferFunds);
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

        return WithFunds(ReservedTransferFunds - amount, SpentTransferFunds + amount);
    }

    private Club WithFunds(int reserved, int spent)
    {
        ValidateFunds(TransferBudgetLimit, reserved, spent);
        return new Club(
            Id,
            DisplayName,
            Code,
            SportiveStrength,
            TransferBudgetLimit,
            reserved,
            spent);
    }

    private static void ValidateFunds(int limit, int reserved, int spent)
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
}
