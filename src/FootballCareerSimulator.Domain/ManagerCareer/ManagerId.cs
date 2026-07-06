namespace FootballCareerSimulator.Domain.ManagerCareer;

public readonly record struct ManagerId
{
    public long Value { get; }

    public ManagerId(long value)
    {
        if (value < 1)
        {
            throw new ManagerCareerInvariantViolationException("Manager id must be positive.");
        }

        Value = value;
    }
}
