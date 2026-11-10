namespace FootballCareerSimulator.Application.CareerHub.Queries;

using FootballCareerSimulator.Application.TeamPreparation.Queries;

/// <summary>
/// Ofisin tek birincil eylemi ("Devam") — oyun döngüsünü menü gezintisi yerine
/// "ilerle → olay çıkınca dur → maça çık" ritmine bağlar.
/// Öncelik: açık karar → Avrupa maçı → lig maçı → sezon geçişi → ilerle → engel.
/// Rutin hazırlık (boş hafta planı, sakatlık toparlanması) ilerlemeden önce otomatik uygulanır;
/// diğer öneriler (transfer, lig, yük ayarı) birincil düğmeyi ele geçirmez.
/// </summary>
public static class ContinueFlowGuide
{
    public const string KindAnswerDecision = "AnswerDecision";
    public const string KindPlayContinental = "PlayContinental";
    public const string KindPrepareMatch = "PrepareMatch";
    public const string KindTransitionSeason = "TransitionSeason";
    public const string KindAdvance = "Advance";
    public const string KindBlocked = "Blocked";

    public static ContinueStep Resolve(ContinueFlowInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.HasOpenDecision)
        {
            return new ContinueStep(
                KindAnswerDecision,
                "Karara Yanıt Ver",
                "Yanıt bekleyen bir karar var",
                "Takvim, karar masasındaki konu yanıtlanana kadar ilerlemez.",
                AutoApplyPlan: null);
        }

        if (input.HasDueContinentalMatch)
        {
            return new ContinueStep(
                KindPlayContinental,
                "Avrupa Maçına Çık",
                "Avrupa gecesi bugün",
                "Kadroyu ve maç planını kontrol et, sonra düdüğü çal.",
                AutoApplyPlan: null);
        }

        if (input.HasDueManagedMatch)
        {
            return new ContinueStep(
                KindPrepareMatch,
                "Maç Öncesine Git",
                string.IsNullOrWhiteSpace(input.NextOpponentName)
                    ? "Maç günü"
                    : $"Maç günü: {input.NextOpponentName}",
                input.DueMatchApproved
                    ? "Kadro onaylı — son kontrolü yap ve maça çık. Devam burada durur, maç atlanmaz."
                    : "İlk 11 ve taktiği kontrol et, kadroyu onayla. Devam burada durur, maç atlanmaz.",
                AutoApplyPlan: null);
        }

        if (input.SeasonTransitionReady)
        {
            return new ContinueStep(
                KindTransitionSeason,
                input.SeasonArchivePhase ? "Yeni Sezona Geç" : "Sezonu Bitir",
                "Sezon tamamlandı",
                "Sezon kapanışı ve yeni sezon hazırlığı bu adımla başlar.",
                AutoApplyPlan: null);
        }

        if (input.CanAdvance)
        {
            var autoPlan = IsRoutinePlan(input.PrepSuggestion) ? input.PrepSuggestion : null;
            var planLine = autoPlan is null
                ? string.Empty
                : $"Önce önerilen hafta planı uygulanır ({DescribePlan(autoPlan)}). ";
            return new ContinueStep(
                KindAdvance,
                "DEVAM",
                BuildUpcomingTitle(input),
                planLine + "Takvim bir sonraki önemli olaya kadar ilerler.",
                autoPlan);
        }

        return new ContinueStep(
            KindBlocked,
            "Engeli Çöz",
            "İlerleme durdu",
            string.IsNullOrWhiteSpace(input.BlockerAdvice)
                ? "Takvimi durduran konuyu Gündem'de çöz."
                : input.BlockerAdvice!,
            AutoApplyPlan: null);
    }

    /// <summary>
    /// Sıradaki maç için gün sayacı metni ("Bugün", "Yarın", "3 gün sonra").
    /// </summary>
    public static string FormatCountdown(int daysUntil) => daysUntil switch
    {
        <= 0 => "Bugün",
        1 => "Yarın",
        _ => $"{daysUntil} gün sonra",
    };

    private static bool IsRoutinePlan(PrepPlanSuggestion? suggestion) =>
        suggestion is not null
        && (string.Equals(suggestion.ActionCode, PrepPlanSuggestion.SeedWeek, StringComparison.Ordinal)
            || string.Equals(suggestion.ActionCode, PrepPlanSuggestion.ApplyRecovery, StringComparison.Ordinal));

    private static string DescribePlan(PrepPlanSuggestion suggestion) =>
        string.Equals(suggestion.ActionCode, PrepPlanSuggestion.ApplyRecovery, StringComparison.Ordinal)
            ? "toparlanma"
            : "orta yoğunluk, dengeli";

    private static string BuildUpcomingTitle(ContinueFlowInput input)
    {
        if (string.IsNullOrWhiteSpace(input.NextOpponentName) || input.DaysToNextMatch is not int days)
        {
            return "Sıradaki olaya ilerle";
        }

        var venue = input.NextMatchIsHome ? "iç saha" : "deplasman";
        return $"Sıradaki maç: {input.NextOpponentName} ({venue}) · {FormatCountdown(days)}";
    }
}

public sealed record ContinueFlowInput(
    bool HasOpenDecision,
    bool HasDueContinentalMatch,
    bool HasDueManagedMatch,
    bool DueMatchApproved,
    bool SeasonTransitionReady,
    bool SeasonArchivePhase,
    bool CanAdvance,
    string? BlockerAdvice,
    PrepPlanSuggestion? PrepSuggestion,
    string? NextOpponentName,
    bool NextMatchIsHome,
    int? DaysToNextMatch);

public sealed record ContinueStep(
    string Kind,
    string ButtonLabel,
    string PreviewTitle,
    string PreviewBody,
    PrepPlanSuggestion? AutoApplyPlan);
