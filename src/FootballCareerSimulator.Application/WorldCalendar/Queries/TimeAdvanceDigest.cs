using FootballCareerSimulator.Application.WorldCalendar.Commands;

namespace FootballCareerSimulator.Application.WorldCalendar.Queries;

/// <summary>
/// Zaman ilerletme özeti — oyuncuya "bu süre ne oldu?" diye okunur.
/// </summary>
public sealed record TimeAdvanceDigest(
    string BrandTitle,
    string Headline,
    string SpanLine,
    IReadOnlyList<string> BeatLines)
{
    public string ToStatusMessage()
    {
        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        var header = string.IsNullOrWhiteSpace(SpanLine)
            ? BrandTitle
            : $"{BrandTitle} · {SpanLine}";
        return $"{header}\n{Headline}{beats}";
    }

    public static TimeAdvanceDigest Blocked(string reason) =>
        new("İlerleme", reason, string.Empty, Array.Empty<string>());

    public static TimeAdvanceDigest Compose(
        AdvanceSimulationTimeResult result,
        int requestedDayCount,
        string? nextMatchHint = null)
    {
        var brand = requestedDayCount >= 7 ? "Hafta Özeti" : "Gün Özeti";
        var from = Domain.WorldCalendar.GameDate.ToDisplayDateString(result.PreviousDayNumber);
        var to = Domain.WorldCalendar.GameDate.ToDisplayDateString(result.NewDayNumber);
        var span = string.Equals(from, to, StringComparison.Ordinal)
            ? from
            : $"{from} → {to}";
        var beats = new List<string>();

        if (result.TransferWindowsClosedBySchedule > 0)
        {
            beats.Add("Transfer penceresi takvimle kapandı.");
        }

        if (result.ExpiredContractCount > 0)
        {
            beats.Add(
                $"{result.ExpiredContractCount} sözleşme bitti"
                + (result.NewlyFreeAgentPlayerIds.Count > 0
                    ? $" · {result.NewlyFreeAgentPlayerIds.Count} serbest."
                    : "."));
        }

        if (result.PromiseBrokenCrisisOpenedCount > 0)
        {
            beats.Add(
                $"{result.PromiseBrokenCrisisOpenedCount} söz ihlali — kriz kararı açıldı.");
        }
        else if (result.PromiseDeadlineResolvedCount > 0)
        {
            beats.Add($"{result.PromiseDeadlineResolvedCount} söz sonuçlandı.");
        }

        if (result.DecisionsExpiredCount > 0)
        {
            beats.Add($"{result.DecisionsExpiredCount} karar süresi doldu.");
        }

        if (result.PlayersAgedCount > 0)
        {
            beats.Add($"{result.PlayersAgedCount} oyuncu yaşlanma düşüşü aldı.");
        }

        if (result.MemoriesDecayedCount > 0)
        {
            beats.Add("Bazı hafızalar zayıfladı.");
        }

        if (!string.IsNullOrWhiteSpace(nextMatchHint))
        {
            beats.Add(nextMatchHint);
        }

        var headline = ResolveHeadline(result, beats.Count, requestedDayCount);
        return new TimeAdvanceDigest(brand, headline, span, beats.Take(5).ToArray());
    }

    private static string ResolveHeadline(
        AdvanceSimulationTimeResult result,
        int beatCount,
        int requestedDayCount)
    {
        if (result.PromiseBrokenCrisisOpenedCount > 0)
        {
            return "Ofiste fırtına — sözler bozuldu.";
        }

        if (result.TransferWindowsClosedBySchedule > 0)
        {
            return "Pencere kapandı; pazar susuyor.";
        }

        if (result.ExpiredContractCount > 0)
        {
            return "Sözleşme masası hareketlendi.";
        }

        if (result.DecisionsExpiredCount > 0)
        {
            return "Bekleyen kararlar zamana yenildi.";
        }

        if (beatCount == 0)
        {
            return requestedDayCount >= 7
                ? "Sakin bir hafta — sahaya odaklan."
                : "Sakin bir gün.";
        }

        return requestedDayCount >= 7
            ? "Hafta dolu geçti."
            : "Gün iz bıraktı.";
    }
}
