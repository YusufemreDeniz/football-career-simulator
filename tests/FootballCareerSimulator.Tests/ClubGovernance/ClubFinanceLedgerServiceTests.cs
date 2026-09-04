using FootballCareerSimulator.Application.ClubGovernance.Infrastructure;
using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.ClubGovernance;

namespace FootballCareerSimulator.Tests.ClubGovernance;

public sealed class ClubFinanceLedgerServiceTests
{
    private static readonly ClubId ClubId = new(1);
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 31);

    [Fact]
    public void MonthlySettlement_PostsSponsorWagesAndOperationsExactlyOnce()
    {
        var store = new InMemoryClubFinanceLedgerStore();
        var service = new ClubFinanceLedgerService(store);
        var projection = Projection();

        var first = service.RecordMonthlySettlement(
            ClubId,
            seasonId: 26,
            Day,
            projection,
            openingBalance: 10_000_000);
        var repeated = service.RecordMonthlySettlement(
            ClubId,
            seasonId: 26,
            Day,
            projection,
            openingBalance: 999_999_999);

        Assert.Equivalent(first, repeated, strict: true);
        Assert.Equal(3, repeated.Entries.Count);
        Assert.Single(repeated.Entries, entry => entry.Category == ClubLedgerCategory.SponsorRevenue);
        Assert.Single(repeated.Entries, entry => entry.Category == ClubLedgerCategory.WageExpense);
        Assert.Single(repeated.Entries, entry => entry.Category == ClubLedgerCategory.FootballOperationsExpense);
        Assert.Equal(10_000_000, repeated.OpeningBalance);
        Assert.Equal(repeated.OpeningBalance + repeated.OperatingResult, repeated.Balance);
    }

    [Fact]
    public void TwelveMonthlySettlements_AllocateAnnualProjectionWithoutRoundingDrift()
    {
        var service = new ClubFinanceLedgerService(new InMemoryClubFinanceLedgerStore());
        var projection = Projection();

        for (var month = 1; month <= 12; month++)
        {
            service.RecordMonthlySettlement(
                ClubId,
                seasonId: 26,
                GameDate.FromCalendarDate(2026, month, 28),
                projection);
        }

        var snapshot = service.Get(ClubId)!;
        Assert.Equal(projection.ProjectedSponsorRevenue, snapshot.Revenue);
        Assert.Equal(
            projection.ProjectedAnnualWageSpend + projection.ProjectedFootballOperationsCost,
            snapshot.Expenses);
        Assert.Equal(36, snapshot.Entries.Count);
    }

    [Fact]
    public void HomeMatchday_UsesActualAttendanceAndFixtureKeyIdempotently()
    {
        var service = new ClubFinanceLedgerService(new InMemoryClubFinanceLedgerStore());
        var projection = Projection();

        var first = service.RecordHomeMatchday(
            ClubId,
            seasonId: 26,
            new FixtureId(901),
            Day,
            projection,
            actualAttendance: 15_000);
        var repeated = service.RecordHomeMatchday(
            ClubId,
            seasonId: 26,
            new FixtureId(901),
            Day,
            projection,
            actualAttendance: 10_000);

        var entry = Assert.Single(repeated.Entries);
        Assert.Equal(15_000L * projection.AverageTicketPrice, entry.SignedAmount);
        Assert.Equal(first.Balance, repeated.Balance);
    }

    [Fact]
    public void BoardOutcome_TracksRealizedResultAndNegativeBalance()
    {
        var healthy = new ClubFinanceLedgerService(new InMemoryClubFinanceLedgerStore());
        var healthySnapshot = healthy.RecordHomeMatchday(
            ClubId,
            seasonId: 26,
            new FixtureId(1),
            Day,
            Projection(),
            actualAttendance: 15_000,
            openingBalance: 1_000_000);
        Assert.Equal(BoardFinancialStatus.Healthy, healthySnapshot.BoardOutcome.Status);
        Assert.Equal(2, healthySnapshot.BoardOutcome.ConfidenceDelta);

        var critical = new ClubFinanceLedgerService(new InMemoryClubFinanceLedgerStore());
        var criticalSnapshot = critical.RecordMonthlySettlement(
            ClubId,
            seasonId: 26,
            Day,
            Projection(),
            openingBalance: -1_000_000_000);
        Assert.Equal(BoardFinancialStatus.Critical, criticalSnapshot.BoardOutcome.Status);
        Assert.Equal(-5, criticalSnapshot.BoardOutcome.ConfidenceDelta);
    }

    [Fact]
    public void LedgerRejectsWrongSignsAndDuplicateRestoreIds()
    {
        Assert.Throws<ArgumentException>(() => ClubLedgerEntry.Create(
            new ClubLedgerEntryId("bad-sign"),
            ClubId,
            Day,
            ClubLedgerCategory.WageExpense,
            signedAmount: 1,
            "invalid"));

        var entry = ClubLedgerEntry.Create(
            new ClubLedgerEntryId("unique"),
            ClubId,
            Day,
            ClubLedgerCategory.SponsorRevenue,
            signedAmount: 1,
            "valid");
        Assert.Throws<ArgumentException>(() => ClubFinanceLedger.Restore(
            ClubId,
            openingBalance: 0,
            [entry, entry]));
    }

    private static MvpClubEconomyProjection Projection() =>
        MvpClubEconomyProjector.Project(new MvpClubEconomyProjectionInput(
            SportiveStrength: 70,
            LeagueSize: 18,
            LeaguePosition: 4,
            WeeklyWageSpend: 300_001,
            SeasonHomeMatches: 17));
}
