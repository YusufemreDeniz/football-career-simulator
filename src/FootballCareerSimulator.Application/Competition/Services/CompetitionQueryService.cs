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
