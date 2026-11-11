using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Infrastructure;

public sealed class InMemoryShortlistStore : IShortlistStore
{
    private readonly Dictionary<long, ShortlistEntry> _byId = new();

    public IReadOnlyList<ShortlistEntry> Entries =>
        _byId.Values.OrderBy(e => e.EntryId.Value).ToArray();

    public ShortlistEntry? Get(ShortlistEntryId entryId) =>
        _byId.TryGetValue(entryId.Value, out var entry) ? entry : null;

    public IReadOnlyList<ShortlistEntry> GetForClub(ClubId clubId) =>
        _byId.Values
            .Where(e => e.ClubId.Value == clubId.Value)
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => e.EntryId.Value)
            .ToArray();

    public void Upsert(ShortlistEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _byId[entry.EntryId.Value] = entry;
    }

    public void ReplaceAll(IEnumerable<ShortlistEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _byId.Clear();
        foreach (var entry in entries)
        {
            _byId[entry.EntryId.Value] = entry;
        }
    }
}
