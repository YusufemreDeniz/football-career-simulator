using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Relationship authoritative owner (iskelet). Promise/Selection girdilerini değerlendirir;
/// Memory doğrudan delta uygulamaz.
/// </summary>
public sealed class RelationshipEvaluationService
{
    public const string PromiseFulfilledRuleId = "PromiseFulfilledTrust";
    public const string PromiseBrokenRuleId = "PromiseBrokenTrust";
    public const string SelectionStartedRuleId = "SelectionStartedRespect";
    public const string SelectionOmittedRuleId = "SelectionOmittedCompatibility";
    public const int RuleVersion = 1;

    private readonly IRelationshipStore _store;

    public RelationshipEvaluationService(IRelationshipStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
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
