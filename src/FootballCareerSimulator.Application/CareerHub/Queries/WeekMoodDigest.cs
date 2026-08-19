using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.Transfer.Queries;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Bugün — sakatlık yayını yokken kısa “haftanın havası” (duygu / tempo).
/// </summary>
public sealed record WeekMoodDigest(
    bool IsActive,
    string BrandTitle,
    string MoodLine,
    string MoodCode)
{
    public const string Brand = "Haftanın Havası";

    public const string MoodDesk = "Desk";
    public const string MoodPromise = "Promise";
    public const string MoodMatchDraft = "MatchDraft";
    public const string MoodMatchReady = "MatchReady";
    public const string MoodPrep = "Prep";
    public const string MoodLeague = "League";
    public const string MoodTransfer = "Transfer";
    public const string MoodFormRise = "FormRise";
    public const string MoodFormCrisis = "FormCrisis";
    public const string MoodCalmMatch = "CalmMatch";
    public const string MoodCalm = "Calm";

    public static WeekMoodDigest Clear() =>
        new(false, Brand, string.Empty, string.Empty);

    public static WeekMoodDigest Compose(
        DecisionDeskDigest desk,
        PreMatchBriefing match,
        PreparationBriefing prep,
        LeagueWorldBriefing league,
        TransferDeskBriefing? transfer = null,
        bool weekStoryActive = false,
        string? formMomentumCode = null)
    {
        ArgumentNullException.ThrowIfNull(desk);
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(prep);
        ArgumentNullException.ThrowIfNull(league);
        transfer ??= TransferDeskBriefing.Unemployed();

        if (weekStoryActive)
        {
            return Clear();
        }

        if (desk.HasOpenDecision)
        {
            if (!string.IsNullOrWhiteSpace(desk.CausalityLine)
                && desk.CausalityLine.Contains("Kırmızı kart", StringComparison.OrdinalIgnoreCase))
            {
                return Active("Kırmızı kart — soyunma odasını temizle, sonra ritim kur.", MoodDesk);
            }

            if (!string.IsNullOrWhiteSpace(desk.CausalityLine)
                && (desk.CausalityLine.Contains("yedek", StringComparison.OrdinalIgnoreCase)
                    || desk.CausalityLine.Contains("kadro dışı", StringComparison.OrdinalIgnoreCase)))
            {
                return Active("Yedek kalan forma istiyor — masayı temizle, sonra ritim kur.", MoodDesk);
            }

            return Active(
                desk.IsHardBlocker
                    ? "Masada zorunlu dosya — hafta burada kilitli."
                    : "Masada iş var — ofisi temizle, sonra ritim kur.",
                MoodDesk);
        }

        if (match is { HasMatch: true, HasPromiseRisk: true })
        {
            return Active("Söz gerilimi havada — XI↔Yedek'i düşünmeden düdük çalma.", MoodPromise);
        }

        if (match is { HasMatch: true, IsReadyToKickOff: false })
        {
            return Active("Maç kapıda — kadro henüz kilitlenmedi, tempo eksik.", MoodMatchDraft);
        }

        if (match is { HasMatch: true, IsReadyToKickOff: true })
        {
            return Active("Düdük yakın — kadro hazır, haftanın ritmi senin.", MoodMatchReady);
        }

        if (transfer.NextStep is { } exitStep
            && (string.Equals(exitStep.ReasonCode, TransferNextStep.ReasonSellFringe, StringComparison.Ordinal)
                || string.Equals(exitStep.ReasonCode, TransferNextStep.ReasonPromiseExit, StringComparison.Ordinal)))
        {
            return Active($"Transfer masası sıcak — {transfer.Headline}", MoodTransfer);
        }

        if (prep is { IsEmployed: true, DemandsAttention: true })
        {
            var mood = prep.Suggestion?.ActionCode switch
            {
                PrepPlanSuggestion.SeedWeek => "Plan boş — haftayı birincil düğmeyle kur.",
                PrepPlanSuggestion.ApplyRecovery => "Kadro yorgun — Toparlanma havası hakim.",
                PrepPlanSuggestion.ApplyFitness => "Fitness düşük — Kondisyon çağırıyor.",
                PrepPlanSuggestion.SoftenLoad => "Yük ağır — temposu düşürmek lazım.",
                _ => "Hazırlık Masası çağırıyor — haftayı sıkı tut.",
            };
            return Active(mood, MoodPrep);
        }

        if (league is { HasSeason: true, DemandsAttention: true })
        {
            return Active(
                league.NextStep?.PulseHeadline ?? "Lig baskısı var — sıralama konuşuyor.",
                MoodLeague);
        }

        if (transfer is { IsEmployed: true, DemandsAttention: true })
        {
            return Active($"Transfer masası sıcak — {transfer.Headline}", MoodTransfer);
        }

        if (string.Equals(
                formMomentumCode,
                DressingRoomEchoDigest.MomentumWinningStreak,
                StringComparison.Ordinal))
        {
            return Active("Üç maçlık galibiyet serisi — ritmi koru.", MoodFormRise);
        }

        if (string.Equals(
                formMomentumCode,
                DressingRoomEchoDigest.MomentumLosingStreak,
                StringComparison.Ordinal))
        {
            return Active("Üç maçlık mağlubiyet serisi — haftayı toparlanmaya çevir.", MoodFormCrisis);
        }

        if (prep.IsEmployed && match.HasMatch)
        {
            return Active("Sakin tempo — sıradaki maça temiz git.", MoodCalmMatch);
        }

        if (prep.IsEmployed)
        {
            return Active("Sakin hafta — plan tutuyor, günü ilerlet.", MoodCalm);
        }

        return Clear();
    }

    public string ToDisplayText()
    {
        if (!IsActive)
        {
            return string.Empty;
        }

        return $"{BrandTitle}\n{MoodLine}";
    }

    public string ToPulseLine() =>
        IsActive ? $"Hava: {MoodLine}" : string.Empty;

    private static WeekMoodDigest Active(string moodLine, string moodCode) =>
        new(true, Brand, moodLine, moodCode);
}
