using FootballCareerSimulator.Application.Career.Services;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Services;
using FootballCareerSimulator.Infrastructure.Career;
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
        CareerGameSessionService gameSession,
        string defaultSavePath)
    {
        WorldModule = worldModule ?? throw new ArgumentNullException(nameof(worldModule));
        CompetitionModule = competitionModule ?? throw new ArgumentNullException(nameof(competitionModule));
        ClubModule = clubModule ?? throw new ArgumentNullException(nameof(clubModule));
        GameSession = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
        DefaultSavePath = defaultSavePath ?? throw new ArgumentNullException(nameof(defaultSavePath));
    }

    public WorldCalendarModule WorldModule { get; }

    public CompetitionModule CompetitionModule { get; }

    public ClubGovernanceModule ClubModule { get; }

    public CareerGameSessionService GameSession { get; }

    public string DefaultSavePath { get; }

    public static CareerPresentationHost CreateDefault(string? defaultSavePath = null)
    {
        var worldModule = WorldCalendarModule.CreateNewGame();
        var competitionModule = CompetitionModule.CreateNewLeague();
        var clubModule = ClubGovernanceModule.CreateMvpLeague();
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
            persistence,
            idempotencyResets);

        var savePath = defaultSavePath ?? Path.Combine(OS.GetUserDataDir(), "career_save.db");
        return new CareerPresentationHost(worldModule, competitionModule, clubModule, gameSession, savePath);
    }
}
