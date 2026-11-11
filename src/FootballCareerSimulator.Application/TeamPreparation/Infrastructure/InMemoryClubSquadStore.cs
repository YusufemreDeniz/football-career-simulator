using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Infrastructure;

public sealed class InMemoryClubSquadStore : IClubSquadStore
{
    private readonly Dictionary<long, ClubSquad> _byClub = new();

    public IReadOnlyList<ClubSquad> Squads =>
        _byClub.Values.OrderBy(s => s.ClubId.Value).ToArray();

    public ClubSquad? Get(ClubId clubId) =>
        _byClub.TryGetValue(clubId.Value, out var squad) ? squad : null;

    public void Upsert(ClubSquad squad)
    {
        ArgumentNullException.ThrowIfNull(squad);
        _byClub[squad.ClubId.Value] = squad;
    }

    public void ReplaceAll(IEnumerable<ClubSquad> squads)
    {
        ArgumentNullException.ThrowIfNull(squads);
        _byClub.Clear();
        foreach (var squad in squads)
        {
            Upsert(squad);
        }
    }
}
