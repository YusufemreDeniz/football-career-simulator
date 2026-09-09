using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Services;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.DataPacks;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

/// <summary>
/// Aktif sözleşmelerden A takım membership senkronu (Contract → Team Prep).
/// </summary>
public sealed class ClubSquadService
{
    private readonly IClubSquadStore _squadStore;
    private readonly IContractStore _contractStore;
    private readonly IPlayerCareerStore _playerCareerStore;
    private readonly ITrainingPhysicalStateStore? _trainingStore;

    public ClubSquadService(
        IClubSquadStore squadStore,
        IContractStore contractStore,
        IPlayerCareerStore playerCareerStore,
        ITrainingPhysicalStateStore? trainingStore = null)
    {
        _squadStore = squadStore ?? throw new ArgumentNullException(nameof(squadStore));
        _contractStore = contractStore ?? throw new ArgumentNullException(nameof(contractStore));
        _playerCareerStore = playerCareerStore
            ?? throw new ArgumentNullException(nameof(playerCareerStore));
        _trainingStore = trainingStore;
    }

    public ClubSquad SyncFromActiveContracts(ClubId clubId, GameDate day)
    {
        var active = _contractStore.GetForClub(clubId)
            .Where(c => c.IsActiveOn(day))
            .Where(c => !_playerCareerStore.Careers.Any(career => career.Id == c.PlayerId && career.IsRetired))
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

            EnsurePlayerCareer(contract.PlayerId, clubId, slot, day);
        }

        var squad = ClubSquad.Rehydrate(clubId, members);
        _squadStore.Upsert(squad);
        if (_trainingStore is not null)
        {
            PhysicalLocationSync.SyncClubMembership(_trainingStore, clubId, squad.Members);
        }

        return squad;
    }

    private void EnsurePlayerCareer(PlayerId playerId, ClubId clubId, int slot, GameDate day)
    {
        if (_playerCareerStore.Careers.Any(c => c.Id == playerId))
        {
            return;
        }

        int ca = 65, pa = 75, birthYear = day.Year - 24;
        if (EuropeanElite202627DataPack.TryResolvePlayer(playerId.Value, out _, out _, out _, out var euroProfile) && euroProfile is not null)
        {
            ca = euroProfile.CurrentAbility ?? 75;
            pa = euroProfile.PotentialAbility ?? Math.Min(99, ca + 5);
            birthYear = day.Year - (euroProfile.Age ?? 25);
        }
        else if (playerId.Value >= 1001 && playerId.Value <= 99999)
        {
            var srcClub = (playerId.Value - 1) / 1000L;
            var srcSlot = (int)((playerId.Value - 1) % 1000L);
            ca = MvpSquadStrengthCalculator.GetPlayerRating(new ClubId(srcClub), 0, srcSlot);
            pa = Math.Min(99, ca + 5);
            birthYear = day.Year - 24;
        }

        var career = Domain.PlayerCareer.PlayerCareer.Rehydrate(
            playerId,
            clubId,
            slot,
            ca,
            pa,
            developmentPoints: 0,
            lastDevelopedOn: null,
            birthYear: birthYear,
            lastAgedCalendarYear: null);
        _playerCareerStore.Upsert(career);
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
        _contractStore.GetForClub(clubId)
            .Count(c => c.IsActiveOn(day)
                && !_playerCareerStore.Careers.Any(career => career.Id == c.PlayerId && career.IsRetired));

    public bool HasFreeSquadCapacity(ClubId clubId, GameDate day) =>
        CountActiveContracts(clubId, day) < ClubSquad.MaxMembers;

    public SquadCapacityDigest GetCapacityDigest(ClubId clubId, GameDate day)
    {
        var activeIds = _contractStore.GetForClub(clubId)
            .Where(c => c.IsActiveOn(day))
            .Where(c => !_playerCareerStore.Careers.Any(career => career.Id == c.PlayerId && career.IsRetired))
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

    /// <summary>
    /// Yer açma adayı: önce taşan (kadrodışı) oyuncu, yoksa en yüksek slot.
    /// </summary>
    public long? SuggestReleaseCandidatePlayerId(ClubId clubId, GameDate day)
    {
        var capacity = GetCapacityDigest(clubId, day);
        if (capacity.OverflowPlayerIds.Count > 0)
        {
            return capacity.OverflowPlayerIds[0];
        }

        if (!capacity.IsFull)
        {
            return null;
        }

        var squad = _squadStore.Get(clubId);
        if (squad is null || squad.Members.Count == 0)
        {
            return null;
        }

        return squad.Members.OrderByDescending(m => m.SlotIndex).First().PlayerId.Value;
    }

    /// <summary>
    /// Satış adayı: taşan veya en yüksek slot — XI altı kadroda satılmaz.
    /// </summary>
    public long? SuggestSaleCandidatePlayerId(ClubId clubId, GameDate day)
    {
        const int minSellerActiveContracts = ClubSquad.MinimumPlayableContracts + 1;
        if (CountActiveContracts(clubId, day) < minSellerActiveContracts)
        {
            return null;
        }

        var capacity = GetCapacityDigest(clubId, day);
        if (capacity.OverflowPlayerIds.Count > 0)
        {
            return capacity.OverflowPlayerIds[0];
        }

        var squad = _squadStore.Get(clubId);
        if (squad is null || squad.Members.Count == 0)
        {
            return null;
        }

        return squad.Members.OrderByDescending(m => m.SlotIndex).First().PlayerId.Value;
    }

    /// <summary>
    /// Taşan (sözleşmeli ama kadro dışı) oyuncuyu en yüksek slotlu üyenin yerine alır.
    /// Sözleşmeler değişmez; yalnızca maç günü membership değişir.
    /// </summary>
    public SquadOverflowPromotionResult PromoteFirstOverflowToSquad(ClubId clubId, GameDate day)
    {
        var capacity = GetCapacityDigest(clubId, day);
        if (!capacity.IsOverCapacity || capacity.OverflowPlayerIds.Count == 0)
        {
            throw new TeamPreparationInvariantViolationException(
                "No overflow player to promote into the matchday squad.");
        }

        var current = _squadStore.Get(clubId)
            ?? throw new TeamPreparationInvariantViolationException(
                $"Club {clubId.Value} has no squad to reshape.");

        if (current.Members.Count == 0)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Club {clubId.Value} squad is empty; sync contracts first.");
        }

        var promoteId = new Domain.PlayerCareer.PlayerId(capacity.OverflowPlayerIds[0]);
        var contract = _contractStore.GetForClub(clubId)
            .FirstOrDefault(c => c.IsActiveOn(day) && c.PlayerId == promoteId)
            ?? throw new TeamPreparationInvariantViolationException(
                $"Overflow player {promoteId.Value} has no active contract at club {clubId.Value}.");

        var demote = current.Members.OrderByDescending(m => m.SlotIndex).First();
        var members = current.Members
            .Where(m => m.PlayerId != demote.PlayerId)
            .Append(SquadMember.Create(promoteId, demote.SlotIndex, contract.StartDate))
            .ToArray();

        var next = ClubSquad.Rehydrate(clubId, members);
        _squadStore.Upsert(next);
        if (_trainingStore is not null)
        {
            PhysicalLocationSync.SyncClubMembership(_trainingStore, clubId, next.Members);
        }

        return new SquadOverflowPromotionResult(
            promoteId.Value,
            demote.PlayerId.Value,
            demote.SlotIndex);
    }
}

public sealed record SquadOverflowPromotionResult(
    long PromotedPlayerId,
    long DemotedPlayerId,
    int SlotIndex);
