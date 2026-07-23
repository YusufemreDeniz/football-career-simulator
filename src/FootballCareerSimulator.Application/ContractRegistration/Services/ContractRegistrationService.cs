using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.ContractRegistration;

namespace FootballCareerSimulator.Application.ContractRegistration.Services;

public sealed class ContractRegistrationService
{
    private readonly IContractStore _store;
    private readonly IPlayerCareerStore _playerCareerStore;

    public ContractRegistrationService(IContractStore store, IPlayerCareerStore playerCareerStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _playerCareerStore = playerCareerStore
            ?? throw new ArgumentNullException(nameof(playerCareerStore));
    }

    public void EnsureClubContracts(ClubId clubId, GameDate day)
    {
        var careers = _playerCareerStore.Careers
            .Where(c => c.OriginClubId == clubId)
            .ToArray();

        foreach (var career in careers)
        {
            var existing = _store.GetByPlayer(career.Id);
            if (existing is not null)
            {
                continue;
            }

            _store.Upsert(MvpContractFactory.CreateForPlayerCareer(career, day));
        }
    }

    public int ExpireDueContracts(GameDate day)
    {
        var updated = 0;
        foreach (var contract in _store.Contracts.ToArray())
        {
            var next = contract.ExpireIfDue(day);
            if (next.Status != contract.Status)
            {
                _store.Upsert(next);
                updated++;
            }
        }

        return updated;
    }
}
