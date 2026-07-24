using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Ports;

public interface IMemoryStore
{
    IReadOnlyList<MemoryRecord> Memories { get; }

    MemoryRecord? Get(MemoryId memoryId);

    void Upsert(MemoryRecord memory);

    void ReplaceAll(IEnumerable<MemoryRecord> memories);
}
