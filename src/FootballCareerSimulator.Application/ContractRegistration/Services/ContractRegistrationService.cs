using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Queries;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;
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
    private PromiseInvalidationService? _promiseInvalidation;
    private RelationshipEvaluationService? _relationships;

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

    public void BindPromiseInvalidation(PromiseInvalidationService invalidation) =>
        _promiseInvalidation = invalidation ?? throw new ArgumentNullException(nameof(invalidation));

    public void BindRelationships(RelationshipEvaluationService relationships) =>
        _relationships = relationships ?? throw new ArgumentNullException(nameof(relationships));

    public bool IsFreeAgent(PlayerId playerId) => _freeAgentStore.Get(playerId) is not null;

    public ClubId? GetActiveClub(PlayerId playerId, GameDate day) =>
        _store.GetActiveForPlayer(playerId, day)?.ClubId;

    public void EnsureClubContracts(ClubId clubId, GameDate day)
    {
        var careers = _playerCareerStore.Careers
            .Where(c => c.OriginClubId == clubId && !c.IsRetired)
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

    public bool RetirePlayer(PlayerId playerId, GameDate day)
    {
        var contract = _store.GetByPlayer(playerId);
        var wasRegistered = contract is not null || _freeAgentStore.Get(playerId) is not null;

        if (contract is { Status: ContractStatus.Active })
        {
            var retiredContract = contract.IsActiveOn(day)
                ? contract.ReleaseEarly(day)
                : contract.ExpireIfDue(day);
            if (retiredContract.Status == ContractStatus.Active)
            {
                throw new ContractRegistrationInvariantViolationException(
                    $"Contract {contract.Id.Value} cannot retire before its start date.");
            }

            _store.Upsert(retiredContract);
        }

        _freeAgentStore.Remove(playerId);
        _promiseInvalidation?.InvalidateForPlayerLeaving(playerId, day);
        _relationships?.MarkDormantForPlayerLeaving(playerId, day);
        return wasRegistered;
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
            _promiseInvalidation?.InvalidateForPlayerLeaving(next.PlayerId, day);
            _relationships?.MarkDormantForPlayerLeaving(next.PlayerId, day);
            affectedClubs.Add(next.ClubId.Value);
            freeAgentPlayers.Add(next.PlayerId.Value);
            expired++;
        }

        return new FreeAgencyExpiryResult(
            expired,
            affectedClubs.OrderBy(id => id).ToArray(),
            freeAgentPlayers);
    }

    /// <summary>
    /// MVP yer açma: aktif sözleşmeyi erken bitirip serbest ajan kaydı oluşturur.
    /// </summary>
    public ClubPlayerReleaseResult ReleasePlayerFromClub(
        PlayerId playerId,
        ClubId clubId,
        GameDate day,
        bool wasOverflow)
    {
        var contract = _store.GetActiveForPlayer(playerId, day)
            ?? throw new ContractRegistrationInvariantViolationException(
                $"Player {playerId.Value} has no active contract on day {day.DayNumber}.");

        if (contract.ClubId != clubId)
        {
            throw new ContractRegistrationInvariantViolationException(
                $"Player {playerId.Value} is not contracted to club {clubId.Value}.");
        }

        var next = contract.ReleaseEarly(day);
        _store.Upsert(next);
        _freeAgentStore.Upsert(PlayerFreeAgency.Release(playerId, clubId, day));
        _promiseInvalidation?.InvalidateForPlayerLeaving(playerId, day);
        _relationships?.MarkDormantForPlayerLeaving(playerId, day);

        var remaining = _store.GetForClub(clubId).Count(c => c.IsActiveOn(day));
        return new ClubPlayerReleaseResult(
            playerId.Value,
            clubId.Value,
            wasOverflow,
            remaining);
    }

    /// <summary>
    /// Transfer olmadan MVP: serbest ajanı yalnızca son kulübüne geri imzalar.
    /// </summary>
    public FreeAgentResignResult SignFreeAgentToLastClub(
        PlayerId playerId,
        ClubId clubId,
        GameDate day,
        int contractYears = 2)
    {
        if (contractYears < 1 || contractYears > 5)
        {
            throw new ContractRegistrationInvariantViolationException(
                "Contract years must be between 1 and 5.");
        }

        var freeAgency = _freeAgentStore.Get(playerId)
            ?? throw new ContractRegistrationInvariantViolationException(
                $"Player {playerId.Value} is not a free agent.");

        if (freeAgency.LastClubId != clubId)
        {
            throw new ContractRegistrationInvariantViolationException(
                "MVP free-agent resign is only allowed back to the last club (no transfer).");
        }

        if (GetActiveClub(playerId, day) is not null)
        {
            throw new ContractRegistrationInvariantViolationException(
                $"Player {playerId.Value} already has an active club.");
        }

        var career = _playerCareerStore.Careers.FirstOrDefault(c => c.Id == playerId)
            ?? throw new ContractRegistrationInvariantViolationException(
                $"Player career {playerId.Value} was not found.");

        var endDate = GameDate.FromCalendarDate(day.Year + contractYears, day.Month, day.Day);
        var wage = Math.Max(500, career.CurrentAbility * 120);
        var contract = PlayerContract.Activate(playerId, clubId, day, endDate, wage);
        _store.Upsert(contract);
        _freeAgentStore.Remove(playerId);

        return new FreeAgentResignResult(
            playerId.Value,
            clubId.Value,
            wage,
            endDate.DayNumber);
    }

    /// <summary>
    /// Transfer completion owner geçişi: teklif şartlarıyla yeni aktif sözleşme.
    /// Serbest ajan kaydını temizler; LastClub kısıtı yoktur.
    /// </summary>
    public TransferContractActivationResult ActivateContractForTransfer(
        PlayerId playerId,
        ClubId buyingClubId,
        GameDate day,
        int weeklyWage,
        int contractYears)
    {
        if (contractYears < 1 || contractYears > 5)
        {
            throw new ContractRegistrationInvariantViolationException(
                "Contract years must be between 1 and 5.");
        }

        if (weeklyWage < 0)
        {
            throw new ContractRegistrationInvariantViolationException(
                "Weekly wage cannot be negative.");
        }

        var wasFreeAgent = _freeAgentStore.Get(playerId) is not null;
        var endDate = GameDate.FromCalendarDate(day.Year + contractYears, day.Month, day.Day);
        var contract = PlayerContract.Activate(playerId, buyingClubId, day, endDate, weeklyWage);
        _store.Upsert(contract);
        _freeAgentStore.Remove(playerId);

        return new TransferContractActivationResult(
            playerId.Value,
            buyingClubId.Value,
            weeklyWage,
            endDate.DayNumber,
            wasFreeAgent);
    }
}
