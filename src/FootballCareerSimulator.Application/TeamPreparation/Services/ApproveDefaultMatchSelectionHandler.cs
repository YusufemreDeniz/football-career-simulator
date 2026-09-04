using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.TeamPreparation;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

public sealed class ApproveDefaultMatchSelectionHandler : ICommandIdempotencyReset
{
    private readonly IMatchSelectionStore _selectionStore;
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly ITrainingPhysicalStateStore? _trainingStore;
    private readonly IWorldTimelineStore? _timelineStore;
    private readonly IClubSquadStore? _squadStore;
    private IPromiseStore? _promiseStore;
    private readonly Dictionary<Guid, ApproveDefaultMatchSelectionResult> _completedCommands = new();

    public ApproveDefaultMatchSelectionHandler(
        IMatchSelectionStore selectionStore,
        ILeagueCompetitionStore competitionStore,
        ITrainingPhysicalStateStore? trainingStore = null,
        IWorldTimelineStore? timelineStore = null,
        IClubSquadStore? squadStore = null,
        IPromiseStore? promiseStore = null)
    {
        _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _trainingStore = trainingStore;
        _timelineStore = timelineStore;
        _squadStore = squadStore;
        _promiseStore = promiseStore;
    }

    public void BindPromiseStore(IPromiseStore promiseStore) =>
        _promiseStore = promiseStore ?? throw new ArgumentNullException(nameof(promiseStore));

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

        var clubSquad = _squadStore?.Get(clubId);
        var previous = _selectionStore.GetLineupTemplate(clubId);

        MatchSelection selection;
        string? autoSwapSummary = null;
        if (_trainingStore is not null && _timelineStore is not null)
        {
            var day = _timelineStore.Timeline.CurrentDate;
            var physical = _trainingStore.PhysicalBySlot;
            IReadOnlyList<MvpAvailabilityAwareSelection.AvailabilityAutoSwap> swaps;
            if (previous is ClubLineupTemplate template)
            {
                selection = MvpAvailabilityAwareSelection.ApproveReusingPreviousPreferringAvailable(
                    fixtureId,
                    clubId,
                    day,
                    physical,
                    template.StartingSlotIndices,
                    template.BenchSlotIndices,
                    clubSquad);
                swaps = MvpAvailabilityAwareSelection.DiffStartingSlots(
                    template.StartingSlotIndices,
                    selection.StartingSlotIndices);
                autoSwapSummary = swaps.Count == 0
                    ? "önceki XI korundu"
                    : SelectionAutoSwapWarning.FormatToastSuffix(swaps, ResolvePlayerNames(clubId));
            }
            else
            {
                swaps = MvpAvailabilityAwareSelection.PreviewDefaultAvailabilitySwaps(
                    clubId,
                    day,
                    physical,
                    clubSquad);
                selection = MvpAvailabilityAwareSelection.ApproveDefaultPreferringAvailable(
                    fixtureId,
                    clubId,
                    day,
                    physical,
                    clubSquad);
                if (swaps.Count > 0)
                {
                    autoSwapSummary = SelectionAutoSwapWarning.FormatToastSuffix(swaps, ResolvePlayerNames(clubId));
                }
            }
        }
        else if (previous is ClubLineupTemplate template)
        {
            selection = MatchSelection.Approve(
                fixtureId,
                clubId,
                template.StartingSlotIndices,
                template.BenchSlotIndices,
                clubSquad);
            autoSwapSummary = "önceki XI korundu";
        }
        else
        {
            selection = MatchSelection.ApproveDefault(fixtureId, clubId, clubSquad);
        }

        var beforeHonor = selection;
        selection = PromiseAwareDefaultSelection.Honor(
            selection,
            clubSquad,
            _promiseStore,
            clubId,
            _timelineStore?.Timeline.CurrentDate,
            _trainingStore?.PhysicalBySlot);

        if (_timelineStore is not null)
        {
            var names = MvpSquadRosterGenerator.GeneratePlayerNames(
                clubId,
                _timelineStore.Timeline.RootSeed);
            var honorNote = PromiseAwareDefaultSelection.FormatHonorNote(beforeHonor, selection, names);
            if (!string.IsNullOrWhiteSpace(honorNote))
            {
                autoSwapSummary = string.IsNullOrWhiteSpace(autoSwapSummary)
                    ? honorNote
                    : $"{autoSwapSummary} · {honorNote}";
            }
        }

        _selectionStore.Upsert(selection);

        var result = new ApproveDefaultMatchSelectionResult(
            true,
            command.FixtureId,
            command.ClubId,
            nameof(MatchSelectionStatus.Approved),
            autoSwapSummary);

        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();

    private IReadOnlyList<string> ResolvePlayerNames(ClubId clubId) =>
        MvpSquadRosterGenerator.GeneratePlayerNames(
            clubId,
            _timelineStore?.Timeline.RootSeed ?? 0);

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
