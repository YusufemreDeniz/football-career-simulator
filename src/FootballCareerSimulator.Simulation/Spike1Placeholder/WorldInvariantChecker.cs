using FootballCareerSimulator.Domain.Spike1Placeholder;

namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 1'in "ardışık çalıştırmalarda invariant ihlali oluşmaz" başarı kriterini (bkz.
/// `docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 16) somut biçimde doğrulayan yer tutucu
/// kontrollerdir. Gerçek domain invariant kataloğu `docs/03_DOMAIN_MODEL.md` kapsamında ayrıca
/// tanımlanacaktır.
/// </summary>
public static class WorldInvariantChecker
{
    public static void Validate(World world, int expectedClubCount, int expectedPlayerCount)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Clubs.Count != expectedClubCount)
        {
            throw new WorldInvariantViolationException(
                $"Beklenen kulüp sayısı {expectedClubCount}, gerçek sayı {world.Clubs.Count}.");
        }

        if (world.Players.Count != expectedPlayerCount)
        {
            throw new WorldInvariantViolationException(
                $"Beklenen futbolcu sayısı {expectedPlayerCount}, gerçek sayı {world.Players.Count}.");
        }

        if (world.CurrentSeason < 0)
        {
            throw new WorldInvariantViolationException(
                $"Sezon sayacı negatif olamaz: {world.CurrentSeason}.");
        }

        var clubIds = new HashSet<ClubId>(world.Clubs.Select(club => club.Id));

        foreach (var player in world.Players)
        {
            if (!clubIds.Contains(player.ClubId))
            {
                throw new WorldInvariantViolationException(
                    $"{player.Id} bilinmeyen bir kulübe ({player.ClubId}) referans veriyor.");
            }

            if (player.Age < 0)
            {
                throw new WorldInvariantViolationException(
                    $"{player.Id} negatif yaşta: {player.Age}.");
            }
        }
    }
}
