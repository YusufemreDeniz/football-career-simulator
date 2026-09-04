using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.PlayerCareer;

namespace FootballCareerSimulator.Application.PlayerCareer.Services;

public sealed class PlayerCareerQueryService
{
    private readonly IPlayerCareerStore _store;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IWorldTimelineStore _timelineStore;

    public PlayerCareerQueryService(
        IPlayerCareerStore store,
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public ClubDevelopmentSummaryReadModel GetManagedClubSummary()
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId })
        {
            return new ClubDevelopmentSummaryReadModel(null, 0, 0, 0, 0, 0, 0);
        }

        var day = _timelineStore.Timeline.CurrentDate;
        var careers = _store.Careers.Where(c => c.OriginClubId == clubId).ToArray();
        if (careers.Length == 0)
        {
            return new ClubDevelopmentSummaryReadModel(clubId.Value, 0, 0, 0, 0, 0, 0);
        }

        return new ClubDevelopmentSummaryReadModel(
            clubId.Value,
            careers.Length,
            (int)Math.Round(careers.Average(c => c.CurrentAbility), MidpointRounding.AwayFromZero),
            (int)Math.Round(careers.Average(c => c.PotentialAbility), MidpointRounding.AwayFromZero),
            careers.Count(c => c.LastDevelopedOn?.DayNumber == day.DayNumber),
            (int)Math.Round(careers.Average(c => c.AgeYears(day)), MidpointRounding.AwayFromZero),
            careers.Count(c => c.GetPhase(day) == CareerPhase.Declining));
    }
}
