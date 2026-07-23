namespace FootballCareerSimulator.Application.Career.Services;

using FootballCareerSimulator.Application.Career.Commands;
using FootballCareerSimulator.Application.Career.Ports;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

public sealed class CareerGameSessionService
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly IClubRegistryStore _clubRegistryStore;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IMatchSelectionStore _matchSelectionStore;
    private readonly IClubSquadStore _clubSquadStore;
    private readonly ITrainingPhysicalStateStore _trainingStore;
    private readonly IPlayerCareerStore _playerCareerStore;
    private readonly IContractStore _contractStore;
    private readonly ICareerPersistence _persistence;
    private readonly IReadOnlyList<ICommandIdempotencyReset> _idempotencyResets;

    public CareerGameSessionService(
        IWorldTimelineStore timelineStore,
        ILeagueCompetitionStore competitionStore,
        IClubRegistryStore clubRegistryStore,
        IManagerCareerStore managerCareerStore,
        IMatchSelectionStore matchSelectionStore,
        IClubSquadStore clubSquadStore,
        ITrainingPhysicalStateStore trainingStore,
        IPlayerCareerStore playerCareerStore,
        IContractStore contractStore,
        ICareerPersistence persistence,
        IEnumerable<ICommandIdempotencyReset> idempotencyResets)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _clubRegistryStore = clubRegistryStore ?? throw new ArgumentNullException(nameof(clubRegistryStore));
        _managerCareerStore = managerCareerStore ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _matchSelectionStore = matchSelectionStore ?? throw new ArgumentNullException(nameof(matchSelectionStore));
        _clubSquadStore = clubSquadStore ?? throw new ArgumentNullException(nameof(clubSquadStore));
        _trainingStore = trainingStore ?? throw new ArgumentNullException(nameof(trainingStore));
        _playerCareerStore = playerCareerStore ?? throw new ArgumentNullException(nameof(playerCareerStore));
        _contractStore = contractStore ?? throw new ArgumentNullException(nameof(contractStore));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _idempotencyResets = idempotencyResets?.ToArray()
            ?? throw new ArgumentNullException(nameof(idempotencyResets));
    }

    public SaveCareerGameResult Save(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var timeline = _timelineStore.Timeline;
        var league = _competitionStore.League;
        var clubRegistry = _clubRegistryStore.Registry;
        var managerCareer = _managerCareerStore.Career;
        var matchSelections = _matchSelectionStore.Selections;
        _persistence.Save(
            filePath,
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            _trainingStore.Plans,
            _trainingStore.PhysicalStates,
            _playerCareerStore.Careers,
            _contractStore.Contracts,
            _clubSquadStore.Squads);

        var fixtureCount = league.Seasons.Sum(season => season.Fixtures.Count);

        return new SaveCareerGameResult(
            Succeeded: true,
            SavePath: filePath,
            SavedDayNumber: timeline.CurrentDate.DayNumber,
            SavedFixtureCount: fixtureCount);
    }

    public LoadCareerGameResult Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var loaded = _persistence.Load(filePath);
        _timelineStore.Replace(loaded.Timeline);
        _competitionStore.Replace(loaded.League);
        _clubRegistryStore.Replace(loaded.ClubRegistry);
        _managerCareerStore.Replace(loaded.ManagerCareer);
        _matchSelectionStore.ReplaceAll(loaded.MatchSelections);
        _trainingStore.ReplaceAll(loaded.TrainingPlans, loaded.PhysicalStates);
        _playerCareerStore.ReplaceAll(loaded.PlayerCareers);
        _contractStore.ReplaceAll(loaded.Contracts);
        _clubSquadStore.ReplaceAll(loaded.ClubSquads);

        foreach (var reset in _idempotencyResets)
        {
            reset.ResetIdempotencyCache();
        }

        var fixtureCount = loaded.League.Seasons.Sum(season => season.Fixtures.Count);

        return new LoadCareerGameResult(
            Succeeded: true,
            SavePath: filePath,
            LoadedDayNumber: loaded.Timeline.CurrentDate.DayNumber,
            LoadedFixtureCount: fixtureCount,
            WasMigrated: loaded.WasMigrated);
    }
}
