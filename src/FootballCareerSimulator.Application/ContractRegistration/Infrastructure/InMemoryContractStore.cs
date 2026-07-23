using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.ContractRegistration.Infrastructure;

public sealed class InMemoryContractStore : IContractStore
{
    private readonly Dictionary<long, PlayerContract> _byId = new();

    public IReadOnlyList<PlayerContract> Contracts =>
        _byId.Values.OrderBy(c => c.Id.Value).ToArray();

    public PlayerContract? GetByPlayer(PlayerId playerId) =>
        _byId.Values.FirstOrDefault(c => c.PlayerId == playerId);

    public PlayerContract? GetActiveForPlayer(PlayerId playerId, GameDate day) =>
        _byId.Values.FirstOrDefault(c => c.PlayerId == playerId && c.IsActiveOn(day));

    public IReadOnlyList<PlayerContract> GetForClub(ClubId clubId) =>
        _byId.Values
            .Where(c => c.ClubId == clubId)
            .OrderBy(c => c.PlayerId.Value)
            .ToArray();

    public void Upsert(PlayerContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var activeConflict = _byId.Values.FirstOrDefault(existing =>
            existing.Id != contract.Id
            && existing.PlayerId == contract.PlayerId
            && existing.Status == ContractStatus.Active
            && contract.Status == ContractStatus.Active);

        if (activeConflict is not null)
        {
            throw new ContractRegistrationInvariantViolationException(
                $"Player {contract.PlayerId.Value} already has an active contract.");
        }

        _byId[contract.Id.Value] = contract;
    }

    public void ReplaceAll(IEnumerable<PlayerContract> contracts)
    {
        ArgumentNullException.ThrowIfNull(contracts);
        _byId.Clear();
        foreach (var contract in contracts)
        {
            Upsert(contract);
        }
    }
}
