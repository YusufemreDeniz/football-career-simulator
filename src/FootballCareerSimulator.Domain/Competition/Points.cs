namespace FootballCareerSimulator.Domain.Competition;

public readonly record struct Points : IComparable<Points>
{
    public const int MaxPerMatch = 3;

    public int Value { get; }

    public Points(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Points cannot be negative.");
        }

        Value = value;
    }

    public static Points Zero => new(0);

    public Points Add(Points other) => new(Value + other.Value);

    public int CompareTo(Points other) => Value.CompareTo(other.Value);
}
