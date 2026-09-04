using FootballCareerSimulator.Domain.ClubGovernance;

namespace FootballCareerSimulator.Simulation.ClubGovernance;

/// <summary>
/// Muhasebe defteri olmadan kulüp gücü, lig konumu ve mevcut taahhütlerden
/// deterministik sezon sonu ekonomik görünüm üretir.
/// </summary>
public static class MvpClubEconomyProjector
{
    public const int WeeksPerSeason = 52;

    public static MvpClubEconomyProjection Project(MvpClubEconomyProjectionInput input)
    {
        Validate(input);

        var stadiumCapacity = 12_000 + (input.SportiveStrength * 430);
        var positionBoost = input.LeaguePosition is int position && input.LeagueSize > 1
            ? (input.LeagueSize - position) * 12d / (input.LeagueSize - 1)
            : 0d;
        var occupancyPercent = Math.Clamp(
            (int)Math.Round(
                50d + (input.SportiveStrength * 0.34d) + positionBoost,
                MidpointRounding.AwayFromZero),
            48,
            96);
        var averageAttendance = (int)Math.Round(
            stadiumCapacity * occupancyPercent / 100d,
            MidpointRounding.AwayFromZero);
        var averageTicketPrice = 160 + (input.SportiveStrength * 3);
        var projectedMatchdayRevenue = checked(
            (long)averageAttendance
            * averageTicketPrice
            * input.SeasonHomeMatches);
        var projectedSponsorRevenue = checked(
            15_000_000L
            + ((long)input.SportiveStrength * input.SportiveStrength * 22_000L));
        var projectedAnnualWageSpend = checked((long)input.WeeklyWageSpend * WeeksPerSeason);
        var projectedFootballOperationsCost = checked(
            20_000_000L
            + ((long)stadiumCapacity * 450L)
            + ((long)input.SportiveStrength * 250_000L));
        var projectedOperatingCosts = checked(
            projectedFootballOperationsCost
            + projectedAnnualWageSpend);
        var projectedOperatingRevenue = checked(
            projectedMatchdayRevenue + projectedSponsorRevenue);

        return new MvpClubEconomyProjection(
            stadiumCapacity,
            occupancyPercent,
            averageAttendance,
            averageTicketPrice,
            projectedMatchdayRevenue,
            projectedSponsorRevenue,
            projectedAnnualWageSpend,
            projectedFootballOperationsCost,
            projectedOperatingCosts,
            projectedOperatingRevenue,
            projectedOperatingRevenue - projectedOperatingCosts);
    }

    private static void Validate(MvpClubEconomyProjectionInput input)
    {
        if (input.SportiveStrength is < Club.MinSportiveStrength or > Club.MaxSportiveStrength)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Sportive strength is out of range.");
        }

        if (input.LeagueSize < 1
            || input.LeaguePosition is int position && (position < 1 || position > input.LeagueSize)
            || input.WeeklyWageSpend < 0
            || input.SeasonHomeMatches < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Economy projection input is invalid.");
        }
    }
}

public sealed record MvpClubEconomyProjectionInput(
    int SportiveStrength,
    int LeagueSize,
    int? LeaguePosition,
    int WeeklyWageSpend,
    int SeasonHomeMatches);

public sealed record MvpClubEconomyProjection(
    int StadiumCapacity,
    int AttendancePercent,
    int ProjectedAverageAttendance,
    int AverageTicketPrice,
    long ProjectedMatchdayRevenue,
    long ProjectedSponsorRevenue,
    long ProjectedAnnualWageSpend,
    long ProjectedFootballOperationsCost,
    long ProjectedOperatingCosts,
    long ProjectedOperatingRevenue,
    long ProjectedOperatingBalance);
