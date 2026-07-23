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
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot)
    {
        ArgumentNullException.ThrowIfNull(physicalBySlot);

        var available = new List<int>();
        var unavailable = new List<int>();

        for (var slot = MatchSelection.MinSquadSlot; slot <= MatchSelection.MaxSquadSlot; slot++)
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
            return MatchSelection.ApproveDefault(fixtureId, clubId);
        }

        var starting = ordered.Take(MatchSelection.StartingXiSize).ToArray();
        var bench = ordered
            .Skip(MatchSelection.StartingXiSize)
            .Take(MatchSelection.MaxBenchSize)
            .ToArray();

        return MatchSelection.Approve(fixtureId, clubId, starting, bench);
    }
}
