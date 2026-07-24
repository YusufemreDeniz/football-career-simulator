using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Interaction;

/// <summary>
/// Interaction & Narrative: DecisionRequest'e bağlı kısa Dialogue Session (tek turn iskeleti).
/// AvailableOptionCodes açılışta dondurulur (D-127); domain sonucu DecisionRequest üzerinden gider.
/// </summary>
public sealed class DialogueSession
{
    public const string PlayingTimeRequestType = "PlayingTimeRequest";

    private readonly string[] _availableOptionCodes;

    private DialogueSession(
        DialogueSessionId dialogueSessionId,
        DecisionRequestId sourceDecisionRequestId,
        string dialogueTypeCode,
        ManagerId managerId,
        PlayerId primaryParticipantPlayerId,
        GameDate createdOn,
        GameDate? deadlineOn,
        DialogueSessionStatus status,
        IReadOnlyList<string> availableOptionCodes,
        string? selectedOptionCode,
        GameDate? resolvedOn)
    {
        DialogueSessionId = dialogueSessionId;
        SourceDecisionRequestId = sourceDecisionRequestId;
        DialogueTypeCode = dialogueTypeCode;
        ManagerId = managerId;
        PrimaryParticipantPlayerId = primaryParticipantPlayerId;
        CreatedOn = createdOn;
        DeadlineOn = deadlineOn;
        Status = status;
        _availableOptionCodes = availableOptionCodes.ToArray();
        SelectedOptionCode = selectedOptionCode;
        ResolvedOn = resolvedOn;
    }

    public DialogueSessionId DialogueSessionId { get; }

    public DecisionRequestId SourceDecisionRequestId { get; }

    public string DialogueTypeCode { get; }

    public ManagerId ManagerId { get; }

    public PlayerId PrimaryParticipantPlayerId { get; }

    public GameDate CreatedOn { get; }

    public GameDate? DeadlineOn { get; }

    public DialogueSessionStatus Status { get; }

    public IReadOnlyList<string> AvailableOptionCodes => _availableOptionCodes;

    public string? SelectedOptionCode { get; }

    public GameDate? ResolvedOn { get; }

    public bool IsAwaitingPlayer => Status == DialogueSessionStatus.AwaitingPlayerDecision;

    public static DialogueSession OpenForDecision(
        DialogueSessionId dialogueSessionId,
        DecisionRequest request,
        IReadOnlyList<string> availableOptionCodes)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(availableOptionCodes);
        if (!request.IsOpen)
        {
            throw new InteractionInvariantViolationException(
                "Dialogue session requires an open decision request.");
        }

        if (availableOptionCodes.Count == 0)
        {
            throw new InteractionInvariantViolationException(
                "Dialogue session requires at least one available option code.");
        }

        var codes = availableOptionCodes
            .Select(c =>
            {
                if (string.IsNullOrWhiteSpace(c))
                {
                    throw new InteractionInvariantViolationException(
                        "Available option codes cannot be blank.");
                }

                return c.Trim();
            })
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();

        var typeCode = request.Kind switch
        {
            DecisionRequestKind.PlayingTimeRequest => PlayingTimeRequestType,
            _ => request.Kind.ToString(),
        };

        return new DialogueSession(
            dialogueSessionId,
            request.DecisionRequestId,
            typeCode,
            request.ManagerId,
            request.SubjectPlayerId,
            request.OpenedOn,
            request.DeadlineOn,
            DialogueSessionStatus.AwaitingPlayerDecision,
            codes,
            selectedOptionCode: null,
            resolvedOn: null);
    }

    public DialogueSession Resolve(string optionCode, GameDate day)
    {
        EnsureAwaitingPlayer();
        if (string.IsNullOrWhiteSpace(optionCode))
        {
            throw new InteractionInvariantViolationException("Option code is required.");
        }

        var trimmed = optionCode.Trim();
        if (!_availableOptionCodes.Contains(trimmed, StringComparer.Ordinal))
        {
            throw new InteractionInvariantViolationException(
                $"Option '{trimmed}' was not in the frozen dialogue option set.");
        }

        return new DialogueSession(
            DialogueSessionId,
            SourceDecisionRequestId,
            DialogueTypeCode,
            ManagerId,
            PrimaryParticipantPlayerId,
            CreatedOn,
            DeadlineOn,
            DialogueSessionStatus.Resolved,
            _availableOptionCodes,
            trimmed,
            day);
    }

    public DialogueSession Expire(GameDate day)
    {
        if (Status != DialogueSessionStatus.AwaitingPlayerDecision)
        {
            return this;
        }

        return new DialogueSession(
            DialogueSessionId,
            SourceDecisionRequestId,
            DialogueTypeCode,
            ManagerId,
            PrimaryParticipantPlayerId,
            CreatedOn,
            DeadlineOn,
            DialogueSessionStatus.Expired,
            _availableOptionCodes,
            selectedOptionCode: null,
            day);
    }

    public DialogueSession Invalidate(GameDate day)
    {
        if (Status is DialogueSessionStatus.Resolved
            or DialogueSessionStatus.Expired
            or DialogueSessionStatus.Invalidated
            or DialogueSessionStatus.Archived)
        {
            return this;
        }

        return new DialogueSession(
            DialogueSessionId,
            SourceDecisionRequestId,
            DialogueTypeCode,
            ManagerId,
            PrimaryParticipantPlayerId,
            CreatedOn,
            DeadlineOn,
            DialogueSessionStatus.Invalidated,
            _availableOptionCodes,
            selectedOptionCode: null,
            day);
    }

    public DialogueSession Archive()
    {
        if (Status == DialogueSessionStatus.AwaitingPlayerDecision)
        {
            throw new InteractionInvariantViolationException(
                "Awaiting dialogue sessions cannot be archived.");
        }

        if (Status == DialogueSessionStatus.Archived)
        {
            return this;
        }

        return new DialogueSession(
            DialogueSessionId,
            SourceDecisionRequestId,
            DialogueTypeCode,
            ManagerId,
            PrimaryParticipantPlayerId,
            CreatedOn,
            DeadlineOn,
            DialogueSessionStatus.Archived,
            _availableOptionCodes,
            SelectedOptionCode,
            ResolvedOn);
    }

    public static DialogueSession Rehydrate(
        DialogueSessionId dialogueSessionId,
        DecisionRequestId sourceDecisionRequestId,
        string dialogueTypeCode,
        ManagerId managerId,
        PlayerId primaryParticipantPlayerId,
        GameDate createdOn,
        GameDate? deadlineOn,
        DialogueSessionStatus status,
        IReadOnlyList<string> availableOptionCodes,
        string? selectedOptionCode,
        GameDate? resolvedOn)
    {
        if (string.IsNullOrWhiteSpace(dialogueTypeCode))
        {
            throw new InteractionInvariantViolationException("Dialogue type code is required.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new InteractionInvariantViolationException($"Unknown dialogue session status: {status}.");
        }

        ArgumentNullException.ThrowIfNull(availableOptionCodes);

        return new DialogueSession(
            dialogueSessionId,
            sourceDecisionRequestId,
            dialogueTypeCode.Trim(),
            managerId,
            primaryParticipantPlayerId,
            createdOn,
            deadlineOn,
            status,
            availableOptionCodes.ToArray(),
            selectedOptionCode,
            resolvedOn);
    }

    private void EnsureAwaitingPlayer()
    {
        if (Status != DialogueSessionStatus.AwaitingPlayerDecision)
        {
            throw new InteractionInvariantViolationException(
                "Only awaiting dialogue sessions can be resolved.");
        }
    }
}
