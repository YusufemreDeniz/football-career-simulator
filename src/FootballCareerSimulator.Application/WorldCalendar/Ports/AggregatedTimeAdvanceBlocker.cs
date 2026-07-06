namespace FootballCareerSimulator.Application.WorldCalendar.Ports;

public sealed record AggregatedTimeAdvanceBlocker(
    string SourceContext,
    string BlockerTypeCode,
    string DescriptionCode,
    bool IsHardBlocker);
