using FootballCareerSimulator.Application.Career.Services;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.Competition.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Services;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Simulation;
using Godot;
namespace FootballCareerSimulator.Presentation;

/// <summary>
/// World Calendar + Competition birleşik Godot composition root (Competition Kart C6).
/// </summary>
public sealed class CareerPresentationHost
{
    public CareerPresentationHost(
        WorldCalendarModule worldModule,
        CompetitionModule competitionModule,
        ClubGovernanceModule clubModule,
        ManagerCareerModule managerModule,
        SquadQueryService squadQueries,
        CareerGameSessionService gameSession,
        string defaultSavePath)
    {
        WorldModule = worldModule ?? throw new ArgumentNullException(nameof(worldModule));
        CompetitionModule = competitionModule ?? throw new ArgumentNullException(nameof(competitionModule));
        ClubModule = clubModule ?? throw new ArgumentNullException(nameof(clubModule));
        ManagerModule = managerModule ?? throw new ArgumentNullException(nameof(managerModule));
        SquadQueries = squadQueries ?? throw new ArgumentNullException(nameof(squadQueries));
        GameSession = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
        DefaultSavePath = defaultSavePath ?? throw new ArgumentNullException(nameof(defaultSavePath));
    }

    public WorldCalendarModule WorldModule { get; }

    public CompetitionModule CompetitionModule { get; }

    public ClubGovernanceModule ClubModule { get; }

    public ManagerCareerModule ManagerModule { get; }

    public SquadQueryService SquadQueries { get; }

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
        var managerModule = ManagerCareerModule.CreateNewCareer(startDate);
        var squadQueries = new SquadQueryService();

        var worldModule = WorldCalendarModule.Create(
            startDate,
            rootSeed: 42,
            blockerSources:
            [
                new UnplayedFixturesTimeAdvanceBlockerSource(competitionStore, timelineStore),
            ],
            timelineStore: timelineStore);

        var competitionModule = CompetitionModule.CreateForCareerFromStore(
            competitionStore,
            worldModule.TimelineStore,
            clubModule.Store);
        var persistence = new CareerSqlitePersistence();

        ICommandIdempotencyReset[] idempotencyResets =
        [
            worldModule.AdvanceSimulationTime,
            worldModule.OpenPlanningPeriod,
            worldModule.CompletePlanningPeriod,
            .. competitionModule.IdempotencyResets,
        ];

        var gameSession = new CareerGameSessionService(
            worldModule.TimelineStore,
            competitionModule.Store,
            clubModule.Store,
            managerModule.Store,
            persistence,
            idempotencyResets);

        var savePath = defaultSavePath ?? Path.Combine(OS.GetUserDataDir(), "career_save.db");
        return new CareerPresentationHost(
            worldModule,
            competitionModule,
            clubModule,
            managerModule,
            squadQueries,
            gameSession,
            savePath);
    }
}
