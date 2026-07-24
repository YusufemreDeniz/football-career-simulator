namespace FootballCareerSimulator.Application.Career.Services;

using FootballCareerSimulator.Application.Career.Commands;
using FootballCareerSimulator.Application.Career.Ports;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.Discipline.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

public sealed class CareerGameSessionService
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly IClubRegistryStore _clubRegistryStore;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IMatchSelectionStore _matchSelectionStore;
    private readonly IClubSquadStore _clubSquadStore;
    private readonly ITacticPlanStore _tacticPlanStore;
    private readonly ITransferNeedStore _transferNeedStore;
    private readonly IShortlistStore _shortlistStore;
    private readonly ITransferTargetStore _transferTargetStore;
    private readonly ITransferProcessStore _transferProcessStore;
    private readonly IClubOfferStore _clubOfferStore;
    private readonly IPlayerContractProposalStore _playerContractProposalStore;
    private readonly IPromiseStore _promiseStore;
    private readonly IMemoryStore _memoryStore;
    private readonly IRelationshipStore _relationshipStore;
    private readonly IDecisionRequestStore _decisionRequestStore;
    private readonly IDialogueSessionStore _dialogueSessionStore;
    private readonly IDisciplinaryActionStore _disciplinaryActionStore;
    private readonly ITrainingPhysicalStateStore _trainingStore;
    private readonly IPlayerCareerStore _playerCareerStore;
    private readonly IContractStore _contractStore;
    private readonly IFreeAgentStore _freeAgentStore;
    private readonly ICareerPersistence _persistence;
    private readonly IReadOnlyList<ICommandIdempotencyReset> _idempotencyResets;

    public CareerGameSessionService(
        IWorldTimelineStore timelineStore,
        ILeagueCompetitionStore competitionStore,
        IClubRegistryStore clubRegistryStore,
        IManagerCareerStore managerCareerStore,
        IMatchSelectionStore matchSelectionStore,
        IClubSquadStore clubSquadStore,
        ITacticPlanStore tacticPlanStore,
        ITransferNeedStore transferNeedStore,
        IShortlistStore shortlistStore,
        ITransferTargetStore transferTargetStore,
        ITransferProcessStore transferProcessStore,
        IClubOfferStore clubOfferStore,
        IPlayerContractProposalStore playerContractProposalStore,
        IPromiseStore promiseStore,
        IMemoryStore memoryStore,
        IRelationshipStore relationshipStore,
        IDecisionRequestStore decisionRequestStore,
        IDialogueSessionStore dialogueSessionStore,
        IDisciplinaryActionStore disciplinaryActionStore,
        ITrainingPhysicalStateStore trainingStore,
        IPlayerCareerStore playerCareerStore,
        IContractStore contractStore,
        IFreeAgentStore freeAgentStore,
        ICareerPersistence persistence,
        IEnumerable<ICommandIdempotencyReset> idempotencyResets)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _clubRegistryStore = clubRegistryStore ?? throw new ArgumentNullException(nameof(clubRegistryStore));
        _managerCareerStore = managerCareerStore ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _matchSelectionStore = matchSelectionStore ?? throw new ArgumentNullException(nameof(matchSelectionStore));
        _clubSquadStore = clubSquadStore ?? throw new ArgumentNullException(nameof(clubSquadStore));
        _tacticPlanStore = tacticPlanStore ?? throw new ArgumentNullException(nameof(tacticPlanStore));
        _transferNeedStore = transferNeedStore ?? throw new ArgumentNullException(nameof(transferNeedStore));
        _shortlistStore = shortlistStore ?? throw new ArgumentNullException(nameof(shortlistStore));
        _transferTargetStore = transferTargetStore ?? throw new ArgumentNullException(nameof(transferTargetStore));
        _transferProcessStore = transferProcessStore ?? throw new ArgumentNullException(nameof(transferProcessStore));
        _clubOfferStore = clubOfferStore ?? throw new ArgumentNullException(nameof(clubOfferStore));
        _playerContractProposalStore = playerContractProposalStore
            ?? throw new ArgumentNullException(nameof(playerContractProposalStore));
        _promiseStore = promiseStore ?? throw new ArgumentNullException(nameof(promiseStore));
        _memoryStore = memoryStore ?? throw new ArgumentNullException(nameof(memoryStore));
        _relationshipStore = relationshipStore ?? throw new ArgumentNullException(nameof(relationshipStore));
        _decisionRequestStore = decisionRequestStore
            ?? throw new ArgumentNullException(nameof(decisionRequestStore));
        _dialogueSessionStore = dialogueSessionStore
            ?? throw new ArgumentNullException(nameof(dialogueSessionStore));
        _disciplinaryActionStore = disciplinaryActionStore
            ?? throw new ArgumentNullException(nameof(disciplinaryActionStore));
        _trainingStore = trainingStore ?? throw new ArgumentNullException(nameof(trainingStore));
        _playerCareerStore = playerCareerStore ?? throw new ArgumentNullException(nameof(playerCareerStore));
        _contractStore = contractStore ?? throw new ArgumentNullException(nameof(contractStore));
        _freeAgentStore = freeAgentStore ?? throw new ArgumentNullException(nameof(freeAgentStore));
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
            _clubSquadStore.Squads,
            _freeAgentStore.FreeAgents,
            _tacticPlanStore.Plans,
            _transferNeedStore.Needs,
            _shortlistStore.Entries,
            _transferTargetStore.Targets,
            _transferProcessStore.Processes,
            _clubOfferStore.Offers,
            _playerContractProposalStore.Proposals,
            _promiseStore.Promises,
            _memoryStore.Memories,
            _relationshipStore.Relationships,
            _decisionRequestStore.Requests,
            _dialogueSessionStore.Sessions,
            _disciplinaryActionStore.Actions);

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
        _freeAgentStore.ReplaceAll(loaded.FreeAgents);
        _tacticPlanStore.ReplaceAll(loaded.TacticPlans);
        _transferNeedStore.ReplaceAll(loaded.TransferNeeds);
        _shortlistStore.ReplaceAll(loaded.ShortlistEntries);
        _transferTargetStore.ReplaceAll(loaded.TransferTargets);
        _transferProcessStore.ReplaceAll(loaded.TransferProcesses);
        _clubOfferStore.ReplaceAll(loaded.ClubOffers);
        _playerContractProposalStore.ReplaceAll(loaded.ContractProposals);
        _promiseStore.ReplaceAll(loaded.Promises);
        _memoryStore.ReplaceAll(loaded.Memories);
        _relationshipStore.ReplaceAll(loaded.Relationships);
        _decisionRequestStore.ReplaceAll(loaded.DecisionRequests);
        _dialogueSessionStore.ReplaceAll(loaded.DialogueSessions);
        _disciplinaryActionStore.ReplaceAll(loaded.DisciplinaryActions);

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
