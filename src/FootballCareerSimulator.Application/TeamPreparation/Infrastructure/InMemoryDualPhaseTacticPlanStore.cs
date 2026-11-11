using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Infrastructure;

public sealed class InMemoryDualPhaseTacticPlanStore : IDualPhaseTacticPlanStore
{
    private readonly Dictionary<long, DualPhaseTacticPlan> _byClub = new();

    public IReadOnlyList<DualPhaseTacticPlan> Plans =>
        _byClub.Values.OrderBy(plan => plan.ClubId.Value).ToArray();

    public DualPhaseTacticPlan? Get(ClubId clubId) =>
        _byClub.GetValueOrDefault(clubId.Value);

    public void Upsert(DualPhaseTacticPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _byClub[plan.ClubId.Value] = plan;
    }

    public void ReplaceAll(IReadOnlyList<DualPhaseTacticPlan> plans)
    {
        ArgumentNullException.ThrowIfNull(plans);
        _byClub.Clear();
        foreach (var plan in plans)
        {
            Upsert(plan);
        }
    }
}
