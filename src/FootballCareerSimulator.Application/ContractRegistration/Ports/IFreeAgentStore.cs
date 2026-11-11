using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.ContractRegistration.Ports;

public interface IFreeAgentStore
{
    IReadOnlyList<PlayerFreeAgency> FreeAgents { get; }

    PlayerFreeAgency? Get(PlayerId playerId);

    IReadOnlyList<PlayerFreeAgency> GetReleasedFromClub(ClubId clubId);

    void Upsert(PlayerFreeAgency freeAgency);

    void Remove(PlayerId playerId);

    void ReplaceAll(IEnumerable<PlayerFreeAgency> freeAgents);
}
