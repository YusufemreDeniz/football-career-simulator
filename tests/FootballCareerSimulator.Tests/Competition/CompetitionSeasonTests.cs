using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Competition.Events;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Competition;

public class CompetitionSeasonTests
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly CompetitionId Competition = new(1);
    private static readonly SeasonId Season = new(1);

    private static CompetitionSeason CreateSeason() =>
        CompetitionSeason.Create(Competition, Season, PreseasonStart);

    private static void RegisterFullLeague(CompetitionSeason season)
    {
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            season.RegisterParticipant(new ClubId(club));
        }
    }

    [Fact]
    public void Create_StartsInPreseasonWithNoParticipants()
    {
        var season = CreateSeason();

        Assert.Equal(SeasonStatus.Preseason, season.Status);
        Assert.Empty(season.Participants);
    }

    [Fact]
    public void RegisterParticipant_RaisesDomainEventAndTracksClub()
    {
        var season = CreateSeason();

        season.RegisterParticipant(new ClubId(1));

        Assert.Single(season.Participants);
        Assert.Contains(season.UncommittedEvents, e => e is SeasonParticipantRegistered registered && registered.ClubId == new ClubId(1));
    }

    [Fact]
    public void RegisterParticipant_RejectsDuplicateClub()
    {
        var season = CreateSeason();
        season.RegisterParticipant(new ClubId(1));

        Assert.Throws<CompetitionInvariantViolationException>(() => season.RegisterParticipant(new ClubId(1)));
    }

    [Fact]
    public void RegisterParticipant_RejectsMoreThanMaxLeagueTeamCount()
    {
        var season = CreateSeason();
        for (var club = 1L; club <= CompetitionMvpConstraints.MaxLeagueTeamCount; club++)
        {
            season.RegisterParticipant(new ClubId(club));
        }

        Assert.Throws<CompetitionInvariantViolationException>(() =>
            season.RegisterParticipant(new ClubId(CompetitionMvpConstraints.MaxLeagueTeamCount + 1)));
    }

    [Fact]
    public void StartActiveSeason_RequiresFullParticipantList()
    {
        var season = CreateSeason();

        Assert.Throws<CompetitionInvariantViolationException>(() =>
            season.StartActiveSeason(PreseasonStart));

        RegisterFullLeague(season);
        season.StartActiveSeason(PreseasonStart);

        Assert.Equal(SeasonStatus.Active, season.Status);
        Assert.Equal(PreseasonStart, season.ActiveStartedAt);
        Assert.Contains(season.UncommittedEvents, e => e is SeasonStarted);
    }

    private static void PlanAndFinishAllFixtures(CompetitionSeason season, GameDate firstMatchday)
    {
        season.PlanLeagueFixtures(firstMatchday, new FixtureId(1));
        foreach (var fixture in season.Fixtures)
        {
            season.AcceptFixtureResult(fixture.Id, new MatchScore(1, 0), fixture.ScheduledDate);
        }
    }

    private static void ActivateSeasonWithFixtures(CompetitionSeason season, GameDate firstMatchday)
    {
        RegisterFullLeague(season);
        season.StartActiveSeason(PreseasonStart);
        PlanAndFinishAllFixtures(season, firstMatchday);
    }

    [Fact]
    public void CompleteSeason_TransitionsFromActiveToCompleted()
    {
        var season = CreateSeason();
        ActivateSeasonWithFixtures(season, GameDate.FromCalendarDate(2026, 8, 1));

        var completedAt = GameDate.FromCalendarDate(2027, 5, 1);
        season.CompleteSeason(completedAt);

        Assert.Equal(SeasonStatus.Completed, season.Status);
        Assert.Equal(completedAt, season.CompletedAt);
        Assert.Contains(season.UncommittedEvents, e => e is SeasonCompleted);
    }

    [Fact]
    public void ArchiveSeason_TransitionsFromCompletedToArchived()
    {
        var season = CreateSeason();
        ActivateSeasonWithFixtures(season, GameDate.FromCalendarDate(2026, 8, 1));
        season.CompleteSeason(GameDate.FromCalendarDate(2027, 5, 1));

        var archivedAt = GameDate.FromCalendarDate(2027, 6, 1);
        season.ArchiveSeason(archivedAt);

        Assert.Equal(SeasonStatus.Archived, season.Status);
        Assert.Equal(archivedAt, season.ArchivedAt);
    }

    [Fact]
    public void RegisterParticipant_RejectsAfterSeasonBecomesActive()
    {
        var season = CreateSeason();
        RegisterFullLeague(season);
        season.StartActiveSeason(PreseasonStart);

        Assert.Throws<CompetitionInvariantViolationException>(() => season.RegisterParticipant(new ClubId(99)));
    }

    [Fact]
    public void CompleteSeason_RejectsWhenNotActive()
    {
        var season = CreateSeason();

        Assert.Throws<CompetitionInvariantViolationException>(() =>
            season.CompleteSeason(PreseasonStart));
    }
}
