using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class PromiseBrokenDecisionTriggerTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

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
    public void BrokenPlayingTime_OpensPlayingTimeDecision()
    {
        var (interaction, social) = Create();
        social.PlayingTime.Create(
            new Domain.ManagerCareer.ManagerId(1),
            new PlayerId(40),
            new Domain.Shared.ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(3),
            createdOn: Day);

        DecisionRequest? opened = null;
        social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(3),
            promise => opened = interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(3)));

        Assert.NotNull(opened);
        Assert.Equal(DecisionRequestKind.PlayingTimeRequest, opened.Kind);
        Assert.Equal(40, opened.SubjectPlayerId.Value);
        Assert.Equal(PromiseStatus.Broken, social.PromiseStore.Promises.Single().Status);
    }

    [Fact]
    public void BrokenStartingOpportunity_OpensStartingOpportunityDecision()
    {
        var (interaction, social) = Create();
        social.StartingOpportunity.Create(
            new Domain.ManagerCareer.ManagerId(1),
            new PlayerId(41),
            new Domain.Shared.ClubId(1),
            targetStarts: 2,
            deadlineOn: Day.AddDays(3),
            createdOn: Day);

        DecisionRequest? opened = null;
        social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(3),
            promise => opened = interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(3)));

        Assert.NotNull(opened);
        Assert.Equal(DecisionRequestKind.StartingOpportunityRequest, opened.Kind);
        Assert.Equal(41, opened.SubjectPlayerId.Value);
    }

    [Fact]
    public void FulfilledPromise_DoesNotOpenDecision()
    {
        var (interaction, social) = Create();
        social.PlayingTime.Create(
            new Domain.ManagerCareer.ManagerId(1),
            new PlayerId(42),
            new Domain.Shared.ClubId(1),
            targetAppearances: 1,
            deadlineOn: Day.AddDays(5),
            createdOn: Day);
        social.PlayingTime.RecordAppearancesForPlayers(
            new Domain.Competition.FixtureId(1),
            new Domain.Shared.ClubId(1),
            [new PlayerId(42)],
            Day);

        DecisionRequest? opened = null;
        social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(5),
            promise => opened = interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(5)));

        Assert.Null(opened);
        Assert.Equal(PromiseStatus.Fulfilled, social.PromiseStore.Promises.Single().Status);
        Assert.Empty(interaction.DecisionRequestStore.Requests);
    }

    [Fact]
    public void AlreadyOpenDecision_DoesNotDuplicate()
    {
        var (interaction, social) = Create();
        interaction.Decisions.OpenPlayingTimeRequest(new PlayerId(43), Day);
        social.PlayingTime.Create(
            new Domain.ManagerCareer.ManagerId(1),
            new PlayerId(43),
            new Domain.Shared.ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(2),
            createdOn: Day);

        DecisionRequest? opened = null;
        social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(2),
            promise => opened = interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(2)));

        Assert.Null(opened);
        Assert.Single(interaction.DecisionRequestStore.Requests);
    }
}
