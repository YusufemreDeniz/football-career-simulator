using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Sosyal Continuity hafıza kaydı. MVP iskelet: Promise / Selection / Trust / Transfer.
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
