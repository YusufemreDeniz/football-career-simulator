using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Simulation.TeamPreparation;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class TacticPlanTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-tactic-plan",
        Guid.NewGuid().ToString("N"));

    public TacticPlanTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void EnsureDefault_CreatesBalanced442()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 1);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var teamPrep = TeamPreparationModule.Create(competition.Store, manager.Store);

        var plan = teamPrep.TacticPlans.EnsureDefault(new ClubId(1), Day);

        Assert.Equal(Formation.F442, plan.Formation);
        Assert.Equal(TacticalApproach.Balanced, plan.Approach);
        Assert.Equal("4-4-2", teamPrep.TacticQueries.GetManagedClubPlan().FormationName);
        Assert.Equal("Dengeli", teamPrep.TacticQueries.GetManagedClubPlan().ApproachName);
    }

    [Fact]
    public void SetApproach_UpdatesPlanAndMatchModifier()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 2);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var teamPrep = TeamPreparationModule.Create(competition.Store, manager.Store);

        var attacking = teamPrep.TacticPlans.SetApproach(
            new ClubId(1),
            TacticalApproach.Attacking,
            Day);

        Assert.Equal(2, MvpTacticMatchModifier.ComputeApproachModifier(attacking));
        Assert.Equal("Hücum", teamPrep.TacticQueries.GetManagedClubPlan().ApproachName);

        var defensive = teamPrep.TacticPlans.SetApproach(
            new ClubId(1),
            TacticalApproach.Defensive,
            Day.AddDays(1));
        Assert.Equal(1, MvpTacticMatchModifier.ComputeApproachModifier(defensive));
    }

    [Fact]
    public void SaveLoad_PreservesTacticPlanAtSchemaV18()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 3);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var teamPrep = TeamPreparationModule.Create(competition.Store, manager.Store);

        teamPrep.TacticPlans.SetFormation(new ClubId(1), Formation.F433, Day);
        teamPrep.TacticPlans.SetApproach(new ClubId(1), TacticalApproach.Attacking, Day);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "tactics.db");
        persistence.Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            clubs.Store.Registry,
            manager.Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<WeeklyTrainingPlan>(),
            Array.Empty<PlayerPhysicalState>(),
            Array.Empty<Domain.PlayerCareer.PlayerCareer>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            teamPrep.TacticPlanStore.Plans,
            Array.Empty<Domain.Transfer.TransferNeed>(),
            Array.Empty<Domain.Transfer.ShortlistEntry>(),
            Array.Empty<Domain.Transfer.TransferTarget>(),
            Array.Empty<Domain.Transfer.TransferProcess>(),
            Array.Empty<Domain.Transfer.ClubOffer>(),
            Array.Empty<Domain.Transfer.PlayerContractProposal>(),
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>());

        var loaded = persistence.Load(path);
        Assert.Equal(32, loaded.SchemaVersion);
        Assert.Single(loaded.TacticPlans);
        Assert.Equal(Formation.F433, loaded.TacticPlans[0].Formation);
        Assert.Equal(TacticalApproach.Attacking, loaded.TacticPlans[0].Approach);
    }
}
