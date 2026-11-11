using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.ContractRegistration.Infrastructure;

public sealed class InMemoryFreeAgentStore : IFreeAgentStore
{
    private readonly Dictionary<long, PlayerFreeAgency> _byPlayer = new();

    public IReadOnlyList<PlayerFreeAgency> FreeAgents =>
        _byPlayer.Values.OrderBy(f => f.PlayerId.Value).ToArray();

    public PlayerFreeAgency? Get(PlayerId playerId) =>
        _byPlayer.TryGetValue(playerId.Value, out var entry) ? entry : null;

    public IReadOnlyList<PlayerFreeAgency> GetReleasedFromClub(ClubId clubId) =>
        _byPlayer.Values
            .Where(f => f.LastClubId == clubId)
            .OrderBy(f => f.PlayerId.Value)
            .ToArray();

    public void Upsert(PlayerFreeAgency freeAgency)
    {
        ArgumentNullException.ThrowIfNull(freeAgency);
        _byPlayer[freeAgency.PlayerId.Value] = freeAgency;
    }

    public void Remove(PlayerId playerId) => _byPlayer.Remove(playerId.Value);

    public void ReplaceAll(IEnumerable<PlayerFreeAgency> freeAgents)
    {
        ArgumentNullException.ThrowIfNull(freeAgents);
        _byPlayer.Clear();
        foreach (var entry in freeAgents)
        {
            Upsert(entry);
        }
    }
}
