using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;

namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Maç gecesinden ofise dönüş — gece özeti + "şimdi ne yapayım?" nabız adımı.
/// </summary>
public sealed record PostMatchOfficeDigest(
    string BrandTitle,
    string Headline,
    string AdviceLine,
    string? NextFocusCode,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Ofiste";

    public static PostMatchOfficeDigest Quiet() =>
        new(Brand, "Ofis sakin — sıradaki güne bak.", "Bugün nabzına bak.", null, Array.Empty<string>());

    /// <summary>
    /// Günlük Bugün ekranı — Ofiste metni nabızla aynı dili konuşsun.
    /// </summary>
    public static PostMatchOfficeDigest FromTodayPulse(TodayPulseDigest pulse)
    {
        ArgumentNullException.ThrowIfNull(pulse);
        return Compose(narrative: null, DecisionDeskDigest.Clear(), hasManagedMatch: false, pulse);
    }

    public static PostMatchOfficeDigest Compose(
        MatchNightNarrative? narrative,
        DecisionDeskDigest desk,
        bool hasManagedMatch,
        TodayPulseDigest? nextPulse = null)
    {
        ArgumentNullException.ThrowIfNull(desk);

        if (narrative is null || !hasManagedMatch)
        {
            if (nextPulse is not null
                && !string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusCalm, StringComparison.Ordinal))
            {
                return new PostMatchOfficeDigest(
                    Brand,
                    "Nabız konuşuyor — sıradaki adımı uygula.",
                    AdviceForFocus(nextPulse.PrimaryFocusCode),
                    nextPulse.PrimaryFocusCode,
                    new[] { $"Sıradaki: {nextPulse.Headline}" });
            }

            return Quiet();
        }

        var beats = new List<string>();
        var nightDecision = ExtractNightDecision(narrative);
        if (!string.IsNullOrWhiteSpace(nightDecision))
        {
            beats.Add(nightDecision);
        }

        foreach (var line in narrative.AfterWhistleLines.Take(4))
        {
            if (line.Contains("Devre arasında", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            beats.Add(line);
        }

        if (desk.HasOpenDecision)
        {
            beats.Add(
                desk.IsHardBlocker
                    ? $"Masada zorunlu: {desk.Headline}"
                    : $"Masada: {desk.Headline}");
        }
        else
        {
            beats.Add("Masada yeni zorunlu dosya yok.");
        }

        if (narrative.KickoffLines.Count > 0
            && narrative.KickoffLines.Any(l =>
                l.Contains("söz riski", StringComparison.OrdinalIgnoreCase)))
        {
            beats.Add("Maça söz gerilimiyle girmiştin — sonuçlar ofise yansıdı.");
        }

        var lineupBeat = narrative.LineupBridge?.ResultBridgeBeatLine();
        if (!string.IsNullOrWhiteSpace(lineupBeat))
        {
            beats.Add(lineupBeat);
        }

        var hasInjury = HasInjuryNight(narrative);
        string? focusCode = null;
        var advice = "Bugün nabzına bak — sonra günü ilerlet.";
        if (hasInjury)
        {
            focusCode = TodayPulseDigest.FocusPrep;
            advice = string.IsNullOrWhiteSpace(nightDecision)
                ? "Hazırlık Masası — sakat kadroyu toparla."
                : "Gece kararı sakatlık getirdi — Hazırlık'ta toparlan.";
        }
        else if (nextPulse is not null)
        {
            focusCode = nextPulse.PrimaryFocusCode;
            advice = AdviceForFocus(nextPulse.PrimaryFocusCode);
            if (!string.IsNullOrWhiteSpace(nightDecision)
                && string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusCalm, StringComparison.Ordinal))
            {
                advice = "Gece kararını hatırla — nabız sakinse günü ilerlet.";
            }
        }

        if (nextPulse is not null
            && !string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusCalm, StringComparison.Ordinal))
        {
            beats.Add($"Sıradaki: {nextPulse.Headline}");
        }

        var headline = ResolveHeadline(narrative, desk, nextPulse);
        if (hasInjury
            && !string.IsNullOrWhiteSpace(nightDecision)
            && nightDecision.Contains("hücuma", StringComparison.OrdinalIgnoreCase))
        {
            headline = "Hücum riski pahalıya patladı — sakatlık var.";
        }

        return new PostMatchOfficeDigest(Brand, headline, advice, focusCode, beats.Take(6).ToArray());
    }

    private static bool HasInjuryNight(MatchNightNarrative narrative) =>
        narrative.AfterWhistleLines.Any(line =>
            line.Contains("Sakatlık", StringComparison.OrdinalIgnoreCase))
        || narrative.BeatLines.Any(line =>
            line.Contains("sakatlık", StringComparison.OrdinalIgnoreCase));

    private static string? ExtractNightDecision(MatchNightNarrative narrative)
    {
        var line = narrative.KickoffLines
            .Concat(narrative.AfterWhistleLines)
            .FirstOrDefault(candidate =>
                candidate.Contains("Devre arasında", StringComparison.OrdinalIgnoreCase));
        return line is null ? null : $"Gece kararı: {line}";
    }

    public string ToStatusMessage()
    {
        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        var advice = string.IsNullOrWhiteSpace(AdviceLine)
            ? string.Empty
            : $"\nÖneri: {AdviceLine}";
        return $"{BrandTitle}\n{Headline}{beats}{advice}";
    }

    public string ToDisplayText() => ToStatusMessage();

    private static string AdviceForFocus(string focusCode) => focusCode switch
    {
        TodayPulseDigest.FocusDesk => "Önce Masada cevap ver — zaman kilitli olabilir.",
        TodayPulseDigest.FocusMatch => "Sıradaki Maç / Bugün — XI ve düdük.",
        TodayPulseDigest.FocusSquad => "Kulüp'te Yer Aç veya Taşanı Kadroya Al.",
        TodayPulseDigest.FocusTransfer => "Birincil düğmeyle satış / pencere / süreci uygula.",
        TodayPulseDigest.FocusSeason => "Sezon geçişini tamamla — Bitir / Yeni Sezon.",
        TodayPulseDigest.FocusPrep => "Birincil düğmeyle hazırlık önerisini uygula.",
        TodayPulseDigest.FocusLeague => "Lig baskısı var — birincil CTA ile devam et.",
        _ => "Bugün nabzına bak — sonra günü ilerlet.",
    };

    private static string ResolveHeadline(
        MatchNightNarrative narrative,
        DecisionDeskDigest desk,
        TodayPulseDigest? nextPulse)
    {
        if (narrative.AfterWhistleLines.Any(l =>
                l.Contains("işten çıkardı", StringComparison.OrdinalIgnoreCase)))
        {
            return "Koltuk gitti — ofis artık senin değil.";
        }

        if (desk.IsHardBlocker)
        {
            return "Ofiste kriz — cevap vermeden ilerleyemezsin.";
        }

        if (narrative.AfterWhistleLines.Any(l =>
                l.Contains("Basın sorusu", StringComparison.OrdinalIgnoreCase)))
        {
            return "Basın ofise üşüştü.";
        }

        if (narrative.AfterWhistleLines.Any(l =>
                l.Contains("Kritik", StringComparison.OrdinalIgnoreCase)
                || l.Contains("İncelemede", StringComparison.OrdinalIgnoreCase)))
        {
            return "Yönetim masası ısınıyor.";
        }

        if (narrative.AfterWhistleLines.Any(l =>
                l.Contains("Sakatlık", StringComparison.OrdinalIgnoreCase)))
        {
            return "Soyunma odası endişeli — sakatlık var.";
        }

        if (desk.HasOpenDecision)
        {
            return "Ofiste iş birikti — Masada bak.";
        }

        if (nextPulse is not null
            && string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusTransfer, StringComparison.Ordinal))
        {
            return "Gece bitti — Transfer Masası bekliyor.";
        }

        if (nextPulse is not null
            && string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusSeason, StringComparison.Ordinal))
        {
            return "Gece bitti — sezon geçişi masada.";
        }

        if (nextPulse is not null
            && string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusSquad, StringComparison.Ordinal))
        {
            return "Gece bitti — kadro kapasitesi konuşuyor.";
        }

        if (nextPulse is not null
            && string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusPrep, StringComparison.Ordinal))
        {
            return "Gece bitti — Hazırlık önerisi bekliyor.";
        }

        if (nextPulse is not null
            && string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusLeague, StringComparison.Ordinal))
        {
            return "Gece bitti — lig baskısı konuşuyor.";
        }

        if (narrative.OutcomeTone.Contains("kazandın", StringComparison.OrdinalIgnoreCase)
            || narrative.OutcomeTone.Contains("galibiyet", StringComparison.OrdinalIgnoreCase)
            || narrative.OutcomeTone.Contains("domine", StringComparison.OrdinalIgnoreCase)
            || narrative.OutcomeTone.Contains("Üç puan", StringComparison.OrdinalIgnoreCase))
        {
            return "Ofis rahatladı — gece senindi.";
        }

        return "Ofise döndün — geceyi değerlendirdin.";
    }
}
