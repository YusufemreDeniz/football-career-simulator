using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.WorldCalendar.Services;

public sealed class OpenTransferWindowHandler : ICommandIdempotencyReset
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly WorldCalendarEventEvaluationService? _eventEvaluation;
    private TransferWindowOpenedConsequenceApplier? _windowOpenedConsequences;
    private readonly Dictionary<Guid, OpenTransferWindowResult> _completedCommands = new();

    public OpenTransferWindowHandler(
        IWorldTimelineStore timelineStore,
        WorldCalendarEventEvaluationService? eventEvaluation = null)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _eventEvaluation = eventEvaluation;
    }

    public void BindWindowOpenedConsequences(TransferWindowOpenedConsequenceApplier applier) =>
        _windowOpenedConsequences = applier ?? throw new ArgumentNullException(nameof(applier));

    public OpenTransferWindowResult Handle(OpenTransferWindowCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        GameDate? closesOn = command.ClosesOnDayNumber is { } day
            ? GameDate.FromDayNumber(day)
            : null;
        var timeline = _timelineStore.Timeline;
        var window = timeline.OpenTransferWindow(closesOn);
        var raised = timeline.UncommittedEvents.ToArray();

        var applied = 0;
        var reactions = 0;
        var aiCompleted = 0;
        var aiAttempted = 0;
        IReadOnlyList<string> raisedTypes = Array.Empty<string>();
        if (_eventEvaluation is not null && raised.Length > 0)
        {
            var evaluated = _eventEvaluation.Evaluate(raised, timeline.RootSeed, command.CommandId);
            applied = evaluated.Effects.Count(e => e.Status == EventEffectApplicationStatus.Applied);
            reactions = evaluated.ReactionIntents.Count;
            raisedTypes = evaluated.Effects
                .Select(e => e.EventType)
                .ToArray();

            if (_windowOpenedConsequences is not null)
            {
                var outcome = _windowOpenedConsequences.ApplyFromReactions(
                    evaluated.ReactionIntents,
                    timeline.RootSeed);
                aiCompleted = outcome.CompletedCount;
                aiAttempted = outcome.AttemptedClubCount;
            }
        }

        timeline.ClearUncommittedEvents();

        var result = new OpenTransferWindowResult(
            command.CommandId,
            window.IsOpen,
            window.OpenedOn!.Value.DayNumber,
            window.ClosesOn?.DayNumber,
            applied,
            reactions,
            raisedTypes,
            aiCompleted,
            aiAttempted);
        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
