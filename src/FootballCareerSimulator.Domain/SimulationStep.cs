namespace FootballCareerSimulator.Domain;

/// <summary>
/// Repository iskeletinin derlenebilir, katmanlı ve test edilebilir olduğunu kanıtlamak için kullanılan
/// yer tutucu bir değer nesnesidir (bkz. docs/18_SPIKE_EXECUTION_PLAN.md, Kart 0). 14 bounded context'ten
/// hiçbirinin gerçek domain modelini temsil etmez; gerçek domain modeli docs/03_DOMAIN_MODEL.md kapsamında
/// ayrı bir çalışmayla implemente edilecektir.
/// </summary>
public readonly record struct SimulationStep
{
    public long Value { get; }

    public SimulationStep(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Simulation step cannot be negative.");
        }

        Value = value;
    }

    public static SimulationStep Zero => new(0);

    public SimulationStep Next() => new(Value + 1);
}
