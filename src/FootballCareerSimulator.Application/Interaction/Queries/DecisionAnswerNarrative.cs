using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Application.Interaction.Queries;

/// <summary>
/// Masada cevap sonrası anlatı — oyuncu "ne yaptım, ne oldu?" diye okusun.
/// </summary>
public sealed record DecisionAnswerNarrative(
    string BrandTitle,
    string Headline,
    string ChoiceLine,
    IReadOnlyList<string> BeatLines)
{
    /// <summary>Yönetim talebi sentinel oyuncusu — UI'da göstermeyiz.</summary>
    private const long BoardDemandSentinelPlayerId = 9_000_000_001L;

    public string ToStatusMessage()
    {
        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        return $"{BrandTitle}\n{Headline}\n{ChoiceLine}{beats}";
    }

    public static DecisionAnswerNarrative Compose(
        string kindName,
        string optionCode,
        string optionDisplayText,
        long subjectPlayerId,
        bool wasHardBlocker,
        int remainingOpenCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kindName);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionDisplayText);

        var headline = HeadlineFor(kindName, optionCode);
        var choice = $"Seçimin: {optionDisplayText}";
        var beats = new List<string>();

        if (ShowSubjectPlayer(subjectPlayerId))
        {
            beats.Add($"Konu: oyuncu#{subjectPlayerId}");
        }

        if (wasHardBlocker)
        {
            beats.Add("Zorunlu engel kalktı — zaman yine akabilir.");
        }

        var consequence = ConsequenceBeat(optionCode);
        if (!string.IsNullOrWhiteSpace(consequence))
        {
            beats.Add(consequence);
        }

        if (remainingOpenCount > 0)
        {
            beats.Add(
                remainingOpenCount == 1
                    ? "Masada hâlâ 1 dosya var."
                    : $"Masada hâlâ {remainingOpenCount} dosya var.");
        }
        else
        {
            beats.Add("Masada bekleyen kalmadı.");
        }

        return new DecisionAnswerNarrative("Masada", headline, choice, beats);
    }

    private static bool ShowSubjectPlayer(long subjectPlayerId) =>
        subjectPlayerId > 0 && subjectPlayerId != BoardDemandSentinelPlayerId;

    private static string HeadlineFor(string kindName, string optionCode)
    {
        if (IsRefuse(optionCode))
        {
            if (kindName.Contains("basın", StringComparison.OrdinalIgnoreCase)
                || kindName.Contains("Yönetim", StringComparison.OrdinalIgnoreCase))
            {
                return "Sert bir hayır — sonuçları zamanla gelir.";
            }

            return "Reddettin — gerilim soğumadı.";
        }

        return optionCode switch
        {
            DecisionRequest.OptionGrantPlayingTimePromise =>
                "Söz verdin — forma süresi hesabı başladı.",
            DecisionRequest.OptionGrantStartingOpportunityPromise =>
                "İlk 11 sözü verdin — tutman lazım.",
            DecisionRequest.OptionAcknowledgeTransferRequest =>
                "Transfer isteği kayda geçti.",
            DecisionRequest.OptionIssueWarning =>
                "Uyarı masaya kondu.",
            DecisionRequest.OptionIssueFine =>
                "Ceza kesildi — soyunma odası sessizleşti.",
            DecisionRequest.OptionOfferSupport =>
                "Arkasında durdun; güven biraz toparlandı.",
            DecisionRequest.OptionAcceptBoardDemand =>
                "Yönetime uyum gösterdin.",
            DecisionRequest.OptionCounterBoardDemand =>
                "Masada pazarlık açıldı.",
            DecisionRequest.OptionPubliclyDefend =>
                "Kamuya savundun — basın bunu duyacak.",
            DecisionRequest.OptionPubliclyCriticize =>
                "Kamuya eleştirdin — manşet senin aleyhine dönebilir.",
            _ => kindName.Contains("basın", StringComparison.OrdinalIgnoreCase)
                ? "Basın sorusunu kapattın."
                : "Karar kapandı — ofis biraz sakinledi.",
        };
    }

    private static string? ConsequenceBeat(string optionCode) =>
        optionCode switch
        {
            DecisionRequest.OptionGrantPlayingTimePromise =>
                "Oyuncu sözü hafızasına yazdı.",
            DecisionRequest.OptionGrantStartingOpportunityPromise =>
                "İlk 11 sözü aktif; kadro seçimleri izlenecek.",
            DecisionRequest.OptionAcknowledgeTransferRequest =>
                "Kulüp transfer ihtiyacı olarak işaretlendi.",
            DecisionRequest.OptionIssueWarning =>
                "Disiplin kaydı: uyarı.",
            DecisionRequest.OptionIssueFine =>
                "Disiplin kaydı: ceza.",
            DecisionRequest.OptionOfferSupport =>
                "Destek jesti ilişkide iz bıraktı.",
            DecisionRequest.OptionAcceptBoardDemand =>
                "Yönetim beklentisi güncellendi.",
            DecisionRequest.OptionCounterBoardDemand =>
                "Yönetim cevabını bekliyor.",
            DecisionRequest.OptionPubliclyDefend =>
                "Savunma kamuoyuna yansıdı.",
            DecisionRequest.OptionPubliclyCriticize =>
                "Eleştiri kamuoyuna yansıdı.",
            DecisionRequest.OptionRefuse =>
                "Red, ilişki ve hafızaya işlendi.",
            _ => null,
        };

    private static bool IsRefuse(string optionCode) =>
        string.Equals(optionCode, DecisionRequest.OptionRefuse, StringComparison.Ordinal);
}
