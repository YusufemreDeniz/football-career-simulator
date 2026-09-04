using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Application.WorldCalendar.Services;

public sealed class CloseTransferWindowHandler : ICommandIdempotencyReset
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly WorldCalendarEventEvaluationService? _eventEvaluation;
    private TransferWindowClosedConsequenceApplier? _windowClosedConsequences;
    private readonly Dictionary<Guid, CloseTransferWindowResult> _completedCommands = new();

    public CloseTransferWindowHandler(
        IWorldTimelineStore timelineStore,
        WorldCalendarEventEvaluationService? eventEvaluation = null)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _eventEvaluation = eventEvaluation;
    }

    public void BindWindowClosedConsequences(TransferWindowClosedConsequenceApplier applier) =>
        _windowClosedConsequences = applier ?? throw new ArgumentNullException(nameof(applier));

    public CloseTransferWindowResult Handle(CloseTransferWindowCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var timeline = _timelineStore.Timeline;
        var window = timeline.CloseTransferWindow();
        var raised = timeline.UncommittedEvents.ToArray();

        var applied = 0;
        var reactions = 0;
        var expired = 0;
        var carried = 0;
        IReadOnlyList<string> raisedTypes = Array.Empty<string>();
        if (_eventEvaluation is not null && raised.Length > 0)
        {
            var evaluated = _eventEvaluation.Evaluate(raised, timeline.RootSeed, command.CommandId);
            applied = evaluated.Effects.Count(e => e.Status == EventEffectApplicationStatus.Applied);
            reactions = evaluated.ReactionIntents.Count;
            raisedTypes = evaluated.Effects
                .Select(e => e.EventType)
                .ToArray();

            if (_windowClosedConsequences is not null)
            {
                var outcome = _windowClosedConsequences.ApplyFromReactions(evaluated.ReactionIntents);
                expired = outcome.ExpiredCount;
                carried = outcome.CarriedCount;
            }
        }

        timeline.ClearUncommittedEvents();

        var result = new CloseTransferWindowResult(
            command.CommandId,
            window.IsOpen,
            applied,
            reactions,
            raisedTypes,
            expired,
            carried);
        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
