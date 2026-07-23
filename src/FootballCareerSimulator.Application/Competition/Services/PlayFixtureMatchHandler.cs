namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.Match;
using FootballCareerSimulator.Simulation.TeamPreparation;

public sealed class PlayFixtureMatchHandler : ICommandIdempotencyReset
{
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly IClubRegistryStore _clubRegistryStore;
    private readonly IWorldTimelineStore _timelineStore;
    private readonly IManagerCareerStore? _managerCareerStore;
    private readonly IMatchSelectionStore? _matchSelectionStore;
    private readonly Dictionary<Guid, PlayFixtureMatchResult> _completedCommands = new();

    public PlayFixtureMatchHandler(
        ILeagueCompetitionStore competitionStore,
        IClubRegistryStore clubRegistryStore,
        IWorldTimelineStore timelineStore,
        IManagerCareerStore? managerCareerStore = null,
        IMatchSelectionStore? matchSelectionStore = null)
    {
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _clubRegistryStore = clubRegistryStore ?? throw new ArgumentNullException(nameof(clubRegistryStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _managerCareerStore = managerCareerStore;
        _matchSelectionStore = matchSelectionStore;
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
        var rootSeed = _timelineStore.Timeline.RootSeed;

        var homeBonus = ResolveLineupBonus(fixture.Id, fixture.HomeClubId, rootSeed);
        var awayBonus = ResolveLineupBonus(fixture.Id, fixture.AwayClubId, rootSeed);

        var score = MvpFixtureMatchSimulator.Simulate(
            rootSeed,
            command.FixtureId,
            homeClub.SportiveStrength,
            awayClub.SportiveStrength,
            homeBonus,
            awayBonus);

        _competitionStore.League.AcceptFixtureResult(
            new SeasonId(command.SeasonId),
            new FixtureId(command.FixtureId),
            score,
            occurredAt);

        _matchSelectionStore?.RemoveForFixture(fixture.Id);
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

    private int ResolveLineupBonus(FixtureId fixtureId, ClubId clubId, int rootSeed)
    {
        var managedClubId = _managerCareerStore?.Career.ActiveEmployment?.ClubId;
        var isManagedClub = managedClubId is ClubId managed && managed == clubId;

        if (isManagedClub)
        {
            if (_matchSelectionStore is null)
            {
                throw new TeamPreparationInvariantViolationException(
                    "Managed club match requires a match selection store.");
            }

            var selection = _matchSelectionStore.Get(fixtureId, clubId)
                ?? throw new TeamPreparationInvariantViolationException(
                    $"Match selection is not approved for managed club {clubId.Value} on fixture {fixtureId.Value}.");

            return MvpSquadStrengthCalculator.ComputeLineupBonus(
                clubId,
                rootSeed,
                selection.StartingSlotIndices);
        }

        return MvpSquadStrengthCalculator.ComputeDefaultLineupBonus(clubId, rootSeed);
    }
}
