using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Infrastructure;

public sealed class InMemoryMemoryStore : IMemoryStore
{
    private readonly Dictionary<long, MemoryRecord> _byId = new();

    public IReadOnlyList<MemoryRecord> Memories =>
        _byId.Values.OrderBy(m => m.MemoryId.Value).ToArray();

    public MemoryRecord? Get(MemoryId memoryId) =>
        _byId.TryGetValue(memoryId.Value, out var memory) ? memory : null;

    public void Upsert(MemoryRecord memory)
    {
        ArgumentNullException.ThrowIfNull(memory);
        _byId[memory.MemoryId.Value] = memory;
    }

    public void ReplaceAll(IEnumerable<MemoryRecord> memories)
    {
        ArgumentNullException.ThrowIfNull(memories);
        _byId.Clear();
        foreach (var memory in memories)
        {
            _byId[memory.MemoryId.Value] = memory;
        }
    }
}
