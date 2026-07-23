using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Ports;

public interface IClubSquadStore
{
    IReadOnlyList<ClubSquad> Squads { get; }

    ClubSquad? Get(ClubId clubId);

    void Upsert(ClubSquad squad);

    void ReplaceAll(IEnumerable<ClubSquad> squads);
}
