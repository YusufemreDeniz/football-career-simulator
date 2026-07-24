using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class RelationshipEvaluationTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-relationship",
        Guid.NewGuid().ToString("N"));

    public RelationshipEvaluationTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void FulfilledPromise_RaisesTrust_Idempotently()
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

        var relationship = Assert.Single(social.RelationshipStore.Relationships);
        Assert.Equal(58, relationship.Trust);
        Assert.Equal(RelationshipRecord.NeutralStart, relationship.Respect);
        Assert.Equal("PromiseFulfilledTrust", relationship.LastChangeReasonCode);

        social.RelationshipEvaluation.ApplyPromiseOutcome(
            social.PromiseStore.Promises.Single(),
            Day);
        Assert.Equal(58, social.RelationshipStore.Relationships.Single().Trust);
    }

    [Fact]
    public void BrokenPromise_LowersTrust()
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

        var relationship = Assert.Single(social.RelationshipStore.Relationships);
        Assert.Equal(38, relationship.Trust);
        Assert.Equal("PromiseBrokenTrust", relationship.LastChangeReasonCode);
    }

    [Fact]
    public void SelectionStartedAndOmitted_AdjustRespectAndCompatibility()
    {
        var social = SocialContinuityModule.Create();
        Assert.Equal(
            1,
            social.RelationshipEvaluation.ApplySelectionStarted(
                new FixtureId(9),
                new PlayerId(10),
                new ManagerId(1),
                Day));
        Assert.Equal(
            1,
            social.RelationshipEvaluation.ApplySelectionOmitted(
                new FixtureId(9),
                new PlayerId(11),
                new ManagerId(1),
                Day));

        var started = social.RelationshipStore.FindPlayerToManager(10, 1)!;
        var omitted = social.RelationshipStore.FindPlayerToManager(11, 1)!;
        Assert.Equal(52, started.Respect);
        Assert.Equal(47, omitted.ProfessionalCompatibility);
    }

    [Fact]
    public void SaveLoad_PreservesRelationshipsAtSchemaV32()
    {
        var social = SocialContinuityModule.Create();
        social.RelationshipEvaluation.ApplySelectionStarted(
            new FixtureId(1),
            new PlayerId(5),
            new ManagerId(1),
            Day);

        var path = Path.Combine(_tempDirectory, "relationship.db");
        new CareerSqlitePersistence().Save(
            path,
            WorldCalendarModule.Create(Day, rootSeed: 2).TimelineStore.Timeline,
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
            social.MemoryStore.Memories,
            social.RelationshipStore.Relationships);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(32, loaded.SchemaVersion);
        Assert.Single(loaded.Relationships);
        Assert.Equal(52, loaded.Relationships[0].Respect);
    }
}
