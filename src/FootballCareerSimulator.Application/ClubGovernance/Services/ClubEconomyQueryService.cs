using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ClubGovernance.Queries;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.ClubGovernance;

namespace FootballCareerSimulator.Application.ClubGovernance.Services;

/// <summary>
/// Mevcut sözleşme ve sportif durumdan, ledger gerektirmeyen kulüp ekonomi ve yönetim hedefi özeti.
/// </summary>
public sealed class ClubEconomyQueryService
{
    public const int WageDisciplineTargetPercent = 90;
    public const string CurrencyCode = "TRY";

    private readonly IClubRegistryStore _clubs;
    private readonly IContractStore _contracts;
    private readonly ILeagueCompetitionStore _competition;
    private readonly IManagerCareerStore _managerCareer;

    public ClubEconomyQueryService(
        IClubRegistryStore clubs,
        IContractStore contracts,
        ILeagueCompetitionStore competition,
        IManagerCareerStore managerCareer)
    {
        _clubs = clubs ?? throw new ArgumentNullException(nameof(clubs));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _competition = competition ?? throw new ArgumentNullException(nameof(competition));
        _managerCareer = managerCareer ?? throw new ArgumentNullException(nameof(managerCareer));
    }

    public ClubEconomyReadModel? GetManagedClub(GameDate day)
    {
        var employment = _managerCareer.Career.ActiveEmployment;
        return employment is null ? null : GetClub(employment.ClubId, day);
    }

    public ClubEconomyReadModel GetClub(ClubId clubId, GameDate day)
    {
        var club = _clubs.Registry.GetClubOrThrow(clubId);
        var season = _competition.League.CurrentSeason
            ?? _competition.League.Seasons.OrderBy(candidate => candidate.SeasonId.Value).LastOrDefault();
        var leagueSize = season?.Participants.Count > 0
            ? season.Participants.Count
            : CompetitionMvpConstraints.LeagueTeamCount;
        var position = season?.Standings.Entries
            .Select((entry, index) => (entry.ClubId, Position: index + 1))
            .Where(entry => entry.ClubId == clubId)
            .Select(entry => (int?)entry.Position)
            .SingleOrDefault();
        var playedMatches = season?.Fixtures.Count(fixture =>
            fixture.Status == FixtureStatus.ResultAccepted
            && (fixture.HomeClubId == clubId || fixture.AwayClubId == clubId)) ?? 0;
        var homeMatches = season?.Fixtures.Count(fixture => fixture.HomeClubId == clubId) ?? 0;
        if (homeMatches == 0)
        {
            homeMatches = Math.Max(1, leagueSize - 1);
        }

        var committedWeeklyWage = _contracts.GetForClub(clubId)
            .Where(contract => contract.IsActiveOn(day))
            .Sum(contract => contract.WeeklyWage);
        var totalWeeklyWage = checked(committedWeeklyWage + club.ReservedWeeklyWage);
        var wageHeadroom = club.AvailableWeeklyWageHeadroom(committedWeeklyWage);
        var wageUtilization = club.WageBudgetLimit == 0
            ? totalWeeklyWage == 0 ? 0 : 100
            : Math.Max(
                0,
                (int)Math.Round(
                    totalWeeklyWage * 100d / club.WageBudgetLimit,
                    MidpointRounding.AwayFromZero));
        var projection = MvpClubEconomyProjector.Project(new MvpClubEconomyProjectionInput(
            club.SportiveStrength,
            leagueSize,
            position,
            totalWeeklyWage,
            homeMatches));
        var employment = _managerCareer.Career.ActiveEmployment;
        var isManagedClub = employment?.ClubId == clubId;
        var expectation = isManagedClub
            ? employment!.SeasonExpectation
            : SeasonExpectation.FromSportiveStrength(club.SportiveStrength);
        var seasonFinished = season?.Status is SeasonStatus.Completed or SeasonStatus.Archived;

        return new ClubEconomyReadModel(
            club.Id.Value,
            club.DisplayName,
            season?.SeasonId.Value,
            position,
            leagueSize,
            playedMatches,
            isManagedClub ? employment!.BoardConfidence.Value : null,
            expectation.ToString(),
            CurrencyCode,
            club.TransferBudgetLimit,
            club.ReservedTransferFunds,
            club.SpentTransferFunds,
            club.AvailableTransferFunds,
            club.WageBudgetLimit,
            committedWeeklyWage,
            club.ReservedWeeklyWage,
            wageHeadroom,
            wageUtilization,
            projection.StadiumCapacity,
            projection.AttendancePercent,
            projection.ProjectedAverageAttendance,
            projection.AverageTicketPrice,
            projection.ProjectedMatchdayRevenue,
            projection.ProjectedSponsorRevenue,
            projection.ProjectedAnnualWageSpend,
            projection.ProjectedFootballOperationsCost,
            projection.ProjectedOperatingCosts,
            projection.ProjectedOperatingRevenue,
            projection.ProjectedOperatingBalance,
            BuildObjectives(
                expectation,
                position,
                leagueSize,
                playedMatches,
                wageUtilization,
                projection,
                seasonFinished));
    }

    private static IReadOnlyList<BoardObjectiveReadModel> BuildObjectives(
        SeasonExpectationTier expectation,
        int? position,
        int leagueSize,
        int playedMatches,
        int wageUtilization,
        MvpClubEconomyProjection projection,
        bool seasonFinished)
    {
        var targetPosition = TargetPosition(expectation, leagueSize);
        var sportingStatus = SportingStatus(
            position,
            targetPosition,
            playedMatches,
            seasonFinished);
        var sportingProgress = position is not int currentPosition
            ? 0
            : Math.Clamp(
                (int)Math.Round(
                    (leagueSize - currentPosition + 1) * 100d
                    / Math.Max(1, leagueSize - targetPosition + 1),
                    MidpointRounding.AwayFromZero),
                0,
                100);
        // Wage/operating degerleri gerceklesmis sezon ledger'i degil, canli projeksiyondur;
        // bu nedenle sezon bitince terminal Achieved/Failed olarak dondurulmaz.
        var wageStatus = wageUtilization <= WageDisciplineTargetPercent
            ? BoardObjectiveStatus.OnTrack
            : wageUtilization <= 100
                ? BoardObjectiveStatus.AtRisk
                : BoardObjectiveStatus.OffTrack;
        var wageProgress = Math.Clamp(
            100 - Math.Max(0, wageUtilization - WageDisciplineTargetPercent) * 5,
            0,
            100);
        var operatingStatus = OperatingStatus(projection);
        var operatingProgress = projection.ProjectedOperatingRevenue == 0
            ? 0
            : Math.Clamp(
                (int)Math.Round(
                    projection.ProjectedOperatingBalance * 100d
                    / projection.ProjectedOperatingRevenue
                    + 100d,
                    MidpointRounding.AwayFromZero),
                0,
                100);

        return
        [
            new BoardObjectiveReadModel(
                "SPORTING_POSITION",
                "Lig hedefi",
                $"İlk {targetPosition}",
                position is int value ? $"{value}. sıra" : "Henüz sıralama yok",
                sportingProgress,
                sportingStatus),
            new BoardObjectiveReadModel(
                "WAGE_DISCIPLINE",
                "Maaş disiplini",
                $"Limitin en fazla %{WageDisciplineTargetPercent}'i",
                $"%{wageUtilization}",
                wageProgress,
                wageStatus),
            new BoardObjectiveReadModel(
                "OPERATING_BALANCE",
                "Sürdürülebilir faaliyet",
                "Sezon projeksiyonunu ekside kapatmama",
                projection.ProjectedOperatingBalance >= 0 ? "Fazla" : "Açık",
                operatingProgress,
                operatingStatus),
        ];
    }

    private static int TargetPosition(SeasonExpectationTier expectation, int leagueSize)
    {
        var half = (leagueSize + 1) / 2;
        return expectation switch
        {
            SeasonExpectationTier.TitleChallenge => Math.Min(3, leagueSize),
            SeasonExpectationTier.UpperHalf => half,
            SeasonExpectationTier.MidTable => Math.Max(1, leagueSize - 4),
            SeasonExpectationTier.LowerHalf => Math.Max(1, leagueSize - 2),
            SeasonExpectationTier.Survival => Math.Max(1, leagueSize - 1),
            _ => leagueSize,
        };
    }

    private static BoardObjectiveStatus SportingStatus(
        int? position,
        int targetPosition,
        int playedMatches,
        bool seasonFinished)
    {
        if (position is null || playedMatches == 0)
        {
            return BoardObjectiveStatus.NotStarted;
        }

        if (seasonFinished)
        {
            return position <= targetPosition
                ? BoardObjectiveStatus.Achieved
                : BoardObjectiveStatus.Failed;
        }

        return position <= targetPosition
            ? BoardObjectiveStatus.OnTrack
            : position <= targetPosition + 2
                ? BoardObjectiveStatus.AtRisk
                : BoardObjectiveStatus.OffTrack;
    }

    private static BoardObjectiveStatus OperatingStatus(
        MvpClubEconomyProjection projection)
    {
        if (projection.ProjectedOperatingBalance >= 0)
        {
            return BoardObjectiveStatus.OnTrack;
        }

        var manageableDeficit = projection.ProjectedOperatingRevenue / 10;
        return projection.ProjectedOperatingBalance >= -manageableDeficit
            ? BoardObjectiveStatus.AtRisk
            : BoardObjectiveStatus.OffTrack;
    }
}
