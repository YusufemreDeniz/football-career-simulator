namespace FootballCareerSimulator.Domain.ManagerCareer;

/// <summary>
/// Manager'ın futbol dünyasındaki kurumsal/sportif tanınırlığı (Board Confidence değildir).
/// Exact tier/formül açık; 0–100 iskelet ölçeği.
/// </summary>
public readonly record struct ManagerReputation
{
    public const int MinValue = 0;
    public const int MaxValue = 100;
    public const int DefaultInitialValue = 50;

    public ManagerReputation(int value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ManagerCareerInvariantViolationException(
                $"Manager reputation must be between {MinValue} and {MaxValue}.");
        }

        Value = value;
    }

    public int Value { get; }

    public ManagerReputation Adjust(int delta) =>
        new(Math.Clamp(Value + delta, MinValue, MaxValue));
}
