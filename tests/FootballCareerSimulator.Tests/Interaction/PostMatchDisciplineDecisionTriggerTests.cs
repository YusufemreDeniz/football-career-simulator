using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class PostMatchDisciplineDecisionTriggerTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private static (InteractionModule Interaction, PostMatchDisciplineDecisionTrigger Trigger)
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
        return (interaction, interaction.PostMatchDiscipline);
    }

    [Fact]
    public void ManagedRedCard_OpensDisciplineForLowestPlayerId()
    {
        var (interaction, trigger) = Create();
        var opened = trigger.TryOpenAfterManagedRedCards(
            [new PlayerId(9), new PlayerId(3)],
            Day);

        Assert.NotNull(opened);
        Assert.Equal(DecisionRequestKind.DisciplineRequest, opened.Kind);
        Assert.Equal(3, opened.SubjectPlayerId.Value);
        Assert.True(opened.IsHardBlocker);
        Assert.Single(interaction.DecisionRequestStore.Requests);
        Assert.Single(interaction.DialogueSessionStore.Sessions);
    }

    [Fact]
    public void EmptySentOff_DoesNotOpen()
    {
        var (_, trigger) = Create();
        Assert.Null(trigger.TryOpenAfterManagedRedCards([], Day));
    }

    [Fact]
    public void SecondRedCard_WhileDisciplineOpen_DoesNotDuplicate()
    {
        var (interaction, trigger) = Create();
        Assert.NotNull(trigger.TryOpenAfterManagedRedCards([new PlayerId(4)], Day));

        var second = trigger.TryOpenAfterManagedRedCards([new PlayerId(8)], Day);

        Assert.Null(second);
        Assert.Single(interaction.DecisionRequestStore.Requests, r => r.IsOpen);
    }

    [Fact]
    public void ExplainCausality_AndDesk_SurfaceRedCardReason()
    {
        var (interaction, trigger) = Create();
        var opened = trigger.TryOpenAfterManagedRedCards([new PlayerId(12)], Day);
        Assert.NotNull(opened);

        var causality = interaction.Queries.ExplainCausality(opened.DecisionRequestId);
        Assert.Equal(
            "Kırmızı kart gördü — soyunma odasında konuşma şart",
            causality);

        var desk = DecisionDeskDigest.Compose(
            interaction.Queries.GetPending(),
            Day.DayNumber,
            causality);
        Assert.Equal("Kırmızı kart — soyunma odasında konuşma.", desk.Headline);
        Assert.Contains("Kırmızı kart gördü", desk.SupportingLine, StringComparison.Ordinal);
    }
}
