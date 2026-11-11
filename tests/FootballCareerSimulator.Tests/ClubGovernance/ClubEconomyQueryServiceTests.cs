using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ClubGovernance.Queries;
using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.ContractRegistration.Infrastructure;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.ClubGovernance;

namespace FootballCareerSimulator.Tests.ClubGovernance;

public sealed class ClubEconomyQueryServiceTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void GetClub_CombinesWagesAttendanceRevenueAndThreeBoardObjectivesDeterministically()
    {
        var context = CreateContext(weeklyWages: [200_000, 100_000]);

        var snapshot = context.Service.GetClub(new ClubId(1), Day);
        var repeated = context.Service.GetClub(new ClubId(1), Day);

        Assert.Equivalent(snapshot, repeated, strict: true);
        Assert.Equal(300_000, snapshot.CommittedWeeklyWage);
        Assert.Equal(175_000, snapshot.WeeklyWageHeadroom);
        Assert.Equal(63, snapshot.WageUtilizationPercent);
        Assert.Equal("TRY", snapshot.CurrencyCode);
        Assert.InRange(snapshot.ProjectedAverageAttendance, 1, snapshot.StadiumCapacity);
        Assert.True(snapshot.ProjectedSponsorRevenue > 0);
        Assert.True(snapshot.ProjectedMatchdayRevenue > 0);
        Assert.Equal(
            snapshot.ProjectedOperatingRevenue - snapshot.ProjectedOperatingCosts,
            snapshot.ProjectedOperatingBalance);
        Assert.Equal(3, snapshot.BoardObjectives.Count);
        Assert.Equal(
            BoardObjectiveStatus.OnTrack,
            snapshot.BoardObjectives.Single(objective => objective.Code == "SPORTING_POSITION").Status);
        Assert.Equal(
            BoardObjectiveStatus.OnTrack,
            snapshot.BoardObjectives.Single(objective => objective.Code == "WAGE_DISCIPLINE").Status);
    }

    [Fact]
    public void WageObjective_MovesOffTrackWhenExistingCommitmentsExceedLimit()
    {
        var context = CreateContext(weeklyWages: [12_000_000]);

        var snapshot = context.Service.GetManagedClub(Day)!;
        var wageObjective = snapshot.BoardObjectives.Single(item => item.Code == "WAGE_DISCIPLINE");
        var operatingObjective = snapshot.BoardObjectives.Single(item => item.Code == "OPERATING_BALANCE");

        Assert.True(snapshot.WeeklyWageHeadroom < 0);
        Assert.True(snapshot.WageUtilizationPercent > 100);
        Assert.True(snapshot.ProjectedOperatingBalance < 0);
        Assert.Equal(BoardObjectiveStatus.OffTrack, wageObjective.Status);
        Assert.Equal(BoardObjectiveStatus.OffTrack, operatingObjective.Status);
    }

    [Fact]
    public void CompletedSeason_FinalizesSportingStatus_ButKeepsLiveProjectionsNonterminal()
    {
        var context = CreateContext(weeklyWages: [200_000], completeSeason: true);

        var snapshot = context.Service.GetManagedClub(Day)!;

        Assert.Equal(
            BoardObjectiveStatus.Achieved,
            snapshot.BoardObjectives.Single(item => item.Code == "SPORTING_POSITION").Status);
        Assert.Equal(
            BoardObjectiveStatus.OnTrack,
            snapshot.BoardObjectives.Single(item => item.Code == "WAGE_DISCIPLINE").Status);
        Assert.Equal(
            BoardObjectiveStatus.OnTrack,
            snapshot.BoardObjectives.Single(item => item.Code == "OPERATING_BALANCE").Status);
    }

    [Fact]
    public void CumulativeTransferSpend_RemainsVisibleButIsNotRechargedAsAnnualOperatingCost()
    {
        var baseline = CreateContext(weeklyWages: [200_000]).Service.GetManagedClub(Day)!;
        var withHistoricalSpend = CreateContext(
                weeklyWages: [200_000],
                transferSpend: 5_000_000)
            .Service.GetManagedClub(Day)!;

        Assert.Equal(0, baseline.SpentTransferFunds);
        Assert.Equal(5_000_000, withHistoricalSpend.SpentTransferFunds);
        Assert.Equal(baseline.ProjectedOperatingCosts, withHistoricalSpend.ProjectedOperatingCosts);
        Assert.Equal(baseline.ProjectedOperatingBalance, withHistoricalSpend.ProjectedOperatingBalance);
    }

    [Fact]
    public void Projector_LeaguePositionRaisesAttendance_AndCostsReactToWages()
    {
        var leading = MvpClubEconomyProjector.Project(new MvpClubEconomyProjectionInput(
            SportiveStrength: 70,
            LeagueSize: 18,
            LeaguePosition: 1,
            WeeklyWageSpend: 300_000,
            SeasonHomeMatches: 17));
        var bottom = MvpClubEconomyProjector.Project(new MvpClubEconomyProjectionInput(
            SportiveStrength: 70,
            LeagueSize: 18,
            LeaguePosition: 18,
            WeeklyWageSpend: 1_000_000,
            SeasonHomeMatches: 17));

        Assert.True(leading.ProjectedAverageAttendance > bottom.ProjectedAverageAttendance);
        Assert.True(bottom.ProjectedAnnualWageSpend > leading.ProjectedAnnualWageSpend);
        Assert.True(bottom.ProjectedOperatingCosts > leading.ProjectedOperatingCosts);
    }

    private static EconomyContext CreateContext(
        IReadOnlyList<int> weeklyWages,
        bool completeSeason = false,
        int transferSpend = 0)
    {
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var club = clubs.Store.Registry.GetClubOrThrow(new ClubId(1));
        if (transferSpend > 0)
        {
            club = club.ReserveTransferFunds(transferSpend).ApplyReservedTransferSpend(transferSpend);
            clubs.Store.Replace(clubs.Store.Registry.WithClub(club));
        }
        var manager = ManagerCareerModule.CreateNewCareer(
            Day,
            startingClubId: club.Id.Value,
            clubSportiveStrength: club.SportiveStrength);
        var league = new LeagueCompetition(new CompetitionId(1));
        var season = league.CreateSeason(new SeasonId(1), Day);
        foreach (var participant in clubs.Store.Registry.Clubs)
        {
            season.RegisterParticipant(participant.Id);
        }

        league.StartSeason(season.SeasonId, Day);
        league.PlanLeagueFixtures(
            season.SeasonId,
            Day.AddDays(7),
            new FixtureId(1));
        var managedFixture = season.Fixtures.First(fixture =>
            fixture.HomeClubId == club.Id || fixture.AwayClubId == club.Id);
        var managedWin = managedFixture.HomeClubId == club.Id
            ? new MatchScore(2, 0)
            : new MatchScore(0, 2);
        league.AcceptFixtureResult(
            season.SeasonId,
            managedFixture.Id,
            managedWin,
            managedFixture.ScheduledDate);
        if (completeSeason)
        {
            foreach (var fixture in season.Fixtures
                         .Where(candidate => candidate.Status == FixtureStatus.Planned)
                         .ToArray())
            {
                var score = fixture.HomeClubId == club.Id
                    ? new MatchScore(2, 0)
                    : fixture.AwayClubId == club.Id
                        ? new MatchScore(0, 2)
                        : new MatchScore(0, 0);
                league.AcceptFixtureResult(
                    season.SeasonId,
                    fixture.Id,
                    score,
                    fixture.ScheduledDate);
            }

            league.CompleteSeason(
                season.SeasonId,
                season.Fixtures.Max(fixture => fixture.ScheduledDate));
        }

        var contracts = new InMemoryContractStore();
        for (var index = 0; index < weeklyWages.Count; index++)
        {
            contracts.Upsert(PlayerContract.Activate(
                PlayerId.FromClubSlot(club.Id.Value, index),
                club.Id,
                Day,
                Day.AddDays(365),
                weeklyWages[index]));
        }

        var competition = new InMemoryLeagueCompetitionStore(league);
        var service = new ClubEconomyQueryService(
            clubs.Store,
            contracts,
            competition,
            manager.Store);
        return new EconomyContext(service);
    }

    private sealed record EconomyContext(ClubEconomyQueryService Service);
}
