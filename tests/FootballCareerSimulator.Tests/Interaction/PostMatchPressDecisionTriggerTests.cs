using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class PostMatchPressDecisionTriggerTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private static (InteractionModule Interaction, PostMatchPressDecisionTrigger Trigger)
        Create()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory);
        return (interaction, interaction.PostMatchPress);
    }

    [Fact]
    public void BlowoutLoss_OpensPressQuestionForLowestPlayerId()
    {
        var (interaction, trigger) = Create();
        var opened = trigger.TryOpenAfterManagedBlowoutLoss(
            managedGoals: 0,
            opponentGoals: 3,
            startingPlayerIds: [new PlayerId(12), new PlayerId(5), new PlayerId(9)],
            Day);

        Assert.NotNull(opened);
        Assert.Equal(DecisionRequestKind.PressQuestionRequest, opened.Kind);
        Assert.Equal(5, opened.SubjectPlayerId.Value);
        Assert.True(opened.IsHardBlocker);
        Assert.Single(interaction.DecisionRequestStore.Requests);
        Assert.Single(interaction.DialogueSessionStore.Sessions);
    }

    [Fact]
    public void BlowoutWin_DoesNotOpen()
    {
        var (_, trigger) = Create();
        var opened = trigger.TryOpenAfterManagedBlowoutLoss(
            managedGoals: 4,
            opponentGoals: 0,
            startingPlayerIds: [new PlayerId(5)],
            Day);

        Assert.Null(opened);
    }

    [Fact]
    public void NarrowLoss_DoesNotOpen()
    {
        var (_, trigger) = Create();
        var opened = trigger.TryOpenAfterManagedBlowoutLoss(
            managedGoals: 1,
            opponentGoals: 3,
            startingPlayerIds: [new PlayerId(5)],
            Day);

        Assert.Null(opened);
    }

    [Fact]
    public void SecondBlowout_WhilePressOpen_DoesNotDuplicate()
    {
        var (interaction, trigger) = Create();
        Assert.NotNull(trigger.TryOpenAfterManagedBlowoutLoss(
            managedGoals: 0,
            opponentGoals: 4,
            startingPlayerIds: [new PlayerId(5)],
            Day));

        var second = trigger.TryOpenAfterManagedBlowoutLoss(
            managedGoals: 0,
            opponentGoals: 5,
            startingPlayerIds: [new PlayerId(6)],
            Day);

        Assert.Null(second);
        Assert.Single(interaction.DecisionRequestStore.Requests, r => r.IsOpen);
    }

    [Fact]
    public void EmptyStarters_DoesNotOpen()
    {
        var (_, trigger) = Create();
        Assert.Null(trigger.TryOpenAfterManagedBlowoutLoss(
            managedGoals: 0,
            opponentGoals: 3,
            startingPlayerIds: Array.Empty<PlayerId>(),
            Day));
    }
}
