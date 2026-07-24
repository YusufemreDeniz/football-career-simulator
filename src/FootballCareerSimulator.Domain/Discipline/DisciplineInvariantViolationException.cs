namespace FootballCareerSimulator.Domain.Discipline;

public sealed class DisciplineInvariantViolationException : Exception
{
    public DisciplineInvariantViolationException(string message)
        : base(message)
    {
    }
}
