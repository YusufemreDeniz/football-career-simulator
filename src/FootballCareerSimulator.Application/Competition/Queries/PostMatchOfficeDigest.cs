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
    /// Sakatlık tamamen bitince — İyileşme Yolu'nun tek kapanış anı.
    /// </summary>
    public static PostMatchOfficeDigest AfterInjuriesCleared(
        IReadOnlyList<string>? recoveredPlayerNames = null,
        bool hasDueUnapprovedMatch = false,
        bool hasDuePlayableMatch = false)
    {
        var names = recoveredPlayerNames ?? Array.Empty<string>();
        var who = names.Count > 0
            ? string.Join(", ", names.Take(2))
            : "Sakatlar";
        var beats = new List<string>
        {
            $"İyileşti: {who}",
            "İyileşme Yolu kapandı",
        };

        string nextFocus;
        string advice;
        if (hasDueUnapprovedMatch)
        {
            nextFocus = TodayPulseDigest.FocusMatch;
            beats.Add("Sıradaki: Kadro Onayla");
            advice = "Kadro temiz — şimdi Kadro Onayla.";
        }
        else if (hasDuePlayableMatch)
        {
            nextFocus = TodayPulseDigest.FocusMatch;
            beats.Add("Sıradaki: Maç Gününe Git");
            advice = "Kadro temiz — şimdi Maç Gününe Git.";
        }
        else
        {
            nextFocus = TodayPulseDigest.FocusCalm;
            beats.Add("Sıradaki: nabız sakin — 1 gün ilerlet");
            advice = "İyileşme kapandı — nabız sakin, günü ilerlet.";
        }

        return new(
            Brand,
            $"İyileşti — {who} sahaya döndü.",
            advice,
            nextFocus,
            beats);
    }

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
    /// Sakin havadan maç temposuna geçiş — Ofiste kısa flash köprüsü.
    /// </summary>
    public static PostMatchOfficeDigest? AfterMoodTempoShift(
        string? previousMoodCode,
        WeekMoodDigest nextMood,
        string? nextCtaLabel = null)
    {
        ArgumentNullException.ThrowIfNull(nextMood);
        if (!nextMood.IsActive)
        {
            return null;
        }

        var shift = WeekMoodTempoBridge.Resolve(previousMoodCode, nextMood.MoodCode);
        if (shift is null)
        {
            return null;
        }

        var beats = new List<string> { nextMood.ToPulseLine() };
        if (!string.IsNullOrWhiteSpace(nextCtaLabel))
        {
            beats.Add($"Sıradaki: {nextCtaLabel}");
        }
        else
        {
            beats.Add(nextMood.MoodCode switch
            {
                WeekMoodDigest.MoodMatchReady => "Sıradaki: Maç Gününe Git",
                WeekMoodDigest.MoodMatchDraft => "Sıradaki: Kadro Onayla",
                WeekMoodDigest.MoodPromise => "Sıradaki: XI↔Yedek kontrol",
                _ => "Sıradaki: Bugün nabzına bak",
            });
        }

        var advice = !string.IsNullOrWhiteSpace(nextCtaLabel)
            ? $"{shift.AdviceLine.TrimEnd('.')} — birincil düğme: {nextCtaLabel}."
            : shift.AdviceLine;

        return new(
            Brand,
            shift.Headline,
            advice,
            shift.NextFocusCode,
            beats);
    }

    /// <summary>
    /// Gün ilerletince sakin Ofis Notu yenilenince — status onayıyla aynı dilde kısa flash.
    /// </summary>
    public static PostMatchOfficeDigest AfterCalmNoteAdvance(
        string nextNote,
        string? previousNote = null,
        string? nextCtaLabel = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nextNote);

        var beats = new List<string> { $"Not: {nextNote}" };
        if (!string.IsNullOrWhiteSpace(nextCtaLabel))
        {
            beats.Add($"Sıradaki: {nextCtaLabel}");
        }
        else
        {
            beats.Add("Sıradaki: nabız sakin — 1 gün ilerlet");
        }

        var headline = string.IsNullOrWhiteSpace(previousNote)
            ? "Sakin hafta — ofis notu geldi."
            : string.Equals(previousNote, nextNote, StringComparison.Ordinal)
                ? "Yeni gün — sakin tempo sürüyor."
                : "Yeni gün — ofis notu yenilendi.";

        var advice = !string.IsNullOrWhiteSpace(nextCtaLabel)
            ? $"Hava tutuyor — birincil düğme: {nextCtaLabel}."
            : "Nabız sakin — günü ilerlet veya Bugün'e bak.";

        return new(
            Brand,
            headline,
            advice,
            TodayPulseDigest.FocusCalm,
            beats);
    }

    /// <summary>
    /// Günlük Bugün ekranı — Ofiste metni nabız / Haftanın Hikâyesi / Havası ile aynı dili konuşsun.
    /// </summary>
    public static PostMatchOfficeDigest FromTodayPulse(
        TodayPulseDigest pulse,
        WeekMoodDigest? weekMood = null,
        WeekStoryDigest? weekStory = null,
        string? nextCtaLabel = null,
        int dayNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(pulse);
        weekMood ??= WeekMoodDigest.Clear();
        weekStory ??= WeekStoryDigest.Clear();

        if (weekStory.IsActive)
        {
            return FromWeekStoryPulse(weekStory, pulse, nextCtaLabel);
        }

        if (weekMood.IsActive)
        {
            return FromWeekMoodPulse(weekMood, pulse, nextCtaLabel, dayNumber);
        }

        return Compose(narrative: null, DecisionDeskDigest.Clear(), hasManagedMatch: false, pulse);
    }

    private static PostMatchOfficeDigest FromWeekStoryPulse(
        WeekStoryDigest story,
        TodayPulseDigest pulse,
        string? nextCtaLabel)
    {
        var beats = new List<string> { $"Hikâye: {story.StoryLine}" };
        if (!string.IsNullOrWhiteSpace(nextCtaLabel))
        {
            beats.Add($"Sıradaki: {nextCtaLabel}");
        }
        else if (string.Equals(pulse.PrimaryFocusCode, TodayPulseDigest.FocusCalm, StringComparison.Ordinal))
        {
            beats.Add("Sıradaki: nabız sakin — 1 gün ilerlet");
        }
        else
        {
            beats.Add($"Sıradaki: {pulse.Headline}");
        }

        var advice = !string.IsNullOrWhiteSpace(nextCtaLabel)
            ? $"Haftanın Hikâyesi sürüyor — birincil düğme: {nextCtaLabel}."
            : "Haftanın Hikâyesi Bugün'de — sıradaki adımı uygula.";

        return new(
            Brand,
            StoryOfficeHeadline(story),
            advice,
            pulse.PrimaryFocusCode,
            beats);
    }

    private static PostMatchOfficeDigest FromWeekMoodPulse(
        WeekMoodDigest mood,
        TodayPulseDigest pulse,
        string? nextCtaLabel,
        int dayNumber)
    {
        var beats = new List<string> { mood.ToPulseLine() };
        var calmNote = pulse.PulseLines.FirstOrDefault(
            line => line.StartsWith("Not:", StringComparison.Ordinal))
            ?? OfficeCalmNote.ToBeatLine(mood.MoodCode, dayNumber);
        if (!string.IsNullOrWhiteSpace(calmNote))
        {
            beats.Add(calmNote);
        }

        if (!string.IsNullOrWhiteSpace(nextCtaLabel))
        {
            beats.Add($"Sıradaki: {nextCtaLabel}");
        }
        else if (string.Equals(pulse.PrimaryFocusCode, TodayPulseDigest.FocusCalm, StringComparison.Ordinal))
        {
            beats.Add("Sıradaki: nabız sakin — 1 gün ilerlet");
        }
        else
        {
            beats.Add($"Sıradaki: {pulse.Headline}");
        }

        var advice = !string.IsNullOrWhiteSpace(nextCtaLabel)
            ? $"Hava tutuyor — birincil düğme: {nextCtaLabel}."
            : AdviceForFocus(pulse.PrimaryFocusCode);

        return new(
            Brand,
            MoodOfficeHeadline(mood),
            advice,
            pulse.PrimaryFocusCode,
            beats);
    }

    private static string StoryOfficeHeadline(WeekStoryDigest story) => story.PhaseCode switch
    {
        WeekStoryDigest.PhaseInjury => "Haftanın hikâyesi — sakatlık baskısı.",
        WeekStoryDigest.PhaseRecovery => "Haftanın hikâyesi — toparlanma yolu.",
        WeekStoryDigest.PhaseXi => "Haftanın hikâyesi — sakatsız kadro.",
        WeekStoryDigest.PhaseKickoff => "Haftanın hikâyesi — düdük sırada.",
        WeekStoryDigest.PhaseCleared => "Haftanın hikâyesi — iyileşti.",
        WeekStoryDigest.PhaseCleanXi => "Haftanın hikâyesi — temiz XI.",
        WeekStoryDigest.PhaseVerdict => "Haftanın hikâyesi — ofis hükmü.",
        _ => "Haftanın hikâyesi — Bugün'e bak.",
    };

    private static string MoodOfficeHeadline(WeekMoodDigest mood) => mood.MoodCode switch
    {
        WeekMoodDigest.MoodDesk => "Haftanın havası — masa konuşuyor.",
        WeekMoodDigest.MoodPromise => "Haftanın havası — söz gerilimi.",
        WeekMoodDigest.MoodMatchDraft => "Haftanın havası — kadro kilitlenmedi.",
        WeekMoodDigest.MoodMatchReady => "Haftanın havası — düdük yakın.",
        WeekMoodDigest.MoodPrep => "Haftanın havası — hazırlık çağırıyor.",
        WeekMoodDigest.MoodLeague => "Haftanın havası — lig baskısı.",
        WeekMoodDigest.MoodTransfer => "Haftanın havası — transfer sıcak.",
        WeekMoodDigest.MoodFormRise => "Haftanın havası — seri yükseliyor.",
        WeekMoodDigest.MoodFormCrisis => "Haftanın havası — form alarmı.",
        WeekMoodDigest.MoodCalmMatch => "Haftanın havası — sakin tempo.",
        WeekMoodDigest.MoodCalm => "Haftanın havası — sakin hafta.",
        _ => "Haftanın havası — ofise bak.",
    };

    public static PostMatchOfficeDigest Compose(
        MatchNightNarrative? narrative,
        DecisionDeskDigest desk,
        bool hasManagedMatch,
        TodayPulseDigest? nextPulse = null,
        string? halfTimeNoteLine = null,
        IReadOnlyList<string>? freshlyRecoveredNames = null)
    {
        ArgumentNullException.ThrowIfNull(desk);

        if (narrative is null || !hasManagedMatch)
        {
            if (freshlyRecoveredNames is { Count: > 0 })
            {
                return AfterInjuriesCleared(freshlyRecoveredNames);
            }

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
        var morningHeadline = MorningHeadline.Compose(
            narrative.ManagedGoalMargin,
            narrative.AfterWhistleLines);
        if (!string.IsNullOrWhiteSpace(morningHeadline))
        {
            beats.Add(morningHeadline);
        }

        var halfTimeNote = !string.IsNullOrWhiteSpace(halfTimeNoteLine)
            ? halfTimeNoteLine
            : ExtractHalfTimeNote(narrative);
        if (!string.IsNullOrWhiteSpace(halfTimeNote))
        {
            beats.Add(halfTimeNote);
        }

        if (freshlyRecoveredNames is { Count: > 0 })
        {
            beats.Add(
                "İyileşti: "
                + string.Join(", ", freshlyRecoveredNames.Take(2))
                + " — İyileşme Yolu kapandı");
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

        var cleanReturnBeat = FormatCleanReturnVerdictBeat(
            narrative.LineupBridge,
            narrative.ManagedGoalMargin);
        if (!string.IsNullOrWhiteSpace(cleanReturnBeat))
        {
            beats.Add(cleanReturnBeat);
        }
        else
        {
            var lineupBeat = narrative.LineupBridge?.ResultBridgeBeatLine();
            if (!string.IsNullOrWhiteSpace(lineupBeat))
            {
                beats.Add(lineupBeat);
            }
        }

        var hasInjury = HasInjuryNight(narrative);
        var hasCleanReturnVerdict = narrative.LineupBridge is { HasCleanReturn: true };
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
            advice = hasCleanReturnVerdict
                ? ComposeCleanReturnAdvice(narrative.ManagedGoalMargin, nextPulse.PrimaryFocusCode)
                : AdviceForFocus(nextPulse.PrimaryFocusCode);
            if (!hasCleanReturnVerdict
                && !string.IsNullOrWhiteSpace(halfTimeNote)
                && string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusCalm, StringComparison.Ordinal))
            {
                advice = "Gece kararını hatırla — nabız sakinse günü ilerlet.";
            }

            if (!string.Equals(nextPulse.PrimaryFocusCode, TodayPulseDigest.FocusCalm, StringComparison.Ordinal))
            {
                beats.Add($"Sıradaki: {nextPulse.Headline}");
            }
        }
        else if (hasCleanReturnVerdict)
        {
            advice = ComposeCleanReturnAdvice(narrative.ManagedGoalMargin, TodayPulseDigest.FocusCalm);
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
        else if (freshlyRecoveredNames is { Count: > 0 })
        {
            var who = string.Join(", ", freshlyRecoveredNames.Take(2));
            headline = $"İyileşti — {who} sahaya döndü.";
        }

        return new PostMatchOfficeDigest(Brand, headline, advice, focusCode, beats.Take(7).ToArray());
    }

    /// <summary>
    /// Temiz XI maçı sonrası — dönenler işe yaradı / dengede / yetmedi.
    /// </summary>
    public static string? FormatCleanReturnVerdictBeat(
        MatchDayLineupStrip? lineupBridge,
        int? managedGoalMargin)
    {
        if (lineupBridge is not { HasCleanReturn: true })
        {
            return null;
        }

        var who = string.Join(
            ", ",
            lineupBridge.ReturnedNames.Take(2).Select(ShortLastName));
        if (string.IsNullOrWhiteSpace(who))
        {
            who = "Dönenler";
        }

        if (managedGoalMargin is null)
        {
            return $"Temiz XI: {who} döndü";
        }

        if (managedGoalMargin > 0)
        {
            return $"Dönenler işe yaradı — {who}";
        }

        if (managedGoalMargin == 0)
        {
            return $"Dönenler dengede — {who}";
        }

        return $"Dönenler yetmedi — {who}";
    }

    public static string? FormatCleanReturnVerdictHeadline(int? managedGoalMargin) =>
        managedGoalMargin switch
        {
            > 0 => "Dönenler işe yaradı — Temiz XI tuttu.",
            0 => "Dönenler dengede — Temiz XI puan getirdi.",
            < 0 => "Dönenler yetmedi — Temiz XI yetmedi.",
            _ => null,
        };

    private static string ComposeCleanReturnAdvice(int? managedGoalMargin, string focusCode)
    {
        var focusAdvice = AdviceForFocus(focusCode);
        return managedGoalMargin switch
        {
            > 0 => "Dönenler tuttu — nabza bak, ritmi bozma.",
            0 => "Dönenler dengede — nabza bak, sıradaki adımı seç.",
            < 0 => "Dönenler yetmedi — nabza bak, yükü yumuşatmayı düşün.",
            _ => focusAdvice,
        };
    }

    private static string ShortLastName(string full)
    {
        if (string.IsNullOrWhiteSpace(full))
        {
            return "?";
        }

        var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^1] : parts[0];
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
                l.Contains("Disiplin", StringComparison.OrdinalIgnoreCase)
                || l.Contains("kırmızı kart", StringComparison.OrdinalIgnoreCase)))
        {
            return "Kırmızı kart — ofiste disiplin masası.";
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

        if (narrative.LineupBridge is { HasCleanReturn: true }
            && FormatCleanReturnVerdictHeadline(narrative.ManagedGoalMargin) is { } cleanHeadline)
        {
            return cleanHeadline;
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
