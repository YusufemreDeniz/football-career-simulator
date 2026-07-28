using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

namespace FootballCareerSimulator.Application.TeamPreparation.Composition;

public sealed class TeamPreparationModule
{
    public TeamPreparationModule(
        IMatchSelectionStore selectionStore,
        IClubSquadStore squadStore,
        ITacticPlanStore tacticPlanStore,
        ClubSquadService? clubSquadService,
        TacticPlanService tacticPlans,
        ApproveDefaultMatchSelectionHandler approveDefaultSelection,
        ApproveMatchSelectionHandler approveSelection,
        SwapStarterWithBenchHandler swapStarterWithBench,
        MatchSelectionQueryService selectionQueries,
        PreMatchPromiseTensionQueryService promiseTension,
        SquadQueryService squadQueries,
        TacticPlanQueryService tacticQueries)
    {
        SelectionStore = selectionStore;
        SquadStore = squadStore;
        TacticPlanStore = tacticPlanStore;
        ClubSquad = clubSquadService;
        TacticPlans = tacticPlans;
        ApproveDefaultSelection = approveDefaultSelection;
        ApproveSelection = approveSelection;
        SwapStarterWithBench = swapStarterWithBench;
        SelectionQueries = selectionQueries;
        PromiseTension = promiseTension;
        SquadQueries = squadQueries;
        TacticQueries = tacticQueries;
        IdempotencyResets =
        [
            approveDefaultSelection,
            approveSelection,
            swapStarterWithBench,
        ];
    }

    public IMatchSelectionStore SelectionStore { get; }

    public IClubSquadStore SquadStore { get; }

    public ITacticPlanStore TacticPlanStore { get; }

    public ClubSquadService? ClubSquad { get; }

    public TacticPlanService TacticPlans { get; }

    public ApproveDefaultMatchSelectionHandler ApproveDefaultSelection { get; }

    public ApproveMatchSelectionHandler ApproveSelection { get; }

    public SwapStarterWithBenchHandler SwapStarterWithBench { get; }

    public MatchSelectionQueryService SelectionQueries { get; }

    public PreMatchPromiseTensionQueryService PromiseTension { get; }

    public SquadQueryService SquadQueries { get; }

    public TacticPlanQueryService TacticQueries { get; }

    public IReadOnlyList<ICommandIdempotencyReset> IdempotencyResets { get; }

    public ICommandIdempotencyReset IdempotencyReset => ApproveDefaultSelection;

    public void BindPromiseStore(IPromiseStore promiseStore) =>
        PromiseTension.BindPromiseStore(promiseStore);

    public static TeamPreparationModule Create(
        ILeagueCompetitionStore competitionStore,
        IManagerCareerStore managerCareerStore,
        IMatchSelectionStore? selectionStore = null,
        ITrainingPhysicalStateStore? trainingStore = null,
        IWorldTimelineStore? timelineStore = null,
        IContractStore? contractStore = null,
        IPlayerCareerStore? playerCareerStore = null,
        IClubSquadStore? squadStore = null,
        ITacticPlanStore? tacticPlanStore = null,
        IPromiseStore? promiseStore = null)
    {
        var store = selectionStore ?? new InMemoryMatchSelectionStore();
        var clubSquadStore = squadStore ?? new InMemoryClubSquadStore();
        var tactics = tacticPlanStore ?? new InMemoryTacticPlanStore();
        ClubSquadService? clubSquadService = null;
        if (contractStore is not null && playerCareerStore is not null)
        {
            clubSquadService = new ClubSquadService(clubSquadStore, contractStore, playerCareerStore);
        }

        var selectionQueries = new MatchSelectionQueryService(store, competitionStore, managerCareerStore);
        var promiseTension = new PreMatchPromiseTensionQueryService(
            selectionQueries,
            store,
            clubSquadStore,
            promiseStore);

        return new TeamPreparationModule(
            store,
            clubSquadStore,
            tactics,
            clubSquadService,
            new TacticPlanService(tactics),
            new ApproveDefaultMatchSelectionHandler(
                store,
                competitionStore,
                trainingStore,
                timelineStore,
                clubSquadStore),
            new ApproveMatchSelectionHandler(
                store,
                competitionStore,
                trainingStore,
                timelineStore,
                clubSquadStore),
            new SwapStarterWithBenchHandler(
                store,
                competitionStore,
                trainingStore,
                timelineStore,
                clubSquadStore),
            selectionQueries,
            promiseTension,
            new SquadQueryService(clubSquadStore, playerCareerStore),
            new TacticPlanQueryService(tactics, managerCareerStore));
    }
}
