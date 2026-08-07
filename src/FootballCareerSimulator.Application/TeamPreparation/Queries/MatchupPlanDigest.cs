namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;

/// <summary>
/// Seçili taktiğin Rakip Dosyası'ndaki öncelikli sinyalle maç öncesi uyumunu yorumlar.
/// Simülasyon sonucunu vaat etmez; oyuncuya tek bir risk veya fırsat odağı verir.
/// </summary>
public sealed record MatchupPlanDigest(
    string BrandTitle,
    string SelectionLine,
    string VerdictLine,
    MatchupPlanSignal Signal,
    OpponentThreatKind ThreatKind,
    Formation Formation,
    TacticalApproach Approach)
{
    public const string Brand = "Eşleşme Planı";

    public static MatchupPlanDigest Compose(
        Formation formation,
        TacticalApproach approach,
        OpponentDossierDigest dossier)
    {
        if (!Enum.IsDefined(formation))
        {
            throw new ArgumentOutOfRangeException(nameof(formation), formation, null);
        }

        if (!Enum.IsDefined(approach))
        {
            throw new ArgumentOutOfRangeException(nameof(approach), approach, null);
        }

        ArgumentNullException.ThrowIfNull(dossier);

        var assessment = approach switch
        {
            TacticalApproach.Attacking => EvaluateAttacking(formation, dossier),
            TacticalApproach.Defensive => EvaluateDefensive(formation, dossier),
            _ => EvaluateBalanced(formation, dossier),
        };

        return new MatchupPlanDigest(
            Brand,
            $"Seçim: {FormatFormationLabel(formation)} · {FormatApproachLabel(approach)}",
            assessment.Line,
            assessment.Signal,
            dossier.ThreatKind,
            formation,
            approach);
    }

    private static Assessment EvaluateAttacking(
        Formation formation,
        OpponentDossierDigest dossier)
    {
        if (formation == Formation.F352
            && dossier.ThreatKind == OpponentThreatKind.ProductiveAttack)
        {
            return Risk(
                "Risk: 3-5-2 + Hücum, üretken rakibe kanat arkası bırakabilir; "
                + "iki kanat bekini aynı anda çıkarma.");
        }

        var transitionThreat = dossier.ThreatKind is
            OpponentThreatKind.WinningStreak
            or OpponentThreatKind.ProductiveAttack
            or OpponentThreatKind.SquadQuality
            or OpponentThreatKind.TopZoneTempo;
        if (!dossier.ManagedIsHome
            && (dossier.StrengthDifference >= 3 || transitionThreat))
        {
            return Risk(
                $"Risk: {FormatFormationLabel(formation)} + Hücum, deplasmanda rakibin "
                + "geçiş tehdidine alan bırakıyor; ilk baskı kırılırsa tempoyu düşür.");
        }

        if (dossier.ThreatKind == OpponentThreatKind.DefensiveResistance)
        {
            var route = formation switch
            {
                Formation.F433 => "kanat genişliği ve ön alan baskısı",
                Formation.F352 => "merkezdeki sayısal üstünlük",
                _ => "iki forvetin ceza sahası varlığı",
            };
            return Opportunity(
                $"Fırsat: {FormatFormationLabel(formation)} + Hücum, savunma direncini "
                + $"{route} ile sınayabilir; sabırlı dolaşımı koru.");
        }

        if (dossier.StrengthDifference <= -7)
        {
            return Opportunity(
                $"Fırsat: {FormatFormationLabel(formation)} + Hücum, kalite üstünlüğünü "
                + "öne taşıyor; top kaybı emniyetini kaybetme.");
        }

        if (dossier.ManagedIsHome)
        {
            return Opportunity(
                $"Fırsat: {FormatFormationLabel(formation)} + Hücum, evde inisiyatifi "
                + "sana verir; ilk gol gelmezse yapıyı bozma.");
        }

        return Balance(
            $"Denge: {FormatFormationLabel(formation)} + Hücum cesur bir deplasman planı; "
            + "ilk 15 dakikada geçiş mesafelerini ölç.");
    }

    private static Assessment EvaluateDefensive(
        Formation formation,
        OpponentDossierDigest dossier)
    {
        var tooPassive = dossier.StrengthDifference <= -7
            || (dossier.ManagedIsHome && dossier.StrengthDifference <= -3)
            || dossier.ThreatKind == OpponentThreatKind.DefensiveResistance;
        if (tooPassive)
        {
            return Risk(
                $"Risk: {FormatFormationLabel(formation)} + Defans, bu eşleşmede inisiyatifi "
                + "gereksiz yere rakibe bırakabilir; baskı çizgisini çok geriye kurma.");
        }

        var pressureThreat = dossier.StrengthDifference >= 3
            || dossier.ThreatKind is OpponentThreatKind.WinningStreak
                or OpponentThreatKind.ProductiveAttack
                or OpponentThreatKind.SquadQuality
                or OpponentThreatKind.TopZoneTempo;
        if (pressureThreat)
        {
            var protection = formation switch
            {
                Formation.F442 => "iki kompakt hat",
                Formation.F433 => "üçlü orta saha koruması",
                _ => "merkezdeki beşli yoğunluk",
            };
            return Opportunity(
                $"Fırsat: {FormatFormationLabel(formation)} + Defans, rakibin baskısına karşı "
                + $"{protection} sunuyor; çıkış pasını hazır tut.");
        }

        if (dossier.ManagedIsHome)
        {
            return Risk(
                $"Risk: {FormatFormationLabel(formation)} + Defans, dengeli rakibe karşı ev "
                + "inisiyatifini teslim edebilir; orta bloktan öne çıkış tetikle.");
        }

        return Balance(
            $"Denge: {FormatFormationLabel(formation)} + Defans deplasmanda oyunda kalmayı "
            + "hedefliyor; yalnızlaşan çıkış oyuncusuna destek ver.");
    }

    private static Assessment EvaluateBalanced(
        Formation formation,
        OpponentDossierDigest dossier)
    {
        if (dossier.ThreatKind is OpponentThreatKind.WinningStreak
            or OpponentThreatKind.TopZoneTempo)
        {
            return Opportunity(
                $"Fırsat: {FormatFormationLabel(formation)} + Dengeli, rakibin erken temposuna "
                + "kapılmadan oyunda kalmanı sağlar; ilk 20 dakikada acele etme.");
        }

        if (dossier.ThreatKind == OpponentThreatKind.DefensiveResistance
            && formation == Formation.F433)
        {
            return Opportunity(
                "Fırsat: 4-3-3 + Dengeli, dirençli savunmaya karşı genişlik verirken "
                + "merkez emniyetini koruyor.");
        }

        if (dossier.StrengthDifference >= 3
            || dossier.ThreatKind is OpponentThreatKind.ProductiveAttack
                or OpponentThreatKind.SquadQuality)
        {
            return Balance(
                $"Denge: {FormatFormationLabel(formation)} + Dengeli, rakibin gücüne karşı "
                + "kontrollü başlangıç sunuyor; baskı kırılmadan risk artırma.");
        }

        if (dossier.ManagedIsHome && dossier.StrengthDifference <= -7)
        {
            return Opportunity(
                $"Fırsat: {FormatFormationLabel(formation)} + Dengeli, kalite üstünlüğünü "
                + "kontrolü kaybetmeden kullanmana izin veriyor.");
        }

        return Balance(
            $"Denge: {FormatFormationLabel(formation)} + Dengeli, dosyada belirgin bir ters "
            + "eşleşme üretmiyor; maçı okuyup ilk ayarı sahada yap.");
    }

    private static Assessment Risk(string line) => new(MatchupPlanSignal.Risk, line);

    private static Assessment Opportunity(string line) =>
        new(MatchupPlanSignal.Opportunity, line);

    private static Assessment Balance(string line) => new(MatchupPlanSignal.Balance, line);

    public static string FormatFormationLabel(Formation formation) => formation switch
    {
        Formation.F442 => "4-4-2",
        Formation.F433 => "4-3-3",
        Formation.F352 => "3-5-2",
        _ => formation.ToString(),
    };

    public static string FormatApproachLabel(TacticalApproach approach) => approach switch
    {
        TacticalApproach.Balanced => "Dengeli",
        TacticalApproach.Attacking => "Hücum",
        TacticalApproach.Defensive => "Defans",
        _ => approach.ToString(),
    };

    private readonly record struct Assessment(MatchupPlanSignal Signal, string Line);
}

public enum MatchupPlanSignal
{
    Balance = 0,
    Opportunity = 1,
    Risk = 2,
}
