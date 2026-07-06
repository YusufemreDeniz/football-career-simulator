namespace FootballCareerSimulator.Domain.Competition.Events;

using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

public abstract record CompetitionDomainEvent(GameDate OccurredAtGameTime);

public sealed record SeasonParticipantRegistered(
    GameDate OccurredAtGameTime,
    SeasonId SeasonId,
    ClubId ClubId)
    : CompetitionDomainEvent(OccurredAtGameTime);

public sealed record SeasonStarted(
    GameDate OccurredAtGameTime,
    CompetitionId CompetitionId,
    SeasonId SeasonId)
    : CompetitionDomainEvent(OccurredAtGameTime);

public sealed record SeasonCompleted(
    GameDate OccurredAtGameTime,
    CompetitionId CompetitionId,
    SeasonId SeasonId)
    : CompetitionDomainEvent(OccurredAtGameTime);

public sealed record SeasonArchived(
    GameDate OccurredAtGameTime,
    CompetitionId CompetitionId,
    SeasonId SeasonId)
    : CompetitionDomainEvent(OccurredAtGameTime);

public sealed record LeagueFixturesPlanned(
    GameDate OccurredAtGameTime,
    CompetitionId CompetitionId,
    SeasonId SeasonId,
    int FixtureCount,
    GameDate FirstMatchdayDate)
    : CompetitionDomainEvent(OccurredAtGameTime);
