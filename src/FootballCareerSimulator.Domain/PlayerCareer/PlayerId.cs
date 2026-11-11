namespace FootballCareerSimulator.Domain.PlayerCareer;

public readonly record struct PlayerId : IComparable<PlayerId>
{
    public long Value { get; }

    public PlayerId(long value)
    {
        if (value <= 0)
        {
            throw new PlayerCareerInvariantViolationException("PlayerId must be positive.");
        }

        Value = value;
    }

    public int CompareTo(PlayerId other) => Value.CompareTo(other.Value);

    /// <summary>
    /// MVP: kulüp+slot'tan sentetik kimlik. Kalıcı dünya üretiminde ayrı generator'a taşınır.
    /// </summary>
    public static PlayerId FromClubSlot(long clubId, int slotIndex) =>
        new((clubId * 1000L) + slotIndex + 1);

    public static PlayerId FromClubSlotGeneration(long clubId, int slotIndex, int generation)
    {
        if (generation <= 0)
        {
            throw new PlayerCareerInvariantViolationException("Generated player generation must be positive.");
        }

        return new(1_000_000_000L + (clubId * 1_000_000L) + (generation * 1000L) + slotIndex + 1);
    }
}
