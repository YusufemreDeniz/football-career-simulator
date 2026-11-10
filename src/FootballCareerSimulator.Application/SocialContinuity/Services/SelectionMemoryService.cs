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
    private readonly MemoryResolutionEvaluator _redemption = new();

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
        GameDate day,
        bool isCrucialMatch = false)
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

        ApplyStartRedemption(started, isCrucialMatch, day);
        return stats;
    }

    /// <summary>
    /// Redemption Memory (Yenilik 7): ilk 11'de görev alan oyuncunun menajere karşı olumsuz seçim/söz/ilişki
    /// anıları telafi edilir — kritik maçta tamamen, diğer maçlarda kısmen. Aynı gün ikinci kez uygulanmaz.
    /// </summary>
    public int ApplyStartRedemption(IEnumerable<PlayerId> startedPlayerIds, bool isCrucialMatch, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(startedPlayerIds);
        var redeemed = 0;
        foreach (var playerId in startedPlayerIds.Distinct())
        {
            var remembering = new ActorRef(ActorKind.Player, playerId.Value);
            var candidates = _store.GetByRememberingActor(remembering)
                .Where(memory => memory.Status == MemoryStatus.Active
                                 && memory.Valence == MemoryValence.Negative
                                 && memory.Category is MemoryCategory.Selection
                                     or MemoryCategory.Promise
                                     or MemoryCategory.Relationship
                                 && memory.ResolvedOn != day)
                .ToArray();
            foreach (var memory in candidates)
            {
                var resolved = _redemption.EvaluateSelectionRedemption(
                    memory,
                    isStartingAppearance: true,
                    isCrucialMatch,
                    day);
                if (!ReferenceEquals(resolved, memory))
                {
                    _store.Upsert(resolved);
                    redeemed++;
                }
            }
        }

        return redeemed;
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

        var existing = _store.FindLatestActiveOrDormant(remembering, ruleId, ruleVersion);

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

        var nextId = _store.NextMemoryId;

        _store.Upsert(factory(new MemoryId(nextId), remembering, fixtureId, day));
        return MemoryCandidateDecision.Created;
    }

    private bool IsProcessedEvent(
        ActorRef remembering,
        string sourceKey,
        string ruleId,
        int ruleVersion) =>
        _store.WasProcessed(remembering, sourceKey, ruleId, ruleVersion);
}
