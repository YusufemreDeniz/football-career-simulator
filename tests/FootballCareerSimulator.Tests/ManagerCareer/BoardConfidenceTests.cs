using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.ManagerCareerBoard;

public sealed class BoardConfidenceTests : IDisposable
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-board-confidence",
        Guid.NewGuid().ToString("N"));

    public BoardConfidenceTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void StartNewCareer_SetsExpectationFromClubStrength()
    {
        var career = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "TD",
            new ClubId(1),
            PreseasonStart,
            clubSportiveStrength: 82);

        Assert.Equal(SeasonExpectationTier.TitleChallenge, career.ActiveEmployment!.SeasonExpectation);
        Assert.Equal(BoardConfidence.DefaultInitialValue, career.ActiveEmployment.BoardConfidence.Value);
        Assert.Equal(EmploymentRiskBand.Stable, career.ActiveEmployment.RiskBand);
    }

    [Fact]
    public void ApplyMatchBoardAssessment_IsIdempotentForSameFixture()
    {
        var career = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "TD",
            new ClubId(1),
            PreseasonStart,
            clubSportiveStrength: 50);

        var first = career.ApplyMatchBoardAssessment(
            new FixtureId(10),
            MatchOutcomeForManagedClub.Win,
            leaguePosition: 8,
            leagueSize: 20);

        Assert.True(first.WasApplied);
        Assert.Equal(60, first.BoardConfidence); // 55 + WinOnTrack(+5)

        var second = first.Career.ApplyMatchBoardAssessment(
            new FixtureId(10),
            MatchOutcomeForManagedClub.Loss,
            leaguePosition: 15,
            leagueSize: 20);

        Assert.True(second.WasAlreadyApplied);
        Assert.Equal(60, second.BoardConfidence);
    }

    [Fact]
    public void PlayManagedMatch_UpdatesBoardConfidence()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 42);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(
            PreseasonStart,
            startingClubId: 1,
            clubSportiveStrength: 50);
        var selectionStore = new InMemoryMatchSelectionStore();
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore);
        var teamPrep = TeamPreparationModule.Create(competition.Store, manager.Store, selectionStore);

        const long seasonId = 1;
        competition.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }

        competition.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                FirstMatchday.DayNumber,
                StartingFixtureId: 1));

        var managedFixture = competition.Queries.GetSeasonFixtures(seasonId)
            .First(fixture => fixture.HomeClubId == 1 || fixture.AwayClubId == 1);

        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), managedFixture.FixtureId, ClubId: 1));

        competition.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(
                Guid.NewGuid(),
                seasonId,
                managedFixture.FixtureId,
                FirstMatchday.DayNumber));

        var after = manager.Queries.GetCareer();
        Assert.NotNull(after.BoardConfidence);
        Assert.False(string.IsNullOrWhiteSpace(after.LastAssessmentReasonCode));
        Assert.Equal(managedFixture.FixtureId, after.LastAssessedFixtureId);
        Assert.False(string.IsNullOrWhiteSpace(after.EmploymentRiskBand));
    }

    [Fact]
    public void SaveLoad_PreservesBoardConfidenceFields()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 3);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(
            PreseasonStart,
            clubSportiveStrength: 70);
        var assessed = manager.Store.Career.ApplyMatchBoardAssessment(
            new FixtureId(3),
            MatchOutcomeForManagedClub.Loss,
            leaguePosition: 18,
            leagueSize: 20);
        manager.Store.Replace(assessed.Career);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "board.db");
        persistence.Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            clubs.Store.Registry,
            manager.Store.Career,
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
        Assert.Equal(46, loaded.SchemaVersion);
        Assert.Equal(
            assessed.Career.ActiveEmployment!.BoardConfidence.Value,
            loaded.ManagerCareer.ActiveEmployment!.BoardConfidence.Value);
        Assert.Equal(
            assessed.Career.ActiveEmployment.LastAssessmentReasonCode,
            loaded.ManagerCareer.ActiveEmployment.LastAssessmentReasonCode);
        Assert.Equal(
            SeasonExpectationTier.UpperHalf,
            loaded.ManagerCareer.ActiveEmployment.SeasonExpectation);
    }
}
