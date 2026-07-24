using FootballCareerSimulator.Application.Interaction.Infrastructure;
using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;

namespace FootballCareerSimulator.Application.Interaction.Composition;

public sealed class InteractionModule
{
    public InteractionModule(
        IDecisionRequestStore decisionRequestStore,
        DecisionRequestService decisions,
        DecisionRequestQueryService queries,
        DecisionRequestTimeAdvanceBlockerSource timeAdvanceBlocker)
    {
        DecisionRequestStore = decisionRequestStore;
        Decisions = decisions;
        Queries = queries;
        TimeAdvanceBlocker = timeAdvanceBlocker;
    }

    public IDecisionRequestStore DecisionRequestStore { get; }

    public DecisionRequestService Decisions { get; }

    public DecisionRequestQueryService Queries { get; }

    public DecisionRequestTimeAdvanceBlockerSource TimeAdvanceBlocker { get; }

    public static InteractionModule Create(
        IManagerCareerStore managerCareerStore,
        PlayingTimePromiseService? playingTime = null,
        IDecisionRequestStore? decisionRequestStore = null,
        RelationshipEvaluationService? relationships = null,
        DecisionMemoryService? decisionMemory = null)
    {
        ArgumentNullException.ThrowIfNull(managerCareerStore);
        var store = decisionRequestStore ?? new InMemoryDecisionRequestStore();
        var decisions = new DecisionRequestService(
            store,
            managerCareerStore,
            playingTime,
            relationships,
            decisionMemory);
        var queries = new DecisionRequestQueryService(store);
        var blocker = new DecisionRequestTimeAdvanceBlockerSource(store);
        return new InteractionModule(store, decisions, queries, blocker);
    }
}
