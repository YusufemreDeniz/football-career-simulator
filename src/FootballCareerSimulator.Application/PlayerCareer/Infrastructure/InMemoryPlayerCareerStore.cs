using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Application.PlayerCareer.Infrastructure;

public sealed class InMemoryPlayerCareerStore : IPlayerCareerStore
{
    private readonly Dictionary<long, PlayerCareerAggregate> _byId = new();
    private readonly Dictionary<(long ClubId, int SlotIndex), PlayerCareerAggregate> _activeByClubSlot = new();
    private readonly Dictionary<long, HashSet<long>> _activeIdsByOriginClub = new();
    private IReadOnlyList<PlayerCareerAggregate>? _orderedCareers;

    public IReadOnlyList<PlayerCareerAggregate> Careers =>
        _orderedCareers ??= Array.AsReadOnly(
            _byId.Values
                .OrderBy(c => c.Id.Value)
                .ThenBy(c => c.OriginClubId.Value)
                .ThenBy(c => c.SlotIndex)
                .ToArray());

    public IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerCareerAggregate> ByClubSlot =>
        _activeByClubSlot;

    public PlayerCareerAggregate? GetById(PlayerId playerId) =>
        _byId.TryGetValue(playerId.Value, out var career) ? career : null;

    public PlayerCareerAggregate? Get(ClubId clubId, int slotIndex) =>
        _activeByClubSlot.TryGetValue((clubId.Value, slotIndex), out var career) ? career : null;

    public IReadOnlyList<PlayerCareerAggregate> GetActiveByOriginClub(ClubId clubId)
    {
        if (!_activeIdsByOriginClub.TryGetValue(clubId.Value, out var ids) || ids.Count == 0)
        {
            return Array.Empty<PlayerCareerAggregate>();
        }

        return ids
            .Select(id => _byId[id])
            .OrderBy(c => c.SlotIndex)
            .ThenBy(c => c.Id.Value)
            .ToArray();
    }

    public void Upsert(PlayerCareerAggregate career)
    {
        ArgumentNullException.ThrowIfNull(career);
        if (_byId.TryGetValue(career.Id.Value, out var previous))
        {
            RemoveIndexes(previous);
        }

        _byId[career.Id.Value] = career;
        AddIndexes(career);
        _orderedCareers = null;
    }

    public void ReplaceAll(IEnumerable<PlayerCareerAggregate> careers)
    {
        ArgumentNullException.ThrowIfNull(careers);
        _byId.Clear();
        _activeByClubSlot.Clear();
        _activeIdsByOriginClub.Clear();
        _orderedCareers = null;
        foreach (var career in careers)
        {
            Upsert(career);
        }
    }

    public void ReplaceClub(ClubId clubId, IEnumerable<PlayerCareerAggregate> careers)
    {
        ArgumentNullException.ThrowIfNull(careers);

        var incoming = careers as IReadOnlyCollection<PlayerCareerAggregate> ?? careers.ToArray();
        foreach (var career in incoming)
        {
            if (career.OriginClubId != clubId)
            {
                throw new ArgumentException(
                    $"Career club {career.OriginClubId.Value} does not match {clubId.Value}.",
                    nameof(careers));
            }
        }

        if (_activeIdsByOriginClub.TryGetValue(clubId.Value, out var existingClubIds))
        {
            foreach (var id in existingClubIds.ToArray())
            {
                if (_byId.Remove(id, out var removed))
                {
                    RemoveIndexes(removed);
                }
            }
        }

        foreach (var career in incoming)
        {
            Upsert(career);
        }

        _orderedCareers = null;
    }

    private void AddIndexes(PlayerCareerAggregate career)
    {
        if (career.IsRetired)
        {
            return;
        }

        var clubId = career.OriginClubId.Value;
        _activeByClubSlot[(clubId, career.SlotIndex)] = career;
        if (!_activeIdsByOriginClub.TryGetValue(clubId, out var clubSet))
        {
            clubSet = new HashSet<long>();
            _activeIdsByOriginClub[clubId] = clubSet;
        }

        clubSet.Add(career.Id.Value);
    }

    private void RemoveIndexes(PlayerCareerAggregate career)
    {
        var clubId = career.OriginClubId.Value;
        var slotKey = (clubId, career.SlotIndex);
        if (_activeByClubSlot.TryGetValue(slotKey, out var existing) && existing.Id == career.Id)
        {
            _activeByClubSlot.Remove(slotKey);
        }

        if (_activeIdsByOriginClub.TryGetValue(clubId, out var clubSet))
        {
            clubSet.Remove(career.Id.Value);
            if (clubSet.Count == 0)
            {
                _activeIdsByOriginClub.Remove(clubId);
            }
        }
    }
}
