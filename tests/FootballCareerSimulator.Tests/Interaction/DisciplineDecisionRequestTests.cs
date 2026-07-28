using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Discipline;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DisciplineDecisionRequestTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-discipline",
        Guid.NewGuid().ToString("N"));

    public DisciplineDecisionRequestTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static (InteractionModule Interaction, SocialContinuityModule Social) Create()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            startingOpportunity: social.StartingOpportunity);
        return (interaction, social);
    }

    [Fact]
    public void Warning_CreatesDisciplinaryAction_AndAdjustsDimensions()
    {
        var (interaction, social) = Create();
        var request = interaction.Decisions.OpenDisciplineRequest(new PlayerId(60), Day);
        Assert.Equal(DecisionRequestKind.DisciplineRequest, request.Kind);

        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionIssueWarning,
            Day);

        var action = Assert.Single(interaction.DisciplinaryActionStore.Actions);
        Assert.Equal(DisciplinaryActionKind.Warning, action.Kind);
        var relationship = social.RelationshipStore.FindPlayerToManager(60, 1)!;
        Assert.Equal(48, relationship.Trust);
        Assert.Equal(54, relationship.Respect);
        Assert.Equal(
            MemoryValence.Negative,
            Assert.Single(
                social.MemoryStore.Memories,
                m => m.RuleId == MemoryRecord.DecisionDisciplineAnswerRuleId).Valence);
    }

    [Fact]
    public void Fine_RequiresPriorWarning()
    {
        var (interaction, _) = Create();
        var request = interaction.Decisions.OpenDisciplineRequest(new PlayerId(61), Day);
        var fine = Assert.Single(
            interaction.DialogueOptions.GetForDecision(request.DecisionRequestId).Options,
            o => o.OptionCode == DecisionRequest.OptionIssueFine);
        Assert.False(fine.IsEligible);

        Assert.Throws<InteractionInvariantViolationException>(() =>
            interaction.Decisions.Answer(
                request.DecisionRequestId,
                DecisionRequest.OptionIssueFine,
                Day));
    }

    [Fact]
    public void Fine_AfterWarning_AppliesAndAdjustsCompatibility()
    {
        var (interaction, social) = Create();
        interaction.Discipline.Apply(
            DisciplinaryActionKind.Warning,
            new Domain.ManagerCareer.ManagerId(1),
            new PlayerId(62),
            new Domain.Shared.ClubId(1),
            Day);

        var request = interaction.Decisions.OpenDisciplineRequest(new PlayerId(62), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionIssueFine,
            Day);

        Assert.Equal(2, interaction.DisciplinaryActionStore.Actions.Count);
        Assert.Contains(
            interaction.DisciplinaryActionStore.Actions,
            a => a.Kind == DisciplinaryActionKind.Fine);
        var relationship = social.RelationshipStore.FindPlayerToManager(62, 1)!;
        Assert.Equal(44, relationship.Trust);
        Assert.Equal(56, relationship.Respect);
        Assert.Equal(48, relationship.ProfessionalCompatibility);
    }

    [Fact]
    public void Support_RaisesTrust()
    {
        var (interaction, social) = Create();
        var request = interaction.Decisions.OpenDisciplineRequest(new PlayerId(63), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionOfferSupport,
            Day);

        Assert.Equal(DisciplinaryActionKind.Support, interaction.DisciplinaryActionStore.Actions.Single().Kind);
        Assert.Equal(56, social.RelationshipStore.FindPlayerToManager(63, 1)!.Trust);
        Assert.Equal(48, social.RelationshipStore.FindPlayerToManager(63, 1)!.Respect);
    }

    [Fact]
    public void SaveLoad_PreservesDisciplinaryActionAtSchemaV36()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(manager.Store, social.PlayingTime);
        var request = interaction.Decisions.OpenDisciplineRequest(new PlayerId(64), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionIssueWarning,
            Day);

        var path = Path.Combine(_tempDirectory, "discipline.db");
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
        Assert.Equal(39, loaded.SchemaVersion);
        var action = Assert.Single(loaded.DisciplinaryActions);
        Assert.Equal(DisciplinaryActionKind.Warning, action.Kind);
        Assert.Equal(64, action.SubjectPlayerId.Value);
        Assert.Equal(request.DecisionRequestId, action.SourceDecisionRequestId);
    }
}
