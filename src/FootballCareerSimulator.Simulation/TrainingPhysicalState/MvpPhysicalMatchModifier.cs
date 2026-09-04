using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

/// <summary>
/// İlk 11 yorgunluk/fitness + sakatlık durumundan maç gücüne -5..+5 modifier.
/// </summary>
public static class MvpPhysicalMatchModifier
{
    public static int ComputeLineupModifier(
        ClubId clubId,
        IReadOnlyList<int> startingSlotIndices,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot,
        GameDate? asOfDay = null)
    {
        ArgumentNullException.ThrowIfNull(startingSlotIndices);
        ArgumentNullException.ThrowIfNull(physicalBySlot);

        if (startingSlotIndices.Count == 0)
        {
            return 0;
        }

        var fatigueSum = 0;
        var fitnessSum = 0;
        var counted = 0;
        var unavailableCount = 0;

        foreach (var slot in startingSlotIndices)
        {
            if (!physicalBySlot.TryGetValue((clubId.Value, slot), out var state))
            {
                continue;
            }

            if (asOfDay is GameDate day && !state.IsAvailableOn(day))
            {
                unavailableCount++;
                continue;
            }

            fatigueSum += state.Fatigue;
            fitnessSum += state.Fitness;
            counted++;
        }

        var injuryPenalty = Math.Min(5, unavailableCount * 2);
        if (counted == 0)
        {
            return -injuryPenalty;
        }

        var avgFatigue = fatigueSum / (double)counted;
        var avgFitness = fitnessSum / (double)counted;

        var raw = ((45.0 - avgFatigue) / 8.0) + ((avgFitness - 75.0) / 20.0) - injuryPenalty;
        return Math.Clamp((int)Math.Round(raw, MidpointRounding.AwayFromZero), -5, 5);
    }
}
