using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class MatchSelectionAvailabilityRevalidationTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private static (
        CompetitionModule Competition,
        TeamPreparationModule TeamPrep,
        TrainingPhysicalStateModule Training,
        MatchSelectionAvailabilityRevalidationService Revalidation) CreateStack()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 11);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var selectionStore = new InMemoryMatchSelectionStore();
        var training = TrainingPhysicalStateModule.Create(
            manager.Store,
            world.TimelineStore,
            matchSelectionStore: selectionStore);
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore,
            training.Store);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            selectionStore,
            training.Store,
            world.TimelineStore);

        competition.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, club));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, Day.DayNumber, StartingFixtureId: 1));

        var revalidation = new MatchSelectionAvailabilityRevalidationService(
            selectionStore,
            training.Store);

        return (competition, teamPrep, training, revalidation);
    }

    private static long ManagedFixtureId(CompetitionModule competition) =>
        competition.Queries.GetSeasonFixtures(1)
            .First(fixture => fixture.HomeClubId == 1 || fixture.AwayClubId == 1)
            .FixtureId;

    [Fact]
    public void Invalidate_RemovesSelection_WhenStarterBecomesUnavailable()
    {
        var (competition, teamPrep, training, revalidation) = CreateStack();
        var fixtureId = ManagedFixtureId(competition);

        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));

        Assert.True(teamPrep.SelectionQueries.IsApproved(fixtureId, 1));

        var starterSlot = teamPrep.SelectionQueries.Get(fixtureId, 1)!.StartingSlotIndices[0];
        var clubId = new ClubId(1);
        training.Store.ReplacePhysicalStatesForClub(
            clubId,
            [
                PlayerPhysicalState.CreateRested(clubId, starterSlot)
                    .WithInjury(InjurySeverity.Serious, Day.AddDays(10)),
            ]);

        var removed = revalidation.InvalidateUnavailableForClub(clubId, Day);

        Assert.Equal(1, removed);
        Assert.False(teamPrep.SelectionQueries.IsApproved(fixtureId, 1));
    }

    [Fact]
    public void Invalidate_KeepsSelection_WhenOnlyBenchInjured()
    {
        var (competition, teamPrep, training, revalidation) = CreateStack();
        var fixtureId = ManagedFixtureId(competition);

        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));

        var benchSlot = teamPrep.SelectionQueries.Get(fixtureId, 1)!.BenchSlotIndices[0];
        var clubId = new ClubId(1);
        training.Store.ReplacePhysicalStatesForClub(
            clubId,
            [
                PlayerPhysicalState.CreateRested(clubId, benchSlot)
                    .WithInjury(InjurySeverity.Minor, Day.AddDays(5)),
            ]);

        var removed = revalidation.InvalidateUnavailableForClub(clubId, Day);

        Assert.Equal(0, removed);
        Assert.True(teamPrep.SelectionQueries.IsApproved(fixtureId, 1));
    }

    [Fact]
    public void PlayFixture_RejectsApprovedSelection_WithUnavailableStarter()
    {
        var (competition, teamPrep, training, _) = CreateStack();
        var fixtureId = ManagedFixtureId(competition);

        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));

        var starterSlot = teamPrep.SelectionQueries.Get(fixtureId, 1)!.StartingSlotIndices[0];
        var clubId = new ClubId(1);
        training.Store.ReplacePhysicalStatesForClub(
            clubId,
            [
                PlayerPhysicalState.CreateRested(clubId, starterSlot)
                    .WithInjury(InjurySeverity.Serious, Day.AddDays(10)),
            ]);

        var ex = Assert.Throws<TeamPreparationInvariantViolationException>(() =>
            competition.PlayFixtureMatch!.Handle(
                new PlayFixtureMatchCommand(Guid.NewGuid(), 1, fixtureId, Day.DayNumber)));

        Assert.Contains("unavailable slot", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
