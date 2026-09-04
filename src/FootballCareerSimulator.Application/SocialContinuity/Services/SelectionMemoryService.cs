using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Queries;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Maç kadro seçimi → Selection Memory (Create / Reinforce / Reject; idempotent).
/// </summary>
public sealed class SelectionMemoryService
{
    private readonly IMemoryStore _store;

    public SelectionMemoryService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int RecordStarts(
        FixtureId fixtureId,
        IReadOnlyList<PlayerId> startingPlayerIds,
        GameDate day) =>
        RecordMatchday(
            fixtureId,
            startingPlayerIds,
            Array.Empty<PlayerId>(),
            squadMembers: null,
            day).Applied;

    public MemoryMutationStats RecordMatchday(
        FixtureId fixtureId,
        IReadOnlyList<PlayerId> startingPlayerIds,
        IReadOnlyList<PlayerId> benchedPlayerIds,
        IReadOnlyList<PlayerId>? squadMembers,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(startingPlayerIds);
        ArgumentNullException.ThrowIfNull(benchedPlayerIds);

        var started = startingPlayerIds.Distinct().ToHashSet();
        var benched = benchedPlayerIds.Distinct().Where(id => !started.Contains(id)).ToHashSet();
        var stats = MemoryMutationStats.Empty;

        foreach (var playerId in started)
        {
            stats = stats.AddDecision(TryCreateOrReinforce(
                fixtureId,
                playerId,
                day,
                MemoryRecord.BuildSelectionStartedSourceKey(fixtureId, playerId.Value),
                MemoryRecord.SelectionStartedRuleId,
                MemoryRecord.SelectionStartedRuleVersion,
                MemoryRecord.CreateSelectionStarted));
        }

        foreach (var playerId in benched)
        {
            stats = stats.AddDecision(TryCreateOnly(
                fixtureId,
                playerId,
                day,
                MemoryRecord.BuildSelectionBenchedSourceKey(fixtureId, playerId.Value),
                MemoryRecord.SelectionBenchedRuleId,
                MemoryRecord.SelectionBenchedRuleVersion,
                MemoryRecord.CreateSelectionBenched));
        }

        if (squadMembers is { Count: > 0 })
        {
            var matchday = started.Concat(benched).ToHashSet();
            foreach (var playerId in squadMembers.Distinct().Where(id => !matchday.Contains(id)))
            {
                stats = stats.AddDecision(TryCreateOrReinforce(
                    fixtureId,
                    playerId,
                    day,
                    MemoryRecord.BuildSelectionOmittedSourceKey(fixtureId, playerId.Value),
                    MemoryRecord.SelectionOmittedRuleId,
                    MemoryRecord.SelectionOmittedRuleVersion,
                    MemoryRecord.CreateSelectionOmitted));
            }
        }

        return stats;
    }

    private MemoryCandidateDecision TryCreateOrReinforce(
        FixtureId fixtureId,
        PlayerId playerId,
        GameDate day,
        string sourceKey,
        string ruleId,
        int ruleVersion,
        Func<MemoryId, ActorRef, FixtureId, GameDate, MemoryRecord> factory)
    {
        var remembering = new ActorRef(ActorKind.Player, playerId.Value);
        if (IsProcessedEvent(remembering, sourceKey, ruleId, ruleVersion))
        {
            return MemoryCandidateDecision.Rejected;
        }

        var existing = _store.Memories
            .Where(m =>
                m.RememberingActor == remembering
                && m.RuleId == ruleId
                && m.RuleVersion == ruleVersion
                && m.Status is MemoryStatus.Active or MemoryStatus.Dormant)
            .OrderByDescending(m => m.LastReinforcedOn.DayNumber)
            .ThenByDescending(m => m.MemoryId.Value)
            .FirstOrDefault();

        if (existing is not null)
        {
            if (existing.ReinforcementCount >= MemoryRecord.MaxReinforcementsPerMemory)
            {
                return MemoryCandidateDecision.Rejected;
            }

            var reinforced = existing.Reinforce(sourceKey, day);
            if (ReferenceEquals(reinforced, existing))
            {
                return MemoryCandidateDecision.Rejected;
            }

            _store.Upsert(reinforced);
            return MemoryCandidateDecision.Reinforced;
        }

        return TryCreateOnly(fixtureId, playerId, day, sourceKey, ruleId, ruleVersion, factory);
    }

    private MemoryCandidateDecision TryCreateOnly(
        FixtureId fixtureId,
        PlayerId playerId,
        GameDate day,
        string sourceKey,
        string ruleId,
        int ruleVersion,
        Func<MemoryId, ActorRef, FixtureId, GameDate, MemoryRecord> factory)
    {
        var remembering = new ActorRef(ActorKind.Player, playerId.Value);
        if (IsProcessedEvent(remembering, sourceKey, ruleId, ruleVersion))
        {
            return MemoryCandidateDecision.Rejected;
        }

        var nextId = _store.Memories.Count == 0
            ? 1L
            : _store.Memories.Max(m => m.MemoryId.Value) + 1;

        _store.Upsert(factory(new MemoryId(nextId), remembering, fixtureId, day));
        return MemoryCandidateDecision.Created;
    }

    private bool IsProcessedEvent(
        ActorRef remembering,
        string sourceKey,
        string ruleId,
        int ruleVersion) =>
        _store.Memories.Any(m =>
            (m.SourceEventKey == sourceKey
             && m.RememberingActor == remembering
             && m.RuleId == ruleId
             && m.RuleVersion == ruleVersion)
            || (m.RememberingActor == remembering
                && m.ProcessedReinforcementKeys.Contains(sourceKey)));
}
