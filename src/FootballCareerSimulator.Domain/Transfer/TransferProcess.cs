using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// Aktif transfer süreci iskeleti. Offer / Approval / Completion içermez.
/// </summary>
public sealed class TransferProcess
{
    private TransferProcess(
        TransferProcessId processId,
        TransferNeedId needId,
        TransferTargetId targetId,
        ClubId buyingClubId,
        PlayerId playerId,
        ClubId? sellingClubId,
        bool isFreeAgent,
        TransferProcessStatus status,
        string? failureReasonCode,
        GameDate openedOn,
        GameDate? terminalOn)
    {
        ProcessId = processId;
        NeedId = needId;
        TargetId = targetId;
        BuyingClubId = buyingClubId;
        PlayerId = playerId;
        SellingClubId = sellingClubId;
        IsFreeAgent = isFreeAgent;
        Status = status;
        FailureReasonCode = failureReasonCode;
        OpenedOn = openedOn;
        TerminalOn = terminalOn;
    }

    public TransferProcessId ProcessId { get; }

    public TransferNeedId NeedId { get; }

    public TransferTargetId TargetId { get; }

    public ClubId BuyingClubId { get; }

    public PlayerId PlayerId { get; }

    public ClubId? SellingClubId { get; }

    public bool IsFreeAgent { get; }

    public TransferProcessStatus Status { get; }

    public string? FailureReasonCode { get; }

    public GameDate OpenedOn { get; }

    public GameDate? TerminalOn { get; }

    public bool IsActive => Status == TransferProcessStatus.UnderEvaluation;

    public bool IsTerminal =>
        Status is TransferProcessStatus.Withdrawn
            or TransferProcessStatus.Failed
            or TransferProcessStatus.Archived;

    public static TransferProcess OpenFromTarget(
        TransferProcessId processId,
        TransferNeedId needId,
        TransferTargetId targetId,
        ClubId buyingClubId,
        PlayerId playerId,
        ClubId? sellingClubId,
        bool isFreeAgent,
        GameDate day)
    {
        if (isFreeAgent && sellingClubId is not null)
        {
            throw new TransferInvariantViolationException(
                "Free-agent process cannot have a selling club.");
        }

        if (!isFreeAgent && sellingClubId is null)
        {
            throw new TransferInvariantViolationException(
                "Club-to-club process requires a selling club.");
        }

        return new TransferProcess(
            processId,
            needId,
            targetId,
            buyingClubId,
            playerId,
            sellingClubId,
            isFreeAgent,
            TransferProcessStatus.UnderEvaluation,
            failureReasonCode: null,
            day,
            terminalOn: null);
    }

    public static TransferProcess Rehydrate(
        TransferProcessId processId,
        TransferNeedId needId,
        TransferTargetId targetId,
        ClubId buyingClubId,
        PlayerId playerId,
        ClubId? sellingClubId,
        bool isFreeAgent,
        TransferProcessStatus status,
        string? failureReasonCode,
        GameDate openedOn,
        GameDate? terminalOn)
    {
        if (!Enum.IsDefined(status))
        {
            throw new TransferInvariantViolationException($"Unknown transfer process status: {status}.");
        }

        if (status == TransferProcessStatus.UnderEvaluation && terminalOn is not null)
        {
            throw new TransferInvariantViolationException("Active process cannot have TerminalOn.");
        }

        if (status != TransferProcessStatus.UnderEvaluation && terminalOn is null)
        {
            throw new TransferInvariantViolationException("Terminal process requires TerminalOn.");
        }

        if (status == TransferProcessStatus.Failed && string.IsNullOrWhiteSpace(failureReasonCode))
        {
            throw new TransferInvariantViolationException("Failed process requires FailureReasonCode.");
        }

        return new TransferProcess(
            processId,
            needId,
            targetId,
            buyingClubId,
            playerId,
            sellingClubId,
            isFreeAgent,
            status,
            failureReasonCode,
            openedOn,
            terminalOn);
    }

    public TransferProcess Withdraw(GameDate day)
    {
        EnsureActive();
        return new TransferProcess(
            ProcessId,
            NeedId,
            TargetId,
            BuyingClubId,
            PlayerId,
            SellingClubId,
            IsFreeAgent,
            TransferProcessStatus.Withdrawn,
            FailureReasonCode,
            OpenedOn,
            day);
    }

    public TransferProcess Fail(string reasonCode, GameDate day)
    {
        EnsureActive();
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new TransferInvariantViolationException("Failure reason is required.");
        }

        if (reasonCode.Length > 64)
        {
            throw new TransferInvariantViolationException("Failure reason max length is 64.");
        }

        return new TransferProcess(
            ProcessId,
            NeedId,
            TargetId,
            BuyingClubId,
            PlayerId,
            SellingClubId,
            IsFreeAgent,
            TransferProcessStatus.Failed,
            reasonCode.Trim(),
            OpenedOn,
            day);
    }

    public TransferProcess Archive(GameDate day)
    {
        if (Status == TransferProcessStatus.Archived)
        {
            return this;
        }

        if (Status == TransferProcessStatus.UnderEvaluation)
        {
            throw new TransferInvariantViolationException(
                "Active process cannot be archived; withdraw or fail first.");
        }

        return new TransferProcess(
            ProcessId,
            NeedId,
            TargetId,
            BuyingClubId,
            PlayerId,
            SellingClubId,
            IsFreeAgent,
            TransferProcessStatus.Archived,
            FailureReasonCode,
            OpenedOn,
            day);
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new TransferInvariantViolationException(
                $"Process #{ProcessId.Value} is terminal ({Status}) and cannot transition.");
        }
    }
}
