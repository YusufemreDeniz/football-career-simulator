namespace FootballCareerSimulator.Application.WorldCalendar.Ports;

public interface ICommandIdempotencyReset
{
    void ResetIdempotencyCache();
}
