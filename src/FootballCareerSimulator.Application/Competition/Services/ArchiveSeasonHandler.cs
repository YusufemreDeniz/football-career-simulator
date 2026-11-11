namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;

public sealed class ArchiveSeasonHandler : ICommandIdempotencyReset
{
    private readonly ILeagueCompetitionStore _store;
    private readonly Dictionary<Guid, ArchiveSeasonResult> _completedCommands = new();

    public ArchiveSeasonHandler(ILeagueCompetitionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ArchiveSeasonResult Handle(ArchiveSeasonCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var occurredAt = CompetitionSeasonCommandSupport.ToGameDate(command.OccurredAtDayNumber);
        _store.League.ArchiveSeason(new SeasonId(command.SeasonId), occurredAt);

        var season = CompetitionSeasonCommandSupport.GetSeasonOrThrow(_store, command.SeasonId);
        season.ClearUncommittedEvents();

        var result = new ArchiveSeasonResult(true, command.SeasonId, season.Status.ToString());
        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
