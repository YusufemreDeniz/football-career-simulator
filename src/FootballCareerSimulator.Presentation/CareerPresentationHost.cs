using FootballCareerSimulator.Application.Career.Services;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.Competition.Services;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Simulation;
using Godot;

namespace FootballCareerSimulator.Presentation;

public sealed class CareerPresentationHost
{
    public CareerPresentationHost(
        WorldCalendarModule worldModule,
        CompetitionModule competitionModule,
        ClubGovernanceModule clubModule,
        ManagerCareerModule managerModule,
        TeamPreparationModule teamPreparationModule,
        TrainingPhysicalStateModule trainingModule,
        PlayerCareerModule playerCareerModule,
        ContractRegistrationModule contractModule,
        CareerGameSessionService gameSession,
        string defaultSavePath)
    {
        WorldModule = worldModule ?? throw new ArgumentNullException(nameof(worldModule));
        CompetitionModule = competitionModule ?? throw new ArgumentNullException(nameof(competitionModule));
        ClubModule = clubModule ?? throw new ArgumentNullException(nameof(clubModule));
        ManagerModule = managerModule ?? throw new ArgumentNullException(nameof(managerModule));
        TeamPreparationModule = teamPreparationModule
            ?? throw new ArgumentNullException(nameof(teamPreparationModule));
        TrainingModule = trainingModule ?? throw new ArgumentNullException(nameof(trainingModule));
        PlayerCareerModule = playerCareerModule
            ?? throw new ArgumentNullException(nameof(playerCareerModule));
        ContractModule = contractModule ?? throw new ArgumentNullException(nameof(contractModule));
        GameSession = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
        DefaultSavePath = defaultSavePath ?? throw new ArgumentNullException(nameof(defaultSavePath));
    }

    public WorldCalendarModule WorldModule { get; }
    public CompetitionModule CompetitionModule { get; }
    public ClubGovernanceModule ClubModule { get; }
    public ManagerCareerModule ManagerModule { get; }
    public TeamPreparationModule TeamPreparationModule { get; }
    public TrainingPhysicalStateModule TrainingModule { get; }
    public PlayerCareerModule PlayerCareerModule { get; }
    public ContractRegistrationModule ContractModule { get; }
    public CareerGameSessionService GameSession { get; }
    public string DefaultSavePath { get; }

    public static CareerPresentationHost CreateDefault(string? defaultSavePath = null)
    {
        var startDate = GameDate.FromCalendarDate(2026, 7, 1);
        var timelineStore = new InMemoryWorldTimelineStore(
            WorldTimeline.Create(startDate, rootSeed: 42, SimulationRandomContext.Version));
        var competitionStore = new InMemoryLeagueCompetitionStore(
            new LeagueCompetition(new CompetitionId(MvpLeagueIdentity.DefaultCompetitionId)));
        var clubModule = ClubGovernanceModule.CreateMvpLeague();
        const long startingClubId = 1;
        var startingStrength = clubModule.Queries.GetClub(startingClubId)?.SportiveStrength ?? 50;
        var worldModule = WorldCalendarModule.Create(
            startDate,
            rootSeed: 42,
            blockerSources:
            [
                new UnplayedFixturesTimeAdvanceBlockerSource(competitionStore, timelineStore),
            ],
            timelineStore: timelineStore);

        var managerModule = ManagerCareerModule.CreateForCareer(
            startDate,
            clubModule.Store,
            worldModule.TimelineStore,
            startingClubId: startingClubId,
            clubSportiveStrength: startingStrength);

        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var playerStore = new InMemoryPlayerCareerStore();
        var contractModule = ContractRegistrationModule.Create(
            playerStore,
            managerModule.Store,
            worldModule.TimelineStore);
        var playerCareer = PlayerCareerModule.Create(
            managerModule.Store,
            worldModule.TimelineStore,
            trainingStore,
            playerStore,
            contractModule.Registration);
        var teamPreparation = TeamPreparationModule.Create(
            competitionStore,
            managerModule.Store,
            trainingStore: trainingStore,
            timelineStore: worldModule.TimelineStore,
            contractStore: contractModule.Store,
            playerCareerStore: playerStore);
        var training = TrainingPhysicalStateModule.Create(
            managerModule.Store,
            worldModule.TimelineStore,
            trainingStore,
            playerCareer.Development,
            teamPreparation.ClubSquad);

        var competitionModule = CompetitionModule.CreateForCareerFromStore(
            competitionStore,
            worldModule.TimelineStore,
            clubModule.Store,
            managerModule.Store,
            teamPreparation.SelectionStore,
            training.Store,
            playerCareer.Store,
            playerCareer.Development,
            teamPreparation.TacticPlanStore);
        var persistence = new CareerSqlitePersistence();

        ICommandIdempotencyReset[] idempotencyResets =
        [
            worldModule.AdvanceSimulationTime,
            worldModule.OpenPlanningPeriod,
            worldModule.CompletePlanningPeriod,
            .. competitionModule.IdempotencyResets,
            teamPreparation.IdempotencyReset,
            training.IdempotencyReset,
            .. managerModule.IdempotencyResets,
        ];

        var gameSession = new CareerGameSessionService(
            worldModule.TimelineStore,
            competitionModule.Store,
            clubModule.Store,
            managerModule.Store,
            teamPreparation.SelectionStore,
            teamPreparation.SquadStore,
            teamPreparation.TacticPlanStore,
            training.Store,
            playerCareer.Store,
            contractModule.Store,
            contractModule.FreeAgentStore,
            persistence,
            idempotencyResets);

        var savePath = defaultSavePath ?? Path.Combine(OS.GetUserDataDir(), "career_save.db");
        return new CareerPresentationHost(
            worldModule,
            competitionModule,
            clubModule,
            managerModule,
            teamPreparation,
            training,
            playerCareer,
            contractModule,
            gameSession,
            savePath);
    }
}
