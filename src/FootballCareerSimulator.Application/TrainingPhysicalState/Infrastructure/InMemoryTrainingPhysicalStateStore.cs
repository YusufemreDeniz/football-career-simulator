using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;

public sealed class InMemoryTrainingPhysicalStateStore : ITrainingPhysicalStateStore
{
    private readonly Dictionary<long, WeeklyTrainingPlan> _plans = new();
    private readonly Dictionary<long, PlayerPhysicalState> _physical = new();
    private readonly Dictionary<long, HashSet<long>> _idsByClub = new();
    private readonly Dictionary<(long ClubId, int SlotIndex), long> _idBySlot = new();
    private readonly HashSet<long> _injuredIds = new();
    private IReadOnlyList<WeeklyTrainingPlan>? _orderedPlans;
    private IReadOnlyList<PlayerPhysicalState>? _orderedPhysicalStates;
    private IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState>? _physicalBySlotCache;

    public IReadOnlyList<WeeklyTrainingPlan> Plans =>
        _orderedPlans ??= Array.AsReadOnly(
            _plans.Values.OrderBy(p => p.ClubId.Value).ToArray());

    public IReadOnlyList<PlayerPhysicalState> PhysicalStates =>
        _orderedPhysicalStates ??= Array.AsReadOnly(
            _physical.Values
                .OrderBy(s => s.PlayerId.Value)
                .ToArray());

    public IReadOnlyDictionary<long, PlayerPhysicalState> PhysicalByPlayerId => _physical;

    public IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> PhysicalBySlot =>
        _physicalBySlotCache ??= _idBySlot.ToDictionary(
            pair => pair.Key,
            pair => _physical[pair.Value]);

    public WeeklyTrainingPlan? GetPlan(ClubId clubId) =>
        _plans.TryGetValue(clubId.Value, out var plan) ? plan : null;

    public PlayerPhysicalState? GetPhysical(PlayerId playerId) =>
        _physical.TryGetValue(playerId.Value, out var state) ? state : null;

    public PlayerPhysicalState? GetPhysical(ClubId clubId, int slotIndex) =>
        _idBySlot.TryGetValue((clubId.Value, slotIndex), out var playerId)
        && _physical.TryGetValue(playerId, out var state)
            ? state
            : null;

    public IReadOnlyList<PlayerPhysicalState> GetPhysicalStatesForClub(ClubId clubId)
    {
        if (!_idsByClub.TryGetValue(clubId.Value, out var ids) || ids.Count == 0)
        {
            return Array.Empty<PlayerPhysicalState>();
        }

        return ids
            .OrderBy(id => id)
            .Select(id => _physical[id])
            .ToArray();
    }

    public int RecoverDueInjuries(GameDate day)
    {
        if (_injuredIds.Count == 0)
        {
            return 0;
        }

        var recoveredCount = 0;
        foreach (var playerId in _injuredIds.ToArray())
        {
            if (!_physical.TryGetValue(playerId, out var state))
            {
                continue;
            }

            var recovered = state.RecoverIfDue(day);
            if (!ReferenceEquals(recovered, state))
            {
                UpsertPhysical(recovered);
                recoveredCount++;
            }
        }

        return recoveredCount;
    }

    public void UpsertPlan(WeeklyTrainingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _plans[plan.ClubId.Value] = plan;
        _orderedPlans = null;
    }

    public void UpsertPhysical(PlayerPhysicalState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.HasLocation)
        {
            var slotKey = (state.ClubId!.Value.Value, state.SlotIndex!.Value);
            if (_idBySlot.TryGetValue(slotKey, out var occupantId)
                && occupantId != state.PlayerId.Value
                && _physical.TryGetValue(occupantId, out var occupant))
            {
                RemovePhysicalIndexes(occupant);
                _physical[occupantId] = occupant.ClearLocation();
                AddPhysicalIndexes(_physical[occupantId]);
            }
        }

        if (_physical.TryGetValue(state.PlayerId.Value, out var previous))
        {
            RemovePhysicalIndexes(previous);
        }

        _physical[state.PlayerId.Value] = state;
        AddPhysicalIndexes(state);
        _orderedPhysicalStates = null;
        _physicalBySlotCache = null;
    }

    public void RemovePhysical(PlayerId playerId)
    {
        if (_physical.Remove(playerId.Value, out var removed))
        {
            RemovePhysicalIndexes(removed);
            _orderedPhysicalStates = null;
            _physicalBySlotCache = null;
        }
    }

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
        if (_idsByClub.TryGetValue(clubId.Value, out var existingClubIds))
        {
            foreach (var key in existingClubIds.Where(id => !keepIds.Contains(id)).ToArray())
            {
                RemovePhysical(new PlayerId(key));
            }
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
        _idsByClub.Clear();
        _idBySlot.Clear();
        _injuredIds.Clear();
        _orderedPlans = null;
        _orderedPhysicalStates = null;
        _physicalBySlotCache = null;

        foreach (var plan in plans)
        {
            UpsertPlan(plan);
        }

        foreach (var state in physicalStates)
        {
            UpsertPhysical(state);
        }
    }

    private void AddPhysicalIndexes(PlayerPhysicalState state)
    {
        var playerId = state.PlayerId.Value;
        if (state.ClubId is ClubId clubId)
        {
            if (!_idsByClub.TryGetValue(clubId.Value, out var clubSet))
            {
                clubSet = new HashSet<long>();
                _idsByClub[clubId.Value] = clubSet;
            }

            clubSet.Add(playerId);

            if (state.SlotIndex is int slotIndex)
            {
                _idBySlot[(clubId.Value, slotIndex)] = playerId;
            }
        }

        if (state.IsInjured)
        {
            _injuredIds.Add(playerId);
        }
    }

    private void RemovePhysicalIndexes(PlayerPhysicalState state)
    {
        var playerId = state.PlayerId.Value;
        if (state.ClubId is ClubId clubId)
        {
            if (_idsByClub.TryGetValue(clubId.Value, out var clubSet))
            {
                clubSet.Remove(playerId);
                if (clubSet.Count == 0)
                {
                    _idsByClub.Remove(clubId.Value);
                }
            }

            if (state.SlotIndex is int slotIndex)
            {
                var slotKey = (clubId.Value, slotIndex);
                if (_idBySlot.TryGetValue(slotKey, out var existingId) && existingId == playerId)
                {
                    _idBySlot.Remove(slotKey);
                }
            }
        }

        _injuredIds.Remove(playerId);
    }
}
