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

    public static IReadOnlyList<string> GeneratePlayerNames(
        ClubId clubId,
        int rootSeed,
        string? clubDisplayName = null)
        => GeneratePlayerProfiles(clubId, rootSeed, clubDisplayName)
            .Select(player => player.DisplayName)
            .ToArray();

    public static IReadOnlyList<MvpSquadPlayerProfile> GeneratePlayerProfiles(
        ClubId clubId,
        int rootSeed,
        string? clubDisplayName = null)
    {
        if (TurkeySuperLig202627DataPack.TryGetClub(clubId, out var realClub)
            && (clubDisplayName is null
                || string.Equals(clubDisplayName, realClub.OfficialName, StringComparison.Ordinal)))
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

    private static MvpSquadPositionRole FallbackPositionFor(int slotIndex) => slotIndex switch
    {
        0 or 11 or 12 => MvpSquadPositionRole.Goalkeeper,
        1 or 13 => MvpSquadPositionRole.RightBack,
        2 or 3 or 14 or 15 => MvpSquadPositionRole.CentreBack,
        4 or 16 => MvpSquadPositionRole.LeftBack,
        5 or 17 => MvpSquadPositionRole.RightMidfielder,
        6 or 18 => MvpSquadPositionRole.DefensiveMidfielder,
        7 or 19 => MvpSquadPositionRole.CentralMidfielder,
        8 or 20 => MvpSquadPositionRole.LeftMidfielder,
        9 or 21 or 23 => MvpSquadPositionRole.Striker,
        22 => MvpSquadPositionRole.RightWinger,
        _ => MvpSquadPositionRole.LeftWinger,
    };
}
