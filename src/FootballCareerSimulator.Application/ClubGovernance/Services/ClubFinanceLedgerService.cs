using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.ClubGovernance;

namespace FootballCareerSimulator.Application.ClubGovernance.Services;

public enum BoardFinancialStatus
{
    Healthy = 1,
    Watch = 2,
    Critical = 3,
}

public sealed record BoardFinancialOutcome(
    BoardFinancialStatus Status,
    int ConfidenceDelta,
    string Summary);

public sealed record ClubFinanceSnapshot(
    long ClubId,
    string CurrencyCode,
    long OpeningBalance,
    long Revenue,
    long Expenses,
    long OperatingResult,
    long Balance,
    IReadOnlyList<ClubLedgerEntry> Entries,
    BoardFinancialOutcome BoardOutcome);

/// <summary>
/// Gerçekleşen finans hareketlerinin idempotent uygulama servisi.
/// Aynı sezon/ay veya fikstür yeniden işlendiğinde bakiye ikinci kez değişmez.
/// </summary>
public sealed class ClubFinanceLedgerService
{
    public const string CurrencyCode = "TRY";

    private readonly IClubFinanceLedgerStore _store;

    public ClubFinanceLedgerService(IClubFinanceLedgerStore store) =>
        _store = store ?? throw new ArgumentNullException(nameof(store));

    public ClubFinanceSnapshot Open(ClubId clubId, long openingBalance)
    {
        var ledger = _store.Find(clubId);
        if (ledger is null)
        {
            ledger = ClubFinanceLedger.Open(clubId, openingBalance);
            _store.Save(ledger);
        }

        return Snapshot(ledger);
    }

    public ClubFinanceSnapshot RecordMonthlySettlement(
        ClubId clubId,
        long seasonId,
        GameDate day,
        MvpClubEconomyProjection projection,
        long openingBalance = 0)
    {
        var ledger = GetOrOpen(clubId, openingBalance);
        foreach (var entry in RealizedClubFinanceCalculator.MonthlySettlement(
                     clubId,
                     seasonId,
                     day,
                     projection))
        {
            ledger = ledger.Post(entry);
        }

        _store.Save(ledger);
        return Snapshot(ledger);
    }

    public ClubFinanceSnapshot RecordHomeMatchday(
        ClubId clubId,
        long seasonId,
        FixtureId fixtureId,
        GameDate day,
        MvpClubEconomyProjection projection,
        int? actualAttendance = null,
        long openingBalance = 0)
    {
        var ledger = GetOrOpen(clubId, openingBalance).Post(
            RealizedClubFinanceCalculator.HomeMatchday(
                clubId,
                seasonId,
                fixtureId,
                day,
                projection,
                actualAttendance));
        _store.Save(ledger);
        return Snapshot(ledger);
    }

    public ClubFinanceSnapshot? Get(ClubId clubId)
    {
        var ledger = _store.Find(clubId);
        return ledger is null ? null : Snapshot(ledger);
    }

    private ClubFinanceLedger GetOrOpen(ClubId clubId, long openingBalance) =>
        _store.Find(clubId) ?? ClubFinanceLedger.Open(clubId, openingBalance);

    private static ClubFinanceSnapshot Snapshot(ClubFinanceLedger ledger) =>
        new(
            ledger.ClubId.Value,
            CurrencyCode,
            ledger.OpeningBalance,
            ledger.Revenue,
            ledger.Expenses,
            ledger.OperatingResult,
            ledger.Balance,
            ledger.Entries,
            EvaluateBoardOutcome(ledger));

    private static BoardFinancialOutcome EvaluateBoardOutcome(ClubFinanceLedger ledger)
    {
        if (ledger.Balance < 0)
        {
            return new BoardFinancialOutcome(
                BoardFinancialStatus.Critical,
                -5,
                "Kulüp bakiyesi eksiye düştü; yönetim acil tasarruf bekliyor.");
        }

        if (ledger.OperatingResult < 0)
        {
            return new BoardFinancialOutcome(
                BoardFinancialStatus.Watch,
                -2,
                "Gerçekleşen faaliyet sonucu açık veriyor; yönetim finansal disiplini izliyor.");
        }

        return new BoardFinancialOutcome(
            BoardFinancialStatus.Healthy,
            2,
            "Gerçekleşen faaliyet sonucu dengeli; yönetim finansal gidişattan memnun.");
    }
}
