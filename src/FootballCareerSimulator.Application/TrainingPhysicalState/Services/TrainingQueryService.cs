using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Services;

public sealed class TrainingQueryService
{
    private readonly ITrainingPhysicalStateStore _store;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IWorldTimelineStore _timelineStore;

    public TrainingQueryService(
        ITrainingPhysicalStateStore store,
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public ClubTrainingSummaryReadModel GetManagedClubSummary()
    {
        var clubId = _managerCareerStore.Career.ActiveEmployment?.ClubId;
        if (clubId is null)
        {
            return new ClubTrainingSummaryReadModel(
                null, null, null, null, null, null, null, null, null, null, false, 0, 0);
        }

        return GetClubSummary(clubId.Value);
    }

    public ClubTrainingSummaryReadModel GetClubSummary(ClubId clubId)
    {
        var day = _timelineStore.Timeline.CurrentDate;
        var plan = _store.GetPlan(clubId);
        var allForClub = _store.PhysicalStates.Where(s => s.ClubId == clubId).ToArray();
        var injured = allForClub.Count(s => s.IsInjured);
        var unavailable = allForClub.Count(s => !s.IsAvailableOn(day));

        var slots = Enumerable.Range(MatchSelection.MinSquadSlot, MatchSelection.StartingXiSize)
            .Select(slot => _store.GetPhysical(clubId, slot))
            .Where(state => state is not null)
            .Select(state => state!)
            .ToArray();

        int? avgFatigue = slots.Length == 0
            ? null
            : (int)Math.Round(slots.Average(s => s.Fatigue), MidpointRounding.AwayFromZero);
        int? avgFitness = slots.Length == 0
            ? null
            : (int)Math.Round(slots.Average(s => s.Fitness), MidpointRounding.AwayFromZero);

        if (plan is null)
        {
            return new ClubTrainingSummaryReadModel(
                clubId.Value,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                avgFatigue,
                avgFitness,
                false,
                injured,
                unavailable);
        }

        return new ClubTrainingSummaryReadModel(
            clubId.Value,
            (int)plan.Focus,
            (int)plan.Intensity,
            (int)plan.RestApproach,
            plan.Focus.ToString(),
            plan.Intensity.ToString(),
            plan.RestApproach.ToString(),
            plan.SetAt.DayNumber,
            avgFatigue,
            avgFitness,
            true,
            injured,
            unavailable);
    }
}
