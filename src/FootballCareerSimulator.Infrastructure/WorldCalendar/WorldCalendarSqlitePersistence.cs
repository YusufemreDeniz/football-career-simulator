using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.WorldCalendar;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Infrastructure.WorldCalendar;

public sealed class WorldCalendarSqlitePersistence : IWorldCalendarPersistence
{
    public void Save(string filePath, WorldTimeline timeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(timeline);

        var canonicalHash = WorldTimelineCanonicalStateHasher.ComputeHash(timeline);
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

            transaction.Commit();
        }

        SqliteConnection.ClearAllPools();
        File.Move(tempPath, filePath, overwrite: true);
    }

    public WorldCalendarLoadResult Load(string filePath)
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

        if (schemaVersion.Version < ProductionWorldCalendarSaveSchema.CurrentVersion)
        {
            WorldCalendarSqliteMigrator.MigrateInPlace(filePath, schemaVersion.Version);
            wasMigrated = true;
        }

        return ReadCurrentVersion(filePath, wasMigrated);
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

    private static WorldCalendarLoadResult ReadCurrentVersion(string filePath, bool wasMigrated)
    {
        try
        {
            string canonicalHash;
            int currentDayNumber;
            long lastCommittedStepId;
            int rootSeed;
            string rngVersion;
            int rngDrawCount;
            string? checkpointLabel;
            long? planningPeriodId = null;
            int? planningStartDayNumber = null;
            int? planningExpectedEndDayNumber = null;
            int? planningStatus = null;
            int? planningCompletedAtDayNumber = null;

            using (var connection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly"))
            {
                connection.Open();

                using (var manifestCommand = connection.CreateCommand())
                {
                    manifestCommand.CommandText = "SELECT CanonicalStateHash FROM ProductionSaveManifest LIMIT 1;";
                    canonicalHash = (string)manifestCommand.ExecuteScalar()!;
                }

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
                    checkpointLabel = reader.IsDBNull(5) ? null : reader.GetString(5);
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
            }

            SqliteConnection.ClearAllPools();

            var timeline = WorldCalendarSnapshotMapper.ToDomain(
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
                checkpointLabel);

            var recomputedHash = WorldTimelineCanonicalStateHasher.ComputeHash(timeline);
            if (!string.Equals(recomputedHash, canonicalHash, StringComparison.Ordinal))
            {
                throw new SaveCorruptionException(
                    $"Bütünlük hash'i eşleşmiyor (beklenen: {canonicalHash}, hesaplanan: {recomputedHash}); save bozulmuş olabilir.");
            }

            return new WorldCalendarLoadResult(timeline, ProductionWorldCalendarSaveSchema.CurrentVersion, wasMigrated);
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
