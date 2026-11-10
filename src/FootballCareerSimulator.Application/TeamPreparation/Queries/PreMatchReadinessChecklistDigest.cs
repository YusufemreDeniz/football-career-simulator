namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

/// <summary>
/// Maç öncesi hazırlık kontrol listesi (Faz 3 Adım 10).
/// İlk 11 onayı, sakat/cezalı durumu, yorgunluk riski, taktik/antrenman planı ve aktif forma sözlerini tek bakışta doğrular.
/// </summary>
public sealed record PreMatchChecklistItem(
    string Code,
    string Label,
    bool IsReady,
    bool IsWarning,
    string DetailText);

public sealed record PreMatchReadinessChecklistDigest(
    bool HasUpcomingMatch,
    bool IsFullyReady,
    int ReadyCount,
    int TotalCount,
    string SummaryBadgeText,
    string Headline,
    IReadOnlyList<PreMatchChecklistItem> Items)
{
    public static PreMatchReadinessChecklistDigest NoMatch() =>
        new(
            HasUpcomingMatch: false,
            IsFullyReady: false,
            ReadyCount: 0,
            TotalCount: 0,
            SummaryBadgeText: "Maç Yok",
            Headline: "Yaklaşan maç bulunmuyor.",
            Items: Array.Empty<PreMatchChecklistItem>());

    public static PreMatchReadinessChecklistDigest Compose(
        bool hasUpcomingMatch,
        bool isSelectionApproved,
        int startingXiCount,
        int unavailableInXiCount,
        int fatiguedInXiCount,
        bool hasTacticPlan,
        bool hasTrainingPlan,
        int activePromiseStarterCount,
        int activePromiseBenchedCount)
    {
        if (!hasUpcomingMatch)
        {
            return NoMatch();
        }

        var items = new List<PreMatchChecklistItem>
        {
            new(
                "XI_APPROVAL",
                "İlk 11 Onayı",
                IsReady: isSelectionApproved && startingXiCount == 11,
                IsWarning: !isSelectionApproved,
                DetailText: isSelectionApproved && startingXiCount == 11
                    ? "İlk 11 onaylandı ve maça hazır."
                    : "Kadro henüz onaylanmadı (Onayla veya Akıllı Rotasyon kullan)."),
            new(
                "AVAILABILITY",
                "Sakat / Cezalı Kontrolü",
                IsReady: unavailableInXiCount == 0,
                IsWarning: unavailableInXiCount > 0,
                DetailText: unavailableInXiCount == 0
                    ? "İlk 11'de sakat veya cezalı oyuncu yok."
                    : $"İlk 11'de {unavailableInXiCount} müsait olmayan oyuncu var!"),
            new(
                "FITNESS",
                "Zindelik & Yorgunluk",
                IsReady: fatiguedInXiCount == 0,
                IsWarning: fatiguedInXiCount > 0,
                DetailText: fatiguedInXiCount == 0
                    ? "İlk 11 zindelik seviyesi ideal."
                    : $"İlk 11'de {fatiguedInXiCount} yorgun oyuncu sakatlık riski taşıyor."),
            new(
                "TACTIC_TRAINING",
                "Taktik & Antrenman Planı",
                IsReady: hasTacticPlan && hasTrainingPlan,
                IsWarning: !hasTacticPlan || !hasTrainingPlan,
                DetailText: hasTacticPlan && hasTrainingPlan
                    ? "Taktik diziliş ve haftalık antrenman programı aktif."
                    : "Taktik veya haftalık antrenman planı eksik."),
            new(
                "PROMISES",
                "Forma Sözü Dengesi",
                IsReady: activePromiseBenchedCount == 0,
                IsWarning: activePromiseBenchedCount > 0,
                DetailText: activePromiseBenchedCount == 0
                    ? (activePromiseStarterCount > 0
                        ? $"{activePromiseStarterCount} söz verilen oyuncu ilk 11'de."
                        : "Risk altında forma sözü bulunmuyor.")
                    : $"{activePromiseBenchedCount} forma sözü verilen oyuncu ilk 11 dışında!"),
        };

        var readyCount = items.Count(i => i.IsReady);
        var totalCount = items.Count;
        var fullyReady = readyCount == totalCount;
        var badge = fullyReady ? $"✓ Maça Hazır ({readyCount}/{totalCount})" : $"⚠️ Kontrol ({readyCount}/{totalCount})";
        var headline = fullyReady
            ? "Tüm maç öncesi hazırlık kriterleri tamamlandı."
            : string.Join(" · ", items.Where(i => !i.IsReady).Select(i => i.DetailText));

        return new PreMatchReadinessChecklistDigest(
            HasUpcomingMatch: true,
            IsFullyReady: fullyReady,
            ReadyCount: readyCount,
            TotalCount: totalCount,
            SummaryBadgeText: badge,
            Headline: headline,
            Items: items);
    }

    public string ToCompactText()
    {
        if (!HasUpcomingMatch)
        {
            return Headline;
        }

        var lines = Items.Select(item =>
            $"{(item.IsReady ? "✓" : "⚠️")} {item.Label}: {item.DetailText}");
        return $"{SummaryBadgeText} — {Headline}\n" + string.Join("\n", lines);
    }
}
