using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class PostMatchBoardDemandTriggerTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private static (InteractionModule Interaction, ManagerCareerModule Manager, PostMatchBoardDemandTrigger Trigger)
        Create()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            memoryStore: social.MemoryStore);
        return (interaction, manager, interaction.PostMatchBoardDemand);
    }

    [Fact]
    public void StableToUnderReview_OpensBoardDemand()
    {
        var (interaction, _, trigger) = Create();
        var opened = trigger.TryOpenAfterRiskEscalation(
            EmploymentRiskBand.Stable,
            EmploymentRiskBand.UnderReview,
            Day);

        Assert.NotNull(opened);
        Assert.Equal(DecisionRequestKind.BoardDemandRequest, opened.Kind);
        Assert.True(opened.IsHardBlocker);
        Assert.Single(interaction.DecisionRequestStore.Requests);
        Assert.Single(interaction.DialogueSessionStore.Sessions);
    }

    [Fact]
    public void SecureToUnderReview_OpensBoardDemand()
    {
        var (_, _, trigger) = Create();
        Assert.NotNull(trigger.TryOpenAfterRiskEscalation(
            EmploymentRiskBand.Secure,
            EmploymentRiskBand.UnderReview,
            Day));
    }

    [Fact]
    public void StableToStable_DoesNotOpen()
    {
        var (_, _, trigger) = Create();
        Assert.Null(trigger.TryOpenAfterRiskEscalation(
            EmploymentRiskBand.Stable,
            EmploymentRiskBand.Stable,
            Day));
    }

    [Fact]
    public void AlreadyUnderReview_DoesNotOpen()
    {
        var (_, _, trigger) = Create();
        Assert.Null(trigger.TryOpenAfterRiskEscalation(
            EmploymentRiskBand.UnderReview,
            EmploymentRiskBand.UnderReview,
            Day));
    }

    [Fact]
    public void SecondEscalation_WhileBoardDemandOpen_DoesNotDuplicate()
    {
        var (interaction, _, trigger) = Create();
        Assert.NotNull(trigger.TryOpenAfterRiskEscalation(
            EmploymentRiskBand.Stable,
            EmploymentRiskBand.UnderReview,
            Day));

        var second = trigger.TryOpenAfterRiskEscalation(
            EmploymentRiskBand.Stable,
            EmploymentRiskBand.UnderReview,
            Day);

        Assert.Null(second);
        Assert.Single(interaction.DecisionRequestStore.Requests, r => r.IsOpen);
    }

    [Fact]
    public void ExplainCausality_AndDesk_SurfaceBoardAssessmentReason()
    {
        var (interaction, manager, trigger) = Create();
        var assessed = manager.Store.Career.ApplyMatchBoardAssessment(
            new FixtureId(1),
            MatchOutcomeForManagedClub.Loss,
            leaguePosition: 18,
            leagueSize: 20);
        manager.Store.Replace(assessed.Career);
        Assert.Equal(EmploymentRiskBand.UnderReview, assessed.RiskBand);
        Assert.Equal("LossBehindExpectation", assessed.ReasonCode);

        var opened = trigger.TryOpenAfterRiskEscalation(
            EmploymentRiskBand.Stable,
            EmploymentRiskBand.UnderReview,
            Day);
        Assert.NotNull(opened);

        var causality = interaction.Queries.ExplainCausality(opened.DecisionRequestId);
        Assert.Equal(
            "Beklentinin altında mağlubiyet — yönetim masaya oturdu",
            causality);

        var desk = DecisionDeskDigest.Compose(
            interaction.Queries.GetPending(),
            Day.DayNumber,
            causality);
        Assert.Equal("Beklentinin altında — yönetim masaya oturdu.", desk.Headline);
        Assert.Contains("Beklentinin altında mağlubiyet", desk.SupportingLine, StringComparison.Ordinal);
    }

    [Fact]
    public void IsEscalationIntoUnderReview_OnlyFromSecureOrStable()
    {
        Assert.True(PostMatchBoardDemandTrigger.IsEscalationIntoUnderReview(
            EmploymentRiskBand.Stable,
            EmploymentRiskBand.UnderReview));
        Assert.False(PostMatchBoardDemandTrigger.IsEscalationIntoUnderReview(
            EmploymentRiskBand.UnderReview,
            EmploymentRiskBand.Critical));
        Assert.False(PostMatchBoardDemandTrigger.IsEscalationIntoUnderReview(
            EmploymentRiskBand.Stable,
            EmploymentRiskBand.Critical));
    }
}
