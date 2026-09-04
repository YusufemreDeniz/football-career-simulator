using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Ports;

public interface ITacticPlanStore
{
    IReadOnlyList<TacticPlan> Plans { get; }

    TacticPlan? Get(ClubId clubId);

    void Upsert(TacticPlan plan);

    void ReplaceAll(IEnumerable<TacticPlan> plans);
}
