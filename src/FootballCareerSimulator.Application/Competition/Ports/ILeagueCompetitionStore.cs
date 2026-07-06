namespace FootballCareerSimulator.Application.Competition.Ports;

using FootballCareerSimulator.Domain.Competition;

public interface ILeagueCompetitionStore
{
    LeagueCompetition League { get; }
}
