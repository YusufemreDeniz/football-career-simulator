namespace FootballCareerSimulator.Application.Career.Services;

using FootballCareerSimulator.Application.Career.Commands;
using FootballCareerSimulator.Application.Career.Ports;
using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.Discipline.Ports;
using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;

public sealed class CareerGameSessionService
{
    public const int MaxRollingAutosaveCount = 3;

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
    private readonly IEventEffectIdempotencyRegistry? _eventEffectRegistry;
    private readonly IScheduledEvaluationStore? _scheduledEvaluationStore;
    private readonly IClubFinanceLedgerStore? _clubFinanceLedgerStore;
    private readonly IDualPhaseTacticPlanStore? _dualPhaseTacticPlanStore;
    private readonly FootballCareerSimulator.Application.Continental.IContinentalTournamentStore? _continentalStore;

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
        IEnumerable<ICommandIdempotencyReset> idempotencyResets,
        IEventEffectIdempotencyRegistry? eventEffectRegistry = null,
        IScheduledEvaluationStore? scheduledEvaluationStore = null,
        IClubFinanceLedgerStore? clubFinanceLedgerStore = null,
        IDualPhaseTacticPlanStore? dualPhaseTacticPlanStore = null,
        FootballCareerSimulator.Application.Continental.IContinentalTournamentStore? continentalStore = null)
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
        _eventEffectRegistry = eventEffectRegistry;
        _scheduledEvaluationStore = scheduledEvaluationStore;
        _clubFinanceLedgerStore = clubFinanceLedgerStore;
        _dualPhaseTacticPlanStore = dualPhaseTacticPlanStore;
        _continentalStore = continentalStore;
    }

    public static IReadOnlyList<string> GetRollingAutosavePaths(string primaryFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryFilePath);
        var directory = Path.GetDirectoryName(primaryFilePath) ?? string.Empty;
        var stem = Path.GetFileNameWithoutExtension(primaryFilePath);
        var extension = Path.GetExtension(primaryFilePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".db";
        }

        var paths = new string[MaxRollingAutosaveCount];
        for (var i = 0; i < MaxRollingAutosaveCount; i++)
        {
            var fileName = $"{stem}.autosave{i + 1}{extension}";
            paths[i] = string.IsNullOrWhiteSpace(directory)
                ? fileName
                : Path.Combine(directory, fileName);
        }

        return paths;
    }

    public static int CountAvailableAutosaves(string primaryFilePath) =>
        GetRollingAutosavePaths(primaryFilePath).Count(File.Exists);

    public static DateTimeOffset? GetLatestAutosaveWriteUtc(string primaryFilePath)
    {
        DateTimeOffset? latest = null;
        foreach (var path in GetRollingAutosavePaths(primaryFilePath))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var writeUtc = new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);
            if (latest is null || writeUtc > latest.Value)
            {
                latest = writeUtc;
            }
        }

        return latest;
    }

    public SaveCareerGameResult Save(
        string filePath,
        HubNarrativeUiState? hubNarrativeUiState = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        RotatePreviousSaveToRollingAutosaves(filePath);

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
            _disciplinaryActionStore.Actions,
            _eventEffectRegistry?.SnapshotKeys(),
            _scheduledEvaluationStore?.Items,
            hubNarrativeUiState,
            _clubFinanceLedgerStore?.Ledgers,
            _dualPhaseTacticPlanStore?.Plans,
            _matchSelectionStore.LineupTemplates,
            _continentalStore?.Record);

        SyncPrimaryToLatestRollingAutosaveIfMissing(filePath);

        var fixtureCount = league.Seasons.Sum(season => season.Fixtures.Count);

        return new SaveCareerGameResult(
            Succeeded: true,
            SavePath: filePath,
            SavedDayNumber: timeline.CurrentDate.DayNumber,
            SavedFixtureCount: fixtureCount,
            RollingAutosaveCount: CountAvailableAutosaves(filePath));
    }

    public SaveCareerGameResult SaveRollingAutosave(
        string primaryFilePath,
        HubNarrativeUiState? hubNarrativeUiState = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryFilePath);
        var slots = GetRollingAutosavePaths(primaryFilePath);
        RotateSlotFiles(slots);

        var targetSlot = slots[0];
        var directory = Path.GetDirectoryName(targetSlot);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var timeline = _timelineStore.Timeline;
        var league = _competitionStore.League;
        var clubRegistry = _clubRegistryStore.Registry;
        var managerCareer = _managerCareerStore.Career;
        var matchSelections = _matchSelectionStore.Selections;
        _persistence.Save(
            targetSlot,
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
            _disciplinaryActionStore.Actions,
            _eventEffectRegistry?.SnapshotKeys(),
            _scheduledEvaluationStore?.Items,
            hubNarrativeUiState,
            _clubFinanceLedgerStore?.Ledgers,
            _dualPhaseTacticPlanStore?.Plans,
            _matchSelectionStore.LineupTemplates,
            _continentalStore?.Record);

        var fixtureCount = league.Seasons.Sum(season => season.Fixtures.Count);

        return new SaveCareerGameResult(
            Succeeded: true,
            SavePath: targetSlot,
            SavedDayNumber: timeline.CurrentDate.DayNumber,
            SavedFixtureCount: fixtureCount,
            RollingAutosaveCount: CountAvailableAutosaves(primaryFilePath));
    }

    public LoadCareerGameResult Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        Exception? primaryException = null;
        if (File.Exists(filePath))
        {
            try
            {
                var loaded = _persistence.Load(filePath);
                ApplyLoadedState(loaded);
                var fixtureCount = loaded.League.Seasons.Sum(season => season.Fixtures.Count);

                return new LoadCareerGameResult(
                    Succeeded: true,
                    SavePath: filePath,
                    LoadedDayNumber: loaded.Timeline.CurrentDate.DayNumber,
                    LoadedFixtureCount: fixtureCount,
                    WasMigrated: loaded.WasMigrated,
                    HubNarrativeUiState: loaded.HubNarrativeUiState,
                    RecoveredFromBackup: false,
                    RecoverySourcePath: null);
            }
            catch (Exception ex)
            {
                primaryException = ex;
            }
        }

        foreach (var backupPath in GetRollingAutosavePaths(filePath))
        {
            if (!File.Exists(backupPath))
            {
                continue;
            }

            try
            {
                var recovered = _persistence.Load(backupPath);
                ApplyLoadedState(recovered);
                TryRestorePrimaryFileFromBackup(backupPath, filePath);
                var fixtureCount = recovered.League.Seasons.Sum(season => season.Fixtures.Count);

                return new LoadCareerGameResult(
                    Succeeded: true,
                    SavePath: filePath,
                    LoadedDayNumber: recovered.Timeline.CurrentDate.DayNumber,
                    LoadedFixtureCount: fixtureCount,
                    WasMigrated: recovered.WasMigrated,
                    HubNarrativeUiState: recovered.HubNarrativeUiState,
                    RecoveredFromBackup: true,
                    RecoverySourcePath: backupPath);
            }
            catch
            {
                // Sonraki sağlam yedeği dene.
            }
        }

        if (primaryException is not null)
        {
            throw primaryException;
        }

        var directLoad = _persistence.Load(filePath);
        ApplyLoadedState(directLoad);
        return new LoadCareerGameResult(
            Succeeded: true,
            SavePath: filePath,
            LoadedDayNumber: directLoad.Timeline.CurrentDate.DayNumber,
            LoadedFixtureCount: directLoad.League.Seasons.Sum(season => season.Fixtures.Count),
            WasMigrated: directLoad.WasMigrated,
            HubNarrativeUiState: directLoad.HubNarrativeUiState);
    }

    private void ApplyLoadedState(CareerLoadResult loaded)
    {
        _timelineStore.Replace(loaded.Timeline);
        _competitionStore.Replace(loaded.League);
        _clubRegistryStore.Replace(loaded.ClubRegistry);
        _managerCareerStore.Replace(loaded.ManagerCareer);
        _matchSelectionStore.ReplaceAll(loaded.MatchSelections);
        if (loaded.LineupTemplates is { Count: > 0 })
        {
            _matchSelectionStore.ReplaceLineupTemplates(loaded.LineupTemplates);
        }
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
        _clubFinanceLedgerStore?.ReplaceAll(
            loaded.ClubFinanceLedgers ?? Array.Empty<FootballCareerSimulator.Domain.ClubGovernance.ClubFinanceLedger>());
        _dualPhaseTacticPlanStore?.ReplaceAll(
            loaded.DualPhaseTacticPlans ?? Array.Empty<FootballCareerSimulator.Domain.TeamPreparation.DualPhaseTacticPlan>());

        foreach (var reset in _idempotencyResets)
        {
            reset.ResetIdempotencyCache();
        }

        _eventEffectRegistry?.ReplaceAll(loaded.EventEffectProcessingKeys ?? Array.Empty<string>());
        _scheduledEvaluationStore?.ReplaceAll(loaded.ScheduledEvaluations ?? Array.Empty<ScheduledEvaluation>());
        _continentalStore?.Replace(loaded.ContinentalTournament);
    }

    private static void RotatePreviousSaveToRollingAutosaves(string primaryFilePath)
    {
        if (!File.Exists(primaryFilePath))
        {
            return;
        }

        try
        {
            var slots = GetRollingAutosavePaths(primaryFilePath);
            RotateSlotFiles(slots);
            File.Copy(primaryFilePath, slots[0], overwrite: true);
        }
        catch
        {
            // Yedekleme hatası ana kaydı engellemez.
        }
    }

    private static void SyncPrimaryToLatestRollingAutosaveIfMissing(string primaryFilePath)
    {
        if (!File.Exists(primaryFilePath))
        {
            return;
        }

        try
        {
            var slots = GetRollingAutosavePaths(primaryFilePath);
            if (!File.Exists(slots[0]))
            {
                File.Copy(primaryFilePath, slots[0], overwrite: true);
            }
        }
        catch
        {
            // Yedek senkronu sessizce geçilir.
        }
    }

    private static void RotateSlotFiles(IReadOnlyList<string> slots)
    {
        for (var i = slots.Count - 1; i >= 1; i--)
        {
            var source = slots[i - 1];
            var destination = slots[i];
            if (File.Exists(source))
            {
                File.Copy(source, destination, overwrite: true);
            }
        }
    }

    private static void TryRestorePrimaryFileFromBackup(string backupPath, string primaryFilePath)
    {
        try
        {
            var tempRestore = primaryFilePath + ".restore.tmp";
            File.Copy(backupPath, tempRestore, overwrite: true);
            File.Move(tempRestore, primaryFilePath, overwrite: true);
        }
        catch
        {
            // Kurtarma bellekte tamamlandı; disk geri yazımı en iyi çaba olarak kalır.
        }
    }
}
