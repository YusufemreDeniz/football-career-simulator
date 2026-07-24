using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// İlk 11 seçimi → Selection Memory (idempotent, oyuncu perspektifi).
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
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(startingPlayerIds);

        var created = 0;
        foreach (var playerId in startingPlayerIds.Distinct())
        {
            var remembering = new ActorRef(ActorKind.Player, playerId.Value);
            var sourceKey = MemoryRecord.BuildSelectionStartedSourceKey(fixtureId, playerId.Value);
            var exists = _store.Memories.Any(m =>
                m.SourceEventKey == sourceKey
                && m.RememberingActor == remembering
                && m.RuleId == MemoryRecord.SelectionStartedRuleId
                && m.RuleVersion == MemoryRecord.SelectionStartedRuleVersion);

            if (exists)
            {
                continue;
            }

            var nextId = _store.Memories.Count == 0
                ? 1L
                : _store.Memories.Max(m => m.MemoryId.Value) + 1;

            _store.Upsert(MemoryRecord.CreateSelectionStarted(
                new MemoryId(nextId),
                remembering,
                fixtureId,
                day));
            created++;
        }

        return created;
    }
}
