using FootballCareerSimulator.Domain.Spike1Placeholder;

namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 1 için dünyayı tek bir sezon ilerleten yer tutucu koordinasyon mantığıdır. Gerçek sezon
/// geçişi (fikstür tamamlanması, standings, kariyer geçişleri vb.) `docs/12_WORLD_SIMULATION.md`
/// Bölüm 24 kapsamında ayrıca implemente edilecektir; burada yalnızca ölçek ve tekrarlanabilirlik
/// doğrulanır.
/// </summary>
public static class SeasonAdvancer
{
    public static void AdvanceOneSeason(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        foreach (var player in world.Players)
        {
            player.AgeOneYear();
        }

        world.AdvanceSeasonCounter();
    }
}
