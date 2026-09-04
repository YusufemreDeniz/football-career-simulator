using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Queries;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;

namespace FootballCareerSimulator.Application.ContractRegistration.Services;

public sealed class ContractQueryService
{
    private readonly IContractStore _store;
    private readonly IFreeAgentStore _freeAgentStore;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IWorldTimelineStore _timelineStore;

    public ContractQueryService(
        IContractStore store,
        IFreeAgentStore freeAgentStore,
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _freeAgentStore = freeAgentStore ?? throw new ArgumentNullException(nameof(freeAgentStore));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public ClubContractSummaryReadModel GetManagedClubSummary()
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId })
        {
            return new ClubContractSummaryReadModel(null, 0, 0, 0, 0, 0);
        }

        var day = _timelineStore.Timeline.CurrentDate;
        var contracts = _store.GetForClub(clubId);
        var active = contracts.Where(c => c.IsActiveOn(day)).ToArray();
        var expired = contracts.Count(c => c.Status == ContractStatus.Expired);
        var yearEnd = Domain.WorldCalendar.GameDate.FromCalendarDate(day.Year + 1, day.Month, day.Day);
        var expiring = active.Count(c => c.EndDate.DayNumber <= yearEnd.DayNumber);
        var freeAgents = _freeAgentStore.GetReleasedFromClub(clubId).Count;

        return new ClubContractSummaryReadModel(
            clubId.Value,
            active.Length,
            expired,
            expiring,
            active.Length == 0
                ? 0
                : (int)Math.Round(active.Average(c => c.WeeklyWage), MidpointRounding.AwayFromZero),
            freeAgents);
    }

    public bool IsFreeAgent(PlayerId playerId) => _freeAgentStore.Get(playerId) is not null;

    public SignableFreeAgentReadModel? GetNextSignableFreeAgentForManagedClub()
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId })
        {
            return null;
        }

        var entry = _freeAgentStore.GetReleasedFromClub(clubId)
            .OrderBy(f => f.BecameFreeAgentOn.DayNumber)
            .ThenBy(f => f.PlayerId.Value)
            .FirstOrDefault();

        return entry is null
            ? null
            : new SignableFreeAgentReadModel(
                entry.PlayerId.Value,
                entry.LastClubId.Value,
                entry.BecameFreeAgentOn.DayNumber);
    }
}
