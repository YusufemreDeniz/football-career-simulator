namespace FootballCareerSimulator.Simulation.WorldCalendar;

public static class DeterministicGuidFactory
{
    public static Guid Create(int seed, long sequence)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes[..4], seed);
        BitConverter.TryWriteBytes(bytes[4..12], sequence);
        BitConverter.TryWriteBytes(bytes[12..16], seed ^ (int)sequence);
        return new Guid(bytes);
    }
}
