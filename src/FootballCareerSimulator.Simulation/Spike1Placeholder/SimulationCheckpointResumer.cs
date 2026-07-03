namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 2 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 3) için, ham RNG iç durumunu serileştirmeden
/// (bu, .NET `Random` için genel biçimde güvenilir değildir), bir "root seed + o ana kadar geçen sezon
/// sayısı" bilgisinden aynı deterministik çağrı dizisini yeniden oynatarak (`WorldFactory` +
/// `SeasonAdvancer` ile birebir aynı sırayla) RNG cursor'ını yeniden kuran yer tutucu bir yöntemdir.
///
/// Bu, gerçek sistemin nihai "RNG stream stratejisi" kararı DEĞİLDİR (`docs/15_DECISION_LOG.md` D-072
/// hâlâ açıktır); yalnızca bu spike'ın kanıtlamak istediği "seed tabanlı deterministik devamlılık"
/// riskini somut biçimde doğrulayan bir adaydır. Yeniden oynatma, gerçek dünya state'ini DEĞİL, yalnızca
/// RNG cursor'ını yeniden üretir; asıl dünya state'i her zaman <see cref="WorldSnapshotSerializer"/>
/// üzerinden gerçek save/load ile taşınır.
/// </summary>
public static class SimulationCheckpointResumer
{
    public static SimulationRandomContext ResumeRandomContext(int seed, int seasonsAlreadyElapsed)
    {
        if (seasonsAlreadyElapsed < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seasonsAlreadyElapsed), seasonsAlreadyElapsed, "Season count cannot be negative.");
        }

        var random = new SimulationRandomContext(seed);
        var throwawayWorld = WorldFactory.CreatePlaceholderWorld(random);

        for (var season = 0; season < seasonsAlreadyElapsed; season++)
        {
            SeasonAdvancer.AdvanceOneSeason(throwawayWorld, random);
        }

        return random;
    }
}
