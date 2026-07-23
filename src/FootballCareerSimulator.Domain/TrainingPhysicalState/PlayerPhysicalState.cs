using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Domain.TrainingPhysicalState;

/// <summary>
/// Slot bazlı yorgunluk/fitness (gerçek PlayerId yok; MatchSelection slot modeli ile hizalı).
/// </summary>
public sealed class PlayerPhysicalState
{
    public const int MinLevel = 0;
    public const int MaxLevel = 100;
    public const int DefaultFatigue = 15;
    public const int DefaultFitness = 80;

    private PlayerPhysicalState(ClubId clubId, int slotIndex, int fatigue, int fitness)
    {
        ClubId = clubId;
        SlotIndex = slotIndex;
        Fatigue = fatigue;
        Fitness = fitness;
    }

    public ClubId ClubId { get; }

    public int SlotIndex { get; }

    public int Fatigue { get; }

    public int Fitness { get; }

    public static PlayerPhysicalState CreateRested(ClubId clubId, int slotIndex)
    {
        EnsureSlot(slotIndex);
        return new PlayerPhysicalState(clubId, slotIndex, DefaultFatigue, DefaultFitness);
    }

    public static PlayerPhysicalState Rehydrate(ClubId clubId, int slotIndex, int fatigue, int fitness)
    {
        EnsureSlot(slotIndex);
        EnsureLevel(fatigue, nameof(fatigue));
        EnsureLevel(fitness, nameof(fitness));
        return new PlayerPhysicalState(clubId, slotIndex, fatigue, fitness);
    }

    public PlayerPhysicalState WithLevels(int fatigue, int fitness)
    {
        EnsureLevel(fatigue, nameof(fatigue));
        EnsureLevel(fitness, nameof(fitness));
        return new PlayerPhysicalState(ClubId, SlotIndex, fatigue, fitness);
    }

    private static void EnsureSlot(int slotIndex)
    {
        if (slotIndex is < MatchSelection.MinSquadSlot or > MatchSelection.MaxSquadSlot)
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                $"Squad slot {slotIndex} is out of range ({MatchSelection.MinSquadSlot}-{MatchSelection.MaxSquadSlot}).");
        }
    }

    private static void EnsureLevel(int value, string name)
    {
        if (value is < MinLevel or > MaxLevel)
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                $"{name} must be between {MinLevel} and {MaxLevel}.");
        }
    }
}
