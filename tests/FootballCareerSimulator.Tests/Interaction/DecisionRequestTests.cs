using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DecisionRequestTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-decision",
        Guid.NewGuid().ToString("N"));

    public DecisionRequestTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void GrantAnswer_CreatesPlayingTimePromise_AndClearsHardBlocker()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory);

        var request = interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(10), Day);
        Assert.True(request.IsHardBlocker);
        var blocker = Assert.Single(interaction.TimeAdvanceBlocker.GetActiveBlockers());
        Assert.Equal(DecisionRequestTimeAdvanceBlockerSource.BlockerTypeCode, blocker.BlockerTypeCode);
        Assert.Equal(DecisionRequestTimeAdvanceBlockerSource.BlockerTypeCode, blocker.DescriptionCode);

        var answered = interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionGrantPlayingTimePromise,
            Day);
        Assert.Equal(DecisionRequestStatus.Answered, answered.Status);
        Assert.Empty(interaction.TimeAdvanceBlocker.GetActiveBlockers());

        var promise = Assert.Single(social.PromiseStore.Promises);
        Assert.Equal(PromiseKind.PlayingTime, promise.Kind);
        Assert.Equal(10, promise.Promisee.Id);

        var relationship = social.RelationshipStore.FindPlayerToManager(10, 1)!;
        Assert.Equal(56, relationship.Trust);
        Assert.Equal("DecisionPlayingTimeGranted", relationship.LastChangeReasonCode);
        var memory = Assert.Single(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.DecisionPlayingTimeAnswerRuleId);
        Assert.Equal(MemoryValence.Positive, memory.Valence);
    }

    [Fact]
    public void RefuseAnswer_LowersTrust_AndWritesMemory_WithoutPromise()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory);

        var request = interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(11), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionRefuse,
            Day);

        Assert.Empty(social.PromiseStore.Promises);
        Assert.Equal(0, interaction.Queries.GetPending().OpenCount);
        Assert.Equal(40, social.RelationshipStore.FindPlayerToManager(11, 1)!.Trust);
        Assert.Equal(
            MemoryValence.Negative,
            Assert.Single(
                social.MemoryStore.Memories,
                m => m.RuleId == MemoryRecord.DecisionPlayingTimeAnswerRuleId).Valence);
    }

    [Fact]
    public void SoftDecision_ExpiresWhenDue_AppliesSocialPenalty()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory);

        interaction.Decisions.OpenPlayingTimeRequest(
            new PlayerId(12),
            Day,
            deadlineDays: 3,
            isHardBlocker: false);

        Assert.Equal(0, interaction.Decisions.ExpireDue(Day.AddDays(2)));
        Assert.Equal(1, interaction.Decisions.ExpireDue(Day.AddDays(3)));
        Assert.Equal(
            DecisionRequestStatus.Expired,
            interaction.DecisionRequestStore.Requests.Single().Status);
        Assert.Equal(44, social.RelationshipStore.FindPlayerToManager(12, 1)!.Trust);
        Assert.Single(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.DecisionPlayingTimeAnswerRuleId);
    }

    [Fact]
    public void SocialOutcomes_AreIdempotent_OnRepeatedExpire()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory);

        interaction.Decisions.OpenPlayingTimeRequest(
            new PlayerId(14),
            Day,
            deadlineDays: 1,
            isHardBlocker: false);

        Assert.Equal(1, interaction.Decisions.ExpireDue(Day.AddDays(1)));
        Assert.Equal(0, interaction.Decisions.ExpireDue(Day.AddDays(1)));
        Assert.Equal(44, social.RelationshipStore.FindPlayerToManager(14, 1)!.Trust);
        Assert.Single(
            social.MemoryStore.Memories,
            m => m.RuleId == MemoryRecord.DecisionPlayingTimeAnswerRuleId);
    }

    [Fact]
    public void SaveLoad_PreservesDecisionRequestAtSchemaV36()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(manager.Store, social.PlayingTime);
        interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(13), Day);

        var path = Path.Combine(_tempDirectory, "decision.db");
        new CareerSqlitePersistence().Save(
            path,
            WorldCalendarModule.Create(Day, rootSeed: 2).TimelineStore.Timeline,
            new Domain.Competition.LeagueCompetition(new Domain.Competition.CompetitionId(1)),
            Application.ClubGovernance.Composition.ClubGovernanceModule.CreateMvpLeague().Store.Registry,
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
            social.PromiseStore.Promises,
            social.MemoryStore.Memories,
            social.RelationshipStore.Relationships,
            interaction.DecisionRequestStore.Requests,
            interaction.DialogueSessionStore.Sessions);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(49, loaded.SchemaVersion);
        var request = Assert.Single(loaded.DecisionRequests);
        Assert.Equal(DecisionRequestKind.PlayingTimeRequest, request.Kind);
        Assert.Equal(13, request.SubjectPlayerId.Value);
        Assert.True(request.IsHardBlocker);
        Assert.Single(loaded.DialogueSessions);
    }
}
