using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
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
            .Where(c => _playerCareerStore.Careers.Any(career => career.Id == c.PlayerId))
            .OrderBy(c => c.PlayerId.Value)
            .ToArray();

        var previousByPlayer = (_squadStore.Get(clubId)?.Members ?? Array.Empty<SquadMember>())
            .ToDictionary(m => m.PlayerId.Value);

        // Önce mevcut slot sahipleri, sonra yeni gelenler — kapasite dolunca fazlası kadro dışı kalır
        // (sözleşme aktif olabilir; maç günü listesine giremez).
        var ordered = active
            .OrderByDescending(c => previousByPlayer.ContainsKey(c.PlayerId.Value))
            .ThenBy(c =>
                previousByPlayer.TryGetValue(c.PlayerId.Value, out var prior)
                    ? prior.SlotIndex
                    : int.MaxValue)
            .ThenBy(c => c.PlayerId.Value)
            .Take(ClubSquad.MaxMembers)
            .ToArray();

        var usedSlots = new HashSet<int>();
        var members = new List<SquadMember>(ordered.Length);

        foreach (var contract in ordered)
        {
            if (previousByPlayer.TryGetValue(contract.PlayerId.Value, out var prior)
                && !usedSlots.Contains(prior.SlotIndex))
            {
                members.Add(SquadMember.Create(contract.PlayerId, prior.SlotIndex, contract.StartDate));
                usedSlots.Add(prior.SlotIndex);
            }
        }

        foreach (var contract in ordered)
        {
            if (members.Any(m => m.PlayerId == contract.PlayerId))
            {
                continue;
            }

            var slot = Enumerable.Range(MatchSelection.MinSquadSlot, ClubSquad.MaxMembers)
                .FirstOrDefault(s => !usedSlots.Contains(s), -1);
            if (slot < 0)
            {
                // Take(MaxMembers) sonrası teorik olarak olmamalı; savunma.
                break;
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

    public int CountActiveContracts(ClubId clubId, GameDate day) =>
        _contractStore.GetForClub(clubId).Count(c => c.IsActiveOn(day));

    public bool HasFreeSquadCapacity(ClubId clubId, GameDate day) =>
        CountActiveContracts(clubId, day) < ClubSquad.MaxMembers;

    public SquadCapacityDigest GetCapacityDigest(ClubId clubId, GameDate day)
    {
        var activeIds = _contractStore.GetForClub(clubId)
            .Where(c => c.IsActiveOn(day))
            .Where(c => _playerCareerStore.Careers.Any(career => career.Id == c.PlayerId))
            .Select(c => c.PlayerId.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var squadIds = (_squadStore.Get(clubId)?.Members ?? Array.Empty<SquadMember>())
            .Select(m => m.PlayerId.Value)
            .ToHashSet();

        var overflow = activeIds.Where(id => !squadIds.Contains(id)).ToArray();
        return SquadCapacityDigest.Compose(
            activeIds.Length,
            squadIds.Count,
            ClubSquad.MaxMembers,
            overflow);
    }
}
