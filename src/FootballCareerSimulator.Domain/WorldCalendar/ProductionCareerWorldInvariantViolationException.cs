namespace FootballCareerSimulator.Domain.WorldCalendar;

public sealed class ProductionCareerWorldInvariantViolationException : Exception
{
    public ProductionCareerWorldInvariantViolationException(string message)
        : base(message)
    {
    }
}
