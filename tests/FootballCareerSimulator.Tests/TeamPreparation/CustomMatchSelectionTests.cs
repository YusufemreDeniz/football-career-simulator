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
using FootballCareerSimulator.Simulation.TrainingPhysicalState;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class CustomMatchSelectionTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private static (
        WorldCalendarModule World,
        CompetitionModule Competition,
        ManagerCareerModule Manager,
        TeamPreparationModule TeamPrep,
        TrainingPhysicalStateModule Training) CreateStack()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 7);
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

        competition.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, club));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, Day.DayNumber, StartingFixtureId: 1));

        return (world, competition, manager, teamPrep, training);
    }

    [Fact]
    public void ApproveCustom_RejectsUnavailableStarter()
    {
        var (_, _, _, teamPrep, training) = CreateStack();
        var clubId = new ClubId(1);
        training.Store.ReplacePhysicalStatesForClub(
            clubId,
            [
                PlayerPhysicalState.CreateRested(clubId, 0)
                    .WithInjury(InjurySeverity.Serious, Day.AddDays(10)),
            ]);

        var starting = Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray();
        var bench = Enumerable.Range(MatchSelection.StartingXiSize, MatchSelection.MaxBenchSize).ToArray();

        Assert.Throws<TeamPreparationInvariantViolationException>(() =>
            teamPrep.ApproveSelection.Handle(
                new ApproveMatchSelectionCommand(
                    Guid.NewGuid(),
                    FixtureId: 1,
                    ClubId: 1,
                    starting,
                    bench)));
    }

    [Fact]
    public void ApproveCustom_AcceptsAvailableLineup()
    {
        var (_, _, _, teamPrep, _) = CreateStack();
        var starting = Enumerable.Range(2, MatchSelection.StartingXiSize).ToArray();
        var bench = Enumerable.Range(13, MatchSelection.MaxBenchSize).ToArray();

        var result = teamPrep.ApproveSelection.Handle(
            new ApproveMatchSelectionCommand(
                Guid.NewGuid(),
                FixtureId: 1,
                ClubId: 1,
                starting,
                bench));

        Assert.True(result.Succeeded);
        Assert.Equal(starting, result.StartingSlotIndices);
        Assert.Equal(bench, result.BenchSlotIndices);
    }

    [Fact]
    public void SwapStarterWithBench_ExchangesSlots()
    {
        var (_, _, _, teamPrep, _) = CreateStack();
        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), FixtureId: 1, ClubId: 1));

        var before = teamPrep.SelectionQueries.Get(1, 1)!;
        var lastStarter = before.StartingSlotIndices[^1];
        var firstBench = before.BenchSlotIndices[0];

        var swapped = teamPrep.SwapStarterWithBench.Handle(
            new SwapStarterWithBenchCommand(
                Guid.NewGuid(),
                FixtureId: 1,
                ClubId: 1,
                StartingIndex: MatchSelection.StartingXiSize - 1,
                BenchIndex: 0));

        Assert.Equal(firstBench, swapped.StartingSlotIndices[^1]);
        Assert.Equal(lastStarter, swapped.BenchSlotIndices[0]);
        Assert.Equal(lastStarter, swapped.OutSlotIndex);
        Assert.Equal(firstBench, swapped.InSlotIndex);
        Assert.False(string.IsNullOrWhiteSpace(swapped.SwapSummary));
        Assert.Contains("çıktı", swapped.SwapSummary!, StringComparison.Ordinal);
        Assert.Contains("XI'ye girdi", swapped.SwapSummary!, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(swapped.HalfTimeBridgeLine));
        Assert.StartsWith("Devre arasında", swapped.HalfTimeBridgeLine!, StringComparison.Ordinal);
        Assert.Contains("↔", swapped.HalfTimeBridgeLine!, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultSelection_ThrowsWhenFewerThanElevenAvailable()
    {
        var clubId = new ClubId(1);
        var physical = Enumerable.Range(0, MatchSelection.MaxSquadSlot + 1)
            .ToDictionary(
                slot => (clubId.Value, slot),
                slot => PlayerPhysicalState.CreateRested(clubId, slot)
                    .WithInjury(InjurySeverity.Minor, Day.AddDays(5)));

        // Leave only 10 available — sakatlar emergency ile XI'ye alınmaz.
        for (var slot = 0; slot < 10; slot++)
        {
            physical[(clubId.Value, slot)] = PlayerPhysicalState.CreateRested(clubId, slot);
        }

        Assert.Throws<TeamPreparationInvariantViolationException>(() =>
            MvpAvailabilityAwareSelection.ApproveDefaultPreferringAvailable(
                new FixtureId(3),
                clubId,
                Day,
                physical));
    }

    [Fact]
    public void DefaultSelection_ThrowsWhenSquadSmallerThanEleven()
    {
        var clubId = new ClubId(1);
        var day = Day;
        var members = Enumerable.Range(0, 10)
            .Select(slot => SquadMember.Create(
                new Domain.PlayerCareer.PlayerId(1000 + slot),
                slot,
                day))
            .ToArray();
        var squad = ClubSquad.Rehydrate(clubId, members);
        var physical = Enumerable.Range(0, 10)
            .ToDictionary(
                slot => (clubId.Value, slot),
                slot => PlayerPhysicalState.CreateRested(clubId, slot));

        Assert.Throws<TeamPreparationInvariantViolationException>(() =>
            MvpAvailabilityAwareSelection.ApproveDefaultPreferringAvailable(
                new FixtureId(3),
                clubId,
                day,
                physical,
                squad));
    }

    [Fact]
    public void DefaultSelection_NeverPlacesUnavailableOnBench()
    {
        var clubId = new ClubId(1);
        var physical = Enumerable.Range(0, MatchSelection.MaxSquadSlot + 1)
            .ToDictionary(
                slot => (clubId.Value, slot),
                slot => PlayerPhysicalState.CreateRested(clubId, slot));
        physical[(clubId.Value, 20)] = PlayerPhysicalState.CreateRested(clubId, 20)
            .WithInjury(InjurySeverity.Minor, Day.AddDays(5));

        var selection = MvpAvailabilityAwareSelection.ApproveDefaultPreferringAvailable(
            new FixtureId(3),
            clubId,
            Day,
            physical);

        Assert.DoesNotContain(20, selection.StartingSlotIndices);
        Assert.DoesNotContain(20, selection.BenchSlotIndices);
    }

    [Fact]
    public void DefaultSelection_PrefersOneGoalkeeperWhenOutfieldStarterIsUnavailable()
    {
        var clubId = new ClubId(1);
        var physical = Enumerable.Range(0, MatchSelection.MaxSquadSlot + 1)
            .ToDictionary(
                slot => (clubId.Value, slot),
                slot => PlayerPhysicalState.CreateRested(clubId, slot));
        physical[(clubId.Value, 1)] = PlayerPhysicalState.CreateRested(clubId, 1)
            .WithInjury(InjurySeverity.Minor, Day.AddDays(5));

        var selection = MvpAvailabilityAwareSelection.ApproveDefaultPreferringAvailable(
            new FixtureId(3),
            clubId,
            Day,
            physical);
        var profiles = MvpSquadRosterGenerator.GeneratePlayerProfiles(clubId, rootSeed: 0);
        var goalkeeperCount = selection.StartingSlotIndices
            .Count(slot => profiles[slot].PositionGroup == MvpSquadPositionGroup.Goalkeeper);

        Assert.Equal(MatchSelection.StartingXiSize, selection.StartingSlotIndices.Count);
        Assert.Equal(1, goalkeeperCount);
    }
}
