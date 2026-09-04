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

public sealed class PromiseInvalidationTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-promise-invalidation",
        Guid.NewGuid().ToString("N"));

    public PromiseInvalidationTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void PlayerLeaving_InvalidatesActivePromisesAndWritesMemory()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 3,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);
        social.PlayingTime.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetAppearances: 4,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);

        var count = social.Invalidation.InvalidateForPlayerLeaving(new PlayerId(1001), Day.AddDays(1));

        Assert.Equal(2, count);
        Assert.All(social.PromiseStore.Promises, p => Assert.Equal(PromiseStatus.Invalidated, p.Status));
        Assert.Equal(4, social.MemoryStore.Memories.Count);
        Assert.All(
            social.MemoryStore.Memories,
            m => Assert.Equal(MemoryValence.Neutral, m.Valence));
    }

    [Fact]
    public void ManagerLeavingClub_InvalidatesOnlyThatClubPromises()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 2,
            deadlineOn: Day.AddDays(10),
            createdOn: Day);
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(2002),
            new ClubId(2),
            targetStarts: 2,
            deadlineOn: Day.AddDays(10),
            createdOn: Day);

        var count = social.Invalidation.InvalidateForManagerLeavingClub(
            new ManagerId(1),
            new ClubId(1),
            Day.AddDays(2));

        Assert.Equal(1, count);
        Assert.Equal(
            PromiseStatus.Invalidated,
            social.PromiseStore.Promises.Single(p => p.ClubId.Value == 1).Status);
        Assert.Equal(
            PromiseStatus.Active,
            social.PromiseStore.Promises.Single(p => p.ClubId.Value == 2).Status);
    }

    [Fact]
    public void Invalidate_IsIdempotent_AndSkipsTerminalPromises()
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
            new FixtureId(1),
            new ClubId(1),
            [new PlayerId(1001)],
            Day);

        Assert.Equal(0, social.Invalidation.InvalidateForPlayerLeaving(new PlayerId(1001), Day));
        Assert.Equal(PromiseStatus.Fulfilled, social.PromiseStore.Promises.Single().Status);
    }

    [Fact]
    public void SaveLoad_PreservesInvalidatedPromise()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 2,
            deadlineOn: Day.AddDays(15),
            createdOn: Day);
        social.Invalidation.InvalidateForPlayerLeaving(new PlayerId(1001), Day.AddDays(1));

        var world = WorldCalendarModule.Create(Day, rootSeed: 8);
        var path = Path.Combine(_tempDirectory, "invalidated.db");
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
        Assert.Equal(49, loaded.SchemaVersion);
        Assert.Equal(PromiseStatus.Invalidated, loaded.Promises.Single().Status);
        Assert.Equal(2, loaded.Memories.Count);
    }
}
