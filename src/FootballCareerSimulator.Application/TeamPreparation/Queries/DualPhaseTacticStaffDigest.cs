using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

public enum DualPhaseTacticRisk
{
    Low = 1,
    Medium = 2,
    High = 3,
}

public sealed record DualPhaseTacticStaffDigest(
    int Compatibility,
    DualPhaseTacticRisk Risk,
    int MatchModifier,
    string Headline,
    string StaffNote)
{
    public static DualPhaseTacticStaffDigest Compose(
        TacticPlan legacyPlan,
        DualPhaseTacticPlan phasePlan)
    {
        ArgumentNullException.ThrowIfNull(legacyPlan);
        ArgumentNullException.ThrowIfNull(phasePlan);

        if (legacyPlan.ClubId != phasePlan.ClubId)
        {
            throw new ArgumentException("Tactic plans must belong to the same club.", nameof(phasePlan));
        }

        var modifier = DualPhaseTacticMatchModifier.Compute(legacyPlan, phasePlan);
        var compatibility = 82;

        if (phasePlan.InPossessionFormation == phasePlan.OutOfPossessionFormation)
        {
            compatibility += 8;
        }
        else if (IsNaturalTransition(phasePlan))
        {
            compatibility += 4;
        }
        else
        {
            compatibility -= 12;
        }

        compatibility += modifier * 5;
        if (legacyPlan.Formation != phasePlan.InPossessionFormation)
        {
            compatibility -= 5;
        }

        compatibility = Math.Clamp(compatibility, 35, 100);
        var risk = compatibility switch
        {
            >= 85 => DualPhaseTacticRisk.Low,
            >= 65 => DualPhaseTacticRisk.Medium,
            _ => DualPhaseTacticRisk.High,
        };

        return new DualPhaseTacticStaffDigest(
            compatibility,
            risk,
            modifier,
            $"Faz uyumu %{compatibility} · {FormatRisk(risk)} risk",
            BuildStaffNote(phasePlan, modifier, risk));
    }

    private static bool IsNaturalTransition(DualPhaseTacticPlan plan) =>
        (plan.InPossessionFormation, plan.OutOfPossessionFormation) is
            (Formation.F433, Formation.F442)
            or (Formation.F352, Formation.F442);

    private static string BuildStaffNote(
        DualPhaseTacticPlan plan,
        int modifier,
        DualPhaseTacticRisk risk)
    {
        var transition = $"{FormatFormation(plan.InPossessionFormation)} hücumdan "
            + $"{FormatFormation(plan.OutOfPossessionFormation)} savunmaya geçiş";
        var effect = modifier switch
        {
            > 0 => $"maç gücüne +{modifier} uyum katkısı veriyor",
            < 0 => $"maç gücünde {modifier} uyum kaybı yaratıyor",
            _ => "maç gücünü değiştirmiyor",
        };
        var warning = risk == DualPhaseTacticRisk.High
            ? "; teknik ekip daha sade bir geçiş öneriyor"
            : string.Empty;
        return $"{transition} {effect}{warning}.";
    }

    private static string FormatFormation(Formation formation) => formation switch
    {
        Formation.F442 => "4-4-2",
        Formation.F433 => "4-3-3",
        Formation.F352 => "3-5-2",
        _ => formation.ToString(),
    };

    private static string FormatRisk(DualPhaseTacticRisk risk) => risk switch
    {
        DualPhaseTacticRisk.Low => "düşük",
        DualPhaseTacticRisk.Medium => "orta",
        DualPhaseTacticRisk.High => "yüksek",
        _ => risk.ToString(),
    };
}
