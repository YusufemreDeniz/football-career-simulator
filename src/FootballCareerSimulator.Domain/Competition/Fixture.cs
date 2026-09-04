namespace FootballCareerSimulator.Domain.Competition;

using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Competition bounded context fixture aggregate (docs/03_DOMAIN_MODEL.md Bölüm 7.2, 12.4).
/// </summary>
public sealed class Fixture
{
    internal Fixture(
        FixtureId id,
        CompetitionId competitionId,
        SeasonId seasonId,
        ClubId homeClubId,
        ClubId awayClubId,
        FixtureRound round,
        GameDate scheduledDate,
        FixtureStatus status,
        int? homeGoals = null,
        int? awayGoals = null)
    {
        if (homeClubId == awayClubId)
        {
            throw new CompetitionInvariantViolationException(
                "A fixture cannot schedule the same club as both home and away.");
        }

        Id = id;
        CompetitionId = competitionId;
        SeasonId = seasonId;
        HomeClubId = homeClubId;
        AwayClubId = awayClubId;
        Round = round;
        ScheduledDate = scheduledDate;
        Status = status;
        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
    }

    public FixtureId Id { get; }

    public CompetitionId CompetitionId { get; }

    public SeasonId SeasonId { get; }

    public ClubId HomeClubId { get; }

    public ClubId AwayClubId { get; }

    public FixtureRound Round { get; }

    public GameDate ScheduledDate { get; }

    public FixtureStatus Status { get; private set; }

    public int? HomeGoals { get; private set; }

    public int? AwayGoals { get; private set; }

    public void AcceptResult(MatchScore score)
    {
        if (Status is not FixtureStatus.Planned)
        {
            throw new CompetitionInvariantViolationException(
                "Only a planned fixture can accept a match result.");
        }

        HomeGoals = score.HomeGoals;
        AwayGoals = score.AwayGoals;
        Status = FixtureStatus.ResultAccepted;
    }

    public static Fixture Rehydrate(
        FixtureId id,
        CompetitionId competitionId,
        SeasonId seasonId,
        ClubId homeClubId,
        ClubId awayClubId,
        FixtureRound round,
        GameDate scheduledDate,
        FixtureStatus status,
        int? homeGoals = null,
        int? awayGoals = null) =>
        new(id, competitionId, seasonId, homeClubId, awayClubId, round, scheduledDate, status, homeGoals, awayGoals);
}
