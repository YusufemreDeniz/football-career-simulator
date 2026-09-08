using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Interaction;

/// <summary>
/// Sezon ve kariyer belgeseli için çıkarılan deterministik anlatı olgusu (Yenilik 10: Career Chronicle).
/// </summary>
public sealed class NarrativeFactRecord
{
    public const int MinImportance = 1;
    public const int MaxImportance = 100;

    public NarrativeFactRecord(
        string factId,
        string milestoneType,
        int importanceScore,
        IReadOnlyList<string> involvedActorNames,
        GameDate occurredOn,
        string narrativeSnippet)
    {
        if (string.IsNullOrWhiteSpace(factId))
        {
            throw new InteractionInvariantViolationException("Fact ID is required.");
        }

        if (string.IsNullOrWhiteSpace(milestoneType))
        {
            throw new InteractionInvariantViolationException("Milestone type is required.");
        }

        if (string.IsNullOrWhiteSpace(narrativeSnippet))
        {
            throw new InteractionInvariantViolationException("Narrative snippet is required.");
        }

        FactId = factId;
        MilestoneType = milestoneType;
        ImportanceScore = Math.Clamp(importanceScore, MinImportance, MaxImportance);
        InvolvedActorNames = involvedActorNames ?? Array.Empty<string>();
        OccurredOn = occurredOn;
        NarrativeSnippet = narrativeSnippet;
    }

    public string FactId { get; }

    public string MilestoneType { get; }

    public int ImportanceScore { get; }

    public IReadOnlyList<string> InvolvedActorNames { get; }

    public GameDate OccurredOn { get; }

    public string NarrativeSnippet { get; }
}
