using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.ContractRegistration.Infrastructure;

public sealed class InMemoryContractStore : IContractStore
{
    private readonly Dictionary<long, PlayerContract> _byId = new();
    private readonly Dictionary<long, long> _idByPlayer = new();
    private readonly Dictionary<long, HashSet<long>> _idsByClub = new();
    private IReadOnlyList<PlayerContract>? _orderedContracts;

    public IReadOnlyList<PlayerContract> Contracts =>
        _orderedContracts ??= Array.AsReadOnly(
            _byId.Values.OrderBy(c => c.Id.Value).ToArray());

    public PlayerContract? GetByPlayer(PlayerId playerId) =>
        _idByPlayer.TryGetValue(playerId.Value, out var id)
        && _byId.TryGetValue(id, out var contract)
            ? contract
            : null;

    public PlayerContract? GetActiveForPlayer(PlayerId playerId, GameDate day) =>
        GetByPlayer(playerId) is { } contract && contract.IsActiveOn(day)
            ? contract
            : null;

    public IReadOnlyList<PlayerContract> GetForClub(ClubId clubId)
    {
        if (!_idsByClub.TryGetValue(clubId.Value, out var ids) || ids.Count == 0)
        {
            return Array.Empty<PlayerContract>();
        }

        return ids
            .Select(id => _byId[id])
            .OrderBy(c => c.PlayerId.Value)
            .ToArray();
    }

    public void Upsert(PlayerContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        if (contract.Status == ContractStatus.Active
            && _idByPlayer.TryGetValue(contract.PlayerId.Value, out var existingId)
            && existingId != contract.Id.Value
            && _byId.TryGetValue(existingId, out var existing)
            && existing.Status == ContractStatus.Active)
        {
            throw new ContractRegistrationInvariantViolationException(
                $"Player {contract.PlayerId.Value} already has an active contract.");
        }

        if (_byId.TryGetValue(contract.Id.Value, out var previous))
        {
            RemoveIndexes(previous);
        }

        _byId[contract.Id.Value] = contract;
        AddIndexes(contract);
        _orderedContracts = null;
    }

    public void ReplaceAll(IEnumerable<PlayerContract> contracts)
    {
        ArgumentNullException.ThrowIfNull(contracts);
        _byId.Clear();
        _idByPlayer.Clear();
        _idsByClub.Clear();
        _orderedContracts = null;
        foreach (var contract in contracts)
        {
            Upsert(contract);
        }
    }

    private void AddIndexes(PlayerContract contract)
    {
        var id = contract.Id.Value;
        _idByPlayer[contract.PlayerId.Value] = id;
        if (!_idsByClub.TryGetValue(contract.ClubId.Value, out var clubSet))
        {
            clubSet = new HashSet<long>();
            _idsByClub[contract.ClubId.Value] = clubSet;
        }

        clubSet.Add(id);
    }

    private void RemoveIndexes(PlayerContract contract)
    {
        var id = contract.Id.Value;
        if (_idByPlayer.TryGetValue(contract.PlayerId.Value, out var existingId) && existingId == id)
        {
            _idByPlayer.Remove(contract.PlayerId.Value);
        }

        if (_idsByClub.TryGetValue(contract.ClubId.Value, out var clubSet))
        {
            clubSet.Remove(id);
            if (clubSet.Count == 0)
            {
                _idsByClub.Remove(contract.ClubId.Value);
            }
        }
    }
}
