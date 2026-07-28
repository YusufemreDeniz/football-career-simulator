namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Dosya sayfası — kayıt masası: kariyerini nereye bıraktın, ne kaydediyorsun?
/// </summary>
public sealed record SaveDeskDigest(
    string BrandTitle,
    string Headline,
    string AdviceLine,
    bool SaveExists,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Kayıt Masası";

    public static SaveDeskDigest Compose(
        string savePath,
        bool saveExists,
        DateTimeOffset? saveLastWriteUtc,
        int currentDayNumber,
        string currentIsoDate,
        string managerDisplayName,
        string? clubDisplayName,
        long? seasonId,
        string? seasonStatus,
        int acceptedFixtureCount,
        int totalFixtureCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(savePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentIsoDate);
        ArgumentException.ThrowIfNullOrWhiteSpace(managerDisplayName);

        var beats = new List<string>
        {
            $"Şimdi: gün {currentDayNumber} ({currentIsoDate})",
            clubDisplayName is null
                ? $"Menajer: {managerDisplayName} · işsiz"
                : $"Menajer: {managerDisplayName} · {clubDisplayName}",
        };

        if (seasonId is long sid)
        {
            var status = string.IsNullOrWhiteSpace(seasonStatus) ? "—" : seasonStatus;
            beats.Add($"Sezon #{sid} ({TranslateStatus(status)}) · maç {acceptedFixtureCount}/{totalFixtureCount}");
        }
        else
        {
            beats.Add("Aktif lig sezonu yok.");
        }

        if (saveExists)
        {
            var when = saveLastWriteUtc is DateTimeOffset t
                ? t.ToLocalTime().ToString("g")
                : "bilinmiyor";
            beats.Add($"Diskteki kayıt: {when}");
            beats.Add($"Dosya: {savePath}");
        }
        else
        {
            beats.Add("Diskte kayıt yok — ilk kaydı sen oluşturursun.");
            beats.Add($"Hedef: {savePath}");
        }

        var headline = saveExists
            ? "Kayıt mevcut — üzerine yazabilir veya yükleyebilirsin."
            : "Henüz kayıt yok — kariyeri diske bırak.";

        var advice = saveExists
            ? "Önemli maç/ilerleme öncesi Kaydet; Yükle mevcut oturumu değiştirir."
            : "İlk Kaydet ile devam noktası aç — Ana Menü'den de Devam Et çalışır.";

        return new SaveDeskDigest(Brand, headline, advice, saveExists, beats);
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

    private static string TranslateStatus(string status) => status switch
    {
        "Preseason" => "Hazırlık",
        "Active" => "Aktif",
        "Completed" => "Tamamlandı",
        "Archived" => "Arşiv",
        _ => status,
    };
}
