namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;

public sealed class PlanLeagueFixturesHandler : ICommandIdempotencyReset
{
    private readonly ILeagueCompetitionStore _store;
    private readonly Dictionary<Guid, PlanLeagueFixturesResult> _completedCommands = new();

    public PlanLeagueFixturesHandler(ILeagueCompetitionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public PlanLeagueFixturesResult Handle(PlanLeagueFixturesCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var firstMatchday = CompetitionSeasonCommandSupport.ToGameDate(command.FirstMatchdayDayNumber);
        _store.League.PlanLeagueFixtures(
            new SeasonId(command.SeasonId),
            firstMatchday,
            new FixtureId(command.StartingFixtureId),
            command.DaysBetweenRounds);

        var season = CompetitionSeasonCommandSupport.GetSeasonOrThrow(_store, command.SeasonId);
        season.ClearUncommittedEvents();

        var result = new PlanLeagueFixturesResult(
            true,
            command.SeasonId,
            season.Fixtures.Count,
            command.FirstMatchdayDayNumber);

        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
