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
        updateCommand.Parameters.AddWithValue("$version", 3);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV3ToV4InPlace(string filePath)
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
            MigrateV3ToV4(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V3 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV3ToV4(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE ClubState (
                    ClubId INTEGER PRIMARY KEY,
                    DisplayName TEXT NOT NULL,
                    ClubCode TEXT NOT NULL,
                    SportiveStrength INTEGER NOT NULL
                );
                """);

            var defaultRegistry = FootballCareerSimulator.Domain.ClubGovernance.LeagueClubRegistry.CreateMvpLeague();
            foreach (var club in defaultRegistry.Clubs)
            {
                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = alterTransaction;
                insertCommand.CommandText = """
                    INSERT INTO ClubState (ClubId, DisplayName, ClubCode, SportiveStrength)
                    VALUES ($clubId, $displayName, $clubCode, $sportiveStrength);
                    """;
                insertCommand.Parameters.AddWithValue("$clubId", club.Id.Value);
                insertCommand.Parameters.AddWithValue("$displayName", club.DisplayName);
                insertCommand.Parameters.AddWithValue("$clubCode", club.Code.Value);
                insertCommand.Parameters.AddWithValue("$sportiveStrength", club.SportiveStrength);
                insertCommand.ExecuteNonQuery();
            }

            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 4);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV4ToV5InPlace(string filePath)
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
            MigrateV4ToV5(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V4 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV4ToV5(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE FixtureState ADD COLUMN HomeGoals INTEGER NULL;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE FixtureState ADD COLUMN AwayGoals INTEGER NULL;
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 5);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV5ToV6InPlace(string filePath)
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
            MigrateV5ToV6(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V5 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV5ToV6(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        int currentDayNumber = 1;
        using (var dayCommand = connection.CreateCommand())
        {
            dayCommand.CommandText = "SELECT CurrentDayNumber FROM WorldTimelineState WHERE SingletonId = 1;";
            var scalar = dayCommand.ExecuteScalar();
            if (scalar is not null)
            {
                currentDayNumber = Convert.ToInt32(scalar);
            }
        }

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE ManagerCareerState (
                    SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                    ManagerId INTEGER NOT NULL,
                    DisplayName TEXT NOT NULL,
                    EmployedClubId INTEGER NOT NULL,
                    EmploymentStartedDayNumber INTEGER NOT NULL
                );
                """);

            using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = alterTransaction;
            insertCommand.CommandText = """
                INSERT INTO ManagerCareerState (
                    SingletonId, ManagerId, DisplayName, EmployedClubId, EmploymentStartedDayNumber)
                VALUES (1, 1, 'Teknik Direktör', 1, $startedDay);
                """;
            insertCommand.Parameters.AddWithValue("$startedDay", currentDayNumber);
            insertCommand.ExecuteNonQuery();

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
