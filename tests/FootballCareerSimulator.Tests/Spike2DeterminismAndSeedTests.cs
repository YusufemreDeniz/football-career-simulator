using FootballCareerSimulator.Simulation;
using FootballCareerSimulator.Simulation.Spike1Placeholder;

namespace FootballCareerSimulator.Tests;

/// <summary>
/// docs/18_SPIKE_EXECUTION_PLAN.md Kart 3'ün (Spike 2) başarı kriterlerini
/// (docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md Bölüm 16) doğrudan doğrular: aynı seed ile
/// tekrarlanan çalıştırmaların aynı canonical hash'i üretmesi, simülasyon ortasında save/load
/// yapıldığında kesintisiz koşuyla aynı sonucun elde edilmesi, farklı seed'lerin anlamlı bir fark
/// üretmesi ve RNG sürümünün raporlanması.
/// </summary>
public class Spike2DeterminismAndSeedTests
{
    private const int SeasonCount = 10;

    [Fact]
    public void Run_SameSeedTwentyTimes_ProducesIdenticalCanonicalHash()
    {
        var hashes = Enumerable.Range(0, 20)
            .Select(_ => HeadlessSimulationRunner.Run(seed: 42, seasonCount: SeasonCount).CanonicalStateHash)
            .ToArray();

        Assert.All(hashes, hash => Assert.Equal(hashes[0], hash));
    }

    /// <summary>
    /// docs/18_SPIKE_EXECUTION_PLAN.md Kart 8'in "aynı seed ile determinism smoke test sonucu CI ve
    /// yerel ortamda eşleşir" kriterini doğrudan doğrular: bu sabit hash, geliştirme makinesinde
    /// `tools/FootballCareerSimulator.SimulationRunner` ile üretilmiştir. Bu test CI'da (farklı işletim
    /// sistemi sürümü/donanım) başarısız olursa, bu ortamlar arası bir determinizm sapması anlamına
    /// gelir ve sessizce göz ardı edilmemelidir.
    /// </summary>
    [Fact]
    public void Run_Seed42TenSeasons_MatchesKnownCanonicalHashAcrossEnvironments()
    {
        const string knownGoodHashFromLocalDevelopmentMachine =
            "63DA08650D5BD04C95E7610F353C545CCDE2780320FA6FB163B0B2F2CBAA0370";

        var report = HeadlessSimulationRunner.Run(seed: 42, seasonCount: SeasonCount);

        Assert.Equal(knownGoodHashFromLocalDevelopmentMachine, report.CanonicalStateHash);
    }

    [Fact]
    public void Run_DifferentSeeds_ProduceDifferentCanonicalHashWithoutViolatingInvariants()
    {
        var reportA = HeadlessSimulationRunner.Run(seed: 1, seasonCount: SeasonCount);
        var reportB = HeadlessSimulationRunner.Run(seed: 2, seasonCount: SeasonCount);

        // Invariant korunumu, Run() içindeki WorldInvariantChecker cagrilarinin istisna atmamasiyla
        // zaten kanitlanmistir; burada yalnizca "anlamli fark" kriteri dogrulanir.
        Assert.NotEqual(reportA.CanonicalStateHash, reportB.CanonicalStateHash);
    }

    [Fact]
    public void Run_ReportsRandomContextVersion()
    {
        var report = HeadlessSimulationRunner.Run(seed: 42, seasonCount: SeasonCount);

        Assert.Equal(SimulationRandomContext.Version, report.RandomContextVersion);
        Assert.False(string.IsNullOrWhiteSpace(report.RandomContextVersion));
    }

    [Fact]
    public void MidSimulationSaveLoad_ProducesSameFinalHashAsUninterruptedRun()
    {
        const int seed = 123;
        const int seasonsBeforeCheckpoint = 4;
        const int seasonsAfterCheckpoint = SeasonCount - seasonsBeforeCheckpoint;

        var uninterrupted = HeadlessSimulationRunner.Run(seed, SeasonCount);

        var (world, random) = HeadlessSimulationRunner.CreateWorld(seed);
        HeadlessSimulationRunner.AdvanceSeasons(world, random, seasonsBeforeCheckpoint);

        // "Save": canlı nesneler değil, yalnızca snapshot taşınır.
        var snapshot = WorldSnapshotSerializer.Capture(world);

        // "Load": tamamen yeni nesnelerle, snapshot'tan yeniden kurulur.
        var restoredWorld = WorldSnapshotSerializer.Restore(snapshot);
        var resumedRandom = SimulationCheckpointResumer.ResumeRandomContext(seed, seasonsBeforeCheckpoint);

        HeadlessSimulationRunner.AdvanceSeasons(restoredWorld, resumedRandom, seasonsAfterCheckpoint);

        var resumedHash = CanonicalStateHasher.ComputeHash(restoredWorld);

        Assert.Equal(uninterrupted.CanonicalStateHash, resumedHash);
        Assert.Equal(uninterrupted.SeasonCount, restoredWorld.CurrentSeason);
    }

    [Fact]
    public void WorldSnapshotRoundTrip_PreservesCanonicalHash()
    {
        var (world, random) = HeadlessSimulationRunner.CreateWorld(seed: 55);
        HeadlessSimulationRunner.AdvanceSeasons(world, random, seasonCount: 3);

        var hashBeforeRoundTrip = CanonicalStateHasher.ComputeHash(world);

        var snapshot = WorldSnapshotSerializer.Capture(world);
        var restoredWorld = WorldSnapshotSerializer.Restore(snapshot);

        var hashAfterRoundTrip = CanonicalStateHasher.ComputeHash(restoredWorld);

        Assert.Equal(hashBeforeRoundTrip, hashAfterRoundTrip);
    }
}
