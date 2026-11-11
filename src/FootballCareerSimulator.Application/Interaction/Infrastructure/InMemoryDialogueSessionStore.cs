using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Application.Interaction.Infrastructure;

public sealed class InMemoryDialogueSessionStore : IDialogueSessionStore
{
    private readonly Dictionary<long, DialogueSession> _sessions = new();

    public IReadOnlyList<DialogueSession> Sessions =>
        _sessions.Values.OrderBy(s => s.DialogueSessionId.Value).ToArray();

    public DialogueSession? Get(DialogueSessionId id) =>
        _sessions.TryGetValue(id.Value, out var session) ? session : null;

    public DialogueSession? FindAwaitingByDecision(DecisionRequestId decisionRequestId) =>
        _sessions.Values.FirstOrDefault(s =>
            s.IsAwaitingPlayer
            && s.SourceDecisionRequestId == decisionRequestId);

    public void Upsert(DialogueSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _sessions[session.DialogueSessionId.Value] = session;
    }

    public void ReplaceAll(IEnumerable<DialogueSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        _sessions.Clear();
        foreach (var session in sessions)
        {
            Upsert(session);
        }
    }
}
