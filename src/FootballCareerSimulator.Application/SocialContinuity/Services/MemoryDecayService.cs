using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Active Memory CurrentInfluence time decay (oyun zamanı; dönemsel batch).
/// </summary>
public sealed class MemoryDecayService
{
    private readonly IMemoryStore _store;

    public MemoryDecayService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int ApplyDue(GameDate day)
    {
        var updated = 0;
        foreach (var memory in _store.Memories
                     .Where(m => m.Status == MemoryStatus.Active)
                     .ToArray())
        {
            var next = memory.ApplyTimeDecay(day);
            if (!ReferenceEquals(next, memory))
            {
                _store.Upsert(next);
                updated++;
            }
        }

        return updated;
    }
}
