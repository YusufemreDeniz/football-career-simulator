using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Sosyal Continuity hafıza kaydı. MVP iskelet: Promise sonucu (Fulfilled/Broken) → Promise Memory.
/// </summary>
public sealed class MemoryRecord
{
    public const string PromiseOutcomeRuleId = "PromiseOutcome";
    public const int PromiseOutcomeRuleVersion = 1;
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
        if (promise.Status is not (PromiseStatus.Fulfilled or PromiseStatus.Broken))
        {
            throw new SocialContinuityInvariantViolationException(
                "Promise outcome memory requires Fulfilled or Broken status.");
        }

        var fulfilled = promise.Status == PromiseStatus.Fulfilled;
        var importance = fulfilled ? 60 : 80;
        var valence = fulfilled ? MemoryValence.Positive : MemoryValence.Negative;
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
}
