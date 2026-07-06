using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Infrastructure.Career;

internal static class CareerSnapshotMapper
{
    public static LeagueCompetition ToLeague(
        long competitionId,
        IReadOnlyList<SeasonSnapshotRow> seasons,
        IReadOnlyList<ParticipantSnapshotRow> participants,
        IReadOnlyList<FixtureSnapshotRow> fixtures)
    {
        var seasonsById = seasons
            .OrderBy(season => season.SeasonId)
            .Select(seasonRow =>
            {
                var seasonParticipants = participants
                    .Where(participant => participant.SeasonId == seasonRow.SeasonId)
                    .OrderBy(participant => participant.ClubId)
                    .Select(participant => SeasonParticipant.Rehydrate(new ClubId(participant.ClubId)))
                    .ToArray();

                var seasonFixtures = fixtures
                    .Where(fixture => fixture.SeasonId == seasonRow.SeasonId)
                    .OrderBy(fixture => fixture.FixtureId)
                    .Select(fixture => Fixture.Rehydrate(
                        new FixtureId(fixture.FixtureId),
                        new CompetitionId(competitionId),
                        new SeasonId(fixture.SeasonId),
                        new ClubId(fixture.HomeClubId),
                        new ClubId(fixture.AwayClubId),
                        new FixtureRound(fixture.Round),
                        GameDate.FromDayNumber(fixture.ScheduledDayNumber),
                        (FixtureStatus)fixture.Status))
                    .ToArray();

                return CompetitionSeason.Rehydrate(
                    new CompetitionId(competitionId),
                    new SeasonId(seasonRow.SeasonId),
                    GameDate.FromDayNumber(seasonRow.PreseasonStartDayNumber),
                    (SeasonStatus)seasonRow.Status,
                    seasonRow.ActiveStartedAtDayNumber is null
                        ? null
                        : GameDate.FromDayNumber(seasonRow.ActiveStartedAtDayNumber.Value),
                    seasonRow.CompletedAtDayNumber is null
                        ? null
                        : GameDate.FromDayNumber(seasonRow.CompletedAtDayNumber.Value),
                    seasonRow.ArchivedAtDayNumber is null
                        ? null
                        : GameDate.FromDayNumber(seasonRow.ArchivedAtDayNumber.Value),
                    seasonParticipants,
                    seasonFixtures);
            })
            .ToArray();

        return LeagueCompetition.Rehydrate(new CompetitionId(competitionId), seasonsById);
    }

    internal sealed record SeasonSnapshotRow(
        long SeasonId,
        int PreseasonStartDayNumber,
        int Status,
        int? ActiveStartedAtDayNumber,
        int? CompletedAtDayNumber,
        int? ArchivedAtDayNumber);

    internal sealed record ParticipantSnapshotRow(long SeasonId, long ClubId);

    internal sealed record FixtureSnapshotRow(
        long FixtureId,
        long SeasonId,
        long HomeClubId,
        long AwayClubId,
        int Round,
        int ScheduledDayNumber,
        int Status);
}
