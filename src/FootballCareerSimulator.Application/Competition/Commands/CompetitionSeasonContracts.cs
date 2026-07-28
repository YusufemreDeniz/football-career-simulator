namespace FootballCareerSimulator.Application.Competition.Commands;

public sealed record CreateSeasonCommand(
    Guid CommandId,
    long SeasonId,
    int PreseasonStartDayNumber);

public sealed record CreateSeasonResult(
    bool Succeeded,
    long SeasonId,
    string Status);

public sealed record RegisterSeasonParticipantCommand(
    Guid CommandId,
    long SeasonId,
    long ClubId);

public sealed record RegisterSeasonParticipantResult(
    bool Succeeded,
    long SeasonId,
    long ClubId,
    int ParticipantCount);

public sealed record StartSeasonCommand(
    Guid CommandId,
    long SeasonId,
    int OccurredAtDayNumber);

public sealed record StartSeasonResult(
    bool Succeeded,
    long SeasonId,
    string Status);

public sealed record PlanLeagueFixturesCommand(
    Guid CommandId,
    long SeasonId,
    int FirstMatchdayDayNumber,
    long StartingFixtureId,
    int DaysBetweenRounds = 7);

public sealed record PlanLeagueFixturesResult(
    bool Succeeded,
    long SeasonId,
    int FixtureCount,
    int FirstMatchdayDayNumber);

public sealed record CompleteSeasonCommand(
    Guid CommandId,
    long SeasonId,
    int OccurredAtDayNumber);

public sealed record CompleteSeasonResult(
    bool Succeeded,
    long SeasonId,
    string Status);

public sealed record ArchiveSeasonCommand(
    Guid CommandId,
    long SeasonId,
    int OccurredAtDayNumber);

public sealed record ArchiveSeasonResult(
    bool Succeeded,
    long SeasonId,
    string Status);

public sealed record PlayFixtureMatchCommand(
    Guid CommandId,
    long SeasonId,
    long FixtureId,
    int OccurredAtDayNumber);

public sealed record PlayFixtureMatchResult(
    bool Succeeded,
    long SeasonId,
    long FixtureId,
    int HomeGoals,
    int AwayGoals,
    string Status,
    int InvalidatedSelectionCount = 0,
    int? ManagedTacticModifier = null);
