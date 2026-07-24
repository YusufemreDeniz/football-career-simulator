namespace FootballCareerSimulator.Application.Competition.Composition;

using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.Competition.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
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
        PlayFixtureMatchHandler? playFixtureMatch,
        CompetitionQueryService queries)
    {
        Store = store;
        CreateSeason = createSeason;
        RegisterSeasonParticipant = registerSeasonParticipant;
        StartSeason = startSeason;
        PlanLeagueFixtures = planLeagueFixtures;
        CompleteSeason = completeSeason;
        ArchiveSeason = archiveSeason;
        PlayFixtureMatch = playFixtureMatch;
        Queries = queries;
        var resets = new List<ICommandIdempotencyReset>
        {
            createSeason,
            registerSeasonParticipant,
            startSeason,
            planLeagueFixtures,
            completeSeason,
            archiveSeason,
        };

        if (playFixtureMatch is not null)
        {
            resets.Add(playFixtureMatch);
        }

        IdempotencyResets = resets;
    }

    public ILeagueCompetitionStore Store { get; }

    public CreateSeasonHandler CreateSeason { get; }

    public RegisterSeasonParticipantHandler RegisterSeasonParticipant { get; }

    public StartSeasonHandler StartSeason { get; }

    public PlanLeagueFixturesHandler PlanLeagueFixtures { get; }

    public CompleteSeasonHandler CompleteSeason { get; }

    public ArchiveSeasonHandler ArchiveSeason { get; }

    public PlayFixtureMatchHandler? PlayFixtureMatch { get; }

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
            playFixtureMatch: null,
            new CompetitionQueryService(store));
    }

    public static CompetitionModule CreateForCareer(
        IWorldTimelineStore timelineStore,
        IClubRegistryStore clubRegistryStore,
        IManagerCareerStore? managerCareerStore = null,
        IMatchSelectionStore? matchSelectionStore = null,
        ITrainingPhysicalStateStore? trainingStore = null,
        IPlayerCareerStore? playerCareerStore = null,
        PlayerCareerDevelopmentService? playerDevelopment = null,
        ITacticPlanStore? tacticPlanStore = null,
        IClubSquadStore? clubSquadStore = null,
        StartingOpportunityPromiseService? startingOpportunityPromises = null,
        SelectionMemoryService? selectionMemory = null,
        PlayingTimePromiseService? playingTimePromises = null,
        PromiseInvalidationService? promiseInvalidation = null,
        CareerMemoryService? careerMemory = null,
        long competitionId = 1)
    {
        var league = new LeagueCompetition(new CompetitionId(competitionId));
        var store = new InMemoryLeagueCompetitionStore(league);
        return CreateForCareerFromStore(
            store,
            timelineStore,
            clubRegistryStore,
            managerCareerStore,
            matchSelectionStore,
            trainingStore,
            playerCareerStore,
            playerDevelopment,
            tacticPlanStore,
            clubSquadStore,
            startingOpportunityPromises,
            selectionMemory,
            playingTimePromises,
            promiseInvalidation,
            careerMemory);
    }

    public static CompetitionModule CreateForCareerFromStore(
        ILeagueCompetitionStore store,
        IWorldTimelineStore timelineStore,
        IClubRegistryStore clubRegistryStore,
        IManagerCareerStore? managerCareerStore = null,
        IMatchSelectionStore? matchSelectionStore = null,
        ITrainingPhysicalStateStore? trainingStore = null,
        IPlayerCareerStore? playerCareerStore = null,
        PlayerCareerDevelopmentService? playerDevelopment = null,
        ITacticPlanStore? tacticPlanStore = null,
        IClubSquadStore? clubSquadStore = null,
        StartingOpportunityPromiseService? startingOpportunityPromises = null,
        SelectionMemoryService? selectionMemory = null,
        PlayingTimePromiseService? playingTimePromises = null,
        PromiseInvalidationService? promiseInvalidation = null,
        CareerMemoryService? careerMemory = null)
    {
        var createSeason = new CreateSeasonHandler(store);
        var registerSeasonParticipant = new RegisterSeasonParticipantHandler(store);
        var startSeason = new StartSeasonHandler(store);
        var planLeagueFixtures = new PlanLeagueFixturesHandler(store);
        var completeSeason = new CompleteSeasonHandler(store);
        var archiveSeason = new ArchiveSeasonHandler(store);
        var playFixtureMatch = new PlayFixtureMatchHandler(
            store,
            clubRegistryStore,
            timelineStore,
            managerCareerStore,
            matchSelectionStore,
            trainingStore,
            playerCareerStore,
            playerDevelopment,
            tacticPlanStore,
            clubSquadStore,
            startingOpportunityPromises,
            selectionMemory,
            playingTimePromises,
            promiseInvalidation,
            careerMemory);

        return new CompetitionModule(
            store,
            createSeason,
            registerSeasonParticipant,
            startSeason,
            planLeagueFixtures,
            completeSeason,
            archiveSeason,
            playFixtureMatch,
            new CompetitionQueryService(store));
    }
}
