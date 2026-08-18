using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class PostMatchPlayingTimeDemandTriggerTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private static (
        InteractionModule Interaction,
        SocialContinuityModule Social,
        PostMatchPlayingTimeDemandTrigger Trigger,
        Domain.ManagerCareer.ManagerId ManagerId) Create()
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
        return (interaction, social, interaction.PostMatchPlayingTimeDemand, manager.Store.Career.ManagerId);
    }

    [Fact]
    public void ThreeBenchMemories_OpensPlayingTimeRequest_ForHighestPressure()
    {
        var (interaction, social, trigger, _) = Create();
        SeedBenchMemories(social, new PlayerId(7), count: 3);
        SeedBenchMemories(social, new PlayerId(3), count: 4);

        var opened = trigger.TryOpenAfterManagedSittingOut(
            [new PlayerId(7), new PlayerId(3)],
            Day);

        Assert.NotNull(opened);
        Assert.Equal(DecisionRequestKind.PlayingTimeRequest, opened.Kind);
        Assert.Equal(3, opened.SubjectPlayerId.Value);
        Assert.True(opened.IsHardBlocker);
        Assert.Single(interaction.DecisionRequestStore.Requests, r => r.IsOpen);
    }

    [Fact]
    public void BelowThreshold_DoesNotOpen()
    {
        var (_, social, trigger, _) = Create();
        SeedBenchMemories(social, new PlayerId(7), count: 2);

        Assert.Null(trigger.TryOpenAfterManagedSittingOut([new PlayerId(7)], Day));
    }

    [Fact]
    public void OmittedReinforcements_CountTowardThreshold()
    {
        var (interaction, social, trigger, _) = Create();
        var player = new PlayerId(11);
        social.SelectionMemory.RecordMatchday(
            new FixtureId(1),
            startingPlayerIds: Array.Empty<PlayerId>(),
            benchedPlayerIds: Array.Empty<PlayerId>(),
            squadMembers: [player],
            Day);
        social.SelectionMemory.RecordMatchday(
            new FixtureId(2),
            startingPlayerIds: Array.Empty<PlayerId>(),
            benchedPlayerIds: Array.Empty<PlayerId>(),
            squadMembers: [player],
            Day.AddDays(7));
        social.SelectionMemory.RecordMatchday(
            new FixtureId(3),
            startingPlayerIds: Array.Empty<PlayerId>(),
            benchedPlayerIds: Array.Empty<PlayerId>(),
            squadMembers: [player],
            Day.AddDays(14));

        Assert.Equal(
            3,
            PostMatchPlayingTimeDemandTrigger.CountSittingOutEvents(social.MemoryStore.Memories, player));

        var opened = trigger.TryOpenAfterManagedSittingOut([player], Day.AddDays(14));
        Assert.NotNull(opened);
        Assert.Equal(11, opened.SubjectPlayerId.Value);
        Assert.Single(interaction.DecisionRequestStore.Requests);
    }

    [Fact]
    public void ActivePlayingTimePromise_BlocksDemand()
    {
        var (_, social, trigger, managerId) = Create();
        var player = new PlayerId(9);
        SeedBenchMemories(social, player, count: 3);
        social.PlayingTime.Create(
            managerId,
            player,
            new Domain.Shared.ClubId(1),
            targetAppearances: 3,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);

        Assert.Null(trigger.TryOpenAfterManagedSittingOut([player], Day));
    }

    [Fact]
    public void OpenPlayingTimeRequest_BlocksDuplicate()
    {
        var (interaction, social, trigger, _) = Create();
        var player = new PlayerId(9);
        SeedBenchMemories(social, player, count: 3);
        Assert.NotNull(trigger.TryOpenAfterManagedSittingOut([player], Day));

        SeedBenchMemories(social, player, count: 1, startFixtureId: 100);
        Assert.Null(trigger.TryOpenAfterManagedSittingOut([player], Day.AddDays(1)));
        Assert.Single(interaction.DecisionRequestStore.Requests, r => r.IsOpen);
    }

    [Fact]
    public void EmptyCandidates_DoesNotOpen()
    {
        var (_, _, trigger, _) = Create();
        Assert.Null(trigger.TryOpenAfterManagedSittingOut(Array.Empty<PlayerId>(), Day));
    }

    private static void SeedBenchMemories(
        SocialContinuityModule social,
        PlayerId playerId,
        int count,
        long startFixtureId = 1)
    {
        for (var i = 0; i < count; i++)
        {
            social.SelectionMemory.RecordMatchday(
                new FixtureId(startFixtureId + i),
                startingPlayerIds: Array.Empty<PlayerId>(),
                benchedPlayerIds: [playerId],
                squadMembers: null,
                Day.AddDays(i));
        }
    }
}
