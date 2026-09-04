using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Application.Interaction.Ports;

public interface IDialogueSessionStore
{
    IReadOnlyList<DialogueSession> Sessions { get; }

    DialogueSession? Get(DialogueSessionId id);

    DialogueSession? FindAwaitingByDecision(DecisionRequestId decisionRequestId);

    void Upsert(DialogueSession session);

    void ReplaceAll(IEnumerable<DialogueSession> sessions);
}
