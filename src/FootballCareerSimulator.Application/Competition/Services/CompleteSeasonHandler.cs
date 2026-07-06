namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;

public sealed class CompleteSeasonHandler : ICommandIdempotencyReset
{
    private readonly ILeagueCompetitionStore _store;
    private readonly Dictionary<Guid, CompleteSeasonResult> _completedCommands = new();

    public CompleteSeasonHandler(ILeagueCompetitionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public CompleteSeasonResult Handle(CompleteSeasonCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var occurredAt = CompetitionSeasonCommandSupport.ToGameDate(command.OccurredAtDayNumber);
        _store.League.CompleteSeason(new SeasonId(command.SeasonId), occurredAt);

        var season = CompetitionSeasonCommandSupport.GetSeasonOrThrow(_store, command.SeasonId);
        season.ClearUncommittedEvents();

        var result = new CompleteSeasonResult(true, command.SeasonId, season.Status.ToString());
        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
