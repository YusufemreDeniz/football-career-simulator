using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Competition;

public class LeagueCompetitionTests
{
    private static readonly CompetitionId Competition = new(1);
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);

    private static void RegisterFullLeague(CompetitionSeason season)
    {
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            season.RegisterParticipant(new ClubId(club));
        }
    }

    [Fact]
    public void CreateSeason_AddsPreseasonSeason()
    {
        var league = new LeagueCompetition(Competition);

        var season = league.CreateSeason(new SeasonId(1), PreseasonStart);

        Assert.Equal(SeasonStatus.Preseason, season.Status);
        Assert.Same(season, league.CurrentSeason);
    }

    [Fact]
    public void CreateSeason_RejectsWhileAnotherSeasonIsActive()
    {
        var league = new LeagueCompetition(Competition);
        var season = league.CreateSeason(new SeasonId(1), PreseasonStart);
        RegisterFullLeague(season);
        league.StartSeason(new SeasonId(1), PreseasonStart);

        Assert.Throws<CompetitionInvariantViolationException>(() =>
            league.CreateSeason(new SeasonId(2), PreseasonStart.AddDays(1)));
    }

    [Fact]
    public void StartSeason_RejectsWhenPreviousSeasonIsNotCompleted()
    {
        var season1 = CompetitionSeason.Rehydrate(
            Competition,
            new SeasonId(1),
            PreseasonStart,
            SeasonStatus.Active,
            activeStartedAt: PreseasonStart,
            completedAt: null,
            archivedAt: null,
            participants: Enumerable.Range(1, CompetitionMvpConstraints.LeagueTeamCount)
                .Select(index => SeasonParticipant.Rehydrate(new ClubId(index))));

        var season2 = CompetitionSeason.Create(Competition, new SeasonId(2), PreseasonStart.AddDays(1));
        var league = LeagueCompetition.Rehydrate(Competition, [season1, season2]);

        Assert.Throws<CompetitionInvariantViolationException>(() =>
            league.StartSeason(new SeasonId(2), PreseasonStart.AddDays(1)));
    }

    [Fact]
    public void StartSeason_RejectsUnknownSeason()
    {
        var league = new LeagueCompetition(Competition);

        Assert.Throws<CompetitionInvariantViolationException>(() =>
            league.StartSeason(new SeasonId(99), PreseasonStart));
    }

    [Fact]
    public void CreateSeason_AllowsNewSeasonAfterPreviousIsArchived()
    {
        var league = new LeagueCompetition(Competition);
        var first = league.CreateSeason(new SeasonId(1), PreseasonStart);
        RegisterFullLeague(first);
        league.StartSeason(new SeasonId(1), PreseasonStart);
        league.CompleteSeason(new SeasonId(1), GameDate.FromCalendarDate(2027, 5, 1));
        league.ArchiveSeason(new SeasonId(1), GameDate.FromCalendarDate(2027, 6, 1));

        var second = league.CreateSeason(new SeasonId(2), GameDate.FromCalendarDate(2027, 7, 1));

        Assert.Equal(new SeasonId(2), second.SeasonId);
        Assert.Equal(2, league.Seasons.Count);
    }

    [Fact]
    public void CreateSeason_RejectsWhilePreviousSeasonIsOnlyCompleted()
    {
        var league = new LeagueCompetition(Competition);
        var first = league.CreateSeason(new SeasonId(1), PreseasonStart);
        RegisterFullLeague(first);
        league.StartSeason(new SeasonId(1), PreseasonStart);
        league.CompleteSeason(new SeasonId(1), GameDate.FromCalendarDate(2027, 5, 1));

        Assert.Throws<CompetitionInvariantViolationException>(() =>
            league.CreateSeason(new SeasonId(2), GameDate.FromCalendarDate(2027, 7, 1)));
    }
}
