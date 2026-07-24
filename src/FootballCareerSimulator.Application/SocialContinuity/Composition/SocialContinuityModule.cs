using FootballCareerSimulator.Application.SocialContinuity.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;

namespace FootballCareerSimulator.Application.SocialContinuity.Composition;

public sealed class SocialContinuityModule
{
    public SocialContinuityModule(
        IPromiseStore promiseStore,
        StartingOpportunityPromiseService startingOpportunity)
    {
        PromiseStore = promiseStore;
        StartingOpportunity = startingOpportunity;
    }

    public IPromiseStore PromiseStore { get; }

    public StartingOpportunityPromiseService StartingOpportunity { get; }

    public static SocialContinuityModule Create(IPromiseStore? promiseStore = null)
    {
        var store = promiseStore ?? new InMemoryPromiseStore();
        return new SocialContinuityModule(store, new StartingOpportunityPromiseService(store));
    }
}
