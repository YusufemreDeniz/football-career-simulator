using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

/// <summary>
/// Aktif sözleşmelerden A takım membership senkronu (Contract → Team Prep).
/// </summary>
public sealed class ClubSquadService
{
    private readonly IClubSquadStore _squadStore;
    private readonly IContractStore _contractStore;
    private readonly IPlayerCareerStore _playerCareerStore;

    public ClubSquadService(
        IClubSquadStore squadStore,
        IContractStore contractStore,
        IPlayerCareerStore playerCareerStore)
    {
        _squadStore = squadStore ?? throw new ArgumentNullException(nameof(squadStore));
        _contractStore = contractStore ?? throw new ArgumentNullException(nameof(contractStore));
        _playerCareerStore = playerCareerStore
            ?? throw new ArgumentNullException(nameof(playerCareerStore));
    }

    public ClubSquad SyncFromActiveContracts(ClubId clubId, GameDate day)
    {
        var active = _contractStore.GetForClub(clubId)
            .Where(c => c.IsActiveOn(day))
            .OrderBy(c => c.PlayerId.Value)
            .ToArray();

        var members = new List<SquadMember>(active.Length);
        foreach (var contract in active)
        {
            var career = _playerCareerStore.Careers
                .FirstOrDefault(c => c.Id == contract.PlayerId);
            if (career is null)
            {
                continue;
            }

            members.Add(SquadMember.Create(contract.PlayerId, career.SlotIndex, contract.StartDate));
        }

        var squad = ClubSquad.Rehydrate(clubId, members);
        _squadStore.Upsert(squad);
        return squad;
    }
}
