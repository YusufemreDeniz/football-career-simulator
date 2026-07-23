using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

/// <summary>
/// Varsayılan XI: uygun slotları öne alır; sakatları yedek/dışarıda bırakır.
/// </summary>
public static class MvpAvailabilityAwareSelection
{
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

        var ordered = available.Concat(unavailable).ToArray();
        if (ordered.Length < MatchSelection.StartingXiSize)
        {
            return MatchSelection.ApproveDefault(fixtureId, clubId, clubSquad);
        }

        var starting = ordered.Take(MatchSelection.StartingXiSize).ToArray();
        var bench = ordered
            .Skip(MatchSelection.StartingXiSize)
            .Take(MatchSelection.MaxBenchSize)
            .ToArray();

        return MatchSelection.Approve(fixtureId, clubId, starting, bench, clubSquad);
    }
}
