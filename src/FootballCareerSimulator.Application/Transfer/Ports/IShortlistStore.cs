using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Ports;

public interface IShortlistStore
{
    IReadOnlyList<ShortlistEntry> Entries { get; }

    ShortlistEntry? Get(ShortlistEntryId entryId);

    IReadOnlyList<ShortlistEntry> GetForClub(ClubId clubId);

    void Upsert(ShortlistEntry entry);

    void ReplaceAll(IEnumerable<ShortlistEntry> entries);
}
