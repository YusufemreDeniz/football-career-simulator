namespace FootballCareerSimulator.Application.WorldCalendar;

using System.Diagnostics;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation;
using FootballCareerSimulator.Simulation.WorldCalendar;

public sealed record WorldCalendarSimulationReport(
    int Seed,
    string RandomContextVersion,
    int SeasonCount,
    int SimulatedDayCount,
    int FinalDayNumber,
    string CanonicalStateHash,
    long ElapsedMilliseconds,
    long MemoryBeforeBytes,
    long MemoryAfterBytes,
    int CommittedEventCount);

/// <summary>
/// Production World &amp; Calendar headless koşucusu (docs/19_PRODUCTION_IMPLEMENTATION_PLAN.md Kart 4).
/// </summary>
public static class WorldCalendarHeadlessRunner
{
    public const int DefaultDaysPerSeason = 365;

    public static WorldCalendarSimulationReport Run(
        int seed,
        int seasonCount,
        GameDate? startingDate = null,
        int daysPerSeason = DefaultDaysPerSeason)
    {
        if (seasonCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seasonCount), seasonCount, "Season count cannot be negative.");
        }

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
        var stopwatch = Stopwatch.StartNew();

        var startDate = startingDate ?? GameDate.FromCalendarDate(2026, 7, 1);
        var module = WorldCalendarModule.Create(startDate, rootSeed: seed, rngVersion: SimulationRandomContext.Version);
        var random = new SimulationRandomContext(seed);
        var totalDays = checked(seasonCount * daysPerSeason);
        var committedEvents = 0;

        for (var dayIndex = 1; dayIndex <= totalDays; dayIndex++)
        {
            var targetDate = startDate.AddDays(dayIndex);
            var commandId = DeterministicGuidFactory.Create(seed, -dayIndex);

            var result = module.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(commandId, targetDate.DayNumber));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Time advancement blocked on day {dayIndex}: {result.Blockers[0].DescriptionCode}");
            }

            module.TimelineStore.Timeline.RecordRngDraw();
            _ = random.NextInt(0, 1_000);

            committedEvents += result.RaisedEventTypes.Count;
        }

        stopwatch.Stop();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);
        var timeline = module.TimelineStore.Timeline;

        return new WorldCalendarSimulationReport(
            Seed: seed,
            RandomContextVersion: SimulationRandomContext.Version,
            SeasonCount: seasonCount,
            SimulatedDayCount: totalDays,
            FinalDayNumber: timeline.CurrentDate.DayNumber,
            CanonicalStateHash: WorldTimelineCanonicalStateHasher.ComputeHash(timeline),
            ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
            MemoryBeforeBytes: memoryBefore,
            MemoryAfterBytes: memoryAfter,
            CommittedEventCount: committedEvents);
    }
}
