using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

namespace FootballCareerSimulator.Application.TeamPreparation.Composition;

public sealed class TeamPreparationModule
{
    public TeamPreparationModule(
        IMatchSelectionStore selectionStore,
        ApproveDefaultMatchSelectionHandler approveDefaultSelection,
        MatchSelectionQueryService selectionQueries,
        SquadQueryService squadQueries)
    {
        SelectionStore = selectionStore;
        ApproveDefaultSelection = approveDefaultSelection;
        SelectionQueries = selectionQueries;
        SquadQueries = squadQueries;
    }

    public IMatchSelectionStore SelectionStore { get; }

    public ApproveDefaultMatchSelectionHandler ApproveDefaultSelection { get; }

    public MatchSelectionQueryService SelectionQueries { get; }

    public SquadQueryService SquadQueries { get; }

    public ICommandIdempotencyReset IdempotencyReset => ApproveDefaultSelection;

    public static TeamPreparationModule Create(
        ILeagueCompetitionStore competitionStore,
        IManagerCareerStore managerCareerStore,
        IMatchSelectionStore? selectionStore = null)
    {
        var store = selectionStore ?? new InMemoryMatchSelectionStore();
        return new TeamPreparationModule(
            store,
            new ApproveDefaultMatchSelectionHandler(store, competitionStore),
            new MatchSelectionQueryService(store, competitionStore, managerCareerStore),
            new SquadQueryService());
    }
}
