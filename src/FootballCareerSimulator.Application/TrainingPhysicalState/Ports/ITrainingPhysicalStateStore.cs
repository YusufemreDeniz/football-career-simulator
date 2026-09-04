using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Ports;

public interface ITrainingPhysicalStateStore
{
    IReadOnlyList<WeeklyTrainingPlan> Plans { get; }

    IReadOnlyList<PlayerPhysicalState> PhysicalStates { get; }

    WeeklyTrainingPlan? GetPlan(ClubId clubId);

    PlayerPhysicalState? GetPhysical(PlayerId playerId);

    /// <summary>Denormalize konum üzerinden lookup (squad slot köprüsü).</summary>
    PlayerPhysicalState? GetPhysical(ClubId clubId, int slotIndex);

    IReadOnlyDictionary<long, PlayerPhysicalState> PhysicalByPlayerId { get; }

    /// <summary>
    /// Konumu olan state'lerin (ClubId, SlotIndex) görünümü — çağrı uyumu için.
    /// </summary>
    IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> PhysicalBySlot { get; }

    void UpsertPlan(WeeklyTrainingPlan plan);

    void UpsertPhysical(PlayerPhysicalState state);

    void RemovePhysical(PlayerId playerId);

    void ReplacePhysicalStatesForClub(ClubId clubId, IEnumerable<PlayerPhysicalState> states);

    void ReplaceAll(
        IEnumerable<WeeklyTrainingPlan> plans,
        IEnumerable<PlayerPhysicalState> physicalStates);
}
