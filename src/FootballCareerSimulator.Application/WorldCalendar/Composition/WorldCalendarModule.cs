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
        OpenPlanningPeriodHandler openPlanningPeriod,
        CompletePlanningPeriodHandler completePlanningPeriod,
        WorldCalendarQueryService queries,
        WorldCalendarGameSessionService? gameSession = null)
    {
        TimelineStore = timelineStore;
        BlockerAggregator = blockerAggregator;
        AdvanceSimulationTime = advanceSimulationTime;
        OpenPlanningPeriod = openPlanningPeriod;
        CompletePlanningPeriod = completePlanningPeriod;
        Queries = queries;
        GameSession = gameSession;
    }

    public IWorldTimelineStore TimelineStore { get; }

    public TimeAdvanceBlockerAggregator BlockerAggregator { get; }

    public AdvanceSimulationTimeHandler AdvanceSimulationTime { get; }

    public OpenPlanningPeriodHandler OpenPlanningPeriod { get; }

    public CompletePlanningPeriodHandler CompletePlanningPeriod { get; }

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
        IWorldCalendarPersistence? persistence = null,
        IWorldTimelineStore? timelineStore = null)
    {
        var store = timelineStore
            ?? new InMemoryWorldTimelineStore(WorldTimeline.Create(startingDate, rootSeed, rngVersion));
        var sources = blockerSources?.ToArray() ?? Array.Empty<ITimeAdvanceBlockerSource>();
        var aggregator = new TimeAdvanceBlockerAggregator(sources);
        var advanceHandler = new AdvanceSimulationTimeHandler(store, aggregator);
        var openPlanningHandler = new OpenPlanningPeriodHandler(store);
        var completePlanningHandler = new CompletePlanningPeriodHandler(store);
        var idempotencyResets = new ICommandIdempotencyReset[]
        {
            advanceHandler,
            openPlanningHandler,
            completePlanningHandler,
        };

        WorldCalendarGameSessionService? gameSession = persistence is null
            ? null
            : new WorldCalendarGameSessionService(store, persistence, idempotencyResets);

        return new WorldCalendarModule(
            store,
            aggregator,
            advanceHandler,
            openPlanningHandler,
            completePlanningHandler,
            new WorldCalendarQueryService(store, aggregator),
            gameSession);
    }
}
