using System.Text;
using ManagerCareerState = FootballCareerSimulator.Domain.ManagerCareer.ManagerCareer;

namespace FootballCareerSimulator.Simulation.ManagerCareer;

public static class ManagerCareerCanonicalStateHasher
{
    public static string BuildCanonicalText(ManagerCareerState career)
    {
        ArgumentNullException.ThrowIfNull(career);

        var builder = new StringBuilder();
        builder.Append("ManagerId=").Append(career.ManagerId.Value).Append(';');
        builder.Append("DisplayName=").Append(career.DisplayName).Append(';');
        builder.Append("EmploymentStatus=").Append((int)career.EmploymentStatus).Append(';');
        builder.Append("EmploymentEndReason=")
            .Append(career.TerminationReason is { } reason ? ((int)reason).ToString() : string.Empty)
            .Append(';');
        builder.Append("LastClubId=")
            .Append(career.LastClubId?.Value.ToString() ?? string.Empty)
            .Append(';');
        builder.Append("DismissedDueToFixtureId=")
            .Append(career.DismissedDueToFixtureId?.Value.ToString() ?? string.Empty)
            .Append(';');
        builder.Append("DismissedAt=")
            .Append(career.DismissedAt?.DayNumber.ToString() ?? string.Empty)
            .Append(';');
        builder.Append("ManagerReputation=").Append(career.Reputation.Value).Append(';');
        builder.Append("LastReputationReasonCode=")
            .Append(career.LastReputationReasonCode ?? string.Empty)
            .Append(';');
        builder.Append("StartingBackground=")
            .Append(career.StartingBackground is { } background ? ((int)background).ToString() : string.Empty)
            .Append(';');

        if (career.PendingJobOffer is { } offer)
        {
            builder.Append("PendingOfferId=").Append(offer.Id.Value).Append(';');
            builder.Append("PendingOfferClubId=").Append(offer.ClubId.Value).Append(';');
            builder.Append("PendingOfferStatus=").Append((int)offer.Status).Append(';');
            builder.Append("PendingOfferCreatedAt=").Append(offer.CreatedAt.DayNumber).Append(';');
        }
        else
        {
            builder.Append("PendingOfferId=").Append(';');
            builder.Append("PendingOfferClubId=").Append(';');
            builder.Append("PendingOfferStatus=").Append(';');
            builder.Append("PendingOfferCreatedAt=").Append(';');
        }

        if (career.ActiveEmployment is { } employment)
        {
            builder.Append("EmployedClubId=").Append(employment.ClubId.Value).Append(';');
            builder.Append("EmploymentStartedAt=").Append(employment.StartedAt.DayNumber).Append(';');
            builder.Append("SeasonExpectation=").Append((int)employment.SeasonExpectation).Append(';');
            builder.Append("BoardConfidence=").Append(employment.BoardConfidence.Value).Append(';');
            builder.Append("RiskBand=").Append((int)employment.RiskBand).Append(';');
            builder.Append("LastAssessedFixtureId=")
                .Append(employment.LastAssessedFixtureId?.Value.ToString() ?? string.Empty)
                .Append(';');
            builder.Append("LastAssessmentReasonCode=")
                .Append(employment.LastAssessmentReasonCode ?? string.Empty)
                .Append(';');
        }
        else
        {
            builder.Append("EmployedClubId=").Append(';');
            builder.Append("EmploymentStartedAt=").Append(';');
            builder.Append("SeasonExpectation=").Append(';');
            builder.Append("BoardConfidence=").Append(';');
            builder.Append("RiskBand=").Append(';');
            builder.Append("LastAssessedFixtureId=").Append(';');
            builder.Append("LastAssessmentReasonCode=").Append(';');
        }

        foreach (var entry in career.EmploymentHistory)
        {
            builder.Append("HistoryClub=").Append(entry.ClubId.Value).Append(';');
            builder.Append("HistoryStart=").Append(entry.StartedAt.DayNumber).Append(';');
            builder.Append("HistoryEnd=").Append(entry.EndedAt.DayNumber).Append(';');
            builder.Append("HistoryReason=").Append((int)entry.EndReason).Append(';');
            builder.Append("HistoryConfidence=").Append(entry.FinalBoardConfidence).Append(';');
            builder.Append("HistoryFixture=")
                .Append(entry.CausationFixtureId?.Value.ToString() ?? string.Empty)
                .Append(';');
            builder.Append("HistoryAssessment=")
                .Append(entry.FinalAssessmentReasonCode ?? string.Empty)
                .Append(';');
        }

        return builder.ToString();
    }
}
