namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;

public sealed class CreateSeasonHandler : ICommandIdempotencyReset
{
    private readonly ILeagueCompetitionStore _store;
    private readonly Dictionary<Guid, CreateSeasonResult> _completedCommands = new();

    public CreateSeasonHandler(ILeagueCompetitionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public CreateSeasonResult Handle(CreateSeasonCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var preseasonStart = CompetitionSeasonCommandSupport.ToGameDate(command.PreseasonStartDayNumber);
        var season = _store.League.CreateSeason(new SeasonId(command.SeasonId), preseasonStart);
        season.ClearUncommittedEvents();

        var result = new CreateSeasonResult(true, season.SeasonId.Value, season.Status.ToString());
        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
