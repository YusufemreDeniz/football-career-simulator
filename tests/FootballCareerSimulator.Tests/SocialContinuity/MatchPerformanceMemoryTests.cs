using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class MatchPerformanceMemoryTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-match-performance-memory",
        Guid.NewGuid().ToString("N"));

    public MatchPerformanceMemoryTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void RecordBlowout_WhenGoalDifferenceBelowThreshold_CreatesNothing()
    {
        var social = SocialContinuityModule.Create();
        var created = social.MatchPerformanceMemory.RecordBlowoutIfApplicable(
            new FixtureId(1),
            managedGoals: 2,
            opponentGoals: 0,
            new ManagerId(1),
            [new PlayerId(10)],
            Day);

        Assert.Equal(0, created);
        Assert.Empty(social.MemoryStore.Memories);
    }

    [Fact]
    public void RecordBlowout_HeavyWin_CreatesPositiveMemoriesForManagerAndStarters_Idempotently()
    {
        var social = SocialContinuityModule.Create();
        var starters = new[] { new PlayerId(10), new PlayerId(11) };

        var created = social.MatchPerformanceMemory.RecordBlowoutIfApplicable(
            new FixtureId(7),
            managedGoals: 4,
            opponentGoals: 1,
            new ManagerId(1),
            starters,
            Day);

        Assert.Equal(3, created);
        Assert.Equal(0, social.MatchPerformanceMemory.RecordBlowoutIfApplicable(
            new FixtureId(7),
            managedGoals: 4,
            opponentGoals: 1,
            new ManagerId(1),
            starters,
            Day));

        Assert.All(
            social.MemoryStore.Memories,
            m =>
            {
                Assert.Equal(MemoryCategory.MatchPerformance, m.Category);
                Assert.Equal(MemoryValence.Positive, m.Valence);
                Assert.Equal(MemorySubjectKind.Fixture, m.SubjectKind);
                Assert.Equal(7, m.SubjectId);
                Assert.Equal(MemoryRecord.MatchBlowoutRuleId, m.RuleId);
            });
        Assert.Contains(social.MemoryStore.Memories, m => m.RememberingActor.Kind == ActorKind.Manager);
        Assert.Equal(2, social.MemoryStore.Memories.Count(m => m.RememberingActor.Kind == ActorKind.Player));
    }

    [Fact]
    public void RecordBlowout_HeavyLoss_CreatesNegativeMemories()
    {
        var social = SocialContinuityModule.Create();
        social.MatchPerformanceMemory.RecordBlowoutIfApplicable(
            new FixtureId(3),
            managedGoals: 0,
            opponentGoals: 3,
            new ManagerId(1),
            [new PlayerId(20)],
            Day);

        Assert.All(social.MemoryStore.Memories, m => Assert.Equal(MemoryValence.Negative, m.Valence));
        Assert.Contains(
            social.Queries.GetActiveCategoryCounts(),
            c => c.CategoryName == "Maç performansı" && c.ActiveCount == 2);
    }

    [Fact]
    public void SaveLoad_PreservesMatchPerformanceAtSchemaV31()
    {
        var social = SocialContinuityModule.Create();
        social.MatchPerformanceMemory.RecordBlowoutIfApplicable(
            new FixtureId(9),
            managedGoals: 5,
            opponentGoals: 0,
            new ManagerId(1),
            [new PlayerId(30)],
            Day);

        var path = Path.Combine(_tempDirectory, "match-performance.db");
        new CareerSqlitePersistence().Save(
            path,
            WorldCalendarModule.Create(Day, rootSeed: 5).TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            ClubGovernanceModule.CreateMvpLeague().Store.Registry,
            ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1).Store.Career,
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
            social.PromiseStore.Promises,
            social.MemoryStore.Memories);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(49, loaded.SchemaVersion);
        Assert.Equal(2, loaded.Memories.Count(m => m.Category == MemoryCategory.MatchPerformance));
    }
}
