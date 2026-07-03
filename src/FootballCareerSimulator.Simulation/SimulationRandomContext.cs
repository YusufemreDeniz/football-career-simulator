namespace FootballCareerSimulator.Simulation;

/// <summary>
/// `docs/15_DECISION_LOG.md` D-058 ile uyumlu, açık ve sürümlenmiş seeded rastlantısallık kaynağıdır.
/// Domain ve Simulation kuralları global veya gizli RNG yerine yalnızca bu tür açık bir context
/// üzerinden rastlantısallık kullanmalıdır.
/// </summary>
public sealed class SimulationRandomContext
{
    /// <summary>
    /// `docs/15_DECISION_LOG.md` D-069 ile uyumlu, RNG davranış sürümüdür. Bu sürüm değiştiğinde
    /// aynı seed ile önceki üretilen sonuçların artık birebir eşleşmeyebileceği açıkça kabul edilir.
    /// </summary>
    public const string Version = "1";

    private readonly Random _random;

    public int Seed { get; }

    /// <summary>Bu context üzerinden yapılmış toplam çekiliş sayısı; raporlama ve teşhis amaçlıdır.</summary>
    public int DrawCount { get; private set; }

    public SimulationRandomContext(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        DrawCount++;
        return _random.Next(minInclusive, maxExclusive);
    }
}
