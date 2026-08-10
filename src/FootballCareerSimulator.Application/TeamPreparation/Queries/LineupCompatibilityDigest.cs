using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

public enum LineupCompatibilitySignal
{
    Strong = 1,
    Watch = 2,
    Risk = 3,
}

public sealed record LineupCompatibilityPlayer(
    string DisplayName,
    string PositionCode,
    bool IsNaturalFit);

public sealed record LineupCompatibilityDigest(
    bool HasLineup,
    Formation Formation,
    int Score,
    int NaturalFitCount,
    int MatchStrengthModifier,
    LineupCompatibilitySignal Signal,
    string Headline,
    string BalanceLine,
    string DetailLine,
    IReadOnlyList<LineupCompatibilityPlayer> Players)
{
    public static LineupCompatibilityDigest Clear() =>
        new(
            false,
            Formation.F442,
            0,
            0,
            0,
            LineupCompatibilitySignal.Risk,
            "Kadro uyumu için ilk 11 bekleniyor.",
            string.Empty,
            string.Empty,
            Array.Empty<LineupCompatibilityPlayer>());

    public static LineupCompatibilityDigest Compose(
        Formation formation,
        IReadOnlyList<int> startingSlotIndices,
        IReadOnlyList<MvpSquadPlayerProfile> squad)
    {
        ArgumentNullException.ThrowIfNull(startingSlotIndices);
        ArgumentNullException.ThrowIfNull(squad);

        if (startingSlotIndices.Count == 0)
        {
            return Clear();
        }

        var selected = startingSlotIndices
            .Take(MatchSelection.StartingXiSize)
            .Where(slot => slot >= 0 && slot < squad.Count)
            .Select(slot => squad[slot])
            .ToArray();
        if (selected.Length != MatchSelection.StartingXiSize)
        {
            return Clear();
        }

        var required = RequirementsFor(formation);
        var roleFit = MvpLineupRoleFitCalculator.Evaluate(formation, selected);
        var players = selected
            .Select((player, index) => new LineupCompatibilityPlayer(
                player.DisplayName,
                player.PositionCode,
                roleFit.PlayerNaturalFits[index]))
            .ToArray();

        var naturalFitCount = roleFit.NaturalFitCount;
        var score = roleFit.Score;
        var signal = score switch
        {
            100 => LineupCompatibilitySignal.Strong,
            >= 82 => LineupCompatibilitySignal.Watch,
            _ => LineupCompatibilitySignal.Risk,
        };

        var actual = Enum.GetValues<MvpSquadPositionGroup>()
            .ToDictionary(position => position, position => selected.Count(player => player.PositionGroup == position));
        var balanceLine = string.Join(
            " · ",
            Enum.GetValues<MvpSquadPositionGroup>().Select(position =>
                $"{PositionCode(position)} {actual[position]}/{required[position]}"));

        var missing = MissingRoleLabels(selected, roleFit.RequiredRoles, roleFit.PlayerByRoleIndex);
        var outOfPosition = players
            .Where(player => !player.IsNaturalFit)
            .Select(player => $"{ShortName(player.DisplayName)} ({player.PositionCode})")
            .ToArray();

        var detailLine = naturalFitCount == MatchSelection.StartingXiSize
            ? "İlk 11'in tamamı doğal mevki grubunda."
            : $"Pozisyon dışı: {string.Join(", ", outOfPosition)}"
              + (missing.Length > 0 ? $" · Eksik: {string.Join(", ", missing)}" : string.Empty);
        var headline = signal switch
        {
            LineupCompatibilitySignal.Strong => $"%{score} · Tam uyum",
            LineupCompatibilitySignal.Watch => $"%{score} · Dengeyi kontrol et",
            _ => $"%{score} · Diziliş riski",
        };

        return new LineupCompatibilityDigest(
            true,
            formation,
            score,
            naturalFitCount,
            roleFit.MatchStrengthModifier,
            signal,
            headline,
            balanceLine,
            detailLine,
            players);
    }

    private static IReadOnlyDictionary<MvpSquadPositionGroup, int> RequirementsFor(Formation formation) =>
        formation switch
        {
            Formation.F442 => Requirements(goalkeepers: 1, defenders: 4, midfielders: 4, forwards: 2),
            Formation.F433 => Requirements(goalkeepers: 1, defenders: 4, midfielders: 3, forwards: 3),
            Formation.F352 => Requirements(goalkeepers: 1, defenders: 3, midfielders: 5, forwards: 2),
            _ => throw new ArgumentOutOfRangeException(nameof(formation), formation, "Unknown formation."),
        };

    private static string[] MissingRoleLabels(
        IReadOnlyList<MvpSquadPlayerProfile> players,
        IReadOnlyList<MvpSquadPositionRole> requiredRoles,
        IReadOnlyList<int> playerByRoleIndex)
    {
        if (players.All(player => player.PositionRole is null))
        {
            var required = Requirements(
                goalkeepers: requiredRoles.Count(role => role.ToPositionGroup() == MvpSquadPositionGroup.Goalkeeper),
                defenders: requiredRoles.Count(role => role.ToPositionGroup() == MvpSquadPositionGroup.Defender),
                midfielders: requiredRoles.Count(role => role.ToPositionGroup() == MvpSquadPositionGroup.Midfielder),
                forwards: requiredRoles.Count(role => role.ToPositionGroup() == MvpSquadPositionGroup.Forward));
            return Enum.GetValues<MvpSquadPositionGroup>()
                .Select(position =>
                    (
                        Position: position,
                        Count: Math.Max(0, required[position] - players.Count(player => player.PositionGroup == position))
                    ))
                .Where(item => item.Count > 0)
                .Select(item => $"{item.Count} {PositionCode(item.Position)}")
                .ToArray();
        }

        return requiredRoles
            .Where((_, index) => playerByRoleIndex[index] < 0)
            .GroupBy(role => role)
            .Select(group => $"{group.Count()} {group.Key.ToPositionCode()}")
            .ToArray();
    }

    private static IReadOnlyDictionary<MvpSquadPositionGroup, int> Requirements(
        int goalkeepers,
        int defenders,
        int midfielders,
        int forwards) =>
        new Dictionary<MvpSquadPositionGroup, int>
        {
            [MvpSquadPositionGroup.Goalkeeper] = goalkeepers,
            [MvpSquadPositionGroup.Defender] = defenders,
            [MvpSquadPositionGroup.Midfielder] = midfielders,
            [MvpSquadPositionGroup.Forward] = forwards,
        };

    private static string PositionCode(MvpSquadPositionGroup position) => position switch
    {
        MvpSquadPositionGroup.Goalkeeper => "KL",
        MvpSquadPositionGroup.Defender => "DEF",
        MvpSquadPositionGroup.Midfielder => "ORT",
        MvpSquadPositionGroup.Forward => "HÜC",
        _ => "?",
    };

    private static string ShortName(string displayName)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[^1] : displayName;
    }
}
