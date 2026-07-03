using FootballCareerSimulator.Domain.Spike1Placeholder;

namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 2'nin "simülasyon ortasında save/load" başarı kriterini kanıtlamak için kullanılan yer tutucu
/// dönüştürücüdür. `Capture` bir "save", `Restore` ise bir "load" adımını temsil eder; ikisi arasında
/// veri yalnızca <see cref="WorldSnapshot"/> aracılığıyla taşınır, canlı nesne referansı paylaşılmaz.
/// </summary>
public static class WorldSnapshotSerializer
{
    public static WorldSnapshot Capture(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var clubs = world.Clubs
            .Select(club => new ClubSnapshot(club.Id.Value, club.Name))
            .ToArray();

        var players = world.Players
            .Select(player => new PlayerSnapshot(player.Id.Value, player.ClubId.Value, player.Age, player.Form))
            .ToArray();

        return new WorldSnapshot(world.CurrentSeason, clubs, players);
    }

    public static World Restore(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var clubs = snapshot.Clubs
            .Select(club => new Club(ClubId.FromIndex(club.ClubId), club.Name))
            .ToArray();

        var players = snapshot.Players
            .Select(player => Player.Rehydrate(
                PlayerId.FromIndex(player.PlayerId),
                ClubId.FromIndex(player.ClubId),
                player.Age,
                player.Form))
            .ToArray();

        return new World(snapshot.CurrentSeason, clubs, players);
    }
}
