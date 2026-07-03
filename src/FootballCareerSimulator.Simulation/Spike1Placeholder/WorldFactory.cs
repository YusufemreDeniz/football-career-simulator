using FootballCareerSimulator.Domain.Spike1Placeholder;

namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 1 için `docs/02_MVP_SCOPE.md`'deki hedef dünya ölçeğini (20 kulüp, ~500 futbolcu) temsil eden
/// yer tutucu bir dünya üretir. Üretilen içerik gerçek content/authoring pipeline'ının yerine geçmez
/// (bkz. `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 11).
/// </summary>
public static class WorldFactory
{
    public const int ClubCount = 20;
    public const int PlayersPerClub = 25;
    public const int TotalPlayerCount = ClubCount * PlayersPerClub;

    public static World CreatePlaceholderWorld(SimulationRandomContext random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var clubs = new List<Club>(ClubCount);
        var players = new List<Player>(TotalPlayerCount);

        for (var clubIndex = 0; clubIndex < ClubCount; clubIndex++)
        {
            var clubId = ClubId.FromIndex(clubIndex);
            clubs.Add(new Club(clubId, $"Placeholder Club {clubIndex + 1:D2}"));

            for (var slot = 0; slot < PlayersPerClub; slot++)
            {
                var playerIndex = (clubIndex * PlayersPerClub) + slot;
                var playerId = PlayerId.FromIndex(playerIndex);
                var initialAge = random.NextInt(17, 36);
                players.Add(new Player(playerId, clubId, initialAge));
            }
        }

        return new World(currentSeason: 0, clubs, players);
    }
}
