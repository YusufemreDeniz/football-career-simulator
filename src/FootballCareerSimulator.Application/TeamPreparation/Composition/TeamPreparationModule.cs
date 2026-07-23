using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
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
        ClubSquadService? clubSquadService,
        ApproveDefaultMatchSelectionHandler approveDefaultSelection,
        MatchSelectionQueryService selectionQueries,
        SquadQueryService squadQueries)
    {
        SelectionStore = selectionStore;
        SquadStore = squadStore;
        ClubSquad = clubSquadService;
        ApproveDefaultSelection = approveDefaultSelection;
        SelectionQueries = selectionQueries;
        SquadQueries = squadQueries;
    }

    public IMatchSelectionStore SelectionStore { get; }

    public IClubSquadStore SquadStore { get; }

    public ClubSquadService? ClubSquad { get; }

    public ApproveDefaultMatchSelectionHandler ApproveDefaultSelection { get; }

    public MatchSelectionQueryService SelectionQueries { get; }

    public SquadQueryService SquadQueries { get; }

    public ICommandIdempotencyReset IdempotencyReset => ApproveDefaultSelection;

    public static TeamPreparationModule Create(
        ILeagueCompetitionStore competitionStore,
        IManagerCareerStore managerCareerStore,
        IMatchSelectionStore? selectionStore = null,
        ITrainingPhysicalStateStore? trainingStore = null,
        IWorldTimelineStore? timelineStore = null,
        IContractStore? contractStore = null,
        IPlayerCareerStore? playerCareerStore = null,
        IClubSquadStore? squadStore = null)
    {
        var store = selectionStore ?? new InMemoryMatchSelectionStore();
        var clubSquadStore = squadStore ?? new InMemoryClubSquadStore();
        ClubSquadService? clubSquadService = null;
        if (contractStore is not null && playerCareerStore is not null)
        {
            clubSquadService = new ClubSquadService(clubSquadStore, contractStore, playerCareerStore);
        }

        return new TeamPreparationModule(
            store,
            clubSquadStore,
            clubSquadService,
            new ApproveDefaultMatchSelectionHandler(
                store,
                competitionStore,
                trainingStore,
                timelineStore,
                clubSquadStore),
            new MatchSelectionQueryService(store, competitionStore, managerCareerStore),
            new SquadQueryService(clubSquadStore, playerCareerStore));
    }
}
