namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Shared;

public sealed class RegisterSeasonParticipantHandler : ICommandIdempotencyReset
{
    private readonly ILeagueCompetitionStore _store;
    private readonly Dictionary<Guid, RegisterSeasonParticipantResult> _completedCommands = new();

    public RegisterSeasonParticipantHandler(ILeagueCompetitionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public RegisterSeasonParticipantResult Handle(RegisterSeasonParticipantCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var season = CompetitionSeasonCommandSupport.GetSeasonOrThrow(_store, command.SeasonId);
        season.RegisterParticipant(new ClubId(command.ClubId));
        season.ClearUncommittedEvents();

        var result = new RegisterSeasonParticipantResult(
            true,
            command.SeasonId,
            command.ClubId,
            season.Participants.Count);

        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
