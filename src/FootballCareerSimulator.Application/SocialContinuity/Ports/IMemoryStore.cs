using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Ports;

public interface IMemoryStore
{
    IReadOnlyList<MemoryRecord> Memories { get; }

    long NextMemoryId { get; }

    MemoryRecord? Get(MemoryId memoryId);

    bool WasProcessed(
        ActorRef rememberingActor,
        string sourceEventKey,
        string ruleId,
        int ruleVersion);

    MemoryRecord? FindLatestActiveOrDormant(
        ActorRef rememberingActor,
        string ruleId,
        int ruleVersion);

    void Upsert(MemoryRecord memory);

    void ReplaceAll(IEnumerable<MemoryRecord> memories);
}
