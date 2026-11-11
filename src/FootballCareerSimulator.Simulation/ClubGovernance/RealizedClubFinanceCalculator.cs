using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.ClubGovernance;

/// <summary>
/// Projeksiyonu gerçekleşmiş aylık ve maç-günü hareketlerine deterministik biçimde böler.
/// </summary>
public static class RealizedClubFinanceCalculator
{
    public const int MonthsPerSeason = 12;

    public static IReadOnlyList<ClubLedgerEntry> MonthlySettlement(
        ClubId clubId,
        long seasonId,
        GameDate day,
        MvpClubEconomyProjection projection)
    {
        ValidateSeason(seasonId);
        ArgumentNullException.ThrowIfNull(projection);

        var period = $"{day.Year:D4}-{day.Month:D2}";
        var prefix = $"season:{seasonId}:club:{clubId.Value}:month:{period}";
        return
        [
            ClubLedgerEntry.Create(
                new ClubLedgerEntryId($"{prefix}:sponsor"),
                clubId,
                day,
                ClubLedgerCategory.SponsorRevenue,
                AllocateMonthly(projection.ProjectedSponsorRevenue, day.Month),
                $"{period} sponsor payı"),
            ClubLedgerEntry.Create(
                new ClubLedgerEntryId($"{prefix}:wages"),
                clubId,
                day,
                ClubLedgerCategory.WageExpense,
                -AllocateMonthly(projection.ProjectedAnnualWageSpend, day.Month),
                $"{period} oyuncu maaşları"),
            ClubLedgerEntry.Create(
                new ClubLedgerEntryId($"{prefix}:operations"),
                clubId,
                day,
                ClubLedgerCategory.FootballOperationsExpense,
                -AllocateMonthly(projection.ProjectedFootballOperationsCost, day.Month),
                $"{period} futbol operasyonları"),
        ];
    }

    public static ClubLedgerEntry HomeMatchday(
        ClubId clubId,
        long seasonId,
        FixtureId fixtureId,
        GameDate day,
        MvpClubEconomyProjection projection,
        int? actualAttendance = null)
    {
        ValidateSeason(seasonId);
        ArgumentNullException.ThrowIfNull(projection);
        var attendance = actualAttendance ?? projection.ProjectedAverageAttendance;
        if (attendance < 0 || attendance > projection.StadiumCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(actualAttendance));
        }

        var revenue = checked((long)attendance * projection.AverageTicketPrice);
        if (revenue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actualAttendance), "Matchday revenue must be positive.");
        }

        return ClubLedgerEntry.Create(
            new ClubLedgerEntryId($"season:{seasonId}:club:{clubId.Value}:fixture:{fixtureId.Value}:tickets"),
            clubId,
            day,
            ClubLedgerCategory.MatchdayTicketRevenue,
            revenue,
            $"{fixtureId.Value} numaralı iç saha maçı bilet geliri");
    }

    private static long AllocateMonthly(long annualAmount, int month)
    {
        if (annualAmount < MonthsPerSeason)
        {
            throw new ArgumentOutOfRangeException(nameof(annualAmount));
        }

        var baseAmount = annualAmount / MonthsPerSeason;
        var remainder = annualAmount % MonthsPerSeason;
        return baseAmount + (month <= remainder ? 1 : 0);
    }

    private static void ValidateSeason(long seasonId)
    {
        if (seasonId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seasonId));
        }
    }
}
