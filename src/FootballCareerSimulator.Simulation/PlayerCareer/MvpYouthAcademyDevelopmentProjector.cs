using FootballCareerSimulator.Domain.PlayerCareer;

namespace FootballCareerSimulator.Simulation.PlayerCareer;

/// <summary>
/// Kabul edilmiş akademi adayının sezonlar arası gelişimini yalnızca kalıcı dünya girdilerinden
/// üretir. Aynı oyuncu, root seed ve tamamlanan sezon sayısı her zaman aynı sonucu verir.
/// </summary>
public static class MvpYouthAcademyDevelopmentProjector
{
    public const string ProjectorVersion1 = "1";
    public const int MinimumPromotionAge = 17;
    public const int MinimumPromotionAbility = 50;

    public static MvpYouthAcademyDevelopmentProjection Project(
        MvpYouthAcademyCandidate candidate,
        int completedAcademySeasons,
        int rootSeed,
        string rngVersion)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (completedAcademySeasons < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(completedAcademySeasons));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rngVersion);
        if (rngVersion != ProjectorVersion1)
        {
            throw new NotSupportedException(
                $"Youth academy development does not support RNG version '{rngVersion}'.");
        }

        var ability = candidate.CurrentAbility;
        for (var seasonIndex = 1; seasonIndex <= completedAcademySeasons; seasonIndex++)
        {
            var random = new SimulationRandomContext(CreateSeasonSeed(candidate.PlayerId, rootSeed, seasonIndex));
            var potentialGap = candidate.PotentialAbility - ability;
            if (potentialGap <= 0)
            {
                break;
            }

            // Geniş PA boşluğu olan ham yetenek daha hızlı büyür; çekiliş sezon bazında bağımsızdır.
            var baseGain = potentialGap >= 30 ? 4 : potentialGap >= 18 ? 3 : 2;
            var gain = baseGain + random.NextInt(0, 3);
            ability = Math.Min(candidate.PotentialAbility, ability + gain);
        }

        var age = candidate.Age + completedAcademySeasons;
        var isEligible = age >= MinimumPromotionAge
            && (ability >= MinimumPromotionAbility || completedAcademySeasons >= 2);
        return new MvpYouthAcademyDevelopmentProjection(
            age,
            ability,
            candidate.PotentialAbility,
            completedAcademySeasons,
            isEligible);
    }

    private static int CreateSeasonSeed(PlayerId playerId, int rootSeed, int seasonIndex) =>
        unchecked(
            rootSeed * 486_187_739
            ^ playerId.Value.GetHashCode() * 16_777_619
            ^ seasonIndex * 104_729);
}

public sealed record MvpYouthAcademyDevelopmentProjection(
    int Age,
    int CurrentAbility,
    int PotentialAbility,
    int CompletedAcademySeasons,
    bool IsPromotionEligible);
