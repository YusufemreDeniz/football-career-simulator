using FootballCareerSimulator.Domain.WorldCalendar;

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
    public DateTimeOffset? SaveLastWriteUtc { get; init; }

    public long SaveSizeBytes { get; init; }

    public string ActiveSlot { get; init; } = "career_save";

    public int WorldSeed { get; init; }

    public int RollingAutosaveCount { get; init; }

    public DateTimeOffset? LatestAutosaveWriteUtc { get; init; }

    public bool LastLoadRecoveredFromBackup { get; init; }

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
        int totalFixtureCount,
        long saveSizeBytes = 0,
        string activeSlot = "career_save",
        int worldSeed = 0,
        int rollingAutosaveCount = 0,
        DateTimeOffset? latestAutosaveWriteUtc = null,
        bool lastLoadRecoveredFromBackup = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(savePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentIsoDate);
        ArgumentException.ThrowIfNullOrWhiteSpace(managerDisplayName);

        var beats = new List<string>
        {
            $"{managerDisplayName} · {clubDisplayName ?? "kulüpsüz"} · {GameDate.ToDisplayDateString(currentDayNumber)}",
        };

        if (seasonId is not null)
        {
            var status = string.IsNullOrWhiteSpace(seasonStatus) ? "—" : seasonStatus;
            beats.Add($"Sezon: {TranslateStatus(status)} · oynanan maç {acceptedFixtureCount}/{totalFixtureCount}");
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

        var safeAutosaveCount = Math.Max(0, rollingAutosaveCount);
        if (safeAutosaveCount > 0)
        {
            var autoStamp = latestAutosaveWriteUtc is DateTimeOffset a
                ? $" ({a.ToLocalTime():g})"
                : string.Empty;
            beats.Add($"Atomik otomatik yedek: {safeAutosaveCount}/3 slot hazır{autoStamp}");
        }

        if (lastLoadRecoveredFromBackup)
        {
            beats.Add("Kurtarma: Ana kayıt bozuk olduğu için son sağlam yedekten geri yüklendi.");
        }

        var headline = lastLoadRecoveredFromBackup
            ? "Yedekten kurtarıldı — sağlam kayıt oturuma ve ana dosyaya geri yazıldı."
            : saveExists
                ? "Kayıt mevcut — üzerine yazabilir veya yükleyebilirsin."
                : "Henüz kayıt yok — kariyeri diske bırak.";

        var advice = saveExists
            ? "Önemli maç/ilerleme öncesi Kaydet; Yükle mevcut oturumu değiştirir."
            : "İlk Kaydet ile devam noktası aç — Ana Menü'den de Devam Et çalışır.";

        return new SaveDeskDigest(Brand, headline, advice, saveExists, beats)
        {
            SaveLastWriteUtc = saveLastWriteUtc,
            SaveSizeBytes = Math.Max(0, saveSizeBytes),
            ActiveSlot = string.IsNullOrWhiteSpace(activeSlot) ? "career_save" : activeSlot.Trim(),
            WorldSeed = worldSeed,
            RollingAutosaveCount = safeAutosaveCount,
            LatestAutosaveWriteUtc = latestAutosaveWriteUtc,
            LastLoadRecoveredFromBackup = lastLoadRecoveredFromBackup,
        };
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
