namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;

internal static class CompetitionSeasonCommandSupport
{
    public static CompetitionSeason GetSeasonOrThrow(ILeagueCompetitionStore store, long seasonId) =>
        store.League.Seasons.FirstOrDefault(season => season.SeasonId.Value == seasonId)
        ?? throw new CompetitionInvariantViolationException($"Season {seasonId} was not found.");

    public static GameDate ToGameDate(int dayNumber)
    {
        try
        {
            return GameDate.FromDayNumber(dayNumber);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new CompetitionInvariantViolationException(ex.Message);
        }
    }
}
