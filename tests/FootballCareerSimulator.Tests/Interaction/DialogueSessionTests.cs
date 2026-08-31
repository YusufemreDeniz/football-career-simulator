using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DialogueSessionTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-dialogue-session",
        Guid.NewGuid().ToString("N"));

    public DialogueSessionTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void OpenPlayingTimeRequest_CreatesAwaitingDialogueSession_WithFrozenOptions()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            promiseStore: social.PromiseStore);

        var request = interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(30), Day);
        var session = Assert.Single(interaction.DialogueSessionStore.Sessions);
        Assert.Equal(request.DecisionRequestId, session.SourceDecisionRequestId);
        Assert.Equal(DialogueSessionStatus.AwaitingPlayerDecision, session.Status);
        Assert.Equal(DialogueSession.PlayingTimeRequestType, session.DialogueTypeCode);
        Assert.Equal(
            new[]
            {
                DecisionRequest.OptionGrantPlayingTimePromise,
                DecisionRequest.OptionRefuse,
            },
            session.AvailableOptionCodes);
    }

    [Fact]
    public void Answer_ResolvesDialogueSession()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore);

        var request = interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(31), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionRefuse,
            Day);

        var session = Assert.Single(interaction.DialogueSessionStore.Sessions);
        Assert.Equal(DialogueSessionStatus.Resolved, session.Status);
        Assert.Equal(DecisionRequest.OptionRefuse, session.SelectedOptionCode);
    }

    [Fact]
    public void ExpireDue_ExpiresDialogueSession()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore);

        interaction.Decisions.OpenPlayingTimeRequest(
            new PlayerId(32),
            Day,
            deadlineDays: 1,
            isHardBlocker: false);
        Assert.Equal(1, interaction.Decisions.ExpireDue(Day.AddDays(1)));

        var session = Assert.Single(interaction.DialogueSessionStore.Sessions);
        Assert.Equal(DialogueSessionStatus.Expired, session.Status);
    }

    [Fact]
    public void SaveLoad_PreservesDialogueSessionAtSchemaV36()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            promiseStore: social.PromiseStore);
        interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(33), Day);

        var path = Path.Combine(_tempDirectory, "dialogue-session.db");
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
        Assert.Equal(46, loaded.SchemaVersion);
        var session = Assert.Single(loaded.DialogueSessions);
        Assert.Equal(DialogueSessionStatus.AwaitingPlayerDecision, session.Status);
        Assert.Equal(33, session.PrimaryParticipantPlayerId.Value);
        Assert.Contains(DecisionRequest.OptionGrantPlayingTimePromise, session.AvailableOptionCodes);
        Assert.Contains(DecisionRequest.OptionRefuse, session.AvailableOptionCodes);
    }
}
