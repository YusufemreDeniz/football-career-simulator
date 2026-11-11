using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Simulation.WorldCalendar;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Services;

public sealed record DueScheduledEvaluationProcessingResult(
    int ProcessedCount,
    int TransferWindowsClosed);

/// <summary>
/// Due scheduled evaluation'ları owner command ile işler (CloseTransferWindow).
/// </summary>
public sealed class DueScheduledEvaluationProcessor
{
    private readonly IScheduledEvaluationStore _scheduleStore;
    private readonly IWorldTimelineStore _timelineStore;
    private readonly CloseTransferWindowHandler _closeTransferWindow;

    public DueScheduledEvaluationProcessor(
        IScheduledEvaluationStore scheduleStore,
        IWorldTimelineStore timelineStore,
        CloseTransferWindowHandler closeTransferWindow)
    {
        _scheduleStore = scheduleStore ?? throw new ArgumentNullException(nameof(scheduleStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _closeTransferWindow = closeTransferWindow ?? throw new ArgumentNullException(nameof(closeTransferWindow));
    }

    public DueScheduledEvaluationProcessingResult ProcessDueThrough(int dayNumber, int rootSeed, Guid correlationId)
    {
        var due = _scheduleStore.GetPendingDueThrough(dayNumber);
        var processed = 0;
        var closed = 0;
        var sequence = 0L;

        foreach (var evaluation in due)
        {
            if (string.Equals(
                    evaluation.EvaluationTypeCode,
                    TransferWindowCloseReactionScheduler.CloseTransferWindowEvaluationType,
                    StringComparison.Ordinal))
            {
                var wasOpen = _timelineStore.Timeline.TransferWindow.IsOpen;
                var commandId = DeterministicGuidFactory.Create(
                    rootSeed,
                    unchecked((long)(uint)correlationId.GetHashCode() * 31L
                        + evaluation.Id.Value * 1009L
                        + sequence));
                _closeTransferWindow.Handle(new CloseTransferWindowCommand(commandId));
                if (wasOpen && !_timelineStore.Timeline.TransferWindow.IsOpen)
                {
                    closed++;
                }
            }

            evaluation.MarkCompleted();
            _scheduleStore.Replace(evaluation);
            processed++;
            sequence++;
        }

        return new DueScheduledEvaluationProcessingResult(processed, closed);
    }
}
