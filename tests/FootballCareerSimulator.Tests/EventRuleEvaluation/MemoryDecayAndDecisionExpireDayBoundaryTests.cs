using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.EventRuleEvaluation;

public sealed class MemoryDecayAndDecisionExpireDayBoundaryTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void Advance_AppliesMemoryDecayViaDayBoundary()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 41);
        var social = SocialContinuityModule.Create();
        social.SelectionMemory.RecordStarts(new FixtureId(2), [new PlayerId(11)], Day);

        world.AdvanceSimulationTime.BindMemoryDecayConsequences(
            new MemoryDecayDayBoundaryApplier(
                social.MemoryDecay,
                world.EventRuleEvaluation!.Gate));

        var advance = world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), Day.AddDays(7).DayNumber));

        Assert.True(advance.Succeeded);
        Assert.True(advance.MemoriesDecayedCount >= 1);
        Assert.Equal(27, social.MemoryStore.Memories.Single().CurrentInfluence);
    }

    [Fact]
    public void Advance_ExpiresSoftDecisionViaDayBoundary()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 42);
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

        world.AdvanceSimulationTime.BindDecisionExpireConsequences(
            new DecisionExpireDayBoundaryApplier(
                interaction.Decisions,
                world.EventRuleEvaluation!.Gate));

        var advance = world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), Day.AddDays(3).DayNumber));

        Assert.True(advance.Succeeded);
        Assert.Equal(1, advance.DecisionsExpiredCount);
        Assert.Equal(
            DecisionRequestStatus.Expired,
            interaction.DecisionRequestStore.Requests.Single().Status);
    }
}
