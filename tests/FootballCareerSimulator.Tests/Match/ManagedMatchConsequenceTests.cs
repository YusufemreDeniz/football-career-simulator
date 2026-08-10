using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.Match;

public sealed class ManagedMatchConsequenceTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void PlayManagedFixture_ReturnsBoardConsequenceSummary()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 21);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var selectionStore = new InMemoryMatchSelectionStore();
        var tacticStore = new InMemoryTacticPlanStore();
        var training = TrainingPhysicalStateModule.Create(
            manager.Store,
            world.TimelineStore,
            matchSelectionStore: selectionStore);
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
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
            world.TimelineStore,
            tacticPlanStore: tacticStore);

        competition.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, club));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, Day.DayNumber, StartingFixtureId: 1));

        var fixtureId = competition.Queries.GetSeasonFixtures(1)
            .First(fixture => fixture.HomeClubId == 1 || fixture.AwayClubId == 1)
            .FixtureId;

        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));
        var halfTime = competition.PlayFixtureMatch!.PreviewHalfTime(1, fixtureId, Day.DayNumber);
        var substitution = teamPrep.SwapStarterWithBench.Handle(
            new SwapStarterWithBenchCommand(
                Guid.NewGuid(),
                fixtureId,
                ClubId: 1,
                StartingIndex: 1,
                BenchIndex: 2));
        teamPrep.TacticPlans.SetApproach(
            new ClubId(1),
            TacticalApproach.Attacking,
            Day);
        var selection = selectionStore.Get(new FixtureId(fixtureId), new ClubId(1));
        var profiles = MvpSquadRosterGenerator.GeneratePlayerProfiles(new ClubId(1), rootSeed: 21);
        var selectedProfiles = selection!.StartingSlotIndices.Select(slot => profiles[slot]).ToArray();
        var expectedLineupModifier = MvpLineupRoleFitCalculator
            .Evaluate(Formation.F442, selectedProfiles)
            .MatchStrengthModifier;

        var commandId = Guid.NewGuid();
        var command = new PlayFixtureMatchCommand(
            commandId,
            1,
            fixtureId,
            Day.DayNumber,
            ManagedSecondHalfDelta: MatchHalfTimeDigest.DecisionAttack,
            ForcedHalfTimeHomeGoals: halfTime.HomeGoals,
            ForcedHalfTimeAwayGoals: halfTime.AwayGoals);
        var first = competition.PlayFixtureMatch!.Handle(command);
        var second = competition.PlayFixtureMatch.Handle(command);

        Assert.True(first.Succeeded);
        Assert.NotNull(first.Consequences);
        Assert.True(first.Consequences!.IsManagedMatch);
        Assert.True(substitution.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(substitution.HalfTimeBridgeLine));
        Assert.Equal(2, first.ManagedTacticModifier);
        Assert.Equal(expectedLineupModifier, first.ManagedLineupRoleModifier);
        Assert.NotNull(first.Consequences.BoardConfidenceDelta);
        Assert.NotNull(first.Consequences.BoardConfidenceAfter);
        Assert.False(string.IsNullOrWhiteSpace(first.Consequences.BoardRiskBand));
        Assert.NotNull(first.KeyMoments);
        Assert.Equal(
            first.HomeGoals + first.AwayGoals,
            first.KeyMoments!.Count(m => m.Kind == nameof(MatchKeyMomentKind.Goal)));
        Assert.All(
            first.KeyMoments,
            moment =>
            {
                Assert.False(string.IsNullOrWhiteSpace(moment.Kind));
                Assert.False(string.IsNullOrWhiteSpace(moment.PrimaryPlayerName));
                Assert.DoesNotContain("slot ", moment.PrimaryPlayerName!, StringComparison.Ordinal);
                if (moment.AssistSlotIndex is not null)
                {
                    Assert.False(string.IsNullOrWhiteSpace(moment.AssistPlayerName));
                }
            });
        Assert.Equal(first, second);
    }
}
