using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Infrastructure.WorldCalendar;

internal static class WorldCalendarSqliteMigrator
{
    private static SqliteConnection OpenMigrationConnection(string workingCopyPath) =>
        new($"Data Source={workingCopyPath};Pooling=False");

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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV1ToV2(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV2ToV3(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV3ToV4(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV4ToV5(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV5ToV6(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV6ToV7(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV7ToV8(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV8ToV9(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV9ToV10(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV10ToV11(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV11ToV12(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV12ToV13(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV13ToV14(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV14ToV15(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV15ToV16(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV16ToV17(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV17ToV18(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV18ToV19(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV19ToV20(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV20ToV21(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV21ToV22(string workingCopyPath)
    {
        // Sporting Approval yalnızca Process Status enum genişlemesi; tablo değişikliği yok.
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV22ToV23(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV23ToV24(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
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

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV24ToV25(string workingCopyPath)
    {
        // Financial Approval yalnızca Process Status enum genişlemesi; tablo değişikliği yok.
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 25);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV25ToV26InPlace(string filePath)
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
            MigrateV25ToV26(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V25 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV25ToV26(string workingCopyPath)
    {
        // Transfer Completion yalnızca Process Status enum genişlemesi; tablo değişikliği yok.
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 26);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV26ToV27InPlace(string filePath)
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
            MigrateV26ToV27(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V26 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV26ToV27(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS TransferWindowState (
                    SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                    Status INTEGER NOT NULL,
                    OpenedOnDayNumber INTEGER NULL,
                    ClosesOnDayNumber INTEGER NULL
                );
                """);

            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                INSERT INTO TransferWindowState (SingletonId, Status, OpenedOnDayNumber, ClosesOnDayNumber)
                SELECT 1, 2, CurrentDayNumber, NULL
                FROM WorldTimelineState
                WHERE SingletonId = 1
                  AND NOT EXISTS (SELECT 1 FROM TransferWindowState WHERE SingletonId = 1);
                """);

            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 27);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV27ToV28InPlace(string filePath)
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
            MigrateV27ToV28(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V27 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV27ToV28(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(
                connection,
                alterTransaction,
                "ALTER TABLE ClubState ADD COLUMN TransferBudgetLimit INTEGER NOT NULL DEFAULT 0;");
            ProductionSqliteCommands.ExecuteNonQuery(
                connection,
                alterTransaction,
                "ALTER TABLE ClubState ADD COLUMN ReservedTransferFunds INTEGER NOT NULL DEFAULT 0;");
            ProductionSqliteCommands.ExecuteNonQuery(
                connection,
                alterTransaction,
                "ALTER TABLE ClubState ADD COLUMN SpentTransferFunds INTEGER NOT NULL DEFAULT 0;");
            ProductionSqliteCommands.ExecuteNonQuery(
                connection,
                alterTransaction,
                """
                UPDATE ClubState
                SET TransferBudgetLimit = SportiveStrength * 100000
                WHERE TransferBudgetLimit = 0;
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 28);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV28ToV29InPlace(string filePath)
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
            MigrateV28ToV29(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V28 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV28ToV29(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(
                connection,
                alterTransaction,
                "ALTER TABLE ClubState ADD COLUMN WageBudgetLimit INTEGER NOT NULL DEFAULT 0;");
            ProductionSqliteCommands.ExecuteNonQuery(
                connection,
                alterTransaction,
                "ALTER TABLE ClubState ADD COLUMN ReservedWeeklyWage INTEGER NOT NULL DEFAULT 0;");
            ProductionSqliteCommands.ExecuteNonQuery(
                connection,
                alterTransaction,
                """
                UPDATE ClubState
                SET WageBudgetLimit = SportiveStrength * 5000
                WHERE WageBudgetLimit = 0;
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 29);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV29ToV30InPlace(string filePath)
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
            MigrateV29ToV30(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V29 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV29ToV30(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS PromiseState (
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
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 30);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV30ToV31InPlace(string filePath)
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
            MigrateV30ToV31(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V30 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV30ToV31(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS MemoryState (
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
                    RuleVersion INTEGER NOT NULL
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 31);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV31ToV32InPlace(string filePath)
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
            MigrateV31ToV32(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V31 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV31ToV32(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS RelationshipState (
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
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 32);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV32ToV33InPlace(string filePath)
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
            MigrateV32ToV33(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V32 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV32ToV33(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            if (TableExists(connection, "MemoryState")
                && !ColumnExists(connection, "MemoryState", "ProcessedReinforcementKeysCsv"))
            {
                ProductionSqliteCommands.ExecuteNonQuery(
                    connection,
                    alterTransaction,
                    "ALTER TABLE MemoryState ADD COLUMN ProcessedReinforcementKeysCsv TEXT NOT NULL DEFAULT '';");
            }

            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 33);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV33ToV34InPlace(string filePath)
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
            MigrateV33ToV34(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V33 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV33ToV34(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS DecisionRequestState (
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
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 34);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV34ToV35InPlace(string filePath)
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
            MigrateV34ToV35(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V34 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV34ToV35(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS DialogueSessionState (
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
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 35);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV35ToV36InPlace(string filePath)
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
            MigrateV35ToV36(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V35 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV35ToV36(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS DisciplinaryActionState (
                    DisciplinaryActionId INTEGER PRIMARY KEY,
                    Kind INTEGER NOT NULL,
                    ManagerId INTEGER NOT NULL,
                    SubjectPlayerId INTEGER NOT NULL,
                    ClubId INTEGER NOT NULL,
                    SourceDecisionRequestId INTEGER NULL,
                    AppliedDayNumber INTEGER NOT NULL
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 36);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV36ToV37InPlace(string filePath)
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
            MigrateV36ToV37(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V36 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV36ToV37(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(
                connection,
                alterTransaction,
                "ALTER TABLE ManagerCareerState ADD COLUMN ManagerReputation INTEGER NOT NULL DEFAULT 50;");
            ProductionSqliteCommands.ExecuteNonQuery(
                connection,
                alterTransaction,
                "ALTER TABLE ManagerCareerState ADD COLUMN LastReputationReasonCode TEXT NULL;");
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 37);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV37ToV38InPlace(string filePath)
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
            MigrateV37ToV38(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V37 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV37ToV38(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS EventEffectIdempotencyState (
                    ProcessingKey TEXT PRIMARY KEY
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 38);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV38ToV39InPlace(string filePath)
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
            MigrateV38ToV39(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V38 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV38ToV39(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS ScheduledEvaluationState (
                    ScheduledEvaluationId INTEGER PRIMARY KEY,
                    EvaluationTypeCode TEXT NOT NULL,
                    DueDayNumber INTEGER NOT NULL,
                    SourceEventId TEXT NULL,
                    Status INTEGER NOT NULL
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 39);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV39ToV40InPlace(string filePath)
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
            MigrateV39ToV40(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V39 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV39ToV40(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS HubNarrativeUiState (
                    SingletonId INTEGER PRIMARY KEY CHECK (SingletonId = 1),
                    WeekStoryClosureBeat TEXT NULL,
                    WeekStoryDismissOnNextAdvance INTEGER NOT NULL DEFAULT 0,
                    CleanXiNamesCsv TEXT NULL,
                    InjuryClearedNamesCsv TEXT NULL
                );
                """);
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 40);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV40ToV41InPlace(string filePath)
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
            MigrateV40ToV41(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V40 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV40ToV41(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            ProductionSqliteCommands.ExecuteNonQuery(connection, alterTransaction, """
                CREATE TABLE IF NOT EXISTS MatchupPlanNotebookState (
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
            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 41);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV41ToV42InPlace(string filePath)
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
            MigrateV41ToV42(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V41 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV41ToV42(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            if (TableExists(connection, "ClubTacticPlanState"))
            {
                if (!ColumnExists(connection, "ClubTacticPlanState", "Pressing"))
                {
                    ProductionSqliteCommands.ExecuteNonQuery(
                        connection,
                        alterTransaction,
                        "ALTER TABLE ClubTacticPlanState ADD COLUMN Pressing INTEGER NOT NULL DEFAULT 2;");
                }

                if (!ColumnExists(connection, "ClubTacticPlanState", "DefensiveLine"))
                {
                    ProductionSqliteCommands.ExecuteNonQuery(
                        connection,
                        alterTransaction,
                        "ALTER TABLE ClubTacticPlanState ADD COLUMN DefensiveLine INTEGER NOT NULL DEFAULT 2;");
                }

                if (!ColumnExists(connection, "ClubTacticPlanState", "PassingStyle"))
                {
                    ProductionSqliteCommands.ExecuteNonQuery(
                        connection,
                        alterTransaction,
                        "ALTER TABLE ClubTacticPlanState ADD COLUMN PassingStyle INTEGER NOT NULL DEFAULT 2;");
                }
            }

            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 42);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV42ToV43InPlace(string filePath)
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
            MigrateV42ToV43(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V42 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV42ToV43(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            if (TableExists(connection, "PlayerCareerState"))
            {
                if (!ColumnExists(connection, "PlayerCareerState", "LifecycleStatus"))
                {
                    ProductionSqliteCommands.ExecuteNonQuery(
                        connection,
                        alterTransaction,
                        "ALTER TABLE PlayerCareerState ADD COLUMN LifecycleStatus INTEGER NOT NULL DEFAULT 1;");
                }

                if (!ColumnExists(connection, "PlayerCareerState", "RetiredDayNumber"))
                {
                    ProductionSqliteCommands.ExecuteNonQuery(
                        connection,
                        alterTransaction,
                        "ALTER TABLE PlayerCareerState ADD COLUMN RetiredDayNumber INTEGER NULL;");
                }

                if (!ColumnExists(connection, "PlayerCareerState", "Generation"))
                {
                    ProductionSqliteCommands.ExecuteNonQuery(
                        connection,
                        alterTransaction,
                        "ALTER TABLE PlayerCareerState ADD COLUMN Generation INTEGER NOT NULL DEFAULT 0;");
                }
            }

            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 43);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    public static void MigrateV43ToV44InPlace(string filePath)
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
            MigrateV43ToV44(workingCopyPath);
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            SqliteConnection.ClearAllPools();
            TryDelete(workingCopyPath);
            throw new SaveCorruptionException(
                "V43 production save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.",
                ex);
        }

        ReplaceWorkingCopy(workingCopyPath, filePath);
    }

    private static void MigrateV43ToV44(string workingCopyPath)
    {
        using var connection = OpenMigrationConnection(workingCopyPath);
        connection.Open();

        using (var alterTransaction = connection.BeginTransaction())
        {
            if (TableExists(connection, "PlayerCareerState")
                && !ColumnExists(connection, "PlayerCareerState", "RetirementReason"))
            {
                ProductionSqliteCommands.ExecuteNonQuery(
                    connection,
                    alterTransaction,
                    "ALTER TABLE PlayerCareerState ADD COLUMN RetirementReason INTEGER NULL;");
                ProductionSqliteCommands.ExecuteNonQuery(
                    connection,
                    alterTransaction,
                    "UPDATE PlayerCareerState SET RetirementReason = 1 WHERE LifecycleStatus = 2;");
            }

            alterTransaction.Commit();
        }

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$version", 44);
        updateCommand.ExecuteNonQuery();
        updateTransaction.Commit();
    }

    private static void ReplaceWorkingCopy(string workingCopyPath, string filePath)
    {
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        SqliteConnection.ClearAllPools();

        const int maxAttempts = 8;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                File.Move(workingCopyPath, filePath, overwrite: true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt == maxAttempts)
                {
                    throw;
                }

                SqliteConnection.ClearAllPools();
                Thread.Sleep(25 * attempt);
            }
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
