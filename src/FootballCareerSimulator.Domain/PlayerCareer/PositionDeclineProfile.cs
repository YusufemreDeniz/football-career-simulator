namespace FootballCareerSimulator.Domain.PlayerCareer;

/// <summary>
/// Oyuncu mevkilerinin fiziksel ve biyomekanik gereksinimlerine göre ayrışan mevki kategorileri.
/// </summary>
public enum PositionCategory
{
    Goalkeeper = 1,
    CentralDefender = 2,
    FullbackOrWinger = 3,
    CentralMidfielder = 4,
    Forward = 5,
}

/// <summary>
/// SM25 ve gerçek futbol verilerinden esinlenen mevki bazlı yaşlanma ve gerileme profili.
/// Kaleciler geç yaşlanır ve yavaş düşer; kanat oyuncuları patlayıcılık kaybıyla erken düşüşe geçer.
/// </summary>
public sealed record PositionDeclineProfile(
    PositionCategory Category,
    int PeakStartAge,
    int DeclineStartAge,
    int RetirementEligibleAge = 35)
{
    // DESIGN_TRANSLATION §1–2: kalecide zirve 30–34, gerileme 35'te başlar; stoper orta sahaya göre +1 yıl.
    public static readonly PositionDeclineProfile Goalkeeper = new(
        PositionCategory.Goalkeeper,
        PeakStartAge: 30,
        DeclineStartAge: 35,
        RetirementEligibleAge: 38);

    public static readonly PositionDeclineProfile CentralDefender = new(
        PositionCategory.CentralDefender,
        PeakStartAge: 24,
        DeclineStartAge: 31,
        RetirementEligibleAge: 35);

    public static readonly PositionDeclineProfile FullbackOrWinger = new(
        PositionCategory.FullbackOrWinger,
        PeakStartAge: 23,
        DeclineStartAge: 28,
        RetirementEligibleAge: 34);

    public static readonly PositionDeclineProfile CentralMidfielder = new(
        PositionCategory.CentralMidfielder,
        PeakStartAge: 24,
        DeclineStartAge: 30,
        RetirementEligibleAge: 35);

    public static readonly PositionDeclineProfile Forward = new(
        PositionCategory.Forward,
        PeakStartAge: 24,
        DeclineStartAge: 30,
        RetirementEligibleAge: 35);

    /// <summary>
    /// Slot indeksine göre mevki profilini belirler (0-24 standardı).
    /// </summary>
    public static PositionDeclineProfile ForSlot(int slotIndex) =>
        slotIndex switch
        {
            0 or 11 or 22 => Goalkeeper,
            1 or 4 or 12 or 15 => FullbackOrWinger,
            2 or 3 or 13 or 14 => CentralDefender,
            5 or 8 or 16 or 19 => CentralMidfielder,
            6 or 7 or 17 or 18 => FullbackOrWinger, // Ofansif kanat / forvet arkası
            9 or 10 or 20 or 21 => Forward,
            _ => CentralMidfielder,
        };

    /// <summary>
    /// Yaşa göre kariyer evresini belirler.
    /// </summary>
    public CareerPhase GetPhase(int age)
    {
        if (age < PeakStartAge)
        {
            return CareerPhase.Developing;
        }

        if (age < DeclineStartAge)
        {
            return CareerPhase.Peak;
        }

        return CareerPhase.Declining;
    }

    /// <summary>
    /// Yıllık yaşlanma döngüsünde düşecek yetenek ve potansiyel miktarını hesaplar.
    /// </summary>
    public (int AbilityDrop, int PotentialDrop) CalculateAnnualDrop(int age)
    {
        if (age < DeclineStartAge)
        {
            return (0, 0);
        }

        return Category switch
        {
            PositionCategory.Goalkeeper =>
                // Kaleciler 35'ten sonra yılda 1 puan kaybeder, 37'den sonra 2 puan
                (age >= 37 ? 2 : 1, age >= 38 ? 1 : 0),

            PositionCategory.FullbackOrWinger =>
                // Kanatlar 28'den sonra patlayıcılık kaybıyla 2 puan, 31'den sonra 3 puan kaybeder
                (age >= 31 ? 3 : 2, age >= 30 ? 2 : 1),

            _ =>
                // Stoper, orta saha ve forvetler için dengeli profil (mevcut slot 3 uyumlu)
                (age >= 33 ? 2 : 1, age >= 34 ? 1 : 0),
        };
    }
}
