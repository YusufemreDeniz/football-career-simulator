using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Transfer;

/// <summary>
/// Kulübün sportif kadro ihtiyacı. Target / Process / müzakere içermez.
/// </summary>
public sealed class TransferNeed
{
    public const int MinPriority = 1;
    public const int MaxPriority = 5;

    private TransferNeed(
        TransferNeedId needId,
        ClubId clubId,
        TransferNeedKind kind,
        TransferNeedStatus status,
        int priority,
        string reasonCode,
        GameDate identifiedOn,
        GameDate? closedOn)
    {
        NeedId = needId;
        ClubId = clubId;
        Kind = kind;
        Status = status;
        Priority = priority;
        ReasonCode = reasonCode;
        IdentifiedOn = identifiedOn;
        ClosedOn = closedOn;
    }

    public TransferNeedId NeedId { get; }

    public ClubId ClubId { get; }

    public TransferNeedKind Kind { get; }

    public TransferNeedStatus Status { get; }

    public int Priority { get; }

    public string ReasonCode { get; }

    public GameDate IdentifiedOn { get; }

    public GameDate? ClosedOn { get; }

    public bool IsOpen => Status == TransferNeedStatus.Open;

    public static TransferNeed Identify(
        TransferNeedId needId,
        ClubId clubId,
        TransferNeedKind kind,
        int priority,
        string reasonCode,
        GameDate day)
    {
        Validate(kind, priority, reasonCode);
        return new TransferNeed(
            needId,
            clubId,
            kind,
            TransferNeedStatus.Open,
            priority,
            reasonCode.Trim(),
            day,
            closedOn: null);
    }

    public static TransferNeed Rehydrate(
        TransferNeedId needId,
        ClubId clubId,
        TransferNeedKind kind,
        TransferNeedStatus status,
        int priority,
        string reasonCode,
        GameDate identifiedOn,
        GameDate? closedOn)
    {
        Validate(kind, priority, reasonCode);

        if (!Enum.IsDefined(status))
        {
            throw new TransferInvariantViolationException($"Unknown transfer need status: {status}.");
        }

        if (status == TransferNeedStatus.Closed && closedOn is null)
        {
            throw new TransferInvariantViolationException("Closed transfer need requires ClosedOn.");
        }

        if (status == TransferNeedStatus.Open && closedOn is not null)
        {
            throw new TransferInvariantViolationException("Open transfer need cannot have ClosedOn.");
        }

        return new TransferNeed(
            needId,
            clubId,
            kind,
            status,
            priority,
            reasonCode.Trim(),
            identifiedOn,
            closedOn);
    }

    public TransferNeed Close(GameDate day)
    {
        if (Status == TransferNeedStatus.Closed)
        {
            return this;
        }

        return new TransferNeed(
            NeedId,
            ClubId,
            Kind,
            TransferNeedStatus.Closed,
            Priority,
            ReasonCode,
            IdentifiedOn,
            day);
    }

    private static void Validate(TransferNeedKind kind, int priority, string reasonCode)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new TransferInvariantViolationException($"Unknown transfer need kind: {kind}.");
        }

        if (priority is < MinPriority or > MaxPriority)
        {
            throw new TransferInvariantViolationException(
                $"Priority must be between {MinPriority} and {MaxPriority}.");
        }

        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new TransferInvariantViolationException("ReasonCode is required.");
        }

        if (reasonCode.Length > 64)
        {
            throw new TransferInvariantViolationException("ReasonCode max length is 64.");
        }
    }
}
