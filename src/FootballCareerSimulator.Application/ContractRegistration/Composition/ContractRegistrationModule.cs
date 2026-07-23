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
        ContractRegistrationService registration,
        ContractQueryService queries)
    {
        Store = store;
        Registration = registration;
        Queries = queries;
    }

    public IContractStore Store { get; }

    public ContractRegistrationService Registration { get; }

    public ContractQueryService Queries { get; }

    public static ContractRegistrationModule Create(
        IPlayerCareerStore playerCareerStore,
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore,
        IContractStore? store = null)
    {
        var contractStore = store ?? new InMemoryContractStore();
        return new ContractRegistrationModule(
            contractStore,
            new ContractRegistrationService(contractStore, playerCareerStore),
            new ContractQueryService(contractStore, managerCareerStore, timelineStore));
    }
}
