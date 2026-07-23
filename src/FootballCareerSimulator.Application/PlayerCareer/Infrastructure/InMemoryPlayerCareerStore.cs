using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Domain.Shared;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Application.PlayerCareer.Infrastructure;

public sealed class InMemoryPlayerCareerStore : IPlayerCareerStore
{
    private readonly Dictionary<long, PlayerCareerAggregate> _byId = new();

    public IReadOnlyList<PlayerCareerAggregate> Careers =>
        _byId.Values
            .OrderBy(c => c.Id.Value)
            .ThenBy(c => c.OriginClubId.Value)
            .ThenBy(c => c.SlotIndex)
            .ToArray();

    public IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerCareerAggregate> ByClubSlot =>
        _byId.Values.ToDictionary(c => (c.OriginClubId.Value, c.SlotIndex));

    public PlayerCareerAggregate? Get(ClubId clubId, int slotIndex) =>
        ByClubSlot.TryGetValue((clubId.Value, slotIndex), out var career) ? career : null;

    public void Upsert(PlayerCareerAggregate career)
    {
        ArgumentNullException.ThrowIfNull(career);
        _byId[career.Id.Value] = career;
    }

    public void ReplaceAll(IEnumerable<PlayerCareerAggregate> careers)
    {
        ArgumentNullException.ThrowIfNull(careers);
        _byId.Clear();
        foreach (var career in careers)
        {
            Upsert(career);
        }
    }

    public void ReplaceClub(ClubId clubId, IEnumerable<PlayerCareerAggregate> careers)
    {
        ArgumentNullException.ThrowIfNull(careers);

        var removeIds = _byId.Values
            .Where(c => c.OriginClubId == clubId)
            .Select(c => c.Id.Value)
            .ToArray();
        foreach (var id in removeIds)
        {
            _byId.Remove(id);
        }

        foreach (var career in careers)
        {
            if (career.OriginClubId != clubId)
            {
                throw new ArgumentException(
                    $"Career club {career.OriginClubId.Value} does not match {clubId.Value}.",
                    nameof(careers));
            }

            Upsert(career);
        }
    }
}
