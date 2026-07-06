using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Infrastructure.WorldCalendar;

internal static class WorldCalendarSqliteMigrator
{
    public static void MigrateInPlace(string filePath, int fromVersion)
    {
        if (fromVersion != 1)
        {
            throw new UnsupportedSaveSchemaVersionException(fromVersion);
        }

        var backupPath = filePath + ".bak";
        File.Copy(filePath, backupPath, overwrite: true);

        var workingCopyPath = filePath + ".migrating.tmp";

        if (File.Exists(workingCopyPath))
        {
            File.Delete(workingCopyPath);
        }

        File.Copy(filePath, workingCopyPath, overwrite: false);

        try
        {
            MigrateV1ToV2(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                $"V{fromVersion} production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV1ToV2(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(
                connection,
                alterTransaction,
                "ALTER TABLE WorldTimelineState ADD COLUMN CheckpointLabel TEXT NULL;");
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 2);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV2ToV3InPlace(string filePath)
    {
        var backupPath = filePath + ".bak";
        File.Copy(filePath, backupPath, overwrite: true);

        var workingCopyPath = filePath + ".migrating.tmp";

        if (File.Exists(workingCopyPath))
        {
            File.Delete(workingCopyPath);
        }

        File.Copy(filePath, workingCopyPath, overwrite: false);

        try
        {
            MigrateV2ToV3(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V2 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV2ToV3(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE LeagueCompetitionState (
                    SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                    CompetitionId INTEGER NOT NULL
                );
                """);

            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE CompetitionSeasonState (
                    SeasonId INTEGER PRIMARY KEY,
                    PreseasonStartDayNumber INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    ActiveStartedAtDayNumber INTEGER NULL,
                    CompletedAtDayNumber INTEGER NULL,
                    ArchivedAtDayNumber INTEGER NULL
                );
                """);

            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE SeasonParticipantState (
                    SeasonId INTEGER NOT NULL,
                    ClubId INTEGER NOT NULL,
                    PRIMARY KEY (SeasonId, ClubId)
                );
                """);

            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
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

            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                INSERT INTO LeagueCompetitionState (SingletonId, CompetitionId)
                VALUES (1, 1);
                """);

            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", ProductionWorldCalendarSaveSchema.CurrentVersion);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
    }
}
