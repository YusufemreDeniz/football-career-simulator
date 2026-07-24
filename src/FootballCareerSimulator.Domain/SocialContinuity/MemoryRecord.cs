using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Sosyal Continuity hafıza kaydı. MVP iskelet: Promise / Selection / Trust / Transfer / Career / ClubHistory / MatchPerformance.
/// </summary>
public sealed class MemoryRecord
{
    public const string PromiseOutcomeRuleId = "PromiseOutcome";
    public const int PromiseOutcomeRuleVersion = 1;
    public const string SelectionStartedRuleId = "SelectionStarted";
    public const int SelectionStartedRuleVersion = 1;
    public const string SelectionBenchedRuleId = "SelectionBenched";
    public const int SelectionBenchedRuleVersion = 1;
    public const string SelectionOmittedRuleId = "SelectionOmitted";
    public const int SelectionOmittedRuleVersion = 1;
    public const string TrustFromPromiseRuleId = "TrustFromPromise";
    public const int TrustFromPromiseRuleVersion = 1;
    public const string TransferCompletedRuleId = "TransferCompleted";
    public const int TransferCompletedRuleVersion = 1;
    public const string ManagerDismissedRuleId = "ManagerDismissed";
    public const int ManagerDismissedRuleVersion = 1;
    public const string ManagerHiredRuleId = "ManagerHired";
    public const int ManagerHiredRuleVersion = 1;
    public const string ClubHistoryLeftDismissedRuleId = "ClubHistoryLeftDismissed";
    public const int ClubHistoryLeftDismissedRuleVersion = 1;
    public const string ClubHistoryReturnedRuleId = "ClubHistoryReturned";
    public const int ClubHistoryReturnedRuleVersion = 1;
    public const string ClubHistoryLeftTransferRuleId = "ClubHistoryLeftTransfer";
    public const int ClubHistoryLeftTransferRuleVersion = 1;
    public const string ClubHistoryJoinedTransferRuleId = "ClubHistoryJoinedTransfer";
    public const int ClubHistoryJoinedTransferRuleVersion = 1;
    public const string MatchBlowoutRuleId = "MatchBlowout";
    public const int MatchBlowoutRuleVersion = 1;
    public const int MatchBlowoutMinGoalDifference = 3;
    public const int MinImportance = 1;
    public const int MaxImportance = 100;

    private MemoryRecord(
        MemoryId memoryId,
        ActorRef rememberingActor,
        MemorySubjectKind subjectKind,
        long subjectId,
        string sourceEventKey,
        MemoryCategory category,
        GameDate createdOn,
        GameDate lastReinforcedOn,
        int baseImportance,
        int currentInfluence,
        MemoryValence valence,
        MemoryVisibility visibility,
        MemoryStatus status,
        int reinforcementCount,
        PromiseId? relatedPromiseId,
        string ruleId,
        int ruleVersion)
    {
        MemoryId = memoryId;
        RememberingActor = rememberingActor;
        SubjectKind = subjectKind;
        SubjectId = subjectId;
        SourceEventKey = sourceEventKey;
        Category = category;
        CreatedOn = createdOn;
        LastReinforcedOn = lastReinforcedOn;
        BaseImportance = baseImportance;
        CurrentInfluence = currentInfluence;
        Valence = valence;
        Visibility = visibility;
        Status = status;
        ReinforcementCount = reinforcementCount;
        RelatedPromiseId = relatedPromiseId;
        RuleId = ruleId;
        RuleVersion = ruleVersion;
    }

    public MemoryId MemoryId { get; }

    public ActorRef RememberingActor { get; }

    public MemorySubjectKind SubjectKind { get; }

    public long SubjectId { get; }

    public string SourceEventKey { get; }

    public MemoryCategory Category { get; }

    public GameDate CreatedOn { get; }

    public GameDate LastReinforcedOn { get; }

    public int BaseImportance { get; }

    public int CurrentInfluence { get; }

    public MemoryValence Valence { get; }

    public MemoryVisibility Visibility { get; }

    public MemoryStatus Status { get; }

    public int ReinforcementCount { get; }

    public PromiseId? RelatedPromiseId { get; }

    public string RuleId { get; }

    public int RuleVersion { get; }

    public static MemoryRecord CreatePromiseOutcome(
        MemoryId memoryId,
        ActorRef rememberingActor,
        Promise promise,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(promise);
        if (promise.Status is not (
            PromiseStatus.Fulfilled
            or PromiseStatus.Broken
            or PromiseStatus.Invalidated))
        {
            throw new SocialContinuityInvariantViolationException(
                "Promise outcome memory requires Fulfilled, Broken, or Invalidated status.");
        }

        var (importance, valence) = promise.Status switch
        {
            PromiseStatus.Fulfilled => (60, MemoryValence.Positive),
            PromiseStatus.Broken => (80, MemoryValence.Negative),
            _ => (55, MemoryValence.Neutral),
        };
        var sourceKey = BuildPromiseOutcomeSourceKey(promise.PromiseId, promise.Status);

        return new MemoryRecord(
            memoryId,
            rememberingActor,
            MemorySubjectKind.Promise,
            promise.PromiseId.Value,
            sourceKey,
            MemoryCategory.Promise,
            day,
            day,
            importance,
            importance,
            valence,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            promise.PromiseId,
            PromiseOutcomeRuleId,
            PromiseOutcomeRuleVersion);
    }

    public static MemoryRecord CreateTrustFromPromiseOutcome(
        MemoryId memoryId,
        Promise promise,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(promise);
        if (promise.Status is not (PromiseStatus.Fulfilled or PromiseStatus.Broken))
        {
            throw new SocialContinuityInvariantViolationException(
                "Trust-from-promise memory requires Fulfilled or Broken status.");
        }

        if (promise.Promisee.Kind != ActorKind.Player)
        {
            throw new SocialContinuityInvariantViolationException(
                "Trust-from-promise remembering actor must be the player promisee.");
        }

        var subjectKind = promise.Promisor.Kind switch
        {
            ActorKind.Manager => MemorySubjectKind.Manager,
            ActorKind.Player => MemorySubjectKind.Player,
            _ => throw new SocialContinuityInvariantViolationException(
                $"Unsupported trust subject kind: {promise.Promisor.Kind}."),
        };

        var reliable = promise.Status == PromiseStatus.Fulfilled;
        return new MemoryRecord(
            memoryId,
            promise.Promisee,
            subjectKind,
            promise.Promisor.Id,
            BuildTrustFromPromiseSourceKey(promise.PromiseId, promise.Status),
            MemoryCategory.Trust,
            day,
            day,
            baseImportance: reliable ? 50 : 70,
            currentInfluence: reliable ? 50 : 70,
            reliable ? MemoryValence.Positive : MemoryValence.Negative,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            promise.PromiseId,
            TrustFromPromiseRuleId,
            TrustFromPromiseRuleVersion);
    }

    public static MemoryRecord CreateManagerDismissed(
        MemoryId memoryId,
        ManagerId managerId,
        ClubId clubId,
        FixtureId causationFixtureId,
        GameDate day)
    {
        const int importance = 85;
        return new MemoryRecord(
            memoryId,
            new ActorRef(ActorKind.Manager, managerId.Value),
            MemorySubjectKind.Club,
            clubId.Value,
            BuildManagerDismissedSourceKey(causationFixtureId, managerId),
            MemoryCategory.Career,
            day,
            day,
            importance,
            importance,
            MemoryValence.Negative,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            relatedPromiseId: null,
            ManagerDismissedRuleId,
            ManagerDismissedRuleVersion);
    }

    public static MemoryRecord CreateManagerHired(
        MemoryId memoryId,
        ManagerId managerId,
        ClubId clubId,
        JobOfferId offerId,
        GameDate day)
    {
        const int importance = 70;
        return new MemoryRecord(
            memoryId,
            new ActorRef(ActorKind.Manager, managerId.Value),
            MemorySubjectKind.Club,
            clubId.Value,
            BuildManagerHiredSourceKey(offerId),
            MemoryCategory.Career,
            day,
            day,
            importance,
            importance,
            MemoryValence.Positive,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            relatedPromiseId: null,
            ManagerHiredRuleId,
            ManagerHiredRuleVersion);
    }

    public static MemoryRecord CreateClubHistoryLeftDismissed(
        MemoryId memoryId,
        ManagerId managerId,
        ClubId clubId,
        FixtureId causationFixtureId,
        GameDate day)
    {
        const int importance = 75;
        return new MemoryRecord(
            memoryId,
            new ActorRef(ActorKind.Manager, managerId.Value),
            MemorySubjectKind.Club,
            clubId.Value,
            BuildClubHistoryLeftDismissedSourceKey(causationFixtureId, managerId),
            MemoryCategory.ClubHistory,
            day,
            day,
            importance,
            importance,
            MemoryValence.Negative,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            relatedPromiseId: null,
            ClubHistoryLeftDismissedRuleId,
            ClubHistoryLeftDismissedRuleVersion);
    }

    public static MemoryRecord CreateClubHistoryReturned(
        MemoryId memoryId,
        ManagerId managerId,
        ClubId clubId,
        JobOfferId offerId,
        GameDate day)
    {
        const int importance = 80;
        return new MemoryRecord(
            memoryId,
            new ActorRef(ActorKind.Manager, managerId.Value),
            MemorySubjectKind.Club,
            clubId.Value,
            BuildClubHistoryReturnedSourceKey(offerId),
            MemoryCategory.ClubHistory,
            day,
            day,
            importance,
            importance,
            MemoryValence.Positive,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            relatedPromiseId: null,
            ClubHistoryReturnedRuleId,
            ClubHistoryReturnedRuleVersion);
    }

    public static MemoryRecord CreateClubHistoryLeftTransfer(
        MemoryId memoryId,
        PlayerId playerId,
        ClubId sellingClubId,
        TransferProcessId processId,
        GameDate day)
    {
        const int importance = 60;
        return new MemoryRecord(
            memoryId,
            new ActorRef(ActorKind.Player, playerId.Value),
            MemorySubjectKind.Club,
            sellingClubId.Value,
            BuildClubHistoryLeftTransferSourceKey(processId),
            MemoryCategory.ClubHistory,
            day,
            day,
            importance,
            importance,
            MemoryValence.Neutral,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            relatedPromiseId: null,
            ClubHistoryLeftTransferRuleId,
            ClubHistoryLeftTransferRuleVersion);
    }

    public static MemoryRecord CreateClubHistoryJoinedTransfer(
        MemoryId memoryId,
        PlayerId playerId,
        ClubId buyingClubId,
        TransferProcessId processId,
        GameDate day,
        bool isFreeAgent)
    {
        const int importance = 60;
        return new MemoryRecord(
            memoryId,
            new ActorRef(ActorKind.Player, playerId.Value),
            MemorySubjectKind.Club,
            buyingClubId.Value,
            BuildClubHistoryJoinedTransferSourceKey(processId),
            MemoryCategory.ClubHistory,
            day,
            day,
            importance,
            importance,
            isFreeAgent ? MemoryValence.Positive : MemoryValence.Neutral,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            relatedPromiseId: null,
            ClubHistoryJoinedTransferRuleId,
            ClubHistoryJoinedTransferRuleVersion);
    }

    public static MemoryRecord CreateMatchBlowout(
        MemoryId memoryId,
        ActorRef rememberingActor,
        FixtureId fixtureId,
        GameDate day,
        bool managedWon)
    {
        if (rememberingActor.Kind is not (ActorKind.Manager or ActorKind.Player))
        {
            throw new SocialContinuityInvariantViolationException(
                "Match-blowout memory remembering actor must be a manager or player.");
        }

        const int importance = 75;
        return new MemoryRecord(
            memoryId,
            rememberingActor,
            MemorySubjectKind.Fixture,
            fixtureId.Value,
            BuildMatchBlowoutSourceKey(fixtureId, rememberingActor),
            MemoryCategory.MatchPerformance,
            day,
            day,
            importance,
            importance,
            managedWon ? MemoryValence.Positive : MemoryValence.Negative,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            relatedPromiseId: null,
            MatchBlowoutRuleId,
            MatchBlowoutRuleVersion);
    }

    public static MemoryRecord CreateTransferCompleted(
        MemoryId memoryId,
        ActorRef rememberingActor,
        TransferProcessId processId,
        GameDate day,
        bool isFreeAgent)
    {
        if (rememberingActor.Kind is not (ActorKind.Player or ActorKind.Manager))
        {
            throw new SocialContinuityInvariantViolationException(
                "Transfer-completed memory remembering actor must be a player or manager.");
        }

        var valence = isFreeAgent ? MemoryValence.Positive : MemoryValence.Neutral;
        const int importance = 65;
        return new MemoryRecord(
            memoryId,
            rememberingActor,
            MemorySubjectKind.TransferProcess,
            processId.Value,
            BuildTransferCompletedSourceKey(processId),
            MemoryCategory.Transfer,
            day,
            day,
            importance,
            importance,
            valence,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            relatedPromiseId: null,
            TransferCompletedRuleId,
            TransferCompletedRuleVersion);
    }

    public static MemoryRecord CreateSelectionStarted(
        MemoryId memoryId,
        ActorRef rememberingPlayer,
        FixtureId fixtureId,
        GameDate day) =>
        CreateSelectionMemory(
            memoryId,
            rememberingPlayer,
            fixtureId,
            day,
            BuildSelectionStartedSourceKey(fixtureId, rememberingPlayer.Id),
            baseImportance: 35,
            MemoryValence.Positive,
            SelectionStartedRuleId,
            SelectionStartedRuleVersion);

    public static MemoryRecord CreateSelectionBenched(
        MemoryId memoryId,
        ActorRef rememberingPlayer,
        FixtureId fixtureId,
        GameDate day) =>
        CreateSelectionMemory(
            memoryId,
            rememberingPlayer,
            fixtureId,
            day,
            BuildSelectionBenchedSourceKey(fixtureId, rememberingPlayer.Id),
            baseImportance: 25,
            MemoryValence.Neutral,
            SelectionBenchedRuleId,
            SelectionBenchedRuleVersion);

    public static MemoryRecord CreateSelectionOmitted(
        MemoryId memoryId,
        ActorRef rememberingPlayer,
        FixtureId fixtureId,
        GameDate day) =>
        CreateSelectionMemory(
            memoryId,
            rememberingPlayer,
            fixtureId,
            day,
            BuildSelectionOmittedSourceKey(fixtureId, rememberingPlayer.Id),
            baseImportance: 45,
            MemoryValence.Negative,
            SelectionOmittedRuleId,
            SelectionOmittedRuleVersion);

    public static MemoryRecord Rehydrate(
        MemoryId memoryId,
        ActorRef rememberingActor,
        MemorySubjectKind subjectKind,
        long subjectId,
        string sourceEventKey,
        MemoryCategory category,
        GameDate createdOn,
        GameDate lastReinforcedOn,
        int baseImportance,
        int currentInfluence,
        MemoryValence valence,
        MemoryVisibility visibility,
        MemoryStatus status,
        int reinforcementCount,
        PromiseId? relatedPromiseId,
        string ruleId,
        int ruleVersion)
    {
        if (!Enum.IsDefined(subjectKind))
        {
            throw new SocialContinuityInvariantViolationException($"Unknown subject kind: {subjectKind}.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new SocialContinuityInvariantViolationException($"Unknown memory category: {category}.");
        }

        if (!Enum.IsDefined(valence))
        {
            throw new SocialContinuityInvariantViolationException($"Unknown memory valence: {valence}.");
        }

        if (!Enum.IsDefined(visibility))
        {
            throw new SocialContinuityInvariantViolationException($"Unknown memory visibility: {visibility}.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new SocialContinuityInvariantViolationException($"Unknown memory status: {status}.");
        }

        if (string.IsNullOrWhiteSpace(sourceEventKey))
        {
            throw new SocialContinuityInvariantViolationException("Source event key is required.");
        }

        if (string.IsNullOrWhiteSpace(ruleId))
        {
            throw new SocialContinuityInvariantViolationException("Rule id is required.");
        }

        if (ruleVersion < 1)
        {
            throw new SocialContinuityInvariantViolationException("Rule version must be positive.");
        }

        if (subjectId <= 0)
        {
            throw new SocialContinuityInvariantViolationException("Subject id must be positive.");
        }

        if (baseImportance is < MinImportance or > MaxImportance
            || currentInfluence is < MinImportance or > MaxImportance)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Importance must be between {MinImportance} and {MaxImportance}.");
        }

        if (reinforcementCount < 0)
        {
            throw new SocialContinuityInvariantViolationException("Reinforcement count cannot be negative.");
        }

        return new MemoryRecord(
            memoryId,
            rememberingActor,
            subjectKind,
            subjectId,
            sourceEventKey,
            category,
            createdOn,
            lastReinforcedOn,
            baseImportance,
            currentInfluence,
            valence,
            visibility,
            status,
            reinforcementCount,
            relatedPromiseId,
            ruleId,
            ruleVersion);
    }

    public static string BuildPromiseOutcomeSourceKey(PromiseId promiseId, PromiseStatus status) =>
        $"PromiseTerminal:{promiseId.Value}:{status}";

    public static string BuildTrustFromPromiseSourceKey(PromiseId promiseId, PromiseStatus status) =>
        $"TrustFromPromise:{promiseId.Value}:{status}";

    public static string BuildTransferCompletedSourceKey(TransferProcessId processId) =>
        $"TransferCompleted:{processId.Value}";

    public static string BuildManagerDismissedSourceKey(FixtureId fixtureId, ManagerId managerId) =>
        $"ManagerDismissed:{fixtureId.Value}:{managerId.Value}";

    public static string BuildManagerHiredSourceKey(JobOfferId offerId) =>
        $"ManagerHired:{offerId.Value}";

    public static string BuildClubHistoryLeftDismissedSourceKey(FixtureId fixtureId, ManagerId managerId) =>
        $"ClubHistoryLeftDismissed:{fixtureId.Value}:{managerId.Value}";

    public static string BuildClubHistoryReturnedSourceKey(JobOfferId offerId) =>
        $"ClubHistoryReturned:{offerId.Value}";

    public static string BuildClubHistoryLeftTransferSourceKey(TransferProcessId processId) =>
        $"ClubHistoryLeftTransfer:{processId.Value}";

    public static string BuildClubHistoryJoinedTransferSourceKey(TransferProcessId processId) =>
        $"ClubHistoryJoinedTransfer:{processId.Value}";

    public static string BuildMatchBlowoutSourceKey(FixtureId fixtureId, ActorRef rememberingActor) =>
        $"MatchBlowout:{fixtureId.Value}:{rememberingActor.Kind}:{rememberingActor.Id}";

    public static string BuildSelectionStartedSourceKey(FixtureId fixtureId, long playerId) =>
        $"SelectionStarted:{fixtureId.Value}:{playerId}";

    public static string BuildSelectionBenchedSourceKey(FixtureId fixtureId, long playerId) =>
        $"SelectionBenched:{fixtureId.Value}:{playerId}";

    public static string BuildSelectionOmittedSourceKey(FixtureId fixtureId, long playerId) =>
        $"SelectionOmitted:{fixtureId.Value}:{playerId}";

    private static MemoryRecord CreateSelectionMemory(
        MemoryId memoryId,
        ActorRef rememberingPlayer,
        FixtureId fixtureId,
        GameDate day,
        string sourceEventKey,
        int baseImportance,
        MemoryValence valence,
        string ruleId,
        int ruleVersion)
    {
        if (rememberingPlayer.Kind != ActorKind.Player)
        {
            throw new SocialContinuityInvariantViolationException(
                "Selection memory remembering actor must be a player.");
        }

        return new MemoryRecord(
            memoryId,
            rememberingPlayer,
            MemorySubjectKind.Fixture,
            fixtureId.Value,
            sourceEventKey,
            MemoryCategory.Selection,
            day,
            day,
            baseImportance,
            baseImportance,
            valence,
            MemoryVisibility.Private,
            MemoryStatus.Active,
            reinforcementCount: 0,
            relatedPromiseId: null,
            ruleId,
            ruleVersion);
    }
}
