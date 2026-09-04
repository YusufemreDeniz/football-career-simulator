using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Services;

public sealed class SetWeeklyTrainingPlanHandler : ICommandIdempotencyReset
{
    private readonly ITrainingPhysicalStateStore _store;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IWorldTimelineStore _timelineStore;
    private readonly PlayerCareerDevelopmentService? _playerDevelopment;
    private readonly ClubSquadService? _clubSquadService;
    private readonly MatchSelectionAvailabilityRevalidationService? _selectionRevalidation;
    private readonly Dictionary<Guid, SetWeeklyTrainingPlanResult> _completed = new();

    public SetWeeklyTrainingPlanHandler(
        ITrainingPhysicalStateStore store,
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore,
        PlayerCareerDevelopmentService? playerDevelopment = null,
        ClubSquadService? clubSquadService = null,
        MatchSelectionAvailabilityRevalidationService? selectionRevalidation = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _playerDevelopment = playerDevelopment;
        _clubSquadService = clubSquadService;
        _selectionRevalidation = selectionRevalidation;
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
        var rootSeed = _timelineStore.Timeline.RootSeed;

        // Plan yalnız hedef yükü kaydeder; sakatlık/yorgunluk gün sınırında uygulanır.
        var plan = WeeklyTrainingPlan.Set(clubId, focus, intensity, rest, day);
        var previousPlan = _store.GetPlan(clubId);
        _store.UpsertPlan(plan);

        _clubSquadService?.SyncFromActiveContracts(clubId, day);
        if (previousPlan is null
            || previousPlan.Focus != plan.Focus
            || previousPlan.Intensity != plan.Intensity
            || previousPlan.RestApproach != plan.RestApproach)
        {
            _playerDevelopment?.EnsureAndApplyWeeklyTraining(clubId, plan, rootSeed, day);
        }

        var invalidated = _selectionRevalidation?.InvalidateUnavailableForClub(clubId, day) ?? 0;
        var physical = _store.PhysicalStates
            .Where(state => state.ClubId == clubId)
            .OrderBy(state => state.SlotIndex)
            .ToArray();
        var xi = physical.Length > 0
            ? physical.Take(MatchSelection.StartingXiSize).ToArray()
            : Array.Empty<PlayerPhysicalState>();
        var averageFatigue = xi.Length == 0
            ? PlayerPhysicalState.DefaultFatigue
            : (int)Math.Round(xi.Average(s => s.Fatigue), MidpointRounding.AwayFromZero);
        var averageFitness = xi.Length == 0
            ? PlayerPhysicalState.DefaultFitness
            : (int)Math.Round(xi.Average(s => s.Fitness), MidpointRounding.AwayFromZero);

        var result = new SetWeeklyTrainingPlanResult(
            true,
            clubId.Value,
            (int)focus,
            (int)intensity,
            (int)rest,
            averageFatigue,
            averageFitness,
            physical.Count(s => s.IsInjured),
            invalidated,
            PhysicalLoadApplied: false);

        _completed[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completed.Clear();
}
