using FootballCareerSimulator.Application.Interaction.Infrastructure;
using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;

namespace FootballCareerSimulator.Application.Interaction.Composition;

public sealed class InteractionModule
{
    public InteractionModule(
        IDecisionRequestStore decisionRequestStore,
        IDialogueSessionStore dialogueSessionStore,
        DecisionRequestService decisions,
        DecisionRequestQueryService queries,
        DialogueOptionGenerationService dialogueOptions,
        DialogueSessionService dialogueSessions,
        DecisionRequestTimeAdvanceBlockerSource timeAdvanceBlocker)
    {
        DecisionRequestStore = decisionRequestStore;
        DialogueSessionStore = dialogueSessionStore;
        Decisions = decisions;
        Queries = queries;
        DialogueOptions = dialogueOptions;
        DialogueSessions = dialogueSessions;
        TimeAdvanceBlocker = timeAdvanceBlocker;
    }

    public IDecisionRequestStore DecisionRequestStore { get; }

    public IDialogueSessionStore DialogueSessionStore { get; }

    public DecisionRequestService Decisions { get; }

    public DecisionRequestQueryService Queries { get; }

    public DialogueOptionGenerationService DialogueOptions { get; }

    public DialogueSessionService DialogueSessions { get; }

    public DecisionRequestTimeAdvanceBlockerSource TimeAdvanceBlocker { get; }

    public static InteractionModule Create(
        IManagerCareerStore managerCareerStore,
        PlayingTimePromiseService? playingTime = null,
        IDecisionRequestStore? decisionRequestStore = null,
        RelationshipEvaluationService? relationships = null,
        DecisionMemoryService? decisionMemory = null,
        IPromiseStore? promiseStore = null,
        IDialogueSessionStore? dialogueSessionStore = null,
        StartingOpportunityPromiseService? startingOpportunity = null)
    {
        ArgumentNullException.ThrowIfNull(managerCareerStore);
        var store = decisionRequestStore ?? new InMemoryDecisionRequestStore();
        var sessions = dialogueSessionStore ?? new InMemoryDialogueSessionStore();
        var dialogueOptions = new DialogueOptionGenerationService(store, promiseStore);
        var dialogueSessionService = new DialogueSessionService(sessions);
        var decisions = new DecisionRequestService(
            store,
            managerCareerStore,
            playingTime,
            relationships,
            decisionMemory,
            dialogueOptions,
            dialogueSessionService,
            startingOpportunity);
        var queries = new DecisionRequestQueryService(store);
        var blocker = new DecisionRequestTimeAdvanceBlockerSource(store);
        return new InteractionModule(
            store,
            sessions,
            decisions,
            queries,
            dialogueOptions,
            dialogueSessionService,
            blocker);
    }
}
