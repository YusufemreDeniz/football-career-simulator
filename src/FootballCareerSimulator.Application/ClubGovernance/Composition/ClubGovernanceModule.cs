namespace FootballCareerSimulator.Application.ClubGovernance.Composition;

using FootballCareerSimulator.Application.ClubGovernance.Infrastructure;
using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Domain.ClubGovernance;

/// <summary>
/// Manuel composition root (D-348).
/// </summary>
public sealed class ClubGovernanceModule
{
    public ClubGovernanceModule(IClubRegistryStore store, ClubQueryService queries)
    {
        Store = store;
        Queries = queries;
    }

    public IClubRegistryStore Store { get; }

    public ClubQueryService Queries { get; }

    public static ClubGovernanceModule CreateMvpLeague()
    {
        var registry = LeagueClubRegistry.CreateMvpLeague();
        var store = new InMemoryClubRegistryStore(registry);
        return new ClubGovernanceModule(store, new ClubQueryService(store));
    }
}
