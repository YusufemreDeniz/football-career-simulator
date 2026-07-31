using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

/// <summary>
/// Maç günü XI şeridi — kim sahada, kim sakatlıkla dışarı, kim yerine girdi.
/// </summary>
public sealed record MatchDayLineupStrip(
    bool HasMatch,
    bool IsApproved,
    string Caption,
    IReadOnlyList<MatchDayLineupChip> StartingXi,
    IReadOnlyList<MatchDayLineupChip> OutPlayers)
{
    public const string MarkerIn = "In";
    public const string MarkerOut = "Out";
    public const string MarkerNormal = "";

    public static MatchDayLineupStrip Clear() =>
        new(
            false,
            false,
            "XI şeridi — vadesi gelmiş maç yok.",
            Array.Empty<MatchDayLineupChip>(),
            Array.Empty<MatchDayLineupChip>());

    public static MatchDayLineupStrip Compose(
        bool hasMatch,
        bool isApproved,
        IReadOnlyList<int> displayStartingSlots,
        IReadOnlyList<MvpAvailabilityAwareSelection.AvailabilityAutoSwap> swaps,
        IReadOnlyList<string> playerNames)
    {
        if (!hasMatch)
        {
            return Clear();
        }

        ArgumentNullException.ThrowIfNull(displayStartingSlots);
        ArgumentNullException.ThrowIfNull(swaps);
        ArgumentNullException.ThrowIfNull(playerNames);

        var inSlots = swaps.Select(s => s.InSlotIndex).ToHashSet();
        var outSlots = swaps.Select(s => s.OutSlotIndex).ToArray();

        var xi = displayStartingSlots
            .Take(11)
            .Select(slot => new MatchDayLineupChip(
                NameOf(playerNames, slot),
                inSlots.Contains(slot) ? MarkerIn : MarkerNormal,
                slot))
            .ToArray();

        var outs = outSlots
            .Select(slot => new MatchDayLineupChip(
                NameOf(playerNames, slot),
                MarkerOut,
                slot))
            .ToArray();

        var caption = outs.Length == 0
            ? isApproved
                ? "Onaylı XI"
                : "Taslak XI (onayla kilitle)"
            : isApproved
                ? $"Onaylı XI · {outs.Length} sakat dışarı"
                : $"Taslak XI · onayda {outs.Length} sakat dışarı";

        return new MatchDayLineupStrip(true, isApproved, caption, xi, outs);
    }

    private static string NameOf(IReadOnlyList<string> playerNames, int slotIndex) =>
        slotIndex >= 0 && slotIndex < playerNames.Count
            ? playerNames[slotIndex]
            : $"#{slotIndex + 1}";
}

public sealed record MatchDayLineupChip(
    string DisplayName,
    string MarkerCode,
    int SlotIndex)
{
    public bool IsIn => string.Equals(MarkerCode, MatchDayLineupStrip.MarkerIn, StringComparison.Ordinal);
    public bool IsOut => string.Equals(MarkerCode, MatchDayLineupStrip.MarkerOut, StringComparison.Ordinal);

    public string ChipLabel =>
        IsIn ? $"↑ {ShortName(DisplayName)}"
        : IsOut ? $"× {ShortName(DisplayName)}"
        : ShortName(DisplayName);

    private static string ShortName(string full)
    {
        if (string.IsNullOrWhiteSpace(full))
        {
            return "?";
        }

        var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^1] : parts[0];
    }
}
