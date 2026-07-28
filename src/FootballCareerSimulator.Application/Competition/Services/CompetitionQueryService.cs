namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;

public sealed class CompetitionQueryService
{
    private readonly ILeagueCompetitionStore _store;

    public CompetitionQueryService(ILeagueCompetitionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public CurrentSeasonReadModel? GetCurrentSeason()
    {
        var season = _store.League.CurrentSeason;
        return season is null ? null : MapSeason(season);
    }

    public CurrentSeasonReadModel? GetSeason(long seasonId)
    {
        var season = FindSeason(seasonId);
        return season is null ? null : MapSeason(season);
    }

    public IReadOnlyList<SeasonParticipantReadModel> GetSeasonParticipants(long seasonId)
    {
        var season = GetSeasonOrThrow(seasonId);
        return season.Participants
            .Select(participant => new SeasonParticipantReadModel(participant.ClubId.Value))
            .ToArray();
    }

    public IReadOnlyList<FixtureReadModel> GetSeasonFixtures(long seasonId)
    {
        var season = GetSeasonOrThrow(seasonId);
        return season.Fixtures.Select(MapFixture).ToArray();
    }

    public IReadOnlyList<FixtureReadModel> GetFixturesByRound(long seasonId, int round)
    {
        _ = new FixtureRound(round);
        var season = GetSeasonOrThrow(seasonId);

        return season.Fixtures
            .Where(fixture => fixture.Round.Value == round)
            .Select(MapFixture)
            .ToArray();
    }

    public IReadOnlyList<StandingEntryReadModel> GetStandings(long seasonId)
    {
        var season = GetSeasonOrThrow(seasonId);
        return season.Standings.Entries
            .Select(entry => new StandingEntryReadModel(
                entry.ClubId.Value,
                entry.Played,
                entry.Won,
                entry.Drawn,
                entry.Lost,
                entry.GoalsFor,
                entry.GoalsAgainst,
                entry.Points.Value,
                entry.GoalDifference))
            .ToArray();
    }

    public StandingStripReadModel GetStandingsStrip(
        long seasonId,
        long? managedClubId = null,
        int topCount = 5)
    {
        if (topCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(topCount));
        }

        var standings = GetStandings(seasonId);
        var ranked = standings
            .Select((entry, index) => (
                Rank: index + 1,
                entry.ClubId,
                entry.Points,
                entry.Played,
                IsManaged: managedClubId is long managed && managed == entry.ClubId))
            .ToArray();

        if (ranked.Length == 0)
        {
            return new StandingStripReadModel(Array.Empty<StandingStripEntryReadModel>(), false);
        }

        var top = ranked
            .Take(topCount)
            .Select(entry => new StandingStripEntryReadModel(
                entry.Rank,
                entry.ClubId,
                entry.Points,
                entry.Played,
                entry.IsManaged))
            .ToList();

        var managedOutsideTop = false;
        if (managedClubId is long clubId)
        {
            var managed = ranked.FirstOrDefault(entry => entry.ClubId == clubId);
            if (managed.Rank > 0 && managed.Rank > topCount)
            {
                managedOutsideTop = true;
                top.Add(new StandingStripEntryReadModel(
                    managed.Rank,
                    managed.ClubId,
                    managed.Points,
                    managed.Played,
                    IsManaged: true));
            }
        }

        return new StandingStripReadModel(top, managedOutsideTop);
    }

    public SeasonProgressReadModel? GetSeasonProgress(long seasonId)
    {
        var season = FindSeason(seasonId);
        if (season is null)
        {
            return null;
        }

        var accepted = season.CountAcceptedFixtures();
        var total = season.Fixtures.Count;
        return new SeasonProgressReadModel(
            season.SeasonId.Value,
            season.Status.ToString(),
            accepted,
            total,
            CanComplete: season.Status is SeasonStatus.Active && total > 0 && accepted == total,
            CanArchive: season.Status is SeasonStatus.Completed);
    }

    private CompetitionSeason GetSeasonOrThrow(long seasonId) =>
        FindSeason(seasonId)
        ?? throw new CompetitionInvariantViolationException($"Season {seasonId} was not found.");

    private CompetitionSeason? FindSeason(long seasonId) =>
        _store.League.Seasons.FirstOrDefault(season => season.SeasonId.Value == seasonId);

    private static CurrentSeasonReadModel MapSeason(CompetitionSeason season) =>
        new(
            season.SeasonId.Value,
            season.CompetitionId.Value,
            season.Status.ToString(),
            season.PreseasonStartDate.DayNumber,
            season.ActiveStartedAt?.DayNumber,
            season.Participants.Count,
            season.Fixtures.Count);

    private static FixtureReadModel MapFixture(Fixture fixture) =>
        new(
            fixture.Id.Value,
            fixture.SeasonId.Value,
            fixture.HomeClubId.Value,
            fixture.AwayClubId.Value,
            fixture.Round.Value,
            fixture.ScheduledDate.DayNumber,
            fixture.ScheduledDate.ToIsoDateString(),
            fixture.Status.ToString(),
            fixture.HomeGoals,
            fixture.AwayGoals);
}
