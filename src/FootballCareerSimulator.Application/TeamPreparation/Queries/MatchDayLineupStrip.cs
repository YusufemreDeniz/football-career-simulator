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
    IReadOnlyList<MatchDayLineupChip> OutPlayers,
    IReadOnlyList<string>? CleanReturnNames = null)
{
    public const string MarkerIn = "In";
    public const string MarkerOut = "Out";
    public const string MarkerNormal = "";

    public IReadOnlyList<string> ReturnedNames => CleanReturnNames ?? Array.Empty<string>();

    public bool HasCleanReturn => ReturnedNames.Count > 0 && OutPlayers.Count == 0;

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
        IReadOnlyList<string> playerNames,
        IReadOnlyList<string>? cleanReturnNames = null,
        IReadOnlyList<string>? positionCodes = null)
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
                slot,
                PositionOf(positionCodes, slot)))
            .ToArray();

        var outs = outSlots
            .Select(slot => new MatchDayLineupChip(
                NameOf(playerNames, slot),
                MarkerOut,
                slot,
                PositionOf(positionCodes, slot)))
            .ToArray();

        var returns = outs.Length == 0
            ? cleanReturnNames ?? Array.Empty<string>()
            : Array.Empty<string>();
        var who = FormatReturnWho(returns);

        var caption = outs.Length == 0
            ? returns.Count > 0
                ? isApproved
                    ? $"Temiz XI — {who} döndü"
                    : $"Temiz XI (onayla) — {who} döndü"
                : isApproved
                    ? "Onaylı XI"
                    : "Taslak XI (onayla kilitle)"
            : isApproved
                ? $"Onaylı XI · {outs.Length} sakat dışarı"
                : $"Taslak XI · onayda {outs.Length} sakat dışarı";

        return new MatchDayLineupStrip(
            true,
            isApproved,
            caption,
            xi,
            outs,
            returns.Count > 0 ? returns : null);
    }

    /// <summary>
    /// Maç sonucu köprüsü — geçmiş zaman: sahaya böyle çıktın.
    /// </summary>
    public string ResultBridgeCaption =>
        !HasMatch || StartingXi.Count == 0
            ? Caption
            : HasCleanReturn
                ? $"Sahaya temiz XI ile çıktın · {FormatReturnWho(ReturnedNames)} döndü"
                : OutPlayers.Count == 0
                    ? "Sahaya bu XI ile çıktın"
                    : $"Sahaya bu XI ile çıktın · {OutPlayers.Count} sakat dışarıda";

    /// <summary>
    /// Devre arası — kim sahada, sakat hatırlatması.
    /// </summary>
    public string HalfTimeBridgeCaption =>
        !HasMatch || StartingXi.Count == 0
            ? Caption
            : HasCleanReturn
                ? $"Temiz XI — {FormatReturnWho(ReturnedNames)} döndü; değişiklik yine XI↔Yedek"
                : OutPlayers.Count == 0
                    ? "Sahadaki XI — bir değişiklik XI↔Yedek ile"
                    : $"Sahadaki XI · {OutPlayers.Count} sakat dışarıda — değişiklik düşün";

    public string? ResultBridgeBeatLine()
    {
        if (!HasMatch)
        {
            return null;
        }

        if (HasCleanReturn)
        {
            return $"Temiz XI: {FormatReturnWho(ReturnedNames)} döndü";
        }

        if (OutPlayers.Count == 0)
        {
            return null;
        }

        var outs = string.Join(", ", OutPlayers.Take(2).Select(c => "× " + ShortLast(c.DisplayName)));
        var ins = StartingXi.Where(c => c.IsIn).Take(2).Select(c => "↑ " + ShortLast(c.DisplayName));
        var inPart = ins.Any() ? " · " + string.Join(", ", ins) : string.Empty;
        return $"Böyle çıktın: {outs}{inPart}";
    }

    private static string FormatReturnWho(IReadOnlyList<string> names) =>
        names.Count == 0
            ? "Sakatlar"
            : string.Join(", ", names.Take(2).Select(ShortLast));

    private static string NameOf(IReadOnlyList<string> playerNames, int slotIndex) =>
        slotIndex >= 0 && slotIndex < playerNames.Count
            ? playerNames[slotIndex]
            : $"#{slotIndex + 1}";

    private static string PositionOf(IReadOnlyList<string>? positionCodes, int slotIndex) =>
        positionCodes is not null && slotIndex >= 0 && slotIndex < positionCodes.Count
            ? positionCodes[slotIndex]
            : string.Empty;

    private static string ShortLast(string full)
    {
        if (string.IsNullOrWhiteSpace(full))
        {
            return "?";
        }

        var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^1] : parts[0];
    }
}

public sealed record MatchDayLineupChip(
    string DisplayName,
    string MarkerCode,
    int SlotIndex,
    string PositionCode = "")
{
    public bool IsIn => string.Equals(MarkerCode, MatchDayLineupStrip.MarkerIn, StringComparison.Ordinal);
    public bool IsOut => string.Equals(MarkerCode, MatchDayLineupStrip.MarkerOut, StringComparison.Ordinal);

    public string ChipLabel
    {
        get
        {
            var name = IsIn ? $"↑ {ShortName(DisplayName)}"
                : IsOut ? $"× {ShortName(DisplayName)}"
                : ShortName(DisplayName);
            return string.IsNullOrWhiteSpace(PositionCode)
                ? name
                : $"{name} · {PositionCode}";
        }
    }

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
