using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Simulation.TeamPreparation;

/// <summary>
/// Aynı girdide her zaman aynı sonucu veren çift faz uyum etkisi.
/// Klasik taktik modifier'ına eklenmek üzere dar bir -2..+2 aralığında tutulur.
/// </summary>
public static class DualPhaseTacticMatchModifier
{
    public static int Compute(TacticPlan? legacyPlan, DualPhaseTacticPlan? phasePlan)
    {
        if (legacyPlan is null || phasePlan is null)
        {
            return 0;
        }

        var score = FormationTransitionScore(phasePlan);
        score += InPossessionRoleScore(phasePlan);
        score += OutOfPossessionRoleScore(legacyPlan, phasePlan);

        return Math.Clamp(score, -2, 2);
    }

    private static int FormationTransitionScore(DualPhaseTacticPlan plan)
    {
        if (plan.InPossessionFormation == plan.OutOfPossessionFormation)
        {
            return 0;
        }

        return (plan.InPossessionFormation, plan.OutOfPossessionFormation) switch
        {
            (Formation.F433, Formation.F442) => 1,
            (Formation.F352, Formation.F442) => 1,
            (Formation.F442, Formation.F352) => -1,
            _ => 0,
        };
    }

    private static int InPossessionRoleScore(DualPhaseTacticPlan plan) =>
        (plan.InPossessionFormation, plan.InPossessionRole) switch
        {
            (Formation.F433, TacticalPhaseRole.WideOverloads) => 1,
            (Formation.F352, TacticalPhaseRole.CentralOverloads) => 1,
            (Formation.F442, TacticalPhaseRole.DirectRunners) => 1,
            (Formation.F352, TacticalPhaseRole.WideOverloads) => -1,
            _ => 0,
        };

    private static int OutOfPossessionRoleScore(
        TacticPlan legacyPlan,
        DualPhaseTacticPlan phasePlan) =>
        (phasePlan.OutOfPossessionFormation, phasePlan.OutOfPossessionRole, legacyPlan.Pressing) switch
        {
            (Formation.F442, TacticalPhaseRole.CompactBlock, not PressingIntensity.HighPress) => 1,
            (Formation.F433 or Formation.F352, TacticalPhaseRole.AggressivePress, PressingIntensity.HighPress) => 1,
            (_, TacticalPhaseRole.AggressivePress, PressingIntensity.LowBlock) => -1,
            (_, TacticalPhaseRole.CompactBlock, PressingIntensity.HighPress) => -1,
            _ => 0,
        };
}
