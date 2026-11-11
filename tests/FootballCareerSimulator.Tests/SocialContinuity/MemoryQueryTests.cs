using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class MemoryQueryTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void GetActiveForActor_OrdersByInfluenceAndLimits()
    {
        var social = SocialContinuityModule.Create();
        social.CareerMemory.RecordDismissal(
            new ManagerId(1),
            new ClubId(2),
            new FixtureId(9),
            Day);
        social.ClubHistoryMemory.RecordManagerLeftDismissed(
            new ManagerId(1),
            new ClubId(2),
            new FixtureId(9),
            Day);
        social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(1001),
            new ClubId(1),
            targetStarts: 1,
            deadlineOn: Day.AddDays(5),
            createdOn: Day);
        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(5));

        var managerView = social.Queries.GetActiveForActor(ActorKind.Manager, 1, take: 2);
        Assert.True(managerView.ActiveCount >= 3);
        Assert.Equal(2, managerView.RecentActive.Count);
        Assert.True(
            managerView.RecentActive[0].CurrentInfluence
            >= managerView.RecentActive[1].CurrentInfluence);

        var playerView = social.Queries.GetActiveForActor(ActorKind.Player, 1001, take: 8);
        Assert.True(playerView.ActiveCount >= 1);
        Assert.Contains(playerView.RecentActive, m => m.CategoryName is "Söz" or "Güven");
    }

    [Fact]
    public void GetActiveCategoryCounts_GroupsActiveMemories()
    {
        var social = SocialContinuityModule.Create();
        social.CareerMemory.RecordDismissal(
            new ManagerId(1),
            new ClubId(2),
            new FixtureId(3),
            Day);
        social.ClubHistoryMemory.RecordManagerLeftDismissed(
            new ManagerId(1),
            new ClubId(2),
            new FixtureId(3),
            Day);

        var counts = social.Queries.GetActiveCategoryCounts();
        Assert.Contains(counts, c => c.CategoryName == "Kariyer" && c.ActiveCount == 1);
        Assert.Contains(counts, c => c.CategoryName == "Kulüp geçmişi" && c.ActiveCount == 1);
    }
}
