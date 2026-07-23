using FootballCareerSimulator.Application.Career.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
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
        IReadOnlyList<TransferProcess> transferProcesses)
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
            transferProcesses);
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
                SportiveStrength INTEGER NOT NULL
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
                PendingOfferCreatedDayNumber INTEGER NULL
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
                LastAgedCalendarYear INTEGER NULL
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

        if (timeline.ActivePlanningPeriod is not { } period)
        {
            return;
        }

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
                INSERT INTO ClubState (ClubId, DisplayName, ClubCode, SportiveStrength)
                VALUES ($clubId, $displayName, $clubCode, $sportiveStrength);
                """;
            command.Parameters.AddWithValue("$clubId", club.Id.Value);
            command.Parameters.AddWithValue("$displayName", club.DisplayName);
            command.Parameters.AddWithValue("$clubCode", club.Code.Value);
            command.Parameters.AddWithValue("$sportiveStrength", club.SportiveStrength);
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
                PendingOfferId, PendingOfferClubId, PendingOfferStatus, PendingOfferCreatedDayNumber)
            VALUES (
                1, $managerId, $displayName, $employedClubId, $employmentStartedDayNumber,
                $seasonExpectation, $boardConfidence, $riskBand,
                $lastAssessedFixtureId, $lastAssessmentReasonCode,
                $employmentStatus, $employmentEndReason, $lastClubId,
                $dismissedDueToFixtureId, $dismissedAtDayNumber,
                $pendingOfferId, $pendingOfferClubId, $pendingOfferStatus, $pendingOfferCreatedDayNumber);
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
        command.ExecuteNonQuery();
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
                    DevelopmentPoints, LastDevelopedDayNumber, BirthYear, LastAgedCalendarYear)
                VALUES ($playerId, $clubId, $slotIndex, $ca, $pa, $dp, $lastDay, $birthYear, $agedYear);
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
                    ClubId, Formation, Approach, LastUpdatedDayNumber)
                VALUES ($clubId, $formation, $approach, $lastUpdated);
                """;
            command.Parameters.AddWithValue("$clubId", plan.ClubId.Value);
            command.Parameters.AddWithValue("$formation", (int)plan.Formation);
            command.Parameters.AddWithValue("$approach", (int)plan.Approach);
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
            transferProcesses);

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
                transferProcesses);
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
                schemaVersion,
                wasMigrated);
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
            checkpointLabel: null);
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
            SELECT ClubId, DisplayName, ClubCode, SportiveStrength
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
                reader.GetInt32(3)));
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

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ManagerId, DisplayName, EmployedClubId, EmploymentStartedDayNumber,
                   SeasonExpectation, BoardConfidence, EmploymentRiskBand,
                   LastAssessedFixtureId, LastAssessmentReasonCode,
                   EmploymentStatus, EmploymentEndReason, LastClubId,
                   DismissedDueToFixtureId, DismissedAtDayNumber,
                   PendingOfferId, PendingOfferClubId, PendingOfferStatus, PendingOfferCreatedDayNumber
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
            pendingOfferCreatedDayNumber: reader.FieldCount > 17 && !reader.IsDBNull(17) ? reader.GetInt32(17) : null);
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
                   DevelopmentPoints, LastDevelopedDayNumber, BirthYear, LastAgedCalendarYear
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
                agedYear));
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
            SELECT ClubId, Formation, Approach, LastUpdatedDayNumber
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
                GameDate.FromDayNumber(reader.GetInt32(3))));
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
}
