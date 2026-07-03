using FootballCareerSimulator.Simulation;
using FootballCareerSimulator.Simulation.Spike1Placeholder;

namespace FootballCareerSimulator.Tests;

/// <summary>
/// docs/18_SPIKE_EXECUTION_PLAN.md Kart 2'nin (Spike 1) başarı kriterlerini
/// (docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md Bölüm 16) doğrudan doğrular:
/// motor/UI olmadan çalışma, 10 sezonluk tamamlanma, ardışık çalıştırmalarda invariant korunumu,
/// CI bütçesi içinde kalma ve bellek büyümesinin sınırlı kalması.
/// </summary>
public class Spike1HeadlessTenSeasonSimulationTests
{
    private const int ExpectedClubCount = WorldFactory.ClubCount;
    private const int ExpectedPlayerCount = WorldFactory.TotalPlayerCount;
    private const int SeasonCount = 10;

    [Fact]
    public void Run_TenSeasons_CompletesAndPreservesWorldScaleInvariants()
    {
        var report = HeadlessSimulationRunner.Run(seed: 42, seasonCount: SeasonCount);

        Assert.Equal(SeasonCount, report.SeasonCount);
        Assert.Equal(ExpectedClubCount, report.ClubCount);
        Assert.Equal(ExpectedPlayerCount, report.PlayerCount);
    }

    [Fact]
    public void Run_TenConsecutiveTimes_NeverThrowsOrViolatesInvariants()
    {
        for (var run = 0; run < 10; run++)
        {
            var exception = Record.Exception(() => HeadlessSimulationRunner.Run(seed: 1000 + run, seasonCount: SeasonCount));

            Assert.Null(exception);
        }
    }

    [Fact]
    public void Run_TenSeasons_CompletesWithinGenerousPerformanceBudget()
    {
        var report = HeadlessSimulationRunner.Run(seed: 7, seasonCount: SeasonCount);

        // docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md Bolum 16, Spike 1: CI ust butcesi 5 dakikadir.
        // Bu is yuku icin cok daha dusuk, gecici ve comert bir esik kullanilir; amac buyuk
        // regresyonlari yakalamaktir, exact performans hedefi degildir (bkz. D-329).
        Assert.True(report.ElapsedMilliseconds < 10_000, $"Beklenenden yavaş çalıştı: {report.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void Run_RepeatedFullSimulations_DoNotLeakMemoryUnboundedly()
    {
        const long generousGrowthCapBytes = 100L * 1024 * 1024;

        var before = GC.GetTotalMemory(forceFullCollection: true);

        for (var run = 0; run < 10; run++)
        {
            HeadlessSimulationRunner.Run(seed: 2000 + run, seasonCount: SeasonCount);
        }

        var after = GC.GetTotalMemory(forceFullCollection: true);

        Assert.True(after - before < generousGrowthCapBytes, $"Bellek beklenenden fazla büyüdü: {(after - before) / 1024.0 / 1024.0:F2} MB");
    }

    [Fact]
    public void CreatePlaceholderWorld_WithSameSeed_ProducesSameInitialAges()
    {
        var worldA = WorldFactory.CreatePlaceholderWorld(new SimulationRandomContext(99));
        var worldB = WorldFactory.CreatePlaceholderWorld(new SimulationRandomContext(99));

        var agesA = worldA.Players.Select(p => p.Age).ToArray();
        var agesB = worldB.Players.Select(p => p.Age).ToArray();

        Assert.Equal(agesA, agesB);
    }
}
