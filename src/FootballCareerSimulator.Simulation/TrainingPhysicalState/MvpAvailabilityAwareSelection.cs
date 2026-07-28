using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

/// <summary>
/// Varsayılan XI: uygun slotları öne alır; sakatları yedek/dışarıda bırakır.
/// İlk 11'de unavailable slot kabul edilmez.
/// </summary>
public static class MvpAvailabilityAwareSelection
{
    public static void EnsureStartingXiAvailable(
        ClubId clubId,
        IReadOnlyList<int> startingSlotIndices,
        GameDate day,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot)
    {
        ArgumentNullException.ThrowIfNull(startingSlotIndices);
        ArgumentNullException.ThrowIfNull(physicalBySlot);

        foreach (var slot in startingSlotIndices)
        {
            if (physicalBySlot.TryGetValue((clubId.Value, slot), out var state)
                && !state.IsAvailableOn(day))
            {
                throw new TeamPreparationInvariantViolationException(
                    $"Starting XI cannot include unavailable slot {slot}.");
            }
        }
    }

    public static bool HasUnavailableStarter(
        ClubId clubId,
        IReadOnlyList<int> startingSlotIndices,
        GameDate day,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot)
    {
        ArgumentNullException.ThrowIfNull(startingSlotIndices);
        ArgumentNullException.ThrowIfNull(physicalBySlot);

        foreach (var slot in startingSlotIndices)
        {
            if (physicalBySlot.TryGetValue((clubId.Value, slot), out var state)
                && !state.IsAvailableOn(day))
            {
                return true;
            }
        }

        return false;
    }

    public static MatchSelection ApproveDefaultPreferringAvailable(
        FixtureId fixtureId,
        ClubId clubId,
        GameDate day,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot,
        ClubSquad? clubSquad = null)
    {
        ArgumentNullException.ThrowIfNull(physicalBySlot);

        var candidateSlots = clubSquad is not null && clubSquad.Members.Count > 0
            ? clubSquad.Members.Select(m => m.SlotIndex).OrderBy(s => s).ToArray()
            : Enumerable.Range(MatchSelection.MinSquadSlot, MatchSelection.MaxSquadSlot - MatchSelection.MinSquadSlot + 1)
                .ToArray();

        var available = new List<int>();
        var unavailable = new List<int>();

        foreach (var slot in candidateSlots)
        {
            if (physicalBySlot.TryGetValue((clubId.Value, slot), out var state)
                && !state.IsAvailableOn(day))
            {
                unavailable.Add(slot);
            }
            else
            {
                available.Add(slot);
            }
        }

        if (available.Count < MatchSelection.StartingXiSize)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Not enough available players for starting XI ({available.Count}/{MatchSelection.StartingXiSize}).");
        }

        var ordered = available.Concat(unavailable).ToArray();
        var starting = ordered.Take(MatchSelection.StartingXiSize).ToArray();
        var bench = ordered
            .Skip(MatchSelection.StartingXiSize)
            .Take(MatchSelection.MaxBenchSize)
            .ToArray();

        EnsureStartingXiAvailable(clubId, starting, day, physicalBySlot);
        return MatchSelection.Approve(fixtureId, clubId, starting, bench, clubSquad);
    }

    public static MatchSelection SwapStarterWithBench(
        MatchSelection selection,
        int startingIndex,
        int benchIndex,
        GameDate day,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>? physicalBySlot,
        ClubSquad? clubSquad = null)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (startingIndex is < 0 or >= MatchSelection.StartingXiSize)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Starting index must be between 0 and {MatchSelection.StartingXiSize - 1}.");
        }

        if (benchIndex < 0 || benchIndex >= selection.BenchSlotIndices.Count)
        {
            throw new TeamPreparationInvariantViolationException(
                "Bench index is out of range for current selection.");
        }

        var starting = selection.StartingSlotIndices.ToArray();
        var bench = selection.BenchSlotIndices.ToArray();
        (starting[startingIndex], bench[benchIndex]) = (bench[benchIndex], starting[startingIndex]);

        if (physicalBySlot is not null)
        {
            EnsureStartingXiAvailable(selection.ClubId, starting, day, physicalBySlot);
        }

        return MatchSelection.Approve(
            selection.FixtureId,
            selection.ClubId,
            starting,
            bench,
            clubSquad);
    }
}
