namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;

/// <summary>
/// Tekrarlanan olumsuz eşleşme desenini rakibin öncelikli tehdidine göre
/// somut ve mevcut seçimden farklı bir taktik reçetesine dönüştürür.
/// </summary>
public sealed record AlternativePlanPrescriptionDigest(
    bool HasPrescription,
    string BrandTitle,
    string Headline,
    string PrescriptionLine,
    Formation? SuggestedFormation,
    TacticalApproach? SuggestedApproach)
{
    public const string Brand = "Alternatif Plan";

    public static AlternativePlanPrescriptionDigest Clear() =>
        new(false, Brand, "Alternatif plan gerekmiyor.", string.Empty, null, null);

    public static AlternativePlanPrescriptionDigest Compose(
        MatchupPlanDigest? currentPlan,
        RepeatedPatternWarningDigest? warning)
    {
        if (currentPlan is null || warning is null || !warning.HasWarning)
        {
            return Clear();
        }

        var recommendation = SelectPrimary(currentPlan.ThreatKind);
        if (recommendation.Formation == currentPlan.Formation
            && recommendation.Approach == currentPlan.Approach)
        {
            recommendation = SelectFallback(currentPlan.ThreatKind);
        }

        var formation = MatchupPlanDigest.FormatFormationLabel(recommendation.Formation);
        var approach = MatchupPlanDigest.FormatApproachLabel(recommendation.Approach);
        return new AlternativePlanPrescriptionDigest(
            true,
            Brand,
            "Aynı deseni kırmak için plan değişikliği.",
            $"Reçete: {formation} · {approach} — {recommendation.Reason}",
            recommendation.Formation,
            recommendation.Approach);
    }

    private static Recommendation SelectPrimary(OpponentThreatKind threatKind) => threatKind switch
    {
        OpponentThreatKind.ProductiveAttack => new(
            Formation.F442,
            TacticalApproach.Defensive,
            "üretken hücuma karşı iki kompakt hatla geçiş alanını daralt."),
        OpponentThreatKind.WinningStreak => new(
            Formation.F442,
            TacticalApproach.Balanced,
            "galibiyet serisinin erken temposuna kapılmadan oyunda kal."),
        OpponentThreatKind.SquadQuality => new(
            Formation.F433,
            TacticalApproach.Defensive,
            "kadro kalitesine karşı üçlü orta saha korumasıyla çıkış pasını hazır tut."),
        OpponentThreatKind.TopZoneTempo => new(
            Formation.F442,
            TacticalApproach.Balanced,
            "zirve temposuna karşı hatlar arası mesafeyi koruyup kontrollü başla."),
        OpponentThreatKind.DefensiveResistance => new(
            Formation.F433,
            TacticalApproach.Attacking,
            "savunma direncini kanat genişliğiyle açarken merkez emniyetini kaybetme."),
        _ => new(
            Formation.F433,
            TacticalApproach.Balanced,
            "dengeli rakip profilinde şekli değiştirip merkez kontrolünü koru."),
    };

    private static Recommendation SelectFallback(OpponentThreatKind threatKind) => threatKind switch
    {
        OpponentThreatKind.ProductiveAttack => new(
            Formation.F433,
            TacticalApproach.Balanced,
            "üretken hücuma karşı orta saha emniyetini artırıp geçiş riskini azalt."),
        OpponentThreatKind.WinningStreak or OpponentThreatKind.TopZoneTempo => new(
            Formation.F433,
            TacticalApproach.Defensive,
            "yüksek tempoya karşı merkezde ekstra koruma kur ve acele etme."),
        OpponentThreatKind.SquadQuality => new(
            Formation.F442,
            TacticalApproach.Balanced,
            "kalite farkına karşı kompakt kalırken çıkış bağlantılarını koru."),
        OpponentThreatKind.DefensiveResistance => new(
            Formation.F433,
            TacticalApproach.Balanced,
            "genişliği koruyup hücum riskini bir kademe düşürerek sabırlı dolaş."),
        _ => new(
            Formation.F442,
            TacticalApproach.Balanced,
            "aynı yaklaşımda farklı hat yerleşimiyle olumsuz deseni kır."),
    };

    private readonly record struct Recommendation(
        Formation Formation,
        TacticalApproach Approach,
        string Reason);
}
