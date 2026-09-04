using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;

public sealed class InMemoryTrainingPhysicalStateStore : ITrainingPhysicalStateStore
{
    private readonly Dictionary<long, WeeklyTrainingPlan> _plans = new();
    private readonly Dictionary<long, PlayerPhysicalState> _physical = new();

    public IReadOnlyList<WeeklyTrainingPlan> Plans =>
        _plans.Values.OrderBy(p => p.ClubId.Value).ToArray();

    public IReadOnlyList<PlayerPhysicalState> PhysicalStates =>
        _physical.Values
            .OrderBy(s => s.PlayerId.Value)
            .ToArray();

    public IReadOnlyDictionary<long, PlayerPhysicalState> PhysicalByPlayerId => _physical;

    public IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> PhysicalBySlot
    {
        get
        {
            var located = _physical.Values.Where(state => state.HasLocation).ToArray();
            var duplicate = located
                .GroupBy(state => (state.ClubId!.Value.Value, state.SlotIndex!.Value))
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                var players = string.Join(
                    ",",
                    duplicate.Select(state => state.PlayerId.Value).OrderBy(id => id));
                throw new TrainingPhysicalStateInvariantViolationException(
                    $"Duplicate physical location ClubId={duplicate.Key.Item1} Slot={duplicate.Key.Item2} for players [{players}].");
            }

            return located.ToDictionary(
                state => (state.ClubId!.Value.Value, state.SlotIndex!.Value));
        }
    }

    public WeeklyTrainingPlan? GetPlan(ClubId clubId) =>
        _plans.TryGetValue(clubId.Value, out var plan) ? plan : null;

    public PlayerPhysicalState? GetPhysical(PlayerId playerId) =>
        _physical.TryGetValue(playerId.Value, out var state) ? state : null;

    public PlayerPhysicalState? GetPhysical(ClubId clubId, int slotIndex) =>
        _physical.Values.FirstOrDefault(state =>
            state.ClubId == clubId && state.SlotIndex == slotIndex);

    public void UpsertPlan(WeeklyTrainingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _plans[plan.ClubId.Value] = plan;
    }

    public void UpsertPhysical(PlayerPhysicalState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.HasLocation)
        {
            var club = state.ClubId!;
            var slot = state.SlotIndex!.Value;
            foreach (var other in _physical.Values
                .Where(candidate =>
                    candidate.PlayerId != state.PlayerId
                    && candidate.ClubId == club
                    && candidate.SlotIndex == slot)
                .ToArray())
            {
                _physical[other.PlayerId.Value] = other.ClearLocation();
            }
        }

        _physical[state.PlayerId.Value] = state;
    }

    public void RemovePhysical(PlayerId playerId) =>
        _physical.Remove(playerId.Value);

    public void ReplacePhysicalStatesForClub(ClubId clubId, IEnumerable<PlayerPhysicalState> states)
    {
        ArgumentNullException.ThrowIfNull(states);

        var incoming = states.ToArray();
        foreach (var state in incoming)
        {
            if (state.ClubId != clubId)
            {
                throw new ArgumentException(
                    $"Physical state club {state.ClubId?.Value} does not match {clubId.Value}.",
                    nameof(states));
            }
        }

        var keepIds = incoming.Select(state => state.PlayerId.Value).ToHashSet();
        var removeKeys = _physical
            .Where(pair => pair.Value.ClubId == clubId && !keepIds.Contains(pair.Key))
            .Select(pair => pair.Key)
            .ToArray();
        foreach (var key in removeKeys)
        {
            _physical.Remove(key);
        }

        foreach (var state in incoming)
        {
            UpsertPhysical(state);
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
            UpsertPhysical(state);
        }
    }
}
