using FootballCareerSimulator.Application.ContractRegistration.Infrastructure;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

namespace FootballCareerSimulator.Application.ContractRegistration.Composition;

public sealed class ContractRegistrationModule
{
    public ContractRegistrationModule(
        IContractStore store,
        IFreeAgentStore freeAgentStore,
        ContractRegistrationService registration,
        ContractQueryService queries)
    {
        Store = store;
        FreeAgentStore = freeAgentStore;
        Registration = registration;
        Queries = queries;
    }

    public IContractStore Store { get; }

    public IFreeAgentStore FreeAgentStore { get; }

    public ContractRegistrationService Registration { get; }

    public ContractQueryService Queries { get; }

    public static ContractRegistrationModule Create(
        IPlayerCareerStore playerCareerStore,
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore,
        IContractStore? store = null,
        IFreeAgentStore? freeAgentStore = null)
    {
        var contractStore = store ?? new InMemoryContractStore();
        var agents = freeAgentStore ?? new InMemoryFreeAgentStore();
        return new ContractRegistrationModule(
            contractStore,
            agents,
            new ContractRegistrationService(contractStore, agents, playerCareerStore),
            new ContractQueryService(contractStore, agents, managerCareerStore, timelineStore));
    }
}
