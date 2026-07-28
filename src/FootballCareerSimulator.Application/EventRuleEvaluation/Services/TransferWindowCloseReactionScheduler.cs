using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Services;

/// <summary>
/// DayBoundaryObserved → transfer penceresi kapanış scheduled evaluation (owner World & Calendar).
/// </summary>
public sealed class TransferWindowCloseReactionScheduler
{
    public const string CloseTransferWindowEvaluationType = "WorldCalendar.CloseTransferWindow";

    private readonly IWorldTimelineStore _timelineStore;
    private readonly IScheduledEvaluationStore _scheduleStore;
    private long _nextId = 1;

    public TransferWindowCloseReactionScheduler(
        IWorldTimelineStore timelineStore,
        IScheduledEvaluationStore scheduleStore)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _scheduleStore = scheduleStore ?? throw new ArgumentNullException(nameof(scheduleStore));
    }

    public int ScheduleFromReactions(IReadOnlyList<ReactionIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(intents);

        var scheduled = 0;
        var window = _timelineStore.Timeline.TransferWindow;
        if (!window.IsOpen || window.ClosesOn is null)
        {
            return 0;
        }

        var closesOnDay = window.ClosesOn.Value.DayNumber;
        foreach (var intent in intents
                     .Where(i => string.Equals(
                         i.IntentTypeCode,
                         ObserveGameDayStartedReactionRule.IntentTypeCode,
                         StringComparison.Ordinal))
                     .OrderBy(i => i.OccurredAtDayNumber)
                     .ThenBy(i => i.SourceEventId))
        {
            if (closesOnDay > intent.OccurredAtDayNumber)
            {
                continue;
            }

            if (_scheduleStore.FindPending(CloseTransferWindowEvaluationType, closesOnDay) is not null)
            {
                continue;
            }

            var evaluation = ScheduledEvaluation.CreatePending(
                new ScheduledEvaluationId(_nextId++),
                CloseTransferWindowEvaluationType,
                closesOnDay,
                intent.SourceEventId);
            _scheduleStore.Add(evaluation);
            scheduled++;
        }

        return scheduled;
    }
}
