namespace FootballCareerSimulator.Simulation;

/// <summary>
/// `docs/15_DECISION_LOG.md` D-058 ile uyumlu, açık ve sürümlenmiş seeded rastlantısallık kaynağıdır.
/// Domain ve Simulation kuralları global veya gizli RNG yerine yalnızca bu tür açık bir context
/// üzerinden rastlantısallık kullanmalıdır.
/// </summary>
public sealed class SimulationRandomContext
{
    private readonly Random _random;

    public int Seed { get; }

    public SimulationRandomContext(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
