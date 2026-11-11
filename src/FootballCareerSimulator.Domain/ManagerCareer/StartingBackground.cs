namespace FootballCareerSimulator.Domain.ManagerCareer;

/// <summary>
/// GDD 7.1 / docs/10_MANAGER_CAREER.md §8.2 authored starting background seti.
/// Kalıcı Manager Profile değildir; kulüp değişiminde değişmez.
/// </summary>
public enum StartingBackground
{
    AmateurHeadCoach = 1,
    YouthAcademyCoach = 2,
    AssistantCoach = 3,
    LowerLeagueYouthManager = 4,
    RecentlyRetiredPlayer = 5,
    TacticalSpecialist = 6,
}

/// <summary>
/// Background'un ilk reputation, Board Confidence, kulüp gücü bandı ve anlatı sinyallerini taşır.
/// Exact denge formülü belgede açık bırakılmıştır; bu bantlar ADIM 2 için geri alınabilir varsayımdır (D-399).
/// </summary>
public static class StartingBackgroundCatalog
{
    public const int OfferCount = 3;
    public const string ReasonPrefix = "StartBackground:";

    public static IReadOnlyList<StartingBackground> All { get; } =
    [
        StartingBackground.AmateurHeadCoach,
        StartingBackground.YouthAcademyCoach,
        StartingBackground.AssistantCoach,
        StartingBackground.LowerLeagueYouthManager,
        StartingBackground.RecentlyRetiredPlayer,
        StartingBackground.TacticalSpecialist,
    ];

    public static int InitialReputation(StartingBackground background) =>
        background switch
        {
            StartingBackground.AmateurHeadCoach => 38,
            StartingBackground.YouthAcademyCoach => 44,
            StartingBackground.AssistantCoach => 56,
            StartingBackground.LowerLeagueYouthManager => 42,
            StartingBackground.RecentlyRetiredPlayer => 58,
            StartingBackground.TacticalSpecialist => 50,
            _ => ManagerReputation.DefaultInitialValue,
        };

    public static int InitialBoardConfidence(StartingBackground background) =>
        background switch
        {
            StartingBackground.AmateurHeadCoach => 50,
            StartingBackground.YouthAcademyCoach => 52,
            StartingBackground.AssistantCoach => 48,
            StartingBackground.LowerLeagueYouthManager => 53,
            StartingBackground.RecentlyRetiredPlayer => 58,
            StartingBackground.TacticalSpecialist => 51,
            _ => BoardConfidence.DefaultInitialValue,
        };

    public static (int MinInclusive, int MaxInclusive) ClubStrengthBand(StartingBackground background) =>
        background switch
        {
            StartingBackground.AmateurHeadCoach => (52, 63),
            StartingBackground.YouthAcademyCoach => (56, 70),
            StartingBackground.AssistantCoach => (70, 88),
            StartingBackground.LowerLeagueYouthManager => (54, 66),
            StartingBackground.RecentlyRetiredPlayer => (64, 82),
            StartingBackground.TacticalSpecialist => (58, 74),
            _ => (52, 70),
        };

    public static string ReasonCode(StartingBackground background) =>
        ReasonPrefix + background;

    public static bool TryParseReason(string? reasonCode, out StartingBackground background)
    {
        background = default;
        if (string.IsNullOrWhiteSpace(reasonCode)
            || !reasonCode.StartsWith(ReasonPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return Enum.TryParse(reasonCode[ReasonPrefix.Length..], ignoreCase: false, out background)
            && Enum.IsDefined(background);
    }

    public static string DisplayName(StartingBackground background) =>
        background switch
        {
            StartingBackground.AmateurHeadCoach => "Amatör takım teknik direktörü",
            StartingBackground.YouthAcademyCoach => "Altyapı antrenörü",
            StartingBackground.AssistantCoach => "Yardımcı antrenör",
            StartingBackground.LowerLeagueYouthManager => "Alt liglerde çalışan genç teknik direktör",
            StartingBackground.RecentlyRetiredPlayer => "Futbolculuktan yeni emekli olmuş eski oyuncu",
            StartingBackground.TacticalSpecialist => "Profesyonel futbol geçmişi olmayan taktik uzmanı",
            _ => background.ToString(),
        };

    public static string Pitch(StartingBackground background) =>
        background switch
        {
            StartingBackground.AmateurHeadCoach => "Mütevazı kulüpler; güveni sahada inşa edersin.",
            StartingBackground.YouthAcademyCoach => "Gelişim odaklı kulüpler genç kadroyu emanet eder.",
            StartingBackground.AssistantCoach => "Daha büyük kulüpler kapıyı açar; yönetim daha temkinlidir.",
            StartingBackground.LowerLeagueYouthManager => "Gerçekçi ilk iş: alt sıra ve inşa kulüpleri.",
            StartingBackground.RecentlyRetiredPlayer => "İsim tanınır; medya ve soyunma odası hemen bakar.",
            StartingBackground.TacticalSpecialist => "Fikirlerinle gelirsin; soyunma odası henüz ikna değildir.",
            _ => string.Empty,
        };

    public static string MediaInterest(StartingBackground background) =>
        background switch
        {
            StartingBackground.AmateurHeadCoach => "Düşük medya ilgisi",
            StartingBackground.YouthAcademyCoach => "Sakin, yerel ilgi",
            StartingBackground.AssistantCoach => "Kulüp gölgesinde ılımlı ilgi",
            StartingBackground.LowerLeagueYouthManager => "Sınırlı yerel haber",
            StartingBackground.RecentlyRetiredPlayer => "Yüksek açılış ilgisi",
            StartingBackground.TacticalSpecialist => "Merak uyandıran ama şüpheli açılış",
            _ => "Nötr medya ilgisi",
        };

    public static string PlayerApproach(StartingBackground background) =>
        background switch
        {
            StartingBackground.AmateurHeadCoach => "Futbolcular seni henüz kanıtlanmamış sayar.",
            StartingBackground.YouthAcademyCoach => "Gençler gelişim vaadine daha açık başlar.",
            StartingBackground.AssistantCoach => "Soyunma odası seni 'içeriden' ama henüz patron değil bilir.",
            StartingBackground.LowerLeagueYouthManager => "Kadro pratik bir iş ortağı bekler.",
            StartingBackground.RecentlyRetiredPlayer => "Eski oyuncu itibarı kapıyı açar, saygı ayrı kazanılır.",
            StartingBackground.TacticalSpecialist => "Fikir dinlenir; maç güveni henüz yoktur.",
            _ => string.Empty,
        };

    public static string ProfileSignal(StartingBackground background) =>
        background switch
        {
            StartingBackground.AmateurHeadCoach => "Sahada duruş, düşük kaynak disiplini",
            StartingBackground.YouthAcademyCoach => "Gelişim ve sabır sinyali",
            StartingBackground.AssistantCoach => "Hazır sisteme uyum",
            StartingBackground.LowerLeagueYouthManager => "İnşa ve tempo",
            StartingBackground.RecentlyRetiredPlayer => "Liderlik ve soyunma odası dili",
            StartingBackground.TacticalSpecialist => "Taktik fikir, saha dışı mesafe",
            _ => string.Empty,
        };

    public static string OfferFit(StartingBackground background) =>
        background switch
        {
            StartingBackground.AmateurHeadCoach => "Geçmişin bu güç bandındaki kulüplerle örtüşüyor.",
            StartingBackground.YouthAcademyCoach => "Kulüp, gelişim bakışını kadrosuna yakıştırıyor.",
            StartingBackground.AssistantCoach => "Yönetim, tanıdık bir ismi merdivenin üst basamağına deniyor.",
            StartingBackground.LowerLeagueYouthManager => "Bu kulüp gerçekçi bir ilk A takım işi arıyor.",
            StartingBackground.RecentlyRetiredPlayer => "İsmin, soyunma odasını hızlı toparlamak isteyen kulübe uyuyor.",
            StartingBackground.TacticalSpecialist => "Kulüp yeni bir fikir arıyor; kanıt senin maçların olacak.",
            _ => "Bu kulüp başlangıç geçmişine uygun bir teklif.",
        };
}
