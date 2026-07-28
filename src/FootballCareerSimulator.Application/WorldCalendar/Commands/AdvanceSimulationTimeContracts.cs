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
        };

    public static AdvanceSimulationTimeResult Advanced(
        int previousDayNumber,
        int newDayNumber,
        IReadOnlyList<string> raisedEventTypes,
        int appliedEffectCount = 0,
        int duplicateEffectCount = 0,
        int reactionIntentCount = 0,
        IReadOnlyList<string>? reactionIntentTypeCodes = null) =>
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
        };
}

public sealed record TimeAdvanceBlockedItem(
    string SourceContext,
    string BlockerTypeCode,
    string DescriptionCode,
    bool IsHardBlocker);
