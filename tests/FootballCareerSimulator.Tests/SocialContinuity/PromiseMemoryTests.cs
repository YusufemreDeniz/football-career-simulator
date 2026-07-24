using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class PromiseMemoryTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-promise-memory",
        Guid.NewGuid().ToString("N"));

    public PromiseMemoryTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void FulfilledPromise_CreatesPromiseMemoriesForBothActors_Idempotently()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 1,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);

        social.StartingOpportunity.RecordStartsForPlayers(
            new FixtureId(7),
            new ClubId(1),
            [new PlayerId(1001)],
            Day);

        Assert.Equal(3, social.MemoryStore.Memories.Count);
        var promiseMemories = social.MemoryStore.Memories
            .Where(m => m.Category == MemoryCategory.Promise)
            .ToArray();
        Assert.Equal(2, promiseMemories.Length);
        Assert.All(promiseMemories, m =>
        {
            Assert.Equal(MemoryValence.Positive, m.Valence);
            Assert.Equal(PromiseStatus.Fulfilled.ToString(), m.SourceEventKey.Split(':')[^1]);
        });
        Assert.Contains(social.MemoryStore.Memories, m => m.Category == MemoryCategory.Trust);

        social.PromiseMemory.RecordOutcome(social.PromiseStore.Promises.Single(), Day);
        Assert.Equal(3, social.MemoryStore.Memories.Count);
    }

    [Fact]
    public void BrokenPromise_CreatesNegativeMemories()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 3,
            deadlineOn: Day.AddDays(5),
            createdOn: Day);

        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(5));

        Assert.Equal(3, social.MemoryStore.Memories.Count);
        var promiseMemories = social.MemoryStore.Memories
            .Where(m => m.Category == MemoryCategory.Promise)
            .ToArray();
        Assert.Equal(2, promiseMemories.Length);
        Assert.All(promiseMemories, m =>
        {
            Assert.Equal(MemoryValence.Negative, m.Valence);
            Assert.Equal(80, m.BaseImportance);
            Assert.Equal(MemoryStatus.Active, m.Status);
        });
    }

    [Fact]
    public void SaveLoad_PreservesMemoriesAtSchemaV31()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 1,
            deadlineOn: Day.AddDays(10),
            createdOn: Day);
        social.StartingOpportunity.RecordStartsForPlayers(
            new FixtureId(3),
            new ClubId(1),
            [new PlayerId(1001)],
            Day);

        var world = WorldCalendarModule.Create(Day, rootSeed: 9);
        var path = Path.Combine(_tempDirectory, "memory.db");
        new CareerSqlitePersistence().Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            ClubGovernanceModule.CreateMvpLeague().Store.Registry,
            ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1).Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<Domain.TrainingPhysicalState.WeeklyTrainingPlan>(),
            Array.Empty<Domain.TrainingPhysicalState.PlayerPhysicalState>(),
            Array.Empty<PlayerCareerAggregate>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<TacticPlan>(),
            Array.Empty<Domain.Transfer.TransferNeed>(),
            Array.Empty<Domain.Transfer.ShortlistEntry>(),
            Array.Empty<Domain.Transfer.TransferTarget>(),
            Array.Empty<Domain.Transfer.TransferProcess>(),
            Array.Empty<Domain.Transfer.ClubOffer>(),
            Array.Empty<Domain.Transfer.PlayerContractProposal>(),
            social.PromiseStore.Promises,
            social.MemoryStore.Memories);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(33, loaded.SchemaVersion);
        Assert.Equal(3, loaded.Memories.Count);
        Assert.Contains(loaded.Memories, m => m.RememberingActor.Kind == ActorKind.Player);
        Assert.Contains(loaded.Memories, m => m.RememberingActor.Kind == ActorKind.Manager);
        Assert.Contains(loaded.Memories, m => m.Category == MemoryCategory.Trust);
    }
}
