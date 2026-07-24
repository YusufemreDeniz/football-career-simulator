using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// DialogueSession owner (iskelet): DecisionRequest açılınca tek-turn oturum; seçim/expire senkronu.
/// </summary>
public sealed class DialogueSessionService
{
    private readonly IDialogueSessionStore _store;

    public DialogueSessionService(IDialogueSessionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public DialogueSession OpenForDecision(
        DecisionRequest request,
        IReadOnlyList<string> availableOptionCodes)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(availableOptionCodes);

        var existing = _store.Sessions.FirstOrDefault(s =>
            s.IsAwaitingPlayer
            && s.SourceDecisionRequestId == request.DecisionRequestId);
        if (existing is not null)
        {
            throw new InteractionInvariantViolationException(
                $"Decision #{request.DecisionRequestId.Value} already has an awaiting dialogue session.");
        }

        var nextId = _store.Sessions.Count == 0
            ? 1L
            : _store.Sessions.Max(s => s.DialogueSessionId.Value) + 1;
        var session = DialogueSession.OpenForDecision(
            new DialogueSessionId(nextId),
            request,
            availableOptionCodes);
        _store.Upsert(session);
        return session;
    }

    public void EnsureOptionInFrozenSet(DecisionRequestId decisionRequestId, string optionCode)
    {
        var session = _store.FindAwaitingByDecision(decisionRequestId)
            ?? throw new InteractionInvariantViolationException(
                $"No awaiting dialogue session for decision #{decisionRequestId.Value}.");

        if (string.IsNullOrWhiteSpace(optionCode))
        {
            throw new InteractionInvariantViolationException("Option code is required.");
        }

        var trimmed = optionCode.Trim();
        if (!session.AvailableOptionCodes.Contains(trimmed, StringComparer.Ordinal))
        {
            throw new InteractionInvariantViolationException(
                $"Option '{trimmed}' was not in the frozen dialogue option set.");
        }
    }

    public DialogueSession? MarkResolved(DecisionRequestId decisionRequestId, string optionCode, GameDate day)
    {
        var session = _store.FindAwaitingByDecision(decisionRequestId);
        if (session is null)
        {
            return null;
        }

        var resolved = session.Resolve(optionCode, day);
        _store.Upsert(resolved);
        return resolved;
    }

    public DialogueSession? MarkExpired(DecisionRequestId decisionRequestId, GameDate day)
    {
        var session = _store.FindAwaitingByDecision(decisionRequestId);
        if (session is null)
        {
            return null;
        }

        var expired = session.Expire(day);
        _store.Upsert(expired);
        return expired;
    }
}
