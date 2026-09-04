using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Bugün — sakatlık → iyileşme → Temiz XI yayının tek cümlelik hafta özeti.
/// </summary>
public sealed record WeekStoryDigest(
    bool IsActive,
    string BrandTitle,
    string StoryLine,
    string PhaseCode)
{
    public const string Brand = "Haftanın Hikâyesi";

    public const string PhaseInjury = "Injury";
    public const string PhaseRecovery = "Recovery";
    public const string PhaseXi = "Xi";
    public const string PhaseKickoff = "Kickoff";
    public const string PhaseCleared = "Cleared";
    public const string PhaseCleanXi = "CleanXi";
    public const string PhaseVerdict = "Verdict";

    public static WeekStoryDigest Clear() =>
        new(false, Brand, string.Empty, string.Empty);

    public static WeekStoryDigest Compose(
        InjuryRecoveryPathDigest recoveryPath,
        PreMatchBriefing match,
        string? closedArcVerdictBeat = null)
    {
        ArgumentNullException.ThrowIfNull(recoveryPath);
        ArgumentNullException.ThrowIfNull(match);

        if (recoveryPath.IsActive
            && !string.Equals(
                recoveryPath.CurrentStepCode,
                InjuryRecoveryPathDigest.StepCleared,
                StringComparison.Ordinal))
        {
            return FromActivePath(recoveryPath);
        }

        if (recoveryPath.IsActive
            && string.Equals(
                recoveryPath.CurrentStepCode,
                InjuryRecoveryPathDigest.StepCleared,
                StringComparison.Ordinal))
        {
            return new WeekStoryDigest(
                true,
                Brand,
                TrimHeadlinePrefix(recoveryPath.Headline),
                PhaseCleared);
        }

        if (match.HasCleanReturn)
        {
            var who = FormatWho(match.ReturnedNames);
            return new WeekStoryDigest(
                true,
                Brand,
                $"Temiz XI — {who} döndü, sakatsız düdük sırada.",
                PhaseCleanXi);
        }

        if (!string.IsNullOrWhiteSpace(closedArcVerdictBeat))
        {
            return new WeekStoryDigest(
                true,
                Brand,
                closedArcVerdictBeat.Trim().TrimEnd('.') + ".",
                PhaseVerdict);
        }

        return Clear();
    }

    public string ToDisplayText()
    {
        if (!IsActive)
        {
            return string.Empty;
        }

        return $"{BrandTitle}\n{StoryLine}";
    }

    public string ToPulseLine() =>
        IsActive ? $"Hikâye: {StoryLine}" : string.Empty;

    private static WeekStoryDigest FromActivePath(InjuryRecoveryPathDigest path)
    {
        var who = ExtractWho(path.Headline);
        return path.CurrentStepCode switch
        {
            InjuryRecoveryPathDigest.StepRecovery => new WeekStoryDigest(
                true,
                Brand,
                $"{who} sakat — Toparlanma uygula.",
                PhaseInjury),
            InjuryRecoveryPathDigest.StepXi => new WeekStoryDigest(
                true,
                Brand,
                "Toparlanma işledi — sakatsız XI sırada.",
                PhaseXi),
            InjuryRecoveryPathDigest.StepKickoff => new WeekStoryDigest(
                true,
                Brand,
                "XI hazır — Temiz düdük için Maç Gününe git.",
                PhaseKickoff),
            InjuryRecoveryPathDigest.StepHold => new WeekStoryDigest(
                true,
                Brand,
                "Toparlanma sürüyor — sıradaki maçı bekle.",
                PhaseRecovery),
            _ => new WeekStoryDigest(
                true,
                Brand,
                TrimHeadlinePrefix(path.Headline),
                PhaseRecovery),
        };
    }

    private static string ExtractWho(string headline)
    {
        // "İyileşme 1/3 — Tolga Kurt: Toparlanma uygula"
        const string dash = " — ";
        var dashAt = headline.IndexOf(dash, StringComparison.Ordinal);
        if (dashAt < 0)
        {
            return "Sakatlar";
        }

        var rest = headline[(dashAt + dash.Length)..];
        var colonAt = rest.IndexOf(':');
        if (colonAt > 0)
        {
            rest = rest[..colonAt];
        }

        rest = rest.Trim();
        return string.IsNullOrWhiteSpace(rest) ? "Sakatlar" : rest;
    }

    private static string TrimHeadlinePrefix(string headline)
    {
        const string cleared = "İyileşti — ";
        if (headline.StartsWith(cleared, StringComparison.Ordinal))
        {
            return headline[cleared.Length..].Trim();
        }

        const string path = "İyileşme";
        if (headline.StartsWith(path, StringComparison.Ordinal))
        {
            var dashAt = headline.IndexOf(" — ", StringComparison.Ordinal);
            if (dashAt >= 0)
            {
                return headline[(dashAt + 3)..].Trim();
            }
        }

        return headline.Trim();
    }

    private static string FormatWho(IReadOnlyList<string> names) =>
        names.Count == 0
            ? "Sakatlar"
            : string.Join(", ", names.Take(2));
}
