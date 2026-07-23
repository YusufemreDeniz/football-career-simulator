using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Ports;

public interface ITrainingPhysicalStateStore
{
    IReadOnlyList<WeeklyTrainingPlan> Plans { get; }

    IReadOnlyList<PlayerPhysicalState> PhysicalStates { get; }

    WeeklyTrainingPlan? GetPlan(ClubId clubId);

    PlayerPhysicalState? GetPhysical(ClubId clubId, int slotIndex);

    IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> PhysicalBySlot { get; }

    void UpsertPlan(WeeklyTrainingPlan plan);

    void ReplacePhysicalStatesForClub(ClubId clubId, IEnumerable<PlayerPhysicalState> states);

    void ReplaceAll(
        IEnumerable<WeeklyTrainingPlan> plans,
        IEnumerable<PlayerPhysicalState> physicalStates);
}
