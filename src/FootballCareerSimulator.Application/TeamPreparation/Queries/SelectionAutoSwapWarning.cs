using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

/// <summary>
/// Onay öncesi / sonrası sakat→yedek otomatik değişim metinleri.
/// </summary>
public static class SelectionAutoSwapWarning
{
    public static IReadOnlyList<string> FormatBeatLines(
        IReadOnlyList<MvpAvailabilityAwareSelection.AvailabilityAutoSwap> swaps,
        IReadOnlyList<string> playerNames,
        int maxPairs = 2)
    {
        ArgumentNullException.ThrowIfNull(swaps);
        ArgumentNullException.ThrowIfNull(playerNames);

        if (swaps.Count == 0 || maxPairs <= 0)
        {
            return Array.Empty<string>();
        }

        return swaps
            .Take(maxPairs)
            .Select(swap =>
                $"Sakat XI'de: {NameOf(playerNames, swap.OutSlotIndex)}"
                + $" — yerine {NameOf(playerNames, swap.InSlotIndex)}.")
            .ToArray();
    }

    public static string? FormatToastSuffix(
        IReadOnlyList<MvpAvailabilityAwareSelection.AvailabilityAutoSwap> swaps,
        IReadOnlyList<string> playerNames,
        int maxPairs = 2)
    {
        ArgumentNullException.ThrowIfNull(swaps);
        ArgumentNullException.ThrowIfNull(playerNames);

        if (swaps.Count == 0 || maxPairs <= 0)
        {
            return null;
        }

        var pairs = swaps
            .Take(maxPairs)
            .Select(swap =>
                $"{NameOf(playerNames, swap.OutSlotIndex)}→{NameOf(playerNames, swap.InSlotIndex)}");
        return "sakatlar dışarı (" + string.Join(", ", pairs) + ")";
    }

    /// <summary>
    /// XI↔Yedek değişimi — oyuncu dilinde toast.
    /// </summary>
    public static string FormatSubstitution(string outPlayerName, string inPlayerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outPlayerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(inPlayerName);
        return $"{outPlayerName} çıktı · {inPlayerName} XI'ye girdi";
    }

    public static string FormatSubstitution(
        int outSlotIndex,
        int inSlotIndex,
        IReadOnlyList<string> playerNames) =>
        FormatSubstitution(NameOf(playerNames, outSlotIndex), NameOf(playerNames, inSlotIndex));

    /// <summary>
    /// Sonuç köprüsü — HT değişimi.
    /// </summary>
    public static string FormatHalfTimeBridge(string outPlayerName, string inPlayerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outPlayerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(inPlayerName);
        return $"Devre arasında {outPlayerName}↔{inPlayerName}.";
    }

    public static string FormatHalfTimeBridge(
        int outSlotIndex,
        int inSlotIndex,
        IReadOnlyList<string> playerNames) =>
        FormatHalfTimeBridge(NameOf(playerNames, outSlotIndex), NameOf(playerNames, inSlotIndex));

    /// <summary>
    /// Maç gecesi Anlar — HT değişimi anahtar anı.
    /// </summary>
    public static string FormatHalfTimeKeyMoment(string outPlayerName, string inPlayerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outPlayerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(inPlayerName);
        return $"46' Değişiklik · {outPlayerName}↔{inPlayerName}";
    }

    public static string? FormatHalfTimeKeyMomentFromBridge(string? bridgeLine)
    {
        if (string.IsNullOrWhiteSpace(bridgeLine))
        {
            return null;
        }

        const string prefix = "Devre arasında ";
        if (!bridgeLine.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !bridgeLine.Contains('↔', StringComparison.Ordinal))
        {
            return null;
        }

        var core = bridgeLine[prefix.Length..].Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(core)
            ? null
            : $"46' Değişiklik · {core}";
    }

    /// <summary>
    /// İlk yarı anlarından sonra, ikinci yarıdan önce yerleştir.
    /// </summary>
    public static void InsertHalfTimeKeyMoment(IList<string> beatLines, string momentLine)
    {
        ArgumentNullException.ThrowIfNull(beatLines);
        ArgumentException.ThrowIfNullOrWhiteSpace(momentLine);

        var insertAt = 0;
        for (var i = 0; i < beatLines.Count; i++)
        {
            var tick = beatLines[i].IndexOf('\'', StringComparison.Ordinal);
            if (tick <= 0)
            {
                continue;
            }

            if (int.TryParse(beatLines[i].AsSpan(0, tick), out var minute) && minute < 46)
            {
                insertAt = i + 1;
            }
        }

        beatLines.Insert(insertAt, momentLine);
    }

    private static string NameOf(IReadOnlyList<string> playerNames, int slotIndex) =>
        slotIndex >= 0 && slotIndex < playerNames.Count
            ? playerNames[slotIndex]
            : $"Oyuncu #{slotIndex + 1}";
}
