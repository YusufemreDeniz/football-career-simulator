using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Relationship authoritative owner (iskelet). Promise/Selection girdileri + Dormant/Reactivate.
/// Memory doğrudan delta uygulamaz; Trust band milestone ayrı servisle üretilir.
/// </summary>
public sealed class RelationshipEvaluationService
{
    public const string PromiseFulfilledRuleId = "PromiseFulfilledTrust";
    public const string PromiseBrokenRuleId = "PromiseBrokenTrust";
    public const string SelectionStartedRuleId = "SelectionStartedRespect";
    public const string SelectionOmittedRuleId = "SelectionOmittedCompatibility";
    public const string PlayerLeftDormantRuleId = "PlayerLeftDormant";
    public const string ManagerLeftDormantRuleId = "ManagerLeftDormant";
    public const string ManagerHiredReactivateRuleId = "ManagerHiredReactivate";
    public const string FormerPlayerEncounterReactivateRuleId = "FormerPlayerEncounterReactivate";
    public const string DecisionPlayingTimeGrantedRuleId = "DecisionPlayingTimeGranted";
    public const string DecisionPlayingTimeRefusedRuleId = "DecisionPlayingTimeRefused";
    public const string DecisionPlayingTimeExpiredRuleId = "DecisionPlayingTimeExpired";
    public const string DecisionStartingOpportunityGrantedRuleId = "DecisionStartingOpportunityGranted";
    public const string DecisionStartingOpportunityRefusedRuleId = "DecisionStartingOpportunityRefused";
    public const string DecisionStartingOpportunityExpiredRuleId = "DecisionStartingOpportunityExpired";
    public const string DecisionTransferAcknowledgedRuleId = "DecisionTransferAcknowledged";
    public const string DecisionTransferRefusedRuleId = "DecisionTransferRefused";
    public const string DecisionTransferExpiredRuleId = "DecisionTransferExpired";
    public const string DecisionDisciplineWarningRuleId = "DecisionDisciplineWarning";
    public const string DecisionDisciplineFineRuleId = "DecisionDisciplineFine";
    public const string DecisionDisciplineSupportRuleId = "DecisionDisciplineSupport";
    public const string DecisionDisciplineExpiredRuleId = "DecisionDisciplineExpired";
    public const string DecisionPressDefendRuleId = "DecisionPressDefend";
    public const string DecisionPressCriticizeRuleId = "DecisionPressCriticize";
    public const string DecisionPressExpiredRuleId = "DecisionPressExpired";
    public const int RuleVersion = 1;

    private readonly IRelationshipStore _store;
    private readonly RelationshipMilestoneService? _milestones;

    public RelationshipEvaluationService(
        IRelationshipStore store,
        RelationshipMilestoneService? milestones = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _milestones = milestones;
    }

    public int ApplyPromiseOutcome(Promise promise, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(promise);
        if (promise.Status is not (PromiseStatus.Fulfilled or PromiseStatus.Broken))
        {
            return 0;
        }

        if (promise.Promisee.Kind != ActorKind.Player || promise.Promisor.Kind != ActorKind.Manager)
        {
            return 0;
        }

        var trustDelta = promise.Status == PromiseStatus.Fulfilled ? 8 : -12;
        var ruleId = promise.Status == PromiseStatus.Fulfilled
            ? PromiseFulfilledRuleId
            : PromiseBrokenRuleId;
        var effectKey = $"Rel:{ruleId}:v{RuleVersion}:{promise.PromiseId.Value}";
        return Apply(
            new PlayerId(promise.Promisee.Id),
            new ManagerId(promise.Promisor.Id),
            effectKey,
            trustDelta,
            respectDelta: 0,
            compatibilityDelta: 0,
            reasonCode: ruleId,
            day);
    }

    public int ApplySelectionStarted(
        FixtureId fixtureId,
        PlayerId playerId,
        ManagerId managerId,
        GameDate day)
    {
        var effectKey = $"Rel:{SelectionStartedRuleId}:v{RuleVersion}:{fixtureId.Value}:{playerId.Value}";
        return Apply(
            playerId,
            managerId,
            effectKey,
            trustDelta: 0,
            respectDelta: 2,
            compatibilityDelta: 0,
            reasonCode: SelectionStartedRuleId,
            day);
    }

    public int ApplySelectionOmitted(
        FixtureId fixtureId,
        PlayerId playerId,
        ManagerId managerId,
        GameDate day)
    {
        var effectKey = $"Rel:{SelectionOmittedRuleId}:v{RuleVersion}:{fixtureId.Value}:{playerId.Value}";
        return Apply(
            playerId,
            managerId,
            effectKey,
            trustDelta: 0,
            respectDelta: 0,
            compatibilityDelta: -3,
            reasonCode: SelectionOmittedRuleId,
            day);
    }

    public int MarkDormantForPlayerLeaving(PlayerId playerId, GameDate day)
    {
        var updated = 0;
        foreach (var relationship in _store.Relationships
                     .Where(r =>
                         r.Status == RelationshipStatus.Active
                         && r.Observer.Kind == ActorKind.Player
                         && r.Observer.Id == playerId.Value)
                     .ToArray())
        {
            var effectKey =
                $"Rel:{PlayerLeftDormantRuleId}:v{RuleVersion}:{relationship.RelationshipId.Value}:{playerId.Value}";
            var next = relationship.MarkDormant(PlayerLeftDormantRuleId, day, effectKey);
            if (!ReferenceEquals(next, relationship))
            {
                _store.Upsert(next);
                updated++;
            }
        }

        return updated;
    }

    public int MarkDormantForManagerLeaving(ManagerId managerId, GameDate day)
    {
        var updated = 0;
        foreach (var relationship in _store.Relationships
                     .Where(r =>
                         r.Status == RelationshipStatus.Active
                         && r.Subject.Kind == ActorKind.Manager
                         && r.Subject.Id == managerId.Value)
                     .ToArray())
        {
            var effectKey =
                $"Rel:{ManagerLeftDormantRuleId}:v{RuleVersion}:{relationship.RelationshipId.Value}:{managerId.Value}";
            var next = relationship.MarkDormant(ManagerLeftDormantRuleId, day, effectKey);
            if (!ReferenceEquals(next, relationship))
            {
                _store.Upsert(next);
                updated++;
            }
        }

        return updated;
    }

    public int ReactivateForManager(ManagerId managerId, GameDate day)
    {
        var updated = 0;
        foreach (var relationship in _store.Relationships
                     .Where(r =>
                         r.Status == RelationshipStatus.Dormant
                         && r.Subject.Kind == ActorKind.Manager
                         && r.Subject.Id == managerId.Value)
                     .ToArray())
        {
            var next = relationship.Reactivate(ManagerHiredReactivateRuleId, day);
            if (!ReferenceEquals(next, relationship))
            {
                _store.Upsert(next);
                updated++;
            }
        }

        return updated;
    }

    public IReadOnlyList<PlayerId> ReactivateForFormerPlayerEncounter(
        ManagerId managerId,
        IReadOnlyCollection<PlayerId> opponentPlayerIds,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(opponentPlayerIds);
        var opponentIds = opponentPlayerIds.Select(playerId => playerId.Value).ToHashSet();
        var reactivated = new List<PlayerId>();
        foreach (var relationship in _store.Relationships
                     .Where(record =>
                         record.Status == RelationshipStatus.Dormant
                         && record.Subject == new ActorRef(ActorKind.Manager, managerId.Value)
                         && record.Observer.Kind == ActorKind.Player
                         && opponentIds.Contains(record.Observer.Id))
                     .OrderBy(record => record.Observer.Id)
                     .ToArray())
        {
            _store.Upsert(relationship.Reactivate(FormerPlayerEncounterReactivateRuleId, day));
            reactivated.Add(new PlayerId(relationship.Observer.Id));
        }

        return reactivated;
    }

    public int ApplyDecisionRequestOutcome(DecisionRequest request, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Kind switch
        {
            DecisionRequestKind.PlayingTimeRequest => ApplyPlayingTimeDecision(request, day),
            DecisionRequestKind.StartingOpportunityRequest => ApplyStartingOpportunityDecision(request, day),
            DecisionRequestKind.TransferRequest => ApplyTransferDecision(request, day),
            DecisionRequestKind.DisciplineRequest => ApplyDisciplineDecision(request, day),
            DecisionRequestKind.PressQuestionRequest => ApplyPressQuestionDecision(request, day),
            _ => 0,
        };
    }

    private int ApplyPlayingTimeDecision(DecisionRequest request, GameDate day) =>
        request.Status switch
        {
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionGrantPlayingTimePromise =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionPlayingTimeGrantedRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: 6,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionPlayingTimeGrantedRuleId,
                    day),
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionRefuse =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionPlayingTimeRefusedRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -10,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionPlayingTimeRefusedRuleId,
                    day),
            DecisionRequestStatus.Expired =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionPlayingTimeExpiredRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -6,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionPlayingTimeExpiredRuleId,
                    day),
            _ => 0,
        };

    private int ApplyStartingOpportunityDecision(DecisionRequest request, GameDate day) =>
        request.Status switch
        {
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionGrantStartingOpportunityPromise =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionStartingOpportunityGrantedRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: 6,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionStartingOpportunityGrantedRuleId,
                    day),
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionRefuse =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionStartingOpportunityRefusedRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -10,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionStartingOpportunityRefusedRuleId,
                    day),
            DecisionRequestStatus.Expired =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionStartingOpportunityExpiredRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -6,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionStartingOpportunityExpiredRuleId,
                    day),
            _ => 0,
        };

    private int ApplyTransferDecision(DecisionRequest request, GameDate day) =>
        request.Status switch
        {
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionAcknowledgeTransferRequest =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionTransferAcknowledgedRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: 6,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionTransferAcknowledgedRuleId,
                    day),
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionRefuse =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionTransferRefusedRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -10,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionTransferRefusedRuleId,
                    day),
            DecisionRequestStatus.Expired =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionTransferExpiredRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -6,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionTransferExpiredRuleId,
                    day),
            _ => 0,
        };

    private int ApplyDisciplineDecision(DecisionRequest request, GameDate day) =>
        request.Status switch
        {
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionIssueWarning =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionDisciplineWarningRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -2,
                    respectDelta: 4,
                    compatibilityDelta: 0,
                    reasonCode: DecisionDisciplineWarningRuleId,
                    day),
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionIssueFine =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionDisciplineFineRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -6,
                    respectDelta: 6,
                    compatibilityDelta: -2,
                    reasonCode: DecisionDisciplineFineRuleId,
                    day),
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionOfferSupport =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionDisciplineSupportRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: 6,
                    respectDelta: -2,
                    compatibilityDelta: 0,
                    reasonCode: DecisionDisciplineSupportRuleId,
                    day),
            DecisionRequestStatus.Expired =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionDisciplineExpiredRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -6,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionDisciplineExpiredRuleId,
                    day),
            _ => 0,
        };

    private int ApplyPressQuestionDecision(DecisionRequest request, GameDate day) =>
        request.Status switch
        {
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionPubliclyDefend =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionPressDefendRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: 8,
                    respectDelta: 2,
                    compatibilityDelta: 0,
                    reasonCode: DecisionPressDefendRuleId,
                    day),
            DecisionRequestStatus.Answered when
                request.SelectedOptionCode == DecisionRequest.OptionPubliclyCriticize =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionPressCriticizeRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -10,
                    respectDelta: -4,
                    compatibilityDelta: 0,
                    reasonCode: DecisionPressCriticizeRuleId,
                    day),
            DecisionRequestStatus.Expired =>
                Apply(
                    request.SubjectPlayerId,
                    request.ManagerId,
                    $"Rel:{DecisionPressExpiredRuleId}:v{RuleVersion}:{request.DecisionRequestId.Value}",
                    trustDelta: -4,
                    respectDelta: 0,
                    compatibilityDelta: 0,
                    reasonCode: DecisionPressExpiredRuleId,
                    day),
            _ => 0,
        };

    private int Apply(
        PlayerId playerId,
        ManagerId managerId,
        string effectKey,
        int trustDelta,
        int respectDelta,
        int compatibilityDelta,
        string reasonCode,
        GameDate day)
    {
        var current = Ensure(playerId, managerId, day);
        if (current.ProcessedEffectKeys.Contains(effectKey))
        {
            return 0;
        }

        var next = current.ApplyDimensionDeltas(
            effectKey,
            trustDelta,
            respectDelta,
            compatibilityDelta,
            reasonCode,
            day);
        _store.Upsert(next);
        _milestones?.EvaluateTrustBandChange(current, next, day);
        return 1;
    }

    private RelationshipRecord Ensure(PlayerId playerId, ManagerId managerId, GameDate day)
    {
        var existing = _store.FindPlayerToManager(playerId.Value, managerId.Value);
        if (existing is not null)
        {
            return existing;
        }

        var nextId = _store.Relationships.Count == 0
            ? 1L
            : _store.Relationships.Max(r => r.RelationshipId.Value) + 1;
        var created = RelationshipRecord.CreatePlayerToManager(
            new RelationshipId(nextId),
            playerId,
            managerId,
            day);
        _store.Upsert(created);
        return created;
    }
}
