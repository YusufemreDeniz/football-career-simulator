using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Interaction;

/// <summary>
/// Soyunma odasında veya kulislerde dolaşan, sınırlı ömürlü (TTL) dedikodu kaydı (Yenilik 3: Epistemik Bilgi Ayrımı).
/// </summary>
public sealed class RumorRecord
{
    public const int DefaultTtlDays = 21;
    public const int MinTtlDays = 7;
    public const int MaxTtlDays = 45;

    public RumorRecord(
        string rumorId,
        string sourceContext,
        string topicCode,
        string narrativeText,
        int spreadDepth,
        GameDate createdOn,
        int ttlDays = DefaultTtlDays)
    {
        if (string.IsNullOrWhiteSpace(rumorId))
        {
            throw new InteractionInvariantViolationException("Rumor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(sourceContext))
        {
            throw new InteractionInvariantViolationException("Source context is required.");
        }

        if (string.IsNullOrWhiteSpace(topicCode))
        {
            throw new InteractionInvariantViolationException("Topic code is required.");
        }

        RumorId = rumorId;
        SourceContext = sourceContext;
        TopicCode = topicCode;
        NarrativeText = narrativeText;
        SpreadDepth = Math.Clamp(spreadDepth, 1, 3);
        CreatedOn = createdOn;
        TtlDays = Math.Clamp(ttlDays, MinTtlDays, MaxTtlDays);
    }

    public string RumorId { get; }

    public string SourceContext { get; }

    public string TopicCode { get; }

    public string NarrativeText { get; }

    public int SpreadDepth { get; }

    public GameDate CreatedOn { get; }

    public int TtlDays { get; }

    public bool IsActive(GameDate currentDay) =>
        currentDay.DayNumber <= CreatedOn.DayNumber + TtlDays;
}
