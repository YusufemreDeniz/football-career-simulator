namespace FootballCareerSimulator.Application.Competition.Composition;

using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.Competition.Services;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;

/// <summary>
/// Manuel composition root (D-348).
/// </summary>
public sealed class CompetitionModule
{
    public CompetitionModule(
        ILeagueCompetitionStore store,
        CreateSeasonHandler createSeason,
        RegisterSeasonParticipantHandler registerSeasonParticipant,
        StartSeasonHandler startSeason,
        PlanLeagueFixturesHandler planLeagueFixtures,
        CompleteSeasonHandler completeSeason,
        ArchiveSeasonHandler archiveSeason,
        CompetitionQueryService queries)
    {
        Store = store;
        CreateSeason = createSeason;
        RegisterSeasonParticipant = registerSeasonParticipant;
        StartSeason = startSeason;
        PlanLeagueFixtures = planLeagueFixtures;
        CompleteSeason = completeSeason;
        ArchiveSeason = archiveSeason;
        Queries = queries;
        IdempotencyResets =
        [
            createSeason,
            registerSeasonParticipant,
            startSeason,
            planLeagueFixtures,
            completeSeason,
            archiveSeason,
        ];
    }

    public ILeagueCompetitionStore Store { get; }

    public CreateSeasonHandler CreateSeason { get; }

    public RegisterSeasonParticipantHandler RegisterSeasonParticipant { get; }

    public StartSeasonHandler StartSeason { get; }

    public PlanLeagueFixturesHandler PlanLeagueFixtures { get; }

    public CompleteSeasonHandler CompleteSeason { get; }

    public ArchiveSeasonHandler ArchiveSeason { get; }

    public CompetitionQueryService Queries { get; }

    public IReadOnlyList<ICommandIdempotencyReset> IdempotencyResets { get; }

    public static CompetitionModule CreateNewLeague(long competitionId = 1)
    {
        var league = new LeagueCompetition(new CompetitionId(competitionId));
        var store = new InMemoryLeagueCompetitionStore(league);
        var createSeason = new CreateSeasonHandler(store);
        var registerSeasonParticipant = new RegisterSeasonParticipantHandler(store);
        var startSeason = new StartSeasonHandler(store);
        var planLeagueFixtures = new PlanLeagueFixturesHandler(store);
        var completeSeason = new CompleteSeasonHandler(store);
        var archiveSeason = new ArchiveSeasonHandler(store);

        return new CompetitionModule(
            store,
            createSeason,
            registerSeasonParticipant,
            startSeason,
            planLeagueFixtures,
            completeSeason,
            archiveSeason,
            new CompetitionQueryService(store));
    }
}
