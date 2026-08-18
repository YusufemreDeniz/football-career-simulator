using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Kayıt yükleme sonrası — "nerede kaldım?" nabız doğrulaması.
/// </summary>
public sealed record CareerResumeDigest(
    string BrandTitle,
    string Headline,
    string AdviceLine,
    string PulseFocusCode,
    IReadOnlyList<string> BeatLines,
    string? NextCtaLabel = null)
{
    public const string Brand = "Kariyere Dönüş";

    public static CareerResumeDigest Compose(
        TodayPulseDigest pulse,
        int dayNumber,
        string managerDisplayName,
        string? clubDisplayName,
        bool wasMigrated,
        WeekStoryDigest? weekStory = null,
        OfficeNextStep? nextStep = null)
    {
        ArgumentNullException.ThrowIfNull(pulse);
        ArgumentException.ThrowIfNullOrWhiteSpace(managerDisplayName);
        weekStory ??= WeekStoryDigest.Clear();

        var beats = new List<string>
        {
            clubDisplayName is null
                ? $"{managerDisplayName} · kulüpsüz · {GameDate.ToDisplayDateString(dayNumber)}"
                : $"{managerDisplayName} · {clubDisplayName} · {GameDate.ToDisplayDateString(dayNumber)}",
        };

        if (wasMigrated)
        {
            beats.Add("Kayıt şeması güncellendi — içerik aynı kariyer.");
        }

        if (weekStory.IsActive)
        {
            beats.Add(weekStory.ToPulseLine());
        }

        beats.Add($"Nabız: {pulse.Headline}");
        foreach (var line in pulse.PulseLines.Take(1))
        {
            if (weekStory.IsActive
                && line.StartsWith("Hikâye:", StringComparison.Ordinal))
            {
                continue;
            }

            beats.Add(line);
        }

        var advice = ResolveAdvice(pulse.PrimaryFocusCode, weekStory, nextStep);
        var headline = ResolveHeadline(pulse, weekStory);

        return new CareerResumeDigest(
            Brand,
            headline,
            advice,
            pulse.PrimaryFocusCode,
            beats.Take(4).ToArray(),
            nextStep?.ButtonLabel);
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

    /// <summary>
    /// Alt nabız şeridi: yalnız "neredeyim" + "sıradaki iş". Ayrıntı Bugün kartlarında kalır.
    /// </summary>
    public string ToStatusMessage()
    {
        var follow = string.IsNullOrWhiteSpace(NextCtaLabel)
            ? AdviceLine
            : $"Sıradaki: {NextCtaLabel}";
        return string.IsNullOrWhiteSpace(follow)
            ? $"{BrandTitle} · {Headline}"
            : $"{BrandTitle} · {Headline}\n{follow}";
    }

    private static string ResolveHeadline(TodayPulseDigest pulse, WeekStoryDigest weekStory)
    {
        if (weekStory.IsActive)
        {
            return $"Tekrar ofistesin — {weekStory.StoryLine}";
        }

        return pulse.PrimaryFocusCode == TodayPulseDigest.FocusCalm
            ? "Tekrar ofistesin — nabız sakin, sen karar ver."
            : $"Tekrar ofistesin — önce: {pulse.Headline}";
    }

    private static string ResolveAdvice(
        string focusCode,
        WeekStoryDigest weekStory,
        OfficeNextStep? nextStep)
    {
        if (weekStory.IsActive && nextStep is not null)
        {
            return $"Birincil düğme: {nextStep.ButtonLabel} — hikâyeyi sürdür.";
        }

        if (weekStory.IsActive)
        {
            return "Haftanın Hikâyesi Bugün'de — nabza bak, sıradaki adımı seç.";
        }

        return focusCode switch
        {
            TodayPulseDigest.FocusDesk => "Önce Masada — Bugün'den dosyaya bak.",
            TodayPulseDigest.FocusMatch => "Sıradaki Maç / Bugün — XI ve düdük.",
            TodayPulseDigest.FocusSquad => "Kulüp'te Yer Aç veya Taşanı Kadroya Al.",
            TodayPulseDigest.FocusTransfer => "Birincil düğmeyle satış / pencere / süreci uygula.",
            TodayPulseDigest.FocusSeason => "Bugün'de sezon geçişini tamamla.",
            TodayPulseDigest.FocusPrep => "Hazırlık'ta önerilen planı uygula.",
            TodayPulseDigest.FocusLeague => "Lig baskısına göre sıradaki adımı uygula.",
            _ => "Bugün nabzını oku — sonra günü ilerlet.",
        };
    }
}
