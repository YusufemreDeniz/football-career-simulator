namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.Match;
using FootballCareerSimulator.Simulation.TeamPreparation;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

public sealed class PlayFixtureMatchHandler : ICommandIdempotencyReset
{
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly IClubRegistryStore _clubRegistryStore;
    private readonly IWorldTimelineStore _timelineStore;
    private readonly IManagerCareerStore? _managerCareerStore;
    private readonly IMatchSelectionStore? _matchSelectionStore;
    private readonly ITrainingPhysicalStateStore? _trainingStore;
    private readonly Dictionary<Guid, PlayFixtureMatchResult> _completedCommands = new();

    public PlayFixtureMatchHandler(
        ILeagueCompetitionStore competitionStore,
        IClubRegistryStore clubRegistryStore,
        IWorldTimelineStore timelineStore,
        IManagerCareerStore? managerCareerStore = null,
        IMatchSelectionStore? matchSelectionStore = null,
        ITrainingPhysicalStateStore? trainingStore = null)
    {
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _clubRegistryStore = clubRegistryStore ?? throw new ArgumentNullException(nameof(clubRegistryStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _managerCareerStore = managerCareerStore;
        _matchSelectionStore = matchSelectionStore;
        _trainingStore = trainingStore;
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

        var homeBonus = ResolveLineupBonus(fixture.Id, fixture.HomeClubId, rootSeed)
            + ResolvePhysicalModifier(fixture.Id, fixture.HomeClubId);
        var awayBonus = ResolveLineupBonus(fixture.Id, fixture.AwayClubId, rootSeed)
            + ResolvePhysicalModifier(fixture.Id, fixture.AwayClubId);

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

        var updatedSeason = CompetitionSeasonCommandSupport.GetSeasonOrThrow(
            _competitionStore,
            command.SeasonId);
        TryApplyBoardAssessment(fixture, score, updatedSeason, occurredAt);
        updatedSeason.ClearUncommittedEvents();

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

    private void TryApplyBoardAssessment(
        Fixture fixture,
        MatchScore score,
        CompetitionSeason season,
        GameDate occurredAt)
    {
        if (_managerCareerStore is null)
        {
            return;
        }

        var employment = _managerCareerStore.Career.ActiveEmployment;
        if (employment is null)
        {
            return;
        }

        var managedClubId = employment.ClubId;
        if (fixture.HomeClubId != managedClubId && fixture.AwayClubId != managedClubId)
        {
            return;
        }

        var isHome = fixture.HomeClubId == managedClubId;
        var managedGoals = isHome ? score.HomeGoals : score.AwayGoals;
        var opponentGoals = isHome ? score.AwayGoals : score.HomeGoals;
        var outcome = managedGoals > opponentGoals
            ? MatchOutcomeForManagedClub.Win
            : managedGoals == opponentGoals
                ? MatchOutcomeForManagedClub.Draw
                : MatchOutcomeForManagedClub.Loss;

        var standings = season.Standings.Entries;
        var leagueSize = standings.Count > 0 ? standings.Count : season.Participants.Count;
        var position = 1;
        for (var i = 0; i < standings.Count; i++)
        {
            if (standings[i].ClubId == managedClubId)
            {
                position = i + 1;
                break;
            }
        }

        var assessment = _managerCareerStore.Career.ApplyMatchBoardAssessment(
            fixture.Id,
            outcome,
            position,
            Math.Max(leagueSize, 1));

        var career = assessment.Career;
        if (assessment.WasApplied && assessment.RiskBand == EmploymentRiskBand.Critical)
        {
            var dismissal = career.DismissDueToBoardConfidence(fixture.Id, occurredAt);
            career = dismissal.Career;
        }

        _managerCareerStore.Replace(career);
    }

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

    private int ResolvePhysicalModifier(FixtureId fixtureId, ClubId clubId)
    {
        if (_trainingStore is null || _trainingStore.GetPlan(clubId) is null)
        {
            return 0;
        }

        IReadOnlyList<int> startingSlots;
        var selection = _matchSelectionStore?.Get(fixtureId, clubId);
        if (selection is not null)
        {
            startingSlots = selection.StartingSlotIndices;
        }
        else
        {
            startingSlots = Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray();
        }

        return MvpPhysicalMatchModifier.ComputeLineupModifier(
            clubId,
            startingSlots,
            _trainingStore.PhysicalBySlot);
    }
}
