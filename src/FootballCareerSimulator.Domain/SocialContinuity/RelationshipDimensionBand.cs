namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Relationship boyutunun niteliksel bandı (label / milestone eşikleri).
/// </summary>
public enum RelationshipDimensionBand
{
    Low = 1,
    Neutral = 2,
    High = 3,
}

public static class RelationshipDimensionBands
{
    public const int LowMaxInclusive = 33;
    public const int HighMinInclusive = 67;

    public static RelationshipDimensionBand FromValue(int value)
    {
        if (value is < RelationshipRecord.MinDimension or > RelationshipRecord.MaxDimension)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Relationship dimension must be between {RelationshipRecord.MinDimension} and {RelationshipRecord.MaxDimension}.");
        }

        if (value <= LowMaxInclusive)
        {
            return RelationshipDimensionBand.Low;
        }

        if (value >= HighMinInclusive)
        {
            return RelationshipDimensionBand.High;
        }

        return RelationshipDimensionBand.Neutral;
    }
}
