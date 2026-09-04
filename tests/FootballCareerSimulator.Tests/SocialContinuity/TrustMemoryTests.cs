using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class TrustMemoryTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void FulfilledPromise_CreatesPositiveTrustMemoryForPromiseeAboutPromisor()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 1,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);

        social.StartingOpportunity.RecordStartsForPlayers(
            new FixtureId(7),
            new ClubId(1),
            [new PlayerId(1001)],
            Day);

        var trust = Assert.Single(
            social.MemoryStore.Memories,
            m => m.Category == MemoryCategory.Trust);
        Assert.Equal(ActorKind.Player, trust.RememberingActor.Kind);
        Assert.Equal(1001, trust.RememberingActor.Id);
        Assert.Equal(MemorySubjectKind.Manager, trust.SubjectKind);
        Assert.Equal(1, trust.SubjectId);
        Assert.Equal(MemoryValence.Positive, trust.Valence);
        Assert.Equal(MemoryRecord.TrustFromPromiseRuleId, trust.RuleId);
        Assert.Equal(PromiseStatus.Fulfilled.ToString(), trust.SourceEventKey.Split(':')[^1]);

        social.PromiseMemory.RecordOutcome(social.PromiseStore.Promises.Single(), Day);
        Assert.Equal(1, social.MemoryStore.Memories.Count(m => m.Category == MemoryCategory.Trust));
    }

    [Fact]
    public void BrokenPromise_CreatesNegativeTrustMemory()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 3,
            deadlineOn: Day.AddDays(5),
            createdOn: Day);

        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(5));

        var trust = Assert.Single(
            social.MemoryStore.Memories,
            m => m.Category == MemoryCategory.Trust);
        Assert.Equal(MemoryValence.Negative, trust.Valence);
        Assert.Equal(70, trust.BaseImportance);
        Assert.Equal(MemorySubjectKind.Manager, trust.SubjectKind);
    }

    [Fact]
    public void InvalidatedPromise_DoesNotCreateTrustMemory()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 3,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);

        social.Invalidation.InvalidateForPlayerLeaving(new PlayerId(1001), Day.AddDays(1));

        Assert.Equal(2, social.MemoryStore.Memories.Count);
        Assert.DoesNotContain(social.MemoryStore.Memories, m => m.Category == MemoryCategory.Trust);
        Assert.All(social.MemoryStore.Memories, m => Assert.Equal(MemoryCategory.Promise, m.Category));
    }
}
