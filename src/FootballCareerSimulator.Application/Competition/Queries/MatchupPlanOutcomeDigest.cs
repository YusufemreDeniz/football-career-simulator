namespace FootballCareerSimulator.Application.Competition.Queries;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.Match;

/// <summary>
/// Düdük öncesi eşleşme uyarısını, maçın ilk kırılma anı ve ikinci yarı skoruyla
/// karşılaştırır. Nedensellik iddia etmez; oyuncuya kararından öğrenebileceği bir iz bırakır.
/// </summary>
public sealed record MatchupPlanOutcomeDigest(
    string BrandTitle,
    string SelectionLine,
    string PreMatchLine,
    string EvidenceLine,
    string VerdictLine,
    MatchupPlanOutcomeSignal Signal)
{
    public const string Brand = "Planın Sahadaki İzi";

    public string SummaryLine =>
        $"{BrandTitle} · {SelectionLine} · {PreMatchLine} · {EvidenceLine} · {VerdictLine}";

    public static MatchupPlanOutcomeDigest? Compose(
        MatchupPlanDigest plan,
        MatchHalfTimeDigest? halfTime,
        PlayFixtureMatchResult result,
        bool managedIsHome)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(result);
        if (!result.Succeeded)
        {
            return null;
        }

        var managedGoals = managedIsHome ? result.HomeGoals : result.AwayGoals;
        var opponentGoals = managedIsHome ? result.AwayGoals : result.HomeGoals;
        var finalMargin = managedGoals - opponentGoals;
        var firstBreak = ResolveFirstBreak(result.KeyMoments, managedIsHome);

        int? secondHalfMargin = null;
        string secondHalfLine;
        if (halfTime is { HasManagedMatch: true }
            && halfTime.ManagedIsHome == managedIsHome
            && result.HomeGoals >= halfTime.HomeGoals
            && result.AwayGoals >= halfTime.AwayGoals)
        {
            var halfTimeManagedGoals = managedIsHome
                ? halfTime.HomeGoals
                : halfTime.AwayGoals;
            var halfTimeOpponentGoals = managedIsHome
                ? halfTime.AwayGoals
                : halfTime.HomeGoals;
            var secondHalfManagedGoals = managedGoals - halfTimeManagedGoals;
            var secondHalfOpponentGoals = opponentGoals - halfTimeOpponentGoals;
            secondHalfMargin = secondHalfManagedGoals - secondHalfOpponentGoals;
            secondHalfLine = $"ikinci yarı {secondHalfManagedGoals}-{secondHalfOpponentGoals}";
        }
        else
        {
            secondHalfLine = "ikinci yarı verisi yok";
        }

        var assessment = ResolveVerdict(
            plan.Signal,
            finalMargin,
            secondHalfMargin,
            firstBreak.FavoursManaged);
        return new MatchupPlanOutcomeDigest(
            Brand,
            plan.SelectionLine,
            $"Düdük öncesi: {FormatPlanSignal(plan.Signal)}",
            $"Saha izi: {firstBreak.Line} · {secondHalfLine} · final {managedGoals}-{opponentGoals}",
            assessment.Line,
            assessment.Signal);
    }

    private static FirstBreak ResolveFirstBreak(
        IReadOnlyList<MatchKeyMomentReadModel>? moments,
        bool managedIsHome)
    {
        var moment = (moments ?? Array.Empty<MatchKeyMomentReadModel>())
            .Where(item =>
                string.Equals(item.Kind, nameof(MatchKeyMomentKind.Goal), StringComparison.Ordinal)
                || string.Equals(item.Kind, nameof(MatchKeyMomentKind.RedCard), StringComparison.Ordinal))
            .OrderBy(item => item.Minute)
            .FirstOrDefault();
        if (moment is null)
        {
            return new FirstBreak("belirgin kırılma yok", null);
        }

        var favoursManaged = moment.IsHomeSide == managedIsHome;
        if (string.Equals(moment.Kind, nameof(MatchKeyMomentKind.RedCard), StringComparison.Ordinal))
        {
            // Kırmızı kartı gören taraf için olay olumlu değil; "favours" rakip tarafı gösterir.
            favoursManaged = !favoursManaged;
            var side = moment.IsHomeSide == managedIsHome ? "sen gördün" : "rakip gördü";
            return new FirstBreak($"{moment.Minute}' kırmızı kartı {side}", favoursManaged);
        }

        return new FirstBreak(
            $"{moment.Minute}' ilk golü {(favoursManaged ? "sen attın" : "rakip attı")}",
            favoursManaged);
    }

    private static Assessment ResolveVerdict(
        MatchupPlanSignal planSignal,
        int finalMargin,
        int? secondHalfMargin,
        bool? firstBreakFavoursManaged)
    {
        if (planSignal == MatchupPlanSignal.Risk)
        {
            if (finalMargin > 0)
            {
                return Positive("Riski yönettin — uyarıya rağmen üstünlüğü sonuca taşıdın.");
            }

            if (finalMargin < 0)
            {
                return Warning("Uyarı sahada karşılık buldu — eşleşme riski giderilemedi.");
            }

            if (secondHalfMargin < 0 || firstBreakFavoursManaged is false)
            {
                return Neutral("Risk maç içinde göründü; skor yine de dengede kaldı.");
            }

            return Neutral("Risk sonucu bozmadı; plan da belirgin üstünlük üretmedi.");
        }

        if (planSignal == MatchupPlanSignal.Opportunity)
        {
            if (finalMargin > 0)
            {
                return Positive("Fırsatı sonuca çevirdin — maç önü okuması sahada karşılık buldu.");
            }

            if (secondHalfMargin > 0 || firstBreakFavoursManaged is true)
            {
                return Neutral("Fırsat maç içinde göründü fakat final skoruna tam taşınmadı.");
            }

            return finalMargin < 0
                ? Warning("Fırsat kağıt üzerinde kaldı — eşleşme rakibin lehine döndü.")
                : Neutral("Fırsat skora dönüşmedi; maç dengede kapandı.");
        }

        return Math.Sign(finalMargin) switch
        {
            > 0 => Positive("Dengeyi lehine bozdun — kontrollü plan galibiyete taşındı."),
            < 0 => Warning("Denge rakibin lehine kırıldı — sonraki maç için ayar gerekiyor."),
            _ => Neutral("Öngörülen denge final skoruna yansıdı."),
        };
    }

    private static string FormatPlanSignal(MatchupPlanSignal signal) => signal switch
    {
        MatchupPlanSignal.Risk => "Risk",
        MatchupPlanSignal.Opportunity => "Fırsat",
        _ => "Denge",
    };

    private static Assessment Positive(string line) =>
        new(MatchupPlanOutcomeSignal.Positive, line);

    private static Assessment Neutral(string line) =>
        new(MatchupPlanOutcomeSignal.Neutral, line);

    private static Assessment Warning(string line) =>
        new(MatchupPlanOutcomeSignal.Warning, line);

    private readonly record struct FirstBreak(string Line, bool? FavoursManaged);

    private readonly record struct Assessment(MatchupPlanOutcomeSignal Signal, string Line);
}

public enum MatchupPlanOutcomeSignal
{
    Neutral = 0,
    Positive = 1,
    Warning = 2,
}
