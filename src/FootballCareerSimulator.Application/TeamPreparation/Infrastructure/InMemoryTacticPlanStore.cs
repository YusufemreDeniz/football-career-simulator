using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Infrastructure;

public sealed class InMemoryTacticPlanStore : ITacticPlanStore
{
    private readonly Dictionary<long, TacticPlan> _byClub = new();

    public IReadOnlyList<TacticPlan> Plans =>
        _byClub.Values.OrderBy(p => p.ClubId.Value).ToArray();

    public TacticPlan? Get(ClubId clubId) =>
        _byClub.TryGetValue(clubId.Value, out var plan) ? plan : null;

    public void Upsert(TacticPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _byClub[plan.ClubId.Value] = plan;
    }

    public void ReplaceAll(IEnumerable<TacticPlan> plans)
    {
        ArgumentNullException.ThrowIfNull(plans);
        _byClub.Clear();
        foreach (var plan in plans)
        {
            Upsert(plan);
        }
    }
}
