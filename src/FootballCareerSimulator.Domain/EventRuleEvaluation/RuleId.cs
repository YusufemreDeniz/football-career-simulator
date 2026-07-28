namespace FootballCareerSimulator.Domain.EventRuleEvaluation;

/// <summary>
/// Reaction rule kimliği (docs/04 §7.4 / §7.8). Business state taşımaz.
/// </summary>
public readonly record struct RuleId
{
    public RuleId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("RuleId cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
