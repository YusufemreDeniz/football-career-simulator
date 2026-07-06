namespace FootballCareerSimulator.Application.Competition.Infrastructure;

using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Domain.Competition;

public sealed class InMemoryLeagueCompetitionStore : ILeagueCompetitionStore
{
    public InMemoryLeagueCompetitionStore(LeagueCompetition league)
    {
        League = league ?? throw new ArgumentNullException(nameof(league));
    }

    public LeagueCompetition League { get; }
}
