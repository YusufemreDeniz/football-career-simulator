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

        if (career.ActiveEmployment is { } employment)
        {
            builder.Append("EmployedClubId=").Append(employment.ClubId.Value).Append(';');
            builder.Append("EmploymentStartedAt=").Append(employment.StartedAt.DayNumber).Append(';');
        }
        else
        {
            builder.Append("EmployedClubId=").Append(';');
            builder.Append("EmploymentStartedAt=").Append(';');
        }

        return builder.ToString();
    }
}
