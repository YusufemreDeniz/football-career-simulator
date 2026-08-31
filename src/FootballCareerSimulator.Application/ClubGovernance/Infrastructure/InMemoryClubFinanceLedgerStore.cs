using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.ClubGovernance.Infrastructure;

public sealed class InMemoryClubFinanceLedgerStore : IClubFinanceLedgerStore
{
    private readonly Dictionary<ClubId, ClubFinanceLedger> _ledgers = [];

    public IReadOnlyList<ClubFinanceLedger> Ledgers =>
        _ledgers.Values.OrderBy(ledger => ledger.ClubId.Value).ToArray();

    public ClubFinanceLedger? Find(ClubId clubId) =>
        _ledgers.GetValueOrDefault(clubId);

    public void Save(ClubFinanceLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        _ledgers[ledger.ClubId] = ledger;
    }

    public void ReplaceAll(IReadOnlyList<ClubFinanceLedger> ledgers)
    {
        ArgumentNullException.ThrowIfNull(ledgers);
        _ledgers.Clear();
        foreach (var ledger in ledgers)
        {
            Save(ledger);
        }
    }
}
