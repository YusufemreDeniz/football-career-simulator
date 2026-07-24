using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Maç kadro seçimi → Selection Memory (ilk 11 / yedek / kadro dışı; idempotent).
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
            day);

    public int RecordMatchday(
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

        var created = 0;
        foreach (var playerId in started)
        {
            created += TryCreate(
                fixtureId,
                playerId,
                day,
                MemoryRecord.BuildSelectionStartedSourceKey(fixtureId, playerId.Value),
                MemoryRecord.SelectionStartedRuleId,
                MemoryRecord.SelectionStartedRuleVersion,
                MemoryRecord.CreateSelectionStarted) ? 1 : 0;
        }

        foreach (var playerId in benched)
        {
            created += TryCreate(
                fixtureId,
                playerId,
                day,
                MemoryRecord.BuildSelectionBenchedSourceKey(fixtureId, playerId.Value),
                MemoryRecord.SelectionBenchedRuleId,
                MemoryRecord.SelectionBenchedRuleVersion,
                MemoryRecord.CreateSelectionBenched) ? 1 : 0;
        }

        if (squadMembers is { Count: > 0 })
        {
            var matchday = started.Concat(benched).ToHashSet();
            foreach (var playerId in squadMembers.Distinct().Where(id => !matchday.Contains(id)))
            {
                created += TryCreate(
                    fixtureId,
                    playerId,
                    day,
                    MemoryRecord.BuildSelectionOmittedSourceKey(fixtureId, playerId.Value),
                    MemoryRecord.SelectionOmittedRuleId,
                    MemoryRecord.SelectionOmittedRuleVersion,
                    MemoryRecord.CreateSelectionOmitted) ? 1 : 0;
            }
        }

        return created;
    }

    private bool TryCreate(
        FixtureId fixtureId,
        PlayerId playerId,
        GameDate day,
        string sourceKey,
        string ruleId,
        int ruleVersion,
        Func<MemoryId, ActorRef, FixtureId, GameDate, MemoryRecord> factory)
    {
        var remembering = new ActorRef(ActorKind.Player, playerId.Value);
        var exists = _store.Memories.Any(m =>
            m.SourceEventKey == sourceKey
            && m.RememberingActor == remembering
            && m.RuleId == ruleId
            && m.RuleVersion == ruleVersion);

        if (exists)
        {
            return false;
        }

        var nextId = _store.Memories.Count == 0
            ? 1L
            : _store.Memories.Max(m => m.MemoryId.Value) + 1;

        _store.Upsert(factory(new MemoryId(nextId), remembering, fixtureId, day));
        return true;
    }
}
