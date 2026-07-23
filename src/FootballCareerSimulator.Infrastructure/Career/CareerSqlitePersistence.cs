using FootballCareerSimulator.Application.Career.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
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
        IReadOnlyList<PlayerPhysicalState> physicalStates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(league);
        ArgumentNullException.ThrowIfNull(clubRegistry);
        ArgumentNullException.ThrowIfNull(managerCareer);
        ArgumentNullException.ThrowIfNull(matchSelections);
        ArgumentNullException.ThrowIfNull(trainingPlans);
        ArgumentNullException.ThrowIfNull(physicalStates);

        var canonicalHash = CareerCanonicalStateHasher.ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates);
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
                PRIMARY KEY (ClubId, SlotIndex)
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
                    ClubId, SlotIndex, Fatigue, Fitness)
                VALUES ($clubId, $slotIndex, $fatigue, $fitness);
                """;
            command.Parameters.AddWithValue("$clubId", state.ClubId.Value);
            command.Parameters.AddWithValue("$slotIndex", state.SlotIndex);
            command.Parameters.AddWithValue("$fatigue", state.Fatigue);
            command.Parameters.AddWithValue("$fitness", state.Fitness);
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
        var canonicalHash = CareerCanonicalStateHasher.ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates);

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
            }

            SqliteConnection.ClearAllPools();

            var recomputedHash = CareerCanonicalStateHasher.ComputeHash(
                timeline,
                league,
                clubRegistry,
                managerCareer,
                matchSelections,
                trainingPlans,
                physicalStates);
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
            SELECT ClubId, SlotIndex, Fatigue, Fitness
            FROM PlayerPhysicalState
            ORDER BY ClubId, SlotIndex;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            states.Add(PlayerPhysicalState.Rehydrate(
                new ClubId(reader.GetInt64(0)),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3)));
        }

        return states;
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
