namespace FootballCareerSimulator.Application.Competition.Queries;

public sealed record CurrentSeasonReadModel(
    long SeasonId,
    long CompetitionId,
    string Status,
    int PreseasonStartDayNumber,
    int? ActiveStartedAtDayNumber,
    int ParticipantCount,
    int FixtureCount);

public sealed record SeasonParticipantReadModel(long ClubId);

public sealed record FixtureReadModel(
    long FixtureId,
    long SeasonId,
    long HomeClubId,
    long AwayClubId,
    int Round,
    int ScheduledDayNumber,
    string ScheduledIsoDate,
    string Status);
