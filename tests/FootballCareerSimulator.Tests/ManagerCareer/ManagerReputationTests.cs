using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.ManagerCareerBoard;

public sealed class ManagerReputationTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-reputation",
        Guid.NewGuid().ToString("N"));

    public ManagerReputationTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void StartNewCareer_SetsDefaultReputation()
    {
        var career = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "TD",
            new Domain.Shared.ClubId(1),
            Day,
            clubSportiveStrength: 50);
        Assert.Equal(ManagerReputation.DefaultInitialValue, career.Reputation.Value);
    }

    [Fact]
    public void PressDefend_RaisesReputation_ViaDecisionRequest()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory);
        var before = manager.Store.Career.Reputation.Value;
        var request = interaction.Decisions.OpenPressQuestionRequest(new PlayerId(80), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionPubliclyDefend,
            Day);

        Assert.Equal(before + 2, manager.Store.Career.Reputation.Value);
        Assert.Equal("PressPubliclyDefend", manager.Store.Career.LastReputationReasonCode);
    }

    [Fact]
    public void PressCriticize_LowersReputation_ViaDecisionRequest()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory);
        var before = manager.Store.Career.Reputation.Value;
        var request = interaction.Decisions.OpenPressQuestionRequest(new PlayerId(81), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionPubliclyCriticize,
            Day);

        Assert.Equal(before - 3, manager.Store.Career.Reputation.Value);
        Assert.Equal("PressPubliclyCriticize", manager.Store.Career.LastReputationReasonCode);
    }

    [Fact]
    public void SaveLoad_PreservesReputationAtSchemaV37()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory);
        var request = interaction.Decisions.OpenPressQuestionRequest(new PlayerId(82), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionPubliclyDefend,
            Day);

        var path = Path.Combine(_tempDirectory, "reputation.db");
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
            interaction.DialogueSessionStore.Sessions,
            interaction.DisciplinaryActionStore.Actions);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(44, loaded.SchemaVersion);
        Assert.Equal(52, loaded.ManagerCareer.Reputation.Value);
        Assert.Equal("PressPubliclyDefend", loaded.ManagerCareer.LastReputationReasonCode);
    }
}
