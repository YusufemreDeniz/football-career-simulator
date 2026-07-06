namespace FootballCareerSimulator.Application.WorldCalendar.Composition;

using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Services;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation;

/// <summary>
/// Manuel composition root (D-348). Test host ve ilerideki headless runner bu modülü kullanır.
/// </summary>
public sealed class WorldCalendarModule
{
    public WorldCalendarModule(
        IWorldTimelineStore timelineStore,
        TimeAdvanceBlockerAggregator blockerAggregator,
        AdvanceSimulationTimeHandler advanceSimulationTime,
        WorldCalendarQueryService queries,
        WorldCalendarGameSessionService? gameSession = null)
    {
        TimelineStore = timelineStore;
        BlockerAggregator = blockerAggregator;
        AdvanceSimulationTime = advanceSimulationTime;
        Queries = queries;
        GameSession = gameSession;
    }

    public IWorldTimelineStore TimelineStore { get; }

    public TimeAdvanceBlockerAggregator BlockerAggregator { get; }

    public AdvanceSimulationTimeHandler AdvanceSimulationTime { get; }

    public WorldCalendarQueryService Queries { get; }

    public WorldCalendarGameSessionService? GameSession { get; }

    public static WorldCalendarModule CreateNewGame(int rootSeed = 42, IWorldCalendarPersistence? persistence = null) =>
        Create(
            GameDate.FromCalendarDate(2026, 7, 1),
            rootSeed,
            SimulationRandomContext.Version,
            persistence: persistence);

    public static WorldCalendarModule Create(
        GameDate startingDate,
        int rootSeed = 0,
        string rngVersion = "1",
        IEnumerable<ITimeAdvanceBlockerSource>? blockerSources = null,
        IWorldCalendarPersistence? persistence = null)
    {
        var timeline = WorldTimeline.Create(startingDate, rootSeed, rngVersion);
        var store = new InMemoryWorldTimelineStore(timeline);
        var sources = blockerSources?.ToArray() ?? Array.Empty<ITimeAdvanceBlockerSource>();
        var aggregator = new TimeAdvanceBlockerAggregator(sources);
        var advanceHandler = new AdvanceSimulationTimeHandler(store, aggregator);
        WorldCalendarGameSessionService? gameSession = persistence is null
            ? null
            : new WorldCalendarGameSessionService(store, advanceHandler, persistence);

        return new WorldCalendarModule(
            store,
            aggregator,
            advanceHandler,
            new WorldCalendarQueryService(store, aggregator),
            gameSession);
    }
}
