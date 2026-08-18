namespace FootballCareerSimulator.Application.Interaction.Queries;

/// <summary>
/// Bugün sayfası "Masada" özeti — bekleyen karar/basın aciliyeti.
/// </summary>
public sealed record DecisionDeskDigest(
    bool HasOpenDecision,
    bool IsHardBlocker,
    string BrandTitle,
    string Headline,
    string SupportingLine,
    long? DecisionRequestId,
    string? KindName,
    int OpenCount,
    string? CausalityLine = null)
{
    /// <summary>Yönetim talebi sentinel oyuncusu — UI'da göstermeyiz.</summary>
    private const long BoardDemandSentinelPlayerId = 9_000_000_001L;

    public static DecisionDeskDigest Clear() =>
        new(
            HasOpenDecision: false,
            IsHardBlocker: false,
            BrandTitle: "Masada",
            Headline: "Masada bekleyen yok — günün işine bak.",
            SupportingLine: string.Empty,
            DecisionRequestId: null,
            KindName: null,
            OpenCount: 0);

    public static DecisionDeskDigest Compose(
        PendingDecisionsReadModel pending,
        int currentDayNumber,
        string? causalityLine = null)
    {
        ArgumentNullException.ThrowIfNull(pending);
        if (pending.OpenCount == 0 || pending.OpenRequests.Count == 0)
        {
            return Clear();
        }

        var first = pending.OpenRequests[0];
        var daysLeft = first.DeadlineDayNumber - currentDayNumber;
        var urgency = first.IsHardBlocker
            ? "ZORUNLU — zaman ilerlemez."
            : daysLeft <= 0
                ? "Son gün."
                : daysLeft == 1
                    ? "Yarın son."
                    : $"{daysLeft} gün kaldı.";

        var headline = HeadlineFor(first.KindName, first.IsHardBlocker, causalityLine);
        var support = ShowSubjectPlayer(first.SubjectPlayerId)
            ? $"{first.KindName} · oyuncu#{first.SubjectPlayerId} · {urgency}"
            : $"{first.KindName} · {urgency}";

        if (pending.OpenCount > 1)
        {
            support += $" · +{pending.OpenCount - 1} kuyrukta";
        }

        if (!string.IsNullOrWhiteSpace(causalityLine))
        {
            support += $" · {causalityLine.Trim()}";
        }

        return new DecisionDeskDigest(
            HasOpenDecision: true,
            first.IsHardBlocker,
            first.IsHardBlocker ? "Masada (zorunlu)" : "Masada",
            headline,
            support,
            first.DecisionRequestId,
            first.KindName,
            pending.OpenCount,
            string.IsNullOrWhiteSpace(causalityLine) ? null : causalityLine.Trim());
    }

    private static bool ShowSubjectPlayer(long subjectPlayerId) =>
        subjectPlayerId > 0 && subjectPlayerId != BoardDemandSentinelPlayerId;

    private static string HeadlineFor(string kindName, bool hard, string? causalityLine)
    {
        if (!string.IsNullOrWhiteSpace(causalityLine)
            && kindName.Contains("Transfer", StringComparison.OrdinalIgnoreCase)
            && causalityLine.Contains("Söz", StringComparison.OrdinalIgnoreCase))
        {
            return "Söz kırıldı — oyuncu ayrılmak istiyor.";
        }

        if (kindName.Contains("basın", StringComparison.OrdinalIgnoreCase))
        {
            return hard
                ? "Basın kapıda — cevap vermeden ilerleyemezsin."
                : "Basın sorusu masada.";
        }

        if (kindName.Contains("Yönetim", StringComparison.OrdinalIgnoreCase))
        {
            return "Yönetim masaya oturdu.";
        }

        if (kindName.Contains("Disiplin", StringComparison.OrdinalIgnoreCase))
        {
            return "Soyunma odasında gerilim.";
        }

        if (kindName.Contains("Transfer", StringComparison.OrdinalIgnoreCase))
        {
            return "Oyuncu transfer istiyor.";
        }

        if (kindName.Contains("İlk 11", StringComparison.OrdinalIgnoreCase))
        {
            return causalityLine is not null && causalityLine.Contains("bozuldu", StringComparison.OrdinalIgnoreCase)
                ? "İlk 11 sözü bozuldu — kriz masada."
                : "İlk 11 sözü/talebi masada.";
        }

        if (kindName.Contains("süre", StringComparison.OrdinalIgnoreCase)
            || kindName.Contains("Forma", StringComparison.OrdinalIgnoreCase))
        {
            if (causalityLine is not null
                && causalityLine.Contains("bozuldu", StringComparison.OrdinalIgnoreCase))
            {
                return "Forma sözü bozuldu — yeni talep masada.";
            }

            if (causalityLine is not null
                && (causalityLine.Contains("yedek", StringComparison.OrdinalIgnoreCase)
                    || causalityLine.Contains("kadro dışı", StringComparison.OrdinalIgnoreCase)))
            {
                return "Yedek kaldı — forma süresi istiyor.";
            }

            return "Forma süresi talebi bekliyor.";
        }

        return hard
            ? "Zorunlu karar seni bekliyor."
            : "Bir karar masada.";
    }

    public string ToDisplayText()
    {
        if (!HasOpenDecision)
        {
            return $"{BrandTitle}\n{Headline}";
        }

        return $"{BrandTitle}\n{Headline}\n{SupportingLine}";
    }
}
