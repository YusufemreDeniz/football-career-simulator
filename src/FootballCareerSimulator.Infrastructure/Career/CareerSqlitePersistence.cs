using FootballCareerSimulator.Application.Career.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.WorldCalendar;
using FootballCareerSimulator.Simulation.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Infrastructure.Career;

public sealed class CareerSqlitePersistence : ICareerPersistence
{
    public void Save(string filePath, WorldTimeline timeline, LeagueCompetition league)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(league);

        var canonicalHash = CareerCanonicalStateHasher.ComputeHash(timeline, league);
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
                Status INTEGER NOT NULL
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
                        FixtureId, SeasonId, HomeClubId, AwayClubId, Round, ScheduledDayNumber, Status)
                    VALUES (
                        $fixtureId, $seasonId, $homeClubId, $awayClubId, $round, $scheduledDayNumber, $status);
                    """;
                fixtureCommand.Parameters.AddWithValue("$fixtureId", fixture.Id.Value);
                fixtureCommand.Parameters.AddWithValue("$seasonId", season.SeasonId.Value);
                fixtureCommand.Parameters.AddWithValue("$homeClubId", fixture.HomeClubId.Value);
                fixtureCommand.Parameters.AddWithValue("$awayClubId", fixture.AwayClubId.Value);
                fixtureCommand.Parameters.AddWithValue("$round", fixture.Round.Value);
                fixtureCommand.Parameters.AddWithValue("$scheduledDayNumber", fixture.ScheduledDate.DayNumber);
                fixtureCommand.Parameters.AddWithValue("$status", (int)fixture.Status);
                fixtureCommand.ExecuteNonQuery();
            }
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
        var canonicalHash = CareerCanonicalStateHasher.ComputeHash(timeline, league);

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
            }

            SqliteConnection.ClearAllPools();

            var recomputedHash = CareerCanonicalStateHasher.ComputeHash(timeline, league);
            if (!string.Equals(recomputedHash, canonicalHash, StringComparison.Ordinal))
            {
                throw new SaveCorruptionException(
                    $"Bütünlük hash'i eşleşmiyor (beklenen: {canonicalHash}, hesaplanan: {recomputedHash}); save bozulmuş olabilir.");
            }

            return new CareerLoadResult(timeline, league, schemaVersion, wasMigrated);
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
                SELECT FixtureId, SeasonId, HomeClubId, AwayClubId, Round, ScheduledDayNumber, Status
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
                    reader.GetInt32(6)));
            }
        }

        return CareerSnapshotMapper.ToLeague(competitionId, seasons, participants, fixtures);
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
