namespace FootballCareerSimulator.Application.WorldCalendar.Composition;

using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Services;
using FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Manuel composition root (D-348). Test host ve ilerideki headless runner bu modülü kullanır.
/// </summary>
public sealed class WorldCalendarModule
{
    public WorldCalendarModule(
        IWorldTimelineStore timelineStore,
        TimeAdvanceBlockerAggregator blockerAggregator,
        AdvanceSimulationTimeHandler advanceSimulationTime,
        WorldCalendarQueryService queries)
    {
        TimelineStore = timelineStore;
        BlockerAggregator = blockerAggregator;
        AdvanceSimulationTime = advanceSimulationTime;
        Queries = queries;
    }

    public IWorldTimelineStore TimelineStore { get; }

    public TimeAdvanceBlockerAggregator BlockerAggregator { get; }

    public AdvanceSimulationTimeHandler AdvanceSimulationTime { get; }

    public WorldCalendarQueryService Queries { get; }

    public static WorldCalendarModule Create(
        GameDate startingDate,
        int rootSeed = 0,
        string rngVersion = "1",
        IEnumerable<ITimeAdvanceBlockerSource>? blockerSources = null)
    {
        var timeline = WorldTimeline.Create(startingDate, rootSeed, rngVersion);
        var store = new InMemoryWorldTimelineStore(timeline);
        var sources = blockerSources?.ToArray() ?? Array.Empty<ITimeAdvanceBlockerSource>();
        var aggregator = new TimeAdvanceBlockerAggregator(sources);

        return new WorldCalendarModule(
            store,
            aggregator,
            new AdvanceSimulationTimeHandler(store, aggregator),
            new WorldCalendarQueryService(store, aggregator));
    }
}
