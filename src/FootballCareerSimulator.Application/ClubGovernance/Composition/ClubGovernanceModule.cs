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
    public ClubGovernanceModule(
        IClubRegistryStore store,
        ClubQueryService queries,
        ClubTransferBudgetService transferBudget)
    {
        Store = store;
        Queries = queries;
        TransferBudget = transferBudget;
    }

    public IClubRegistryStore Store { get; }

    public ClubQueryService Queries { get; }

    public ClubTransferBudgetService TransferBudget { get; }

    public static ClubGovernanceModule CreateMvpLeague()
    {
        var registry = LeagueClubRegistry.CreateMvpLeague();
        var store = new InMemoryClubRegistryStore(registry);
        return new ClubGovernanceModule(
            store,
            new ClubQueryService(store),
            new ClubTransferBudgetService(store));
    }
}
