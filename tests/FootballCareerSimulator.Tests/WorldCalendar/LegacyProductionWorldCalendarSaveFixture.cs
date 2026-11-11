using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.WorldCalendar;

internal static class LegacyProductionWorldCalendarSaveFixture
{
    public static void CreateV1File(
        string filePath,
        int currentDayNumber,
        long lastCommittedStepId,
        int rootSeed,
        string rngVersion,
        int rngDrawCount,
        string canonicalStateHash)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using var connection = new SqliteConnection($"Data Source={filePath}");
        connection.Open();

        using var transaction = connection.BeginTransaction();

        Execute(connection, transaction, """
            CREATE TABLE ProductionSaveManifest (
                SchemaVersion INTEGER NOT NULL,
                SaveFormatId TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                CanonicalStateHash TEXT NOT NULL
            );
            """);

        Execute(connection, transaction, """
            CREATE TABLE WorldTimelineState (
                SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                CurrentDayNumber INTEGER NOT NULL,
                LastCommittedStepId INTEGER NOT NULL,
                RootSeed INTEGER NOT NULL,
                RngVersion TEXT NOT NULL,
                RngDrawCount INTEGER NOT NULL
            );
            """);

        using (var manifestCommand = connection.CreateCommand())
        {
            manifestCommand.Transaction = transaction;
            manifestCommand.CommandText = """
                INSERT INTO ProductionSaveManifest (SchemaVersion, SaveFormatId, CreatedAtUtc, CanonicalStateHash)
                VALUES (1, 'WorldCalendar', '2026-07-06T00:00:00.0000000Z', $hash);
                """;
            manifestCommand.Parameters.AddWithValue("$hash", canonicalStateHash);
            manifestCommand.ExecuteNonQuery();
        }

        using (var timelineCommand = connection.CreateCommand())
        {
            timelineCommand.Transaction = transaction;
            timelineCommand.CommandText = """
                INSERT INTO WorldTimelineState (
                    SingletonId, CurrentDayNumber, LastCommittedStepId, RootSeed, RngVersion, RngDrawCount)
                VALUES (1, $dayNumber, $stepId, $rootSeed, $rngVersion, $rngDrawCount);
                """;
            timelineCommand.Parameters.AddWithValue("$dayNumber", currentDayNumber);
            timelineCommand.Parameters.AddWithValue("$stepId", lastCommittedStepId);
            timelineCommand.Parameters.AddWithValue("$rootSeed", rootSeed);
            timelineCommand.Parameters.AddWithValue("$rngVersion", rngVersion);
            timelineCommand.Parameters.AddWithValue("$rngDrawCount", rngDrawCount);
            timelineCommand.ExecuteNonQuery();
        }

        transaction.Commit();
        SqliteConnection.ClearAllPools();
    }

    public static void CreateV2File(
        string filePath,
        int currentDayNumber,
        long lastCommittedStepId,
        int rootSeed,
        string rngVersion,
        int rngDrawCount,
        string canonicalStateHash)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using var connection = new SqliteConnection($"Data Source={filePath}");
        connection.Open();

        using var transaction = connection.BeginTransaction();

        Execute(connection, transaction, """
            CREATE TABLE ProductionSaveManifest (
                SchemaVersion INTEGER NOT NULL,
                SaveFormatId TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                CanonicalStateHash TEXT NOT NULL
            );
            """);

        Execute(connection, transaction, """
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

        Execute(connection, transaction, """
            CREATE TABLE PlanningPeriodState (
                SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                PlanningPeriodId INTEGER NOT NULL,
                StartDayNumber INTEGER NOT NULL,
                ExpectedEndDayNumber INTEGER NULL,
                Status INTEGER NOT NULL,
                CompletedAtDayNumber INTEGER NULL
            );
            """);

        using (var manifestCommand = connection.CreateCommand())
        {
            manifestCommand.Transaction = transaction;
            manifestCommand.CommandText = """
                INSERT INTO ProductionSaveManifest (SchemaVersion, SaveFormatId, CreatedAtUtc, CanonicalStateHash)
                VALUES (2, 'WorldCalendar', '2026-07-06T00:00:00.0000000Z', $hash);
                """;
            manifestCommand.Parameters.AddWithValue("$hash", canonicalStateHash);
            manifestCommand.ExecuteNonQuery();
        }

        using (var timelineCommand = connection.CreateCommand())
        {
            timelineCommand.Transaction = transaction;
            timelineCommand.CommandText = """
                INSERT INTO WorldTimelineState (
                    SingletonId, CurrentDayNumber, LastCommittedStepId, RootSeed, RngVersion, RngDrawCount, CheckpointLabel)
                VALUES (1, $dayNumber, $stepId, $rootSeed, $rngVersion, $rngDrawCount, NULL);
                """;
            timelineCommand.Parameters.AddWithValue("$dayNumber", currentDayNumber);
            timelineCommand.Parameters.AddWithValue("$stepId", lastCommittedStepId);
            timelineCommand.Parameters.AddWithValue("$rootSeed", rootSeed);
            timelineCommand.Parameters.AddWithValue("$rngVersion", rngVersion);
            timelineCommand.Parameters.AddWithValue("$rngDrawCount", rngDrawCount);
            timelineCommand.ExecuteNonQuery();
        }

        transaction.Commit();
        SqliteConnection.ClearAllPools();
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
