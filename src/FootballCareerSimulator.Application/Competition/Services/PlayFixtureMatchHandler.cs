using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.Match;
using FootballCareerSimulator.Simulation.TeamPreparation;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.Competition.Services;

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
    private readonly IDualPhaseTacticPlanStore? _dualPhaseTacticPlanStore;
    private readonly IClubSquadStore? _clubSquadStore;
    private readonly StartingOpportunityPromiseService? _startingOpportunityPromises;
    private readonly PlayingTimePromiseService? _playingTimePromises;
    private readonly SelectionMemoryService? _selectionMemory;
    private readonly PromiseInvalidationService? _promiseInvalidation;
    private readonly CareerMemoryService? _careerMemory;
    private readonly ClubHistoryMemoryService? _clubHistoryMemory;
    private readonly MatchPerformanceMemoryService? _matchPerformanceMemory;
    private readonly RelationshipEvaluationService? _relationships;
    private readonly PostMatchPressDecisionTrigger? _postMatchPress;
    private readonly PostMatchPlayingTimeDemandTrigger? _postMatchPlayingTimeDemand;
    private readonly PostMatchBoardDemandTrigger? _postMatchBoardDemand;
    private readonly PostMatchDisciplineDecisionTrigger? _postMatchDiscipline;
    private readonly MatchSelectionAvailabilityRevalidationService? _selectionRevalidation;
    private readonly Dictionary<Guid, PlayFixtureMatchResult> _completedCommands = new();
    private readonly Dictionary<(long FixtureId, long ClubId), IReadOnlyList<int>> _resolvedAiStartingSlots = new();

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
        CareerMemoryService? careerMemory = null,
        ClubHistoryMemoryService? clubHistoryMemory = null,
        MatchPerformanceMemoryService? matchPerformanceMemory = null,
        RelationshipEvaluationService? relationships = null,
        PostMatchPressDecisionTrigger? postMatchPress = null,
        MatchSelectionAvailabilityRevalidationService? selectionRevalidation = null,
        PostMatchPlayingTimeDemandTrigger? postMatchPlayingTimeDemand = null,
        PostMatchBoardDemandTrigger? postMatchBoardDemand = null,
        PostMatchDisciplineDecisionTrigger? postMatchDiscipline = null,
        IDualPhaseTacticPlanStore? dualPhaseTacticPlanStore = null)
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
        _dualPhaseTacticPlanStore = dualPhaseTacticPlanStore;
        _clubSquadStore = clubSquadStore;
        _startingOpportunityPromises = startingOpportunityPromises;
        _selectionMemory = selectionMemory;
        _playingTimePromises = playingTimePromises;
        _promiseInvalidation = promiseInvalidation;
        _careerMemory = careerMemory;
        _clubHistoryMemory = clubHistoryMemory;
        _matchPerformanceMemory = matchPerformanceMemory;
        _relationships = relationships;
        _postMatchPress = postMatchPress;
        _postMatchPlayingTimeDemand = postMatchPlayingTimeDemand;
        _postMatchBoardDemand = postMatchBoardDemand;
        _postMatchDiscipline = postMatchDiscipline;
        _selectionRevalidation = selectionRevalidation;
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

        var homeTactic = ResolveTacticModifier(fixture.HomeClubId);
        var awayTactic = ResolveTacticModifier(fixture.AwayClubId);
        var managedClubBefore = _managerCareerStore?.Career.ActiveEmployment?.ClubId;
        var isManagedMatch = managedClubBefore is ClubId managedPre
            && (fixture.HomeClubId == managedPre || fixture.AwayClubId == managedPre);
        // Adaptif plan yalnızca insan menajere karşı görünür ve anlamlıdır.
        // AI-AI maçlarını klasik hızlı yolda tutmak, uzun sezon simülasyonunu
        // binlerce gereksiz kadro/fitness taramasından korur.
        var homeAiPlan = isManagedMatch && managedClubBefore != fixture.HomeClubId
            ? ResolveOpponentMatchPlan(season, fixture, fixture.HomeClubId, homeClub.SportiveStrength, awayClub.SportiveStrength, occurredAt)
            : null;
        var awayAiPlan = isManagedMatch && managedClubBefore != fixture.AwayClubId
            ? ResolveOpponentMatchPlan(season, fixture, fixture.AwayClubId, awayClub.SportiveStrength, homeClub.SportiveStrength, occurredAt)
            : null;
        var homeLineupRole = ResolveLineupRoleModifier(fixture.Id, fixture.HomeClubId, rootSeed);
        var awayLineupRole = ResolveLineupRoleModifier(fixture.Id, fixture.AwayClubId, rootSeed);
        var managedTacticModifier = isManagedMatch && managedClubBefore is ClubId managedClub
            ? fixture.HomeClubId == managedClub ? homeTactic : awayTactic
            : (int?)null;
        var managedLineupRoleModifier = isManagedMatch && managedClubBefore is ClubId managedRoleClub
            ? fixture.HomeClubId == managedRoleClub ? homeLineupRole : awayLineupRole
            : (int?)null;

        var homeBonus = ResolveLineupBonus(fixture.Id, fixture.HomeClubId, rootSeed, occurredAt)
            + ResolvePhysicalModifier(fixture.Id, fixture.HomeClubId, occurredAt)
            + homeTactic
            + homeLineupRole
            + (homeAiPlan?.MatchStrengthModifier ?? 0);
        var awayBonus = ResolveLineupBonus(fixture.Id, fixture.AwayClubId, rootSeed, occurredAt)
            + ResolvePhysicalModifier(fixture.Id, fixture.AwayClubId, occurredAt)
            + awayTactic
            + awayLineupRole
            + (awayAiPlan?.MatchStrengthModifier ?? 0);

        var managedPreparationModifier = isManagedMatch
            ? Math.Clamp(command.ManagedPreparationModifier, -4, 4)
            : 0;
        if (managedPreparationModifier != 0 && managedClubBefore is ClubId preparedClub)
        {
            if (fixture.HomeClubId == preparedClub)
            {
                homeBonus += managedPreparationModifier;
            }
            else
            {
                awayBonus += managedPreparationModifier;
            }
        }

        var homeSecondHalfDelta = 0;
        var awaySecondHalfDelta = 0;
        if (isManagedMatch
            && managedClubBefore is ClubId managedForDelta
            && command.ManagedSecondHalfDelta != 0)
        {
            if (fixture.HomeClubId == managedForDelta)
            {
                homeSecondHalfDelta = command.ManagedSecondHalfDelta;
            }
            else
            {
                awaySecondHalfDelta = command.ManagedSecondHalfDelta;
            }
        }

        MatchScore? forcedHalfTime = null;
        if (command.ForcedHalfTimeHomeGoals is int forcedHome
            && command.ForcedHalfTimeAwayGoals is int forcedAway)
        {
            forcedHalfTime = new MatchScore(forcedHome, forcedAway);
        }

        var simulation = MvpFixtureMatchSimulator.SimulateWithKeyMoments(
            rootSeed,
            command.FixtureId,
            homeClub.SportiveStrength,
            awayClub.SportiveStrength,
            homeBonus,
            awayBonus,
            homeSecondHalfDelta,
            awaySecondHalfDelta,
            forcedHalfTime);
        var score = simulation.Score;

        _competitionStore.League.AcceptFixtureResult(
            new SeasonId(command.SeasonId),
            new FixtureId(command.FixtureId),
            score,
            occurredAt);

        var injuredBefore = isManagedMatch && managedClubBefore is ClubId clubForInjury
            ? SnapshotInjuredSlots(clubForInjury)
            : Array.Empty<int>();

        ApplyMatchPhysicalConsequences(
            fixture,
            occurredAt,
            rootSeed,
            managedClubBefore,
            command.ManagedSecondHalfDelta);
        var newlyInjured = isManagedMatch && managedClubBefore is ClubId clubAfterInjury
            ? DiffNewlyInjuredSlots(clubAfterInjury, injuredBefore)
            : Array.Empty<int>();

        ApplyMatchDevelopment(fixture, occurredAt, rootSeed);
        ApplySocialContinuityAfterMatch(fixture, occurredAt);
        ApplyMatchPerformanceMemory(fixture, score, occurredAt);
        var pressOpened = TryOpenPressQuestionAfterBlowoutLoss(fixture, score, occurredAt);
        var playingTimeDemand = TryOpenPlayingTimeDemandAfterSittingOut(fixture, occurredAt);
        var disciplineDemand = TryOpenDisciplineAfterManagedRedCard(
            fixture,
            simulation.KeyMoments,
            occurredAt);
        ApplyRelationshipSelectionEffects(fixture, occurredAt);
        var reencounter = ApplyFormerEncounters(fixture, managedClubBefore, occurredAt);
        _matchSelectionStore?.RemoveForFixture(fixture.Id);

        var invalidated = 0;
        if (_selectionRevalidation is not null)
        {
            invalidated += _selectionRevalidation.InvalidateUnavailableForClub(
                fixture.HomeClubId,
                occurredAt);
            invalidated += _selectionRevalidation.InvalidateUnavailableForClub(
                fixture.AwayClubId,
                occurredAt);
        }

        var updatedSeason = CompetitionSeasonCommandSupport.GetSeasonOrThrow(
            _competitionStore,
            command.SeasonId);
        var board = TryApplyBoardAssessment(fixture, score, updatedSeason, occurredAt);
        updatedSeason.ClearUncommittedEvents();

        ManagedMatchConsequenceSummary? consequences = null;
        if (isManagedMatch)
        {
            consequences = new ManagedMatchConsequenceSummary(
                IsManagedMatch: true,
                managedTacticModifier,
                board?.ConfidenceDelta,
                board?.BoardConfidence,
                board?.RiskBand,
                board?.ReasonCode,
                board?.ManagerDismissed ?? false,
                newlyInjured,
                pressOpened,
                playingTimeDemand is not null,
                playingTimeDemand?.SubjectPlayerId.Value,
                board?.BoardDemandOpened ?? false,
                disciplineDemand is not null,
                disciplineDemand?.SubjectPlayerId.Value,
                reencounter.FormerClubEncounter,
                reencounter.FormerPlayerCount);
        }

        var keyMoments = simulation.KeyMoments
            .Select(moment => MapMomentToReadModel(moment, fixture.Id, fixture.HomeClubId, fixture.AwayClubId, rootSeed))
            .ToList();

        if (newlyInjured.Length > 0 && managedClubBefore is ClubId injuredClub)
        {
            var injuredStarting = ResolveStartingSlots(fixture.Id, injuredClub);
            var injuredNames = MvpSquadRosterGenerator.GeneratePlayerNames(injuredClub, rootSeed);
            AppendInjuryMoments(
                keyMoments,
                newlyInjured,
                fixture.HomeClubId == injuredClub,
                injuredStarting,
                injuredNames,
                rootSeed,
                command.FixtureId);
        }

        var keyMomentArray = keyMoments
            .OrderBy(moment => moment.Minute)
            .ThenBy(moment => moment.Kind, StringComparer.Ordinal)
            .ThenBy(moment => moment.IsHomeSide ? 0 : 1)
            .ThenBy(moment => moment.PrimarySlotIndex)
            .ToArray();
        var statistics = new MatchStatisticsReadModel(
            simulation.Statistics.HomePossessionPercent,
            simulation.Statistics.AwayPossessionPercent,
            simulation.Statistics.HomeShots,
            simulation.Statistics.AwayShots,
            simulation.Statistics.HomeShotsOnTarget,
            simulation.Statistics.AwayShotsOnTarget,
            simulation.Statistics.HomeCorners,
            simulation.Statistics.AwayCorners);

        var result = new PlayFixtureMatchResult(
            true,
            command.SeasonId,
            command.FixtureId,
            score.HomeGoals,
            score.AwayGoals,
            nameof(FixtureStatus.ResultAccepted),
            invalidated,
            managedTacticModifier,
            consequences,
            keyMomentArray,
            statistics,
            managedLineupRoleModifier,
            isManagedMatch ? managedPreparationModifier : null,
            MapOpponentPlan(isManagedMatch
                ? managedClubBefore is ClubId managedForPlan && fixture.HomeClubId == managedForPlan
                    ? awayAiPlan
                    : homeAiPlan
                : null));

        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache()
    {
        _completedCommands.Clear();
        _resolvedAiStartingSlots.Clear();
    }

    private (bool FormerClubEncounter, int FormerPlayerCount) ApplyFormerEncounters(
        Fixture fixture,
        ClubId? managedClubId,
        GameDate day)
    {
        if (managedClubId is not ClubId currentClub
            || _managerCareerStore is null
            || (fixture.HomeClubId != currentClub && fixture.AwayClubId != currentClub))
        {
            return (false, 0);
        }

        var career = _managerCareerStore.Career;
        var opponentClub = fixture.HomeClubId == currentClub
            ? fixture.AwayClubId
            : fixture.HomeClubId;
        var formerClubEncounter = career.EmploymentHistory.Any(entry => entry.ClubId == opponentClub);
        if (formerClubEncounter)
        {
            _clubHistoryMemory?.RecordFormerClubEncounter(
                career.ManagerId,
                opponentClub,
                fixture.Id,
                day);
        }

        var opponentPlayers = (_clubSquadStore?.Get(opponentClub)?.Members
                ?? Array.Empty<SquadMember>())
            .Select(member => member.PlayerId)
            .ToArray();
        var formerPlayers = _relationships?.ReactivateForFormerPlayerEncounter(
                career.ManagerId,
                opponentPlayers,
                day)
            ?? Array.Empty<PlayerId>();
        foreach (var playerId in formerPlayers)
        {
            _clubHistoryMemory?.RecordFormerPlayerEncounter(
                career.ManagerId,
                playerId,
                fixture.Id,
                day);
        }

        return (formerClubEncounter, formerPlayers.Count);
    }

    private sealed record BoardConsequenceSummary(
        int? ConfidenceDelta,
        int? BoardConfidence,
        string? RiskBand,
        string? ReasonCode,
        bool ManagerDismissed,
        bool BoardDemandOpened);

    private BoardConsequenceSummary? TryApplyBoardAssessment(
        Fixture fixture,
        MatchScore score,
        CompetitionSeason season,
        GameDate occurredAt)
    {
        if (_managerCareerStore is null)
        {
            return null;
        }

        var employment = _managerCareerStore.Career.ActiveEmployment;
        if (employment is null)
        {
            return null;
        }

        var managedClubId = employment.ClubId;
        if (fixture.HomeClubId != managedClubId && fixture.AwayClubId != managedClubId)
        {
            return null;
        }

        var previousRisk = employment.RiskBand;
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
        var managerDismissed = false;
        if (assessment.WasApplied && assessment.RiskBand == EmploymentRiskBand.Critical)
        {
            var clubId = career.ActiveEmployment?.ClubId;
            var managerId = career.ManagerId;
            var dismissal = career.DismissDueToBoardConfidence(fixture.Id, occurredAt);
            career = dismissal.Career;
            managerDismissed = dismissal.WasApplied;
            if (clubId is ClubId dismissedClub)
            {
                _promiseInvalidation?.InvalidateForManagerLeavingClub(
                    managerId,
                    dismissedClub,
                    occurredAt);
                if (dismissal.WasApplied)
                {
                    _relationships?.MarkDormantForManagerLeaving(managerId, occurredAt);
                    _careerMemory?.RecordDismissal(
                        managerId,
                        dismissedClub,
                        fixture.Id,
                        occurredAt);
                    _clubHistoryMemory?.RecordManagerLeftDismissed(
                        managerId,
                        dismissedClub,
                        fixture.Id,
                        occurredAt);
                }
            }
        }

        _managerCareerStore.Replace(career);

        var boardDemandOpened = false;
        if (!managerDismissed
            && assessment.WasApplied
            && _postMatchBoardDemand is not null)
        {
            boardDemandOpened = _postMatchBoardDemand.TryOpenAfterRiskEscalation(
                previousRisk,
                assessment.RiskBand,
                occurredAt) is not null;
        }

        return new BoardConsequenceSummary(
            assessment.WasApplied || assessment.WasAlreadyApplied
                ? assessment.ConfidenceDelta
                : null,
            assessment.WasApplied || assessment.WasAlreadyApplied
                ? assessment.BoardConfidence
                : null,
            (assessment.WasApplied || assessment.WasAlreadyApplied)
                ? assessment.RiskBand.ToString()
                : null,
            assessment.ReasonCode,
            managerDismissed,
            boardDemandOpened);
    }

    private int ResolveLineupBonus(FixtureId fixtureId, ClubId clubId, int rootSeed, GameDate day)
    {
        _playerDevelopment?.EnsureClub(clubId, rootSeed, day);
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

            if (_trainingStore is not null)
            {
                MvpAvailabilityAwareSelection.EnsureStartingXiAvailable(
                    clubId,
                    selection.StartingSlotIndices,
                    day,
                    _trainingStore.PhysicalBySlot,
                    _clubSquadStore?.Get(clubId));
            }

            return MvpSquadStrengthCalculator.ComputeLineupBonus(
                clubId,
                rootSeed,
                selection.StartingSlotIndices,
                abilities);
        }

        return MvpSquadStrengthCalculator.ComputeLineupBonus(
            clubId,
            rootSeed,
            ResolveStartingSlots(fixtureId, clubId),
            abilities);
    }

    private IReadOnlyDictionary<(long ClubId, int SlotIndex), int>? BuildAbilityMap(ClubId clubId)
    {
        if (_playerCareerStore is null)
        {
            return null;
        }

        var activeCareers = _playerCareerStore.Careers
            .Where(career => !career.IsRetired)
            .ToArray();
        var squad = _clubSquadStore?.Get(clubId);
        if (squad is not null)
        {
            var careerByPlayer = activeCareers.ToDictionary(career => career.Id);
            var squadMap = squad.Members
                .Where(member => careerByPlayer.ContainsKey(member.PlayerId))
                .ToDictionary(
                    member => (clubId.Value, member.SlotIndex),
                    member => careerByPlayer[member.PlayerId].CurrentAbility);
            return squadMap.Count == 0 ? null : squadMap;
        }

        var map = activeCareers
            .Where(career => career.OriginClubId == clubId)
            .GroupBy(career => career.SlotIndex)
            .ToDictionary(
                group => (clubId.Value, group.Key),
                group => group.OrderByDescending(career => career.Generation).First().CurrentAbility);
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

        var startingSlots = ResolveStartingSlots(fixtureId, clubId);

        return MvpPhysicalMatchModifier.ComputeLineupModifier(
            clubId,
            startingSlots,
            _trainingStore.PhysicalBySlot,
            day);
    }

    private int ResolveTacticModifier(ClubId clubId)
    {
        var legacyPlan = _tacticPlanStore?.Get(clubId);
        return Math.Clamp(
            MvpTacticMatchModifier.ComputeTacticModifier(legacyPlan)
            + DualPhaseTacticMatchModifier.Compute(
                legacyPlan,
                _dualPhaseTacticPlanStore?.Get(clubId)),
            -3,
            6);
    }

    private int ResolveLineupRoleModifier(FixtureId fixtureId, ClubId clubId, int rootSeed)
    {
        var startingSlots = ResolveStartingSlots(fixtureId, clubId);
        var profiles = MvpSquadRosterGenerator.GeneratePlayerProfiles(clubId, rootSeed);
        var selected = startingSlots
            .Where(slot => slot >= 0 && slot < profiles.Count)
            .Select(slot => profiles[slot])
            .ToArray();
        var formation = _dualPhaseTacticPlanStore?.Get(clubId)?.InPossessionFormation
            ?? _tacticPlanStore?.Get(clubId)?.Formation
            ?? Formation.F442;
        return MvpLineupRoleFitCalculator.Evaluate(formation, selected).MatchStrengthModifier;
    }

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

    private void ApplyMatchPhysicalConsequences(
        Fixture fixture,
        GameDate day,
        int rootSeed,
        ClubId? managedClubId,
        int managedSecondHalfDelta)
    {
        if (_trainingStore is null)
        {
            return;
        }

        var homeRiskBonus = managedClubId == fixture.HomeClubId
            ? ResolveHalfTimeInjuryRiskBonus(managedSecondHalfDelta)
            : 0;
        var awayRiskBonus = managedClubId == fixture.AwayClubId
            ? ResolveHalfTimeInjuryRiskBonus(managedSecondHalfDelta)
            : 0;

        ApplyMatchLoadForClub(fixture.HomeClubId, fixture.Id, day, rootSeed, homeRiskBonus);
        ApplyMatchLoadForClub(fixture.AwayClubId, fixture.Id, day, rootSeed, awayRiskBonus);
    }

    private static int ResolveHalfTimeInjuryRiskBonus(int managedSecondHalfDelta) =>
        managedSecondHalfDelta >= 2 ? 8 : 0;

    private void ApplyMatchLoadForClub(
        ClubId clubId,
        FixtureId fixtureId,
        GameDate day,
        int rootSeed,
        int riskBonusPercent)
    {
        var existing = _trainingStore!.PhysicalStates
            .Where(state => state.ClubId == clubId)
            .Select(state => state.RecoverIfDue(day))
            .ToDictionary(state => state.SlotIndex);

        if (existing.Count == 0)
        {
            for (var slot = MatchSelection.MinSquadSlot; slot <= MatchSelection.MaxSquadSlot; slot++)
            {
                existing[slot] = PlayerPhysicalState.CreateRested(clubId, slot);
            }
        }

        var startingSlots = ResolveStartingSlots(fixtureId, clubId);
        var matchdaySlots = ResolveMatchdaySlots(fixtureId, clubId).ToHashSet();
        var startingSet = startingSlots.ToHashSet();

        foreach (var slot in existing.Keys.ToArray())
        {
            if (!existing.TryGetValue(slot, out var state) || !state.IsAvailableOn(day))
            {
                continue;
            }

            if (startingSet.Contains(slot))
            {
                existing[slot] = MvpInjuryRiskEvaluator.MaybeInjureFromMatch(
                    state,
                    rootSeed,
                    fixtureId.Value,
                    day,
                    minutesPlayed: 90,
                    riskBonusPercent);
            }
            else if (matchdaySlots.Contains(slot))
            {
                existing[slot] = state.WithLevels(
                    Math.Clamp(
                        state.Fatigue + MvpInjuryRiskEvaluator.MatchFatigueGain(20),
                        PlayerPhysicalState.MinLevel,
                        PlayerPhysicalState.MaxLevel),
                    Math.Clamp(
                        state.Fitness - MvpInjuryRiskEvaluator.MatchFitnessLoss(20),
                        PlayerPhysicalState.MinLevel,
                        PlayerPhysicalState.MaxLevel));
            }
        }

        _trainingStore.ReplacePhysicalStatesForClub(
            clubId,
            existing.Values.OrderBy(state => state.SlotIndex));
    }

    private static void AppendInjuryMoments(
        List<MatchKeyMomentReadModel> moments,
        IReadOnlyList<int> newlyInjuredClubSlots,
        bool managedIsHome,
        IReadOnlyList<int> startingSlots,
        IReadOnlyList<string> names,
        int rootSeed,
        long fixtureId)
    {
        var usedMinutes = moments.Select(moment => moment.Minute).ToHashSet();
        foreach (var clubSlot in newlyInjuredClubSlots.OrderBy(slot => slot))
        {
            var xiIndex = -1;
            for (var i = 0; i < startingSlots.Count; i++)
            {
                if (startingSlots[i] == clubSlot)
                {
                    xiIndex = i;
                    break;
                }
            }

            if (xiIndex < 0)
            {
                xiIndex = Math.Clamp(clubSlot, 0, MatchSelection.StartingXiSize - 1);
            }

            var playerName = clubSlot >= 0 && clubSlot < names.Count
                ? names[clubSlot]
                : ResolveXiPlayerName(startingSlots, names, xiIndex);
            var minute = NextInjuryMinute(rootSeed, fixtureId, clubSlot, usedMinutes);
            moments.Add(
                new MatchKeyMomentReadModel(
                    nameof(MatchKeyMomentKind.Injury),
                    minute,
                    managedIsHome,
                    xiIndex,
                    AssistSlotIndex: null,
                    playerName,
                    AssistPlayerName: null));
        }
    }

    private static int NextInjuryMinute(
        int rootSeed,
        long fixtureId,
        int clubSlot,
        HashSet<int> usedMinutes)
    {
        var rng = new FootballCareerSimulator.Simulation.SimulationRandomContext(
            unchecked(rootSeed * 613) ^ ((int)fixtureId * 17) ^ (clubSlot * 41));
        for (var attempt = 0; attempt < 24; attempt++)
        {
            // Maç içi his: çoğu sakatlık ikinci yarıda.
            var minute = rng.NextInt(38, 90);
            if (usedMinutes.Add(minute))
            {
                return minute;
            }
        }

        for (var minute = 38; minute <= 90; minute++)
        {
            if (usedMinutes.Add(minute))
            {
                return minute;
            }
        }

        return 90;
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

    private void ApplyMatchPerformanceMemory(Fixture fixture, MatchScore score, GameDate day)
    {
        if (_matchPerformanceMemory is null || _managerCareerStore is null)
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
        var startingIds = ResolvePlayerIdsForSlots(
            fixture.Id,
            managedClubId,
            ResolveStartingSlots(fixture.Id, managedClubId));

        _matchPerformanceMemory.RecordBlowoutIfApplicable(
            fixture.Id,
            managedGoals,
            opponentGoals,
            _managerCareerStore.Career.ManagerId,
            startingIds,
            day);
    }

    private bool TryOpenPressQuestionAfterBlowoutLoss(Fixture fixture, MatchScore score, GameDate day)
    {
        if (_postMatchPress is null || _managerCareerStore is null)
        {
            return false;
        }

        var employment = _managerCareerStore.Career.ActiveEmployment;
        if (employment is null)
        {
            return false;
        }

        var managedClubId = employment.ClubId;
        if (fixture.HomeClubId != managedClubId && fixture.AwayClubId != managedClubId)
        {
            return false;
        }

        var isHome = fixture.HomeClubId == managedClubId;
        var managedGoals = isHome ? score.HomeGoals : score.AwayGoals;
        var opponentGoals = isHome ? score.AwayGoals : score.HomeGoals;
        var startingIds = ResolvePlayerIdsForSlots(
            fixture.Id,
            managedClubId,
            ResolveStartingSlots(fixture.Id, managedClubId));

        return _postMatchPress.TryOpenAfterManagedBlowoutLoss(
            managedGoals,
            opponentGoals,
            startingIds,
            day) is not null;
    }

    private Domain.Interaction.DecisionRequest? TryOpenPlayingTimeDemandAfterSittingOut(
        Fixture fixture,
        GameDate day)
    {
        if (_postMatchPlayingTimeDemand is null || _managerCareerStore is null)
        {
            return null;
        }

        var employment = _managerCareerStore.Career.ActiveEmployment;
        if (employment is null)
        {
            return null;
        }

        var managedClubId = employment.ClubId;
        if (fixture.HomeClubId != managedClubId && fixture.AwayClubId != managedClubId)
        {
            return null;
        }

        var startingSlots = ResolveStartingSlots(fixture.Id, managedClubId);
        var matchdaySlots = ResolveMatchdaySlots(fixture.Id, managedClubId);
        var benchSlots = matchdaySlots.Except(startingSlots).ToArray();
        var benchedIds = ResolvePlayerIdsForSlots(fixture.Id, managedClubId, benchSlots);
        var matchdayIds = ResolvePlayerIdsForSlots(fixture.Id, managedClubId, matchdaySlots)
            .ToHashSet();
        var omittedIds = _clubSquadStore?.Get(managedClubId)?.Members
            .Select(m => m.PlayerId)
            .Where(id => !matchdayIds.Contains(id))
            .ToArray()
            ?? Array.Empty<PlayerId>();

        return _postMatchPlayingTimeDemand.TryOpenAfterManagedSittingOut(
            benchedIds.Concat(omittedIds).ToArray(),
            day);
    }

    private Domain.Interaction.DecisionRequest? TryOpenDisciplineAfterManagedRedCard(
        Fixture fixture,
        IReadOnlyList<MatchKeyMoment> keyMoments,
        GameDate day)
    {
        if (_postMatchDiscipline is null || _managerCareerStore is null)
        {
            return null;
        }

        var employment = _managerCareerStore.Career.ActiveEmployment;
        if (employment is null)
        {
            return null;
        }

        var managedClubId = employment.ClubId;
        if (fixture.HomeClubId != managedClubId && fixture.AwayClubId != managedClubId)
        {
            return null;
        }

        var managedIsHome = fixture.HomeClubId == managedClubId;
        var redSlots = keyMoments
            .Where(moment =>
                moment.Kind == MatchKeyMomentKind.RedCard
                && moment.IsHomeSide == managedIsHome)
            .Select(moment => moment.PrimarySlotIndex)
            .Distinct()
            .ToArray();
        if (redSlots.Length == 0)
        {
            return null;
        }

        var sentOffIds = ResolvePlayerIdsForSlots(fixture.Id, managedClubId, redSlots);
        return _postMatchDiscipline.TryOpenAfterManagedRedCards(sentOffIds, day);
    }

    private int[] SnapshotInjuredSlots(ClubId clubId)
    {
        if (_trainingStore is null)
        {
            return [];
        }

        return _trainingStore.PhysicalStates
            .Where(state => state.ClubId == clubId && state.IsInjured)
            .Select(state => state.SlotIndex)
            .OrderBy(slot => slot)
            .ToArray();
    }

    private int[] DiffNewlyInjuredSlots(ClubId clubId, IReadOnlyList<int> injuredBefore)
    {
        var before = injuredBefore.ToHashSet();
        return SnapshotInjuredSlots(clubId)
            .Where(slot => !before.Contains(slot))
            .ToArray();
    }

    private void ApplyRelationshipSelectionEffects(Fixture fixture, GameDate day)
    {
        if (_relationships is null || _managerCareerStore is null)
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

        var managerId = _managerCareerStore.Career.ManagerId;
        var startingIds = ResolvePlayerIdsForSlots(
            fixture.Id,
            managedClubId,
            ResolveStartingSlots(fixture.Id, managedClubId));
        var matchdayIds = ResolvePlayerIdsForSlots(
            fixture.Id,
            managedClubId,
            ResolveMatchdaySlots(fixture.Id, managedClubId));
        var matchdaySet = matchdayIds.ToHashSet();
        var squadMemberIds = _clubSquadStore?.Get(managedClubId)?.Members
            .Select(m => m.PlayerId)
            .ToArray()
            ?? Array.Empty<PlayerId>();

        foreach (var playerId in startingIds)
        {
            _relationships.ApplySelectionStarted(fixture.Id, playerId, managerId, day);
        }

        foreach (var playerId in squadMemberIds.Where(id => !matchdaySet.Contains(id)))
        {
            _relationships.ApplySelectionOmitted(fixture.Id, playerId, managerId, day);
        }
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
            ?? _resolvedAiStartingSlots.GetValueOrDefault((fixtureId.Value, clubId.Value))
            ?? Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray();
    }

    private OpponentMatchPlan ResolveOpponentMatchPlan(
        CompetitionSeason season,
        Fixture fixture,
        ClubId clubId,
        int clubStrength,
        int opponentStrength,
        GameDate day)
    {
        var standings = season.Standings.Entries;
        var positionIndex = Array.FindIndex(
            standings.ToArray(),
            entry => entry.ClubId == clubId);
        var position = positionIndex >= 0
            ? positionIndex + 1
            : Math.Max(1, season.Participants.Count / 2);

        var previous = season.Fixtures
            .Where(candidate => candidate.Status == FixtureStatus.ResultAccepted)
            .Where(candidate => candidate.HomeClubId == clubId || candidate.AwayClubId == clubId)
            .Where(candidate => candidate.ScheduledDate.DayNumber < day.DayNumber)
            .OrderByDescending(candidate => candidate.ScheduledDate.DayNumber)
            .FirstOrDefault();
        var daysSincePrevious = previous is null
            ? 7
            : day.DayNumber - previous.ScheduledDate.DayNumber;
        var rosterSlots = _clubSquadStore?.Get(clubId)?.Members.Select(member => member.SlotIndex)
            ?? Enumerable.Range(0, 25);
        var physicalBySlot = _trainingStore?.PhysicalStates
            .Where(state => state.ClubId == clubId)
            .ToDictionary(state => state.SlotIndex);
        var availableSlots = rosterSlots
            .Where(slot => physicalBySlot is null
                || !physicalBySlot.TryGetValue(slot, out var physical)
                || physical.IsAvailableOn(day))
            .Distinct()
            .OrderBy(slot => slot)
            .ToArray();
        if (availableSlots.Length < MatchSelection.StartingXiSize)
        {
            availableSlots = Enumerable.Range(0, 25)
                .Where(slot => physicalBySlot is null
                    || !physicalBySlot.TryGetValue(slot, out var physical)
                    || physical.IsAvailableOn(day))
                .ToArray();
        }
        var plan = OpponentMatchPlanResolver.Resolve(new OpponentMatchPlanInput(
            clubId.Value,
            fixture.Id.Value,
            fixture.Round.Value,
            Math.Max(2, season.Participants.Count),
            position,
            clubStrength,
            opponentStrength,
            daysSincePrevious,
            _timelineStore.Timeline.RootSeed,
            availableSlots));
        _resolvedAiStartingSlots[(fixture.Id.Value, clubId.Value)] = plan.StartingSlots;
        return plan;
    }

    private static OpponentMatchPlanReadModel? MapOpponentPlan(OpponentMatchPlan? plan) =>
        plan is null
            ? null
            : new OpponentMatchPlanReadModel(
                plan.Priority.ToString(),
                plan.Intent.ToString(),
                plan.RotationCount,
                plan.MatchStrengthModifier,
                plan.Headline);

    private static string ResolveXiPlayerName(
        IReadOnlyList<int> startingSlots,
        IReadOnlyList<string> names,
        int xiIndex)
    {
        if (xiIndex < 0 || xiIndex >= startingSlots.Count)
        {
            return $"slot {xiIndex}";
        }

        var clubSlot = startingSlots[xiIndex];
        if (clubSlot >= 0 && clubSlot < names.Count)
        {
            return names[clubSlot];
        }

        return $"slot {clubSlot}";
    }

    private MatchKeyMomentReadModel MapMomentToReadModel(
        MatchKeyMoment moment,
        FixtureId fixtureId,
        ClubId homeClubId,
        ClubId awayClubId,
        int rootSeed)
    {
        var clubId = moment.IsHomeSide ? homeClubId : awayClubId;
        var starting = ResolveStartingSlots(fixtureId, clubId);
        var names = MvpSquadRosterGenerator.GeneratePlayerNames(clubId, rootSeed);
        return new MatchKeyMomentReadModel(
            moment.Kind.ToString(),
            moment.Minute,
            moment.IsHomeSide,
            moment.PrimarySlotIndex,
            moment.AssistSlotIndex,
            ResolveXiPlayerName(starting, names, moment.PrimarySlotIndex),
            moment.AssistSlotIndex is int assistXi
                ? ResolveXiPlayerName(starting, names, assistXi)
                : null);
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

    /// <summary>
    /// Devre arası kontrol noktası — maçı işlemeden ilk yarı skorunu üretir.
    /// </summary>
    public MatchHalfTimePreview PreviewHalfTime(
        long seasonId,
        long fixtureId,
        int occurredAtDayNumber,
        int managedPreparationModifier = 0)
    {
        var occurredAt = CompetitionSeasonCommandSupport.ToGameDate(occurredAtDayNumber);
        var season = CompetitionSeasonCommandSupport.GetSeasonOrThrow(_competitionStore, seasonId);
        var fixture = season.Fixtures.FirstOrDefault(candidate => candidate.Id.Value == fixtureId)
            ?? throw new CompetitionInvariantViolationException($"Fixture {fixtureId} was not found.");

        var homeClub = _clubRegistryStore.Registry.GetClubOrThrow(fixture.HomeClubId);
        var awayClub = _clubRegistryStore.Registry.GetClubOrThrow(fixture.AwayClubId);
        var rootSeed = _timelineStore.Timeline.RootSeed;

        var managedClubId = _managerCareerStore?.Career.ActiveEmployment?.ClubId;
        var isManagedMatch = managedClubId is ClubId managed
            && (fixture.HomeClubId == managed || fixture.AwayClubId == managed);
        var safePreparationModifier = isManagedMatch
            ? Math.Clamp(managedPreparationModifier, -4, 4)
            : 0;

        var homeBonus = ResolveLineupBonus(fixture.Id, fixture.HomeClubId, rootSeed, occurredAt)
            + ResolvePhysicalModifier(fixture.Id, fixture.HomeClubId, occurredAt)
            + ResolveTacticModifier(fixture.HomeClubId)
            + ResolveLineupRoleModifier(fixture.Id, fixture.HomeClubId, rootSeed)
            + (managedClubId is ClubId homeManaged && fixture.HomeClubId == homeManaged
                ? safePreparationModifier
                : 0);
        var awayBonus = ResolveLineupBonus(fixture.Id, fixture.AwayClubId, rootSeed, occurredAt)
            + ResolvePhysicalModifier(fixture.Id, fixture.AwayClubId, occurredAt)
            + ResolveTacticModifier(fixture.AwayClubId)
            + ResolveLineupRoleModifier(fixture.Id, fixture.AwayClubId, rootSeed)
            + (managedClubId is ClubId awayManaged && fixture.AwayClubId == awayManaged
                ? safePreparationModifier
                : 0);

        var simulation = MvpFixtureMatchSimulator.SimulateWithKeyMoments(
            rootSeed,
            fixtureId,
            homeClub.SportiveStrength,
            awayClub.SportiveStrength,
            homeBonus,
            awayBonus);
        var halfTime = simulation.HalfTimeScore;

        var firstHalfMoments = simulation.KeyMoments
            .Where(moment => moment.Minute <= MvpFixtureMatchSimulator.HalfTimeMinute)
            .Select(moment => MapMomentToReadModel(moment, fixture.Id, fixture.HomeClubId, fixture.AwayClubId, rootSeed))
            .OrderBy(moment => moment.Minute)
            .ThenBy(moment => moment.Kind, StringComparer.Ordinal)
            .ThenBy(moment => moment.IsHomeSide ? 0 : 1)
            .ThenBy(moment => moment.PrimarySlotIndex)
            .ToArray();

        var managedIsHome = managedClubId is ClubId managedHome && fixture.HomeClubId == managedHome;

        return new MatchHalfTimePreview(
            fixtureId,
            homeClub.DisplayName,
            awayClub.DisplayName,
            halfTime.HomeGoals,
            halfTime.AwayGoals,
            managedIsHome,
            firstHalfMoments);
    }
}

public sealed record MatchHalfTimePreview(
    long FixtureId,
    string HomeClubName,
    string AwayClubName,
    int HomeGoals,
    int AwayGoals,
    bool ManagedIsHome,
    IReadOnlyList<MatchKeyMomentReadModel> FirstHalfMoments);
