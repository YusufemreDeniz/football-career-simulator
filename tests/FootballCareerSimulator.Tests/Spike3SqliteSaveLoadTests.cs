using FootballCareerSimulator.Infrastructure;
using FootballCareerSimulator.Simulation;
using FootballCareerSimulator.Simulation.Spike1Placeholder;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests;

/// <summary>
/// docs/18_SPIKE_EXECUTION_PLAN.md Kart 4'ün (Spike 3) başarı kriterlerini
/// (docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md Bölüm 16) doğrudan doğrular: save/load round-trip
/// canonical eşdeğerliği, eski sürümün migrate edilmesi, migration öncesi backup, migration hatasında
/// orijinalin değişmemesi, bozuk save'in reddedilmesi ve yarım/geçici dosyanın geçerli save sayılmaması.
/// </summary>
public sealed class Spike3SqliteSaveLoadTests : IDisposable
{
    private readonly string _tempDirectory;

    public Spike3SqliteSaveLoadTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "fcs-spike3-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private string GetSavePath(string name) => Path.Combine(_tempDirectory, name);

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesCanonicalState()
    {
        const int seed = 42;
        var path = GetSavePath("roundtrip.db");

        var (world, random) = HeadlessSimulationRunner.CreateWorld(seed);
        HeadlessSimulationRunner.AdvanceSeasons(world, random, seasonCount: 5);
        var expectedHash = CanonicalStateHasher.ComputeHash(world);

        SqliteSaveWriter.Save(path, world, seed, SimulationRandomContext.Version);
        var result = SqliteSaveReader.Load(path);

        Assert.Equal(expectedHash, CanonicalStateHasher.ComputeHash(result.World));
        Assert.Equal(seed, result.RootSeed);
        Assert.Equal(SimulationRandomContext.Version, result.RandomContextVersion);
        Assert.False(result.WasMigrated);
    }

    [Fact]
    public void SaveThenLoad_MultipleSequentialSeeds_AlwaysRoundTripsCorrectly()
    {
        var path = GetSavePath("sequential.db");

        foreach (var seed in new[] { 1, 2, 3, 4, 5 })
        {
            var (world, random) = HeadlessSimulationRunner.CreateWorld(seed);
            HeadlessSimulationRunner.AdvanceSeasons(world, random, seasonCount: 3);
            var expectedHash = CanonicalStateHasher.ComputeHash(world);

            SqliteSaveWriter.Save(path, world, seed, SimulationRandomContext.Version);
            var result = SqliteSaveReader.Load(path);

            Assert.Equal(expectedHash, CanonicalStateHasher.ComputeHash(result.World));
            Assert.Equal(seed, result.RootSeed);
        }
    }

    [Fact]
    public void Load_LegacyV1Save_MigratesToCurrentVersionAndPreservesData()
    {
        var path = GetSavePath("legacy-v1.db");

        var (world, random) = HeadlessSimulationRunner.CreateWorld(seed: 7);
        HeadlessSimulationRunner.AdvanceSeasons(world, random, seasonCount: 2);
        var snapshot = WorldSnapshotSerializer.Capture(world);

        LegacySaveFixture.CreateV1File(path, rootSeed: 7, randomContextVersion: "1", snapshot.CurrentSeason, snapshot.Clubs, snapshot.Players);

        var result = SqliteSaveReader.Load(path);

        Assert.True(result.WasMigrated);
        Assert.Equal(2, result.SchemaVersionLoaded);
        Assert.Equal(snapshot.CurrentSeason, result.World.CurrentSeason);
        Assert.Equal(snapshot.Clubs.Count, result.World.Clubs.Count);
        Assert.Equal(snapshot.Players.Count, result.World.Players.Count);
        Assert.All(result.World.Players, player => Assert.Equal(0, player.Form));
        Assert.True(File.Exists(path + ".bak"), "Migration öncesi backup dosyası oluşturulmalıdır.");

        var secondLoad = SqliteSaveReader.Load(path);
        Assert.False(secondLoad.WasMigrated, "Zaten migrate edilmiş bir save ikinci kez migrate edilmemelidir.");
    }

    [Fact]
    public void Migration_PoisonedLegacyFile_FailsWithoutModifyingOriginal()
    {
        var path = GetSavePath("poisoned-v1.db");

        var (world, random) = HeadlessSimulationRunner.CreateWorld(seed: 3);
        HeadlessSimulationRunner.AdvanceSeasons(world, random, seasonCount: 1);
        var snapshot = WorldSnapshotSerializer.Capture(world);

        LegacySaveFixture.CreateV1File(
            path, rootSeed: 3, randomContextVersion: "1", snapshot.CurrentSeason, snapshot.Clubs, snapshot.Players,
            poisonWithConflictingFormColumn: true);

        var originalBytes = File.ReadAllBytes(path);

        Assert.Throws<SaveCorruptionException>(() => SqliteSaveReader.Load(path));

        var bytesAfterFailedMigration = File.ReadAllBytes(path);
        Assert.Equal(originalBytes, bytesAfterFailedMigration);
        Assert.False(File.Exists(path + ".migrating.tmp"), "Başarısız migration çalışma kopyasını temizlemelidir.");
    }

    [Fact]
    public void Load_CorruptedFileBytes_ThrowsSaveCorruptionException()
    {
        var path = GetSavePath("corrupted-bytes.db");

        var (world, _) = HeadlessSimulationRunner.CreateWorld(seed: 11);
        SqliteSaveWriter.Save(path, world, rootSeed: 11, SimulationRandomContext.Version);

        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write))
        {
            var garbage = new byte[32];
            new Random(999).NextBytes(garbage);
            stream.Write(garbage, 0, garbage.Length);
        }

        Assert.Throws<SaveCorruptionException>(() => SqliteSaveReader.Load(path));
    }

    [Fact]
    public void Load_TamperedDataWithStaleHash_ThrowsSaveCorruptionException()
    {
        var path = GetSavePath("tampered-data.db");

        var (world, _) = HeadlessSimulationRunner.CreateWorld(seed: 21);
        SqliteSaveWriter.Save(path, world, rootSeed: 21, SimulationRandomContext.Version);

        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Players SET Age = Age + 100 WHERE PlayerId = 0;";
            command.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();

        Assert.Throws<SaveCorruptionException>(() => SqliteSaveReader.Load(path));
    }

    [Fact]
    public void Save_StaleTempFileFromPreviousAttempt_DoesNotPreventNewValidSave()
    {
        var path = GetSavePath("stale-temp.db");

        File.WriteAllBytes(path + ".tmp", [1, 2, 3, 4, 5]);

        var (world, _) = HeadlessSimulationRunner.CreateWorld(seed: 55);
        var expectedHash = CanonicalStateHasher.ComputeHash(world);

        SqliteSaveWriter.Save(path, world, rootSeed: 55, SimulationRandomContext.Version);
        var result = SqliteSaveReader.Load(path);

        Assert.Equal(expectedHash, CanonicalStateHasher.ComputeHash(result.World));
        Assert.False(File.Exists(path + ".tmp"), "Başarılı save sonrası geçici dosya kalmamalıdır.");
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        var path = GetSavePath("does-not-exist.db");

        Assert.Throws<FileNotFoundException>(() => SqliteSaveReader.Load(path));
    }
}
