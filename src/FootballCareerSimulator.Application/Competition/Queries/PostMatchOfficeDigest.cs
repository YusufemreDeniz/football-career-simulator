using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;

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
    /// Toparlanma CTA sonrası kısa onay — prep bitti, sıradaki maç/sakin nabız.
    /// </summary>
    public static PostMatchOfficeDigest AfterRecoveryApplied(
        IReadOnlyList<string>? injuredPlayerNames = null,
        bool hasDueUnapprovedMatch = false,
        bool hasDuePlayableMatch = false,
        bool hasInjuryPressure = false)
    {
        var names = injuredPlayerNames ?? Array.Empty<string>();
        var beats = new List<string>
        {
            names.Count > 0
                ? "Sakat: " + string.Join(", ", names.Take(3))
                : "Aktif sakat yok — plan yine de yumuşak.",
            "Plan: Toparlanma / Hafif / Bol dinlenme",
        };

        string nextFocus;
        string advice;
        if (hasDueUnapprovedMatch)
        {
            nextFocus = TodayPulseDigest.FocusMatch;
            var cta = hasInjuryPressure ? "Sakatsız Kadro Onayla" : "Kadro Onayla";
            beats.Add($"Sıradaki: {cta}");
            advice = $"Hazırlık oturdu — şimdi {cta}.";
        }
        else if (hasDuePlayableMatch)
        {
            nextFocus = TodayPulseDigest.FocusMatch;
            var cta = hasInjuryPressure ? "Maç Günü — XI Kontrol" : "Maç Gününe Git";
            beats.Add($"Sıradaki: {cta}");
            advice = $"Hazırlık oturdu — şimdi {cta}.";
        }
        else
        {
            nextFocus = TodayPulseDigest.FocusCalm;
            beats.Add("Sıradaki: nabız sakin — 1 gün ilerlet");
            advice = "Hazırlık oturdu — nabız sakin, günü ilerlet.";
        }

        return new(
            Brand,
            "Toparlanma işledi — sakatlar listede.",
            advice,
            nextFocus,
            beats);
    }

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
        TodayPulseDigest? nextPulse = null,
        string? halfTimeNoteLine = null)
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
        var halfTimeNote = !string.IsNullOrWhiteSpace(halfTimeNoteLine)
            ? halfTimeNoteLine
            : ExtractHalfTimeNote(narrative);
        if (!string.IsNullOrWhiteSpace(halfTimeNote))
        {
            beats.Add(halfTimeNote);
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
        var recoveryCta = PrepPlanSuggestion.RecoveryPlan().ButtonLabel;
        if (hasInjury)
        {
            focusCode = TodayPulseDigest.FocusPrep;
            beats.Add($"Sıradaki: {recoveryCta}");
            advice = ComposeInjuryRecoveryAdvice(halfTimeNote, recoveryCta);
        }
        else if (nextPulse is not null)
        {
            focusCode = nextPulse.PrimaryFocusCode;
            advice = AdviceForFocus(nextPulse.PrimaryFocusCode);
            if (!string.IsNullOrWhiteSpace(halfTimeNote)
                && string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusCalm, StringComparison.Ordinal))
            {
                advice = "Gece kararını hatırla — nabız sakinse günü ilerlet.";
            }

            if (!string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusCalm, StringComparison.Ordinal))
            {
                beats.Add($"Sıradaki: {nextPulse.Headline}");
            }
        }

        var headline = ResolveHeadline(narrative, desk, nextPulse);
        if (hasInjury
            && !string.IsNullOrWhiteSpace(halfTimeNote)
            && halfTimeNote.Contains("Hücuma", StringComparison.OrdinalIgnoreCase))
        {
            headline = "Hücum riski pahalıya patladı — sakatlık var.";
        }
        else if (hasInjury)
        {
            headline = "Sakatlık var — Toparlanma sırada.";
        }

        return new PostMatchOfficeDigest(Brand, headline, advice, focusCode, beats.Take(6).ToArray());
    }

    private static string ComposeInjuryRecoveryAdvice(string? halfTimeNote, string recoveryCta)
    {
        if (string.IsNullOrWhiteSpace(halfTimeNote))
        {
            return $"Hazırlık Masası — birincil düğmeyle {recoveryCta}.";
        }

        const string prefix = "Devre arası: ";
        var noteCore = halfTimeNote.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? halfTimeNote[prefix.Length..].Trim()
            : halfTimeNote.Trim();
        return $"{noteCore} — şimdi {recoveryCta}.";
    }

    private static bool HasInjuryNight(MatchNightNarrative narrative) =>
        narrative.AfterWhistleLines.Any(line =>
            line.Contains("Sakatlık", StringComparison.OrdinalIgnoreCase))
        || narrative.BeatLines.Any(line =>
            line.Contains("sakatlık", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Raporla aynı dil — tek satır HT hatırlatması.
    /// </summary>
    private static string? ExtractHalfTimeNote(MatchNightNarrative narrative)
    {
        var lines = narrative.KickoffLines.Concat(narrative.AfterWhistleLines).ToArray();
        var decision = lines.FirstOrDefault(line =>
            MatchHalfTimeDigest.FormatDecisionKeyMoment(line) is not null);
        var substitution = lines.FirstOrDefault(line =>
            line.Contains('↔', StringComparison.Ordinal));
        return MatchReportDigest.ComposeHalfTimeNote(decision, substitution);
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
