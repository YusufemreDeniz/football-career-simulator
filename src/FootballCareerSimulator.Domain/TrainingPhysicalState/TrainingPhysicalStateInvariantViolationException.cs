namespace FootballCareerSimulator.Domain.TrainingPhysicalState;

public sealed class TrainingPhysicalStateInvariantViolationException : Exception
{
    public TrainingPhysicalStateInvariantViolationException(string message)
        : base(message)
    {
    }
}
