using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// Transfer süreci: Sporting Approval + kulüp müzakeresi iskeleti. Financial / Completion yok.
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

    public bool IsActive => IsActiveStatus(Status);

    public bool IsTerminal => !IsActive;

    public bool AwaitsSportingDecision => Status == TransferProcessStatus.SportingApprovalPending;

    public bool HasSportingApproval =>
        Status is TransferProcessStatus.SportingApproved
            or TransferProcessStatus.ClubNegotiation
            or TransferProcessStatus.ClubAgreementReached;

    public bool IsInClubNegotiation => Status == TransferProcessStatus.ClubNegotiation;

    public bool HasClubAgreement => Status == TransferProcessStatus.ClubAgreementReached;

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

        if (IsActiveStatus(status) && terminalOn is not null)
        {
            throw new TransferInvariantViolationException("Active process cannot have TerminalOn.");
        }

        if (!IsActiveStatus(status) && terminalOn is null)
        {
            throw new TransferInvariantViolationException("Terminal process requires TerminalOn.");
        }

        if (status is TransferProcessStatus.Failed or TransferProcessStatus.Rejected
            && string.IsNullOrWhiteSpace(failureReasonCode))
        {
            throw new TransferInvariantViolationException(
                $"{status} process requires FailureReasonCode.");
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

    public TransferProcess RequestSportingApproval()
    {
        if (Status == TransferProcessStatus.SportingApprovalPending)
        {
            return this;
        }

        if (Status == TransferProcessStatus.SportingApproved)
        {
            throw new TransferInvariantViolationException(
                "Sporting approval already granted; cannot request again.");
        }

        EnsureActive();
        if (Status != TransferProcessStatus.UnderEvaluation)
        {
            throw new TransferInvariantViolationException(
                $"Cannot request sporting approval from {Status}.");
        }

        return WithStatus(TransferProcessStatus.SportingApprovalPending, FailureReasonCode, terminalOn: null);
    }

    public TransferProcess GrantSportingApproval()
    {
        if (Status == TransferProcessStatus.SportingApproved)
        {
            return this;
        }

        if (Status != TransferProcessStatus.SportingApprovalPending)
        {
            throw new TransferInvariantViolationException(
                "Sporting approval can only be granted while pending.");
        }

        return WithStatus(TransferProcessStatus.SportingApproved, FailureReasonCode, terminalOn: null);
    }

    public TransferProcess RejectSportingApproval(string reasonCode, GameDate day)
    {
        if (Status == TransferProcessStatus.Rejected)
        {
            return this;
        }

        if (Status != TransferProcessStatus.SportingApprovalPending)
        {
            throw new TransferInvariantViolationException(
                "Sporting rejection can only occur while approval is pending.");
        }

        return WithStatus(
            TransferProcessStatus.Rejected,
            RequireReason(reasonCode),
            day);
    }

    public TransferProcess EnterClubNegotiation()
    {
        if (Status == TransferProcessStatus.ClubNegotiation)
        {
            return this;
        }

        if (IsFreeAgent)
        {
            throw new TransferInvariantViolationException(
                "Free-agent process skips club negotiation.");
        }

        if (Status != TransferProcessStatus.SportingApproved)
        {
            throw new TransferInvariantViolationException(
                "Club negotiation requires sporting approval.");
        }

        return WithStatus(TransferProcessStatus.ClubNegotiation, FailureReasonCode, terminalOn: null);
    }

    public TransferProcess ReachClubAgreement()
    {
        if (Status == TransferProcessStatus.ClubAgreementReached)
        {
            return this;
        }

        if (Status != TransferProcessStatus.ClubNegotiation)
        {
            throw new TransferInvariantViolationException(
                "Club agreement requires an active club negotiation.");
        }

        return WithStatus(TransferProcessStatus.ClubAgreementReached, FailureReasonCode, terminalOn: null);
    }

    public TransferProcess Withdraw(GameDate day)
    {
        EnsureActive();
        return WithStatus(TransferProcessStatus.Withdrawn, FailureReasonCode, day);
    }

    public TransferProcess Fail(string reasonCode, GameDate day)
    {
        EnsureActive();
        return WithStatus(TransferProcessStatus.Failed, RequireReason(reasonCode), day);
    }

    public TransferProcess Archive(GameDate day)
    {
        if (Status == TransferProcessStatus.Archived)
        {
            return this;
        }

        if (IsActive)
        {
            throw new TransferInvariantViolationException(
                "Active process cannot be archived; withdraw, fail, or reject first.");
        }

        return WithStatus(TransferProcessStatus.Archived, FailureReasonCode, day);
    }

    private TransferProcess WithStatus(
        TransferProcessStatus status,
        string? failureReasonCode,
        GameDate? terminalOn) =>
        new(
            ProcessId,
            NeedId,
            TargetId,
            BuyingClubId,
            PlayerId,
            SellingClubId,
            IsFreeAgent,
            status,
            failureReasonCode,
            OpenedOn,
            terminalOn);

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new TransferInvariantViolationException(
                $"Process #{ProcessId.Value} is terminal ({Status}) and cannot transition.");
        }
    }

    private static bool IsActiveStatus(TransferProcessStatus status) =>
        status is TransferProcessStatus.UnderEvaluation
            or TransferProcessStatus.SportingApprovalPending
            or TransferProcessStatus.SportingApproved
            or TransferProcessStatus.ClubNegotiation
            or TransferProcessStatus.ClubAgreementReached;

    private static string RequireReason(string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new TransferInvariantViolationException("Failure reason is required.");
        }

        if (reasonCode.Length > 64)
        {
            throw new TransferInvariantViolationException("Failure reason max length is 64.");
        }

        return reasonCode.Trim();
    }
}
