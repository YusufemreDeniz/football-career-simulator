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
    public void PlayerLeaving_MarksRelationshipsDormant_PreservingDimensions()
    {
        var social = SocialContinuityModule.Create();
        social.RelationshipEvaluation.ApplySelectionStarted(
            new FixtureId(1),
            new PlayerId(10),
            new ManagerId(1),
            Day);

        Assert.Equal(
            1,
            social.RelationshipEvaluation.MarkDormantForPlayerLeaving(new PlayerId(10), Day.AddDays(1)));

        var dormant = Assert.Single(social.RelationshipStore.Relationships);
        Assert.Equal(RelationshipStatus.Dormant, dormant.Status);
        Assert.Equal(52, dormant.Respect);
        Assert.Equal("PlayerLeftDormant", dormant.LastChangeReasonCode);
        Assert.Equal(
            0,
            social.RelationshipEvaluation.MarkDormantForPlayerLeaving(new PlayerId(10), Day.AddDays(2)));
    }

    [Fact]
    public void ManagerLeaving_MarksDormant_AndHiringReactivates()
    {
        var social = SocialContinuityModule.Create();
        social.RelationshipEvaluation.ApplySelectionStarted(
            new FixtureId(2),
            new PlayerId(20),
            new ManagerId(7),
            Day);

        Assert.Equal(
            1,
            social.RelationshipEvaluation.MarkDormantForManagerLeaving(new ManagerId(7), Day.AddDays(1)));
        var dormant = social.RelationshipStore.FindPlayerToManager(20, 7)!;
        Assert.Equal(RelationshipStatus.Dormant, dormant.Status);
        Assert.Equal(52, dormant.Respect);

        Assert.Equal(
            1,
            social.RelationshipEvaluation.ReactivateForManager(new ManagerId(7), Day.AddDays(3)));
        var active = social.RelationshipStore.FindPlayerToManager(20, 7)!;
        Assert.Equal(RelationshipStatus.Active, active.Status);
        Assert.Equal(52, active.Respect);
        Assert.Equal("ManagerHiredReactivate", active.LastChangeReasonCode);
    }

    [Fact]
    public void DormantRelationship_ReactivatesWhenSelectionInputApplies()
    {
        var social = SocialContinuityModule.Create();
        social.RelationshipEvaluation.ApplySelectionStarted(
            new FixtureId(1),
            new PlayerId(30),
            new ManagerId(1),
            Day);
        social.RelationshipEvaluation.MarkDormantForPlayerLeaving(new PlayerId(30), Day.AddDays(1));

        Assert.Equal(
            1,
            social.RelationshipEvaluation.ApplySelectionStarted(
                new FixtureId(4),
                new PlayerId(30),
                new ManagerId(1),
                Day.AddDays(5)));

        var relationship = social.RelationshipStore.FindPlayerToManager(30, 1)!;
        Assert.Equal(RelationshipStatus.Active, relationship.Status);
        Assert.Equal(54, relationship.Respect);
    }

    [Fact]
    public void TrustBandCrossing_CreatesRelationshipMilestoneMemory_Idempotently()
    {
        var social = SocialContinuityModule.Create();
        // Neutral(50) → iki Broken (−12/−12) = 26 Low
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(40),
            new ClubId(1),
            targetStarts: 3,
            deadlineOn: Day.AddDays(5),
            createdOn: Day);
        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(5));
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(40),
            new ClubId(1),
            targetStarts: 3,
            deadlineOn: Day.AddDays(12),
            createdOn: Day.AddDays(6));
        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(12));

        var relationship = social.RelationshipStore.FindPlayerToManager(40, 1)!;
        Assert.Equal(26, relationship.Trust);
        Assert.Equal(RelationshipDimensionBand.Low, RelationshipDimensionBands.FromValue(relationship.Trust));

        var milestone = Assert.Single(
            social.MemoryStore.Memories,
            m => m.Category == MemoryCategory.Relationship
                && m.RuleId == MemoryRecord.RelationshipTrustBandRuleId);
        Assert.Equal(MemoryValence.Negative, milestone.Valence);
        Assert.Equal(
            MemoryRecord.BuildRelationshipTrustBandSourceKey(
                relationship.RelationshipId,
                RelationshipDimensionBand.Neutral,
                RelationshipDimensionBand.Low),
            milestone.SourceEventKey);

        Assert.Equal(
            0,
            social.RelationshipMilestones.EvaluateTrustBandChange(
                relationship,
                relationship,
                Day.AddDays(13)));
        Assert.Single(
            social.MemoryStore.Memories,
            m => m.Category == MemoryCategory.Relationship);
    }

    [Fact]
    public void SelectionOnly_DoesNotCreateTrustBandMilestone()
    {
        var social = SocialContinuityModule.Create();
        social.RelationshipEvaluation.ApplySelectionStarted(
            new FixtureId(8),
            new PlayerId(41),
            new ManagerId(1),
            Day);

        Assert.DoesNotContain(
            social.MemoryStore.Memories,
            m => m.Category == MemoryCategory.Relationship);
    }

    [Fact]
    public void SaveLoad_PreservesRelationshipsAtSchemaV34()
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
        Assert.Equal(35, loaded.SchemaVersion);
        Assert.Single(loaded.Relationships);
        Assert.Equal(52, loaded.Relationships[0].Respect);
    }
}
