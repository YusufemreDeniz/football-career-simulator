using FootballCareerSimulator.Domain.WorldCalendar;

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
    IReadOnlyList<string> BeatLines,
    TransferNextStep? NextStep = null,
    bool WindowOpen = false,
    string WindowStatusName = "",
    int? WindowClosesOnDayNumber = null,
    int? DaysUntilClose = null,
    int? DaysSinceClose = null)
{
    public const string Brand = "Transfer Masası";

    /// <summary>Kapanış yaklaşınca nabız/CTA baskısı (gün).</summary>
    public const int ClosingPressureDays = 7;

    /// <summary>Son gün uyarısı eşiği.</summary>
    public const int ClosingCriticalDays = 3;

    /// <summary>
    /// Pencere ritmi — ofis nabzına taşınan kısa satır: kapanış baskısı, açık ritim,
    /// ya da yeni kapanmış pencere hükmü.
    /// </summary>
    public string? WindowRhythmLine
    {
        get
        {
            if (!IsEmployed)
            {
                return null;
            }

            if (WindowOpen)
            {
                if (DaysUntilClose is int left && left >= 0 && left <= ClosingCriticalDays)
                {
                    return left == 0
                        ? "Pencere bugün kapanıyor — işi bitir."
                        : $"Pencere {left} gün içinde kapanıyor — masaya bak.";
                }

                return "Pencere açık — transfer masası çalışıyor.";
            }

            if (DaysSinceClose is int since && since >= 0 && since <= 2)
            {
                return "Pencere kapandı — kadro bu haliyle ilerliyor.";
            }

            return null;
        }
    }

    public static TransferDeskBriefing Unemployed() =>
        new(
            IsEmployed: false,
            Brand,
            "Kulüp yok — transfer masası kapalı.",
            "Önce işe dön; sonra pencere ve satış burada açılır.",
            DemandsAttention: false,
            Array.Empty<string>(),
            NextStep: null);

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
        long? saleCandidatePlayerId,
        int? currentDayNumber = null,
        long? promiseExitPressurePlayerId = null,
        string? promiseExitPressureHint = null,
        string? saleCandidatePlayerName = null,
        string? saleCandidateDetail = null,
        string? promiseExitPlayerName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowStatusName);

        int? daysUntilClose = null;
        if (windowOpen
            && windowClosesOnDayNumber is int closesOn
            && currentDayNumber is int today)
        {
            daysUntilClose = closesOn - today;
        }

        int? daysSinceClose = null;
        if (!windowOpen
            && windowClosesOnDayNumber is int closedOn
            && currentDayNumber is int todayDate)
        {
            daysSinceClose = todayDate - closedOn;
        }

        var beats = new List<string>
        {
            ResolveWindowBeat(windowOpen, windowStatusName, windowClosesOnDayNumber, daysUntilClose),
        };

        if (promiseExitPressurePlayerId is not null)
        {
            var exitName = TransferPlayerCopy.NamedPlayer(promiseExitPlayerName);
            beats.Add(
                string.IsNullOrWhiteSpace(promiseExitPressureHint)
                    ? $"Söz baskısı: {exitName} ayrılma kararı masada"
                    : $"Söz baskısı: {exitName} — {promiseExitPressureHint.Trim()}");
        }

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

        if (saleCandidatePlayerId is not null)
        {
            var saleName = TransferPlayerCopy.NamedPlayer(saleCandidatePlayerName, saleCandidateDetail);
            beats.Add(
                windowOpen
                    ? $"Satış adayı: {saleName}"
                    : $"Satış adayı: {saleName} (pencere kapalı)");
        }

        if (squadFull)
        {
            beats.Add("Kadro dolu — yer için Kadro dosyasından Satışa Çıkar veya Yer Aç.");
        }

        var nextStep = ResolveNextStep(
            windowOpen,
            openNeedCount,
            openExitNeedCount,
            listedTargetCount,
            activeProcessCount,
            pendingOfferCount,
            squadFull,
            saleCandidatePlayerId,
            daysUntilClose,
            promiseExitPressurePlayerId,
            saleCandidatePlayerName,
            saleCandidateDetail,
            promiseExitPlayerName);

        var (headline, advice) = ResolveFocus(
            windowOpen,
            openNeedCount,
            openExitNeedCount,
            listedTargetCount,
            activeProcessCount,
            pendingOfferCount,
            squadFull,
            saleCandidatePlayerId,
            daysUntilClose,
            nextStep,
            promiseExitPressurePlayerId,
            promiseExitPressureHint,
            saleCandidatePlayerName,
            saleCandidateDetail,
            promiseExitPlayerName);

        var unfinished =
            pendingOfferCount > 0
            || activeProcessCount > 0
            || openExitNeedCount > 0
            || promiseExitPressurePlayerId is not null
            || (listedTargetCount > 0 && activeProcessCount == 0)
            || (!windowOpen && (squadFull || saleCandidatePlayerId is not null));

        var closingPressure = daysUntilClose is int left
            && left >= 0
            && left <= ClosingPressureDays
            && unfinished;

        // ScanNeeds / PickTarget: sakin masa rehberi — nabız baskısı değil.
        var softGuide = nextStep is
        {
            ReasonCode: TransferNextStep.ReasonScanNeeds or TransferNextStep.ReasonPickTarget
        };
        var demands = unfinished
            || closingPressure
            || (nextStep is not null && !softGuide);

        return new TransferDeskBriefing(
            true,
            Brand,
            headline,
            advice,
            demands,
            beats.Take(6).ToArray(),
            nextStep,
            windowOpen,
            windowStatusName,
            windowClosesOnDayNumber,
            daysUntilClose,
            daysSinceClose);
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

    public string ToSummaryText()
    {
        var advice = string.IsNullOrWhiteSpace(AdviceLine)
            ? string.Empty
            : $"\n{AdviceLine}";
        return $"{Headline}{advice}";
    }

    public string ToScoutReportText()
    {
        var beats = BeatLines.Count == 0
            ? "Scout ekibi henüz ek not düşmedi."
            : string.Join("\n", BeatLines.Select(b => "· " + b));
        return beats;
    }

    private static string ResolveWindowBeat(
        bool windowOpen,
        string windowStatusName,
        int? windowClosesOnDayNumber,
        int? daysUntilClose)
    {
        if (!windowOpen)
        {
            return $"Pencere kapalı ({windowStatusName})";
        }

        if (daysUntilClose is int left)
        {
            if (left < 0)
            {
                return windowClosesOnDayNumber is int close
                    ? $"Pencere açık · kapanış {GameDate.ToDisplayDateString(close)} (geçmiş)"
                    : "Pencere açık";
            }

            if (left == 0)
            {
                return "Pencere açık · kapanış bugün";
            }

            if (left == 1)
            {
                return "Pencere açık · kapanış yarın";
            }

            if (left <= ClosingCriticalDays)
            {
                return $"Pencere açık · kapanış {left} gün (kritik)";
            }

            if (left <= ClosingPressureDays)
            {
                return $"Pencere açık · kapanış {left} gün";
            }

            return windowClosesOnDayNumber is int closeDay
                ? $"Pencere açık · kapanış {GameDate.ToDisplayDateString(closeDay)}"
                : $"Pencere açık · kapanış {left} gün";
        }

        return windowClosesOnDayNumber is int closeOnly
            ? $"Pencere açık · kapanış {GameDate.ToDisplayDateString(closeOnly)}"
            : "Pencere açık";
    }

    private static (string Headline, string Advice) ResolveFocus(
        bool windowOpen,
        int openNeedCount,
        int openExitNeedCount,
        int listedTargetCount,
        int activeProcessCount,
        int pendingOfferCount,
        bool squadFull,
        long? saleCandidatePlayerId,
        int? daysUntilClose,
        TransferNextStep? nextStep,
        long? promiseExitPressurePlayerId,
        string? promiseExitPressureHint,
        string? saleCandidatePlayerName,
        string? saleCandidateDetail,
        string? promiseExitPlayerName)
    {
        if (promiseExitPressurePlayerId is not null
            && nextStep is { ReasonCode: TransferNextStep.ReasonPromiseExit })
        {
            var exitName = TransferPlayerCopy.NamedPlayer(promiseExitPlayerName);
            return (
                string.IsNullOrWhiteSpace(promiseExitPressureHint)
                    ? $"Söz kırılması — {exitName} ayrılma kararı masada."
                    : $"Söz kırılması — {exitName} ayrılmak istiyor.",
                "Masada Cevapla; kabul ayrılma ihtiyacı açar.");
        }

        if (nextStep is not null
            && daysUntilClose is int left
            && left >= 0
            && left <= ClosingCriticalDays
            && windowOpen)
        {
            return (
                left == 0
                    ? "Pencere bugün kapanıyor — işi bitir."
                    : $"Pencere {left} gün içinde kapanıyor.",
                nextStep.ButtonLabel + " — sonra başka işe geç.");
        }

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

        if (openExitNeedCount > 0 && saleCandidatePlayerId is not null)
        {
            var listedExit = TransferPlayerCopy.NamedPlayer(saleCandidatePlayerName, saleCandidateDetail);
            return (
                $"Ayrılma listesi açık — {listedExit} satılabilir.",
                "Satışa Çıkar ile alıcıyı tamamla.");
        }

        if (squadFull)
        {
            return (
                "Kadro dolu — satılacak oyuncuyu Kadro'dan seç.",
                "Kadro dosyasında Satışa Çıkar veya Yer Aç ile slot aç.");
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

    private static TransferNextStep? ResolveNextStep(
        bool windowOpen,
        int openNeedCount,
        int openExitNeedCount,
        int listedTargetCount,
        int activeProcessCount,
        int pendingOfferCount,
        bool squadFull,
        long? saleCandidatePlayerId,
        int? daysUntilClose,
        long? promiseExitPressurePlayerId,
        string? saleCandidatePlayerName,
        string? saleCandidateDetail,
        string? promiseExitPlayerName)
    {
        var closingSoon = daysUntilClose is int d
            && d >= 0
            && d <= ClosingPressureDays;
        var closingCritical = daysUntilClose is int c
            && c >= 0
            && c <= ClosingCriticalDays;

        if (!windowOpen && (squadFull || saleCandidatePlayerId is not null))
        {
            return TransferNextStep.OpenWindow();
        }

        if (pendingOfferCount > 0)
        {
            return TransferNextStep.AnswerOffers(closingCritical);
        }

        if (activeProcessCount > 0)
        {
            return TransferNextStep.AdvanceProcess(closingCritical);
        }

        if (promiseExitPressurePlayerId is long exitPlayerId)
        {
            return TransferNextStep.AnswerPromiseExit(exitPlayerId, promiseExitPlayerName);
        }

        if (windowOpen
            && saleCandidatePlayerId is long saleId
            && openExitNeedCount > 0)
        {
            return TransferNextStep.SellFringe(
                saleId,
                closingCritical || closingSoon,
                saleCandidatePlayerName,
                saleCandidateDetail);
        }

        if (listedTargetCount > 0 && activeProcessCount == 0)
        {
            return TransferNextStep.OpenProcess(closingCritical);
        }

        if (closingCritical && windowOpen && openNeedCount > 0)
        {
            return TransferNextStep.ClosingCheck();
        }

        if (windowOpen
            && activeProcessCount == 0
            && pendingOfferCount == 0
            && promiseExitPressurePlayerId is null)
        {
            if (openNeedCount == 0 && listedTargetCount == 0)
            {
                return TransferNextStep.ScanNeeds();
            }

            if (openNeedCount > 0 && listedTargetCount == 0)
            {
                return TransferNextStep.PickTarget();
            }
        }

        return null;
    }
}

/// <summary>
/// Transfer baskısı için Bugün birincil CTA — masaya gömülmez, aksiyona götürür.
/// </summary>
public sealed record TransferNextStep(
    string ReasonCode,
    string ButtonLabel,
    string TargetPageCode,
    string ActionCode,
    string PulseHeadline)
{
    public const string ReasonOpenWindow = "OpenWindow";
    public const string ReasonSellFringe = "SellFringe";
    public const string ReasonAnswerOffers = "AnswerOffers";
    public const string ReasonAdvanceProcess = "AdvanceProcess";
    public const string ReasonStartProcess = "StartProcess";
    public const string ReasonClosingCheck = "ClosingCheck";
    public const string ReasonPromiseExit = "PromiseExitPressure";
    public const string ReasonScanNeeds = "ScanNeeds";
    public const string ReasonPickTarget = "PickTarget";

    public const string TargetTransfer = "Transfer";
    public const string TargetClub = "Club";
    public const string TargetToday = "Today";

    public const string ActionNavigate = "Navigate";
    public const string ActionSellFringe = "SellFringe";
    public const string ActionOpenTransferWindow = "OpenTransferWindow";
    public const string ActionScanNeeds = "ScanNeeds";
    public const string ActionPickTarget = "PickTarget";
    public const string ActionStartProcess = "StartProcess";
    public const string ActionAdvanceProcess = "AdvanceProcess";
    public const string ActionAnswerOffers = "AnswerOffers";

    public static TransferNextStep OpenWindow() =>
        new(
            ReasonOpenWindow,
            "Pencere Aç",
            TargetTransfer,
            ActionOpenTransferWindow,
            "Pencere kapalı — satış için önce aç.");

    public static TransferNextStep SellFringe(
        long playerId,
        bool closingPressure,
        string? playerName = null,
        string? playerDetail = null)
    {
        _ = playerId;
        var who = TransferPlayerCopy.NamedPlayer(playerName);
        var detail = TransferPlayerCopy.NamedPlayer(playerName, playerDetail);
        return new(
            ReasonSellFringe,
            $"Satışa Çıkar — {who}",
            TargetTransfer,
            ActionSellFringe,
            closingPressure
                ? $"Pencere daralıyor — {detail} için Satışa Çıkar."
                : $"Kadro dolu — {detail} için Satışa Çıkar.");
    }

    public static TransferNextStep AnswerOffers(bool closingPressure) =>
        new(
            ReasonAnswerOffers,
            "Teklifleri Yanıtla",
            TargetTransfer,
            ActionAnswerOffers,
            closingPressure
                ? "Pencere bitiyor — bekleyen teklifleri yanıtla."
                : "Bekleyen teklif var — Transfer Masası.");

    public static TransferNextStep AdvanceProcess(bool closingPressure) =>
        new(
            ReasonAdvanceProcess,
            "Süreci İlerlet",
            TargetTransfer,
            ActionAdvanceProcess,
            closingPressure
                ? "Pencere bitiyor — aktif süreci tamamla."
                : "Aktif süreç var — Transfer Masası.");

    public static TransferNextStep OpenProcess(bool closingPressure) =>
        new(
            ReasonStartProcess,
            "Süreç Aç",
            TargetTransfer,
            ActionStartProcess,
            closingPressure
                ? "Pencere bitiyor — listedeki hedefe süreç aç."
                : "Hedef listede — Süreç Aç.");

    public static TransferNextStep ClosingCheck() =>
        new(
            ReasonClosingCheck,
            "Transfer Masası",
            TargetTransfer,
            ActionNavigate,
            "Pencere kapanmak üzere — ihtiyaçları bitir.");

    public static TransferNextStep ScanNeeds() =>
        new(
            ReasonScanNeeds,
            "İhtiyaç Tara",
            TargetTransfer,
            ActionScanNeeds,
            "Pencere açık — önce kadro ihtiyacını tara.");

    public static TransferNextStep PickTarget() =>
        new(
            ReasonPickTarget,
            "Scout'tan Hedef Seç",
            TargetTransfer,
            ActionPickTarget,
            "İhtiyaç açık — scout listesinden hedef seç.");

    public static TransferNextStep AnswerPromiseExit(long playerId, string? playerName = null)
    {
        _ = playerId;
        var who = TransferPlayerCopy.NamedPlayer(playerName);
        return new(
            ReasonPromiseExit,
            "Masada Cevapla",
            TargetToday,
            ActionNavigate,
            $"Söz kırılması — {who} ayrılma kararı masada.");
    }
}

file static class TransferPlayerCopy
{
    public static string NamedPlayer(string? name, string? detail = null)
    {
        var who = string.IsNullOrWhiteSpace(name) ? "kenar oyuncu" : name.Trim();
        return string.IsNullOrWhiteSpace(detail) ? who : detail.Trim();
    }
}
