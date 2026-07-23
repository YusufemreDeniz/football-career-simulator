using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Services;

public sealed class SetWeeklyTrainingPlanHandler : ICommandIdempotencyReset
{
    private readonly ITrainingPhysicalStateStore _store;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IWorldTimelineStore _timelineStore;
    private readonly Dictionary<Guid, SetWeeklyTrainingPlanResult> _completed = new();

    public SetWeeklyTrainingPlanHandler(
        ITrainingPhysicalStateStore store,
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public SetWeeklyTrainingPlanResult Handle(SetWeeklyTrainingPlanCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completed.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var career = _managerCareerStore.Career;
        if (career.EmploymentStatus != ManagerEmploymentStatus.Employed
            || career.ActiveEmployment is null)
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                "Only an employed manager can set a weekly training plan.");
        }

        var clubId = career.ActiveEmployment.ClubId;
        var focus = (TrainingFocus)command.Focus;
        var intensity = (TrainingIntensity)command.Intensity;
        var rest = (RestApproach)command.RestApproach;
        var day = _timelineStore.Timeline.CurrentDate;

        var plan = WeeklyTrainingPlan.Set(clubId, focus, intensity, rest, day);
        var physical = MvpTrainingLoadApplier.ApplyPlanToSquad(plan);

        _store.UpsertPlan(plan);
        _store.ReplacePhysicalStatesForClub(clubId, physical);

        var xi = physical.Take(MatchSelection.StartingXiSize).ToArray();
        var result = new SetWeeklyTrainingPlanResult(
            true,
            clubId.Value,
            (int)focus,
            (int)intensity,
            (int)rest,
            (int)Math.Round(xi.Average(s => s.Fatigue), MidpointRounding.AwayFromZero),
            (int)Math.Round(xi.Average(s => s.Fitness), MidpointRounding.AwayFromZero));

        _completed[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completed.Clear();
}
