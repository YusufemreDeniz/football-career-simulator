namespace FootballCareerSimulator.Domain.Discipline;

public readonly record struct DisciplinaryActionId : IComparable<DisciplinaryActionId>
{
    public DisciplinaryActionId(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Disciplinary action id must be positive.");
        }

        Value = value;
    }

    public long Value { get; }

    public int CompareTo(DisciplinaryActionId other) => Value.CompareTo(other.Value);
}
