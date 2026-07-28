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

public sealed class ApproveMatchSelectionHandler : ICommandIdempotencyReset
{
    private readonly IMatchSelectionStore _selectionStore;
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly ITrainingPhysicalStateStore? _trainingStore;
    private readonly IWorldTimelineStore? _timelineStore;
    private readonly IClubSquadStore? _squadStore;
    private readonly Dictionary<Guid, ApproveMatchSelectionResult> _completedCommands = new();

    public ApproveMatchSelectionHandler(
        IMatchSelectionStore selectionStore,
        ILeagueCompetitionStore competitionStore,
        ITrainingPhysicalStateStore? trainingStore = null,
        IWorldTimelineStore? timelineStore = null,
        IClubSquadStore? squadStore = null)
    {
        _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _trainingStore = trainingStore;
        _timelineStore = timelineStore;
        _squadStore = squadStore;
    }

    public ApproveMatchSelectionResult Handle(ApproveMatchSelectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var fixtureId = new FixtureId(command.FixtureId);
        var clubId = new ClubId(command.ClubId);
        EnsurePlannedParticipant(fixtureId, clubId);

        var clubSquad = _squadStore?.Get(clubId);
        if (_trainingStore is not null && _timelineStore is not null)
        {
            MvpAvailabilityAwareSelection.EnsureStartingXiAvailable(
                clubId,
                command.StartingSlotIndices,
                _timelineStore.Timeline.CurrentDate,
                _trainingStore.PhysicalBySlot);
        }

        var selection = MatchSelection.Approve(
            fixtureId,
            clubId,
            command.StartingSlotIndices,
            command.BenchSlotIndices,
            clubSquad);
        _selectionStore.Upsert(selection);

        var result = new ApproveMatchSelectionResult(
            true,
            command.FixtureId,
            command.ClubId,
            nameof(MatchSelectionStatus.Approved),
            selection.StartingSlotIndices,
            selection.BenchSlotIndices);
        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();

    private void EnsurePlannedParticipant(FixtureId fixtureId, ClubId clubId)
    {
        Fixture? fixture = null;
        foreach (var season in _competitionStore.League.Seasons)
        {
            fixture = season.Fixtures.FirstOrDefault(candidate => candidate.Id == fixtureId);
            if (fixture is not null)
            {
                break;
            }
        }

        if (fixture is null)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Fixture {fixtureId.Value} was not found.");
        }

        if (fixture.Status is not FixtureStatus.Planned)
        {
            throw new TeamPreparationInvariantViolationException(
                "Only planned fixtures can receive a match selection.");
        }

        if (fixture.HomeClubId != clubId && fixture.AwayClubId != clubId)
        {
            throw new TeamPreparationInvariantViolationException(
                $"Club {clubId.Value} does not participate in fixture {fixtureId.Value}.");
        }
    }
}
