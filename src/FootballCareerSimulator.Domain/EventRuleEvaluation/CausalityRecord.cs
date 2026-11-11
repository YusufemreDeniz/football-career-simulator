namespace FootballCareerSimulator.Domain.EventRuleEvaluation;

/// <summary>
/// Domain sonucunun nedensellik kaydı (docs/20 §6 / §29). Business state değiştirmez.
/// </summary>
public sealed class CausalityRecord
{
    public CausalityRecord(
        Guid targetEventId,
        CausalityDimension dimension,
        string explanationKey,
        int evaluatedAtDayNumber,
        IEnumerable<CausalFactor> factors)
    {
        if (targetEventId == Guid.Empty)
        {
            throw new ArgumentException("Target event ID cannot be empty.", nameof(targetEventId));
        }

        if (string.IsNullOrWhiteSpace(explanationKey))
        {
            throw new ArgumentException("Explanation key cannot be empty.", nameof(explanationKey));
        }

        TargetEventId = targetEventId;
        Dimension = dimension;
        ExplanationKey = explanationKey;
        EvaluatedAtDayNumber = evaluatedAtDayNumber;
        Factors = (factors ?? Array.Empty<CausalFactor>()).ToArray();
    }

    public Guid TargetEventId { get; }
    public CausalityDimension Dimension { get; }
    public string ExplanationKey { get; }
    public int EvaluatedAtDayNumber { get; }
    public IReadOnlyList<CausalFactor> Factors { get; }

    public CausalFactor? PrimaryCause => Factors.FirstOrDefault(f => f.Weight == CausalFactorWeight.Primary);
    public IReadOnlyList<CausalFactor> ContributingCauses => Factors.Where(f => f.Weight == CausalFactorWeight.Contributing).ToArray();
    public IReadOnlyList<CausalFactor> MitigatingCauses => Factors.Where(f => f.Weight == CausalFactorWeight.Mitigating).ToArray();
    public IReadOnlyList<CausalFactor> ContextualCauses => Factors.Where(f => f.Weight == CausalFactorWeight.Contextual).ToArray();
}
