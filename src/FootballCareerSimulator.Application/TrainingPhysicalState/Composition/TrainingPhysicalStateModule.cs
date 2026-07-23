using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Services;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Composition;

public sealed class TrainingPhysicalStateModule
{
    public TrainingPhysicalStateModule(
        ITrainingPhysicalStateStore store,
        SetWeeklyTrainingPlanHandler setWeeklyPlan,
        TrainingQueryService queries)
    {
        Store = store;
        SetWeeklyPlan = setWeeklyPlan;
        Queries = queries;
    }

    public ITrainingPhysicalStateStore Store { get; }

    public SetWeeklyTrainingPlanHandler SetWeeklyPlan { get; }

    public TrainingQueryService Queries { get; }

    public ICommandIdempotencyReset IdempotencyReset => SetWeeklyPlan;

    public static TrainingPhysicalStateModule Create(
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore,
        ITrainingPhysicalStateStore? store = null)
    {
        var trainingStore = store ?? new InMemoryTrainingPhysicalStateStore();
        return new TrainingPhysicalStateModule(
            trainingStore,
            new SetWeeklyTrainingPlanHandler(trainingStore, managerCareerStore, timelineStore),
            new TrainingQueryService(trainingStore, managerCareerStore, timelineStore));
    }
}
