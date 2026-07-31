using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

public sealed class SwapStarterWithBenchHandler : ICommandIdempotencyReset
{
    private readonly IMatchSelectionStore _selectionStore;
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly ITrainingPhysicalStateStore? _trainingStore;
    private readonly IWorldTimelineStore? _timelineStore;
    private readonly IClubSquadStore? _squadStore;
    private readonly Dictionary<Guid, SwapStarterWithBenchResult> _completedCommands = new();

    public SwapStarterWithBenchHandler(
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

    public SwapStarterWithBenchResult Handle(SwapStarterWithBenchCommand command)
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
        var current = _selectionStore.Get(fixtureId, clubId);
        if (current is null)
        {
            current = _trainingStore is not null && _timelineStore is not null
                ? MvpAvailabilityAwareSelection.ApproveDefaultPreferringAvailable(
                    fixtureId,
                    clubId,
                    _timelineStore.Timeline.CurrentDate,
                    _trainingStore.PhysicalBySlot,
                    clubSquad)
                : MatchSelection.ApproveDefault(fixtureId, clubId, clubSquad);
        }

        var day = _timelineStore?.Timeline.CurrentDate;
        var physical = _trainingStore is not null && day is not null
            ? _trainingStore.PhysicalBySlot
            : null;

        var outSlot = current.StartingSlotIndices[command.StartingIndex];
        var inSlot = current.BenchSlotIndices[command.BenchIndex];

        var swapped = MvpAvailabilityAwareSelection.SwapStarterWithBench(
            current,
            command.StartingIndex,
            command.BenchIndex,
            day ?? GameDate.FromDayNumber(1),
            physical,
            clubSquad);
        _selectionStore.Upsert(swapped);

        string? swapSummary = null;
        string? halfTimeBridge = null;
        if (_timelineStore is not null)
        {
            var names = MvpSquadRosterGenerator.GeneratePlayerNames(
                clubId,
                _timelineStore.Timeline.RootSeed);
            swapSummary = SelectionAutoSwapWarning.FormatSubstitution(outSlot, inSlot, names);
            halfTimeBridge = SelectionAutoSwapWarning.FormatHalfTimeBridge(outSlot, inSlot, names);
        }

        var result = new SwapStarterWithBenchResult(
            true,
            command.FixtureId,
            command.ClubId,
            swapped.StartingSlotIndices,
            swapped.BenchSlotIndices,
            outSlot,
            inSlot,
            swapSummary,
            halfTimeBridge);
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
