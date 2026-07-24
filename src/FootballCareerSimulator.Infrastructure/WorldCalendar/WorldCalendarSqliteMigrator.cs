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
        updateCommand.Parameters.AddWithValue("$version", 6);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV6ToV7InPlace(string filePath)
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
            MigrateV6ToV7(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V6 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV6ToV7(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE MatchSelectionState (
                    FixtureId INTEGER NOT NULL,
                    ClubId INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    StartingSlotsCsv TEXT NOT NULL,
                    BenchSlotsCsv TEXT NOT NULL,
                    PRIMARY KEY (FixtureId, ClubId)
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 7);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV7ToV8InPlace(string filePath)
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
            MigrateV7ToV8(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V7 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV7ToV8(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE ManagerCareerState ADD COLUMN SeasonExpectation INTEGER NOT NULL DEFAULT 3;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE ManagerCareerState ADD COLUMN BoardConfidence INTEGER NOT NULL DEFAULT 55;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE ManagerCareerState ADD COLUMN EmploymentRiskBand INTEGER NOT NULL DEFAULT 2;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE ManagerCareerState ADD COLUMN LastAssessedFixtureId INTEGER NULL;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE ManagerCareerState ADD COLUMN LastAssessmentReasonCode TEXT NULL;
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 8);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV8ToV9InPlace(string filePath)
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
            MigrateV8ToV9(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V8 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV8ToV9(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE ManagerCareerState_v9 (
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
                    DismissedAtDayNumber INTEGER NULL
                );
                """);

            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                INSERT INTO ManagerCareerState_v9 (
                    SingletonId, ManagerId, DisplayName, EmployedClubId, EmploymentStartedDayNumber,
                    SeasonExpectation, BoardConfidence, EmploymentRiskBand,
                    LastAssessedFixtureId, LastAssessmentReasonCode,
                    EmploymentStatus, EmploymentEndReason, LastClubId,
                    DismissedDueToFixtureId, DismissedAtDayNumber)
                SELECT
                    SingletonId, ManagerId, DisplayName, EmployedClubId, EmploymentStartedDayNumber,
                    SeasonExpectation, BoardConfidence, EmploymentRiskBand,
                    LastAssessedFixtureId, LastAssessmentReasonCode,
                    1, NULL, EmployedClubId,
                    NULL, NULL
                FROM ManagerCareerState;
                """);

            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                DROP TABLE ManagerCareerState;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE ManagerCareerState_v9 RENAME TO ManagerCareerState;
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 9);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV9ToV10InPlace(string filePath)
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
            MigrateV9ToV10(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V9 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV9ToV10(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE ManagerCareerState ADD COLUMN PendingOfferId INTEGER NULL;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE ManagerCareerState ADD COLUMN PendingOfferClubId INTEGER NULL;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE ManagerCareerState ADD COLUMN PendingOfferStatus INTEGER NULL;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE ManagerCareerState ADD COLUMN PendingOfferCreatedDayNumber INTEGER NULL;
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 10);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV10ToV11InPlace(string filePath)
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
            MigrateV10ToV11(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V10 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV10ToV11(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS WeeklyTrainingPlanState (
                    ClubId INTEGER PRIMARY KEY,
                    Focus INTEGER NOT NULL,
                    Intensity INTEGER NOT NULL,
                    RestApproach INTEGER NOT NULL,
                    SetAtDayNumber INTEGER NOT NULL
                );
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS PlayerPhysicalState (
                    ClubId INTEGER NOT NULL,
                    SlotIndex INTEGER NOT NULL,
                    Fatigue INTEGER NOT NULL,
                    Fitness INTEGER NOT NULL,
                    PRIMARY KEY (ClubId, SlotIndex)
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 11);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV11ToV12InPlace(string filePath)
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
            MigrateV11ToV12(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V11 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV11ToV12(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE PlayerPhysicalState ADD COLUMN InjurySeverity INTEGER NOT NULL DEFAULT 0;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE PlayerPhysicalState ADD COLUMN InjuredUntilDayNumber INTEGER NULL;
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 12);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV12ToV13InPlace(string filePath)
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
            MigrateV12ToV13(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V12 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV12ToV13(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS PlayerCareerState (
                    PlayerId INTEGER PRIMARY KEY,
                    OriginClubId INTEGER NOT NULL,
                    SlotIndex INTEGER NOT NULL,
                    CurrentAbility INTEGER NOT NULL,
                    PotentialAbility INTEGER NOT NULL,
                    DevelopmentPoints INTEGER NOT NULL,
                    LastDevelopedDayNumber INTEGER NULL
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 13);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV13ToV14InPlace(string filePath)
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
            MigrateV13ToV14(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V13 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV13ToV14(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE PlayerCareerState ADD COLUMN BirthYear INTEGER NOT NULL DEFAULT 2000;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                ALTER TABLE PlayerCareerState ADD COLUMN LastAgedCalendarYear INTEGER NULL;
                """);
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                UPDATE PlayerCareerState
                SET BirthYear = 2008 - (SlotIndex % 15);
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 14);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV14ToV15InPlace(string filePath)
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
            MigrateV14ToV15(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V14 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV14ToV15(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS PlayerContractState (
                    ContractId INTEGER PRIMARY KEY,
                    PlayerId INTEGER NOT NULL,
                    ClubId INTEGER NOT NULL,
                    StartDayNumber INTEGER NOT NULL,
                    EndDayNumber INTEGER NOT NULL,
                    WeeklyWage INTEGER NOT NULL,
                    Status INTEGER NOT NULL
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 15);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV15ToV16InPlace(string filePath)
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
            MigrateV15ToV16(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V15 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV15ToV16(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS ClubSquadMemberState (
                    ClubId INTEGER NOT NULL,
                    PlayerId INTEGER NOT NULL,
                    SlotIndex INTEGER NOT NULL,
                    JoinedDayNumber INTEGER NOT NULL,
                    PRIMARY KEY (ClubId, PlayerId)
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 16);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV16ToV17InPlace(string filePath)
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
            MigrateV16ToV17(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V16 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV16ToV17(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS PlayerFreeAgencyState (
                    PlayerId INTEGER PRIMARY KEY,
                    LastClubId INTEGER NOT NULL,
                    BecameFreeAgentDayNumber INTEGER NOT NULL
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 17);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV17ToV18InPlace(string filePath)
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
            MigrateV17ToV18(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V17 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV17ToV18(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS ClubTacticPlanState (
                    ClubId INTEGER PRIMARY KEY,
                    Formation INTEGER NOT NULL,
                    Approach INTEGER NOT NULL,
                    LastUpdatedDayNumber INTEGER NOT NULL
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 18);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV18ToV19InPlace(string filePath)
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
            MigrateV18ToV19(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V18 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV18ToV19(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS TransferNeedState (
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
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 19);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV19ToV20InPlace(string filePath)
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
            MigrateV19ToV20(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V19 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV19ToV20(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS ShortlistEntryState (
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
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS TransferTargetState (
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
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 20);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV20ToV21InPlace(string filePath)
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
            MigrateV20ToV21(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V20 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV20ToV21(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS TransferProcessState (
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
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 21);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV21ToV22InPlace(string filePath)
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
            MigrateV21ToV22(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V21 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV21ToV22(string workingCopyPath)
    {
        // Sporting Approval yalnızca Process Status enum genişlemesi; tablo değişikliği yok.
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 22);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV22ToV23InPlace(string filePath)
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
            MigrateV22ToV23(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V22 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV22ToV23(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS ClubOfferState (
                    OfferId INTEGER PRIMARY KEY,
                    ProcessId INTEGER NOT NULL,
                    Round INTEGER NOT NULL,
                    OfferedFee INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    SubmittedDayNumber INTEGER NOT NULL
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 23);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV23ToV24InPlace(string filePath)
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
            MigrateV23ToV24(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V23 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV23ToV24(string workingCopyPath)
    {
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS PlayerContractProposalState (
                    ProposalId INTEGER PRIMARY KEY,
                    ProcessId INTEGER NOT NULL,
                    Round INTEGER NOT NULL,
                    WeeklyWage INTEGER NOT NULL,
                    ContractYears INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    SubmittedDayNumber INTEGER NOT NULL
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 24);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV24ToV25InPlace(string filePath)
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
            MigrateV24ToV25(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V24 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        SqliteConnection.ClearAllPools();
        File.Move(workingCopyPath, filePath, overwrite: true);
    }

    private static void MigrateV24ToV25(string workingCopyPath)
    {
        // Financial Approval yalnızca Process Status enum genişlemesi; tablo değişikliği yok.
        using var connection = new SqliteConnection($"Data Source={workingCopyPath}");
        connection.Open();

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 25);
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
