using FootballCareerSimulator.Application.CareerWorld;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Queries;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.CareerWorld;

public sealed class NextMeaningfulCalendarPointTests
{
    [Fact]
    public void PendingDecision_DoesNotSkip()
    {
        var point = NextMeaningfulCalendarPointResolver.Resolve(
            currentDayNumber: 10,
            hasHardBlocker: false,
            hasPendingDecision: true,
            plannedFixtureDayNumbers: [14, 21],
            transferWindowBoundaryDayNumbers: [18]);

        Assert.True(point.AlreadyAtPoint);
        Assert.Equal(0, point.DaysToAdvance);
        Assert.Equal(NextMeaningfulCalendarPointResolver.ReasonPendingDecision, point.ReasonCode);
    }

    [Fact]
    public void HardBlocker_DoesNotSkip()
    {
        var point = NextMeaningfulCalendarPointResolver.Resolve(
            currentDayNumber: 10,
            hasHardBlocker: true,
            hasPendingDecision: false,
            plannedFixtureDayNumbers: [10, 17],
            transferWindowBoundaryDayNumbers: Array.Empty<int>());

        Assert.True(point.AlreadyAtPoint);
        Assert.Equal(NextMeaningfulCalendarPointResolver.ReasonAlreadyBlocked, point.ReasonCode);
    }

    [Fact]
    public void SkipsEmptyDaysToNextFixture()
    {
        var point = NextMeaningfulCalendarPointResolver.Resolve(
            currentDayNumber: 10,
            hasHardBlocker: false,
            hasPendingDecision: false,
            plannedFixtureDayNumbers: [14, 21],
            transferWindowBoundaryDayNumbers: Array.Empty<int>());

        Assert.False(point.AlreadyAtPoint);
        Assert.Equal(14, point.TargetDayNumber);
        Assert.Equal(4, point.DaysToAdvance);
        Assert.Equal(NextMeaningfulCalendarPointResolver.ReasonUpcomingFixture, point.ReasonCode);
    }

    [Fact]
    public void StopsAtEarlierTransferWindowBoundary()
    {
        var point = NextMeaningfulCalendarPointResolver.Resolve(
            currentDayNumber: 10,
            hasHardBlocker: false,
            hasPendingDecision: false,
            plannedFixtureDayNumbers: [20],
            transferWindowBoundaryDayNumbers: [12, 40]);

        Assert.Equal(12, point.TargetDayNumber);
        Assert.Equal(NextMeaningfulCalendarPointResolver.ReasonTransferWindow, point.ReasonCode);
    }

    [Fact]
    public void DoesNotJumpPastLookaheadHorizon()
    {
        var far = 10 + NextMeaningfulCalendarPointResolver.MaxLookaheadDays + 5;
        var point = NextMeaningfulCalendarPointResolver.Resolve(
            currentDayNumber: 10,
            hasHardBlocker: false,
            hasPendingDecision: false,
            plannedFixtureDayNumbers: [far],
            transferWindowBoundaryDayNumbers: Array.Empty<int>());

        Assert.Equal(11, point.TargetDayNumber);
        Assert.Equal(NextMeaningfulCalendarPointResolver.ReasonCalmStep, point.ReasonCode);
    }

    [Fact]
    public void ProductionNewCareer_FirstMatchdayIsTheNextPoint()
    {
        var opening = ProductionCareerWorldConstraints.DefaultOpeningDate;
        var world = ProductionCareerWorldBootstrap.Create(741852, opening);
        var clubs = ClubGovernanceModule.Create(world.ClubRegistry);
        var calendar = Application.WorldCalendar.Composition.WorldCalendarModule.Create(
            opening,
            rootSeed: 741852);
        var competition = CompetitionModule.CreateForCareer(calendar.TimelineStore, clubs.Store);
        competition.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 1, opening.DayNumber));
        foreach (var club in world.Clubs)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, club.Id.Value));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, opening.DayNumber));
        var firstMatchday = opening.AddDays(4).DayNumber;
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, firstMatchday, StartingFixtureId: 1));

        var fixtureDays = competition.Queries.GetSeasonFixtures(1)
            .Select(fixture => fixture.ScheduledDayNumber)
            .Distinct()
            .ToArray();
        var point = NextMeaningfulCalendarPointResolver.Resolve(
            opening.DayNumber,
            hasHardBlocker: false,
            hasPendingDecision: false,
            fixtureDays,
            Array.Empty<int>());

        Assert.Equal(firstMatchday, point.TargetDayNumber);
        Assert.Equal(4, point.DaysToAdvance);
        Assert.Contains(firstMatchday, fixtureDays);
    }
}
