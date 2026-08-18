using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

/// <summary>
/// Varsayılan kadro onayında aktif sözleri mevcut yerleşim kurallarıyla onurlandırır.
/// İlk 11 sözü → XI; forma süresi sözü → maç günü kadrosu (XI ∪ yedek). Yeni eşik/formül yok.
/// </summary>
public static class PromiseAwareDefaultSelection
{
    public static MatchSelection Honor(
        MatchSelection selection,
        ClubSquad? squad,
        IPromiseStore? promises,
        ClubId clubId,
        GameDate? day = null,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>? physicalBySlot = null)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (promises is null || squad is null || squad.Members.Count == 0)
        {
            return selection;
        }

        var slotByPlayer = squad.Members.ToDictionary(m => m.PlayerId.Value, m => m.SlotIndex);
        var startingNeeded = ResolvePromisedSlots(
            promises,
            clubId,
            slotByPlayer,
            PromiseKind.StartingOpportunity,
            day,
            physicalBySlot);
        var matchdayNeeded = ResolvePromisedSlots(
            promises,
            clubId,
            slotByPlayer,
            PromiseKind.PlayingTime,
            day,
            physicalBySlot);

        if (startingNeeded.Count == 0 && matchdayNeeded.Count == 0)
        {
            return selection;
        }

        var starting = selection.StartingSlotIndices.ToList();
        var bench = selection.BenchSlotIndices.ToList();

        foreach (var slot in startingNeeded)
        {
            PlaceInStarting(starting, bench, slot, startingNeeded);
        }

        foreach (var slot in matchdayNeeded)
        {
            PlaceOnMatchday(starting, bench, slot, startingNeeded, matchdayNeeded);
        }

        return MatchSelection.Approve(
            selection.FixtureId,
            selection.ClubId,
            starting,
            bench,
            squad);
    }

    public static string? FormatHonorNote(
        MatchSelection before,
        MatchSelection after,
        IReadOnlyList<string> playerNames)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(playerNames);

        var movedIn = after.StartingSlotIndices
            .Where(slot => !before.StartingSlotIndices.Contains(slot))
            .Take(2)
            .Select(slot => NameOf(playerNames, slot))
            .ToArray();
        if (movedIn.Length == 0)
        {
            var ontoBench = after.BenchSlotIndices
                .Where(slot =>
                    !before.StartingSlotIndices.Contains(slot)
                    && !before.BenchSlotIndices.Contains(slot))
                .Take(2)
                .Select(slot => NameOf(playerNames, slot))
                .ToArray();
            return ontoBench.Length == 0
                ? null
                : "söz için kadro: " + string.Join(", ", ontoBench);
        }

        return "söz için XI: " + string.Join(", ", movedIn);
    }

    private static IReadOnlyList<int> ResolvePromisedSlots(
        IPromiseStore promises,
        ClubId clubId,
        IReadOnlyDictionary<long, int> slotByPlayer,
        PromiseKind kind,
        GameDate? day,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>? physicalBySlot) =>
        promises.Promises
            .Where(p =>
                p.IsActive
                && p.Kind == kind
                && p.ClubId == clubId
                && p.Promisee.Kind == ActorKind.Player)
            .OrderBy(p => p.DeadlineOn.DayNumber)
            .ThenBy(p => p.PromiseId.Value)
            .Select(p => slotByPlayer.TryGetValue(p.Promisee.Id, out var slot) ? slot : (int?)null)
            .Where(slot => slot is int value && IsAvailable(clubId, value, day, physicalBySlot))
            .Select(slot => slot!.Value)
            .Distinct()
            .ToArray();

    private static bool IsAvailable(
        ClubId clubId,
        int slot,
        GameDate? day,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>? physicalBySlot) =>
        physicalBySlot is null
        || day is null
        || !physicalBySlot.TryGetValue((clubId.Value, slot), out var state)
        || state.IsAvailableOn(day.Value);

    private static void PlaceInStarting(
        List<int> starting,
        List<int> bench,
        int slot,
        IReadOnlyCollection<int> protectedStarting)
    {
        if (starting.Contains(slot))
        {
            return;
        }

        var victimIndex = starting.FindLastIndex(s => !protectedStarting.Contains(s));
        if (victimIndex < 0)
        {
            return;
        }

        var victim = starting[victimIndex];
        starting[victimIndex] = slot;
        var benchIndex = bench.IndexOf(slot);
        if (benchIndex >= 0)
        {
            bench[benchIndex] = victim;
            return;
        }

        if (bench.Count < MatchSelection.MaxBenchSize)
        {
            bench.Add(victim);
            return;
        }

        var dropIndex = bench.FindLastIndex(s => !protectedStarting.Contains(s));
        if (dropIndex >= 0)
        {
            bench[dropIndex] = victim;
        }
    }

    private static void PlaceOnMatchday(
        List<int> starting,
        List<int> bench,
        int slot,
        IReadOnlyCollection<int> protectedStarting,
        IReadOnlyCollection<int> protectedMatchday)
    {
        if (starting.Contains(slot) || bench.Contains(slot))
        {
            return;
        }

        if (bench.Count < MatchSelection.MaxBenchSize)
        {
            bench.Add(slot);
            return;
        }

        var dropIndex = bench.FindLastIndex(s =>
            !protectedStarting.Contains(s) && !protectedMatchday.Contains(s));
        if (dropIndex >= 0)
        {
            bench[dropIndex] = slot;
        }
    }

    private static string NameOf(IReadOnlyList<string> playerNames, int slotIndex) =>
        slotIndex >= 0 && slotIndex < playerNames.Count
            ? playerNames[slotIndex]
            : "oyuncu";
}
