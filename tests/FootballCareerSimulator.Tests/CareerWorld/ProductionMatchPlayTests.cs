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
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.CareerWorld;

public sealed class ProductionMatchPlayTests
{
    private const int Seed = 424242;
    private static readonly GameDate Opening = ProductionCareerWorldConstraints.DefaultOpeningDate;

    private static (
        CompetitionModule Competition,
        TeamPreparationModule TeamPrep,
        TrainingPhysicalStateModule Training) CreateStack(int seed)
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

        return (competition, teamPrep, training);
    }

    private static long FirstManagedFixtureId(CompetitionModule competition) =>
        competition.Queries.GetSeasonFixtures(1)
            .Where(fixture => fixture.HomeClubId == 1 || fixture.AwayClubId == 1)
            .OrderBy(fixture => fixture.ScheduledDayNumber)
            .ThenBy(fixture => fixture.FixtureId)
            .First()
            .FixtureId;

    [Fact]
    public void SameSeed_PlaysIdenticalManagedMatch()
    {
        var first = PlayDefaultManagedMatch(Seed);
        var second = PlayDefaultManagedMatch(Seed);

        Assert.Equal(first.HomeGoals, second.HomeGoals);
        Assert.Equal(first.AwayGoals, second.AwayGoals);
        Assert.Equal(first.KeyMomentFingerprint, second.KeyMomentFingerprint);
        Assert.False(string.IsNullOrWhiteSpace(first.PrimaryPlayerName));
        Assert.Contains(' ', first.PrimaryPlayerName);
    }

    [Fact]
    public void ProductionMatch_UpdatesStandings_AndRejectsDuplicateResult()
    {
        var (competition, teamPrep, _) = CreateStack(Seed);
        var fixtureId = FirstManagedFixtureId(competition);
        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));

        var result = competition.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(Guid.NewGuid(), 1, fixtureId, Opening.DayNumber));

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(FixtureStatus.ResultAccepted), result.Status);
        var standings = competition.Queries.GetStandings(1);
        Assert.Equal(2, standings.Count(entry => entry.Played > 0));
        Assert.Equal(2, standings.Sum(entry => entry.Played));

        var ex = Assert.Throws<CompetitionInvariantViolationException>(() =>
            competition.PlayFixtureMatch.Handle(
                new PlayFixtureMatchCommand(Guid.NewGuid(), 1, fixtureId, Opening.DayNumber)));
        Assert.Contains("planned fixture", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidInjuredLineup_IsRejected()
    {
        var (competition, teamPrep, training) = CreateStack(Seed);
        var fixtureId = FirstManagedFixtureId(competition);
        var clubId = new ClubId(1);
        training.Store.ReplacePhysicalStatesForClub(
            clubId,
            [
                PlayerPhysicalState.CreateRested(clubId, 0)
                    .WithInjury(InjurySeverity.Serious, Opening.AddDays(10)),
            ]);

        var starting = Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray();
        var bench = Enumerable.Range(MatchSelection.StartingXiSize, MatchSelection.MaxBenchSize).ToArray();

        Assert.Throws<TeamPreparationInvariantViolationException>(() =>
            teamPrep.ApproveSelection.Handle(
                new ApproveMatchSelectionCommand(
                    Guid.NewGuid(),
                    fixtureId,
                    ClubId: 1,
                    starting,
                    bench)));
    }

    [Fact]
    public void WeakLineupAndAttackingTactic_ChangeMatchInputs()
    {
        var club = new ClubId(1);
        var defaultBonus = MvpSquadStrengthCalculator.ComputeDefaultLineupBonus(club, Seed);
        var weakBonus = MvpSquadStrengthCalculator.ComputeLineupBonus(
            club,
            Seed,
            Enumerable.Range(12, MatchSelection.StartingXiSize).ToArray());
        Assert.NotEqual(defaultBonus, weakBonus);

        var (competition, teamPrep, _) = CreateStack(Seed);
        var fixtureId = FirstManagedFixtureId(competition);
        teamPrep.TacticPlans.SetApproach(club, TacticalApproach.Attacking, Opening);
        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));

        var played = competition.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(Guid.NewGuid(), 1, fixtureId, Opening.DayNumber));

        Assert.True(played.Succeeded);
        Assert.Equal(2, played.ManagedTacticModifier);
        Assert.NotNull(played.KeyMoments);
        Assert.Equal(
            played.HomeGoals + played.AwayGoals,
            played.KeyMoments!.Count(moment => moment.Kind == "Goal"));
    }

    [Fact]
    public void HalfTimeAttackIntervention_KeepsScoreConsistentWithGoals()
    {
        var (competition, teamPrep, _) = CreateStack(Seed + 1);
        var fixtureId = FirstManagedFixtureId(competition);
        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));
        var halfTime = competition.PlayFixtureMatch!.PreviewHalfTime(1, fixtureId, Opening.DayNumber);
        var swapped = teamPrep.SwapStarterWithBench.Handle(
            new SwapStarterWithBenchCommand(
                Guid.NewGuid(),
                fixtureId,
                ClubId: 1,
                StartingIndex: 10,
                BenchIndex: 0));

        var played = competition.PlayFixtureMatch.Handle(
            new PlayFixtureMatchCommand(
                Guid.NewGuid(),
                1,
                fixtureId,
                Opening.DayNumber,
                ManagedSecondHalfDelta: 2,
                ForcedHalfTimeHomeGoals: halfTime.HomeGoals,
                ForcedHalfTimeAwayGoals: halfTime.AwayGoals));

        Assert.True(swapped.Succeeded);
        Assert.True(played.Succeeded);
        Assert.Equal(
            played.HomeGoals + played.AwayGoals,
            played.KeyMoments!.Count(moment => moment.Kind == "Goal"));
        Assert.NotNull(played.Statistics);
        Assert.Equal(
            100,
            played.Statistics!.HomePossessionPercent + played.Statistics.AwayPossessionPercent);
    }

    private static (
        int HomeGoals,
        int AwayGoals,
        string KeyMomentFingerprint,
        string PrimaryPlayerName) PlayDefaultManagedMatch(int seed)
    {
        var (competition, teamPrep, _) = CreateStack(seed);
        var fixtureId = FirstManagedFixtureId(competition);
        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));
        var result = competition.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(Guid.NewGuid(), 1, fixtureId, Opening.DayNumber));
        Assert.True(result.Succeeded);
        var fingerprint = string.Join(
            '|',
            (result.KeyMoments ?? []).Select(moment =>
                $"{moment.Minute}:{moment.Kind}:{moment.PrimaryPlayerName}"));
        var name = result.KeyMoments?.FirstOrDefault()?.PrimaryPlayerName ?? "Ali Deneme";
        return (result.HomeGoals, result.AwayGoals, fingerprint, name);
    }
}
