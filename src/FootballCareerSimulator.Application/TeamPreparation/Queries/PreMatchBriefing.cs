using FootballCareerSimulator.Application.TeamPreparation.Services;

namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

/// <summary>
/// Bugün sayfası maç brifingi — düdüğe basmadan önce neyin önemli olduğunu özetler.
/// </summary>
public sealed record PreMatchBriefing(
    bool HasMatch,
    bool IsReadyToKickOff,
    bool HasPromiseRisk,
    string BrandTitle,
    string Headline,
    string FixtureLine,
    IReadOnlyList<string> BeatLines,
    bool HasInjuryPressure = false)
{
    public const string Brand = "Sıradaki Maç";

    public static PreMatchBriefing Clear() =>
        new(
            HasMatch: false,
            IsReadyToKickOff: false,
            HasPromiseRisk: false,
            Brand,
            "Takvimde vadesi gelmiş maç yok — günü ilerlet veya lig kur.",
            string.Empty,
            Array.Empty<string>(),
            HasInjuryPressure: false);

    public static PreMatchBriefing Compose(
        ManagedFixtureSelectionStatusReadModel? pending,
        string opponentName,
        int currentDayNumber,
        string? formationName = null,
        string? approachName = null,
        int? averageFatigue = null,
        int? averageFitness = null,
        int injuredSlotCount = 0,
        PreMatchPromiseTensionReadModel? tension = null,
        IReadOnlyList<string>? injuredPlayerNames = null,
        IReadOnlyList<string>? autoSwapWarningLines = null)
    {
        if (pending is null)
        {
            return Clear();
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(opponentName);

        var venue = pending.IsHome ? "Ev" : "Dep";
        var when = WhenLabel(pending.ScheduledDayNumber, currentDayNumber, pending.ScheduledIsoDate);
        var fixtureLine = $"{venue} vs {opponentName} · {when}";

        var atRisk = tension is
        {
            HasTension: true,
            ToneCode: PreMatchPromiseTensionQueryService.ToneAtRisk
        };
        var onTrack = tension is
        {
            HasTension: true,
            ToneCode: PreMatchPromiseTensionQueryService.ToneOnTrack
        };

        var ready = pending.IsApproved;
        var injuryPressure = injuredSlotCount > 0;
        var hasAutoSwap = autoSwapWarningLines is { Count: > 0 };
        var headline = ResolveHeadline(ready, atRisk, onTrack, injuryPressure, hasAutoSwap);

        var beats = new List<string>();
        if (!ready)
        {
            beats.Add(injuryPressure
                ? hasAutoSwap
                    ? "Onayda sakatlar otomatik dışarı — sakatsız XI kilitle."
                    : "Sakatlık onayı düşürdü — sakatsız XI onayla."
                : "Kadro onayı bekliyor — onaylamadan maç oynanmaz.");
        }
        else
        {
            beats.Add("Kadro onaylı.");
        }

        if (autoSwapWarningLines is { Count: > 0 })
        {
            beats.AddRange(autoSwapWarningLines.Take(2));
        }

        if (injuredPlayerNames is { Count: > 0 })
        {
            beats.Add("Sakat: " + string.Join(", ", injuredPlayerNames.Take(3)));
        }
        else if (injuredSlotCount > 0)
        {
            beats.Add($"Sakat oyuncu: {injuredSlotCount}");
        }

        if (!string.IsNullOrWhiteSpace(formationName)
            && !string.Equals(formationName, "yok", StringComparison.Ordinal)
            && !string.Equals(formationName, "—", StringComparison.Ordinal))
        {
            var approach = string.IsNullOrWhiteSpace(approachName)
                || string.Equals(approachName, "yok", StringComparison.Ordinal)
                || string.Equals(approachName, "—", StringComparison.Ordinal)
                ? string.Empty
                : $" · {approachName}";
            beats.Add($"Taktik: {formationName}{approach}");
        }

        if (averageFatigue is int fatigue && averageFitness is int fitness)
        {
            var injury = injuredSlotCount > 0 ? $" · sakat {injuredSlotCount}" : string.Empty;
            beats.Add($"XI yorgunluk {fatigue} · fitness {fitness}{injury}");
        }

        if (tension is { HasTension: true })
        {
            foreach (var line in tension.Lines.Take(2))
            {
                var marker = line.PlacementCode is PreMatchPromiseTensionQueryService.PlacementBench
                    or PreMatchPromiseTensionQueryService.PlacementOut
                    ? "Söz riski: "
                    : "Söz: ";
                beats.Add(marker + line.SummaryLine);
            }
        }

        return new PreMatchBriefing(
            HasMatch: true,
            IsReadyToKickOff: ready,
            HasPromiseRisk: atRisk,
            Brand,
            headline,
            fixtureLine,
            beats,
            HasInjuryPressure: injuryPressure);
    }

    public string ToDisplayText()
    {
        if (!HasMatch)
        {
            return $"{BrandTitle}\n{Headline}";
        }

        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        return $"{BrandTitle}\n{Headline}\n{FixtureLine}{beats}";
    }

    /// <summary>
    /// Maç gecesi "Maça böyle girdin" köprüsü — brifingden 1–4 satır.
    /// </summary>
    public IReadOnlyList<string> ToKickoffBridgeLines()
    {
        if (!HasMatch)
        {
            return Array.Empty<string>();
        }

        var lines = new List<string> { FixtureLine };
        if (HasInjuryPressure)
        {
            lines.Add("Maça sakatlık baskısıyla girdin.");
        }
        else if (HasPromiseRisk)
        {
            lines.Add("Maça söz riskiyle girdin.");
        }
        else if (IsReadyToKickOff)
        {
            lines.Add("Kadro hazırdı — düdük çaldı.");
        }

        foreach (var beat in BeatLines)
        {
            if (beat.StartsWith("Sakat XI'de:", StringComparison.Ordinal)
                || beat.StartsWith("Sakat:", StringComparison.Ordinal)
                || beat.StartsWith("Taktik:", StringComparison.Ordinal)
                || beat.StartsWith("Söz riski:", StringComparison.Ordinal)
                || beat.StartsWith("XI yorgunluk", StringComparison.Ordinal))
            {
                lines.Add(beat);
            }

            if (lines.Count >= 4)
            {
                break;
            }
        }

        return lines.Take(4).ToArray();
    }

    private static string ResolveHeadline(
        bool approved,
        bool atRisk,
        bool onTrack,
        bool injuryPressure,
        bool hasAutoSwap = false)
    {
        if (!approved)
        {
            if (injuryPressure)
            {
                return hasAutoSwap
                    ? "Sakatlar XI'den düşecek — onayda yedekler gelir."
                    : "Sakatlık kadroyu düşürdü — sakatsız XI onayla.";
            }

            return atRisk
                ? "Kadro eksik ve söz riski var — önce XI'yi düzelt."
                : "Henüz hazır değilsin — önce kadroyu onayla.";
        }

        if (injuryPressure)
        {
            return "Sakatlar listede — XI'yi kontrol et.";
        }

        if (atRisk)
        {
            return "Kadro hazır ama söz riski var — XI↔Yedek düşün.";
        }

        if (onTrack)
        {
            return "Hazırsın — sözler yolunda, düdük için basabilirsin.";
        }

        return "Hazırsın — düdük için basabilirsin.";
    }

    private static string WhenLabel(int scheduledDay, int currentDay, string isoDate)
    {
        var delta = scheduledDay - currentDay;
        return delta switch
        {
            <= 0 => string.IsNullOrWhiteSpace(isoDate) ? "bugün" : $"bugün ({isoDate})",
            1 => string.IsNullOrWhiteSpace(isoDate) ? "yarın" : $"yarın ({isoDate})",
            _ => string.IsNullOrWhiteSpace(isoDate) ? $"{delta} gün sonra" : $"{delta} gün sonra ({isoDate})",
        };
    }
}
