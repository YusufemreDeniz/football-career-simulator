using FootballCareerSimulator.Application.ClubGovernance.Infrastructure;
using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.ContractRegistration.Infrastructure;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.Transfer.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Domain.ClubGovernance;

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
        TransferWindowCloseService windowClose,
        AiClubTransferSimulationService aiSimulation,
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
        WindowClose = windowClose;
        AiSimulation = aiSimulation;
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

    public TransferWindowCloseService WindowClose { get; }

    public AiClubTransferSimulationService AiSimulation { get; }

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
        IPlayerContractProposalStore? proposalStore = null,
        ITransferWindowQuery? transferWindow = null,
        ClubTransferBudgetService? transferBudget = null,
        ClubWageBudgetService? wageBudget = null,
        IClubRegistryStore? clubRegistry = null,
        IFreeAgentStore? freeAgentStore = null,
        PromiseInvalidationService? promiseInvalidation = null,
        TransferMemoryService? transferMemory = null,
        ClubHistoryMemoryService? clubHistoryMemory = null,
        RelationshipEvaluationService? relationships = null,
        ITrainingPhysicalStateStore? trainingStore = null)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(clubSquad);

        var window = transferWindow ?? AlwaysOpenTransferWindowQuery.Instance;
        var needs = needStore ?? new InMemoryTransferNeedStore();
        var shortlist = shortlistStore ?? new InMemoryShortlistStore();
        var targets = targetStore ?? new InMemoryTransferTargetStore();
        var processes = processStore ?? new InMemoryTransferProcessStore();
        var offers = offerStore ?? new InMemoryClubOfferStore();
        var proposals = proposalStore ?? new InMemoryPlayerContractProposalStore();
        var needService = new TransferNeedService(needs, contractStore, squadStore);
        var shortlistService = new ShortlistTargetService(shortlist, targets, needs);
        var processService = new TransferProcessService(
            processes,
            targets,
            needs,
            managerCareerStore,
            window,
            offers,
            transferBudget,
            proposals,
            wageBudget);
        var proposalService = new PlayerContractProposalService(
            proposals,
            processes,
            managerCareerStore,
            window,
            wageBudget);
        var completionService = new TransferCompletionService(
            processes,
            proposals,
            offers,
            registration,
            clubSquad,
            managerCareerStore,
            window,
            transferBudget,
            wageBudget,
            promiseInvalidation,
            transferMemory,
            clubHistoryMemory,
            relationships,
            trainingStore,
            squadStore);

        var clubs = clubRegistry
            ?? new InMemoryClubRegistryStore(LeagueClubRegistry.CreateMvpLeague());
        var freeAgents = freeAgentStore ?? new InMemoryFreeAgentStore();
        var clubOfferService = new ClubOfferService(
            offers,
            processes,
            managerCareerStore,
            window,
            transferBudget);

        var aiSimulation = new AiClubTransferSimulationService(
            clubs,
            managerCareerStore,
            freeAgents,
            contractStore,
            squadStore,
            needs,
            processes,
            window,
            transferBudget,
            wageBudget,
            needService,
            shortlistService,
            processService,
            clubOfferService,
            proposalService,
            completionService);

        return new TransferModule(
            needs,
            shortlist,
            targets,
            processes,
            offers,
            proposals,
            needService,
            shortlistService,
            processService,
            clubOfferService,
            proposalService,
            completionService,
            new TransferWindowCloseService(processes, offers, proposals, transferBudget),
            aiSimulation,
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
