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

    private static string NameOf(IReadOnlyList<string> playerNames, int slotIndex) =>
        slotIndex >= 0 && slotIndex < playerNames.Count
            ? playerNames[slotIndex]
            : $"Oyuncu #{slotIndex + 1}";
}
