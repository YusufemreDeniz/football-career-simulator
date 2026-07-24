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

        var previousByPlayer = (_squadStore.Get(clubId)?.Members ?? Array.Empty<SquadMember>())
            .ToDictionary(m => m.PlayerId.Value);

        var usedSlots = new HashSet<int>();
        var members = new List<SquadMember>(active.Length);

        foreach (var contract in active)
        {
            if (_playerCareerStore.Careers.All(c => c.Id != contract.PlayerId))
            {
                continue;
            }

            if (!previousByPlayer.TryGetValue(contract.PlayerId.Value, out var prior))
            {
                continue;
            }

            members.Add(SquadMember.Create(contract.PlayerId, prior.SlotIndex, contract.StartDate));
            usedSlots.Add(prior.SlotIndex);
        }

        foreach (var contract in active)
        {
            if (members.Any(m => m.PlayerId == contract.PlayerId))
            {
                continue;
            }

            if (_playerCareerStore.Careers.All(c => c.Id != contract.PlayerId))
            {
                continue;
            }

            var slot = Enumerable.Range(MatchSelection.MinSquadSlot, ClubSquad.MaxMembers)
                .FirstOrDefault(s => !usedSlots.Contains(s), -1);
            if (slot < 0)
            {
                throw new TeamPreparationInvariantViolationException(
                    $"Club {clubId.Value} has no free squad slot for incoming player {contract.PlayerId.Value}.");
            }

            members.Add(SquadMember.Create(contract.PlayerId, slot, contract.StartDate));
            usedSlots.Add(slot);
        }

        var squad = ClubSquad.Rehydrate(clubId, members);
        _squadStore.Upsert(squad);
        return squad;
    }

    public void SyncClubs(IEnumerable<long> clubIds, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(clubIds);
        foreach (var clubId in clubIds.Distinct())
        {
            SyncFromActiveContracts(new ClubId(clubId), day);
        }
    }
}
