namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Bugün — sakatlık → Toparlanma → XI → maç yolunun tek özeti.
/// </summary>
public sealed record InjuryRecoveryPathDigest(
    bool IsActive,
    string BrandTitle,
    string Headline,
    string CurrentStepCode,
    IReadOnlyList<string> StepLines)
{
    public const string Brand = "İyileşme Yolu";

    public const string StepRecovery = "Recovery";
    public const string StepXi = "Xi";
    public const string StepKickoff = "Kickoff";
    public const string StepHold = "Hold";
    public const string StepCleared = "Cleared";

    public static InjuryRecoveryPathDigest Clear() =>
        new(false, Brand, string.Empty, string.Empty, Array.Empty<string>());

    /// <summary>
    /// Sakatlık tamamen bitince tek kapanış anı — yol kaybolmaz, kutlanır.
    /// </summary>
    public static InjuryRecoveryPathDigest ComposeCleared(IReadOnlyList<string>? recoveredPlayerNames)
    {
        var names = recoveredPlayerNames ?? Array.Empty<string>();
        var who = names.Count > 0
            ? string.Join(", ", names.Take(2))
            : "Sakatlar";

        return new InjuryRecoveryPathDigest(
            true,
            Brand,
            $"İyileşti — {who} sahaya döndü",
            StepCleared,
            new[]
            {
                "✓ Toparlanma uygula",
                "✓ Sakatsız XI onayla",
                "✓ Maç gününe git",
            });
    }

    public static InjuryRecoveryPathDigest Compose(
        bool hasInjuryPressure,
        IReadOnlyList<string>? injuredPlayerNames,
        bool isOnRecoveryPlan,
        bool hasDueMatch,
        bool isMatchApproved,
        IReadOnlyList<string>? freshlyRecoveredNames = null)
    {
        if (!hasInjuryPressure)
        {
            return freshlyRecoveredNames is { Count: > 0 }
                ? ComposeCleared(freshlyRecoveredNames)
                : Clear();
        }

        var names = injuredPlayerNames ?? Array.Empty<string>();
        var who = names.Count > 0 ? names[0] : "Sakatlar";

        string current;
        string headline;
        if (!isOnRecoveryPlan)
        {
            current = StepRecovery;
            headline = $"İyileşme 1/3 — {who}: Toparlanma uygula";
        }
        else if (hasDueMatch && !isMatchApproved)
        {
            current = StepXi;
            headline = $"İyileşme 2/3 — Toparlanma işledi: sakatsız XI onayla";
        }
        else if (hasDueMatch && isMatchApproved)
        {
            current = StepKickoff;
            headline = "İyileşme 3/3 — XI hazır: Maç Gününe git";
        }
        else
        {
            current = StepHold;
            headline = "İyileşme — Toparlanma sürüyor, sıradaki maçı bekle";
        }

        var steps = new[]
        {
            FormatStep(StepRecovery, current, "Toparlanma uygula"),
            FormatStep(StepXi, current, "Sakatsız XI onayla"),
            FormatStep(StepKickoff, current, "Maç gününe git"),
        };

        return new InjuryRecoveryPathDigest(true, Brand, headline, current, steps);
    }

    public string ToDisplayText()
    {
        if (!IsActive)
        {
            return string.Empty;
        }

        var steps = StepLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", StepLines);
        return $"{BrandTitle}\n{Headline}{steps}";
    }

    private static string FormatStep(string stepCode, string currentStepCode, string label)
    {
        var order = StepOrder(stepCode);
        var current = StepOrder(currentStepCode);
        if (currentStepCode == StepHold)
        {
            // Toparlanma yapıldı, maç yok — ilk adım tamam, diğerleri bekliyor.
            return stepCode == StepRecovery ? $"✓ {label}" : $"○ {label}";
        }

        if (order < current)
        {
            return $"✓ {label}";
        }

        if (order == current)
        {
            return $"→ {label}";
        }

        return $"○ {label}";
    }

    private static int StepOrder(string stepCode) =>
        stepCode switch
        {
            StepRecovery => 1,
            StepXi => 2,
            StepKickoff => 3,
            StepHold => 1,
            _ => 0,
        };
}
