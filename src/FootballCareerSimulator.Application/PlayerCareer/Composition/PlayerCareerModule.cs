using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

namespace FootballCareerSimulator.Application.PlayerCareer.Composition;

public sealed class PlayerCareerModule
{
    public PlayerCareerModule(
        IPlayerCareerStore store,
        PlayerCareerDevelopmentService development,
        PlayerCareerQueryService queries)
    {
        Store = store;
        Development = development;
        Queries = queries;
    }

    public IPlayerCareerStore Store { get; }

    public PlayerCareerDevelopmentService Development { get; }

    public PlayerCareerQueryService Queries { get; }

    public static PlayerCareerModule Create(
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore,
        ITrainingPhysicalStateStore? trainingStore = null,
        IPlayerCareerStore? store = null,
        ContractRegistrationService? contracts = null)
    {
        var careerStore = store ?? new InMemoryPlayerCareerStore();
        return new PlayerCareerModule(
            careerStore,
            new PlayerCareerDevelopmentService(careerStore, trainingStore, contracts),
            new PlayerCareerQueryService(careerStore, managerCareerStore, timelineStore));
    }
}
