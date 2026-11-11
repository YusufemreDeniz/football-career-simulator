namespace FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Mantıksal simülasyon adımının benzersiz kimliği. Frame veya wall-clock değildir
/// (bkz. docs/19_PRODUCTION_IMPLEMENTATION_PLAN.md Bölüm 5.1, D-345).
/// </summary>
public readonly record struct SimulationStepId : IComparable<SimulationStepId>
{
    public long Value { get; }

    public SimulationStepId(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Simulation step id cannot be negative.");
        }

        Value = value;
    }

    public static SimulationStepId Zero => new(0);

    public SimulationStepId Next() => new(checked(Value + 1));

    public int CompareTo(SimulationStepId other) => Value.CompareTo(other.Value);

    public static bool operator <(SimulationStepId left, SimulationStepId right) => left.Value < right.Value;

    public static bool operator >(SimulationStepId left, SimulationStepId right) => left.Value > right.Value;

    public static bool operator <=(SimulationStepId left, SimulationStepId right) => left.Value <= right.Value;

    public static bool operator >=(SimulationStepId left, SimulationStepId right) => left.Value >= right.Value;
}
