using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.Transfer.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Application.Transfer.Services;

namespace FootballCareerSimulator.Application.Transfer.Composition;

public sealed class TransferModule
{
    public TransferModule(
        ITransferNeedStore needStore,
        TransferNeedService needs,
        TransferNeedQueryService queries)
    {
        NeedStore = needStore;
        Needs = needs;
        Queries = queries;
    }

    public ITransferNeedStore NeedStore { get; }

    public TransferNeedService Needs { get; }

    public TransferNeedQueryService Queries { get; }

    public static TransferModule Create(
        IContractStore contractStore,
        IClubSquadStore squadStore,
        IManagerCareerStore managerCareerStore,
        ITransferNeedStore? needStore = null)
    {
        var store = needStore ?? new InMemoryTransferNeedStore();
        return new TransferModule(
            store,
            new TransferNeedService(store, contractStore, squadStore),
            new TransferNeedQueryService(store, managerCareerStore));
    }
}
