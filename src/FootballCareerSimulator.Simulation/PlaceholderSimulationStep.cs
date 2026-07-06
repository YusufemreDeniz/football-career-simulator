namespace FootballCareerSimulator.Simulation;

/// <summary>
/// Kart 0 yer tutucu döngüsü için kullanılan sayaç. Gerçek <see cref="Domain.WorldCalendar.SimulationStepId"/>
/// ile karıştırılmamalıdır; Production Kart 3'te kaldırılacaktır.
/// </summary>
public readonly record struct PlaceholderSimulationStep
{
    public long Value { get; }

    public PlaceholderSimulationStep(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Simulation step cannot be negative.");
        }

        Value = value;
    }

    public static PlaceholderSimulationStep Zero => new(0);

    public PlaceholderSimulationStep Next() => new(Value + 1);
}
