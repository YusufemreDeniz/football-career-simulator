namespace FootballCareerSimulator.Application.WorldCalendar.Commands;

public sealed record AdvanceSimulationTimeCommand(
    Guid CommandId,
    int TargetDayNumber);

public sealed record AdvanceSimulationTimeResult
{
    public required bool Succeeded { get; init; }

    public required bool WasBlocked { get; init; }

    public required int PreviousDayNumber { get; init; }

    public required int NewDayNumber { get; init; }

    public required IReadOnlyList<string> RaisedEventTypes { get; init; }

    public required IReadOnlyList<TimeAdvanceBlockedItem> Blockers { get; init; }

    /// <summary>Event & Rule Evaluation: ilk kez commit edilen effect sayısı.</summary>
    public int AppliedEffectCount { get; init; }

    /// <summary>Event & Rule Evaluation: duplicate sayılan effect sayısı.</summary>
    public int DuplicateEffectCount { get; init; }

    /// <summary>Reaction rule intent sayısı (foreign state mutation değil).</summary>
    public int ReactionIntentCount { get; init; }

    public IReadOnlyList<string> ReactionIntentTypeCodes { get; init; } = Array.Empty<string>();

    /// <summary>Reaction intent'ten üretilen scheduled evaluation sayısı.</summary>
    public int ScheduledEvaluationCount { get; init; }

    /// <summary>Due scheduled evaluation işlenen sayısı.</summary>
    public int DueEvaluationsProcessed { get; init; }

    /// <summary>Due evaluation ile kapanan transfer penceresi sayısı.</summary>
    public int TransferWindowsClosedBySchedule { get; init; }

    /// <summary>DayBoundaryObserved → süresi dolan sözleşme sayısı.</summary>
    public int ExpiredContractCount { get; init; }

    public IReadOnlyList<long> ContractExpiryAffectedClubIds { get; init; } = Array.Empty<long>();

    public IReadOnlyList<long> NewlyFreeAgentPlayerIds { get; init; } = Array.Empty<long>();

    public static AdvanceSimulationTimeResult Blocked(
        int currentDayNumber,
        IReadOnlyList<TimeAdvanceBlockedItem> blockers) =>
        new()
        {
            Succeeded = false,
            WasBlocked = true,
            PreviousDayNumber = currentDayNumber,
            NewDayNumber = currentDayNumber,
            RaisedEventTypes = Array.Empty<string>(),
            Blockers = blockers,
            AppliedEffectCount = 0,
            DuplicateEffectCount = 0,
            ReactionIntentCount = 0,
            ReactionIntentTypeCodes = Array.Empty<string>(),
            ScheduledEvaluationCount = 0,
            DueEvaluationsProcessed = 0,
            TransferWindowsClosedBySchedule = 0,
            ExpiredContractCount = 0,
            ContractExpiryAffectedClubIds = Array.Empty<long>(),
            NewlyFreeAgentPlayerIds = Array.Empty<long>(),
        };

    public static AdvanceSimulationTimeResult Advanced(
        int previousDayNumber,
        int newDayNumber,
        IReadOnlyList<string> raisedEventTypes,
        int appliedEffectCount = 0,
        int duplicateEffectCount = 0,
        int reactionIntentCount = 0,
        IReadOnlyList<string>? reactionIntentTypeCodes = null,
        int scheduledEvaluationCount = 0,
        int dueEvaluationsProcessed = 0,
        int transferWindowsClosedBySchedule = 0,
        int expiredContractCount = 0,
        IReadOnlyList<long>? contractExpiryAffectedClubIds = null,
        IReadOnlyList<long>? newlyFreeAgentPlayerIds = null) =>
        new()
        {
            Succeeded = true,
            WasBlocked = false,
            PreviousDayNumber = previousDayNumber,
            NewDayNumber = newDayNumber,
            RaisedEventTypes = raisedEventTypes,
            Blockers = Array.Empty<TimeAdvanceBlockedItem>(),
            AppliedEffectCount = appliedEffectCount,
            DuplicateEffectCount = duplicateEffectCount,
            ReactionIntentCount = reactionIntentCount,
            ReactionIntentTypeCodes = reactionIntentTypeCodes ?? Array.Empty<string>(),
            ScheduledEvaluationCount = scheduledEvaluationCount,
            DueEvaluationsProcessed = dueEvaluationsProcessed,
            TransferWindowsClosedBySchedule = transferWindowsClosedBySchedule,
            ExpiredContractCount = expiredContractCount,
            ContractExpiryAffectedClubIds = contractExpiryAffectedClubIds ?? Array.Empty<long>(),
            NewlyFreeAgentPlayerIds = newlyFreeAgentPlayerIds ?? Array.Empty<long>(),
        };
}

public sealed record TimeAdvanceBlockedItem(
    string SourceContext,
    string BlockerTypeCode,
    string DescriptionCode,
    bool IsHardBlocker);
