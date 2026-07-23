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
        TransferNeedService needs,
        ShortlistTargetService shortlistTargets,
        TransferNeedQueryService queries)
    {
        NeedStore = needStore;
        ShortlistStore = shortlistStore;
        TargetStore = targetStore;
        Needs = needs;
        ShortlistTargets = shortlistTargets;
        Queries = queries;
    }

    public ITransferNeedStore NeedStore { get; }

    public IShortlistStore ShortlistStore { get; }

    public ITransferTargetStore TargetStore { get; }

    public TransferNeedService Needs { get; }

    public ShortlistTargetService ShortlistTargets { get; }

    public TransferNeedQueryService Queries { get; }

    public static TransferModule Create(
        IContractStore contractStore,
        IClubSquadStore squadStore,
        IManagerCareerStore managerCareerStore,
        ITransferNeedStore? needStore = null,
        IShortlistStore? shortlistStore = null,
        ITransferTargetStore? targetStore = null)
    {
        var needs = needStore ?? new InMemoryTransferNeedStore();
        var shortlist = shortlistStore ?? new InMemoryShortlistStore();
        var targets = targetStore ?? new InMemoryTransferTargetStore();
        return new TransferModule(
            needs,
            shortlist,
            targets,
            new TransferNeedService(needs, contractStore, squadStore),
            new ShortlistTargetService(shortlist, targets, needs),
            new TransferNeedQueryService(needs, shortlist, targets, managerCareerStore));
    }
}
