namespace FootballCareerSimulator.Application.WorldCalendar.Services;

using FootballCareerSimulator.Application.WorldCalendar.Ports;

/// <summary>
/// docs/19_PRODUCTION_IMPLEMENTATION_PLAN.md Bölüm 5.6 Blocker Aggregator deseni.
/// Henüz var olmayan context'ler test stub'ları ile temsil edilir.
/// </summary>
public sealed class TimeAdvanceBlockerAggregator
{
    private readonly IReadOnlyList<ITimeAdvanceBlockerSource> _sources;

    public TimeAdvanceBlockerAggregator(IEnumerable<ITimeAdvanceBlockerSource> sources)
    {
        _sources = sources?.ToArray() ?? throw new ArgumentNullException(nameof(sources));
    }

    public IReadOnlyList<AggregatedTimeAdvanceBlocker> GetActiveBlockers()
    {
        return _sources
            .SelectMany(source => source.GetActiveBlockers()
                .Select(blocker => new AggregatedTimeAdvanceBlocker(
                    source.SourceContext,
                    blocker.BlockerTypeCode,
                    blocker.DescriptionCode,
                    blocker.IsHardBlocker)))
            .OrderBy(blocker => blocker.SourceContext, StringComparer.Ordinal)
            .ThenBy(blocker => blocker.BlockerTypeCode, StringComparer.Ordinal)
            .ThenBy(blocker => blocker.DescriptionCode, StringComparer.Ordinal)
            .ToArray();
    }

    public bool HasHardBlocker() => GetActiveBlockers().Any(blocker => blocker.IsHardBlocker);
}
