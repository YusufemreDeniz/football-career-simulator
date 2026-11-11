using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class PromiseQueryTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void GetActiveForPromisor_ReturnsActiveOrderedByDeadline()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 3,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);
        social.PlayingTime.Create(
            new ManagerId(1),
            new PlayerId(1002),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(10),
            createdOn: Day);

        var view = social.PromiseQueries.GetActiveForPromisor(ActorKind.Manager, 1, take: 8);

        Assert.Equal(2, view.ActiveCount);
        Assert.Equal(2, view.RecentActive.Count);
        Assert.Equal("Oyun süresi", view.RecentActive[0].KindName);
        Assert.Equal("İlk 11", view.RecentActive[1].KindName);
        Assert.True(view.RecentActive[0].DeadlineDayNumber <= view.RecentActive[1].DeadlineDayNumber);
    }

    [Fact]
    public void GetActiveForClub_ExcludesTerminalPromises()
    {
        var social = SocialContinuityModule.Create();
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 1,
            deadlineOn: Day.AddDays(5),
            createdOn: Day);
        social.PlayingTime.Create(
            new ManagerId(1),
            new PlayerId(1002),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);

        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(5));

        var clubView = social.PromiseQueries.GetActiveForClub(new ClubId(1));
        Assert.Equal(1, clubView.ActiveCount);
        Assert.Equal("Oyun süresi", clubView.RecentActive[0].KindName);

        var promiseeView = social.PromiseQueries.GetActiveForPromisee(ActorKind.Player, 1002);
        Assert.Equal(1, promiseeView.ActiveCount);
        Assert.Equal(1002, promiseeView.RecentActive[0].PromiseeId);
    }

    [Fact]
    public void ReplaceAll_RebuildsPromiseIndexesAndNextId()
    {
        var source = SocialContinuityModule.Create();
        var active = source.PlayingTime.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(14),
            createdOn: Day);
        source.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1002),
            new ClubId(1),
            targetStarts: 1,
            deadlineOn: Day.AddDays(3),
            createdOn: Day);
        source.StartingOpportunity.EvaluateDeadlines(Day.AddDays(3));

        var rehydrated = SocialContinuityModule.Create();
        rehydrated.PromiseStore.ReplaceAll(source.PromiseStore.Promises);

        Assert.Equal(3L, rehydrated.PromiseStore.NextPromiseId);
        Assert.Single(rehydrated.PromiseStore.ActivePromises);
        Assert.Equal(
            active.PromiseId,
            rehydrated.PromiseStore.FindActivePlayerPromise(1001, new ClubId(1), PromiseKind.PlayingTime)?.PromiseId);
        Assert.Null(
            rehydrated.PromiseStore.FindActivePlayerPromise(1002, new ClubId(1), PromiseKind.StartingOpportunity));
    }
}
