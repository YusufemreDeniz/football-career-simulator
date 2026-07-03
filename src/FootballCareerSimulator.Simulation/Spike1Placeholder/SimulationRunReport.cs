namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16, Spike 1'in "seed ve performans raporu
/// üretilir" başarı kriterini karşılayan rapor.
/// </summary>
public sealed record SimulationRunReport(
    int Seed,
    int SeasonCount,
    int ClubCount,
    int PlayerCount,
    long ElapsedMilliseconds,
    long MemoryBeforeBytes,
    long MemoryAfterBytes);
