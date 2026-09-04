namespace FootballCareerSimulator.Domain.Competition;

public readonly record struct GoalDifference : IComparable<GoalDifference>
{
    public int Value { get; }

    public GoalDifference(int value) => Value = value;

    public GoalDifference Add(GoalDifference other) => new(Value + other.Value);

    public int CompareTo(GoalDifference other) => Value.CompareTo(other.Value);
}
