using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

public sealed class ApproveDefaultMatchSelectionHandler : ICommandIdempotencyReset
{
    private readonly IMatchSelectionStore _selectionStore;
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly ITrainingPhysicalStateStore? _trainingStore;
    private readonly IWorldTimelineStore? _timelineStore;
    private readonly Dictionary<Guid, ApproveDefaultMatchSelectionResult> _completedCommands = new();

    public ApproveDefaultMatchSelectionHandler(
        IMatchSelectionStore selectionStore,
        ILeagueCompetitionStore competitionStore,
        ITrainingPhysicalStateStore? trainingStore = null,
        IWorldTimelineStore? timelineStore = null)
    {
        _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _trainingStore = trainingStore;
        _timelineStore = timelineStore;
    }

    public ApproveDefaultMatchSelectionResult Handle(ApproveDefaultMatchSelectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var fixtureId = new FixtureId(command.FixtureId);
        var clubId = new ClubId(command.ClubId);
        var fixture = FindFixtureOrThrow(fixtureId);

        if (fixture.Status is not FixtureStatus.Planned)
        {
            throw new TeamPreparationInvariantViolationException(
                "Only planned fixtures can receive a match selection.");
        }

        if (fixture.HomeClubId != clubId && fixture.AwayClubId != clubId)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Club {command.ClubId} does not participate in fixture {command.FixtureId}.");
        }

        MatchSelection selection;
        if (_trainingStore is not null && _timelineStore is not null)
        {
            selection = MvpAvailabilityAwareSelection.ApproveDefaultPreferringAvailable(
                fixtureId,
                clubId,
                _timelineStore.Timeline.CurrentDate,
                _trainingStore.PhysicalBySlot);
        }
        else
        {
            selection = MatchSelection.ApproveDefault(fixtureId, clubId);
        }

        _selectionStore.Upsert(selection);

        var result = new ApproveDefaultMatchSelectionResult(
            true,
            command.FixtureId,
            command.ClubId,
            nameof(MatchSelectionStatus.Approved));

        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();

    private Fixture FindFixtureOrThrow(FixtureId fixtureId)
    {
        foreach (var season in _competitionStore.League.Seasons)
        {
            var fixture = season.Fixtures.FirstOrDefault(candidate => candidate.Id == fixtureId);
            if (fixture is not null)
            {
                return fixture;
            }
        }

        throw new TeamPreparationInvariantViolationException(
            $"Fixture {fixtureId.Value} was not found.");
    }
}
