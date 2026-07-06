using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.WorldCalendar;

namespace FootballCareerSimulator.Infrastructure.WorldCalendar;

internal static class WorldCalendarSnapshotMapper
{
    public static WorldTimeline ToDomain(
        int currentDayNumber,
        long lastCommittedStepId,
        int rootSeed,
        string rngVersion,
        int rngDrawCount,
        long? planningPeriodId,
        int? planningStartDayNumber,
        int? planningExpectedEndDayNumber,
        int? planningStatus,
        int? planningCompletedAtDayNumber,
        string? checkpointLabel)
    {
        PlanningPeriod? planningPeriod = null;

        if (planningPeriodId is not null
            && planningStartDayNumber is not null
            && planningStatus is not null)
        {
            planningPeriod = PlanningPeriod.Rehydrate(
                new PlanningPeriodId(planningPeriodId.Value),
                GameDate.FromDayNumber(planningStartDayNumber.Value),
                planningExpectedEndDayNumber is null ? null : GameDate.FromDayNumber(planningExpectedEndDayNumber.Value),
                (PlanningPeriodStatus)planningStatus.Value,
                planningCompletedAtDayNumber is null ? null : GameDate.FromDayNumber(planningCompletedAtDayNumber.Value));
        }

        _ = checkpointLabel;

        return WorldTimeline.Rehydrate(
            GameDate.FromDayNumber(currentDayNumber),
            new SimulationStepId(lastCommittedStepId),
            rootSeed,
            rngVersion,
            rngDrawCount,
            planningPeriod);
    }
}
