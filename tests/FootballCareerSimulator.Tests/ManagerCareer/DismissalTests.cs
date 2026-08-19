using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.ManagerCareerBoard;

public sealed class DismissalTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-dismissal",
        Guid.NewGuid().ToString("N"));

    public DismissalTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static ManagerCareer CareerAtCritical()
    {
        var career = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "TD",
            new ClubId(1),
            Day,
            clubSportiveStrength: 50,
            initialBoardConfidence: 32);

        // 32 + LossBehindExpectation(-6) => 26 Critical
        return career.ApplyMatchBoardAssessment(
            new FixtureId(1),
            MatchOutcomeForManagedClub.Loss,
            leaguePosition: 20,
            leagueSize: 20).Career;
    }

    [Fact]
    public void DismissDueToBoardConfidence_WhenCritical_BecomesUnemployed()
    {
        var career = CareerAtCritical();
        Assert.Equal(EmploymentRiskBand.Critical, career.ActiveEmployment!.RiskBand);

        var dismissal = career.DismissDueToBoardConfidence(new FixtureId(1), Day);

        Assert.True(dismissal.WasApplied);
        Assert.Equal(ManagerEmploymentStatus.Unemployed, dismissal.Career.EmploymentStatus);
        Assert.Null(dismissal.Career.ActiveEmployment);
        Assert.Equal(EmploymentEndReason.Dismissed, dismissal.Career.TerminationReason);
        Assert.NotNull(dismissal.Career.LastClubId);
        Assert.Equal(1L, dismissal.Career.LastClubId.Value.Value);
        Assert.NotNull(dismissal.Career.DismissedDueToFixtureId);
        Assert.Equal(1L, dismissal.Career.DismissedDueToFixtureId.Value.Value);
    }

    [Fact]
    public void DismissDueToBoardConfidence_SameFixture_IsIdempotent()
    {
        var career = CareerAtCritical();
        var first = career.DismissDueToBoardConfidence(new FixtureId(1), Day);
        var second = first.Career.DismissDueToBoardConfidence(new FixtureId(1), Day);

        Assert.True(first.WasApplied);
        Assert.True(second.WasAlreadyApplied);
        Assert.Equal(ManagerEmploymentStatus.Unemployed, second.Career.EmploymentStatus);
    }

    [Fact]
    public void Dismiss_WhenNotCritical_Throws()
    {
        var career = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "TD",
            new ClubId(1),
            Day,
            clubSportiveStrength: 50);

        Assert.Throws<ManagerCareerInvariantViolationException>(() =>
            career.DismissDueToBoardConfidence(new FixtureId(9), Day));
    }

    [Fact]
    public void SaveLoad_PreservesUnemployment()
    {
        var dismissed = CareerAtCritical()
            .DismissDueToBoardConfidence(new FixtureId(1), Day)
            .Career;

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "unemployed.db");
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        persistence.Save(
            path,
            WorldTimeline.Create(Day, rootSeed: 1, rngVersion: "1"),
            new LeagueCompetition(new CompetitionId(1)),
            clubs.Store.Registry,
            dismissed,
            Array.Empty<Domain.TeamPreparation.MatchSelection>(),
            Array.Empty<Domain.TrainingPhysicalState.WeeklyTrainingPlan>(),
            Array.Empty<Domain.TrainingPhysicalState.PlayerPhysicalState>(),
            Array.Empty<Domain.PlayerCareer.PlayerCareer>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<Domain.TeamPreparation.ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<Domain.TeamPreparation.TacticPlan>(),
            Array.Empty<Domain.Transfer.TransferNeed>(),
            Array.Empty<Domain.Transfer.ShortlistEntry>(),
            Array.Empty<Domain.Transfer.TransferTarget>(),
            Array.Empty<Domain.Transfer.TransferProcess>(),
            Array.Empty<Domain.Transfer.ClubOffer>(),
            Array.Empty<Domain.Transfer.PlayerContractProposal>(),
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>());

        var loaded = persistence.Load(path);
        Assert.Equal(44, loaded.SchemaVersion);
        Assert.Equal(ManagerEmploymentStatus.Unemployed, loaded.ManagerCareer.EmploymentStatus);
        Assert.Null(loaded.ManagerCareer.ActiveEmployment);
        Assert.NotNull(loaded.ManagerCareer.LastClubId);
        Assert.Equal(1L, loaded.ManagerCareer.LastClubId.Value.Value);
        Assert.Equal(EmploymentEndReason.Dismissed, loaded.ManagerCareer.TerminationReason);
    }
}
