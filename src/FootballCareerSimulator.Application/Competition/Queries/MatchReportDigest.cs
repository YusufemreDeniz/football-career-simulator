namespace FootballCareerSimulator.Application.Competition.Queries;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Domain.Match;

/// <summary>
/// Maç sonucunun oyuncuya dönük kısa raporu: temel sayılar, sakatlık ve gecenin öne çıkanı.
/// </summary>
public sealed record MatchReportDigest(
    string HomeClubName,
    string AwayClubName,
    IReadOnlyList<MatchReportStatLine> StatLines,
    string? StandoutLine,
    string? InjuryLine = null)
{
    public static MatchReportDigest? Compose(
        PlayFixtureMatchResult result,
        string homeClubName,
        string awayClubName)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(homeClubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(awayClubName);

        if (result.Statistics is not { } stats)
        {
            return null;
        }

        return new MatchReportDigest(
            homeClubName,
            awayClubName,
            [
                new MatchReportStatLine(
                    "Topa sahip olma",
                    $"%{stats.HomePossessionPercent}",
                    $"%{stats.AwayPossessionPercent}"),
                new MatchReportStatLine("Şut", stats.HomeShots.ToString(), stats.AwayShots.ToString()),
                new MatchReportStatLine(
                    "İsabetli şut",
                    stats.HomeShotsOnTarget.ToString(),
                    stats.AwayShotsOnTarget.ToString()),
                new MatchReportStatLine("Korner", stats.HomeCorners.ToString(), stats.AwayCorners.ToString()),
            ],
            ComposeStandout(result.KeyMoments, homeClubName, awayClubName),
            ComposeInjuryLine(result.KeyMoments));
    }

    private static string? ComposeInjuryLine(IReadOnlyList<MatchKeyMomentReadModel>? moments)
    {
        if (moments is null || moments.Count == 0)
        {
            return null;
        }

        var injuries = moments
            .Where(moment => string.Equals(moment.Kind, nameof(MatchKeyMomentKind.Injury), StringComparison.Ordinal))
            .OrderBy(moment => moment.Minute)
            .Select(moment =>
            {
                var name = string.IsNullOrWhiteSpace(moment.PrimaryPlayerName)
                    ? $"Oyuncu #{moment.PrimarySlotIndex + 1}"
                    : moment.PrimaryPlayerName;
                return $"{moment.Minute}' {name}";
            })
            .ToArray();

        return injuries.Length == 0
            ? null
            : $"Sakatlık: {string.Join(" · ", injuries)}";
    }

    private static string? ComposeStandout(
        IReadOnlyList<MatchKeyMomentReadModel>? moments,
        string homeClubName,
        string awayClubName)
    {
        if (moments is null || moments.Count == 0)
        {
            return null;
        }

        var performances = new Dictionary<(bool IsHome, string Name), Performance>();
        foreach (var goal in moments.Where(moment =>
                     string.Equals(moment.Kind, nameof(MatchKeyMomentKind.Goal), StringComparison.Ordinal)))
        {
            var scorerName = PlayerName(goal.PrimaryPlayerName, goal.PrimarySlotIndex);
            var scorerKey = (goal.IsHomeSide, scorerName);
            performances[scorerKey] = performances.GetValueOrDefault(scorerKey) with
            {
                Goals = performances.GetValueOrDefault(scorerKey).Goals + 1,
            };

            if (goal.AssistSlotIndex is int assistSlot)
            {
                var assistName = PlayerName(goal.AssistPlayerName, assistSlot);
                var assistKey = (goal.IsHomeSide, assistName);
                performances[assistKey] = performances.GetValueOrDefault(assistKey) with
                {
                    Assists = performances.GetValueOrDefault(assistKey).Assists + 1,
                };
            }
        }

        var standout = performances
            .OrderByDescending(pair => (pair.Value.Goals * 2) + pair.Value.Assists)
            .ThenByDescending(pair => pair.Value.Goals)
            .ThenBy(pair => pair.Key.Name, StringComparer.Ordinal)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(standout.Key.Name))
        {
            return null;
        }

        var clubName = standout.Key.IsHome ? homeClubName : awayClubName;
        var contributions = new List<string>(2);
        if (standout.Value.Goals > 0)
        {
            contributions.Add($"{standout.Value.Goals} gol");
        }

        if (standout.Value.Assists > 0)
        {
            contributions.Add($"{standout.Value.Assists} asist");
        }

        return $"Öne çıkan: {standout.Key.Name} ({clubName}) · {string.Join(" · ", contributions)}";
    }

    private static string PlayerName(string? name, int slotIndex) =>
        string.IsNullOrWhiteSpace(name) ? $"Oyuncu #{slotIndex + 1}" : name;

    private readonly record struct Performance(int Goals = 0, int Assists = 0);
}

public sealed record MatchReportStatLine(
    string Label,
    string HomeValue,
    string AwayValue);
