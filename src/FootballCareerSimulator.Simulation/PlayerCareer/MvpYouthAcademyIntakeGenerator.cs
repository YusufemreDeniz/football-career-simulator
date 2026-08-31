using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Simulation.PlayerCareer;

/// <summary>
/// Kulüp + sezon + root seed üzerinden tekrar üretilebilen, persist gerektirmeyen
/// sezonluk akademi aday havuzu.
/// </summary>
public static class MvpYouthAcademyIntakeGenerator
{
    public const string GeneratorVersion1 = "1";
    public const int MinCandidateCount = 3;
    public const int MaxCandidateCount = 5;
    public const int MinCandidateAge = 15;
    public const int MaxCandidateAge = 17;

    // PlayerId'nin normal kadro/halef bantlarından ayrılmış, SQLite INTEGER içinde kalan bant.
    private const long CandidatePlayerIdBase = 6_000_000_000_000_000_000L;
    private const long ClubIdentityStride = 1_000_000_000L;
    private const long MaxSupportedSeasonId = 99_999_999L;

    private static readonly string[] FirstNames =
    [
        "Aras", "Bora", "Cem", "Doruk", "Efe", "Emir", "Kuzey", "Mete", "Rüzgar", "Toprak",
        "Yağız", "Yiğit", "Alp", "Baran", "Deniz", "Kaan", "Mert", "Ozan", "Umut", "Kerem",
    ];

    private static readonly string[] LastNames =
    [
        "Acar", "Aksoy", "Arslan", "Aydın", "Bulut", "Çelik", "Demir", "Doğan", "Erdem", "Güneş",
        "Kaya", "Koç", "Kurt", "Öztürk", "Polat", "Şahin", "Taş", "Tekin", "Yalçın", "Yılmaz",
    ];

    private static readonly MvpSquadPositionRole[] PositionRoles =
    [
        MvpSquadPositionRole.Goalkeeper,
        MvpSquadPositionRole.CentreBack,
        MvpSquadPositionRole.RightBack,
        MvpSquadPositionRole.LeftBack,
        MvpSquadPositionRole.DefensiveMidfielder,
        MvpSquadPositionRole.CentralMidfielder,
        MvpSquadPositionRole.AttackingMidfielder,
        MvpSquadPositionRole.RightWinger,
        MvpSquadPositionRole.LeftWinger,
        MvpSquadPositionRole.Striker,
    ];

    public static IReadOnlyList<MvpYouthAcademyCandidate> Generate(
        ClubId clubId,
        SeasonId seasonId,
        int rootSeed,
        int sportiveStrength,
        string rngVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rngVersion);
        return rngVersion switch
        {
            GeneratorVersion1 => GenerateVersion1(clubId, seasonId, rootSeed, sportiveStrength),
            _ => throw new NotSupportedException(
                $"Youth academy generator does not support RNG version '{rngVersion}'."),
        };
    }

    // Timeline'da persist edilen RNG v1 kayitlari icin uyumluluk sozlesmesi. Bu govde
    // degistirilmek yerine yeni bir surum dali eklenmelidir.
    private static IReadOnlyList<MvpYouthAcademyCandidate> GenerateVersion1(
        ClubId clubId,
        SeasonId seasonId,
        int rootSeed,
        int sportiveStrength)
    {
        if (sportiveStrength is < Club.MinSportiveStrength or > Club.MaxSportiveStrength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sportiveStrength),
                sportiveStrength,
                $"Sportive strength must be between {Club.MinSportiveStrength} and {Club.MaxSportiveStrength}.");
        }

        if (seasonId.Value > MaxSupportedSeasonId)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seasonId),
                seasonId.Value,
                $"Academy identity supports season ids up to {MaxSupportedSeasonId}.");
        }

        var random = new SimulationRandomContext(CreateSeed(clubId, seasonId, rootSeed));
        var count = random.NextInt(MinCandidateCount, MaxCandidateCount + 1);
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var candidates = new List<MvpYouthAcademyCandidate>(count);

        for (var index = 0; index < count; index++)
        {
            var displayName = DrawUniqueName(random, usedNames);
            var age = random.NextInt(MinCandidateAge, MaxCandidateAge + 1);
            var currentFloor = Domain.PlayerCareer.PlayerCareer.MinAbility
                + Math.Max(0, sportiveStrength - 40) / 20;
            var currentCeiling = Math.Min(62, 50 + (sportiveStrength / 12));
            var currentAbility = random.NextInt(currentFloor, currentCeiling + 1);
            var potentialFloor = Math.Max(currentAbility + 8, 62 + (sportiveStrength / 9));
            var potentialCeiling = Math.Min(
                Domain.PlayerCareer.PlayerCareer.MaxAbility,
                Math.Max(potentialFloor, 78 + (sportiveStrength / 6)));
            var potentialAbility = random.NextInt(potentialFloor, potentialCeiling + 1);
            var position = PositionRoles[random.NextInt(0, PositionRoles.Length)];

            candidates.Add(new MvpYouthAcademyCandidate(
                CreateCandidatePlayerId(clubId, seasonId, index),
                index,
                displayName,
                position,
                age,
                currentAbility,
                potentialAbility,
                DevelopmentProfile(currentAbility, potentialAbility)));
        }

        return candidates;
    }

    private static int CreateSeed(ClubId clubId, SeasonId seasonId, int rootSeed) =>
        unchecked(
            (rootSeed * 486_187_739)
            ^ ((int)clubId.Value * 16_777_619)
            ^ ((int)seasonId.Value * 104_729));

    private static PlayerId CreateCandidatePlayerId(ClubId clubId, SeasonId seasonId, int index)
    {
        var value = checked(
            CandidatePlayerIdBase
            + (clubId.Value * ClubIdentityStride)
            + (seasonId.Value * 10L)
            + index
            + 1L);
        return new PlayerId(value);
    }

    private static string DrawUniqueName(
        SimulationRandomContext random,
        ISet<string> usedNames)
    {
        while (true)
        {
            var name = $"{FirstNames[random.NextInt(0, FirstNames.Length)]} {LastNames[random.NextInt(0, LastNames.Length)]}";
            if (usedNames.Add(name))
            {
                return name;
            }
        }
    }

    private static string DevelopmentProfile(int currentAbility, int potentialAbility) =>
        (potentialAbility - currentAbility) switch
        {
            >= 35 => "Yüksek tavanlı ham yetenek",
            >= 27 => "Uzun vadeli gelişim projesi",
            >= 20 => "Dengeli akademi adayı",
            _ => "A takıma yakın profil",
        };
}

public sealed record MvpYouthAcademyCandidate(
    PlayerId PlayerId,
    int CandidateIndex,
    string DisplayName,
    MvpSquadPositionRole PositionRole,
    int Age,
    int CurrentAbility,
    int PotentialAbility,
    string DevelopmentProfile);
