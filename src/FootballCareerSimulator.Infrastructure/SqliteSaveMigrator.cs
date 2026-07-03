using FootballCareerSimulator.Domain.Spike1Placeholder;
using FootballCareerSimulator.Simulation.Spike1Placeholder;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Infrastructure;

/// <summary>
/// Spike 3 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 4) için yer tutucu, tek adımlı (V1 → V2)
/// migration uygulayıcısıdır.
///
/// Bağlayıcı ilke: orijinal dosya (<paramref name="filePath"/>) migration süresince yalnızca OKUNUR;
/// bütün ALTER/UPDATE işlemleri ayrı bir çalışma kopyası üzerinde yapılır ve gerçek dosya yalnızca
/// migration tamamen başarılı olduktan sonra atomik bir `File.Move` ile değiştirilir. Bu, "migration
/// hatasında orijinal save değişmez" kriterini yapı gereği (tasarımla) garanti eder; rollback'e veya
/// "en iyi çaba" davranışına dayanmaz.
/// </summary>
internal static class SqliteSaveMigrator
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
                $"V{fromVersion} save'i güncel şemaya taşırken hata oluştu; orijinal dosya değiştirilmedi.", ex);
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
            SqliteSaveWriter.ExecuteNonQuery(connection, alterTransaction, "ALTER TABLE Players ADD COLUMN Form INTEGER NOT NULL DEFAULT 0;");
            SqliteSaveWriter.ExecuteNonQuery(connection, alterTransaction, "ALTER TABLE SaveManifest ADD COLUMN CanonicalStateHash TEXT NULL;");
            alterTransaction.Commit();
        }

        var clubs = SqliteRowReader.ReadClubs(connection);
        var players = SqliteRowReader.ReadPlayers(connection);
        var manifest = SqliteRowReader.ReadManifest(connection);

        var snapshot = new WorldSnapshot(manifest.CurrentSeason, clubs, players);
        var world = WorldSnapshotSerializer.Restore(snapshot);
        var canonicalHash = CanonicalStateHasher.ComputeHash(world);

        using var updateTransaction = connection.BeginTransaction();
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = updateTransaction;
        updateCommand.CommandText = "UPDATE SaveManifest SET CanonicalStateHash = $hash, SchemaVersion = $version;";
        updateCommand.Parameters.AddWithValue("$hash", canonicalHash);
        updateCommand.Parameters.AddWithValue("$version", SqliteSaveSchema.CurrentVersion);
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
            // En iyi çaba temizliği; başarısız olursa bir sonraki migration denemesi zaten üzerine yazar.
        }
    }
}
