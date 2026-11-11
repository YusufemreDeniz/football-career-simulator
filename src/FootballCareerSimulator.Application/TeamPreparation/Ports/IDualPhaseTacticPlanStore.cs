using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Ports;

public interface IDualPhaseTacticPlanStore
{
    IReadOnlyList<DualPhaseTacticPlan> Plans { get; }

    DualPhaseTacticPlan? Get(ClubId clubId);

    void Upsert(DualPhaseTacticPlan plan);

    void ReplaceAll(IReadOnlyList<DualPhaseTacticPlan> plans);
}
