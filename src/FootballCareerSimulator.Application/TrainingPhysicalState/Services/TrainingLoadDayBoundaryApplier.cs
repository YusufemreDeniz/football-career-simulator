using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
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
    private readonly ILeagueCompetitionStore? _competitionStore;
    private readonly EventEffectIdempotencyGate _gate;
    private readonly IWorldTimelineStore _timelineStore;

    public TrainingLoadDayBoundaryApplier(
        ITrainingPhysicalStateStore store,
        IManagerCareerStore managerCareerStore,
        IWorldTimelineStore timelineStore,
        EventEffectIdempotencyGate gate,
        IClubSquadStore? squadStore = null,
        ILeagueCompetitionStore? competitionStore = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _squadStore = squadStore;
        _competitionStore = competitionStore;
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

        var squad = _squadStore?.Get(clubId);
        IReadOnlyList<(Domain.PlayerCareer.PlayerId PlayerId, int SlotIndex)> members =
            squad is { Members.Count: > 0 }
                ? squad.Members.Select(member => (member.PlayerId, member.SlotIndex)).ToArray()
                : Array.Empty<(Domain.PlayerCareer.PlayerId, int)>();

        if (members.Count == 0)
        {
            return 0;
        }

        var next = MvpTrainingLoadApplier.ApplyDailyTickToMembers(
            plan,
            day,
            rootSeed,
            members,
            _store.PhysicalByPlayerId,
            isMatchDay: ClubHasMatchOn(clubId, day));
        foreach (var state in next)
        {
            _store.UpsertPhysical(state);
        }

        return next.Count;
    }

    private bool ClubHasMatchOn(ClubId clubId, GameDate day)
    {
        var season = _competitionStore?.League.CurrentSeason;
        if (season is null)
        {
            return false;
        }

        return season.Fixtures.Any(fixture =>
            fixture.ScheduledDate.DayNumber == day.DayNumber
            && fixture.Status is FixtureStatus.Planned
            && (fixture.HomeClubId == clubId || fixture.AwayClubId == clubId));
    }
}
