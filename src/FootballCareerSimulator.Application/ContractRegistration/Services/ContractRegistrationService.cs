using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Queries;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.ContractRegistration;

namespace FootballCareerSimulator.Application.ContractRegistration.Services;

public sealed class ContractRegistrationService
{
    private readonly IContractStore _store;
    private readonly IFreeAgentStore _freeAgentStore;
    private readonly IPlayerCareerStore _playerCareerStore;

    public ContractRegistrationService(
        IContractStore store,
        IFreeAgentStore freeAgentStore,
        IPlayerCareerStore playerCareerStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _freeAgentStore = freeAgentStore ?? throw new ArgumentNullException(nameof(freeAgentStore));
        _playerCareerStore = playerCareerStore
            ?? throw new ArgumentNullException(nameof(playerCareerStore));
    }

    public bool IsFreeAgent(PlayerId playerId) => _freeAgentStore.Get(playerId) is not null;

    public ClubId? GetActiveClub(PlayerId playerId, GameDate day) =>
        _store.GetActiveForPlayer(playerId, day)?.ClubId;

    public void EnsureClubContracts(ClubId clubId, GameDate day)
    {
        var careers = _playerCareerStore.Careers
            .Where(c => c.OriginClubId == clubId)
            .ToArray();

        foreach (var career in careers)
        {
            if (_freeAgentStore.Get(career.Id) is not null)
            {
                continue;
            }

            var existing = _store.GetByPlayer(career.Id);
            if (existing is not null)
            {
                continue;
            }

            var contract = MvpContractFactory.CreateForPlayerCareer(career, day);
            _store.Upsert(contract);
            _freeAgentStore.Remove(career.Id);
        }
    }

    public FreeAgencyExpiryResult ExpireDueContracts(GameDate day)
    {
        var affectedClubs = new HashSet<long>();
        var freeAgentPlayers = new List<long>();
        var expired = 0;

        foreach (var contract in _store.Contracts.ToArray())
        {
            var next = contract.ExpireIfDue(day);
            if (next.Status == contract.Status)
            {
                continue;
            }

            _store.Upsert(next);
            _freeAgentStore.Upsert(
                PlayerFreeAgency.Release(next.PlayerId, next.ClubId, day));
            affectedClubs.Add(next.ClubId.Value);
            freeAgentPlayers.Add(next.PlayerId.Value);
            expired++;
        }

        return new FreeAgencyExpiryResult(
            expired,
            affectedClubs.OrderBy(id => id).ToArray(),
            freeAgentPlayers);
    }
}
