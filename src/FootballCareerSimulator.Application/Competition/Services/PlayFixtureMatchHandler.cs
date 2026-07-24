namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.PlayerCareer;
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
    private readonly IPlayerCareerStore? _playerCareerStore;
    private readonly PlayerCareerDevelopmentService? _playerDevelopment;
    private readonly ITacticPlanStore? _tacticPlanStore;
    private readonly IClubSquadStore? _clubSquadStore;
    private readonly StartingOpportunityPromiseService? _startingOpportunityPromises;
    private readonly PlayingTimePromiseService? _playingTimePromises;
    private readonly SelectionMemoryService? _selectionMemory;
    private readonly PromiseInvalidationService? _promiseInvalidation;
    private readonly CareerMemoryService? _careerMemory;
    private readonly Dictionary<Guid, PlayFixtureMatchResult> _completedCommands = new();

    public PlayFixtureMatchHandler(
        ILeagueCompetitionStore competitionStore,
        IClubRegistryStore clubRegistryStore,
        IWorldTimelineStore timelineStore,
        IManagerCareerStore? managerCareerStore = null,
        IMatchSelectionStore? matchSelectionStore = null,
        ITrainingPhysicalStateStore? trainingStore = null,
        IPlayerCareerStore? playerCareerStore = null,
        PlayerCareerDevelopmentService? playerDevelopment = null,
        ITacticPlanStore? tacticPlanStore = null,
        IClubSquadStore? clubSquadStore = null,
        StartingOpportunityPromiseService? startingOpportunityPromises = null,
        SelectionMemoryService? selectionMemory = null,
        PlayingTimePromiseService? playingTimePromises = null,
        PromiseInvalidationService? promiseInvalidation = null,
        CareerMemoryService? careerMemory = null)
    {
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _clubRegistryStore = clubRegistryStore ?? throw new ArgumentNullException(nameof(clubRegistryStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _managerCareerStore = managerCareerStore;
        _matchSelectionStore = matchSelectionStore;
        _trainingStore = trainingStore;
        _playerCareerStore = playerCareerStore;
        _playerDevelopment = playerDevelopment;
        _tacticPlanStore = tacticPlanStore;
        _clubSquadStore = clubSquadStore;
        _startingOpportunityPromises = startingOpportunityPromises;
        _selectionMemory = selectionMemory;
        _playingTimePromises = playingTimePromises;
        _promiseInvalidation = promiseInvalidation;
        _careerMemory = careerMemory;
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

        RecoverInjuriesToDate(occurredAt);

        var homeBonus = ResolveLineupBonus(fixture.Id, fixture.HomeClubId, rootSeed)
            + ResolvePhysicalModifier(fixture.Id, fixture.HomeClubId, occurredAt)
            + ResolveTacticModifier(fixture.HomeClubId);
        var awayBonus = ResolveLineupBonus(fixture.Id, fixture.AwayClubId, rootSeed)
            + ResolvePhysicalModifier(fixture.Id, fixture.AwayClubId, occurredAt)
            + ResolveTacticModifier(fixture.AwayClubId);

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

        ApplyMatchPhysicalConsequences(fixture, occurredAt, rootSeed);
        ApplyMatchDevelopment(fixture, occurredAt, rootSeed);
        ApplySocialContinuityAfterMatch(fixture, occurredAt);
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
            var clubId = career.ActiveEmployment?.ClubId;
            var managerId = career.ManagerId;
            var dismissal = career.DismissDueToBoardConfidence(fixture.Id, occurredAt);
            career = dismissal.Career;
            if (clubId is ClubId dismissedClub)
            {
                _promiseInvalidation?.InvalidateForManagerLeavingClub(
                    managerId,
                    dismissedClub,
                    occurredAt);
                if (dismissal.WasApplied)
                {
                    _careerMemory?.RecordDismissal(
                        managerId,
                        dismissedClub,
                        fixture.Id,
                        occurredAt);
                }
            }
        }

        _managerCareerStore.Replace(career);
    }

    private int ResolveLineupBonus(FixtureId fixtureId, ClubId clubId, int rootSeed)
    {
        _playerDevelopment?.EnsureClub(clubId, rootSeed, _timelineStore.Timeline.CurrentDate);
        var abilities = BuildAbilityMap(clubId);

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
                selection.StartingSlotIndices,
                abilities);
        }

        return MvpSquadStrengthCalculator.ComputeDefaultLineupBonus(clubId, rootSeed, abilities);
    }

    private IReadOnlyDictionary<(long ClubId, int SlotIndex), int>? BuildAbilityMap(ClubId clubId)
    {
        if (_playerCareerStore is null)
        {
            return null;
        }

        var map = _playerCareerStore.Careers
            .Where(career => career.OriginClubId == clubId)
            .ToDictionary(
                career => (career.OriginClubId.Value, career.SlotIndex),
                career => career.CurrentAbility);
        return map.Count == 0 ? null : map;
    }

    private void ApplyMatchDevelopment(Fixture fixture, GameDate day, int rootSeed)
    {
        if (_playerDevelopment is null)
        {
            return;
        }

        ApplyMatchDevelopmentForClub(fixture.HomeClubId, fixture.Id, day, rootSeed);
        ApplyMatchDevelopmentForClub(fixture.AwayClubId, fixture.Id, day, rootSeed);
    }

    private void ApplyMatchDevelopmentForClub(ClubId clubId, FixtureId fixtureId, GameDate day, int rootSeed)
    {
        IReadOnlyList<int> startingSlots;
        var selection = _matchSelectionStore?.Get(fixtureId, clubId);
        startingSlots = selection?.StartingSlotIndices
            ?? Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray();

        _playerDevelopment!.EnsureAndApplyMatchAppearances(clubId, startingSlots, rootSeed, day);
    }

    private int ResolvePhysicalModifier(FixtureId fixtureId, ClubId clubId, GameDate day)
    {
        if (_trainingStore is null
            || !_trainingStore.PhysicalStates.Any(state => state.ClubId == clubId))
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
            _trainingStore.PhysicalBySlot,
            day);
    }

    private int ResolveTacticModifier(ClubId clubId) =>
        MvpTacticMatchModifier.ComputeApproachModifier(_tacticPlanStore?.Get(clubId));

    private void RecoverInjuriesToDate(GameDate day)
    {
        if (_trainingStore is null)
        {
            return;
        }

        foreach (var group in _trainingStore.PhysicalStates.GroupBy(state => state.ClubId))
        {
            var recovered = group.Select(state => state.RecoverIfDue(day)).ToArray();
            _trainingStore.ReplacePhysicalStatesForClub(group.Key, recovered);
        }
    }

    private void ApplyMatchPhysicalConsequences(Fixture fixture, GameDate day, int rootSeed)
    {
        if (_trainingStore is null)
        {
            return;
        }

        ApplyMatchLoadForClub(fixture.HomeClubId, fixture.Id, day, rootSeed);
        ApplyMatchLoadForClub(fixture.AwayClubId, fixture.Id, day, rootSeed);
    }

    private void ApplyMatchLoadForClub(ClubId clubId, FixtureId fixtureId, GameDate day, int rootSeed)
    {
        var existing = _trainingStore!.PhysicalStates
            .Where(state => state.ClubId == clubId)
            .Select(state => state.RecoverIfDue(day))
            .ToDictionary(state => state.SlotIndex);

        if (existing.Count == 0)
        {
            return;
        }

        IReadOnlyList<int> startingSlots;
        var selection = _matchSelectionStore?.Get(fixtureId, clubId);
        startingSlots = selection?.StartingSlotIndices
            ?? Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray();

        foreach (var slot in startingSlots)
        {
            if (!existing.TryGetValue(slot, out var state) || !state.IsAvailableOn(day))
            {
                continue;
            }

            existing[slot] = MvpInjuryRiskEvaluator.MaybeInjureFromMatch(
                state,
                rootSeed,
                fixtureId.Value,
                day);
        }

        _trainingStore.ReplacePhysicalStatesForClub(
            clubId,
            existing.Values.OrderBy(state => state.SlotIndex));
    }

    private void ApplySocialContinuityAfterMatch(Fixture fixture, GameDate day)
    {
        if (_startingOpportunityPromises is null
            && _playingTimePromises is null
            && _selectionMemory is null)
        {
            return;
        }

        ApplySocialContinuityForClub(fixture.Id, fixture.HomeClubId, day);
        ApplySocialContinuityForClub(fixture.Id, fixture.AwayClubId, day);
    }

    private void ApplySocialContinuityForClub(FixtureId fixtureId, ClubId clubId, GameDate day)
    {
        var startingSlots = ResolveStartingSlots(fixtureId, clubId);
        var matchdaySlots = ResolveMatchdaySlots(fixtureId, clubId);
        var benchSlots = matchdaySlots.Except(startingSlots).ToArray();

        var startingIds = ResolvePlayerIdsForSlots(fixtureId, clubId, startingSlots);
        var benchedIds = ResolvePlayerIdsForSlots(fixtureId, clubId, benchSlots);
        var participantIds = startingIds.Concat(benchedIds).Distinct().ToArray();
        var squadMemberIds = _clubSquadStore?.Get(clubId)?.Members
            .Select(m => m.PlayerId)
            .ToArray();

        _startingOpportunityPromises?.RecordStartsForPlayers(fixtureId, clubId, startingIds, day);
        _playingTimePromises?.RecordAppearancesForPlayers(fixtureId, clubId, participantIds, day);
        _selectionMemory?.RecordMatchday(
            fixtureId,
            startingIds,
            benchedIds,
            squadMemberIds,
            day);
    }

    private IReadOnlyList<int> ResolveStartingSlots(FixtureId fixtureId, ClubId clubId)
    {
        var selection = _matchSelectionStore?.Get(fixtureId, clubId);
        return selection?.StartingSlotIndices
            ?? Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray();
    }

    private IReadOnlyList<int> ResolveMatchdaySlots(FixtureId fixtureId, ClubId clubId)
    {
        var selection = _matchSelectionStore?.Get(fixtureId, clubId);
        if (selection is null)
        {
            return Enumerable.Range(0, MatchSelection.StartingXiSize + MatchSelection.MaxBenchSize)
                .ToArray();
        }

        return selection.StartingSlotIndices.Concat(selection.BenchSlotIndices).ToArray();
    }

    private IReadOnlyList<PlayerId> ResolvePlayerIdsForSlots(
        FixtureId fixtureId,
        ClubId clubId,
        IReadOnlyList<int> slots)
    {
        _ = fixtureId;
        var squad = _clubSquadStore?.Get(clubId);
        return slots
            .Select(slot =>
            {
                var member = squad?.Members.FirstOrDefault(m => m.SlotIndex == slot);
                return member?.PlayerId ?? PlayerId.FromClubSlot(clubId.Value, slot);
            })
            .ToArray();
    }
}
