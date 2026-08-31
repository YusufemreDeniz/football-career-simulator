using FootballCareerSimulator.Application.Career.Ports;
using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.Discipline;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Infrastructure.WorldCalendar;
using FootballCareerSimulator.Simulation.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Infrastructure.Career;

public sealed class CareerSqlitePersistence : ICareerPersistence
{
    public void Save(
        string filePath,
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareer managerCareer,
        IReadOnlyList<MatchSelection> matchSelections,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans,
        IReadOnlyList<PlayerPhysicalState> physicalStates,
        IReadOnlyList<PlayerCareerAggregate> playerCareers,
        IReadOnlyList<PlayerContract> contracts,
        IReadOnlyList<ClubSquad> clubSquads,
        IReadOnlyList<PlayerFreeAgency> freeAgents,
        IReadOnlyList<TacticPlan> tacticPlans,
        IReadOnlyList<TransferNeed> transferNeeds,
        IReadOnlyList<ShortlistEntry> shortlistEntries,
        IReadOnlyList<TransferTarget> transferTargets,
        IReadOnlyList<TransferProcess> transferProcesses,
        IReadOnlyList<ClubOffer> clubOffers,
        IReadOnlyList<PlayerContractProposal> contractProposals,
        IReadOnlyList<Promise> promises,
        IReadOnlyList<MemoryRecord> memories,
        IReadOnlyList<RelationshipRecord>? relationships = null,
        IReadOnlyList<DecisionRequest>? decisionRequests = null,
        IReadOnlyList<DialogueSession>? dialogueSessions = null,
        IReadOnlyList<DisciplinaryAction>? disciplinaryActions = null,
        IReadOnlyList<string>? eventEffectProcessingKeys = null,
        IReadOnlyList<ScheduledEvaluation>? scheduledEvaluations = null,
        HubNarrativeUiState? hubNarrativeUiState = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(league);
        ArgumentNullException.ThrowIfNull(clubRegistry);
        ArgumentNullException.ThrowIfNull(managerCareer);
        ArgumentNullException.ThrowIfNull(matchSelections);
        ArgumentNullException.ThrowIfNull(trainingPlans);
        ArgumentNullException.ThrowIfNull(physicalStates);
        ArgumentNullException.ThrowIfNull(playerCareers);
        ArgumentNullException.ThrowIfNull(contracts);
        ArgumentNullException.ThrowIfNull(clubSquads);
        ArgumentNullException.ThrowIfNull(freeAgents);
        ArgumentNullException.ThrowIfNull(tacticPlans);
        ArgumentNullException.ThrowIfNull(transferNeeds);
        ArgumentNullException.ThrowIfNull(shortlistEntries);
        ArgumentNullException.ThrowIfNull(transferTargets);
        ArgumentNullException.ThrowIfNull(transferProcesses);
        ArgumentNullException.ThrowIfNull(clubOffers);
        ArgumentNullException.ThrowIfNull(contractProposals);
        ArgumentNullException.ThrowIfNull(promises);
        ArgumentNullException.ThrowIfNull(memories);
        relationships ??= Array.Empty<RelationshipRecord>();
        decisionRequests ??= Array.Empty<DecisionRequest>();
        dialogueSessions ??= Array.Empty<DialogueSession>();
        disciplinaryActions ??= Array.Empty<DisciplinaryAction>();
        eventEffectProcessingKeys ??= Array.Empty<string>();
        scheduledEvaluations ??= Array.Empty<ScheduledEvaluation>();
        hubNarrativeUiState ??= HubNarrativeUiState.Empty;
        var hubNarrativeCanonical = BuildHubNarrativeCanonicalText(hubNarrativeUiState);

        var canonicalHash = CareerCanonicalStateHasher.ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            freeAgents,
            tacticPlans,
            transferNeeds,
            shortlistEntries,
            transferTargets,
            transferProcesses,
            clubOffers,
            contractProposals,
            promises,
            memories,
            relationships,
            decisionRequests,
            dialogueSessions,
            disciplinaryActions,
            eventEffectProcessingKeys,
            scheduledEvaluations,
            hubNarrativeCanonical);
        var tempPath = filePath + ".tmp";

        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        using (var connection = new SqliteConnection($"Data Source={tempPath}"))
        {
            connection.Open();
            using var transaction = connection.BeginTransaction();

            CreateCurrentSchema(connection, transaction);
            InsertManifest(connection, transaction, canonicalHash);
            InsertTimeline(connection, transaction, timeline);
            InsertCompetition(connection, transaction, league);
            InsertClubs(connection, transaction, clubRegistry);
            InsertManager(connection, transaction, managerCareer);
            InsertManagerEmploymentHistory(connection, transaction, managerCareer.EmploymentHistory);
            InsertMatchSelections(connection, transaction, matchSelections);
            InsertTrainingPlans(connection, transaction, trainingPlans);
            InsertPhysicalStates(connection, transaction, physicalStates);
            InsertPlayerCareers(connection, transaction, playerCareers);
            InsertContracts(connection, transaction, contracts);
            InsertClubSquads(connection, transaction, clubSquads);
            InsertFreeAgents(connection, transaction, freeAgents);
            InsertTacticPlans(connection, transaction, tacticPlans);
            InsertTransferNeeds(connection, transaction, transferNeeds);
            InsertShortlistEntries(connection, transaction, shortlistEntries);
            InsertTransferTargets(connection, transaction, transferTargets);
            InsertTransferProcesses(connection, transaction, transferProcesses);
            InsertClubOffers(connection, transaction, clubOffers);
            InsertPlayerContractProposals(connection, transaction, contractProposals);
            InsertPromises(connection, transaction, promises);
            InsertMemories(connection, transaction, memories);
            InsertRelationships(connection, transaction, relationships);
            InsertDecisionRequests(connection, transaction, decisionRequests);
            InsertDialogueSessions(connection, transaction, dialogueSessions);
            InsertDisciplinaryActions(connection, transaction, disciplinaryActions);
            InsertEventEffectProcessingKeys(connection, transaction, eventEffectProcessingKeys);
            InsertScheduledEvaluations(connection, transaction, scheduledEvaluations);
            InsertHubNarrativeUiState(connection, transaction, hubNarrativeUiState);
            InsertMatchupPlanHistory(connection, transaction, hubNarrativeUiState.MatchupPlanHistory);

            transaction.Commit();
        }

        SqliteConnection.ClearAllPools();
        File.Move(tempPath, filePath, overwrite: true);
    }

    public CareerLoadResult Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Save dosyası bulunamadı: {filePath}", filePath);
        }

        var schemaVersion = ReadSchemaMetadata(filePath);

        if (schemaVersion.IsLegacySpikeSave)
        {
            throw new UnsupportedLegacySpikeSaveException();
        }

        if (schemaVersion.Version > ProductionWorldCalendarSaveSchema.CurrentVersion
            || schemaVersion.Version < ProductionWorldCalendarSaveSchema.MinSupportedVersion)
        {
            throw new UnsupportedSaveSchemaVersionException(schemaVersion.Version);
        }

        var wasMigrated = false;
        var version = schemaVersion.Version;

        if (version == 1)
        {
            WorldCalendarSqliteMigrator.MigrateInPlace(filePath, 1);
            wasMigrated = true;
            version = 2;
        }

        if (version == 2 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 3)
        {
            WorldCalendarSqliteMigrator.MigrateV2ToV3InPlace(filePath);
            wasMigrated = true;
            version = 3;
        }

        if (version == 3 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 4)
        {
            WorldCalendarSqliteMigrator.MigrateV3ToV4InPlace(filePath);
            wasMigrated = true;
            version = 4;
        }

        if (version == 4 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 5)
        {
            WorldCalendarSqliteMigrator.MigrateV4ToV5InPlace(filePath);
            wasMigrated = true;
            version = 5;
        }

        if (version == 5 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 6)
        {
            WorldCalendarSqliteMigrator.MigrateV5ToV6InPlace(filePath);
            wasMigrated = true;
            version = 6;
        }

        if (version == 6 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 7)
        {
            WorldCalendarSqliteMigrator.MigrateV6ToV7InPlace(filePath);
            wasMigrated = true;
            version = 7;
        }

        if (version == 7 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 8)
        {
            WorldCalendarSqliteMigrator.MigrateV7ToV8InPlace(filePath);
            wasMigrated = true;
            version = 8;
        }

        if (version == 8 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 9)
        {
            WorldCalendarSqliteMigrator.MigrateV8ToV9InPlace(filePath);
            wasMigrated = true;
            version = 9;
        }

        if (version == 9 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 10)
        {
            WorldCalendarSqliteMigrator.MigrateV9ToV10InPlace(filePath);
            wasMigrated = true;
            version = 10;
        }

        if (version == 10 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 11)
        {
            WorldCalendarSqliteMigrator.MigrateV10ToV11InPlace(filePath);
            wasMigrated = true;
            version = 11;
        }

        if (version == 11 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 12)
        {
            WorldCalendarSqliteMigrator.MigrateV11ToV12InPlace(filePath);
            wasMigrated = true;
            version = 12;
        }

        if (version == 12 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 13)
        {
            WorldCalendarSqliteMigrator.MigrateV12ToV13InPlace(filePath);
            wasMigrated = true;
            version = 13;
        }

        if (version == 13 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 14)
        {
            WorldCalendarSqliteMigrator.MigrateV13ToV14InPlace(filePath);
            wasMigrated = true;
            version = 14;
        }

        if (version == 14 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 15)
        {
            WorldCalendarSqliteMigrator.MigrateV14ToV15InPlace(filePath);
            wasMigrated = true;
            version = 15;
        }

        if (version == 15 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 16)
        {
            WorldCalendarSqliteMigrator.MigrateV15ToV16InPlace(filePath);
            wasMigrated = true;
            version = 16;
        }

        if (version == 16 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 17)
        {
            WorldCalendarSqliteMigrator.MigrateV16ToV17InPlace(filePath);
            wasMigrated = true;
            version = 17;
        }

        if (version == 17 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 18)
        {
            WorldCalendarSqliteMigrator.MigrateV17ToV18InPlace(filePath);
            wasMigrated = true;
            version = 18;
        }

        if (version == 18 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 19)
        {
            WorldCalendarSqliteMigrator.MigrateV18ToV19InPlace(filePath);
            wasMigrated = true;
            version = 19;
        }

        if (version == 19 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 20)
        {
            WorldCalendarSqliteMigrator.MigrateV19ToV20InPlace(filePath);
            wasMigrated = true;
            version = 20;
        }

        if (version == 20 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 21)
        {
            WorldCalendarSqliteMigrator.MigrateV20ToV21InPlace(filePath);
            wasMigrated = true;
            version = 21;
        }

        if (version == 21 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 22)
        {
            WorldCalendarSqliteMigrator.MigrateV21ToV22InPlace(filePath);
            wasMigrated = true;
            version = 22;
        }

        if (version == 22 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 23)
        {
            WorldCalendarSqliteMigrator.MigrateV22ToV23InPlace(filePath);
            wasMigrated = true;
            version = 23;
        }

        if (version == 23 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 24)
        {
            WorldCalendarSqliteMigrator.MigrateV23ToV24InPlace(filePath);
            wasMigrated = true;
            version = 24;
        }

        if (version == 24 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 25)
        {
            WorldCalendarSqliteMigrator.MigrateV24ToV25InPlace(filePath);
            wasMigrated = true;
            version = 25;
        }

        if (version == 25 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 26)
        {
            WorldCalendarSqliteMigrator.MigrateV25ToV26InPlace(filePath);
            wasMigrated = true;
            version = 26;
        }

        if (version == 26 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 27)
        {
            WorldCalendarSqliteMigrator.MigrateV26ToV27InPlace(filePath);
            wasMigrated = true;
            version = 27;
        }

        if (version == 27 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 28)
        {
            WorldCalendarSqliteMigrator.MigrateV27ToV28InPlace(filePath);
            wasMigrated = true;
            version = 28;
        }

        if (version == 28 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 29)
        {
            WorldCalendarSqliteMigrator.MigrateV28ToV29InPlace(filePath);
            wasMigrated = true;
            version = 29;
        }

        if (version == 29 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 30)
        {
            WorldCalendarSqliteMigrator.MigrateV29ToV30InPlace(filePath);
            wasMigrated = true;
            version = 30;
        }

        if (version == 30 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 31)
        {
            WorldCalendarSqliteMigrator.MigrateV30ToV31InPlace(filePath);
            wasMigrated = true;
            version = 31;
        }

        if (version == 31 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 32)
        {
            WorldCalendarSqliteMigrator.MigrateV31ToV32InPlace(filePath);
            wasMigrated = true;
            version = 32;
        }

        if (version == 32 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 33)
        {
            WorldCalendarSqliteMigrator.MigrateV32ToV33InPlace(filePath);
            wasMigrated = true;
            version = 33;
        }

        if (version == 33 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 34)
        {
            WorldCalendarSqliteMigrator.MigrateV33ToV34InPlace(filePath);
            wasMigrated = true;
            version = 34;
        }

        if (version == 34 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 35)
        {
            WorldCalendarSqliteMigrator.MigrateV34ToV35InPlace(filePath);
            wasMigrated = true;
            version = 35;
        }

        if (version == 35 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 36)
        {
            WorldCalendarSqliteMigrator.MigrateV35ToV36InPlace(filePath);
            wasMigrated = true;
            version = 36;
        }

        if (version == 36 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 37)
        {
            WorldCalendarSqliteMigrator.MigrateV36ToV37InPlace(filePath);
            wasMigrated = true;
            version = 37;
        }

        if (version == 37 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 38)
        {
            WorldCalendarSqliteMigrator.MigrateV37ToV38InPlace(filePath);
            wasMigrated = true;
            version = 38;
        }

        if (version == 38 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 39)
        {
            WorldCalendarSqliteMigrator.MigrateV38ToV39InPlace(filePath);
            wasMigrated = true;
            version = 39;
        }

        if (version == 39 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 40)
        {
            WorldCalendarSqliteMigrator.MigrateV39ToV40InPlace(filePath);
            wasMigrated = true;
            version = 40;
        }

        if (version == 40 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 41)
        {
            WorldCalendarSqliteMigrator.MigrateV40ToV41InPlace(filePath);
            wasMigrated = true;
            version = 41;
        }

        if (version == 41 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 42)
        {
            WorldCalendarSqliteMigrator.MigrateV41ToV42InPlace(filePath);
            wasMigrated = true;
            version = 42;
        }

        if (version == 42 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 43)
        {
            WorldCalendarSqliteMigrator.MigrateV42ToV43InPlace(filePath);
            wasMigrated = true;
            version = 43;
        }

        if (version == 43 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 44)
        {
            WorldCalendarSqliteMigrator.MigrateV43ToV44InPlace(filePath);
            wasMigrated = true;
            version = 44;
        }

        if (version == 44 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 45)
        {
            WorldCalendarSqliteMigrator.MigrateV44ToV45InPlace(filePath);
            wasMigrated = true;
            version = 45;
        }

        if (version == 45 && ProductionWorldCalendarSaveSchema.CurrentVersion >= 46)
        {
            WorldCalendarSqliteMigrator.MigrateV45ToV46InPlace(filePath);
            wasMigrated = true;
            version = 46;
        }

        if (wasMigrated)
        {
            RepairManifestHash(filePath);
        }

        return ReadCurrentVersion(filePath, wasMigrated, version);
    }

    private static void CreateCurrentSchema(SqliteConnection connection, SqliteTransaction transaction)
    {
        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE ProductionSaveManifest (
                SchemaVersion INTEGER NOT NULL,
                SaveFormatId TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                CanonicalStateHash TEXT NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE WorldTimelineState (
                SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                CurrentDayNumber INTEGER NOT NULL,
                LastCommittedStepId INTEGER NOT NULL,
                RootSeed INTEGER NOT NULL,
                RngVersion TEXT NOT NULL,
                RngDrawCount INTEGER NOT NULL,
                CheckpointLabel TEXT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE PlanningPeriodState (
                SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                PlanningPeriodId INTEGER NOT NULL,
                StartDayNumber INTEGER NOT NULL,
                ExpectedEndDayNumber INTEGER NULL,
                Status INTEGER NOT NULL,
                CompletedAtDayNumber INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE TransferWindowState (
                SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                Status INTEGER NOT NULL,
                OpenedOnDayNumber INTEGER NULL,
                ClosesOnDayNumber INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE LeagueCompetitionState (
                SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                CompetitionId INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE CompetitionSeasonState (
                SeasonId INTEGER PRIMARY KEY,
                PreseasonStartDayNumber INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                ActiveStartedAtDayNumber INTEGER NULL,
                CompletedAtDayNumber INTEGER NULL,
                ArchivedAtDayNumber INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE SeasonParticipantState (
                SeasonId INTEGER NOT NULL,
                ClubId INTEGER NOT NULL,
                PRIMARY KEY (SeasonId, ClubId)
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE FixtureState (
                FixtureId INTEGER PRIMARY KEY,
                SeasonId INTEGER NOT NULL,
                HomeClubId INTEGER NOT NULL,
                AwayClubId INTEGER NOT NULL,
                Round INTEGER NOT NULL,
                ScheduledDayNumber INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                HomeGoals INTEGER NULL,
                AwayGoals INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE ClubState (
                ClubId INTEGER PRIMARY KEY,
                DisplayName TEXT NOT NULL,
                ClubCode TEXT NOT NULL,
                SportiveStrength INTEGER NOT NULL,
                TransferBudgetLimit INTEGER NOT NULL,
                ReservedTransferFunds INTEGER NOT NULL,
                SpentTransferFunds INTEGER NOT NULL,
                WageBudgetLimit INTEGER NOT NULL,
                ReservedWeeklyWage INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE ManagerCareerState (
                SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                ManagerId INTEGER NOT NULL,
                DisplayName TEXT NOT NULL,
                EmployedClubId INTEGER NULL,
                EmploymentStartedDayNumber INTEGER NULL,
                SeasonExpectation INTEGER NULL,
                BoardConfidence INTEGER NULL,
                EmploymentRiskBand INTEGER NULL,
                LastAssessedFixtureId INTEGER NULL,
                LastAssessmentReasonCode TEXT NULL,
                EmploymentStatus INTEGER NOT NULL,
                EmploymentEndReason INTEGER NULL,
                LastClubId INTEGER NULL,
                DismissedDueToFixtureId INTEGER NULL,
                DismissedAtDayNumber INTEGER NULL,
                PendingOfferId INTEGER NULL,
                PendingOfferClubId INTEGER NULL,
                PendingOfferStatus INTEGER NULL,
                PendingOfferCreatedDayNumber INTEGER NULL,
                ManagerReputation INTEGER NOT NULL DEFAULT 50,
                LastReputationReasonCode TEXT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE MatchSelectionState (
                FixtureId INTEGER NOT NULL,
                ClubId INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                StartingSlotsCsv TEXT NOT NULL,
                BenchSlotsCsv TEXT NOT NULL,
                PRIMARY KEY (FixtureId, ClubId)
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE ManagerEmploymentHistoryState (
                Ordinal INTEGER PRIMARY KEY,
                ClubId INTEGER NOT NULL,
                StartedDayNumber INTEGER NOT NULL,
                EndedDayNumber INTEGER NOT NULL,
                EndReason INTEGER NOT NULL,
                FinalBoardConfidence INTEGER NOT NULL,
                CausationFixtureId INTEGER NULL,
                FinalAssessmentReasonCode TEXT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE WeeklyTrainingPlanState (
                ClubId INTEGER PRIMARY KEY,
                Focus INTEGER NOT NULL,
                Intensity INTEGER NOT NULL,
                RestApproach INTEGER NOT NULL,
                SetAtDayNumber INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE PlayerPhysicalState (
                ClubId INTEGER NOT NULL,
                SlotIndex INTEGER NOT NULL,
                Fatigue INTEGER NOT NULL,
                Fitness INTEGER NOT NULL,
                InjurySeverity INTEGER NOT NULL,
                InjuredUntilDayNumber INTEGER NULL,
                PRIMARY KEY (ClubId, SlotIndex)
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE PlayerCareerState (
                PlayerId INTEGER PRIMARY KEY,
                OriginClubId INTEGER NOT NULL,
                SlotIndex INTEGER NOT NULL,
                CurrentAbility INTEGER NOT NULL,
                PotentialAbility INTEGER NOT NULL,
                DevelopmentPoints INTEGER NOT NULL,
                LastDevelopedDayNumber INTEGER NULL,
                BirthYear INTEGER NOT NULL,
                LastAgedCalendarYear INTEGER NULL,
                LifecycleStatus INTEGER NOT NULL,
                RetiredDayNumber INTEGER NULL,
                RetirementReason INTEGER NULL,
                Generation INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE PlayerContractState (
                ContractId INTEGER PRIMARY KEY,
                PlayerId INTEGER NOT NULL,
                ClubId INTEGER NOT NULL,
                StartDayNumber INTEGER NOT NULL,
                EndDayNumber INTEGER NOT NULL,
                WeeklyWage INTEGER NOT NULL,
                Status INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE ClubSquadMemberState (
                ClubId INTEGER NOT NULL,
                PlayerId INTEGER NOT NULL,
                SlotIndex INTEGER NOT NULL,
                JoinedDayNumber INTEGER NOT NULL,
                PRIMARY KEY (ClubId, PlayerId)
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE PlayerFreeAgencyState (
                PlayerId INTEGER PRIMARY KEY,
                LastClubId INTEGER NOT NULL,
                BecameFreeAgentDayNumber INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE ClubTacticPlanState (
                ClubId INTEGER PRIMARY KEY,
                Formation INTEGER NOT NULL,
                Approach INTEGER NOT NULL,
                Pressing INTEGER NOT NULL,
                DefensiveLine INTEGER NOT NULL,
                PassingStyle INTEGER NOT NULL,
                LastUpdatedDayNumber INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE TransferNeedState (
                NeedId INTEGER PRIMARY KEY,
                ClubId INTEGER NOT NULL,
                Kind INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                Priority INTEGER NOT NULL,
                ReasonCode TEXT NOT NULL,
                IdentifiedDayNumber INTEGER NOT NULL,
                ClosedDayNumber INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE ShortlistEntryState (
                EntryId INTEGER PRIMARY KEY,
                ClubId INTEGER NOT NULL,
                PlayerId INTEGER NOT NULL,
                NeedId INTEGER NULL,
                Priority INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                AddedDayNumber INTEGER NOT NULL,
                ArchivedDayNumber INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE TransferTargetState (
                TargetId INTEGER PRIMARY KEY,
                NeedId INTEGER NOT NULL,
                ClubId INTEGER NOT NULL,
                PlayerId INTEGER NOT NULL,
                ShortlistEntryId INTEGER NULL,
                Status INTEGER NOT NULL,
                ListedDayNumber INTEGER NOT NULL,
                DroppedDayNumber INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE TransferProcessState (
                ProcessId INTEGER PRIMARY KEY,
                NeedId INTEGER NOT NULL,
                TargetId INTEGER NOT NULL,
                BuyingClubId INTEGER NOT NULL,
                PlayerId INTEGER NOT NULL,
                SellingClubId INTEGER NULL,
                IsFreeAgent INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                FailureReasonCode TEXT NULL,
                OpenedDayNumber INTEGER NOT NULL,
                TerminalDayNumber INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE ClubOfferState (
                OfferId INTEGER PRIMARY KEY,
                ProcessId INTEGER NOT NULL,
                Round INTEGER NOT NULL,
                OfferedFee INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                SubmittedDayNumber INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE PlayerContractProposalState (
                ProposalId INTEGER PRIMARY KEY,
                ProcessId INTEGER NOT NULL,
                Round INTEGER NOT NULL,
                WeeklyWage INTEGER NOT NULL,
                ContractYears INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                SubmittedDayNumber INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE PromiseState (
                PromiseId INTEGER PRIMARY KEY,
                Kind INTEGER NOT NULL,
                PromisorKind INTEGER NOT NULL,
                PromisorId INTEGER NOT NULL,
                PromiseeKind INTEGER NOT NULL,
                PromiseeId INTEGER NOT NULL,
                ClubId INTEGER NOT NULL,
                TargetStarts INTEGER NOT NULL,
                StartsGiven INTEGER NOT NULL,
                DeadlineDayNumber INTEGER NOT NULL,
                CreatedDayNumber INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                TerminalDayNumber INTEGER NULL,
                CountedFixtureIdsCsv TEXT NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE MemoryState (
                MemoryId INTEGER PRIMARY KEY,
                RememberingActorKind INTEGER NOT NULL,
                RememberingActorId INTEGER NOT NULL,
                SubjectKind INTEGER NOT NULL,
                SubjectId INTEGER NOT NULL,
                SourceEventKey TEXT NOT NULL,
                Category INTEGER NOT NULL,
                CreatedDayNumber INTEGER NOT NULL,
                LastReinforcedDayNumber INTEGER NOT NULL,
                BaseImportance INTEGER NOT NULL,
                CurrentInfluence INTEGER NOT NULL,
                Valence INTEGER NOT NULL,
                Visibility INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                ReinforcementCount INTEGER NOT NULL,
                RelatedPromiseId INTEGER NULL,
                RuleId TEXT NOT NULL,
                RuleVersion INTEGER NOT NULL,
                ProcessedReinforcementKeysCsv TEXT NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE RelationshipState (
                RelationshipId INTEGER PRIMARY KEY,
                ObserverKind INTEGER NOT NULL,
                ObserverId INTEGER NOT NULL,
                SubjectKind INTEGER NOT NULL,
                SubjectId INTEGER NOT NULL,
                Trust INTEGER NOT NULL,
                Respect INTEGER NOT NULL,
                ProfessionalCompatibility INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                CreatedDayNumber INTEGER NOT NULL,
                LastChangedDayNumber INTEGER NOT NULL,
                LastChangeReasonCode TEXT NULL,
                ProcessedEffectKeysCsv TEXT NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE DecisionRequestState (
                DecisionRequestId INTEGER PRIMARY KEY,
                Kind INTEGER NOT NULL,
                ManagerId INTEGER NOT NULL,
                SubjectPlayerId INTEGER NOT NULL,
                ClubId INTEGER NOT NULL,
                OpenedDayNumber INTEGER NOT NULL,
                DeadlineDayNumber INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                IsHardBlocker INTEGER NOT NULL,
                SelectedOptionCode TEXT NULL,
                ResolvedDayNumber INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE DialogueSessionState (
                DialogueSessionId INTEGER PRIMARY KEY,
                SourceDecisionRequestId INTEGER NOT NULL,
                DialogueTypeCode TEXT NOT NULL,
                ManagerId INTEGER NOT NULL,
                PrimaryParticipantPlayerId INTEGER NOT NULL,
                CreatedDayNumber INTEGER NOT NULL,
                DeadlineDayNumber INTEGER NULL,
                Status INTEGER NOT NULL,
                AvailableOptionCodesCsv TEXT NOT NULL,
                SelectedOptionCode TEXT NULL,
                ResolvedDayNumber INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE DisciplinaryActionState (
                DisciplinaryActionId INTEGER PRIMARY KEY,
                Kind INTEGER NOT NULL,
                ManagerId INTEGER NOT NULL,
                SubjectPlayerId INTEGER NOT NULL,
                ClubId INTEGER NOT NULL,
                SourceDecisionRequestId INTEGER NULL,
                AppliedDayNumber INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE EventEffectIdempotencyState (
                ProcessingKey TEXT PRIMARY KEY
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE ScheduledEvaluationState (
                ScheduledEvaluationId INTEGER PRIMARY KEY,
                EvaluationTypeCode TEXT NOT NULL,
                DueDayNumber INTEGER NOT NULL,
                SourceEventId TEXT NULL,
                Status INTEGER NOT NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE HubNarrativeUiState (
                SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                WeekStoryClosureBeat TEXT NULL,
                WeekStoryDismissOnNextAdvance INTEGER NOT NULL DEFAULT 0,
                CleanXiNamesCsv TEXT NULL,
                InjuryClearedNamesCsv TEXT NULL,
                PendingMatchTrainingFixtureId INTEGER NULL,
                PendingMatchTrainingPriorityCode TEXT NULL,
                PendingMatchTrainingModifier INTEGER NULL
            );
            """);

        ProductionSqliteCommands.ExecuteNonQuery(connection, transaction, """
            CREATE TABLE MatchupPlanNotebookState (
                SequenceIndex INTEGER PRIMARY KEY,
                DayNumber INTEGER NOT NULL,
                OpponentName TEXT NOT NULL,
                SelectionLine TEXT NOT NULL,
                ThreatKind INTEGER NOT NULL,
                PlanSignal INTEGER NOT NULL,
                OutcomeSignal INTEGER NOT NULL,
                VerdictLine TEXT NOT NULL
            );
            """);
    }

    private static void InsertManifest(SqliteConnection connection, SqliteTransaction transaction, string canonicalHash)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ProductionSaveManifest (SchemaVersion, SaveFormatId, CreatedAtUtc, CanonicalStateHash)
            VALUES ($schemaVersion, $saveFormatId, $createdAtUtc, $canonicalHash);
            """;
        command.Parameters.AddWithValue("$schemaVersion", ProductionWorldCalendarSaveSchema.CurrentVersion);
        command.Parameters.AddWithValue("$saveFormatId", ProductionWorldCalendarSaveSchema.SaveFormatId);
        command.Parameters.AddWithValue("$createdAtUtc", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$canonicalHash", canonicalHash);
        command.ExecuteNonQuery();
    }

    private static void InsertTimeline(SqliteConnection connection, SqliteTransaction transaction, WorldTimeline timeline)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO WorldTimelineState (
                SingletonId, CurrentDayNumber, LastCommittedStepId, RootSeed, RngVersion, RngDrawCount, CheckpointLabel)
            VALUES (1, $currentDayNumber, $lastCommittedStepId, $rootSeed, $rngVersion, $rngDrawCount, NULL);
            """;
        command.Parameters.AddWithValue("$currentDayNumber", timeline.CurrentDate.DayNumber);
        command.Parameters.AddWithValue("$lastCommittedStepId", timeline.LastCommittedStepId.Value);
        command.Parameters.AddWithValue("$rootSeed", timeline.RootSeed);
        command.Parameters.AddWithValue("$rngVersion", timeline.RngVersion);
        command.Parameters.AddWithValue("$rngDrawCount", timeline.RngDrawCount);
        command.ExecuteNonQuery();

        if (timeline.ActivePlanningPeriod is { } period)
        {
            using var periodCommand = connection.CreateCommand();
            periodCommand.Transaction = transaction;
            periodCommand.CommandText = """
                INSERT INTO PlanningPeriodState (
                    SingletonId, PlanningPeriodId, StartDayNumber, ExpectedEndDayNumber, Status, CompletedAtDayNumber)
                VALUES (1, $planningPeriodId, $startDayNumber, $expectedEndDayNumber, $status, $completedAtDayNumber);
                """;
            periodCommand.Parameters.AddWithValue("$planningPeriodId", period.Id.Value);
            periodCommand.Parameters.AddWithValue("$startDayNumber", period.StartDate.DayNumber);
            periodCommand.Parameters.AddWithValue("$expectedEndDayNumber", (object?)period.ExpectedEndDate?.DayNumber ?? DBNull.Value);
            periodCommand.Parameters.AddWithValue("$status", (int)period.Status);
            periodCommand.Parameters.AddWithValue("$completedAtDayNumber", (object?)period.CompletedAt?.DayNumber ?? DBNull.Value);
            periodCommand.ExecuteNonQuery();
        }

        var window = timeline.TransferWindow;
        using var windowCommand = connection.CreateCommand();
        windowCommand.Transaction = transaction;
        windowCommand.CommandText = """
            INSERT INTO TransferWindowState (
                SingletonId, Status, OpenedOnDayNumber, ClosesOnDayNumber)
            VALUES (1, $status, $openedOnDayNumber, $closesOnDayNumber);
            """;
        windowCommand.Parameters.AddWithValue("$status", (int)window.Status);
        windowCommand.Parameters.AddWithValue("$openedOnDayNumber", (object?)window.OpenedOn?.DayNumber ?? DBNull.Value);
        windowCommand.Parameters.AddWithValue("$closesOnDayNumber", (object?)window.ClosesOn?.DayNumber ?? DBNull.Value);
        windowCommand.ExecuteNonQuery();
    }

    private static void InsertCompetition(SqliteConnection connection, SqliteTransaction transaction, LeagueCompetition league)
    {
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO LeagueCompetitionState (SingletonId, CompetitionId)
                VALUES (1, $competitionId);
                """;
            command.Parameters.AddWithValue("$competitionId", league.CompetitionId.Value);
            command.ExecuteNonQuery();
        }

        foreach (var season in league.Seasons.OrderBy(season => season.SeasonId.Value))
        {
            using (var seasonCommand = connection.CreateCommand())
            {
                seasonCommand.Transaction = transaction;
                seasonCommand.CommandText = """
                    INSERT INTO CompetitionSeasonState (
                        SeasonId, PreseasonStartDayNumber, Status, ActiveStartedAtDayNumber, CompletedAtDayNumber, ArchivedAtDayNumber)
                    VALUES (
                        $seasonId, $preseasonStartDayNumber, $status, $activeStartedAtDayNumber, $completedAtDayNumber, $archivedAtDayNumber);
                    """;
                seasonCommand.Parameters.AddWithValue("$seasonId", season.SeasonId.Value);
                seasonCommand.Parameters.AddWithValue("$preseasonStartDayNumber", season.PreseasonStartDate.DayNumber);
                seasonCommand.Parameters.AddWithValue("$status", (int)season.Status);
                seasonCommand.Parameters.AddWithValue("$activeStartedAtDayNumber", (object?)season.ActiveStartedAt?.DayNumber ?? DBNull.Value);
                seasonCommand.Parameters.AddWithValue("$completedAtDayNumber", (object?)season.CompletedAt?.DayNumber ?? DBNull.Value);
                seasonCommand.Parameters.AddWithValue("$archivedAtDayNumber", (object?)season.ArchivedAt?.DayNumber ?? DBNull.Value);
                seasonCommand.ExecuteNonQuery();
            }

            foreach (var participant in season.Participants.OrderBy(participant => participant.ClubId.Value))
            {
                using var participantCommand = connection.CreateCommand();
                participantCommand.Transaction = transaction;
                participantCommand.CommandText = """
                    INSERT INTO SeasonParticipantState (SeasonId, ClubId)
                    VALUES ($seasonId, $clubId);
                    """;
                participantCommand.Parameters.AddWithValue("$seasonId", season.SeasonId.Value);
                participantCommand.Parameters.AddWithValue("$clubId", participant.ClubId.Value);
                participantCommand.ExecuteNonQuery();
            }

            foreach (var fixture in season.Fixtures.OrderBy(fixture => fixture.Id.Value))
            {
                using var fixtureCommand = connection.CreateCommand();
                fixtureCommand.Transaction = transaction;
                fixtureCommand.CommandText = """
                    INSERT INTO FixtureState (
                        FixtureId, SeasonId, HomeClubId, AwayClubId, Round, ScheduledDayNumber, Status, HomeGoals, AwayGoals)
                    VALUES (
                        $fixtureId, $seasonId, $homeClubId, $awayClubId, $round, $scheduledDayNumber, $status, $homeGoals, $awayGoals);
                    """;
                fixtureCommand.Parameters.AddWithValue("$fixtureId", fixture.Id.Value);
                fixtureCommand.Parameters.AddWithValue("$seasonId", season.SeasonId.Value);
                fixtureCommand.Parameters.AddWithValue("$homeClubId", fixture.HomeClubId.Value);
                fixtureCommand.Parameters.AddWithValue("$awayClubId", fixture.AwayClubId.Value);
                fixtureCommand.Parameters.AddWithValue("$round", fixture.Round.Value);
                fixtureCommand.Parameters.AddWithValue("$scheduledDayNumber", fixture.ScheduledDate.DayNumber);
                fixtureCommand.Parameters.AddWithValue("$status", (int)fixture.Status);
                fixtureCommand.Parameters.AddWithValue("$homeGoals", (object?)fixture.HomeGoals ?? DBNull.Value);
                fixtureCommand.Parameters.AddWithValue("$awayGoals", (object?)fixture.AwayGoals ?? DBNull.Value);
                fixtureCommand.ExecuteNonQuery();
            }
        }
    }

    private static void InsertClubs(
        SqliteConnection connection,
        SqliteTransaction transaction,
        LeagueClubRegistry clubRegistry)
    {
        foreach (var club in clubRegistry.Clubs.OrderBy(club => club.Id.Value))
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO ClubState (
                    ClubId, DisplayName, ClubCode, SportiveStrength,
                    TransferBudgetLimit, ReservedTransferFunds, SpentTransferFunds,
                    WageBudgetLimit, ReservedWeeklyWage)
                VALUES (
                    $clubId, $displayName, $clubCode, $sportiveStrength,
                    $transferBudgetLimit, $reservedTransferFunds, $spentTransferFunds,
                    $wageBudgetLimit, $reservedWeeklyWage);
                """;
            command.Parameters.AddWithValue("$clubId", club.Id.Value);
            command.Parameters.AddWithValue("$displayName", club.DisplayName);
            command.Parameters.AddWithValue("$clubCode", club.Code.Value);
            command.Parameters.AddWithValue("$sportiveStrength", club.SportiveStrength);
            command.Parameters.AddWithValue("$transferBudgetLimit", club.TransferBudgetLimit);
            command.Parameters.AddWithValue("$reservedTransferFunds", club.ReservedTransferFunds);
            command.Parameters.AddWithValue("$spentTransferFunds", club.SpentTransferFunds);
            command.Parameters.AddWithValue("$wageBudgetLimit", club.WageBudgetLimit);
            command.Parameters.AddWithValue("$reservedWeeklyWage", club.ReservedWeeklyWage);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertManager(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ManagerCareer managerCareer)
    {
        var employment = managerCareer.ActiveEmployment;

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ManagerCareerState (
                SingletonId, ManagerId, DisplayName, EmployedClubId, EmploymentStartedDayNumber,
                SeasonExpectation, BoardConfidence, EmploymentRiskBand,
                LastAssessedFixtureId, LastAssessmentReasonCode,
                EmploymentStatus, EmploymentEndReason, LastClubId,
                DismissedDueToFixtureId, DismissedAtDayNumber,
                PendingOfferId, PendingOfferClubId, PendingOfferStatus, PendingOfferCreatedDayNumber,
                ManagerReputation, LastReputationReasonCode)
            VALUES (
                1, $managerId, $displayName, $employedClubId, $employmentStartedDayNumber,
                $seasonExpectation, $boardConfidence, $riskBand,
                $lastAssessedFixtureId, $lastAssessmentReasonCode,
                $employmentStatus, $employmentEndReason, $lastClubId,
                $dismissedDueToFixtureId, $dismissedAtDayNumber,
                $pendingOfferId, $pendingOfferClubId, $pendingOfferStatus, $pendingOfferCreatedDayNumber,
                $managerReputation, $lastReputationReasonCode);
            """;
        command.Parameters.AddWithValue("$managerId", managerCareer.ManagerId.Value);
        command.Parameters.AddWithValue("$displayName", managerCareer.DisplayName);
        command.Parameters.AddWithValue(
            "$employedClubId",
            employment is null ? DBNull.Value : employment.ClubId.Value);
        command.Parameters.AddWithValue(
            "$employmentStartedDayNumber",
            employment is null ? DBNull.Value : employment.StartedAt.DayNumber);
        command.Parameters.AddWithValue(
            "$seasonExpectation",
            employment is null ? DBNull.Value : (int)employment.SeasonExpectation);
        command.Parameters.AddWithValue(
            "$boardConfidence",
            employment is null ? DBNull.Value : employment.BoardConfidence.Value);
        command.Parameters.AddWithValue(
            "$riskBand",
            employment is null ? DBNull.Value : (int)employment.RiskBand);
        command.Parameters.AddWithValue(
            "$lastAssessedFixtureId",
            employment?.LastAssessedFixtureId is FixtureId fixture
                ? fixture.Value
                : DBNull.Value);
        command.Parameters.AddWithValue(
            "$lastAssessmentReasonCode",
            (object?)employment?.LastAssessmentReasonCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$employmentStatus", (int)managerCareer.EmploymentStatus);
        command.Parameters.AddWithValue(
            "$employmentEndReason",
            managerCareer.TerminationReason is { } endReason
                ? (int)endReason
                : DBNull.Value);
        command.Parameters.AddWithValue(
            "$lastClubId",
            managerCareer.LastClubId is ClubId lastClub ? lastClub.Value : DBNull.Value);
        command.Parameters.AddWithValue(
            "$dismissedDueToFixtureId",
            managerCareer.DismissedDueToFixtureId is FixtureId dismissedFixture
                ? dismissedFixture.Value
                : DBNull.Value);
        command.Parameters.AddWithValue(
            "$dismissedAtDayNumber",
            managerCareer.DismissedAt is GameDate dismissedAt
                ? dismissedAt.DayNumber
                : DBNull.Value);
        var offer = managerCareer.PendingJobOffer;
        command.Parameters.AddWithValue(
            "$pendingOfferId",
            offer is null ? DBNull.Value : offer.Id.Value);
        command.Parameters.AddWithValue(
            "$pendingOfferClubId",
            offer is null ? DBNull.Value : offer.ClubId.Value);
        command.Parameters.AddWithValue(
            "$pendingOfferStatus",
            offer is null ? DBNull.Value : (int)offer.Status);
        command.Parameters.AddWithValue(
            "$pendingOfferCreatedDayNumber",
            offer is null ? DBNull.Value : offer.CreatedAt.DayNumber);
        command.Parameters.AddWithValue("$managerReputation", managerCareer.Reputation.Value);
        command.Parameters.AddWithValue(
            "$lastReputationReasonCode",
            (object?)managerCareer.LastReputationReasonCode ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static void InsertManagerEmploymentHistory(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<EmploymentHistoryEntry> history)
    {
        for (var index = 0; index < history.Count; index++)
        {
            var entry = history[index];
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO ManagerEmploymentHistoryState (
                    Ordinal, ClubId, StartedDayNumber, EndedDayNumber, EndReason,
                    FinalBoardConfidence, CausationFixtureId, FinalAssessmentReasonCode)
                VALUES ($ordinal, $clubId, $started, $ended, $reason, $confidence, $fixture, $assessment);
                """;
            command.Parameters.AddWithValue("$ordinal", index);
            command.Parameters.AddWithValue("$clubId", entry.ClubId.Value);
            command.Parameters.AddWithValue("$started", entry.StartedAt.DayNumber);
            command.Parameters.AddWithValue("$ended", entry.EndedAt.DayNumber);
            command.Parameters.AddWithValue("$reason", (int)entry.EndReason);
            command.Parameters.AddWithValue("$confidence", entry.FinalBoardConfidence);
            command.Parameters.AddWithValue(
                "$fixture",
                entry.CausationFixtureId is FixtureId fixture ? fixture.Value : DBNull.Value);
            command.Parameters.AddWithValue(
                "$assessment",
                (object?)entry.FinalAssessmentReasonCode ?? DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertMatchSelections(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<MatchSelection> matchSelections)
    {
        foreach (var selection in matchSelections)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO MatchSelectionState (
                    FixtureId, ClubId, Status, StartingSlotsCsv, BenchSlotsCsv)
                VALUES ($fixtureId, $clubId, $status, $startingSlots, $benchSlots);
                """;
            command.Parameters.AddWithValue("$fixtureId", selection.FixtureId.Value);
            command.Parameters.AddWithValue("$clubId", selection.ClubId.Value);
            command.Parameters.AddWithValue("$status", (int)selection.Status);
            command.Parameters.AddWithValue(
                "$startingSlots",
                string.Join(',', selection.StartingSlotIndices));
            command.Parameters.AddWithValue(
                "$benchSlots",
                string.Join(',', selection.BenchSlotIndices));
            command.ExecuteNonQuery();
        }
    }

    private static void InsertTrainingPlans(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans)
    {
        foreach (var plan in trainingPlans)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO WeeklyTrainingPlanState (
                    ClubId, Focus, Intensity, RestApproach, SetAtDayNumber)
                VALUES ($clubId, $focus, $intensity, $rest, $setAt);
                """;
            command.Parameters.AddWithValue("$clubId", plan.ClubId.Value);
            command.Parameters.AddWithValue("$focus", (int)plan.Focus);
            command.Parameters.AddWithValue("$intensity", (int)plan.Intensity);
            command.Parameters.AddWithValue("$rest", (int)plan.RestApproach);
            command.Parameters.AddWithValue("$setAt", plan.SetAt.DayNumber);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertPhysicalStates(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<PlayerPhysicalState> physicalStates)
    {
        foreach (var state in physicalStates)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO PlayerPhysicalState (
                    ClubId, SlotIndex, Fatigue, Fitness, InjurySeverity, InjuredUntilDayNumber)
                VALUES ($clubId, $slotIndex, $fatigue, $fitness, $injurySeverity, $injuredUntil);
                """;
            command.Parameters.AddWithValue("$clubId", state.ClubId.Value);
            command.Parameters.AddWithValue("$slotIndex", state.SlotIndex);
            command.Parameters.AddWithValue("$fatigue", state.Fatigue);
            command.Parameters.AddWithValue("$fitness", state.Fitness);
            command.Parameters.AddWithValue("$injurySeverity", (int)state.InjurySeverity);
            command.Parameters.AddWithValue(
                "$injuredUntil",
                state.InjuredUntilDayNumber is int until ? until : DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertPlayerCareers(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<PlayerCareerAggregate> playerCareers)
    {
        foreach (var career in playerCareers)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO PlayerCareerState (
                    PlayerId, OriginClubId, SlotIndex, CurrentAbility, PotentialAbility,
                    DevelopmentPoints, LastDevelopedDayNumber, BirthYear, LastAgedCalendarYear,
                    LifecycleStatus, RetiredDayNumber, RetirementReason, Generation)
                VALUES ($playerId, $clubId, $slotIndex, $ca, $pa, $dp, $lastDay, $birthYear, $agedYear,
                        $lifecycleStatus, $retiredDay, $retirementReason, $generation);
                """;
            command.Parameters.AddWithValue("$playerId", career.Id.Value);
            command.Parameters.AddWithValue("$clubId", career.OriginClubId.Value);
            command.Parameters.AddWithValue("$slotIndex", career.SlotIndex);
            command.Parameters.AddWithValue("$ca", career.CurrentAbility);
            command.Parameters.AddWithValue("$pa", career.PotentialAbility);
            command.Parameters.AddWithValue("$dp", career.DevelopmentPoints);
            command.Parameters.AddWithValue(
                "$lastDay",
                career.LastDevelopedOn is GameDate day ? day.DayNumber : DBNull.Value);
            command.Parameters.AddWithValue("$birthYear", career.BirthYear);
            command.Parameters.AddWithValue(
                "$agedYear",
                career.LastAgedCalendarYear is int aged ? aged : DBNull.Value);
            command.Parameters.AddWithValue("$lifecycleStatus", (int)career.LifecycleStatus);
            command.Parameters.AddWithValue(
                "$retiredDay",
                career.RetiredOn is GameDate retired ? retired.DayNumber : DBNull.Value);
            command.Parameters.AddWithValue(
                "$retirementReason",
                career.RetirementReason is PlayerRetirementReason reason ? (int)reason : DBNull.Value);
            command.Parameters.AddWithValue("$generation", career.Generation);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertContracts(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<PlayerContract> contracts)
    {
        foreach (var contract in contracts)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO PlayerContractState (
                    ContractId, PlayerId, ClubId, StartDayNumber, EndDayNumber, WeeklyWage, Status)
                VALUES ($id, $playerId, $clubId, $start, $end, $wage, $status);
                """;
            command.Parameters.AddWithValue("$id", contract.Id.Value);
            command.Parameters.AddWithValue("$playerId", contract.PlayerId.Value);
            command.Parameters.AddWithValue("$clubId", contract.ClubId.Value);
            command.Parameters.AddWithValue("$start", contract.StartDate.DayNumber);
            command.Parameters.AddWithValue("$end", contract.EndDate.DayNumber);
            command.Parameters.AddWithValue("$wage", contract.WeeklyWage);
            command.Parameters.AddWithValue("$status", (int)contract.Status);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertClubSquads(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<ClubSquad> clubSquads)
    {
        foreach (var squad in clubSquads)
        {
            foreach (var member in squad.Members)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO ClubSquadMemberState (
                        ClubId, PlayerId, SlotIndex, JoinedDayNumber)
                    VALUES ($clubId, $playerId, $slot, $joined);
                    """;
                command.Parameters.AddWithValue("$clubId", squad.ClubId.Value);
                command.Parameters.AddWithValue("$playerId", member.PlayerId.Value);
                command.Parameters.AddWithValue("$slot", member.SlotIndex);
                command.Parameters.AddWithValue("$joined", member.JoinedOn.DayNumber);
                command.ExecuteNonQuery();
            }
        }
    }

    private static void InsertFreeAgents(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<PlayerFreeAgency> freeAgents)
    {
        foreach (var entry in freeAgents)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO PlayerFreeAgencyState (
                    PlayerId, LastClubId, BecameFreeAgentDayNumber)
                VALUES ($playerId, $clubId, $day);
                """;
            command.Parameters.AddWithValue("$playerId", entry.PlayerId.Value);
            command.Parameters.AddWithValue("$clubId", entry.LastClubId.Value);
            command.Parameters.AddWithValue("$day", entry.BecameFreeAgentOn.DayNumber);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertTacticPlans(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<TacticPlan> tacticPlans)
    {
        foreach (var plan in tacticPlans)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO ClubTacticPlanState (
                    ClubId, Formation, Approach, Pressing, DefensiveLine, PassingStyle, LastUpdatedDayNumber)
                VALUES (
                    $clubId, $formation, $approach, $pressing, $defensiveLine, $passingStyle, $lastUpdated);
                """;
            command.Parameters.AddWithValue("$clubId", plan.ClubId.Value);
            command.Parameters.AddWithValue("$formation", (int)plan.Formation);
            command.Parameters.AddWithValue("$approach", (int)plan.Approach);
            command.Parameters.AddWithValue("$pressing", (int)plan.Pressing);
            command.Parameters.AddWithValue("$defensiveLine", (int)plan.DefensiveLine);
            command.Parameters.AddWithValue("$passingStyle", (int)plan.PassingStyle);
            command.Parameters.AddWithValue("$lastUpdated", plan.LastUpdatedOn.DayNumber);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertTransferNeeds(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<TransferNeed> transferNeeds)
    {
        foreach (var need in transferNeeds)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO TransferNeedState (
                    NeedId, ClubId, Kind, Status, Priority, ReasonCode,
                    IdentifiedDayNumber, ClosedDayNumber)
                VALUES (
                    $needId, $clubId, $kind, $status, $priority, $reason,
                    $identified, $closed);
                """;
            command.Parameters.AddWithValue("$needId", need.NeedId.Value);
            command.Parameters.AddWithValue("$clubId", need.ClubId.Value);
            command.Parameters.AddWithValue("$kind", (int)need.Kind);
            command.Parameters.AddWithValue("$status", (int)need.Status);
            command.Parameters.AddWithValue("$priority", need.Priority);
            command.Parameters.AddWithValue("$reason", need.ReasonCode);
            command.Parameters.AddWithValue("$identified", need.IdentifiedOn.DayNumber);
            command.Parameters.AddWithValue(
                "$closed",
                need.ClosedOn is GameDate closed ? closed.DayNumber : DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertShortlistEntries(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<ShortlistEntry> shortlistEntries)
    {
        foreach (var entry in shortlistEntries)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO ShortlistEntryState (
                    EntryId, ClubId, PlayerId, NeedId, Priority, Status,
                    AddedDayNumber, ArchivedDayNumber)
                VALUES (
                    $entryId, $clubId, $playerId, $needId, $priority, $status,
                    $added, $archived);
                """;
            command.Parameters.AddWithValue("$entryId", entry.EntryId.Value);
            command.Parameters.AddWithValue("$clubId", entry.ClubId.Value);
            command.Parameters.AddWithValue("$playerId", entry.PlayerId.Value);
            command.Parameters.AddWithValue(
                "$needId",
                entry.NeedId is TransferNeedId needId ? needId.Value : DBNull.Value);
            command.Parameters.AddWithValue("$priority", entry.Priority);
            command.Parameters.AddWithValue("$status", (int)entry.Status);
            command.Parameters.AddWithValue("$added", entry.AddedOn.DayNumber);
            command.Parameters.AddWithValue(
                "$archived",
                entry.ArchivedOn is GameDate archived ? archived.DayNumber : DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertTransferTargets(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<TransferTarget> transferTargets)
    {
        foreach (var target in transferTargets)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO TransferTargetState (
                    TargetId, NeedId, ClubId, PlayerId, ShortlistEntryId, Status,
                    ListedDayNumber, DroppedDayNumber)
                VALUES (
                    $targetId, $needId, $clubId, $playerId, $shortlistEntryId, $status,
                    $listed, $dropped);
                """;
            command.Parameters.AddWithValue("$targetId", target.TargetId.Value);
            command.Parameters.AddWithValue("$needId", target.NeedId.Value);
            command.Parameters.AddWithValue("$clubId", target.ClubId.Value);
            command.Parameters.AddWithValue("$playerId", target.PlayerId.Value);
            command.Parameters.AddWithValue(
                "$shortlistEntryId",
                target.ShortlistEntryId is ShortlistEntryId entryId ? entryId.Value : DBNull.Value);
            command.Parameters.AddWithValue("$status", (int)target.Status);
            command.Parameters.AddWithValue("$listed", target.ListedOn.DayNumber);
            command.Parameters.AddWithValue(
                "$dropped",
                target.DroppedOn is GameDate dropped ? dropped.DayNumber : DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertTransferProcesses(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<TransferProcess> transferProcesses)
    {
        foreach (var process in transferProcesses)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO TransferProcessState (
                    ProcessId, NeedId, TargetId, BuyingClubId, PlayerId, SellingClubId,
                    IsFreeAgent, Status, FailureReasonCode, OpenedDayNumber, TerminalDayNumber)
                VALUES (
                    $processId, $needId, $targetId, $buyingClubId, $playerId, $sellingClubId,
                    $isFreeAgent, $status, $failureReason, $opened, $terminal);
                """;
            command.Parameters.AddWithValue("$processId", process.ProcessId.Value);
            command.Parameters.AddWithValue("$needId", process.NeedId.Value);
            command.Parameters.AddWithValue("$targetId", process.TargetId.Value);
            command.Parameters.AddWithValue("$buyingClubId", process.BuyingClubId.Value);
            command.Parameters.AddWithValue("$playerId", process.PlayerId.Value);
            command.Parameters.AddWithValue(
                "$sellingClubId",
                process.SellingClubId is ClubId selling ? selling.Value : DBNull.Value);
            command.Parameters.AddWithValue("$isFreeAgent", process.IsFreeAgent ? 1 : 0);
            command.Parameters.AddWithValue("$status", (int)process.Status);
            command.Parameters.AddWithValue(
                "$failureReason",
                (object?)process.FailureReasonCode ?? DBNull.Value);
            command.Parameters.AddWithValue("$opened", process.OpenedOn.DayNumber);
            command.Parameters.AddWithValue(
                "$terminal",
                process.TerminalOn is GameDate terminal ? terminal.DayNumber : DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertClubOffers(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<ClubOffer> clubOffers)
    {
        foreach (var offer in clubOffers)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO ClubOfferState (
                    OfferId, ProcessId, Round, OfferedFee, Status, SubmittedDayNumber)
                VALUES (
                    $offerId, $processId, $round, $offeredFee, $status, $submitted);
                """;
            command.Parameters.AddWithValue("$offerId", offer.OfferId.Value);
            command.Parameters.AddWithValue("$processId", offer.ProcessId.Value);
            command.Parameters.AddWithValue("$round", offer.Round);
            command.Parameters.AddWithValue("$offeredFee", offer.OfferedFee);
            command.Parameters.AddWithValue("$status", (int)offer.Status);
            command.Parameters.AddWithValue("$submitted", offer.SubmittedOn.DayNumber);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertPlayerContractProposals(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<PlayerContractProposal> contractProposals)
    {
        foreach (var proposal in contractProposals)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO PlayerContractProposalState (
                    ProposalId, ProcessId, Round, WeeklyWage, ContractYears, Status, SubmittedDayNumber)
                VALUES (
                    $proposalId, $processId, $round, $weeklyWage, $contractYears, $status, $submitted);
                """;
            command.Parameters.AddWithValue("$proposalId", proposal.ProposalId.Value);
            command.Parameters.AddWithValue("$processId", proposal.ProcessId.Value);
            command.Parameters.AddWithValue("$round", proposal.Round);
            command.Parameters.AddWithValue("$weeklyWage", proposal.WeeklyWage);
            command.Parameters.AddWithValue("$contractYears", proposal.ContractYears);
            command.Parameters.AddWithValue("$status", (int)proposal.Status);
            command.Parameters.AddWithValue("$submitted", proposal.SubmittedOn.DayNumber);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertPromises(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<Promise> promises)
    {
        foreach (var promise in promises)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO PromiseState (
                    PromiseId, Kind, PromisorKind, PromisorId, PromiseeKind, PromiseeId,
                    ClubId, TargetStarts, StartsGiven, DeadlineDayNumber, CreatedDayNumber,
                    Status, TerminalDayNumber, CountedFixtureIdsCsv)
                VALUES (
                    $promiseId, $kind, $promisorKind, $promisorId, $promiseeKind, $promiseeId,
                    $clubId, $targetStarts, $startsGiven, $deadline, $created,
                    $status, $terminal, $fixtures);
                """;
            command.Parameters.AddWithValue("$promiseId", promise.PromiseId.Value);
            command.Parameters.AddWithValue("$kind", (int)promise.Kind);
            command.Parameters.AddWithValue("$promisorKind", (int)promise.Promisor.Kind);
            command.Parameters.AddWithValue("$promisorId", promise.Promisor.Id);
            command.Parameters.AddWithValue("$promiseeKind", (int)promise.Promisee.Kind);
            command.Parameters.AddWithValue("$promiseeId", promise.Promisee.Id);
            command.Parameters.AddWithValue("$clubId", promise.ClubId.Value);
            command.Parameters.AddWithValue("$targetStarts", promise.TargetStarts);
            command.Parameters.AddWithValue("$startsGiven", promise.StartsGiven);
            command.Parameters.AddWithValue("$deadline", promise.DeadlineOn.DayNumber);
            command.Parameters.AddWithValue("$created", promise.CreatedOn.DayNumber);
            command.Parameters.AddWithValue("$status", (int)promise.Status);
            command.Parameters.AddWithValue(
                "$terminal",
                (object?)promise.TerminalOn?.DayNumber ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "$fixtures",
                string.Join(',', promise.CountedFixtureIds.OrderBy(id => id)));
            command.ExecuteNonQuery();
        }
    }

    private static void InsertRelationships(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<RelationshipRecord> relationships)
    {
        foreach (var relationship in relationships)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO RelationshipState (
                    RelationshipId, ObserverKind, ObserverId, SubjectKind, SubjectId,
                    Trust, Respect, ProfessionalCompatibility, Status,
                    CreatedDayNumber, LastChangedDayNumber, LastChangeReasonCode, ProcessedEffectKeysCsv)
                VALUES (
                    $id, $observerKind, $observerId, $subjectKind, $subjectId,
                    $trust, $respect, $compatibility, $status,
                    $created, $changed, $reason, $effects);
                """;
            command.Parameters.AddWithValue("$id", relationship.RelationshipId.Value);
            command.Parameters.AddWithValue("$observerKind", (int)relationship.Observer.Kind);
            command.Parameters.AddWithValue("$observerId", relationship.Observer.Id);
            command.Parameters.AddWithValue("$subjectKind", (int)relationship.Subject.Kind);
            command.Parameters.AddWithValue("$subjectId", relationship.Subject.Id);
            command.Parameters.AddWithValue("$trust", relationship.Trust);
            command.Parameters.AddWithValue("$respect", relationship.Respect);
            command.Parameters.AddWithValue("$compatibility", relationship.ProfessionalCompatibility);
            command.Parameters.AddWithValue("$status", (int)relationship.Status);
            command.Parameters.AddWithValue("$created", relationship.CreatedOn.DayNumber);
            command.Parameters.AddWithValue("$changed", relationship.LastChangedOn.DayNumber);
            command.Parameters.AddWithValue(
                "$reason",
                (object?)relationship.LastChangeReasonCode ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "$effects",
                string.Join(',', relationship.ProcessedEffectKeys.OrderBy(k => k, StringComparer.Ordinal)));
            command.ExecuteNonQuery();
        }
    }

    private static void InsertDecisionRequests(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<DecisionRequest> decisionRequests)
    {
        foreach (var request in decisionRequests)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO DecisionRequestState (
                    DecisionRequestId, Kind, ManagerId, SubjectPlayerId, ClubId,
                    OpenedDayNumber, DeadlineDayNumber, Status, IsHardBlocker,
                    SelectedOptionCode, ResolvedDayNumber)
                VALUES (
                    $id, $kind, $managerId, $playerId, $clubId,
                    $opened, $deadline, $status, $hard,
                    $option, $resolved);
                """;
            command.Parameters.AddWithValue("$id", request.DecisionRequestId.Value);
            command.Parameters.AddWithValue("$kind", (int)request.Kind);
            command.Parameters.AddWithValue("$managerId", request.ManagerId.Value);
            command.Parameters.AddWithValue("$playerId", request.SubjectPlayerId.Value);
            command.Parameters.AddWithValue("$clubId", request.ClubId.Value);
            command.Parameters.AddWithValue("$opened", request.OpenedOn.DayNumber);
            command.Parameters.AddWithValue("$deadline", request.DeadlineOn.DayNumber);
            command.Parameters.AddWithValue("$status", (int)request.Status);
            command.Parameters.AddWithValue("$hard", request.IsHardBlocker ? 1 : 0);
            command.Parameters.AddWithValue(
                "$option",
                (object?)request.SelectedOptionCode ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "$resolved",
                (object?)request.ResolvedOn?.DayNumber ?? DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertDialogueSessions(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<DialogueSession> dialogueSessions)
    {
        foreach (var session in dialogueSessions)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO DialogueSessionState (
                    DialogueSessionId, SourceDecisionRequestId, DialogueTypeCode,
                    ManagerId, PrimaryParticipantPlayerId, CreatedDayNumber, DeadlineDayNumber,
                    Status, AvailableOptionCodesCsv, SelectedOptionCode, ResolvedDayNumber)
                VALUES (
                    $id, $decisionId, $type, $managerId, $playerId, $created, $deadline,
                    $status, $options, $selected, $resolved);
                """;
            command.Parameters.AddWithValue("$id", session.DialogueSessionId.Value);
            command.Parameters.AddWithValue("$decisionId", session.SourceDecisionRequestId.Value);
            command.Parameters.AddWithValue("$type", session.DialogueTypeCode);
            command.Parameters.AddWithValue("$managerId", session.ManagerId.Value);
            command.Parameters.AddWithValue("$playerId", session.PrimaryParticipantPlayerId.Value);
            command.Parameters.AddWithValue("$created", session.CreatedOn.DayNumber);
            command.Parameters.AddWithValue(
                "$deadline",
                (object?)session.DeadlineOn?.DayNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("$status", (int)session.Status);
            command.Parameters.AddWithValue(
                "$options",
                string.Join('|', session.AvailableOptionCodes));
            command.Parameters.AddWithValue(
                "$selected",
                (object?)session.SelectedOptionCode ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "$resolved",
                (object?)session.ResolvedOn?.DayNumber ?? DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertDisciplinaryActions(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<DisciplinaryAction> disciplinaryActions)
    {
        foreach (var action in disciplinaryActions)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO DisciplinaryActionState (
                    DisciplinaryActionId, Kind, ManagerId, SubjectPlayerId, ClubId,
                    SourceDecisionRequestId, AppliedDayNumber)
                VALUES (
                    $id, $kind, $managerId, $playerId, $clubId, $decisionId, $applied);
                """;
            command.Parameters.AddWithValue("$id", action.DisciplinaryActionId.Value);
            command.Parameters.AddWithValue("$kind", (int)action.Kind);
            command.Parameters.AddWithValue("$managerId", action.ManagerId.Value);
            command.Parameters.AddWithValue("$playerId", action.SubjectPlayerId.Value);
            command.Parameters.AddWithValue("$clubId", action.ClubId.Value);
            command.Parameters.AddWithValue(
                "$decisionId",
                (object?)action.SourceDecisionRequestId?.Value ?? DBNull.Value);
            command.Parameters.AddWithValue("$applied", action.AppliedOn.DayNumber);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertEventEffectProcessingKeys(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<string> processingKeys)
    {
        foreach (var key in processingKeys.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO EventEffectIdempotencyState (ProcessingKey)
                VALUES ($key);
                """;
            command.Parameters.AddWithValue("$key", key);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertHubNarrativeUiState(
        SqliteConnection connection,
        SqliteTransaction transaction,
        HubNarrativeUiState state)
    {
        if (state.IsEmpty)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO HubNarrativeUiState (
                SingletonId, WeekStoryClosureBeat, WeekStoryDismissOnNextAdvance,
                CleanXiNamesCsv, InjuryClearedNamesCsv,
                PendingMatchTrainingFixtureId, PendingMatchTrainingPriorityCode,
                PendingMatchTrainingModifier)
            VALUES (1, $beat, $dismiss, $clean, $cleared, $fixture, $priority, $modifier);
            """;
        command.Parameters.AddWithValue(
            "$beat",
            (object?)state.WeekStoryClosureBeat ?? DBNull.Value);
        command.Parameters.AddWithValue("$dismiss", state.WeekStoryDismissOnNextAdvance ? 1 : 0);
        command.Parameters.AddWithValue(
            "$clean",
            state.CleanXiNames.Count == 0
                ? DBNull.Value
                : string.Join('|', state.CleanXiNames));
        command.Parameters.AddWithValue(
            "$cleared",
            state.InjuryClearedNames.Count == 0
                ? DBNull.Value
                : string.Join('|', state.InjuryClearedNames));
        command.Parameters.AddWithValue(
            "$fixture",
            (object?)state.PendingMatchTrainingFixtureId ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$priority",
            (object?)state.PendingMatchTrainingPriorityCode ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$modifier",
            (object?)state.PendingMatchTrainingModifier ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static void InsertMatchupPlanHistory(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<MatchupPlanNotebookEntry> history)
    {
        var ordered = history
            .OrderBy(entry => entry.DayNumber)
            .TakeLast(MatchupPlanNotebookEntry.HistoryLimit)
            .ToArray();
        for (var index = 0; index < ordered.Length; index++)
        {
            var entry = ordered[index];
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO MatchupPlanNotebookState (
                    SequenceIndex, DayNumber, OpponentName, SelectionLine,
                    ThreatKind, PlanSignal, OutcomeSignal, VerdictLine)
                VALUES ($index, $day, $opponent, $selection, $threat, $plan, $outcome, $verdict);
                """;
            command.Parameters.AddWithValue("$index", index);
            command.Parameters.AddWithValue("$day", entry.DayNumber);
            command.Parameters.AddWithValue("$opponent", entry.OpponentName);
            command.Parameters.AddWithValue("$selection", entry.SelectionLine);
            command.Parameters.AddWithValue("$threat", (int)entry.ThreatKind);
            command.Parameters.AddWithValue("$plan", (int)entry.PlanSignal);
            command.Parameters.AddWithValue("$outcome", (int)entry.OutcomeSignal);
            command.Parameters.AddWithValue("$verdict", entry.VerdictLine);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertScheduledEvaluations(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<ScheduledEvaluation> evaluations)
    {
        foreach (var evaluation in evaluations.OrderBy(item => item.Id.Value))
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO ScheduledEvaluationState (
                    ScheduledEvaluationId, EvaluationTypeCode, DueDayNumber, SourceEventId, Status)
                VALUES ($id, $type, $due, $source, $status);
                """;
            command.Parameters.AddWithValue("$id", evaluation.Id.Value);
            command.Parameters.AddWithValue("$type", evaluation.EvaluationTypeCode);
            command.Parameters.AddWithValue("$due", evaluation.DueDayNumber);
            command.Parameters.AddWithValue(
                "$source",
                evaluation.SourceEventId is Guid source ? source.ToString("N") : DBNull.Value);
            command.Parameters.AddWithValue("$status", (int)evaluation.Status);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertMemories(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<MemoryRecord> memories)
    {
        foreach (var memory in memories)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO MemoryState (
                    MemoryId, RememberingActorKind, RememberingActorId, SubjectKind, SubjectId,
                    SourceEventKey, Category, CreatedDayNumber, LastReinforcedDayNumber,
                    BaseImportance, CurrentInfluence, Valence, Visibility, Status,
                    ReinforcementCount, RelatedPromiseId, RuleId, RuleVersion,
                    ProcessedReinforcementKeysCsv)
                VALUES (
                    $memoryId, $actorKind, $actorId, $subjectKind, $subjectId,
                    $sourceKey, $category, $created, $reinforced,
                    $baseImportance, $currentInfluence, $valence, $visibility, $status,
                    $reinforcement, $relatedPromise, $ruleId, $ruleVersion,
                    $processedReinforcementKeys);
                """;
            command.Parameters.AddWithValue("$memoryId", memory.MemoryId.Value);
            command.Parameters.AddWithValue("$actorKind", (int)memory.RememberingActor.Kind);
            command.Parameters.AddWithValue("$actorId", memory.RememberingActor.Id);
            command.Parameters.AddWithValue("$subjectKind", (int)memory.SubjectKind);
            command.Parameters.AddWithValue("$subjectId", memory.SubjectId);
            command.Parameters.AddWithValue("$sourceKey", memory.SourceEventKey);
            command.Parameters.AddWithValue("$category", (int)memory.Category);
            command.Parameters.AddWithValue("$created", memory.CreatedOn.DayNumber);
            command.Parameters.AddWithValue("$reinforced", memory.LastReinforcedOn.DayNumber);
            command.Parameters.AddWithValue("$baseImportance", memory.BaseImportance);
            command.Parameters.AddWithValue("$currentInfluence", memory.CurrentInfluence);
            command.Parameters.AddWithValue("$valence", (int)memory.Valence);
            command.Parameters.AddWithValue("$visibility", (int)memory.Visibility);
            command.Parameters.AddWithValue("$status", (int)memory.Status);
            command.Parameters.AddWithValue("$reinforcement", memory.ReinforcementCount);
            command.Parameters.AddWithValue(
                "$relatedPromise",
                (object?)memory.RelatedPromiseId?.Value ?? DBNull.Value);
            command.Parameters.AddWithValue("$ruleId", memory.RuleId);
            command.Parameters.AddWithValue("$ruleVersion", memory.RuleVersion);
            command.Parameters.AddWithValue(
                "$processedReinforcementKeys",
                string.Join(',', memory.ProcessedReinforcementKeys.OrderBy(k => k, StringComparer.Ordinal)));
            command.ExecuteNonQuery();
        }
    }

    private static (int Version, bool IsLegacySpikeSave) ReadSchemaMetadata(string filePath)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly");
            connection.Open();

            var hasProductionManifest = TableExists(connection, "ProductionSaveManifest");
            var hasSpikeManifest = TableExists(connection, "SaveManifest");

            if (!hasProductionManifest && hasSpikeManifest)
            {
                SqliteConnection.ClearAllPools();
                return (0, true);
            }

            if (!hasProductionManifest)
            {
                throw new SaveCorruptionException("Production save manifest table was not found.");
            }

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT SchemaVersion, SaveFormatId
                FROM ProductionSaveManifest
                LIMIT 1;
                """;
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                throw new SaveCorruptionException("Production save manifest is empty.");
            }

            var version = reader.GetInt32(0);
            var formatId = reader.GetString(1);

            if (!string.Equals(formatId, ProductionWorldCalendarSaveSchema.SaveFormatId, StringComparison.Ordinal))
            {
                throw new UnsupportedSaveSchemaVersionException(version);
            }

            SqliteConnection.ClearAllPools();
            return (version, false);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            throw new SaveCorruptionException($"Save dosyası okunamadı veya geçerli bir SQLite dosyası değil: {filePath}", ex);
        }
    }

    private static void RepairManifestHash(string filePath)
    {
        using var connection = new SqliteConnection($"Data Source={filePath}");
        connection.Open();

        var timeline = ReadTimeline(connection);
        var league = ReadLeague(connection);
        var clubRegistry = ReadClubRegistry(connection);
        var managerCareer = ReadManager(connection, timeline.CurrentDate);
        var matchSelections = ReadMatchSelections(connection);
        var trainingPlans = ReadTrainingPlans(connection);
        var physicalStates = ReadPhysicalStates(connection);
        var playerCareers = ReadPlayerCareers(connection);
        var contracts = ReadContracts(connection);
        var clubSquads = ReadClubSquads(connection);
        var freeAgents = ReadFreeAgents(connection);
        var tacticPlans = ReadTacticPlans(connection);
        var transferNeeds = ReadTransferNeeds(connection);
        var shortlistEntries = ReadShortlistEntries(connection);
        var transferTargets = ReadTransferTargets(connection);
        var transferProcesses = ReadTransferProcesses(connection);
        var clubOffers = ReadClubOffers(connection);
        var contractProposals = ReadPlayerContractProposals(connection);
        var promises = ReadPromises(connection);
        var memories = ReadMemories(connection);
        var relationships = ReadRelationships(connection);
        var decisionRequests = ReadDecisionRequests(connection);
        var dialogueSessions = ReadDialogueSessions(connection);
        var disciplinaryActions = ReadDisciplinaryActions(connection);
        var eventEffectProcessingKeys = ReadEventEffectProcessingKeys(connection);
        var scheduledEvaluations = ReadScheduledEvaluations(connection);
        var hubNarrativeUiState = ReadHubNarrativeUiState(connection);
        var canonicalHash = CareerCanonicalStateHasher.ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            freeAgents,
            tacticPlans,
            transferNeeds,
            shortlistEntries,
            transferTargets,
            transferProcesses,
            clubOffers,
            contractProposals,
            promises,
            memories,
            relationships,
            decisionRequests,
            dialogueSessions,
            disciplinaryActions,
            eventEffectProcessingKeys,
            scheduledEvaluations,
            BuildHubNarrativeCanonicalText(hubNarrativeUiState));

        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE ProductionSaveManifest
            SET SchemaVersion = $schemaVersion,
                CanonicalStateHash = $canonicalHash;
            """;
        command.Parameters.AddWithValue("$schemaVersion", ProductionWorldCalendarSaveSchema.CurrentVersion);
        command.Parameters.AddWithValue("$canonicalHash", canonicalHash);
        command.ExecuteNonQuery();
        transaction.Commit();

        SqliteConnection.ClearAllPools();
    }

    private static CareerLoadResult ReadCurrentVersion(string filePath, bool wasMigrated, int schemaVersion)
    {
        try
        {
            string canonicalHash;
            WorldTimeline timeline;
            LeagueCompetition league;
            LeagueClubRegistry clubRegistry;
            ManagerCareer managerCareer;
            IReadOnlyList<MatchSelection> matchSelections;
            IReadOnlyList<WeeklyTrainingPlan> trainingPlans;
            IReadOnlyList<PlayerPhysicalState> physicalStates;
            IReadOnlyList<PlayerCareerAggregate> playerCareers;
            IReadOnlyList<PlayerContract> contracts;
            IReadOnlyList<ClubSquad> clubSquads;
            IReadOnlyList<PlayerFreeAgency> freeAgents;
            IReadOnlyList<TacticPlan> tacticPlans;
            IReadOnlyList<TransferNeed> transferNeeds;
            IReadOnlyList<ShortlistEntry> shortlistEntries;
            IReadOnlyList<TransferTarget> transferTargets;
            IReadOnlyList<TransferProcess> transferProcesses;
            IReadOnlyList<ClubOffer> clubOffers;
            IReadOnlyList<PlayerContractProposal> contractProposals;
            IReadOnlyList<Promise> promises;
            IReadOnlyList<MemoryRecord> memories;
            IReadOnlyList<RelationshipRecord> relationships;
            IReadOnlyList<DecisionRequest> decisionRequests;
            IReadOnlyList<DialogueSession> dialogueSessions;
            IReadOnlyList<DisciplinaryAction> disciplinaryActions;
            IReadOnlyList<string> eventEffectProcessingKeys;
            IReadOnlyList<ScheduledEvaluation> scheduledEvaluations;
            HubNarrativeUiState hubNarrativeUiState;

            using (var connection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly"))
            {
                connection.Open();

                using (var manifestCommand = connection.CreateCommand())
                {
                    manifestCommand.CommandText = "SELECT CanonicalStateHash FROM ProductionSaveManifest LIMIT 1;";
                    canonicalHash = (string)manifestCommand.ExecuteScalar()!;
                }

                timeline = ReadTimeline(connection);
                league = ReadLeague(connection);
                clubRegistry = ReadClubRegistry(connection);
                managerCareer = ReadManager(connection, timeline.CurrentDate);
                matchSelections = ReadMatchSelections(connection);
                trainingPlans = ReadTrainingPlans(connection);
                physicalStates = ReadPhysicalStates(connection);
                playerCareers = ReadPlayerCareers(connection);
                contracts = ReadContracts(connection);
                clubSquads = ReadClubSquads(connection);
                freeAgents = ReadFreeAgents(connection);
                tacticPlans = ReadTacticPlans(connection);
                transferNeeds = ReadTransferNeeds(connection);
                shortlistEntries = ReadShortlistEntries(connection);
                transferTargets = ReadTransferTargets(connection);
                transferProcesses = ReadTransferProcesses(connection);
                clubOffers = ReadClubOffers(connection);
                contractProposals = ReadPlayerContractProposals(connection);
                promises = ReadPromises(connection);
                memories = ReadMemories(connection);
                relationships = ReadRelationships(connection);
                decisionRequests = ReadDecisionRequests(connection);
                dialogueSessions = ReadDialogueSessions(connection);
                disciplinaryActions = ReadDisciplinaryActions(connection);
                eventEffectProcessingKeys = ReadEventEffectProcessingKeys(connection);
                scheduledEvaluations = ReadScheduledEvaluations(connection);
                hubNarrativeUiState = ReadHubNarrativeUiState(connection);
            }

            SqliteConnection.ClearAllPools();

            var recomputedHash = CareerCanonicalStateHasher.ComputeHash(
                timeline,
                league,
                clubRegistry,
                managerCareer,
                matchSelections,
                trainingPlans,
                physicalStates,
                playerCareers,
                contracts,
                clubSquads,
                freeAgents,
                tacticPlans,
                transferNeeds,
                shortlistEntries,
                transferTargets,
                transferProcesses,
                clubOffers,
                contractProposals,
                promises,
                memories,
                relationships,
                decisionRequests,
                dialogueSessions,
                disciplinaryActions,
                eventEffectProcessingKeys,
                scheduledEvaluations,
                BuildHubNarrativeCanonicalText(hubNarrativeUiState));
            if (!string.Equals(recomputedHash, canonicalHash, StringComparison.Ordinal))
            {
                throw new SaveCorruptionException(
                    $"Bütünlük hash'i eşleşmiyor (beklenen: {canonicalHash}, hesaplanan: {recomputedHash}); save bozulmuş olabilir.");
            }

            return new CareerLoadResult(
                timeline,
                league,
                clubRegistry,
                managerCareer,
                matchSelections,
                trainingPlans,
                physicalStates,
                playerCareers,
                contracts,
                clubSquads,
                freeAgents,
                tacticPlans,
                transferNeeds,
                shortlistEntries,
                transferTargets,
                transferProcesses,
                clubOffers,
                contractProposals,
                promises,
                memories,
                relationships,
                decisionRequests,
                dialogueSessions,
                disciplinaryActions,
                schemaVersion,
                wasMigrated,
                eventEffectProcessingKeys,
                scheduledEvaluations,
                hubNarrativeUiState);
        }
        catch (SaveIntegrityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SaveCorruptionException($"Save dosyası okunamadı veya geçerli bir SQLite dosyası değil: {filePath}", ex);
        }
    }

    private static WorldTimeline ReadTimeline(SqliteConnection connection)
    {
        int currentDayNumber;
        long lastCommittedStepId;
        int rootSeed;
        string rngVersion;
        int rngDrawCount;
        long? planningPeriodId = null;
        int? planningStartDayNumber = null;
        int? planningExpectedEndDayNumber = null;
        int? planningStatus = null;
        int? planningCompletedAtDayNumber = null;
        int? transferWindowStatus = null;
        int? transferWindowOpenedOnDayNumber = null;
        int? transferWindowClosesOnDayNumber = null;

        using (var timelineCommand = connection.CreateCommand())
        {
            timelineCommand.CommandText = """
                SELECT CurrentDayNumber, LastCommittedStepId, RootSeed, RngVersion, RngDrawCount, CheckpointLabel
                FROM WorldTimelineState
                WHERE SingletonId = 1;
                """;
            using var reader = timelineCommand.ExecuteReader();
            if (!reader.Read())
            {
                throw new SaveCorruptionException("World timeline state is missing.");
            }

            currentDayNumber = reader.GetInt32(0);
            lastCommittedStepId = reader.GetInt64(1);
            rootSeed = reader.GetInt32(2);
            rngVersion = reader.GetString(3);
            rngDrawCount = reader.GetInt32(4);
            _ = reader.IsDBNull(5) ? null : reader.GetString(5);
        }

        if (TableExists(connection, "PlanningPeriodState"))
        {
            using var periodCommand = connection.CreateCommand();
            periodCommand.CommandText = """
                SELECT PlanningPeriodId, StartDayNumber, ExpectedEndDayNumber, Status, CompletedAtDayNumber
                FROM PlanningPeriodState
                WHERE SingletonId = 1;
                """;
            using var reader = periodCommand.ExecuteReader();
            if (reader.Read())
            {
                planningPeriodId = reader.GetInt64(0);
                planningStartDayNumber = reader.GetInt32(1);
                planningExpectedEndDayNumber = reader.IsDBNull(2) ? null : reader.GetInt32(2);
                planningStatus = reader.GetInt32(3);
                planningCompletedAtDayNumber = reader.IsDBNull(4) ? null : reader.GetInt32(4);
            }
        }

        if (TableExists(connection, "TransferWindowState"))
        {
            using var windowCommand = connection.CreateCommand();
            windowCommand.CommandText = """
                SELECT Status, OpenedOnDayNumber, ClosesOnDayNumber
                FROM TransferWindowState
                WHERE SingletonId = 1;
                """;
            using var reader = windowCommand.ExecuteReader();
            if (reader.Read())
            {
                transferWindowStatus = reader.GetInt32(0);
                transferWindowOpenedOnDayNumber = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                transferWindowClosesOnDayNumber = reader.IsDBNull(2) ? null : reader.GetInt32(2);
            }
        }

        return WorldCalendarSnapshotMapper.ToDomain(
            currentDayNumber,
            lastCommittedStepId,
            rootSeed,
            rngVersion,
            rngDrawCount,
            planningPeriodId,
            planningStartDayNumber,
            planningExpectedEndDayNumber,
            planningStatus,
            planningCompletedAtDayNumber,
            checkpointLabel: null,
            transferWindowStatus,
            transferWindowOpenedOnDayNumber,
            transferWindowClosesOnDayNumber);
    }

    private static LeagueCompetition ReadLeague(SqliteConnection connection)
    {
        if (!TableExists(connection, "LeagueCompetitionState"))
        {
            return new LeagueCompetition(new Domain.Competition.CompetitionId(1));
        }

        long competitionId;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT CompetitionId FROM LeagueCompetitionState WHERE SingletonId = 1;";
            var scalar = command.ExecuteScalar();
            if (scalar is null)
            {
                return new LeagueCompetition(new Domain.Competition.CompetitionId(1));
            }

            competitionId = Convert.ToInt64(scalar);
        }

        var seasons = new List<CareerSnapshotMapper.SeasonSnapshotRow>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT SeasonId, PreseasonStartDayNumber, Status, ActiveStartedAtDayNumber, CompletedAtDayNumber, ArchivedAtDayNumber
                FROM CompetitionSeasonState
                ORDER BY SeasonId;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                seasons.Add(new CareerSnapshotMapper.SeasonSnapshotRow(
                    reader.GetInt64(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetInt32(5)));
            }
        }

        var participants = new List<CareerSnapshotMapper.ParticipantSnapshotRow>();
        if (TableExists(connection, "SeasonParticipantState"))
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT SeasonId, ClubId FROM SeasonParticipantState ORDER BY SeasonId, ClubId;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                participants.Add(new CareerSnapshotMapper.ParticipantSnapshotRow(reader.GetInt64(0), reader.GetInt64(1)));
            }
        }

        var fixtures = new List<CareerSnapshotMapper.FixtureSnapshotRow>();
        if (TableExists(connection, "FixtureState"))
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT FixtureId, SeasonId, HomeClubId, AwayClubId, Round, ScheduledDayNumber, Status, HomeGoals, AwayGoals
                FROM FixtureState
                ORDER BY FixtureId;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                fixtures.Add(new CareerSnapshotMapper.FixtureSnapshotRow(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.GetInt64(2),
                    reader.GetInt64(3),
                    reader.GetInt32(4),
                    reader.GetInt32(5),
                    reader.GetInt32(6),
                    reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    reader.IsDBNull(8) ? null : reader.GetInt32(8)));
            }
        }

        return CareerSnapshotMapper.ToLeague(competitionId, seasons, participants, fixtures);
    }

    private static LeagueClubRegistry ReadClubRegistry(SqliteConnection connection)
    {
        if (!TableExists(connection, "ClubState"))
        {
            return LeagueClubRegistry.CreateMvpLeague();
        }

        var rows = new List<ClubSnapshotMapper.ClubSnapshotRow>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ClubId, DisplayName, ClubCode, SportiveStrength,
                   TransferBudgetLimit, ReservedTransferFunds, SpentTransferFunds,
                   WageBudgetLimit, ReservedWeeklyWage
            FROM ClubState
            ORDER BY ClubId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new ClubSnapshotMapper.ClubSnapshotRow(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetInt32(6),
                reader.GetInt32(7),
                reader.GetInt32(8)));
        }

        return ClubSnapshotMapper.ToRegistry(rows);
    }

    private static ManagerCareer ReadManager(SqliteConnection connection, GameDate fallbackStartDate)
    {
        if (!TableExists(connection, "ManagerCareerState"))
        {
            return ManagerCareer.StartNewCareerForClubStrength(
                new ManagerId(1),
                "Teknik Direktör",
                new Domain.Shared.ClubId(1),
                fallbackStartDate,
                clubSportiveStrength: 50);
        }

        var employmentHistory = ReadManagerEmploymentHistory(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ManagerId, DisplayName, EmployedClubId, EmploymentStartedDayNumber,
                   SeasonExpectation, BoardConfidence, EmploymentRiskBand,
                   LastAssessedFixtureId, LastAssessmentReasonCode,
                   EmploymentStatus, EmploymentEndReason, LastClubId,
                   DismissedDueToFixtureId, DismissedAtDayNumber,
                   PendingOfferId, PendingOfferClubId, PendingOfferStatus, PendingOfferCreatedDayNumber,
                   ManagerReputation, LastReputationReasonCode
            FROM ManagerCareerState
            WHERE SingletonId = 1;
            """;
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return ManagerCareer.StartNewCareerForClubStrength(
                new ManagerId(1),
                "Teknik Direktör",
                new Domain.Shared.ClubId(1),
                fallbackStartDate,
                clubSportiveStrength: 50);
        }

        return ManagerSnapshotMapper.ToDomain(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetInt64(2),
            reader.IsDBNull(3) ? null : reader.GetInt32(3),
            fallbackStartDate,
            seasonExpectation: reader.IsDBNull(4) ? null : reader.GetInt32(4),
            boardConfidence: reader.IsDBNull(5) ? null : reader.GetInt32(5),
            riskBand: reader.IsDBNull(6) ? null : reader.GetInt32(6),
            lastAssessedFixtureId: reader.IsDBNull(7) ? null : reader.GetInt64(7),
            lastAssessmentReasonCode: reader.IsDBNull(8) ? null : reader.GetString(8),
            employmentStatus: reader.IsDBNull(9) ? null : reader.GetInt32(9),
            employmentEndReason: reader.IsDBNull(10) ? null : reader.GetInt32(10),
            lastClubId: reader.IsDBNull(11) ? null : reader.GetInt64(11),
            dismissedDueToFixtureId: reader.IsDBNull(12) ? null : reader.GetInt64(12),
            dismissedAtDayNumber: reader.IsDBNull(13) ? null : reader.GetInt32(13),
            pendingOfferId: reader.FieldCount > 14 && !reader.IsDBNull(14) ? reader.GetInt64(14) : null,
            pendingOfferClubId: reader.FieldCount > 15 && !reader.IsDBNull(15) ? reader.GetInt64(15) : null,
            pendingOfferStatus: reader.FieldCount > 16 && !reader.IsDBNull(16) ? reader.GetInt32(16) : null,
            pendingOfferCreatedDayNumber: reader.FieldCount > 17 && !reader.IsDBNull(17) ? reader.GetInt32(17) : null,
            managerReputation: reader.FieldCount > 18 && !reader.IsDBNull(18) ? reader.GetInt32(18) : null,
            lastReputationReasonCode: reader.FieldCount > 19 && !reader.IsDBNull(19) ? reader.GetString(19) : null,
            employmentHistory: employmentHistory);
    }

    private static IReadOnlyList<EmploymentHistoryEntry> ReadManagerEmploymentHistory(
        SqliteConnection connection)
    {
        if (!TableExists(connection, "ManagerEmploymentHistoryState"))
        {
            return Array.Empty<EmploymentHistoryEntry>();
        }

        var history = new List<EmploymentHistoryEntry>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ClubId, StartedDayNumber, EndedDayNumber, EndReason,
                   FinalBoardConfidence, CausationFixtureId, FinalAssessmentReasonCode
            FROM ManagerEmploymentHistoryState
            ORDER BY Ordinal;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            history.Add(EmploymentHistoryEntry.Rehydrate(
                new ClubId(reader.GetInt64(0)),
                GameDate.FromDayNumber(reader.GetInt32(1)),
                GameDate.FromDayNumber(reader.GetInt32(2)),
                (EmploymentEndReason)reader.GetInt32(3),
                reader.GetInt32(4),
                reader.IsDBNull(5) ? null : new FixtureId(reader.GetInt64(5)),
                reader.IsDBNull(6) ? null : reader.GetString(6)));
        }

        return history;
    }

    private static IReadOnlyList<MatchSelection> ReadMatchSelections(SqliteConnection connection)
    {
        if (!TableExists(connection, "MatchSelectionState"))
        {
            return Array.Empty<MatchSelection>();
        }

        var selections = new List<MatchSelection>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT FixtureId, ClubId, Status, StartingSlotsCsv, BenchSlotsCsv
            FROM MatchSelectionState
            ORDER BY FixtureId, ClubId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var starting = ParseSlotCsv(reader.GetString(3));
            var bench = ParseSlotCsv(reader.GetString(4));
            selections.Add(MatchSelection.Rehydrate(
                new FixtureId(reader.GetInt64(0)),
                new ClubId(reader.GetInt64(1)),
                starting,
                bench,
                (MatchSelectionStatus)reader.GetInt32(2)));
        }

        return selections;
    }

    private static IReadOnlyList<WeeklyTrainingPlan> ReadTrainingPlans(SqliteConnection connection)
    {
        if (!TableExists(connection, "WeeklyTrainingPlanState"))
        {
            return Array.Empty<WeeklyTrainingPlan>();
        }

        var plans = new List<WeeklyTrainingPlan>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ClubId, Focus, Intensity, RestApproach, SetAtDayNumber
            FROM WeeklyTrainingPlanState
            ORDER BY ClubId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            plans.Add(WeeklyTrainingPlan.Rehydrate(
                new ClubId(reader.GetInt64(0)),
                (TrainingFocus)reader.GetInt32(1),
                (TrainingIntensity)reader.GetInt32(2),
                (RestApproach)reader.GetInt32(3),
                GameDate.FromDayNumber(reader.GetInt32(4))));
        }

        return plans;
    }

    private static IReadOnlyList<PlayerPhysicalState> ReadPhysicalStates(SqliteConnection connection)
    {
        if (!TableExists(connection, "PlayerPhysicalState"))
        {
            return Array.Empty<PlayerPhysicalState>();
        }

        var states = new List<PlayerPhysicalState>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ClubId, SlotIndex, Fatigue, Fitness, InjurySeverity, InjuredUntilDayNumber
            FROM PlayerPhysicalState
            ORDER BY ClubId, SlotIndex;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var injurySeverity = reader.FieldCount > 4
                ? (InjurySeverity)reader.GetInt32(4)
                : InjurySeverity.None;
            int? injuredUntil = reader.FieldCount > 5 && !reader.IsDBNull(5)
                ? reader.GetInt32(5)
                : null;
            states.Add(PlayerPhysicalState.Rehydrate(
                new ClubId(reader.GetInt64(0)),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                injurySeverity,
                injuredUntil));
        }

        return states;
    }

    private static IReadOnlyList<PlayerCareerAggregate> ReadPlayerCareers(SqliteConnection connection)
    {
        if (!TableExists(connection, "PlayerCareerState"))
        {
            return Array.Empty<PlayerCareerAggregate>();
        }

        var careers = new List<PlayerCareerAggregate>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT PlayerId, OriginClubId, SlotIndex, CurrentAbility, PotentialAbility,
                   DevelopmentPoints, LastDevelopedDayNumber, BirthYear, LastAgedCalendarYear,
                   LifecycleStatus, RetiredDayNumber, RetirementReason, Generation
            FROM PlayerCareerState
            ORDER BY PlayerId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var birthYear = reader.FieldCount > 7 ? reader.GetInt32(7) : 2000 - reader.GetInt32(2);
            int? agedYear = reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetInt32(8) : null;
            careers.Add(PlayerCareerAggregate.Rehydrate(
                new PlayerId(reader.GetInt64(0)),
                new ClubId(reader.GetInt64(1)),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.IsDBNull(6) ? null : GameDate.FromDayNumber(reader.GetInt32(6)),
                birthYear,
                agedYear,
                (PlayerLifecycleStatus)reader.GetInt32(9),
                reader.IsDBNull(10) ? null : GameDate.FromDayNumber(reader.GetInt32(10)),
                reader.IsDBNull(11) ? null : (PlayerRetirementReason)reader.GetInt32(11),
                reader.GetInt32(12)));
        }

        return careers;
    }

    private static IReadOnlyList<PlayerContract> ReadContracts(SqliteConnection connection)
    {
        if (!TableExists(connection, "PlayerContractState"))
        {
            return Array.Empty<PlayerContract>();
        }

        var contracts = new List<PlayerContract>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ContractId, PlayerId, ClubId, StartDayNumber, EndDayNumber, WeeklyWage, Status
            FROM PlayerContractState
            ORDER BY ContractId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            contracts.Add(PlayerContract.Rehydrate(
                new ContractId(reader.GetInt64(0)),
                new PlayerId(reader.GetInt64(1)),
                new ClubId(reader.GetInt64(2)),
                GameDate.FromDayNumber(reader.GetInt32(3)),
                GameDate.FromDayNumber(reader.GetInt32(4)),
                reader.GetInt32(5),
                (ContractStatus)reader.GetInt32(6)));
        }

        return contracts;
    }

    private static IReadOnlyList<ClubSquad> ReadClubSquads(SqliteConnection connection)
    {
        if (!TableExists(connection, "ClubSquadMemberState"))
        {
            return Array.Empty<ClubSquad>();
        }

        var membersByClub = new Dictionary<long, List<SquadMember>>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ClubId, PlayerId, SlotIndex, JoinedDayNumber
            FROM ClubSquadMemberState
            ORDER BY ClubId, SlotIndex, PlayerId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var clubId = reader.GetInt64(0);
            if (!membersByClub.TryGetValue(clubId, out var members))
            {
                members = new List<SquadMember>();
                membersByClub[clubId] = members;
            }

            members.Add(SquadMember.Rehydrate(
                new PlayerId(reader.GetInt64(1)),
                reader.GetInt32(2),
                GameDate.FromDayNumber(reader.GetInt32(3))));
        }

        return membersByClub
            .OrderBy(pair => pair.Key)
            .Select(pair => ClubSquad.Rehydrate(new ClubId(pair.Key), pair.Value))
            .ToArray();
    }

    private static IReadOnlyList<PlayerFreeAgency> ReadFreeAgents(SqliteConnection connection)
    {
        if (!TableExists(connection, "PlayerFreeAgencyState"))
        {
            return Array.Empty<PlayerFreeAgency>();
        }

        var freeAgents = new List<PlayerFreeAgency>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT PlayerId, LastClubId, BecameFreeAgentDayNumber
            FROM PlayerFreeAgencyState
            ORDER BY PlayerId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            freeAgents.Add(PlayerFreeAgency.Rehydrate(
                new PlayerId(reader.GetInt64(0)),
                new ClubId(reader.GetInt64(1)),
                GameDate.FromDayNumber(reader.GetInt32(2))));
        }

        return freeAgents;
    }

    private static IReadOnlyList<TacticPlan> ReadTacticPlans(SqliteConnection connection)
    {
        if (!TableExists(connection, "ClubTacticPlanState"))
        {
            return Array.Empty<TacticPlan>();
        }

        var plans = new List<TacticPlan>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ClubId, Formation, Approach, Pressing, DefensiveLine, PassingStyle, LastUpdatedDayNumber
            FROM ClubTacticPlanState
            ORDER BY ClubId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            plans.Add(TacticPlan.Rehydrate(
                new ClubId(reader.GetInt64(0)),
                (Formation)reader.GetInt32(1),
                (TacticalApproach)reader.GetInt32(2),
                (PressingIntensity)reader.GetInt32(3),
                (DefensiveLine)reader.GetInt32(4),
                (PassingStyle)reader.GetInt32(5),
                GameDate.FromDayNumber(reader.GetInt32(6))));
        }

        return plans;
    }

    private static IReadOnlyList<TransferNeed> ReadTransferNeeds(SqliteConnection connection)
    {
        if (!TableExists(connection, "TransferNeedState"))
        {
            return Array.Empty<TransferNeed>();
        }

        var needs = new List<TransferNeed>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT NeedId, ClubId, Kind, Status, Priority, ReasonCode,
                   IdentifiedDayNumber, ClosedDayNumber
            FROM TransferNeedState
            ORDER BY NeedId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            GameDate? closedOn = reader.IsDBNull(7)
                ? null
                : GameDate.FromDayNumber(reader.GetInt32(7));
            needs.Add(TransferNeed.Rehydrate(
                new TransferNeedId(reader.GetInt64(0)),
                new ClubId(reader.GetInt64(1)),
                (TransferNeedKind)reader.GetInt32(2),
                (TransferNeedStatus)reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetString(5),
                GameDate.FromDayNumber(reader.GetInt32(6)),
                closedOn));
        }

        return needs;
    }

    private static IReadOnlyList<ShortlistEntry> ReadShortlistEntries(SqliteConnection connection)
    {
        if (!TableExists(connection, "ShortlistEntryState"))
        {
            return Array.Empty<ShortlistEntry>();
        }

        var entries = new List<ShortlistEntry>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EntryId, ClubId, PlayerId, NeedId, Priority, Status,
                   AddedDayNumber, ArchivedDayNumber
            FROM ShortlistEntryState
            ORDER BY EntryId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            TransferNeedId? needId = reader.IsDBNull(3)
                ? null
                : new TransferNeedId(reader.GetInt64(3));
            GameDate? archivedOn = reader.IsDBNull(7)
                ? null
                : GameDate.FromDayNumber(reader.GetInt32(7));
            entries.Add(ShortlistEntry.Rehydrate(
                new ShortlistEntryId(reader.GetInt64(0)),
                new ClubId(reader.GetInt64(1)),
                new PlayerId(reader.GetInt64(2)),
                needId,
                reader.GetInt32(4),
                (ShortlistEntryStatus)reader.GetInt32(5),
                GameDate.FromDayNumber(reader.GetInt32(6)),
                archivedOn));
        }

        return entries;
    }

    private static IReadOnlyList<TransferTarget> ReadTransferTargets(SqliteConnection connection)
    {
        if (!TableExists(connection, "TransferTargetState"))
        {
            return Array.Empty<TransferTarget>();
        }

        var targets = new List<TransferTarget>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TargetId, NeedId, ClubId, PlayerId, ShortlistEntryId, Status,
                   ListedDayNumber, DroppedDayNumber
            FROM TransferTargetState
            ORDER BY TargetId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ShortlistEntryId? shortlistEntryId = reader.IsDBNull(4)
                ? null
                : new ShortlistEntryId(reader.GetInt64(4));
            GameDate? droppedOn = reader.IsDBNull(7)
                ? null
                : GameDate.FromDayNumber(reader.GetInt32(7));
            targets.Add(TransferTarget.Rehydrate(
                new TransferTargetId(reader.GetInt64(0)),
                new TransferNeedId(reader.GetInt64(1)),
                new ClubId(reader.GetInt64(2)),
                new PlayerId(reader.GetInt64(3)),
                shortlistEntryId,
                (TransferTargetStatus)reader.GetInt32(5),
                GameDate.FromDayNumber(reader.GetInt32(6)),
                droppedOn));
        }

        return targets;
    }

    private static IReadOnlyList<TransferProcess> ReadTransferProcesses(SqliteConnection connection)
    {
        if (!TableExists(connection, "TransferProcessState"))
        {
            return Array.Empty<TransferProcess>();
        }

        var processes = new List<TransferProcess>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProcessId, NeedId, TargetId, BuyingClubId, PlayerId, SellingClubId,
                   IsFreeAgent, Status, FailureReasonCode, OpenedDayNumber, TerminalDayNumber
            FROM TransferProcessState
            ORDER BY ProcessId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ClubId? sellingClubId = reader.IsDBNull(5)
                ? null
                : new ClubId(reader.GetInt64(5));
            string? failureReason = reader.IsDBNull(8) ? null : reader.GetString(8);
            GameDate? terminalOn = reader.IsDBNull(10)
                ? null
                : GameDate.FromDayNumber(reader.GetInt32(10));
            processes.Add(TransferProcess.Rehydrate(
                new TransferProcessId(reader.GetInt64(0)),
                new TransferNeedId(reader.GetInt64(1)),
                new TransferTargetId(reader.GetInt64(2)),
                new ClubId(reader.GetInt64(3)),
                new PlayerId(reader.GetInt64(4)),
                sellingClubId,
                reader.GetInt32(6) != 0,
                (TransferProcessStatus)reader.GetInt32(7),
                failureReason,
                GameDate.FromDayNumber(reader.GetInt32(9)),
                terminalOn));
        }

        return processes;
    }

    private static IReadOnlyList<ClubOffer> ReadClubOffers(SqliteConnection connection)
    {
        if (!TableExists(connection, "ClubOfferState"))
        {
            return Array.Empty<ClubOffer>();
        }

        var offers = new List<ClubOffer>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT OfferId, ProcessId, Round, OfferedFee, Status, SubmittedDayNumber
            FROM ClubOfferState
            ORDER BY OfferId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            offers.Add(ClubOffer.Rehydrate(
                new ClubOfferId(reader.GetInt64(0)),
                new TransferProcessId(reader.GetInt64(1)),
                reader.GetInt32(2),
                reader.GetInt32(3),
                (ClubOfferStatus)reader.GetInt32(4),
                GameDate.FromDayNumber(reader.GetInt32(5))));
        }

        return offers;
    }

    private static IReadOnlyList<PlayerContractProposal> ReadPlayerContractProposals(SqliteConnection connection)
    {
        if (!TableExists(connection, "PlayerContractProposalState"))
        {
            return Array.Empty<PlayerContractProposal>();
        }

        var proposals = new List<PlayerContractProposal>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProposalId, ProcessId, Round, WeeklyWage, ContractYears, Status, SubmittedDayNumber
            FROM PlayerContractProposalState
            ORDER BY ProposalId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            proposals.Add(PlayerContractProposal.Rehydrate(
                new PlayerContractProposalId(reader.GetInt64(0)),
                new TransferProcessId(reader.GetInt64(1)),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                (PlayerContractProposalStatus)reader.GetInt32(5),
                GameDate.FromDayNumber(reader.GetInt32(6))));
        }

        return proposals;
    }

    private static IReadOnlyList<Promise> ReadPromises(SqliteConnection connection)
    {
        if (!TableExists(connection, "PromiseState"))
        {
            return Array.Empty<Promise>();
        }

        var promises = new List<Promise>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT PromiseId, Kind, PromisorKind, PromisorId, PromiseeKind, PromiseeId,
                   ClubId, TargetStarts, StartsGiven, DeadlineDayNumber, CreatedDayNumber,
                   Status, TerminalDayNumber, CountedFixtureIdsCsv
            FROM PromiseState
            ORDER BY PromiseId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            GameDate? terminal = reader.IsDBNull(12)
                ? null
                : GameDate.FromDayNumber(reader.GetInt32(12));
            promises.Add(Promise.Rehydrate(
                new PromiseId(reader.GetInt64(0)),
                (PromiseKind)reader.GetInt32(1),
                new ActorRef((ActorKind)reader.GetInt32(2), reader.GetInt64(3)),
                new ActorRef((ActorKind)reader.GetInt32(4), reader.GetInt64(5)),
                new ClubId(reader.GetInt64(6)),
                reader.GetInt32(7),
                reader.GetInt32(8),
                GameDate.FromDayNumber(reader.GetInt32(9)),
                GameDate.FromDayNumber(reader.GetInt32(10)),
                (PromiseStatus)reader.GetInt32(11),
                terminal,
                ParseLongCsv(reader.GetString(13))));
        }

        return promises;
    }

    private static IReadOnlyList<MemoryRecord> ReadMemories(SqliteConnection connection)
    {
        if (!TableExists(connection, "MemoryState"))
        {
            return Array.Empty<MemoryRecord>();
        }

        var memories = new List<MemoryRecord>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT MemoryId, RememberingActorKind, RememberingActorId, SubjectKind, SubjectId,
                   SourceEventKey, Category, CreatedDayNumber, LastReinforcedDayNumber,
                   BaseImportance, CurrentInfluence, Valence, Visibility, Status,
                   ReinforcementCount, RelatedPromiseId, RuleId, RuleVersion,
                   ProcessedReinforcementKeysCsv
            FROM MemoryState
            ORDER BY MemoryId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            PromiseId? relatedPromise = reader.IsDBNull(15)
                ? null
                : new PromiseId(reader.GetInt64(15));
            var reinforcementKeysCsv = reader.IsDBNull(18) ? string.Empty : reader.GetString(18);
            var reinforcementKeys = string.IsNullOrWhiteSpace(reinforcementKeysCsv)
                ? Array.Empty<string>()
                : reinforcementKeysCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            memories.Add(MemoryRecord.Rehydrate(
                new MemoryId(reader.GetInt64(0)),
                new ActorRef((ActorKind)reader.GetInt32(1), reader.GetInt64(2)),
                (MemorySubjectKind)reader.GetInt32(3),
                reader.GetInt64(4),
                reader.GetString(5),
                (MemoryCategory)reader.GetInt32(6),
                GameDate.FromDayNumber(reader.GetInt32(7)),
                GameDate.FromDayNumber(reader.GetInt32(8)),
                reader.GetInt32(9),
                reader.GetInt32(10),
                (MemoryValence)reader.GetInt32(11),
                (MemoryVisibility)reader.GetInt32(12),
                (MemoryStatus)reader.GetInt32(13),
                reader.GetInt32(14),
                relatedPromise,
                reader.GetString(16),
                reader.GetInt32(17),
                reinforcementKeys));
        }

        return memories;
    }

    private static IReadOnlyList<RelationshipRecord> ReadRelationships(SqliteConnection connection)
    {
        if (!TableExists(connection, "RelationshipState"))
        {
            return Array.Empty<RelationshipRecord>();
        }

        var relationships = new List<RelationshipRecord>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT RelationshipId, ObserverKind, ObserverId, SubjectKind, SubjectId,
                   Trust, Respect, ProfessionalCompatibility, Status,
                   CreatedDayNumber, LastChangedDayNumber, LastChangeReasonCode, ProcessedEffectKeysCsv
            FROM RelationshipState
            ORDER BY RelationshipId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            relationships.Add(RelationshipRecord.Rehydrate(
                new RelationshipId(reader.GetInt64(0)),
                new ActorRef((ActorKind)reader.GetInt32(1), reader.GetInt64(2)),
                new ActorRef((ActorKind)reader.GetInt32(3), reader.GetInt64(4)),
                reader.GetInt32(5),
                reader.GetInt32(6),
                reader.GetInt32(7),
                (RelationshipStatus)reader.GetInt32(8),
                GameDate.FromDayNumber(reader.GetInt32(9)),
                GameDate.FromDayNumber(reader.GetInt32(10)),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                ParseStringCsv(reader.GetString(12))));
        }

        return relationships;
    }

    private static IReadOnlyList<DecisionRequest> ReadDecisionRequests(SqliteConnection connection)
    {
        if (!TableExists(connection, "DecisionRequestState"))
        {
            return Array.Empty<DecisionRequest>();
        }

        var requests = new List<DecisionRequest>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DecisionRequestId, Kind, ManagerId, SubjectPlayerId, ClubId,
                   OpenedDayNumber, DeadlineDayNumber, Status, IsHardBlocker,
                   SelectedOptionCode, ResolvedDayNumber
            FROM DecisionRequestState
            ORDER BY DecisionRequestId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            requests.Add(DecisionRequest.Rehydrate(
                new DecisionRequestId(reader.GetInt64(0)),
                (DecisionRequestKind)reader.GetInt32(1),
                new ManagerId(reader.GetInt64(2)),
                new PlayerId(reader.GetInt64(3)),
                new ClubId(reader.GetInt64(4)),
                GameDate.FromDayNumber(reader.GetInt32(5)),
                GameDate.FromDayNumber(reader.GetInt32(6)),
                (DecisionRequestStatus)reader.GetInt32(7),
                reader.GetInt32(8) != 0,
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : GameDate.FromDayNumber(reader.GetInt32(10))));
        }

        return requests;
    }

    private static IReadOnlyList<DialogueSession> ReadDialogueSessions(SqliteConnection connection)
    {
        if (!TableExists(connection, "DialogueSessionState"))
        {
            return Array.Empty<DialogueSession>();
        }

        var sessions = new List<DialogueSession>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DialogueSessionId, SourceDecisionRequestId, DialogueTypeCode,
                   ManagerId, PrimaryParticipantPlayerId, CreatedDayNumber, DeadlineDayNumber,
                   Status, AvailableOptionCodesCsv, SelectedOptionCode, ResolvedDayNumber
            FROM DialogueSessionState
            ORDER BY DialogueSessionId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var optionsCsv = reader.GetString(8);
            var options = string.IsNullOrWhiteSpace(optionsCsv)
                ? Array.Empty<string>()
                : optionsCsv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            sessions.Add(DialogueSession.Rehydrate(
                new DialogueSessionId(reader.GetInt64(0)),
                new DecisionRequestId(reader.GetInt64(1)),
                reader.GetString(2),
                new ManagerId(reader.GetInt64(3)),
                new PlayerId(reader.GetInt64(4)),
                GameDate.FromDayNumber(reader.GetInt32(5)),
                reader.IsDBNull(6) ? null : GameDate.FromDayNumber(reader.GetInt32(6)),
                (DialogueSessionStatus)reader.GetInt32(7),
                options,
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : GameDate.FromDayNumber(reader.GetInt32(10))));
        }

        return sessions;
    }

    private static IReadOnlyList<ScheduledEvaluation> ReadScheduledEvaluations(SqliteConnection connection)
    {
        if (!TableExists(connection, "ScheduledEvaluationState"))
        {
            return Array.Empty<ScheduledEvaluation>();
        }

        var evaluations = new List<ScheduledEvaluation>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ScheduledEvaluationId, EvaluationTypeCode, DueDayNumber, SourceEventId, Status
            FROM ScheduledEvaluationState
            ORDER BY ScheduledEvaluationId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            Guid? sourceEventId = null;
            if (!reader.IsDBNull(3))
            {
                sourceEventId = Guid.Parse(reader.GetString(3));
            }

            evaluations.Add(ScheduledEvaluation.Rehydrate(
                new ScheduledEvaluationId(reader.GetInt64(0)),
                reader.GetString(1),
                reader.GetInt32(2),
                sourceEventId,
                (ScheduledEvaluationStatus)reader.GetInt32(4)));
        }

        return evaluations;
    }

    private static IReadOnlyList<string> ReadEventEffectProcessingKeys(SqliteConnection connection)
    {
        if (!TableExists(connection, "EventEffectIdempotencyState"))
        {
            return Array.Empty<string>();
        }

        var keys = new List<string>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProcessingKey
            FROM EventEffectIdempotencyState
            ORDER BY ProcessingKey;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            keys.Add(reader.GetString(0));
        }

        return keys;
    }

    private static HubNarrativeUiState ReadHubNarrativeUiState(SqliteConnection connection)
    {
        var matchupPlanHistory = ReadMatchupPlanHistory(connection);
        if (!TableExists(connection, "HubNarrativeUiState"))
        {
            return HubNarrativeUiState.Compose(
                null,
                false,
                null,
                null,
                matchupPlanHistory);
        }

        var hasMatchTrainingColumns =
            ColumnExists(connection, "HubNarrativeUiState", "PendingMatchTrainingFixtureId")
            && ColumnExists(connection, "HubNarrativeUiState", "PendingMatchTrainingPriorityCode")
            && ColumnExists(connection, "HubNarrativeUiState", "PendingMatchTrainingModifier");
        using var command = connection.CreateCommand();
        command.CommandText = hasMatchTrainingColumns
            ? """
                SELECT WeekStoryClosureBeat, WeekStoryDismissOnNextAdvance,
                       CleanXiNamesCsv, InjuryClearedNamesCsv,
                       PendingMatchTrainingFixtureId, PendingMatchTrainingPriorityCode,
                       PendingMatchTrainingModifier
                FROM HubNarrativeUiState
                WHERE SingletonId = 1
                LIMIT 1;
                """
            : """
                SELECT WeekStoryClosureBeat, WeekStoryDismissOnNextAdvance,
                       CleanXiNamesCsv, InjuryClearedNamesCsv
                FROM HubNarrativeUiState
                WHERE SingletonId = 1
                LIMIT 1;
                """;
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return HubNarrativeUiState.Compose(
                null,
                false,
                null,
                null,
                matchupPlanHistory);
        }

        var beat = reader.IsDBNull(0) ? null : reader.GetString(0);
        var dismiss = !reader.IsDBNull(1) && reader.GetInt32(1) != 0;
        var cleanCsv = reader.IsDBNull(2) ? null : reader.GetString(2);
        var clearedCsv = reader.IsDBNull(3) ? null : reader.GetString(3);
        var pendingFixture = hasMatchTrainingColumns && !reader.IsDBNull(4)
            ? reader.GetInt64(4)
            : (long?)null;
        var pendingPriority = hasMatchTrainingColumns && !reader.IsDBNull(5)
            ? reader.GetString(5)
            : null;
        var pendingModifier = hasMatchTrainingColumns && !reader.IsDBNull(6)
            ? reader.GetInt32(6)
            : (int?)null;
        return HubNarrativeUiState.Compose(
            beat,
            dismiss,
            SplitCsv(cleanCsv),
            SplitCsv(clearedCsv),
            matchupPlanHistory,
            pendingFixture,
            pendingPriority,
            pendingModifier);
    }

    private static IReadOnlyList<MatchupPlanNotebookEntry> ReadMatchupPlanHistory(
        SqliteConnection connection)
    {
        if (!TableExists(connection, "MatchupPlanNotebookState"))
        {
            return Array.Empty<MatchupPlanNotebookEntry>();
        }

        var history = new List<MatchupPlanNotebookEntry>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DayNumber, OpponentName, SelectionLine, ThreatKind,
                   PlanSignal, OutcomeSignal, VerdictLine
            FROM MatchupPlanNotebookState
            ORDER BY SequenceIndex;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            history.Add(MatchupPlanNotebookEntry.Compose(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                (OpponentThreatKind)reader.GetInt32(3),
                (MatchupPlanSignal)reader.GetInt32(4),
                (MatchupPlanOutcomeSignal)reader.GetInt32(5),
                reader.GetString(6)));
        }

        return history;
    }

    private static string BuildHubNarrativeCanonicalText(HubNarrativeUiState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.IsEmpty)
        {
            return string.Empty;
        }

        var beat = state.WeekStoryClosureBeat ?? string.Empty;
        var dismiss = state.WeekStoryDismissOnNextAdvance ? "1" : "0";
        var clean = string.Join('|', state.CleanXiNames.OrderBy(n => n, StringComparer.Ordinal));
        var cleared = string.Join('|', state.InjuryClearedNames.OrderBy(n => n, StringComparer.Ordinal));
        var baseText = $"hub|{beat}|{dismiss}|{clean}|{cleared}";
        if (state.PendingMatchTrainingFixtureId is long fixtureId)
        {
            baseText += $"|training:{fixtureId}:{state.PendingMatchTrainingPriorityCode}:"
                + state.PendingMatchTrainingModifier;
        }
        if (state.MatchupPlanHistory.Count == 0)
        {
            return baseText;
        }

        var notebook = string.Join(
            "||",
            state.MatchupPlanHistory
                .OrderBy(entry => entry.DayNumber)
                .Select(entry => string.Join(
                    '|',
                    entry.DayNumber,
                    entry.OpponentName,
                    entry.SelectionLine,
                    (int)entry.ThreatKind,
                    (int)entry.PlanSignal,
                    (int)entry.OutcomeSignal,
                    entry.VerdictLine)));
        return $"{baseText}|notebook|{notebook}";
    }

    private static IReadOnlyList<string> SplitCsv(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<string>()
            : csv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IReadOnlyList<DisciplinaryAction> ReadDisciplinaryActions(SqliteConnection connection)
    {
        if (!TableExists(connection, "DisciplinaryActionState"))
        {
            return Array.Empty<DisciplinaryAction>();
        }

        var actions = new List<DisciplinaryAction>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DisciplinaryActionId, Kind, ManagerId, SubjectPlayerId, ClubId,
                   SourceDecisionRequestId, AppliedDayNumber
            FROM DisciplinaryActionState
            ORDER BY DisciplinaryActionId;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            actions.Add(DisciplinaryAction.Rehydrate(
                new DisciplinaryActionId(reader.GetInt64(0)),
                (DisciplinaryActionKind)reader.GetInt32(1),
                new ManagerId(reader.GetInt64(2)),
                new PlayerId(reader.GetInt64(3)),
                new ClubId(reader.GetInt64(4)),
                reader.IsDBNull(5) ? null : new DecisionRequestId(reader.GetInt64(5)),
                GameDate.FromDayNumber(reader.GetInt32(6))));
        }

        return actions;
    }

    private static IReadOnlyList<long> ParseLongCsv(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<long>();
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(long.Parse)
            .ToArray();
    }

    private static IReadOnlyList<string> ParseStringCsv(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<string>();
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static IReadOnlyList<int> ParseSlotCsv(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<int>();
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToArray();
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM sqlite_master
            WHERE type = 'table' AND name = $tableName
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$tableName", tableName);
        return command.ExecuteScalar() is not null;
    }

    private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
