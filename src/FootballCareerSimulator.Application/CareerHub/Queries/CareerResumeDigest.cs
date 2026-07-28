namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Kayıt yükleme sonrası — "nerede kaldım?" nabız doğrulaması.
/// </summary>
public sealed record CareerResumeDigest(
    string BrandTitle,
    string Headline,
    string AdviceLine,
    string PulseFocusCode,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Kariyere Dönüş";

    public static CareerResumeDigest Compose(
        TodayPulseDigest pulse,
        int dayNumber,
        string isoDate,
        string managerDisplayName,
        string? clubDisplayName,
        int loadedFixtureCount,
        bool wasMigrated)
    {
        ArgumentNullException.ThrowIfNull(pulse);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoDate);
        ArgumentException.ThrowIfNullOrWhiteSpace(managerDisplayName);

        var beats = new List<string>
        {
            $"Şimdi: gün {dayNumber} ({isoDate})",
            clubDisplayName is null
                ? $"Menajer: {managerDisplayName} · işsiz"
                : $"Menajer: {managerDisplayName} · {clubDisplayName}",
            $"Kayıttaki maçlar: {loadedFixtureCount}",
        };

        if (wasMigrated)
        {
            beats.Add("Kayıt şeması güncellendi — içerik aynı kariyer.");
        }

        beats.Add($"Nabız: {pulse.Headline}");
        foreach (var line in pulse.PulseLines.Take(2))
        {
            beats.Add(line);
        }

        var advice = pulse.PrimaryFocusCode switch
        {
            TodayPulseDigest.FocusDesk => "Önce Masada — Bugün'den dosyaya bak.",
            TodayPulseDigest.FocusMatch => "Sıradaki Maç / Bugün — XI ve düdük.",
            TodayPulseDigest.FocusSquad => "Kulüp'te Yer Aç veya Taşanı Kadroya Al.",
            TodayPulseDigest.FocusTransfer => "Transfer Masası — pencere, Satışa Çıkar veya süreç.",
            TodayPulseDigest.FocusSeason => "Bugün'de sezon geçişini tamamla.",
            TodayPulseDigest.FocusPrep => "Hazırlık'ta önerilen planı uygula.",
            TodayPulseDigest.FocusLeague => "Lig Masası'na bir bak.",
            _ => "Bugün nabzını oku — sonra günü ilerlet.",
        };

        var headline = pulse.PrimaryFocusCode == TodayPulseDigest.FocusCalm
            ? "Tekrar ofistesin — nabız sakin, sen karar ver."
            : $"Tekrar ofistesin — önce: {pulse.Headline}";

        return new CareerResumeDigest(
            Brand,
            headline,
            advice,
            pulse.PrimaryFocusCode,
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

    public string ToStatusMessage() => ToDisplayText();
}
