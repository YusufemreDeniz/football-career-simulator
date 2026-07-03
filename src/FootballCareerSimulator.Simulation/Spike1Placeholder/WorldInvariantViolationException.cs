namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 1 yer tutucu dünya modelinde bir invariant ihlali tespit edildiğinde fırlatılır.
/// </summary>
public sealed class WorldInvariantViolationException : Exception
{
    public WorldInvariantViolationException(string message)
        : base(message)
    {
    }
}
