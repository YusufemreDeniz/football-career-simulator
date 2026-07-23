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
        IShortlistStore shortlistStore,
        ITransferTargetStore targetStore,
        ITransferProcessStore processStore,
        TransferNeedService needs,
        ShortlistTargetService shortlistTargets,
        TransferProcessService processes,
        TransferNeedQueryService queries)
    {
        NeedStore = needStore;
        ShortlistStore = shortlistStore;
        TargetStore = targetStore;
        ProcessStore = processStore;
        Needs = needs;
        ShortlistTargets = shortlistTargets;
        Processes = processes;
        Queries = queries;
    }

    public ITransferNeedStore NeedStore { get; }

    public IShortlistStore ShortlistStore { get; }

    public ITransferTargetStore TargetStore { get; }

    public ITransferProcessStore ProcessStore { get; }

    public TransferNeedService Needs { get; }

    public ShortlistTargetService ShortlistTargets { get; }

    public TransferProcessService Processes { get; }

    public TransferNeedQueryService Queries { get; }

    public static TransferModule Create(
        IContractStore contractStore,
        IClubSquadStore squadStore,
        IManagerCareerStore managerCareerStore,
        ITransferNeedStore? needStore = null,
        IShortlistStore? shortlistStore = null,
        ITransferTargetStore? targetStore = null,
        ITransferProcessStore? processStore = null)
    {
        var needs = needStore ?? new InMemoryTransferNeedStore();
        var shortlist = shortlistStore ?? new InMemoryShortlistStore();
        var targets = targetStore ?? new InMemoryTransferTargetStore();
        var processes = processStore ?? new InMemoryTransferProcessStore();
        return new TransferModule(
            needs,
            shortlist,
            targets,
            processes,
            new TransferNeedService(needs, contractStore, squadStore),
            new ShortlistTargetService(shortlist, targets, needs),
            new TransferProcessService(processes, targets, needs, managerCareerStore),
            new TransferNeedQueryService(needs, shortlist, targets, processes, managerCareerStore));
    }
}
