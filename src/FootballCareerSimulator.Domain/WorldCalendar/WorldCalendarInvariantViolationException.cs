namespace FootballCareerSimulator.Domain.WorldCalendar;

public sealed class WorldCalendarInvariantViolationException : Exception
{
    public WorldCalendarInvariantViolationException(string message)
        : base(message)
    {
    }
}
