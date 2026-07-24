using FootballCareerSimulator.Application.SocialContinuity.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;

namespace FootballCareerSimulator.Application.SocialContinuity.Composition;

public sealed class SocialContinuityModule
{
    public SocialContinuityModule(
        IPromiseStore promiseStore,
        IMemoryStore memoryStore,
        StartingOpportunityPromiseService startingOpportunity,
        PlayingTimePromiseService playingTime,
        PromiseMemoryService promiseMemory,
        SelectionMemoryService selectionMemory,
        PromiseInvalidationService invalidation)
    {
        PromiseStore = promiseStore;
        MemoryStore = memoryStore;
        StartingOpportunity = startingOpportunity;
        PlayingTime = playingTime;
        PromiseMemory = promiseMemory;
        SelectionMemory = selectionMemory;
        Invalidation = invalidation;
    }

    public IPromiseStore PromiseStore { get; }

    public IMemoryStore MemoryStore { get; }

    public StartingOpportunityPromiseService StartingOpportunity { get; }

    public PlayingTimePromiseService PlayingTime { get; }

    public PromiseMemoryService PromiseMemory { get; }

    public SelectionMemoryService SelectionMemory { get; }

    public PromiseInvalidationService Invalidation { get; }

    public static SocialContinuityModule Create(
        IPromiseStore? promiseStore = null,
        IMemoryStore? memoryStore = null)
    {
        var promises = promiseStore ?? new InMemoryPromiseStore();
        var memories = memoryStore ?? new InMemoryMemoryStore();
        var promiseMemory = new PromiseMemoryService(memories);
        var selectionMemory = new SelectionMemoryService(memories);
        var startingOpportunity = new StartingOpportunityPromiseService(promises, promiseMemory);
        var playingTime = new PlayingTimePromiseService(promises, promiseMemory);
        var invalidation = new PromiseInvalidationService(promises, promiseMemory);
        return new SocialContinuityModule(
            promises,
            memories,
            startingOpportunity,
            playingTime,
            promiseMemory,
            selectionMemory,
            invalidation);
    }
}
