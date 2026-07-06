using System.Security.Cryptography;
using System.Text;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Simulation.WorldCalendar;

public static class WorldTimelineCanonicalStateHasher
{
    public static string ComputeHash(WorldTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        var canonicalText = BuildCanonicalText(timeline);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText));
        return Convert.ToHexString(hashBytes);
    }

    public static string BuildCanonicalText(WorldTimeline timeline)
    {
        var builder = new StringBuilder();
        builder.Append("DayNumber=").Append(timeline.CurrentDate.DayNumber).Append(';');
        builder.Append("LastStep=").Append(timeline.LastCommittedStepId.Value).Append(';');
        builder.Append("RootSeed=").Append(timeline.RootSeed).Append(';');
        builder.Append("RngVersion=").Append(timeline.RngVersion).Append(';');
        builder.Append("RngDrawCount=").Append(timeline.RngDrawCount).Append(';');

        if (timeline.ActivePlanningPeriod is { } period)
        {
            builder.Append("PlanningPeriodId=").Append(period.Id.Value).Append(';');
            builder.Append("PlanningPeriodStatus=").Append(period.Status).Append(';');
            builder.Append("PlanningPeriodStart=").Append(period.StartDate.DayNumber).Append(';');
        }

        return builder.ToString();
    }
}
