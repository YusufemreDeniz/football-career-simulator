using FootballCareerSimulator.Domain.Spike1Placeholder;

namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 1/2 için dünyayı tek bir sezon ilerleten yer tutucu koordinasyon mantığıdır. Gerçek sezon
/// geçişi (fikstür tamamlanması, standings, kariyer geçişleri vb.) `docs/12_WORLD_SIMULATION.md`
/// Bölüm 24 kapsamında ayrıca implemente edilecektir; burada yalnızca ölçek, tekrarlanabilirlik ve
/// RNG akışının doğru sürdürülmesi doğrulanır. Verilen <see cref="SimulationRandomContext"/>
/// haricinde başka bir rastlantısallık kaynağı kullanılmaz (D-058 ile uyumlu).
/// </summary>
public static class SeasonAdvancer
{
    public static void AdvanceOneSeason(World world, SimulationRandomContext random)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(random);

        foreach (var player in world.Players)
        {
            player.AgeOneYear();
            var formDelta = random.NextInt(-2, 3);
            player.AdjustForm(formDelta);
        }

        world.AdvanceSeasonCounter();
    }
}
