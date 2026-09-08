using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Domain.ClubGovernance;

/// <summary>
/// Kulübün kurumsal kimliği, gelenekleri ve yönetim kültürünü temsil eden model (Yenilik 5: Club DNA).
/// </summary>
public sealed class ClubDnaProfile
{
    public const int MinScore = 0;
    public const int MaxScore = 100;
    public const int DefaultNeutralScore = 50;

    public ClubDnaProfile(
        ClubId clubId,
        int youthDevelopmentCulture = DefaultNeutralScore,
        int recruitmentAggression = DefaultNeutralScore,
        int sellingClubTendency = DefaultNeutralScore,
        int managerialPatience = DefaultNeutralScore,
        int wageDiscipline = DefaultNeutralScore,
        int highIntensityTradition = DefaultNeutralScore,
        int seasonsTracked = 0)
    {
        ClubId = clubId;
        YouthDevelopmentCulture = Math.Clamp(youthDevelopmentCulture, MinScore, MaxScore);
        RecruitmentAggression = Math.Clamp(recruitmentAggression, MinScore, MaxScore);
        SellingClubTendency = Math.Clamp(sellingClubTendency, MinScore, MaxScore);
        ManagerialPatience = Math.Clamp(managerialPatience, MinScore, MaxScore);
        WageDiscipline = Math.Clamp(wageDiscipline, MinScore, MaxScore);
        HighIntensityTradition = Math.Clamp(highIntensityTradition, MinScore, MaxScore);
        SeasonsTracked = Math.Max(0, seasonsTracked);
    }

    public ClubId ClubId { get; }

    /// <summary>
    /// Altyapıdan gelen veya genç oyunculara süre verme kültürü (0–100).
    /// </summary>
    public int YouthDevelopmentCulture { get; private set; }

    /// <summary>
    /// Transfer piyasasında agresif ve büyük harcamalar yapma eğilimi (0–100).
    /// </summary>
    public int RecruitmentAggression { get; private set; }

    /// <summary>
    /// Yıldız oyuncuları yüksek kârla satma refleks ve alışkanlığı (0–100).
    /// </summary>
    public int SellingClubTendency { get; private set; }

    /// <summary>
    /// Kötü gidişatta menajere verilen kredi ve sabır eşiği (0–100).
    /// </summary>
    public int ManagerialPatience { get; private set; }

    /// <summary>
    /// Maaş bütçesini aşmama ve sıkı harcama disiplini (0–100).
    /// </summary>
    public int WageDiscipline { get; private set; }

    /// <summary>
    /// Yüksek pres ve tempolu oyunu kulüp felsefesi olarak benimseme geleneği (0–100).
    /// </summary>
    public int HighIntensityTradition { get; private set; }

    public int SeasonsTracked { get; private set; }

    public static ClubDnaProfile CreateDefault(ClubId clubId) =>
        new(clubId);

    /// <summary>
    /// Sezon sonu committed fact'leri ile kulüp DNA'sını aşamalı ve deterministik olarak günceller.
    /// </summary>
    public void RecordSeasonFacts(
        int youthMinutesRatioPercent,
        int netTransferSpendMillion,
        int managersDismissedCount,
        int wageUtilizationPercent)
    {
        // Genç geliştirme: %25 genç süresi nötr kabul edilir; fazlası artırır, azı düşürür
        var youthDelta = (youthMinutesRatioPercent - 25) / 5;
        YouthDevelopmentCulture = Math.Clamp(YouthDevelopmentCulture + youthDelta, MinScore, MaxScore);

        // Agresif transfer: Net harcama pozitifse agresifliği artırır, satış fazlaysa düşürür
        var aggressionDelta = netTransferSpendMillion / 5;
        RecruitmentAggression = Math.Clamp(RecruitmentAggression + aggressionDelta, MinScore, MaxScore);

        // Satan kulüp refleksi: Net harcama negatifse (kâr) satış eğilimi artar
        var sellingDelta = netTransferSpendMillion < 0 ? Math.Abs(netTransferSpendMillion) / 4 : -2;
        SellingClubTendency = Math.Clamp(SellingClubTendency + sellingDelta, MinScore, MaxScore);

        // Menajer sabrı: Görevden alma sayısı sabrı eritir
        var patienceDelta = managersDismissedCount == 0 ? +4 : -(managersDismissedCount * 12);
        ManagerialPatience = Math.Clamp(ManagerialPatience + patienceDelta, MinScore, MaxScore);

        // Maaş disiplini: Bütçe %90'ın altındaysa disiplinli sayılır
        var wageDelta = wageUtilizationPercent > 95 ? -6 : (wageUtilizationPercent < 80 ? +4 : 0);
        WageDiscipline = Math.Clamp(WageDiscipline + wageDelta, MinScore, MaxScore);

        SeasonsTracked++;
    }
}
