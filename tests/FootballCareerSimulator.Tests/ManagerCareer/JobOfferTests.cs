using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Commands;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.ManagerCareerBoard;

public sealed class JobOfferTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 10);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-job-offer",
        Guid.NewGuid().ToString("N"));

    public JobOfferTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static ManagerCareer UnemployedCareer()
    {
        var career = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "TD",
            new ClubId(1),
            Day,
            clubSportiveStrength: 50,
            initialBoardConfidence: 32);

        career = career.ApplyMatchBoardAssessment(
            new FixtureId(1),
            MatchOutcomeForManagedClub.Loss,
            leaguePosition: 20,
            leagueSize: 20).Career;

        return career.DismissDueToBoardConfidence(new FixtureId(1), Day).Career;
    }

    [Fact]
    public void AcceptPendingJobOffer_ReturnsToEmployed()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 7);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var module = ManagerCareerModule.CreateForCareer(
            Day,
            clubs.Store,
            world.TimelineStore,
            clubSportiveStrength: 50);

        module.Store.Replace(UnemployedCareer());

        var generated = module.GenerateJobOffer!.Handle(
            new GenerateUnemployedJobOfferCommand(Guid.NewGuid()));
        Assert.True(generated.Succeeded);
        Assert.NotNull(generated.ClubId);

        var accepted = module.AcceptJobOffer!.Handle(
            new AcceptPendingJobOfferCommand(Guid.NewGuid()));

        Assert.True(accepted.Succeeded);
        Assert.Equal(ManagerEmploymentStatus.Employed, module.Store.Career.EmploymentStatus);
        Assert.NotNull(module.Store.Career.ActiveEmployment);
        Assert.Null(module.Store.Career.PendingJobOffer);
        Assert.Null(module.Store.Career.DismissedAt);
        Assert.Equal(accepted.ClubId, module.Store.Career.ActiveEmployment!.ClubId.Value);
    }

    [Fact]
    public void GenerateJobOffer_WhileEmployed_Fails()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 7);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var module = ManagerCareerModule.CreateForCareer(
            Day,
            clubs.Store,
            world.TimelineStore);

        Assert.Throws<ManagerCareerInvariantViolationException>(() =>
            module.GenerateJobOffer!.Handle(new GenerateUnemployedJobOfferCommand(Guid.NewGuid())));
    }

    [Fact]
    public void SaveLoad_PreservesPendingJobOffer()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 3);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var module = ManagerCareerModule.CreateForCareer(
            Day,
            clubs.Store,
            world.TimelineStore);
        module.Store.Replace(UnemployedCareer());
        module.GenerateJobOffer!.Handle(new GenerateUnemployedJobOfferCommand(Guid.NewGuid()));

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "offer.db");
        persistence.Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            clubs.Store.Registry,
            module.Store.Career,
            Array.Empty<Domain.TeamPreparation.MatchSelection>());

        var loaded = persistence.Load(path);
        Assert.Equal(10, loaded.SchemaVersion);
        Assert.NotNull(loaded.ManagerCareer.PendingJobOffer);
        Assert.Equal(
            module.Store.Career.PendingJobOffer!.ClubId.Value,
            loaded.ManagerCareer.PendingJobOffer!.ClubId.Value);
        Assert.Equal(JobOfferStatus.Offered, loaded.ManagerCareer.PendingJobOffer.Status);
    }
}
