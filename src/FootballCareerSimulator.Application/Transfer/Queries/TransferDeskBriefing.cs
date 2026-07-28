namespace FootballCareerSimulator.Application.Transfer.Queries;

/// <summary>
/// Transfer sayfası brifingi — pencere, ihtiyaç, süreç ve satış çıkışı tek bakışta.
/// </summary>
public sealed record TransferDeskBriefing(
    bool IsEmployed,
    string BrandTitle,
    string Headline,
    string AdviceLine,
    bool DemandsAttention,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Transfer Masası";

    public static TransferDeskBriefing Unemployed() =>
        new(
            IsEmployed: false,
            Brand,
            "Kulüp yok — transfer masası kapalı.",
            "Önce işe dön; sonra pencere ve satış burada açılır.",
            DemandsAttention: false,
            Array.Empty<string>());

    public static TransferDeskBriefing Compose(
        bool windowOpen,
        string windowStatusName,
        int? windowClosesOnDayNumber,
        int openNeedCount,
        int openExitNeedCount,
        int listedTargetCount,
        int activeProcessCount,
        int pendingOfferCount,
        int? budgetAvailable,
        int? budgetSpent,
        bool squadFull,
        long? saleCandidatePlayerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowStatusName);

        var beats = new List<string>
        {
            windowOpen
                ? windowClosesOnDayNumber is int close
                    ? $"Pencere açık · kapanış gün {close}"
                    : "Pencere açık"
                : $"Pencere kapalı ({windowStatusName})",
        };

        if (budgetAvailable is int available)
        {
            var spent = budgetSpent is int s ? $" · harcanan {s:N0}" : string.Empty;
            beats.Add($"Bütçe: kullanılabilir {available:N0}{spent}");
        }

        beats.Add(
            openNeedCount == 0
                ? "Açık ihtiyaç yok"
                : $"Açık ihtiyaç {openNeedCount}"
                  + (openExitNeedCount > 0 ? $" · ayrılma {openExitNeedCount}" : string.Empty));

        beats.Add($"Hedef {listedTargetCount} · aktif süreç {activeProcessCount}"
            + (pendingOfferCount > 0 ? $" · bekleyen teklif {pendingOfferCount}" : string.Empty));

        if (saleCandidatePlayerId is long saleId)
        {
            beats.Add(
                windowOpen
                    ? $"Satış adayı: #{saleId}"
                    : $"Satış adayı: #{saleId} (pencere kapalı)");
        }

        if (squadFull)
        {
            beats.Add("Kadro dolu — yer için Satışa Çıkar veya Yer Aç.");
        }

        var (headline, advice) = ResolveFocus(
            windowOpen,
            openNeedCount,
            openExitNeedCount,
            listedTargetCount,
            activeProcessCount,
            pendingOfferCount,
            squadFull,
            saleCandidatePlayerId);

        var demands =
            pendingOfferCount > 0
            || activeProcessCount > 0
            || openExitNeedCount > 0
            || (listedTargetCount > 0 && activeProcessCount == 0)
            || (squadFull && saleCandidatePlayerId is not null)
            || (!windowOpen && (squadFull || saleCandidatePlayerId is not null));

        return new TransferDeskBriefing(
            true,
            Brand,
            headline,
            advice,
            demands,
            beats.Take(6).ToArray());
    }

    public string ToDisplayText()
    {
        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        var advice = string.IsNullOrWhiteSpace(AdviceLine)
            ? string.Empty
            : $"\nÖneri: {AdviceLine}";
        return $"{BrandTitle}\n{Headline}{beats}{advice}";
    }

    private static (string Headline, string Advice) ResolveFocus(
        bool windowOpen,
        int openNeedCount,
        int openExitNeedCount,
        int listedTargetCount,
        int activeProcessCount,
        int pendingOfferCount,
        bool squadFull,
        long? saleCandidatePlayerId)
    {
        if (!windowOpen)
        {
            return (
                "Pencere kapalı — satış ve yeni süreç bekler.",
                saleCandidatePlayerId is not null || squadFull
                    ? "Pencere Aç, sonra Satışa Çıkar; acil yer için Yer Aç."
                    : "Pencere Aç; ardından ihtiyaç tara veya hedef öner.");
        }

        if (pendingOfferCount > 0)
        {
            return (
                "Masada bekleyen kulüp teklifi var.",
                "Teklifleri kabul / ret / karşı — süreci ilerlet.");
        }

        if (activeProcessCount > 0)
        {
            return (
                $"Aktif süreç var ({activeProcessCount}).",
                "Sportif / mali onay veya teklif adımlarını tamamla.");
        }

        if (openExitNeedCount > 0 && saleCandidatePlayerId is long exitId)
        {
            return (
                $"Ayrılma listesi açık — #{exitId} satılabilir.",
                "Satışa Çıkar ile AI alıcıya tamamla.");
        }

        if (squadFull && saleCandidatePlayerId is long fullId)
        {
            return (
                "Kadro dolu — çıkış masada.",
                $"Satışa Çıkar (#{fullId}) veya Yer Aç ile slot aç.");
        }

        if (listedTargetCount > 0 && activeProcessCount == 0)
        {
            return (
                "Hedef listede — süreç açılmayı bekliyor.",
                "Süreç Aç ile müzakereyi başlat.");
        }

        if (openNeedCount == 0)
        {
            return (
                "Pencere açık — masa sakin.",
                "İhtiyaç Tara veya Hedef Öner; kenar oyuncu için Satışa Çıkar.");
        }

        if (listedTargetCount == 0)
        {
            return (
                "İhtiyaç var — hedef yok.",
                "Hedef Öner ile shortlist doldur.");
        }

        return (
            "Transfer masası hareketli.",
            "Süreç Aç veya teklif adımlarına bak.");
    }
}
