using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// Futbolcuya sunulan sözleşme teklifi (iskelet — aktivasyon / Financial Approval yok).
/// </summary>
public sealed class PlayerContractProposal
{
    public const int MinWeeklyWage = 1;
    public const int MaxWeeklyWage = 500_000;
    public const int MinContractYears = 1;
    public const int MaxContractYears = 5;

    private PlayerContractProposal(
        PlayerContractProposalId proposalId,
        TransferProcessId processId,
        int round,
        int weeklyWage,
        int contractYears,
        PlayerContractProposalStatus status,
        GameDate submittedOn)
    {
        ProposalId = proposalId;
        ProcessId = processId;
        Round = round;
        WeeklyWage = weeklyWage;
        ContractYears = contractYears;
        Status = status;
        SubmittedOn = submittedOn;
    }

    public PlayerContractProposalId ProposalId { get; }

    public TransferProcessId ProcessId { get; }

    public int Round { get; }

    public int WeeklyWage { get; }

    public int ContractYears { get; }

    public PlayerContractProposalStatus Status { get; }

    public GameDate SubmittedOn { get; }

    public bool IsPending => Status == PlayerContractProposalStatus.Pending;

    public static PlayerContractProposal Submit(
        PlayerContractProposalId proposalId,
        TransferProcessId processId,
        int round,
        int weeklyWage,
        int contractYears,
        GameDate day)
    {
        Validate(round, weeklyWage, contractYears);
        return new PlayerContractProposal(
            proposalId,
            processId,
            round,
            weeklyWage,
            contractYears,
            PlayerContractProposalStatus.Pending,
            day);
    }

    public static PlayerContractProposal Rehydrate(
        PlayerContractProposalId proposalId,
        TransferProcessId processId,
        int round,
        int weeklyWage,
        int contractYears,
        PlayerContractProposalStatus status,
        GameDate submittedOn)
    {
        Validate(round, weeklyWage, contractYears);
        if (!Enum.IsDefined(status))
        {
            throw new TransferInvariantViolationException(
                $"Unknown player contract proposal status: {status}.");
        }

        return new PlayerContractProposal(
            proposalId,
            processId,
            round,
            weeklyWage,
            contractYears,
            status,
            submittedOn);
    }

    public PlayerContractProposal Accept() =>
        Status == PlayerContractProposalStatus.Accepted
            ? this
            : EnsurePendingTransition(PlayerContractProposalStatus.Accepted);

    public PlayerContractProposal Reject() =>
        Status == PlayerContractProposalStatus.Rejected
            ? this
            : EnsurePendingTransition(PlayerContractProposalStatus.Rejected);

    public PlayerContractProposal Supersede() =>
        Status == PlayerContractProposalStatus.Superseded
            ? this
            : EnsurePendingTransition(PlayerContractProposalStatus.Superseded);

    private PlayerContractProposal EnsurePendingTransition(PlayerContractProposalStatus next)
    {
        if (Status != PlayerContractProposalStatus.Pending)
        {
            throw new TransferInvariantViolationException(
                $"Proposal #{ProposalId.Value} is {Status} and cannot become {next}.");
        }

        return new PlayerContractProposal(
            ProposalId,
            ProcessId,
            Round,
            WeeklyWage,
            ContractYears,
            next,
            SubmittedOn);
    }

    private static void Validate(int round, int weeklyWage, int contractYears)
    {
        if (round <= 0)
        {
            throw new TransferInvariantViolationException("Proposal round must be positive.");
        }

        if (weeklyWage is < MinWeeklyWage or > MaxWeeklyWage)
        {
            throw new TransferInvariantViolationException(
                $"Weekly wage must be between {MinWeeklyWage} and {MaxWeeklyWage}.");
        }

        if (contractYears is < MinContractYears or > MaxContractYears)
        {
            throw new TransferInvariantViolationException(
                $"Contract years must be between {MinContractYears} and {MaxContractYears}.");
        }
    }
}
