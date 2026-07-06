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
