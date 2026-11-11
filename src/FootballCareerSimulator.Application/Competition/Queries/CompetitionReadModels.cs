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
    string Status,
    int? HomeGoals,
    int? AwayGoals);

public sealed record StandingEntryReadModel(
    long ClubId,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int Points,
    int GoalDifference);

public sealed record StandingStripEntryReadModel(
    int Rank,
    long ClubId,
    int Points,
    int Played,
    bool IsManaged);

public sealed record StandingStripReadModel(
    IReadOnlyList<StandingStripEntryReadModel> Entries,
    bool ManagedOutsideTop);

public sealed record SeasonProgressReadModel(
    long SeasonId,
    string Status,
    int AcceptedFixtureCount,
    int TotalFixtureCount,
    bool CanComplete,
    bool CanArchive);
