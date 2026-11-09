namespace FootballCareerSimulator.Domain.PlayerCareer;

/// <summary>
/// SM25 reverse-engineering araştırmasından modellenen Sinüs Dalgası (SinWave) büyüme eğrisi.
/// Oyuncunun potansiyeline olan uzaklığına (headroom) bağlı olarak büyüme verimi
/// Sin(HeadroomRatio * 90°) dalgası ve yaş olgunluk çarpanı ile organik olarak hesaplanır.
/// Tavanına uzak genç yetenekler hızla gelişirken, tavanına yaklaşan oyuncuların gelişimi platoya ulaşır.
/// </summary>
public static class SinWaveGrowthCalculator
{
    public const int MinDevelopmentAge = 15;
    public const int DefaultPeakAge = 24;

    /// <summary>
    /// Ham antrenman/maç puanına sinüs dalgası çarpanı uygulayarak efektif gelişim puanını hesaplar.
    /// </summary>
    /// <param name="rawPoints">Antrenman veya maçtan gelen baz gelişim puanı.</param>
    /// <param name="currentAbility">Mevcut yetenek (CA: 40–99).</param>
    /// <param name="potentialAbility">Potansiyel yetenek (PA: 40–99).</param>
    /// <param name="age">Oyuncunun yaşı.</param>
    /// <param name="peakAge">Oyuncunun mevki bazlı zirveye ulaşma yaşı.</param>
    /// <returns>Organik sinüs eğrisi uygulanmış net gelişim puanı.</returns>
    public static int CalculateEffectivePoints(
        int rawPoints,
        int currentAbility,
        int potentialAbility,
        int age,
        int peakAge = DefaultPeakAge)
    {
        if (rawPoints <= 0 || currentAbility >= potentialAbility)
        {
            return 0;
        }

        // 1. Kalan potansiyel tavan oranı (Headroom: 0.0 - 1.0)
        // Tavandan ne kadar uzaktaysa sin(HeadroomRatio * 90°) o kadar 1.0'a yaklaşır.
        var maxHeadroom = Math.Max(1, potentialAbility - PlayerCareer.MinAbility);
        var currentHeadroom = potentialAbility - currentAbility;
        var headroomRatio = Math.Clamp((double)currentHeadroom / maxHeadroom, 0.0, 1.0);

        // SinWave formülü: sin(headroomRatio * π / 2)
        var sinFactor = Math.Sin(headroomRatio * (Math.PI / 2.0));

        // 2. Yaş faktörü mevki profilinin zirve yaşına göredir (varsayılan zirve 24 → 17/21/24/28 eşikleri).
        var yearsToPeak = peakAge - age;
        var ageMultiplier = yearsToPeak switch
        {
            >= 7 => 1.15,  // Çok genç: öğrenme hızlı ama fiziksel olgunluk eksik
            >= 3 => 1.25,  // Altın gelişim çağı
            >= 0 => 1.00,  // Zirveye yaklaşma
            >= -4 => 0.80, // Olgunluk
            _ => 0.50,     // Düşüş dönemi
        };

        var multiplier = sinFactor * ageMultiplier;
        var effective = (int)Math.Round(rawPoints * multiplier);

        // Tavan dolmamışsa ve puan verilmişse en az 1 puan garantisi
        return Math.Max(1, effective);
    }
}
