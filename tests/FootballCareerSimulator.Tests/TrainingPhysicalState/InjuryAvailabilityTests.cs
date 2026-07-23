using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.TrainingPhysicalState;

public sealed class InjuryAvailabilityTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-injury",
        Guid.NewGuid().ToString("N"));

    public InjuryAvailabilityTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ShouldInjure_RespectsRiskThreshold()
    {
        Assert.True(MvpInjuryRiskEvaluator.ShouldInjure(riskPercent: 20, roll0To99: 5));
        Assert.False(MvpInjuryRiskEvaluator.ShouldInjure(riskPercent: 20, roll0To99: 20));
        Assert.True(MvpInjuryRiskEvaluator.ComputeTrainingRiskPercent(80, TrainingIntensity.High) >= 24);
    }

    [Fact]
    public void RecoverIfDue_ClearsExpiredInjury()
    {
        var injured = PlayerPhysicalState.CreateRested(new ClubId(1), 0)
            .WithInjury(InjurySeverity.Minor, Day.AddDays(3));

        Assert.False(injured.IsAvailableOn(Day));
        Assert.Equal(AvailabilityStatus.Unavailable, injured.GetAvailability(Day));

        var recovered = injured.RecoverIfDue(Day.AddDays(4));
        Assert.False(recovered.IsInjured);
        Assert.True(recovered.IsAvailableOn(Day.AddDays(4)));
    }

    [Fact]
    public void DefaultSelection_PrefersAvailableSlots()
    {
        var clubId = new ClubId(1);
        var physical = new Dictionary<(long, int), PlayerPhysicalState>
        {
            [(1, 0)] = PlayerPhysicalState.CreateRested(clubId, 0)
                .WithInjury(InjurySeverity.Serious, Day.AddDays(14)),
            [(1, 1)] = PlayerPhysicalState.CreateRested(clubId, 1)
                .WithInjury(InjurySeverity.Moderate, Day.AddDays(7)),
        };

        var selection = MvpAvailabilityAwareSelection.ApproveDefaultPreferringAvailable(
            new FixtureId(9),
            clubId,
            Day,
            physical);

        Assert.DoesNotContain(0, selection.StartingSlotIndices);
        Assert.DoesNotContain(1, selection.StartingSlotIndices);
        Assert.DoesNotContain(0, selection.BenchSlotIndices);
        Assert.DoesNotContain(1, selection.BenchSlotIndices);
    }

    [Fact]
    public void UnavailableInXi_AppliesNegativeModifier()
    {
        var clubId = new ClubId(1);
        var physical = Enumerable.Range(0, MatchSelection.StartingXiSize)
            .ToDictionary(
                slot => (clubId.Value, slot),
                slot => PlayerPhysicalState.CreateRested(clubId, slot)
                    .WithInjury(InjurySeverity.Minor, Day.AddDays(3)));

        var modifier = MvpPhysicalMatchModifier.ComputeLineupModifier(
            clubId,
            Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray(),
            physical,
            Day);

        Assert.True(modifier < 0, $"Expected injury penalty, got {modifier}.");
    }

    [Fact]
    public void SaveLoad_PreservesInjuryFields()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 3);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var training = TrainingPhysicalStateModule.Create(manager.Store, world.TimelineStore);

        var injured = PlayerPhysicalState.CreateRested(new ClubId(1), 4)
            .WithLevels(40, 70)
            .WithInjury(InjurySeverity.Moderate, Day.AddDays(7));
        training.Store.ReplacePhysicalStatesForClub(new ClubId(1), [injured]);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "injury.db");
        persistence.Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            clubs.Store.Registry,
            manager.Store.Career,
            Array.Empty<MatchSelection>(),
            training.Store.Plans,
            training.Store.PhysicalStates);

        var loaded = persistence.Load(path);
        Assert.Equal(12, loaded.SchemaVersion);
        Assert.Single(loaded.PhysicalStates);
        Assert.Equal(InjurySeverity.Moderate, loaded.PhysicalStates[0].InjurySeverity);
        Assert.Equal(Day.AddDays(7).DayNumber, loaded.PhysicalStates[0].InjuredUntilDayNumber);
    }

    [Fact]
    public void ApproveDefault_SkipsInjuredSlots_InCareerStack()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 42);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var selectionStore = new InMemoryMatchSelectionStore();
        var training = TrainingPhysicalStateModule.Create(manager.Store, world.TimelineStore);
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
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, FirstMatchday.DayNumber, 1));

        training.Store.ReplacePhysicalStatesForClub(
            new ClubId(1),
            [
                PlayerPhysicalState.CreateRested(new ClubId(1), 0)
                    .WithInjury(InjurySeverity.Serious, Day.AddDays(14)),
            ]);

        var fixtureId = competition.Queries.GetSeasonFixtures(1)[0].FixtureId;
        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, 1));

        var selection = selectionStore.Get(new FixtureId(fixtureId), new ClubId(1))!;
        Assert.DoesNotContain(0, selection.StartingSlotIndices);
    }
}
