using FootballCareerSimulator.Application.Discipline.Services;
using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Domain.Discipline;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// DecisionRequest owner: PlayingTime / StartingOpportunity / Transfer / Discipline / BoardDemand / Press.
/// </summary>
public sealed class DecisionRequestService
{
    public const int DefaultPlayingTimeTargetAppearances = 3;
    public const int DefaultStartingOpportunityTargetStarts = 2;
    public const int DefaultDeadlineDays = 14;

    private readonly IDecisionRequestStore _store;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly PlayingTimePromiseService? _playingTime;
    private readonly StartingOpportunityPromiseService? _startingOpportunity;
    private readonly TransferNeedService? _transferNeeds;
    private readonly DisciplinaryActionService? _discipline;
    private readonly RelationshipEvaluationService? _relationships;
    private readonly DecisionMemoryService? _decisionMemory;
    private readonly DialogueOptionGenerationService? _dialogueOptions;
    private readonly DialogueSessionService? _dialogueSessions;

    public DecisionRequestService(
        IDecisionRequestStore store,
        IManagerCareerStore managerCareerStore,
        PlayingTimePromiseService? playingTime = null,
        RelationshipEvaluationService? relationships = null,
        DecisionMemoryService? decisionMemory = null,
        DialogueOptionGenerationService? dialogueOptions = null,
        DialogueSessionService? dialogueSessions = null,
        StartingOpportunityPromiseService? startingOpportunity = null,
        TransferNeedService? transferNeeds = null,
        DisciplinaryActionService? discipline = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _playingTime = playingTime;
        _startingOpportunity = startingOpportunity;
        _transferNeeds = transferNeeds;
        _discipline = discipline;
        _relationships = relationships;
        _decisionMemory = decisionMemory;
        _dialogueOptions = dialogueOptions;
        _dialogueSessions = dialogueSessions;
    }

    public DecisionRequest OpenPlayingTimeRequest(
        PlayerId subjectPlayerId,
        GameDate day,
        int? deadlineDays = null,
        bool isHardBlocker = true) =>
        OpenRequest(
            DecisionRequestKind.PlayingTimeRequest,
            subjectPlayerId,
            day,
            deadlineDays,
            isHardBlocker,
            DecisionRequest.OpenPlayingTimeRequest,
            "playing-time");

    public DecisionRequest OpenStartingOpportunityRequest(
        PlayerId subjectPlayerId,
        GameDate day,
        int? deadlineDays = null,
        bool isHardBlocker = true) =>
        OpenRequest(
            DecisionRequestKind.StartingOpportunityRequest,
            subjectPlayerId,
            day,
            deadlineDays,
            isHardBlocker,
            DecisionRequest.OpenStartingOpportunityRequest,
            "starting-opportunity");

    public DecisionRequest OpenTransferRequest(
        PlayerId subjectPlayerId,
        GameDate day,
        int? deadlineDays = null,
        bool isHardBlocker = true) =>
        OpenRequest(
            DecisionRequestKind.TransferRequest,
            subjectPlayerId,
            day,
            deadlineDays,
            isHardBlocker,
            DecisionRequest.OpenTransferRequest,
            "transfer");

    public DecisionRequest OpenDisciplineRequest(
        PlayerId subjectPlayerId,
        GameDate day,
        int? deadlineDays = null,
        bool isHardBlocker = true) =>
        OpenRequest(
            DecisionRequestKind.DisciplineRequest,
            subjectPlayerId,
            day,
            deadlineDays,
            isHardBlocker,
            DecisionRequest.OpenDisciplineRequest,
            "discipline");

    public DecisionRequest OpenPressQuestionRequest(
        PlayerId subjectPlayerId,
        GameDate day,
        int? deadlineDays = null,
        bool isHardBlocker = true) =>
        OpenRequest(
            DecisionRequestKind.PressQuestionRequest,
            subjectPlayerId,
            day,
            deadlineDays,
            isHardBlocker,
            DecisionRequest.OpenPressQuestionRequest,
            "press-question");

    public bool HasOpenPressQuestionForManagedClub()
    {
        var career = _managerCareerStore.Career;
        if (!career.IsEmployed || career.ActiveEmployment is null)
        {
            return false;
        }

        var clubId = career.ActiveEmployment.ClubId;
        return _store.Requests.Any(r =>
            r.IsOpen
            && r.Kind == DecisionRequestKind.PressQuestionRequest
            && r.ClubId == clubId);
    }

    public bool HasOpenDisciplineForManagedClub()
    {
        var career = _managerCareerStore.Career;
        if (!career.IsEmployed || career.ActiveEmployment is null)
        {
            return false;
        }

        var clubId = career.ActiveEmployment.ClubId;
        return _store.Requests.Any(r =>
            r.IsOpen
            && r.Kind == DecisionRequestKind.DisciplineRequest
            && r.ClubId == clubId);
    }

    public bool HasOpenPlayerRequest(PlayerId subjectPlayerId, params DecisionRequestKind[] kinds)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        if (kinds.Length == 0)
        {
            return false;
        }

        var career = _managerCareerStore.Career;
        if (!career.IsEmployed || career.ActiveEmployment is null)
        {
            return false;
        }

        var clubId = career.ActiveEmployment.ClubId;
        var kindSet = kinds.ToHashSet();
        return _store.Requests.Any(r =>
            r.IsOpen
            && kindSet.Contains(r.Kind)
            && r.SubjectPlayerId == subjectPlayerId
            && r.ClubId == clubId);
    }

    public int CountPlayerRequests(PlayerId subjectPlayerId, DecisionRequestKind kind)
    {
        var career = _managerCareerStore.Career;
        if (!career.IsEmployed || career.ActiveEmployment is null)
        {
            return 0;
        }

        var clubId = career.ActiveEmployment.ClubId;
        return _store.Requests.Count(r =>
            r.Kind == kind
            && r.SubjectPlayerId == subjectPlayerId
            && r.ClubId == clubId);
    }

    public DecisionRequest OpenBoardDemandRequest(
        GameDate day,
        int? deadlineDays = null,
        bool isHardBlocker = true)
    {
        var career = _managerCareerStore.Career;
        if (!career.IsEmployed || career.ActiveEmployment is null)
        {
            throw new InteractionInvariantViolationException(
                "Manager must be employed to open a board-demand decision request.");
        }

        var clubId = career.ActiveEmployment.ClubId;
        var hasOpen = _store.Requests.Any(r =>
            r.IsOpen
            && r.Kind == DecisionRequestKind.BoardDemandRequest
            && r.ClubId == clubId);
        if (hasOpen)
        {
            throw new InteractionInvariantViolationException(
                $"Club {clubId.Value} already has an open board-demand decision request.");
        }

        var days = deadlineDays ?? DefaultDeadlineDays;
        if (days < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(deadlineDays), days, "Deadline days must be positive.");
        }

        var nextId = _store.Requests.Count == 0
            ? 1L
            : _store.Requests.Max(r => r.DecisionRequestId.Value) + 1;
        var request = DecisionRequest.OpenBoardDemandRequest(
            new DecisionRequestId(nextId),
            career.ManagerId,
            clubId,
            day,
            day.AddDays(days),
            isHardBlocker);
        _store.Upsert(request);
        OpenDialogueSession(request);
        return request;
    }

    public DecisionRequest Answer(
        DecisionRequestId decisionRequestId,
        string optionCode,
        GameDate day,
        int playingTimeTargetAppearances = DefaultPlayingTimeTargetAppearances,
        int startingOpportunityTargetStarts = DefaultStartingOpportunityTargetStarts)
    {
        var current = Require(decisionRequestId);
        _dialogueOptions?.EnsureEligible(decisionRequestId, optionCode);
        _dialogueSessions?.EnsureOptionInFrozenSet(decisionRequestId, optionCode);
        var answered = current.Answer(optionCode, day);
        _store.Upsert(answered);

        if (answered.Kind == DecisionRequestKind.PlayingTimeRequest
            && answered.SelectedOptionCode == DecisionRequest.OptionGrantPlayingTimePromise)
        {
            if (_playingTime is null)
            {
                throw new InteractionInvariantViolationException(
                    "Playing-time promise service is required to grant a playing-time decision.");
            }

            _playingTime.Create(
                answered.ManagerId,
                answered.SubjectPlayerId,
                answered.ClubId,
                playingTimeTargetAppearances,
                answered.DeadlineOn,
                day);
        }
        else if (answered.Kind == DecisionRequestKind.StartingOpportunityRequest
                 && answered.SelectedOptionCode == DecisionRequest.OptionGrantStartingOpportunityPromise)
        {
            if (_startingOpportunity is null)
            {
                throw new InteractionInvariantViolationException(
                    "Starting-opportunity promise service is required to grant a starting decision.");
            }

            _startingOpportunity.Create(
                answered.ManagerId,
                answered.SubjectPlayerId,
                answered.ClubId,
                startingOpportunityTargetStarts,
                answered.DeadlineOn,
                day);
        }
        else if (answered.Kind == DecisionRequestKind.TransferRequest
                 && answered.SelectedOptionCode == DecisionRequest.OptionAcknowledgeTransferRequest)
        {
            if (_transferNeeds is null)
            {
                throw new InteractionInvariantViolationException(
                    "Transfer need service is required to acknowledge a transfer decision.");
            }

            _transferNeeds.DeclarePlayerExitRequest(
                answered.ClubId,
                answered.SubjectPlayerId,
                day);
        }
        else if (answered.Kind == DecisionRequestKind.DisciplineRequest)
        {
            if (_discipline is null)
            {
                throw new InteractionInvariantViolationException(
                    "Disciplinary action service is required to resolve a discipline decision.");
            }

            var kind = answered.SelectedOptionCode switch
            {
                DecisionRequest.OptionIssueWarning => DisciplinaryActionKind.Warning,
                DecisionRequest.OptionIssueFine => DisciplinaryActionKind.Fine,
                DecisionRequest.OptionOfferSupport => DisciplinaryActionKind.Support,
                _ => throw new InteractionInvariantViolationException(
                    $"Unsupported discipline option: {answered.SelectedOptionCode}."),
            };
            _discipline.ApplyFromDecision(answered, kind, day);
        }
        else if (answered.Kind == DecisionRequestKind.BoardDemandRequest)
        {
            var assessment = _managerCareerStore.Career.ApplyBoardDemandResponse(
                answered.SelectedOptionCode!);
            _managerCareerStore.Replace(assessment.Career);
        }
        else if (answered.Kind == DecisionRequestKind.PressQuestionRequest)
        {
            var reputation = _managerCareerStore.Career.ApplyPressPublicNarrative(
                answered.SelectedOptionCode!);
            _managerCareerStore.Replace(reputation.Career);
        }

        _dialogueSessions?.MarkResolved(decisionRequestId, answered.SelectedOptionCode!, day);
        ApplySocialOutcomes(answered, day);
        return answered;
    }

    public int ExpireDue(GameDate day)
    {
        var expired = 0;
        foreach (var request in _store.Requests.Where(r => r.IsOpen).ToArray())
        {
            var next = request.ExpireIfDue(day);
            if (next.Status != request.Status)
            {
                _store.Upsert(next);
                _dialogueSessions?.MarkExpired(next.DecisionRequestId, day);
                ApplySocialOutcomes(next, day);
                expired++;
            }
        }

        return expired;
    }

    private DecisionRequest OpenRequest(
        DecisionRequestKind kind,
        PlayerId subjectPlayerId,
        GameDate day,
        int? deadlineDays,
        bool isHardBlocker,
        Func<DecisionRequestId, Domain.ManagerCareer.ManagerId, PlayerId, Domain.Shared.ClubId, GameDate, GameDate, bool, DecisionRequest> factory,
        string kindLabel)
    {
        var career = _managerCareerStore.Career;
        if (!career.IsEmployed || career.ActiveEmployment is null)
        {
            throw new InteractionInvariantViolationException(
                $"Manager must be employed to open a {kindLabel} decision request.");
        }

        var clubId = career.ActiveEmployment.ClubId;
        var hasOpen = _store.Requests.Any(r =>
            r.IsOpen
            && r.Kind == kind
            && r.SubjectPlayerId == subjectPlayerId
            && r.ClubId == clubId);
        if (hasOpen)
        {
            throw new InteractionInvariantViolationException(
                $"Player {subjectPlayerId.Value} already has an open {kindLabel} decision request.");
        }

        var days = deadlineDays ?? DefaultDeadlineDays;
        if (days < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(deadlineDays), days, "Deadline days must be positive.");
        }

        var nextId = _store.Requests.Count == 0
            ? 1L
            : _store.Requests.Max(r => r.DecisionRequestId.Value) + 1;
        var request = factory(
            new DecisionRequestId(nextId),
            career.ManagerId,
            subjectPlayerId,
            clubId,
            day,
            day.AddDays(days),
            isHardBlocker);
        _store.Upsert(request);
        OpenDialogueSession(request);
        return request;
    }

    private void OpenDialogueSession(DecisionRequest request)
    {
        if (_dialogueSessions is null)
        {
            return;
        }

        IReadOnlyList<string> optionCodes;
        if (_dialogueOptions is not null)
        {
            optionCodes = _dialogueOptions.GetForDecision(request.DecisionRequestId)
                .Options
                .Select(o => o.OptionCode)
                .ToArray();
        }
        else
        {
            optionCodes = request.Kind switch
            {
                DecisionRequestKind.PlayingTimeRequest =>
                [
                    DecisionRequest.OptionGrantPlayingTimePromise,
                    DecisionRequest.OptionRefuse,
                ],
                DecisionRequestKind.StartingOpportunityRequest =>
                [
                    DecisionRequest.OptionGrantStartingOpportunityPromise,
                    DecisionRequest.OptionRefuse,
                ],
                DecisionRequestKind.TransferRequest =>
                [
                    DecisionRequest.OptionAcknowledgeTransferRequest,
                    DecisionRequest.OptionRefuse,
                ],
                DecisionRequestKind.DisciplineRequest =>
                [
                    DecisionRequest.OptionIssueWarning,
                    DecisionRequest.OptionIssueFine,
                    DecisionRequest.OptionOfferSupport,
                ],
                DecisionRequestKind.BoardDemandRequest =>
                [
                    DecisionRequest.OptionAcceptBoardDemand,
                    DecisionRequest.OptionCounterBoardDemand,
                    DecisionRequest.OptionRefuse,
                ],
                DecisionRequestKind.PressQuestionRequest =>
                [
                    DecisionRequest.OptionPubliclyDefend,
                    DecisionRequest.OptionPubliclyCriticize,
                ],
                _ => Array.Empty<string>(),
            };
        }

        if (optionCodes.Count == 0)
        {
            return;
        }

        _dialogueSessions.OpenForDecision(request, optionCodes);
    }

    private void ApplySocialOutcomes(DecisionRequest request, GameDate day)
    {
        _relationships?.ApplyDecisionRequestOutcome(request, day);
        _decisionMemory?.RecordOutcome(request, day);
    }

    private DecisionRequest Require(DecisionRequestId id) =>
        _store.Get(id)
        ?? throw new InteractionInvariantViolationException(
            $"Decision request #{id.Value} not found.");
}
