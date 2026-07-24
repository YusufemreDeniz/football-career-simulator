using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Interaction;

/// <summary>
/// Interaction & Narrative: bekleyen zorunlu/kritik karar isteği (ilk dilim: forma süresi talebi).
/// </summary>
public sealed class DecisionRequest
{
    public const string OptionGrantPlayingTimePromise = "GrantPlayingTimePromise";
    public const string OptionRefuse = "Refuse";

    private DecisionRequest(
        DecisionRequestId decisionRequestId,
        DecisionRequestKind kind,
        ManagerId managerId,
        PlayerId subjectPlayerId,
        ClubId clubId,
        GameDate openedOn,
        GameDate deadlineOn,
        DecisionRequestStatus status,
        bool isHardBlocker,
        string? selectedOptionCode,
        GameDate? resolvedOn)
    {
        DecisionRequestId = decisionRequestId;
        Kind = kind;
        ManagerId = managerId;
        SubjectPlayerId = subjectPlayerId;
        ClubId = clubId;
        OpenedOn = openedOn;
        DeadlineOn = deadlineOn;
        Status = status;
        IsHardBlocker = isHardBlocker;
        SelectedOptionCode = selectedOptionCode;
        ResolvedOn = resolvedOn;
    }

    public DecisionRequestId DecisionRequestId { get; }

    public DecisionRequestKind Kind { get; }

    public ManagerId ManagerId { get; }

    public PlayerId SubjectPlayerId { get; }

    public ClubId ClubId { get; }

    public GameDate OpenedOn { get; }

    public GameDate DeadlineOn { get; }

    public DecisionRequestStatus Status { get; }

    public bool IsHardBlocker { get; }

    public string? SelectedOptionCode { get; }

    public GameDate? ResolvedOn { get; }

    public bool IsOpen => Status == DecisionRequestStatus.Open;

    public static DecisionRequest OpenPlayingTimeRequest(
        DecisionRequestId decisionRequestId,
        ManagerId managerId,
        PlayerId subjectPlayerId,
        ClubId clubId,
        GameDate openedOn,
        GameDate deadlineOn,
        bool isHardBlocker = true)
    {
        if (deadlineOn.DayNumber < openedOn.DayNumber)
        {
            throw new InteractionInvariantViolationException(
                "Decision deadline cannot be before opened date.");
        }

        return new DecisionRequest(
            decisionRequestId,
            DecisionRequestKind.PlayingTimeRequest,
            managerId,
            subjectPlayerId,
            clubId,
            openedOn,
            deadlineOn,
            DecisionRequestStatus.Open,
            isHardBlocker,
            selectedOptionCode: null,
            resolvedOn: null);
    }

    public DecisionRequest Answer(string optionCode, GameDate day)
    {
        EnsureOpen();
        if (string.IsNullOrWhiteSpace(optionCode))
        {
            throw new InteractionInvariantViolationException("Option code is required.");
        }

        var trimmed = optionCode.Trim();
        if (Kind == DecisionRequestKind.PlayingTimeRequest
            && trimmed is not (OptionGrantPlayingTimePromise or OptionRefuse))
        {
            throw new InteractionInvariantViolationException(
                $"Unsupported option for playing-time request: {trimmed}.");
        }

        return new DecisionRequest(
            DecisionRequestId,
            Kind,
            ManagerId,
            SubjectPlayerId,
            ClubId,
            OpenedOn,
            DeadlineOn,
            DecisionRequestStatus.Answered,
            IsHardBlocker,
            trimmed,
            day);
    }

    public DecisionRequest ExpireIfDue(GameDate day)
    {
        if (Status != DecisionRequestStatus.Open)
        {
            return this;
        }

        if (day.DayNumber < DeadlineOn.DayNumber)
        {
            return this;
        }

        return new DecisionRequest(
            DecisionRequestId,
            Kind,
            ManagerId,
            SubjectPlayerId,
            ClubId,
            OpenedOn,
            DeadlineOn,
            DecisionRequestStatus.Expired,
            IsHardBlocker,
            selectedOptionCode: null,
            day);
    }

    public DecisionRequest Cancel(GameDate day)
    {
        EnsureOpen();
        return new DecisionRequest(
            DecisionRequestId,
            Kind,
            ManagerId,
            SubjectPlayerId,
            ClubId,
            OpenedOn,
            DeadlineOn,
            DecisionRequestStatus.Cancelled,
            IsHardBlocker,
            selectedOptionCode: null,
            day);
    }

    public DecisionRequest Archive()
    {
        if (Status is DecisionRequestStatus.Open)
        {
            throw new InteractionInvariantViolationException(
                "Open decision requests cannot be archived.");
        }

        if (Status == DecisionRequestStatus.Archived)
        {
            return this;
        }

        return new DecisionRequest(
            DecisionRequestId,
            Kind,
            ManagerId,
            SubjectPlayerId,
            ClubId,
            OpenedOn,
            DeadlineOn,
            DecisionRequestStatus.Archived,
            IsHardBlocker,
            SelectedOptionCode,
            ResolvedOn);
    }

    public static DecisionRequest Rehydrate(
        DecisionRequestId decisionRequestId,
        DecisionRequestKind kind,
        ManagerId managerId,
        PlayerId subjectPlayerId,
        ClubId clubId,
        GameDate openedOn,
        GameDate deadlineOn,
        DecisionRequestStatus status,
        bool isHardBlocker,
        string? selectedOptionCode,
        GameDate? resolvedOn)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new InteractionInvariantViolationException($"Unknown decision kind: {kind}.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new InteractionInvariantViolationException($"Unknown decision status: {status}.");
        }

        return new DecisionRequest(
            decisionRequestId,
            kind,
            managerId,
            subjectPlayerId,
            clubId,
            openedOn,
            deadlineOn,
            status,
            isHardBlocker,
            selectedOptionCode,
            resolvedOn);
    }

    private void EnsureOpen()
    {
        if (Status != DecisionRequestStatus.Open)
        {
            throw new InteractionInvariantViolationException(
                "Only open decision requests can be answered or cancelled.");
        }
    }
}
