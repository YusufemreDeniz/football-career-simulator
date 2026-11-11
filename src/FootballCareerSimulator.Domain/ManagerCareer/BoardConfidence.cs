namespace FootballCareerSimulator.Domain.ManagerCareer;

public readonly record struct BoardConfidence
{
    public const int MinValue = 0;
    public const int MaxValue = 100;
    public const int DefaultInitialValue = 55;

    public BoardConfidence(int value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ManagerCareerInvariantViolationException(
                $"Board confidence must be between {MinValue} and {MaxValue}.");
        }

        Value = value;
    }

    public int Value { get; }

    public BoardConfidence Adjust(int delta) =>
        new(Math.Clamp(Value + delta, MinValue, MaxValue));
}
