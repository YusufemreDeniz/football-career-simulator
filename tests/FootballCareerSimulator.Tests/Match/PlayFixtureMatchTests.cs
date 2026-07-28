using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.Match;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Tests.Match;

public sealed class MvpFixtureMatchSimulatorTests
{
    [Fact]
    public void Simulate_SameInputs_ProducesDeterministicScore()
    {
        var first = MvpFixtureMatchSimulator.Simulate(42, fixtureId: 7, homeStrength: 70, awayStrength: 55);
        var second = MvpFixtureMatchSimulator.Simulate(42, fixtureId: 7, homeStrength: 70, awayStrength: 55);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Simulate_ProducesGoalsWithinMvpRange()
    {
        var score = MvpFixtureMatchSimulator.Simulate(42, fixtureId: 5, homeStrength: 60, awayStrength: 60);

        Assert.InRange(score.HomeGoals, 0, 6);
        Assert.InRange(score.AwayGoals, 0, 6);
    }

    [Fact]
    public void SimulateWithKeyMoments_IsDeterministic_AndMatchesScore()
    {
        var first = MvpFixtureMatchSimulator.SimulateWithKeyMoments(
            42, fixtureId: 11, homeStrength: 75, awayStrength: 50);
        var second = MvpFixtureMatchSimulator.SimulateWithKeyMoments(
            42, fixtureId: 11, homeStrength: 75, awayStrength: 50);

        Assert.Equal(first.Score, second.Score);
        Assert.Equal(first.KeyMoments, second.KeyMoments);
        Assert.Equal(
            first.Score.HomeGoals,
            first.KeyMoments.Count(m => m.Kind == MatchKeyMomentKind.Goal && m.IsHomeSide));
        Assert.Equal(
            first.Score.AwayGoals,
            first.KeyMoments.Count(m => m.Kind == MatchKeyMomentKind.Goal && !m.IsHomeSide));
        Assert.Equal(first.Score, MvpFixtureMatchSimulator.Simulate(42, 11, 75, 50));
        Assert.True(first.KeyMoments.Select(m => m.Minute).SequenceEqual(
            first.KeyMoments.Select(m => m.Minute).OrderBy(m => m)));
        Assert.All(
            first.KeyMoments,
            moment =>
            {
                Assert.InRange(moment.Minute, MvpFixtureMatchSimulator.MinMomentMinute, MvpFixtureMatchSimulator.MaxMomentMinute);
                Assert.InRange(moment.PrimarySlotIndex, 0, MvpFixtureMatchSimulator.StartingXiSize - 1);
                if (moment.AssistSlotIndex is int assist)
                {
                    Assert.Equal(MatchKeyMomentKind.Goal, moment.Kind);
                    Assert.InRange(assist, 0, MvpFixtureMatchSimulator.StartingXiSize - 1);
                    Assert.NotEqual(moment.PrimarySlotIndex, assist);
                }
                else
                {
                    Assert.Null(moment.AssistSlotIndex);
                }
            });
        Assert.Equal(first.KeyMoments.Count, first.KeyMoments.Select(m => m.Minute).Distinct().Count());
        Assert.True(first.KeyMoments.Count(m => m.Kind is MatchKeyMomentKind.YellowCard or MatchKeyMomentKind.RedCard)
            <= MvpFixtureMatchSimulator.MaxCardsPerMatch);
    }

    [Fact]
    public void SimulateWithKeyMoments_CanProduceAssistAndCards()
    {
        MatchSimulationOutcome? withAssist = null;
        MatchSimulationOutcome? withCard = null;
        for (var seed = 1; seed <= 200 && (withAssist is null || withCard is null); seed++)
        {
            var outcome = MvpFixtureMatchSimulator.SimulateWithKeyMoments(
                seed, fixtureId: 3, homeStrength: 80, awayStrength: 70);
            if (withAssist is null
                && outcome.KeyMoments.Any(m => m.Kind == MatchKeyMomentKind.Goal && m.AssistSlotIndex is not null))
            {
                withAssist = outcome;
            }

            if (withCard is null
                && outcome.KeyMoments.Any(m => m.Kind is MatchKeyMomentKind.YellowCard or MatchKeyMomentKind.RedCard))
            {
                withCard = outcome;
            }
        }

        Assert.NotNull(withAssist);
        Assert.NotNull(withCard);
    }
}

public sealed class PlayFixtureMatchHandlerTests
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private static CompetitionModule CreateModule(int rootSeed = 42)
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: rootSeed);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        return CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
    }

    private static void RegisterFullLeague(CompetitionModule module, long seasonId)
    {
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            module.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }
    }

    private static void SetupActiveSeasonWithFixtures(CompetitionModule module, long seasonId)
    {
        module.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        RegisterFullLeague(module, seasonId);
        module.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        module.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                FirstMatchday.DayNumber,
                StartingFixtureId: 1));
    }

    [Fact]
    public void PlayFixtureMatch_UpdatesFixtureStatusAndStandings()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 99);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var module = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        const long seasonId = 1;
        SetupActiveSeasonWithFixtures(module, seasonId);

        var result = module.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(
                Guid.NewGuid(),
                seasonId,
                1,
                FirstMatchday.DayNumber));

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(FixtureStatus.ResultAccepted), result.Status);

        var fixture = module.Queries.GetSeasonFixtures(seasonId)[0];
        Assert.NotNull(result.KeyMoments);
        var homeNames = MvpSquadRosterGenerator.GeneratePlayerNames(new ClubId(fixture.HomeClubId), rootSeed: 99);
        var awayNames = MvpSquadRosterGenerator.GeneratePlayerNames(new ClubId(fixture.AwayClubId), rootSeed: 99);
        Assert.All(
            result.KeyMoments!,
            moment =>
            {
                var expected = moment.IsHomeSide
                    ? homeNames[moment.PrimarySlotIndex]
                    : awayNames[moment.PrimarySlotIndex];
                Assert.Equal(expected, moment.PrimaryPlayerName);
            });

        Assert.Equal(result.HomeGoals, fixture.HomeGoals);
        Assert.Equal(result.AwayGoals, fixture.AwayGoals);

        var standings = module.Queries.GetStandings(seasonId);
        Assert.Equal(2, standings.Count(entry => entry.Played > 0));
        Assert.Equal(2, standings.Sum(entry => entry.Played));
    }

    [Fact]
    public void PlayFixtureMatch_SameCommandId_IsIdempotent()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 11);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var module = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        const long seasonId = 1;
        var commandId = Guid.NewGuid();
        SetupActiveSeasonWithFixtures(module, seasonId);

        var command = new PlayFixtureMatchCommand(
            commandId,
            seasonId,
            1,
            FirstMatchday.DayNumber);

        var first = module.PlayFixtureMatch!.Handle(command);
        var second = module.PlayFixtureMatch.Handle(command);

        Assert.Equal(first, second);
    }
}

public sealed class SeasonStandingsTests
{
    [Fact]
    public void Rebuild_OrdersByPointsGoalDifferenceAndGoalsFor()
    {
        var participants = new[]
        {
            SeasonParticipant.Rehydrate(new Domain.Shared.ClubId(1)),
            SeasonParticipant.Rehydrate(new Domain.Shared.ClubId(2)),
        };
        var fixtures = new[]
        {
            Fixture.Rehydrate(
                new FixtureId(1),
                new CompetitionId(1),
                new SeasonId(1),
                new Domain.Shared.ClubId(1),
                new Domain.Shared.ClubId(2),
                new FixtureRound(1),
                GameDate.FromCalendarDate(2026, 8, 1),
                FixtureStatus.ResultAccepted,
                homeGoals: 2,
                awayGoals: 1),
        };

        var standings = SeasonStandings.Rebuild(participants.Select(participant => participant.ClubId), fixtures);

        Assert.Equal(2, standings.Entries.Count);
        Assert.Equal(1, standings.Entries[0].ClubId.Value);
        Assert.Equal(3, standings.Entries[0].Points.Value);
        Assert.Equal(0, standings.Entries[1].Points.Value);
    }
}
