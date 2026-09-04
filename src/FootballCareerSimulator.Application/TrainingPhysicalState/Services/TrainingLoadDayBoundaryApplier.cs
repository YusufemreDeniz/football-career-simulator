using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Services;

/// <summary>
/// DayBoundaryObserved → kulüp antrenman planının günlük yük tick'i.
/// </summary>
public sealed class TrainingLoadDayBoundaryApplier
{
    public const string ConsumerId = "TrainingPhysicalState";
    public const string EffectType = "ApplyDailyTrainingLoad";

    private readonly ITrainingPhysicalStateStore _store;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IClubSquadStore? _squadStore;
    private readonly EventEffectIdempotencyGate _gate;
    private readonly IWorldTimelineStore _timelineStore;

    public TrainingLoadDayBoundaryApplier(
        ITrainingPhysicalStateStore store,
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore,
        EventEffectIdempotencyGate gate,
        IClubSquadStore? squadStore = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _squadStore = squadStore;
    }

    public int ApplyFromReactions(IReadOnlyList<ReactionIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(intents);

        var applied = 0;
        var rootSeed = _timelineStore.Timeline.RootSeed;
        foreach (var intent in intents
                     .Where(i => string.Equals(
                         i.IntentTypeCode,
                         ObserveGameDayStartedReactionRule.IntentTypeCode,
                         StringComparison.Ordinal))
                     .OrderBy(i => i.OccurredAtDayNumber)
                     .ThenBy(i => i.SourceEventId))
        {
            var key = EventEffectProcessingKey.ForConsumerEffect(
                ConsumerId,
                intent.SourceEventId,
                EffectType);
            if (_gate.TryApply(key) == EventEffectApplicationStatus.Duplicate)
            {
                continue;
            }

            var day = GameDate.FromDayNumber(intent.OccurredAtDayNumber);
            applied += ApplyDailyLoadForManagedClubs(day, rootSeed);
        }

        return applied;
    }

    private int ApplyDailyLoadForManagedClubs(GameDate day, int rootSeed)
    {
        var career = _managerCareerStore.Career;
        if (career.EmploymentStatus != ManagerEmploymentStatus.Employed
            || career.ActiveEmployment is null)
        {
            return 0;
        }

        var clubId = career.ActiveEmployment.ClubId;
        var plan = _store.GetPlan(clubId);
        if (plan is null)
        {
            return 0;
        }

        IReadOnlyList<int>? occupied = null;
        var squad = _squadStore?.Get(clubId);
        if (squad is { Members.Count: > 0 })
        {
            occupied = squad.Members.Select(member => member.SlotIndex).ToArray();
        }

        var next = MvpTrainingLoadApplier.ApplyDailyTick(
            plan,
            day,
            rootSeed,
            _store.PhysicalBySlot,
            occupied);
        _store.ReplacePhysicalStatesForClub(clubId, next);
        return next.Count;
    }
}
