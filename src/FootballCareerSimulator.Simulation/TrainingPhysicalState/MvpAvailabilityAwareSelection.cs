using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

/// <summary>
/// Varsayılan XI: uygun slotları öne alır; sakatları yedek/dışarıda bırakır.
/// İlk 11'de unavailable slot kabul edilmez.
/// </summary>
public static class MvpAvailabilityAwareSelection
{
    /// <summary>
    /// Naive varsayılan XI ile uygunluk-öncelikli XI arasındaki otomatik değişimler
    /// (dışarı itilen sakat → yerine giren müsait).
    /// </summary>
    public readonly record struct AvailabilityAutoSwap(int OutSlotIndex, int InSlotIndex);

    public static IReadOnlyList<AvailabilityAutoSwap> PreviewDefaultAvailabilitySwaps(
        ClubId clubId,
        GameDate day,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot,
        ClubSquad? clubSquad = null) =>
        TryPreviewPreferredStartingXi(clubId, day, physicalBySlot, clubSquad, out _, out var swaps)
            ? swaps
            : Array.Empty<AvailabilityAutoSwap>();

    /// <summary>
    /// Onayda seçilecek sakatsız XI + naive XI'ye göre auto-swap çiftleri.
    /// Yeterli müsait yoksa false.
    /// </summary>
    public static bool TryPreviewPreferredStartingXi(
        ClubId clubId,
        GameDate day,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot,
        ClubSquad? clubSquad,
        out IReadOnlyList<int> startingSlotIndices,
        out IReadOnlyList<AvailabilityAutoSwap> swaps)
    {
        ArgumentNullException.ThrowIfNull(physicalBySlot);

        var candidateSlots = ResolveCandidateSlots(clubSquad);
        var available = new List<int>();
        var unavailable = new List<int>();
        PartitionByAvailability(clubId, day, candidateSlots, physicalBySlot, available, unavailable);

        if (available.Count < MatchSelection.StartingXiSize)
        {
            startingSlotIndices = Array.Empty<int>();
            swaps = Array.Empty<AvailabilityAutoSwap>();
            return false;
        }

        var preferredStarting = SelectBalancedStartingSlots(clubId, available);
        startingSlotIndices = preferredStarting;

        if (unavailable.Count == 0)
        {
            swaps = Array.Empty<AvailabilityAutoSwap>();
            return true;
        }

        // Yalnız sakat/naive XI farkını raporla; rol dengelemesi injury swap sayılmaz.
        var naiveStarting = candidateSlots.Take(MatchSelection.StartingXiSize).ToArray();
        var swappedOut = naiveStarting.Where(unavailable.Contains).ToArray();
        var swappedIn = preferredStarting
            .Where(slot => !naiveStarting.Contains(slot))
            .Take(swappedOut.Length)
            .ToArray();
        var count = Math.Min(swappedOut.Length, swappedIn.Length);
        if (count == 0)
        {
            swaps = Array.Empty<AvailabilityAutoSwap>();
            return true;
        }

        var pairList = new AvailabilityAutoSwap[count];
        for (var i = 0; i < count; i++)
        {
            pairList[i] = new AvailabilityAutoSwap(swappedOut[i], swappedIn[i]);
        }

        swaps = pairList;
        return true;
    }

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

        var candidateSlots = ResolveCandidateSlots(clubSquad);
        var available = new List<int>();
        var unavailable = new List<int>();
        PartitionByAvailability(clubId, day, candidateSlots, physicalBySlot, available, unavailable);

        if (available.Count < MatchSelection.StartingXiSize)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Not enough available players for starting XI ({available.Count}/{MatchSelection.StartingXiSize}; "
                + $"squad {candidateSlots.Length}, unavailable {unavailable.Count}, "
                + $"club {clubId.Value}, day {day.DayNumber}).");
        }

        var starting = SelectBalancedStartingSlots(clubId, available);
        var bench = available
            .Where(slot => !starting.Contains(slot))
            .Concat(unavailable)
            .Take(MatchSelection.MaxBenchSize)
            .ToArray();

        EnsureStartingXiAvailable(clubId, starting, day, physicalBySlot);
        return MatchSelection.Approve(fixtureId, clubId, starting, bench, clubSquad);
    }

    public static MatchSelection ApproveReusingPreviousPreferringAvailable(
        FixtureId fixtureId,
        ClubId clubId,
        GameDate day,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot,
        IReadOnlyList<int> previousStartingSlotIndices,
        IReadOnlyList<int> previousBenchSlotIndices,
        ClubSquad? clubSquad = null)
    {
        ArgumentNullException.ThrowIfNull(physicalBySlot);
        ArgumentNullException.ThrowIfNull(previousStartingSlotIndices);
        ArgumentNullException.ThrowIfNull(previousBenchSlotIndices);

        var candidateSlots = ResolveCandidateSlots(clubSquad);
        var candidateSet = candidateSlots.ToHashSet();
        var available = new List<int>();
        var unavailable = new List<int>();
        PartitionByAvailability(clubId, day, candidateSlots, physicalBySlot, available, unavailable);
        var availableSet = available.ToHashSet();

        if (available.Count < MatchSelection.StartingXiSize)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Not enough available players for starting XI ({available.Count}/{MatchSelection.StartingXiSize}; "
                + $"squad {candidateSlots.Length}, unavailable {unavailable.Count}, "
                + $"club {clubId.Value}, day {day.DayNumber}).");
        }

        var starting = new List<int>(MatchSelection.StartingXiSize);
        foreach (var slot in previousStartingSlotIndices)
        {
            if (starting.Count >= MatchSelection.StartingXiSize)
            {
                break;
            }

            if (availableSet.Contains(slot) && candidateSet.Contains(slot) && !starting.Contains(slot))
            {
                starting.Add(slot);
            }
        }

        foreach (var slot in previousBenchSlotIndices.Concat(available))
        {
            if (starting.Count >= MatchSelection.StartingXiSize)
            {
                break;
            }

            if (availableSet.Contains(slot) && candidateSet.Contains(slot) && !starting.Contains(slot))
            {
                starting.Add(slot);
            }
        }

        var used = starting.ToHashSet();
        var bench = previousBenchSlotIndices
            .Concat(previousStartingSlotIndices)
            .Concat(available)
            .Concat(unavailable)
            .Where(slot => candidateSet.Contains(slot) && !used.Contains(slot))
            .Distinct()
            .Take(MatchSelection.MaxBenchSize)
            .ToArray();

        EnsureStartingXiAvailable(clubId, starting, day, physicalBySlot);
        return MatchSelection.Approve(fixtureId, clubId, starting, bench, clubSquad);
    }

    public static IReadOnlyList<AvailabilityAutoSwap> DiffStartingSlots(
        IReadOnlyList<int> previousStartingSlotIndices,
        IReadOnlyList<int> nextStartingSlotIndices)
    {
        ArgumentNullException.ThrowIfNull(previousStartingSlotIndices);
        ArgumentNullException.ThrowIfNull(nextStartingSlotIndices);

        var previous = previousStartingSlotIndices.ToHashSet();
        var next = nextStartingSlotIndices.ToHashSet();
        var swappedOut = previousStartingSlotIndices.Where(slot => !next.Contains(slot)).ToArray();
        var swappedIn = nextStartingSlotIndices.Where(slot => !previous.Contains(slot)).ToArray();
        var count = Math.Min(swappedOut.Length, swappedIn.Length);
        if (count == 0)
        {
            return Array.Empty<AvailabilityAutoSwap>();
        }

        var swaps = new AvailabilityAutoSwap[count];
        for (var i = 0; i < count; i++)
        {
            swaps[i] = new AvailabilityAutoSwap(swappedOut[i], swappedIn[i]);
        }

        return swaps;
    }

    private static int[] ResolveCandidateSlots(ClubSquad? clubSquad) =>
        clubSquad is not null && clubSquad.Members.Count > 0
            ? clubSquad.Members.Select(m => m.SlotIndex).OrderBy(s => s).ToArray()
            : Enumerable.Range(
                    MatchSelection.MinSquadSlot,
                    MatchSelection.MaxSquadSlot - MatchSelection.MinSquadSlot + 1)
                .ToArray();

    private static void PartitionByAvailability(
        ClubId clubId,
        GameDate day,
        IReadOnlyList<int> candidateSlots,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot,
        List<int> available,
        List<int> unavailable)
    {
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
    }

    private static int[] SelectBalancedStartingSlots(ClubId clubId, IReadOnlyList<int> available)
    {
        var profiles = MvpSquadRosterGenerator.GeneratePlayerProfiles(clubId, rootSeed: 0);
        var eligible = available
            .Where(slot => slot >= 0 && slot < profiles.Count)
            .Select(slot => (Slot: slot, Profile: profiles[slot]))
            .ToArray();
        var roleFit = MvpLineupRoleFitCalculator.Evaluate(
            Formation.F442,
            eligible.Select(item => item.Profile).ToArray());
        var naturalSlots = roleFit.PlayerByRoleIndex
            .Where(index => index >= 0 && index < eligible.Length)
            .Select(index => eligible[index].Slot)
            .Distinct()
            .ToArray();

        return naturalSlots
            .Concat(available.Where(slot => !naturalSlots.Contains(slot)))
            .Take(MatchSelection.StartingXiSize)
            .ToArray();
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
