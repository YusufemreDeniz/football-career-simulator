namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Simulation.Match;

public sealed class PlayFixtureMatchHandler : ICommandIdempotencyReset
{
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly IClubRegistryStore _clubRegistryStore;
    private readonly IWorldTimelineStore _timelineStore;
    private readonly Dictionary<Guid, PlayFixtureMatchResult> _completedCommands = new();

    public PlayFixtureMatchHandler(
        ILeagueCompetitionStore competitionStore,
        IClubRegistryStore clubRegistryStore,
        IWorldTimelineStore timelineStore)
    {
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _clubRegistryStore = clubRegistryStore ?? throw new ArgumentNullException(nameof(clubRegistryStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public PlayFixtureMatchResult Handle(PlayFixtureMatchCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var occurredAt = CompetitionSeasonCommandSupport.ToGameDate(command.OccurredAtDayNumber);
        var season = CompetitionSeasonCommandSupport.GetSeasonOrThrow(_competitionStore, command.SeasonId);
        var fixture = season.Fixtures.FirstOrDefault(candidate => candidate.Id.Value == command.FixtureId)
            ?? throw new CompetitionInvariantViolationException($"Fixture {command.FixtureId} was not found.");

        if (fixture.Status is not FixtureStatus.Planned)
        {
            throw new CompetitionInvariantViolationException(
                "Only planned fixtures can be played.");
        }

        if (fixture.ScheduledDate.DayNumber > occurredAt.DayNumber)
        {
            throw new CompetitionInvariantViolationException(
                "A fixture cannot be played before its scheduled date.");
        }

        var homeClub = _clubRegistryStore.Registry.GetClubOrThrow(fixture.HomeClubId);
        var awayClub = _clubRegistryStore.Registry.GetClubOrThrow(fixture.AwayClubId);
        var score = MvpFixtureMatchSimulator.Simulate(
            _timelineStore.Timeline.RootSeed,
            command.FixtureId,
            homeClub.SportiveStrength,
            awayClub.SportiveStrength);

        _competitionStore.League.AcceptFixtureResult(
            new SeasonId(command.SeasonId),
            new FixtureId(command.FixtureId),
            score,
            occurredAt);

        season.ClearUncommittedEvents();

        var result = new PlayFixtureMatchResult(
            true,
            command.SeasonId,
            command.FixtureId,
            score.HomeGoals,
            score.AwayGoals,
            nameof(FixtureStatus.ResultAccepted));

        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
