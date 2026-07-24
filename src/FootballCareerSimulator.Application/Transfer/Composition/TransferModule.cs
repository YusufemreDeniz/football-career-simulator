using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Services;
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
        IClubOfferStore offerStore,
        IPlayerContractProposalStore proposalStore,
        TransferNeedService needs,
        ShortlistTargetService shortlistTargets,
        TransferProcessService processes,
        ClubOfferService clubOffers,
        PlayerContractProposalService contractProposals,
        TransferCompletionService completion,
        TransferNeedQueryService queries)
    {
        NeedStore = needStore;
        ShortlistStore = shortlistStore;
        TargetStore = targetStore;
        ProcessStore = processStore;
        OfferStore = offerStore;
        ProposalStore = proposalStore;
        Needs = needs;
        ShortlistTargets = shortlistTargets;
        Processes = processes;
        ClubOffers = clubOffers;
        ContractProposals = contractProposals;
        Completion = completion;
        Queries = queries;
    }

    public ITransferNeedStore NeedStore { get; }

    public IShortlistStore ShortlistStore { get; }

    public ITransferTargetStore TargetStore { get; }

    public ITransferProcessStore ProcessStore { get; }

    public IClubOfferStore OfferStore { get; }

    public IPlayerContractProposalStore ProposalStore { get; }

    public TransferNeedService Needs { get; }

    public ShortlistTargetService ShortlistTargets { get; }

    public TransferProcessService Processes { get; }

    public ClubOfferService ClubOffers { get; }

    public PlayerContractProposalService ContractProposals { get; }

    public TransferCompletionService Completion { get; }

    public TransferNeedQueryService Queries { get; }

    public static TransferModule Create(
        IContractStore contractStore,
        IClubSquadStore squadStore,
        IManagerCareerStore managerCareerStore,
        ContractRegistrationService registration,
        ClubSquadService clubSquad,
        ITransferNeedStore? needStore = null,
        IShortlistStore? shortlistStore = null,
        ITransferTargetStore? targetStore = null,
        ITransferProcessStore? processStore = null,
        IClubOfferStore? offerStore = null,
        IPlayerContractProposalStore? proposalStore = null)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(clubSquad);

        var needs = needStore ?? new InMemoryTransferNeedStore();
        var shortlist = shortlistStore ?? new InMemoryShortlistStore();
        var targets = targetStore ?? new InMemoryTransferTargetStore();
        var processes = processStore ?? new InMemoryTransferProcessStore();
        var offers = offerStore ?? new InMemoryClubOfferStore();
        var proposals = proposalStore ?? new InMemoryPlayerContractProposalStore();
        return new TransferModule(
            needs,
            shortlist,
            targets,
            processes,
            offers,
            proposals,
            new TransferNeedService(needs, contractStore, squadStore),
            new ShortlistTargetService(shortlist, targets, needs),
            new TransferProcessService(processes, targets, needs, managerCareerStore),
            new ClubOfferService(offers, processes, managerCareerStore),
            new PlayerContractProposalService(proposals, processes, managerCareerStore),
            new TransferCompletionService(
                processes,
                proposals,
                registration,
                clubSquad,
                managerCareerStore),
            new TransferNeedQueryService(
                needs,
                shortlist,
                targets,
                processes,
                offers,
                proposals,
                managerCareerStore));
    }
}
