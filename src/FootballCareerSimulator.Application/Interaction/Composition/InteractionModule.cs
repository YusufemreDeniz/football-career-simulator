using FootballCareerSimulator.Application.Discipline.Infrastructure;
using FootballCareerSimulator.Application.Discipline.Ports;
using FootballCareerSimulator.Application.Discipline.Services;
using FootballCareerSimulator.Application.Interaction.Infrastructure;
using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.Transfer.Services;

namespace FootballCareerSimulator.Application.Interaction.Composition;

public sealed class InteractionModule
{
    public InteractionModule(
        IDecisionRequestStore decisionRequestStore,
        IDialogueSessionStore dialogueSessionStore,
        IDisciplinaryActionStore disciplinaryActionStore,
        DecisionRequestService decisions,
        DecisionRequestQueryService queries,
        DialogueOptionGenerationService dialogueOptions,
        DialogueSessionService dialogueSessions,
        DisciplinaryActionService discipline,
        DecisionRequestTimeAdvanceBlockerSource timeAdvanceBlocker,
        PostMatchPressDecisionTrigger postMatchPress,
        PostMatchPlayingTimeDemandTrigger postMatchPlayingTimeDemand,
        PostMatchBoardDemandTrigger postMatchBoardDemand,
        PromiseBrokenDecisionTrigger promiseBroken)
    {
        DecisionRequestStore = decisionRequestStore;
        DialogueSessionStore = dialogueSessionStore;
        DisciplinaryActionStore = disciplinaryActionStore;
        Decisions = decisions;
        Queries = queries;
        DialogueOptions = dialogueOptions;
        DialogueSessions = dialogueSessions;
        Discipline = discipline;
        TimeAdvanceBlocker = timeAdvanceBlocker;
        PostMatchPress = postMatchPress;
        PostMatchPlayingTimeDemand = postMatchPlayingTimeDemand;
        PostMatchBoardDemand = postMatchBoardDemand;
        PromiseBroken = promiseBroken;
    }

    public IDecisionRequestStore DecisionRequestStore { get; }

    public IDialogueSessionStore DialogueSessionStore { get; }

    public IDisciplinaryActionStore DisciplinaryActionStore { get; }

    public DecisionRequestService Decisions { get; }

    public DecisionRequestQueryService Queries { get; }

    public DialogueOptionGenerationService DialogueOptions { get; }

    public DialogueSessionService DialogueSessions { get; }

    public DisciplinaryActionService Discipline { get; }

    public DecisionRequestTimeAdvanceBlockerSource TimeAdvanceBlocker { get; }

    public PostMatchPressDecisionTrigger PostMatchPress { get; }

    public PostMatchPlayingTimeDemandTrigger PostMatchPlayingTimeDemand { get; }

    public PostMatchBoardDemandTrigger PostMatchBoardDemand { get; }

    public PromiseBrokenDecisionTrigger PromiseBroken { get; }

    public static InteractionModule Create(
        IManagerCareerStore managerCareerStore,
        PlayingTimePromiseService? playingTime = null,
        IDecisionRequestStore? decisionRequestStore = null,
        RelationshipEvaluationService? relationships = null,
        DecisionMemoryService? decisionMemory = null,
        IPromiseStore? promiseStore = null,
        IDialogueSessionStore? dialogueSessionStore = null,
        StartingOpportunityPromiseService? startingOpportunity = null,
        TransferNeedService? transferNeeds = null,
        IDisciplinaryActionStore? disciplinaryActionStore = null,
        IRelationshipStore? relationshipStore = null,
        IMemoryStore? memoryStore = null)
    {
        ArgumentNullException.ThrowIfNull(managerCareerStore);
        var store = decisionRequestStore ?? new InMemoryDecisionRequestStore();
        var sessions = dialogueSessionStore ?? new InMemoryDialogueSessionStore();
        var disciplineStore = disciplinaryActionStore ?? new InMemoryDisciplinaryActionStore();
        var discipline = new DisciplinaryActionService(disciplineStore);
        var dialogueOptions = new DialogueOptionGenerationService(
            store,
            promiseStore,
            transferNeeds,
            disciplineStore,
            relationshipStore);
        var dialogueSessionService = new DialogueSessionService(sessions);
        var decisions = new DecisionRequestService(
            store,
            managerCareerStore,
            playingTime,
            relationships,
            decisionMemory,
            dialogueOptions,
            dialogueSessionService,
            startingOpportunity,
            transferNeeds,
            discipline);
        var queries = new DecisionRequestQueryService(
            store,
            relationshipStore,
            promiseStore,
            memoryStore,
            managerCareerStore);
        var blocker = new DecisionRequestTimeAdvanceBlockerSource(store);
        var postMatchPress = new PostMatchPressDecisionTrigger(decisions);
        var postMatchPlayingTimeDemand = new PostMatchPlayingTimeDemandTrigger(
            decisions,
            memoryStore ?? new InMemoryMemoryStore(),
            promiseStore);
        var postMatchBoardDemand = new PostMatchBoardDemandTrigger(decisions);
        var promiseBroken = new PromiseBrokenDecisionTrigger(decisions, relationshipStore);
        return new InteractionModule(
            store,
            sessions,
            disciplineStore,
            decisions,
            queries,
            dialogueOptions,
            dialogueSessionService,
            discipline,
            blocker,
            postMatchPress,
            postMatchPlayingTimeDemand,
            postMatchBoardDemand,
            promiseBroken);
    }
}
