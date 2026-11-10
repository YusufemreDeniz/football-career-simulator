using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.WorldCalendar;
using Xunit;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class MobilePortalAndDeckDigestTests
{
    [Fact]
    public void Compose_BuildsSevenDayCalendarThreeDailyObjectivesAndFourTactileCards()
    {
        var today = GameDate.FromCalendarDate(2026, 8, 15);

        var digest = MobilePortalAndDeckDigest.Compose(
            currentDate: today,
            hasUpcomingMatch: true,
            nextMatchDaysUntil: 2,
            nextOpponentName: "Fenerbahçe",
            isHomeMatch: true,
            isSelectionApproved: false,
            hasTrainingPlan: true,
            hasTacticPlan: true,
            averageSquadFitness: 82,
            fatiguedPlayerCount: 1,
            pendingDecisionCount: 1,
            pendingDecisionSummary: "İlk 11 forma talebi",
            openTransferNeedCount: 1,
            facilityLevel: 3,
            cardsUsedTodayCount: 1);

        Assert.Equal(7, digest.CalendarStrip.Count);
        Assert.True(digest.CalendarStrip[2].IsMatchDay);
        Assert.Contains("Fenerbahçe", digest.CalendarStrip[2].Title);
        Assert.Equal(3, digest.DailyObjectives.Count);
        Assert.Equal(2, digest.CompletedObjectivesCount);
        Assert.Equal(4, digest.TactileDeck.Count);
        Assert.Contains(digest.PortalStream, item => item.CategoryCode == MobilePortalAndDeckDigest.FilterMustRespond);
    }
}
