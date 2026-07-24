using FootballCareerSimulator.Application.SocialContinuity.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;

namespace FootballCareerSimulator.Application.SocialContinuity.Composition;

public sealed class SocialContinuityModule
{
    public SocialContinuityModule(
        IPromiseStore promiseStore,
        IMemoryStore memoryStore,
        IRelationshipStore relationshipStore,
        StartingOpportunityPromiseService startingOpportunity,
        PlayingTimePromiseService playingTime,
        PromiseMemoryService promiseMemory,
        SelectionMemoryService selectionMemory,
        TrustMemoryService trustMemory,
        TransferMemoryService transferMemory,
        CareerMemoryService careerMemory,
        ClubHistoryMemoryService clubHistoryMemory,
        MatchPerformanceMemoryService matchPerformanceMemory,
        RelationshipEvaluationService relationshipEvaluation,
        PromiseInvalidationService invalidation,
        MemoryQueryService queries,
        PromiseQueryService promiseQueries,
        RelationshipQueryService relationshipQueries)
    {
        PromiseStore = promiseStore;
        MemoryStore = memoryStore;
        RelationshipStore = relationshipStore;
        StartingOpportunity = startingOpportunity;
        PlayingTime = playingTime;
        PromiseMemory = promiseMemory;
        SelectionMemory = selectionMemory;
        TrustMemory = trustMemory;
        TransferMemory = transferMemory;
        CareerMemory = careerMemory;
        ClubHistoryMemory = clubHistoryMemory;
        MatchPerformanceMemory = matchPerformanceMemory;
        RelationshipEvaluation = relationshipEvaluation;
        Invalidation = invalidation;
        Queries = queries;
        PromiseQueries = promiseQueries;
        RelationshipQueries = relationshipQueries;
    }

    public IPromiseStore PromiseStore { get; }

    public IMemoryStore MemoryStore { get; }

    public IRelationshipStore RelationshipStore { get; }

    public StartingOpportunityPromiseService StartingOpportunity { get; }

    public PlayingTimePromiseService PlayingTime { get; }

    public PromiseMemoryService PromiseMemory { get; }

    public SelectionMemoryService SelectionMemory { get; }

    public TrustMemoryService TrustMemory { get; }

    public TransferMemoryService TransferMemory { get; }

    public CareerMemoryService CareerMemory { get; }

    public ClubHistoryMemoryService ClubHistoryMemory { get; }

    public MatchPerformanceMemoryService MatchPerformanceMemory { get; }

    public RelationshipEvaluationService RelationshipEvaluation { get; }

    public PromiseInvalidationService Invalidation { get; }

    public MemoryQueryService Queries { get; }

    public PromiseQueryService PromiseQueries { get; }

    public RelationshipQueryService RelationshipQueries { get; }

    public static SocialContinuityModule Create(
        IPromiseStore? promiseStore = null,
        IMemoryStore? memoryStore = null,
        IRelationshipStore? relationshipStore = null)
    {
        var promises = promiseStore ?? new InMemoryPromiseStore();
        var memories = memoryStore ?? new InMemoryMemoryStore();
        var relationships = relationshipStore ?? new InMemoryRelationshipStore();
        var trustMemory = new TrustMemoryService(memories);
        var promiseMemory = new PromiseMemoryService(memories, trustMemory);
        var selectionMemory = new SelectionMemoryService(memories);
        var transferMemory = new TransferMemoryService(memories);
        var careerMemory = new CareerMemoryService(memories);
        var clubHistoryMemory = new ClubHistoryMemoryService(memories);
        var matchPerformanceMemory = new MatchPerformanceMemoryService(memories);
        var relationshipEvaluation = new RelationshipEvaluationService(relationships);
        var startingOpportunity = new StartingOpportunityPromiseService(
            promises,
            promiseMemory,
            relationshipEvaluation);
        var playingTime = new PlayingTimePromiseService(
            promises,
            promiseMemory,
            relationshipEvaluation);
        var invalidation = new PromiseInvalidationService(promises, promiseMemory);
        var queries = new MemoryQueryService(memories);
        var promiseQueries = new PromiseQueryService(promises);
        var relationshipQueries = new RelationshipQueryService(relationships);
        return new SocialContinuityModule(
            promises,
            memories,
            relationships,
            startingOpportunity,
            playingTime,
            promiseMemory,
            selectionMemory,
            trustMemory,
            transferMemory,
            careerMemory,
            clubHistoryMemory,
            matchPerformanceMemory,
            relationshipEvaluation,
            invalidation,
            queries,
            promiseQueries,
            relationshipQueries);
    }
}
