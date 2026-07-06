namespace FootballCareerSimulator.Domain.Shared;

/// <summary>
/// Kulüp kimliği. Authoritative owner Club &amp; Governance; Competition yalnızca referans taşır.
/// </summary>
public readonly record struct ClubId : IComparable<ClubId>
{
    public long Value { get; }

    public ClubId(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Club id must be positive.");
        }

        Value = value;
    }

    public int CompareTo(ClubId other) => Value.CompareTo(other.Value);
}
