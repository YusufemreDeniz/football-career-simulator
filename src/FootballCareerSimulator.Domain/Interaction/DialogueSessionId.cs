namespace FootballCareerSimulator.Domain.Interaction;

public readonly record struct DialogueSessionId : IComparable<DialogueSessionId>
{
    public DialogueSessionId(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Dialogue session id must be positive.");
        }

        Value = value;
    }

    public long Value { get; }

    public int CompareTo(DialogueSessionId other) => Value.CompareTo(other.Value);
}
