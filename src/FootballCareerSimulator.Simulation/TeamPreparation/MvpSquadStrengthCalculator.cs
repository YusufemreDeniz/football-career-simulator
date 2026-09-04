using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Simulation.TeamPreparation;

/// <summary>
/// Slot bazlı deterministik oyuncu gücü ve ilk 11 bonus hesabı.
/// </summary>
public static class MvpSquadStrengthCalculator
{
    public const int MinRating = 40;
    public const int MaxRating = 90;

    public static int GetPlayerRating(ClubId clubId, int rootSeed, int slotIndex)
    {
        if (slotIndex is < MatchSelection.MinSquadSlot or > MatchSelection.MaxSquadSlot)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }

        var rng = new SimulationRandomContext(
            unchecked((rootSeed * 641) ^ ((int)clubId.Value * 31) ^ (slotIndex * 997)));
        return rng.NextInt(MinRating, MaxRating + 1);
    }

    public static int GetEffectiveRating(
        ClubId clubId,
        int rootSeed,
        int slotIndex,
        int? currentAbilityOverride)
    {
        if (currentAbilityOverride is int ability)
        {
            return Math.Clamp(ability, MinRating, MaxRating);
        }

        return GetPlayerRating(clubId, rootSeed, slotIndex);
    }

    /// <summary>
    /// İlk 11 ortalamasının 65'e göre sapması; -10..+10 aralığında maç gücüne eklenir.
    /// </summary>
    public static int ComputeLineupBonus(
        ClubId clubId,
        int rootSeed,
        IReadOnlyList<int> startingSlotIndices,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), int>? abilityBySlot = null)
    {
        ArgumentNullException.ThrowIfNull(startingSlotIndices);
        if (startingSlotIndices.Count == 0)
        {
            return 0;
        }

        var average = startingSlotIndices.Average(slot =>
        {
            int? overrideAbility = null;
            if (abilityBySlot is not null
                && abilityBySlot.TryGetValue((clubId.Value, slot), out var ability))
            {
                overrideAbility = ability;
            }

            return GetEffectiveRating(clubId, rootSeed, slot, overrideAbility);
        });
        var bonus = (int)Math.Round(average - 65.0, MidpointRounding.AwayFromZero);
        return Math.Clamp(bonus, -10, 10);
    }

    public static int ComputeDefaultLineupBonus(
        ClubId clubId,
        int rootSeed,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), int>? abilityBySlot = null) =>
        ComputeLineupBonus(
            clubId,
            rootSeed,
            Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray(),
            abilityBySlot);
}
