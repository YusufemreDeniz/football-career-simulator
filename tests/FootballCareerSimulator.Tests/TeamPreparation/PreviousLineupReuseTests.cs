using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class PreviousLineupReuseTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private static (
        CompetitionModule Competition,
        TeamPreparationModule TeamPrep,
        TrainingPhysicalStateModule Training) CreateStack()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 17);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var selectionStore = new InMemoryMatchSelectionStore();
        var training = TrainingPhysicalStateModule.Create(manager.Store, world.TimelineStore);
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore);
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

        return (competition, teamPrep, training);
    }

    private static long[] ManagedFixtureIds(CompetitionModule competition) =>
        competition.Queries.GetSeasonFixtures(1)
            .Where(fixture => fixture.HomeClubId == 1 || fixture.AwayClubId == 1)
            .OrderBy(fixture => fixture.ScheduledDayNumber)
            .ThenBy(fixture => fixture.FixtureId)
            .Select(fixture => fixture.FixtureId)
            .ToArray();

    [Fact]
    public void SecondDefaultApprove_ReusesCustomPreviousXi()
    {
        var (competition, teamPrep, _) = CreateStack();
        var managed = ManagedFixtureIds(competition);
        Assert.True(managed.Length >= 2);

        var starting = Enumerable.Range(2, MatchSelection.StartingXiSize).ToArray();
        var bench = Enumerable.Range(13, MatchSelection.MaxBenchSize).ToArray();
        teamPrep.ApproveSelection.Handle(
            new ApproveMatchSelectionCommand(
                Guid.NewGuid(),
                managed[0],
                ClubId: 1,
                starting,
                bench));

        var second = teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), managed[1], ClubId: 1));

        Assert.Equal("önceki XI korundu", second.AutoSwapSummary);
        var loaded = teamPrep.SelectionQueries.Get(managed[1], 1)!;
        Assert.Equal(starting, loaded.StartingSlotIndices);
        Assert.Equal(bench, loaded.BenchSlotIndices);
    }

    [Fact]
    public void Reuse_DropsInjuredPreviousStarter()
    {
        var (competition, teamPrep, training) = CreateStack();
        var managed = ManagedFixtureIds(competition);
        Assert.True(managed.Length >= 2);

        var starting = Enumerable.Range(2, MatchSelection.StartingXiSize).ToArray();
        var bench = Enumerable.Range(13, MatchSelection.MaxBenchSize).ToArray();
        teamPrep.ApproveSelection.Handle(
            new ApproveMatchSelectionCommand(
                Guid.NewGuid(),
                managed[0],
                ClubId: 1,
                starting,
                bench));

        var injuredSlot = starting[0];
        var clubId = new ClubId(1);
        training.Store.ReplacePhysicalStatesForClub(
            clubId,
            [
                PlayerPhysicalState.CreateRested(clubId, injuredSlot)
                    .WithInjury(InjurySeverity.Serious, Day.AddDays(10)),
            ]);

        var second = teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), managed[1], ClubId: 1));

        var loaded = teamPrep.SelectionQueries.Get(managed[1], 1)!;
        Assert.DoesNotContain(injuredSlot, loaded.StartingSlotIndices);
        Assert.Contains(injuredSlot, loaded.BenchSlotIndices);
        Assert.Contains("sakatlar dışarı", second.AutoSwapSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayMatch_ClearsFixtureSelection_ButKeepsClubLineupTemplate()
    {
        var (competition, teamPrep, _) = CreateStack();
        var managed = ManagedFixtureIds(competition);
        Assert.True(managed.Length >= 1);

        var starting = Enumerable.Range(2, MatchSelection.StartingXiSize).ToArray();
        var bench = Enumerable.Range(13, MatchSelection.MaxBenchSize).ToArray();
        teamPrep.ApproveSelection.Handle(
            new ApproveMatchSelectionCommand(
                Guid.NewGuid(),
                managed[0],
                ClubId: 1,
                starting,
                bench));

        var played = competition.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(Guid.NewGuid(), 1, managed[0], Day.DayNumber));
        Assert.True(played.Succeeded);
        Assert.Null(teamPrep.SelectionQueries.Get(managed[0], 1));

        var template = teamPrep.SelectionStore.GetLineupTemplate(new ClubId(1));
        Assert.NotNull(template);
        Assert.Equal(starting, template.Value.StartingSlotIndices);
        Assert.Equal(bench, template.Value.BenchSlotIndices);
    }

    [Fact]
    public void TacticPlan_StaysAsNextMatchStartingPoint()
    {
        var (_, teamPrep, _) = CreateStack();
        var clubId = new ClubId(1);
        teamPrep.TacticPlans.SetApproach(clubId, TacticalApproach.Attacking, Day);
        teamPrep.TacticPlans.SetPressing(clubId, PressingIntensity.HighPress, Day);
        teamPrep.TacticPlans.SetDefensiveLine(clubId, DefensiveLine.High, Day);

        var later = teamPrep.TacticPlans.EnsureDefault(clubId, Day.AddDays(7));
        Assert.Equal(TacticalApproach.Attacking, later.Approach);
        Assert.Equal(PressingIntensity.HighPress, later.Pressing);
        Assert.Equal(DefensiveLine.High, later.DefensiveLine);
        Assert.Equal("Hücum", teamPrep.TacticQueries.GetManagedClubPlan().ApproachName);
    }
}
