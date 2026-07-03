using FootballCareerSimulator.Domain.Spike1Placeholder;

namespace FootballCareerSimulator.Application.Spike4Placeholder;

/// <summary>
/// Spike 4 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 6) için oluşturulmuş, Godot'a hiç bağımlı
/// olmayan (dolayısıyla düz xUnit ile test edilebilen) filtre/sıralama/sayfalama read-model
/// sorgusudur. UI, bu sorgunun ürettiği <see cref="PlayerListRow"/> listesini görüntüler; hiçbir
/// zaman Domain/Simulation state'ini doğrudan okumaz veya değiştirmez.
/// </summary>
public static class PlayerListQuery
{
    public static IReadOnlyList<PlayerListRow> BuildRows(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var clubNamesById = world.Clubs.ToDictionary(club => club.Id.Value, club => club.Name);

        return world.Players
            .Select(player => new PlayerListRow(
                PlayerId: player.Id.Value,
                PlayerLabel: player.Id.ToString(),
                ClubId: player.ClubId.Value,
                ClubName: clubNamesById.GetValueOrDefault(player.ClubId.Value, "?"),
                Age: player.Age,
                Form: player.Form))
            .ToArray();
    }

    public static IReadOnlyList<PlayerListRow> Filter(IReadOnlyList<PlayerListRow> rows, string? searchText)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return rows;
        }

        var needle = searchText.Trim();

        return rows
            .Where(row =>
                row.PlayerLabel.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || row.ClubName.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public static IReadOnlyList<PlayerListRow> Sort(IReadOnlyList<PlayerListRow> rows, PlayerListSortColumn column, bool ascending)
    {
        ArgumentNullException.ThrowIfNull(rows);

        IOrderedEnumerable<PlayerListRow> ordered = column switch
        {
            PlayerListSortColumn.PlayerId => ascending
                ? rows.OrderBy(row => row.PlayerId)
                : rows.OrderByDescending(row => row.PlayerId),
            PlayerListSortColumn.ClubName => ascending
                ? rows.OrderBy(row => row.ClubName, StringComparer.OrdinalIgnoreCase)
                : rows.OrderByDescending(row => row.ClubName, StringComparer.OrdinalIgnoreCase),
            PlayerListSortColumn.Age => ascending
                ? rows.OrderBy(row => row.Age)
                : rows.OrderByDescending(row => row.Age),
            PlayerListSortColumn.Form => ascending
                ? rows.OrderBy(row => row.Form)
                : rows.OrderByDescending(row => row.Form),
            _ => throw new ArgumentOutOfRangeException(nameof(column), column, "Unknown sort column."),
        };

        // Eşit anahtarlarda kararlı bir ikincil sıralama, sayfalar arası tutarlı sırayı garanti eder.
        return ordered.ThenBy(row => row.PlayerId).ToArray();
    }

    public static IReadOnlyList<PlayerListRow> Page(IReadOnlyList<PlayerListRow> rows, int pageIndex, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (pageIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), pageIndex, "Page index cannot be negative.");
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be positive.");
        }

        return rows.Skip(pageIndex * pageSize).Take(pageSize).ToArray();
    }

    public static int GetPageCount(int totalRowCount, int pageSize)
    {
        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be positive.");
        }

        return totalRowCount == 0 ? 1 : (int)Math.Ceiling(totalRowCount / (double)pageSize);
    }
}
