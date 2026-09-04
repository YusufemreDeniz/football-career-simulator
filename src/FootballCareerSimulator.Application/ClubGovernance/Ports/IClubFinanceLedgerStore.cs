using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.ClubGovernance.Ports;

/// <summary>
/// SQLite entegrasyon sınırı: ledger opening balance ve bütün entry'lerle restore edilmelidir.
/// </summary>
public interface IClubFinanceLedgerStore
{
    IReadOnlyList<ClubFinanceLedger> Ledgers { get; }

    ClubFinanceLedger? Find(ClubId clubId);

    void Save(ClubFinanceLedger ledger);

    void ReplaceAll(IReadOnlyList<ClubFinanceLedger> ledgers);
}
