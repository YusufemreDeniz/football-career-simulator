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
        string? checkpointLabel,
        int? transferWindowStatus = null,
        int? transferWindowOpenedOnDayNumber = null,
        int? transferWindowClosesOnDayNumber = null)
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

        var currentDate = GameDate.FromDayNumber(currentDayNumber);
        TransferWindow? transferWindow = transferWindowStatus is null
            ? TransferWindow.Open(currentDate)
            : TransferWindow.Rehydrate(
                (TransferWindowStatus)transferWindowStatus.Value,
                transferWindowOpenedOnDayNumber is null
                    ? null
                    : GameDate.FromDayNumber(transferWindowOpenedOnDayNumber.Value),
                transferWindowClosesOnDayNumber is null
                    ? null
                    : GameDate.FromDayNumber(transferWindowClosesOnDayNumber.Value));

        return WorldTimeline.Rehydrate(
            currentDate,
            new SimulationStepId(lastCommittedStepId),
            rootSeed,
            rngVersion,
            rngDrawCount,
            planningPeriod,
            transferWindow);
    }
}
