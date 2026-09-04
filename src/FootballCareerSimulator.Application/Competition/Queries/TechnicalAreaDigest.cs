namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Devre arası yaklaşımının ardından oluşan ikinci yarı skorunu, yönetilen takım
/// perspektifinden kısa ve deterministik bir maç sonu değerlendirmesine dönüştürür.
/// </summary>
public sealed record TechnicalAreaDigest(
    string BrandTitle,
    string DecisionLine,
    string ScoreFlowLine,
    string VerdictLine)
{
    public const string Brand = "Teknik Alan";

    public static TechnicalAreaDigest? Compose(
        MatchHalfTimeDigest? halfTime,
        int finalHomeGoals,
        int finalAwayGoals,
        int managedSecondHalfDelta)
    {
        if (halfTime is not { HasManagedMatch: true }
            || finalHomeGoals < halfTime.HomeGoals
            || finalAwayGoals < halfTime.AwayGoals)
        {
            return null;
        }

        var decision = managedSecondHalfDelta switch
        {
            MatchHalfTimeDigest.DecisionAttack => "Karar: Hücuma geçtin",
            MatchHalfTimeDigest.DecisionDefend => "Karar: Savunmaya çektin",
            MatchHalfTimeDigest.DecisionContinue => "Karar: Aynı planla devam ettin",
            _ => null,
        };
        if (decision is null)
        {
            return null;
        }

        var halfTimeManagedGoals = halfTime.ManagedIsHome
            ? halfTime.HomeGoals
            : halfTime.AwayGoals;
        var halfTimeOpponentGoals = halfTime.ManagedIsHome
            ? halfTime.AwayGoals
            : halfTime.HomeGoals;
        var finalManagedGoals = halfTime.ManagedIsHome
            ? finalHomeGoals
            : finalAwayGoals;
        var finalOpponentGoals = halfTime.ManagedIsHome
            ? finalAwayGoals
            : finalHomeGoals;
        var secondHalfManagedGoals = finalManagedGoals - halfTimeManagedGoals;
        var secondHalfOpponentGoals = finalOpponentGoals - halfTimeOpponentGoals;

        return new TechnicalAreaDigest(
            Brand,
            decision,
            $"Skor akışı: devre {halfTimeManagedGoals}-{halfTimeOpponentGoals}"
                + $" · ikinci yarı {secondHalfManagedGoals}-{secondHalfOpponentGoals}"
                + $" · final {finalManagedGoals}-{finalOpponentGoals}",
            ResolveVerdict(
                managedSecondHalfDelta,
                halfTimeManagedGoals - halfTimeOpponentGoals,
                secondHalfManagedGoals,
                secondHalfOpponentGoals,
                finalManagedGoals - finalOpponentGoals));
    }

    private static string ResolveVerdict(
        int decision,
        int halfTimeMargin,
        int secondHalfManagedGoals,
        int secondHalfOpponentGoals,
        int finalMargin)
    {
        var secondHalfMargin = secondHalfManagedGoals - secondHalfOpponentGoals;
        if (decision == MatchHalfTimeDigest.DecisionAttack)
        {
            if (secondHalfMargin > 0)
            {
                return "Risk karşılık buldu — ikinci yarıyı kazandın.";
            }

            if (secondHalfMargin < 0)
            {
                return "Risk geri tepti — ikinci yarıyı rakip kazandı.";
            }

            return secondHalfManagedGoals > 0
                ? "Risk gol getirdi ama ikinci yarıdaki dengeyi bozmadı."
                : "Hücum kararı skora yansımadı.";
        }

        if (decision == MatchHalfTimeDigest.DecisionDefend)
        {
            if (halfTimeMargin > 0)
            {
                return finalMargin > 0
                    ? "Plan tuttu — devre üstünlüğünü korudun."
                    : "Koruma planı yetmedi — devre üstünlüğü eridi.";
            }

            if (secondHalfMargin > 0)
            {
                return "Savunma dengesi rakibi durdurdu; fırsatı da buldun.";
            }

            if (secondHalfOpponentGoals == 0)
            {
                return "Savunma kararı rakibi durdurdu.";
            }

            return secondHalfMargin == 0
                ? "Savunma kararı ikinci yarıdaki dengeyi tuttu."
                : "Geri çekilmek rakibi durdurmadı.";
        }

        return Math.Sign(secondHalfMargin) switch
        {
            1 => "Aynı plan ikinci yarıda üstünlük getirdi.",
            -1 => "Aynı plan ikinci yarıda rakibe cevap veremedi.",
            _ => "Aynı plan ikinci yarıdaki dengeyi bozmadı.",
        };
    }
}
