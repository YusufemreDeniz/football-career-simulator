namespace FootballCareerSimulator.Domain.EventRuleEvaluation;

/// <summary>
/// Consumer effect idempotency anahtarı (docs/04 §11). Processing ledger değildir.
/// </summary>
public readonly record struct EventEffectProcessingKey
{
    public EventEffectProcessingKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Processing key cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;

    public static EventEffectProcessingKey ForConsumerEffect(
        string consumerId,
        Guid eventId,
        string effectType) =>
        new($"{consumerId}|{effectType}|{eventId:N}");
}
