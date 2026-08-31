using FootballCareerSimulator.Application.CareerWorld;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.CareerWorld;

public sealed class ProductionBalanceTelemetryTests
{
    private static readonly GameDate Opening = ProductionCareerWorldConstraints.DefaultOpeningDate;
    private static readonly int[] SampleSeeds = [11, 22, 33, 44, 55, 66, 77, 88, 99, 111];

    [Fact]
    public void OpeningManagedMatches_StayInsidePlayableScoreAndInjuryBands()
    {
        var totals = new List<int>(SampleSeeds.Length);
        var injuredStarters = new List<int>(SampleSeeds.Length);

        foreach (var seed in SampleSeeds)
        {
            var world = ProductionCareerWorldBootstrap.Create(seed, Opening);
            var calendar = WorldCalendarModule.Create(Opening, rootSeed: seed);
            var clubs = ClubGovernanceModule.Create(world.ClubRegistry);
            var manager = ManagerCareerModule.CreateNewCareer(Opening, startingClubId: 1);
            var selectionStore = new InMemoryMatchSelectionStore();
            var tacticStore = new InMemoryTacticPlanStore();
            var training = TrainingPhysicalStateModule.Create(manager.Store, calendar.TimelineStore);
            var competition = CompetitionModule.CreateForCareer(
                calendar.TimelineStore,
                clubs.Store,
                manager.Store,
                selectionStore,
                training.Store,
                tacticPlanStore: tacticStore);
            var teamPrep = TeamPreparationModule.Create(
                competition.Store,
                manager.Store,
                selectionStore,
                training.Store,
                calendar.TimelineStore,
                tacticPlanStore: tacticStore);

            competition.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 1, Opening.DayNumber));
            foreach (var club in world.Clubs)
            {
                competition.RegisterSeasonParticipant.Handle(
                    new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, club.Id.Value));
            }

            competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, Opening.DayNumber));
            competition.PlanLeagueFixtures.Handle(
                new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, Opening.DayNumber, StartingFixtureId: 1));

            var fixtureId = competition.Queries.GetSeasonFixtures(1)
                .Where(fixture => fixture.HomeClubId == 1 || fixture.AwayClubId == 1)
                .OrderBy(fixture => fixture.ScheduledDayNumber)
                .ThenBy(fixture => fixture.FixtureId)
                .First()
                .FixtureId;

            teamPrep.ApproveDefaultSelection.Handle(
                new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));
            var played = competition.PlayFixtureMatch!.Handle(
                new PlayFixtureMatchCommand(Guid.NewGuid(), 1, fixtureId, Opening.DayNumber));

            Assert.True(played.Succeeded);
            totals.Add(played.HomeGoals + played.AwayGoals);
            injuredStarters.Add(
                training.Store.PhysicalStates.Count(state =>
                    state.ClubId.Value == 1
                    && state.SlotIndex < 11
                    && state.InjurySeverity != InjurySeverity.None));
        }

        Assert.Contains(totals, total => total > 0);
        Assert.All(totals, total => Assert.InRange(total, 0, 8));
        Assert.True(totals.Average() < 5.0);
        Assert.All(injuredStarters, count => Assert.InRange(count, 0, 4));
        Assert.True(injuredStarters.Average() < 2.0);
    }
}
