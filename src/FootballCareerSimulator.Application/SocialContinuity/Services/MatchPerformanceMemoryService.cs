using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Sınırlı Match Performance Memory: yönetilen kulüpte fark ≥ 3 (blowout).
/// Menajer + ilk 11; ilişki / narrative yok.
/// </summary>
public sealed class MatchPerformanceMemoryService
{
    private readonly IMemoryStore _store;

    public MatchPerformanceMemoryService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int RecordBlowoutIfApplicable(
        FixtureId fixtureId,
        int managedGoals,
        int opponentGoals,
        ManagerId managerId,
        IReadOnlyList<PlayerId> startingPlayerIds,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(startingPlayerIds);

        var difference = managedGoals - opponentGoals;
        if (Math.Abs(difference) < MemoryRecord.MatchBlowoutMinGoalDifference)
        {
            return 0;
        }

        var managedWon = difference > 0;
        var created = 0;
        created += TryCreate(
            new ActorRef(ActorKind.Manager, managerId.Value),
            fixtureId,
            day,
            managedWon)
            ? 1
            : 0;

        foreach (var playerId in startingPlayerIds.Distinct())
        {
            created += TryCreate(
                new ActorRef(ActorKind.Player, playerId.Value),
                fixtureId,
                day,
                managedWon)
                ? 1
                : 0;
        }

        return created;
    }

    private bool TryCreate(
        ActorRef rememberingActor,
        FixtureId fixtureId,
        GameDate day,
        bool managedWon)
    {
        var sourceKey = MemoryRecord.BuildMatchBlowoutSourceKey(fixtureId, rememberingActor);
        var exists = _store.Memories.Any(m =>
            m.SourceEventKey == sourceKey
            && m.RememberingActor == rememberingActor
            && m.RuleId == MemoryRecord.MatchBlowoutRuleId
            && m.RuleVersion == MemoryRecord.MatchBlowoutRuleVersion);

        if (exists)
        {
            return false;
        }

        var nextId = _store.Memories.Count == 0
            ? 1L
            : _store.Memories.Max(m => m.MemoryId.Value) + 1;

        _store.Upsert(MemoryRecord.CreateMatchBlowout(
            new MemoryId(nextId),
            rememberingActor,
            fixtureId,
            day,
            managedWon));
        return true;
    }
}
