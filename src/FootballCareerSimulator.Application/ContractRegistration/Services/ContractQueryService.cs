using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Queries;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ContractRegistration;

namespace FootballCareerSimulator.Application.ContractRegistration.Services;

public sealed class ContractQueryService
{
    private readonly IContractStore _store;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IWorldTimelineStore _timelineStore;

    public ContractQueryService(
        IContractStore store,
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public ClubContractSummaryReadModel GetManagedClubSummary()
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId })
        {
            return new ClubContractSummaryReadModel(null, 0, 0, 0, 0);
        }

        var day = _timelineStore.Timeline.CurrentDate;
        var contracts = _store.GetForClub(clubId);
        var active = contracts.Where(c => c.IsActiveOn(day)).ToArray();
        var expired = contracts.Count(c => c.Status == ContractStatus.Expired || !c.IsActiveOn(day));
        var yearEnd = Domain.WorldCalendar.GameDate.FromCalendarDate(day.Year + 1, day.Month, day.Day);
        var expiring = active.Count(c => c.EndDate.DayNumber <= yearEnd.DayNumber);

        return new ClubContractSummaryReadModel(
            clubId.Value,
            active.Length,
            expired,
            expiring,
            active.Length == 0
                ? 0
                : (int)Math.Round(active.Average(c => c.WeeklyWage), MidpointRounding.AwayFromZero));
    }
}
