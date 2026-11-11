namespace FootballCareerSimulator.Application.WorldCalendar.Ports;

public interface ITimeAdvanceBlockerSource
{
    string SourceContext { get; }

    IReadOnlyList<TimeAdvanceBlockerDescriptor> GetActiveBlockers();
}

public sealed record TimeAdvanceBlockerDescriptor(
    string BlockerTypeCode,
    string DescriptionCode,
    bool IsHardBlocker);
