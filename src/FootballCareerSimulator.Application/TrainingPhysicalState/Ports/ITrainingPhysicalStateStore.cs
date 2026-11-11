using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Ports;

public interface ITrainingPhysicalStateStore
{
    IReadOnlyList<WeeklyTrainingPlan> Plans { get; }

    IReadOnlyList<PlayerPhysicalState> PhysicalStates { get; }

    WeeklyTrainingPlan? GetPlan(ClubId clubId);

    PlayerPhysicalState? GetPhysical(PlayerId playerId);

    /// <summary>Denormalize konum üzerinden lookup (squad slot köprüsü).</summary>
    PlayerPhysicalState? GetPhysical(ClubId clubId, int slotIndex);

    IReadOnlyList<PlayerPhysicalState> GetPhysicalStatesForClub(ClubId clubId) =>
        PhysicalStates
            .Where(state => state.ClubId == clubId)
            .OrderBy(state => state.PlayerId.Value)
            .ToArray();

    int RecoverDueInjuries(GameDate day)
    {
        var recoveredCount = 0;
        foreach (var state in PhysicalStates.Where(s => s.IsInjured).ToArray())
        {
            var recovered = state.RecoverIfDue(day);
            if (!ReferenceEquals(recovered, state))
            {
                UpsertPhysical(recovered);
                recoveredCount++;
            }
        }

        return recoveredCount;
    }

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
