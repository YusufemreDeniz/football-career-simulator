using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;

public sealed class InMemoryTrainingPhysicalStateStore : ITrainingPhysicalStateStore
{
    private readonly Dictionary<long, WeeklyTrainingPlan> _plans = new();
    private readonly Dictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> _physical = new();

    public IReadOnlyList<WeeklyTrainingPlan> Plans =>
        _plans.Values.OrderBy(p => p.ClubId.Value).ToArray();

    public IReadOnlyList<PlayerPhysicalState> PhysicalStates =>
        _physical.Values
            .OrderBy(s => s.ClubId.Value)
            .ThenBy(s => s.SlotIndex)
            .ToArray();

    public IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> PhysicalBySlot =>
        _physical;

    public WeeklyTrainingPlan? GetPlan(ClubId clubId) =>
        _plans.TryGetValue(clubId.Value, out var plan) ? plan : null;

    public PlayerPhysicalState? GetPhysical(ClubId clubId, int slotIndex) =>
        _physical.TryGetValue((clubId.Value, slotIndex), out var state) ? state : null;

    public void UpsertPlan(WeeklyTrainingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _plans[plan.ClubId.Value] = plan;
    }

    public void ReplacePhysicalStatesForClub(ClubId clubId, IEnumerable<PlayerPhysicalState> states)
    {
        ArgumentNullException.ThrowIfNull(states);

        var keys = _physical.Keys.Where(key => key.ClubId == clubId.Value).ToArray();
        foreach (var key in keys)
        {
            _physical.Remove(key);
        }

        foreach (var state in states)
        {
            if (state.ClubId != clubId)
            {
                throw new ArgumentException(
                    $"Physical state club {state.ClubId.Value} does not match {clubId.Value}.",
                    nameof(states));
            }

            _physical[(state.ClubId.Value, state.SlotIndex)] = state;
        }
    }

    public void ReplaceAll(
        IEnumerable<WeeklyTrainingPlan> plans,
        IEnumerable<PlayerPhysicalState> physicalStates)
    {
        ArgumentNullException.ThrowIfNull(plans);
        ArgumentNullException.ThrowIfNull(physicalStates);

        _plans.Clear();
        _physical.Clear();

        foreach (var plan in plans)
        {
            UpsertPlan(plan);
        }

        foreach (var state in physicalStates)
        {
            _physical[(state.ClubId.Value, state.SlotIndex)] = state;
        }
    }
}
