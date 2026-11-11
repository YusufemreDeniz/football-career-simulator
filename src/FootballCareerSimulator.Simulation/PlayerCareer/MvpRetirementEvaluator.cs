using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.PlayerCareer;

public static class MvpRetirementEvaluator
{
    public static RetirementEvaluation Evaluate(
        Domain.PlayerCareer.PlayerCareer career,
        GameDate day,
        int rootSeed)
    {
        ArgumentNullException.ThrowIfNull(career);
        if (career.IsRetired)
        {
            return RetirementEvaluation.Continue("AlreadyRetired");
        }

        var age = career.AgeYears(day);
        if (age < Domain.PlayerCareer.PlayerCareer.RetirementEligibleAge)
        {
            return RetirementEvaluation.Continue("BelowCandidateAge");
        }

        var seed = unchecked(
            (rootSeed * 397)
            ^ ((int)career.Id.Value * 7919)
            ^ (career.Generation * 104729));
        var random = new SimulationRandomContext(seed);
        var individualRetirementAge = random.NextInt(35, 41);

        if (career.CurrentAbility >= 72)
        {
            individualRetirementAge++;
        }
        else if (career.CurrentAbility <= 50)
        {
            individualRetirementAge--;
        }

        return age >= individualRetirementAge
            ? RetirementEvaluation.Retire(
                PlayerRetirementReason.AgeAndDecline,
                $"Age{age}:Threshold{individualRetirementAge}:Ability{career.CurrentAbility}")
            : RetirementEvaluation.ReevaluateLater(
                $"Age{age}:Threshold{individualRetirementAge}:Ability{career.CurrentAbility}");
    }
}

public enum RetirementEvaluationDecision
{
    Continue = 1,
    ReevaluateLater = 2,
    Retire = 3,
}

public sealed record RetirementEvaluation(
    RetirementEvaluationDecision Decision,
    PlayerRetirementReason? Reason,
    string Explanation)
{
    public static RetirementEvaluation Continue(string explanation) =>
        new(RetirementEvaluationDecision.Continue, null, explanation);

    public static RetirementEvaluation ReevaluateLater(string explanation) =>
        new(RetirementEvaluationDecision.ReevaluateLater, null, explanation);

    public static RetirementEvaluation Retire(PlayerRetirementReason reason, string explanation) =>
        new(RetirementEvaluationDecision.Retire, reason, explanation);
}
