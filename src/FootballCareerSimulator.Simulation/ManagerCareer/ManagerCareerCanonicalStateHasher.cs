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

        return builder.ToString();
    }
}
