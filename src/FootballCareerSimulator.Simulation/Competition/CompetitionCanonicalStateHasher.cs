using System.Security.Cryptography;
using System.Text;
using FootballCareerSimulator.Domain.Competition;

namespace FootballCareerSimulator.Simulation.Competition;

public static class CompetitionCanonicalStateHasher
{
    public static string ComputeHash(LeagueCompetition league)
    {
        ArgumentNullException.ThrowIfNull(league);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(BuildCanonicalText(league)));
        return Convert.ToHexString(hashBytes);
    }

    public static string BuildCanonicalText(LeagueCompetition league)
    {
        var builder = new StringBuilder();
        builder.Append("CompetitionId=").Append(league.CompetitionId.Value).Append(';');
        builder.Append("SeasonCount=").Append(league.Seasons.Count).Append(';');

        foreach (var season in league.Seasons.OrderBy(season => season.SeasonId.Value))
        {
            builder.Append("SeasonId=").Append(season.SeasonId.Value).Append(';');
            builder.Append("SeasonStatus=").Append(season.Status).Append(';');
            builder.Append("PreseasonStart=").Append(season.PreseasonStartDate.DayNumber).Append(';');
            builder.Append("ActiveStartedAt=").Append(season.ActiveStartedAt?.DayNumber).Append(';');
            builder.Append("CompletedAt=").Append(season.CompletedAt?.DayNumber).Append(';');
            builder.Append("ArchivedAt=").Append(season.ArchivedAt?.DayNumber).Append(';');
            builder.Append("ParticipantCount=").Append(season.Participants.Count).Append(';');

            foreach (var participant in season.Participants.OrderBy(participant => participant.ClubId.Value))
            {
                builder.Append("ClubId=").Append(participant.ClubId.Value).Append(';');
            }

            builder.Append("FixtureCount=").Append(season.Fixtures.Count).Append(';');

            foreach (var fixture in season.Fixtures.OrderBy(fixture => fixture.Id.Value))
            {
                builder.Append("FixtureId=").Append(fixture.Id.Value).Append(';');
                builder.Append("HomeClubId=").Append(fixture.HomeClubId.Value).Append(';');
                builder.Append("AwayClubId=").Append(fixture.AwayClubId.Value).Append(';');
                builder.Append("Round=").Append(fixture.Round.Value).Append(';');
                builder.Append("ScheduledDayNumber=").Append(fixture.ScheduledDate.DayNumber).Append(';');
                builder.Append("FixtureStatus=").Append(fixture.Status).Append(';');
                builder.Append("HomeGoals=").Append(fixture.HomeGoals).Append(';');
                builder.Append("AwayGoals=").Append(fixture.AwayGoals).Append(';');
            }
        }

        return builder.ToString();
    }
}
