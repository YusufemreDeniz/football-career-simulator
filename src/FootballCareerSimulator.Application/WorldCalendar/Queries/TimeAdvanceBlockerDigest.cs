namespace FootballCareerSimulator.Application.WorldCalendar.Queries;

/// <summary>
/// Zaman ilerletme engeli — Bugün ekranında "neden ilerleyemiyorum, ne yapayım?".
/// </summary>
public sealed record TimeAdvanceBlockerDigest(
    bool CanAdvance,
    string BrandTitle,
    string Headline,
    string AdviceLine,
    string? PrimaryBlockerCode,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "İlerleme";

    public const string CodeUnplayedFixtures = "UnplayedFixturesDue";
    public const string CodePendingDecision = "PendingDecisionRequest";

    public static TimeAdvanceBlockerDigest Clear() =>
        new(true, Brand, "İlerleme açık.", "1 Gün İlerlet ile devam.", null, Array.Empty<string>());

    public static TimeAdvanceBlockerDigest Compose(
        bool canAdvance,
        IReadOnlyList<(string SourceContext, string DescriptionCode, bool IsHardBlocker)> blockers)
    {
        ArgumentNullException.ThrowIfNull(blockers);

        if (canAdvance || blockers.Count == 0)
        {
            return Clear();
        }

        var primary = blockers
            .OrderByDescending(b => b.IsHardBlocker)
            .ThenBy(b => b.DescriptionCode, StringComparer.Ordinal)
            .First();

        var beats = blockers
            .Select(b => Describe(b.DescriptionCode))
            .Distinct(StringComparer.Ordinal)
            .Take(4)
            .ToArray();

        var advice = primary.DescriptionCode switch
        {
            CodeUnplayedFixtures => "Önce kadroyu kilitle veya Maç Gününe Git.",
            CodePendingDecision => "Masada zorunlu kararı yanıtla — sonra gün ilerler.",
            _ => "Listedeki engeli çöz; sonra 1 Gün İlerlet.",
        };

        return new TimeAdvanceBlockerDigest(
            CanAdvance: false,
            Brand,
            "Zaman kilitli — önce engeli kaldır.",
            advice,
            primary.DescriptionCode,
            beats);
    }

    public string ToDisplayText()
    {
        if (CanAdvance)
        {
            return $"{BrandTitle}\n{Headline}";
        }

        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        return $"{BrandTitle}\n{Headline}{beats}\nÖneri: {AdviceLine}";
    }

    public static string Describe(string descriptionCode) => descriptionCode switch
    {
        CodeUnplayedFixtures => "Oynanmamış maçlar var — önce Maç Gününe Git.",
        CodePendingDecision => "Bekleyen zorunlu karar var — önce Masada yanıtla.",
        _ => descriptionCode,
    };
}
