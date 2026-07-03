using FootballCareerSimulator.Domain.Spike1Placeholder;
using FootballCareerSimulator.Simulation.Spike1Placeholder;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Infrastructure;

/// <summary>
/// Spike 3 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 4) için yer tutucu SQLite save okuyucusudur.
/// Gerekirse şeffaf biçimde migration tetikler ve okunan veriyi saklanan bütünlük hash'iyle
/// karşılaştırarak bozulma tespiti yapar.
/// </summary>
public static class SqliteSaveReader
{
    public static SqliteLoadResult Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Save dosyası bulunamadı: {filePath}", filePath);
        }

        var schemaVersion = ReadSchemaVersionSafely(filePath);

        if (schemaVersion > SqliteSaveSchema.CurrentVersion || schemaVersion < SqliteSaveSchema.MinSupportedVersion)
        {
            throw new UnsupportedSaveSchemaVersionException(schemaVersion);
        }

        var wasMigrated = false;

        if (schemaVersion < SqliteSaveSchema.CurrentVersion)
        {
            SqliteSaveMigrator.MigrateInPlace(filePath, schemaVersion);
            wasMigrated = true;
        }

        return ReadCurrentVersion(filePath, wasMigrated);
    }

    private static int ReadSchemaVersionSafely(string filePath)
    {
        try
        {
            int schemaVersion;

            using (var connection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly"))
            {
                connection.Open();
                schemaVersion = SqliteRowReader.ReadSchemaVersion(connection);
            }

            // Microsoft.Data.Sqlite varsayılan olarak native bağlantıları havuzlar; `using` C# nesnesini
            // dispose eder ama OS dosya kolunu havuzda açık tutabilir. Aynı yola art arda yazılan bir
            // sonraki `Save` çağrısının `File.Move`'unun Windows'ta "Access to the path is denied"
            // hatasıyla başarısız olmaması için havuz burada da temizlenir (bkz. SqliteSaveWriter.Save).
            SqliteConnection.ClearAllPools();

            return schemaVersion;
        }
        catch (Exception ex) when (ex is not SaveIntegrityException)
        {
            throw new SaveCorruptionException($"Save dosyası okunamadı veya geçerli bir SQLite dosyası değil: {filePath}", ex);
        }
    }

    private static SqliteLoadResult ReadCurrentVersion(string filePath, bool wasMigrated)
    {
        try
        {
            (int RootSeed, string RandomContextVersion, int CurrentSeason, string CanonicalStateHash) manifest;
            IReadOnlyList<ClubSnapshot> clubs;
            IReadOnlyList<PlayerSnapshot> players;

            using (var connection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly"))
            {
                connection.Open();
                manifest = SqliteRowReader.ReadManifest(connection);
                clubs = SqliteRowReader.ReadClubs(connection);
                players = SqliteRowReader.ReadPlayers(connection);
            }

            // Bkz. ReadSchemaVersionSafely'deki not: aynı yolun tekrar yazılabilmesi için pool temizlenir.
            SqliteConnection.ClearAllPools();

            var snapshot = new WorldSnapshot(manifest.CurrentSeason, clubs, players);
            var world = WorldSnapshotSerializer.Restore(snapshot);

            var recomputedHash = CanonicalStateHasher.ComputeHash(world);

            if (!string.Equals(recomputedHash, manifest.CanonicalStateHash, StringComparison.Ordinal))
            {
                throw new SaveCorruptionException(
                    $"Bütünlük hash'i eşleşmiyor (beklenen: {manifest.CanonicalStateHash}, hesaplanan: {recomputedHash}); save bozulmuş olabilir.");
            }

            return new SqliteLoadResult(
                world,
                manifest.RootSeed,
                manifest.RandomContextVersion,
                SqliteSaveSchema.CurrentVersion,
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
}
