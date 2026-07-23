using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.ContractRegistration.Ports;

public interface IContractStore
{
    IReadOnlyList<PlayerContract> Contracts { get; }

    PlayerContract? GetByPlayer(PlayerId playerId);

    PlayerContract? GetActiveForPlayer(PlayerId playerId, Domain.WorldCalendar.GameDate day);

    IReadOnlyList<PlayerContract> GetForClub(ClubId clubId);

    void Upsert(PlayerContract contract);

    void ReplaceAll(IEnumerable<PlayerContract> contracts);
}
