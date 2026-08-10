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
    {
        if (TurkeySuperLig202627DataPack.TryGetClub(clubId, out var realClub))
        {
            return realClub.PlayerNames;
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

        return names;
    }
}
