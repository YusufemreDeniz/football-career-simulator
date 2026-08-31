namespace FootballCareerSimulator.Application.ClubGovernance.Composition;

using FootballCareerSimulator.Application.ClubGovernance.Infrastructure;
using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;

/// <summary>
/// Manuel composition root (D-348).
/// </summary>
public sealed class ClubGovernanceModule
{
    public ClubGovernanceModule(
        IClubRegistryStore store,
        ClubQueryService queries,
        ClubTransferBudgetService transferBudget,
        ClubWageBudgetService? wageBudget = null)
    {
        Store = store;
        Queries = queries;
        TransferBudget = transferBudget;
        WageBudget = wageBudget;
    }

    public IClubRegistryStore Store { get; }

    public ClubQueryService Queries { get; }

    public ClubTransferBudgetService TransferBudget { get; }

    public ClubWageBudgetService? WageBudget { get; private set; }

    public void BindWageBudget(IContractStore contractStore)
    {
        ArgumentNullException.ThrowIfNull(contractStore);
        WageBudget = new ClubWageBudgetService(Store, contractStore);
    }

    public static ClubGovernanceModule Create(LeagueClubRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var store = new InMemoryClubRegistryStore(registry);
        return new ClubGovernanceModule(
            store,
            new ClubQueryService(store),
            new ClubTransferBudgetService(store));
    }

    public static ClubGovernanceModule CreateMvpLeague() =>
        Create(LeagueClubRegistry.CreateMvpLeague());
}
