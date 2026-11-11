namespace FootballCareerSimulator.Domain.SocialContinuity;

public readonly record struct RelationshipId : IComparable<RelationshipId>
{
    public long Value { get; }

    public RelationshipId(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Relationship id must be positive.");
        }

        Value = value;
    }

    public int CompareTo(RelationshipId other) => Value.CompareTo(other.Value);
}
