using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.DataPacks;

namespace FootballCareerSimulator.Simulation.TeamPreparation;

/// <summary>
/// MVP kadro listesi — deterministik fictional oyuncu isimleri (persist edilmez).
/// </summary>
public static class MvpSquadRosterGenerator
{
    public const int SquadSize = 25;

    private static readonly string[] FirstNames =
    [
        "Emre", "Can", "Burak", "Oğuz", "Kerem", "Arda", "Mert", "Barış", "Tolga", "Serkan",
        "Hakan", "Volkan", "Umut", "Onur", "Deniz", "Kaan", "Yiğit", "Alp", "Eren", "Batu",
    ];

    private static readonly string[] LastNames =
    [
        "Yılmaz", "Demir", "Kaya", "Çelik", "Aydın", "Koç", "Şahin", "Öztürk", "Arslan", "Doğan",
        "Kurt", "Polat", "Aslan", "Güneş", "Acar", "Tekin", "Erdoğan", "Bulut", "Taş", "Aksoy",
    ];

    public static IReadOnlyList<string> GeneratePlayerNames(ClubId clubId, int rootSeed)
        => GeneratePlayerProfiles(clubId, rootSeed)
            .Select(player => player.DisplayName)
            .ToArray();

    public static IReadOnlyList<MvpSquadPlayerProfile> GeneratePlayerProfiles(ClubId clubId, int rootSeed)
    {
        if (TurkeySuperLig202627DataPack.TryGetClub(clubId, out var realClub))
        {
            return realClub.Players;
        }

        var rng = new SimulationRandomContext(unchecked(rootSeed * 911) ^ (int)clubId.Value);
        var names = new List<string>(SquadSize);
        var used = new HashSet<string>();

        while (names.Count < SquadSize)
        {
            var first = FirstNames[rng.NextInt(0, FirstNames.Length)];
            var last = LastNames[rng.NextInt(0, LastNames.Length)];
            var candidate = $"{first} {last}";
            if (used.Add(candidate))
            {
                names.Add(candidate);
            }
        }

        return names
            .Select((name, slotIndex) => new MvpSquadPlayerProfile(
                name,
                FallbackPositionFor(slotIndex)))
            .ToArray();
    }

    private static MvpSquadPositionGroup FallbackPositionFor(int slotIndex) => slotIndex switch
    {
        0 or 11 or 12 => MvpSquadPositionGroup.Goalkeeper,
        >= 1 and <= 4 or >= 13 and <= 16 => MvpSquadPositionGroup.Defender,
        >= 5 and <= 8 or >= 17 and <= 20 => MvpSquadPositionGroup.Midfielder,
        _ => MvpSquadPositionGroup.Forward,
    };
}
