namespace FootballCareerSimulator.Domain.TeamPreparation;

/// <summary>
/// Takımın soyunma odası uyumunu, saha içi kimyasını ve maç motoru performans çarpanını temsil eder.
/// </summary>
public sealed record TeamChemistryProfile(
    int ChemistryScore,
    string ChemistryBand,
    double PerformanceMultiplier,
    string NarrativeSummary);

/// <summary>
/// İlk 11'in pozisyon uyumu, yaş dağılımı, kulüp tesisleri ve momentumuna göre takım kimyasını hesaplar.
/// </summary>
public static class TeamChemistryCalculator
{
    public const int MinChemistry = 30;
    public const int MaxChemistry = 100;
    public const int BaselineChemistry = 65;

    public static TeamChemistryProfile Evaluate(
        int starterCount,
        int naturalPositionMatches,
        int averageSquadAge,
        int winStreakLength,
        int facilityLevel = 1)
    {
        var positionRatio = starterCount > 0 ? (double)naturalPositionMatches / starterCount : 1.0;
        var positionScore = (int)Math.Round(positionRatio * 40.0);

        var facilityScore = Math.Clamp(facilityLevel * 3, 3, 15);

        var ageBalance = 20 - Math.Min(15, Math.Abs(averageSquadAge - 26) * 3);
        var ageScore = Math.Clamp(ageBalance, 5, 20);

        var streakBonus = Math.Clamp(winStreakLength * 5, -15, 25);

        var total = Math.Clamp(
            BaselineChemistry + (positionScore - 20) + (facilityScore - 8) + (ageScore - 12) + streakBonus,
            MinChemistry,
            MaxChemistry);

        var band = total switch
        {
            >= 85 => "Mükemmel Uyum",
            >= 70 => "Yüksek Kimya",
            >= 50 => "Gelişmekte Olan Uyum",
            _ => "Düşük Uyum / Dağınık Kadro",
        };

        var multiplier = 0.90 + (((double)(total - MinChemistry) / (MaxChemistry - MinChemistry)) * 0.20);

        var narrative = total >= 85
            ? "Oyuncular sahada gözü kapalı anlaşıyor; pas akışı ve yardımlaşma zirvede."
            : total >= 70
                ? "Takım içi iletişim güçlü, taktiksel disiplin korunuyor."
                : total >= 50
                    ? "Takım birbirine alışma evresinde; maç tecrübesiyle uyum artacak."
                    : "Mevki kaymaları ve dalgalı moral nedeniyle sahada iletişim kopuklukları yaşanıyor.";

        return new TeamChemistryProfile(total, band, Math.Round(multiplier, 2), narrative);
    }
}
