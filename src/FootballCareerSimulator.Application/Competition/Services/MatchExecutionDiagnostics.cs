using System.Diagnostics;

namespace FootballCareerSimulator.Application.Competition.Services;

/// <summary>
/// Lightweight match-pipeline diagnostics. When no ActivityListener subscribes,
/// StartActivity returns null and the production hot path does not allocate an activity.
/// </summary>
public static class MatchExecutionDiagnostics
{
    public const string ActivitySourceName = "FootballCareerSimulator.MatchExecution";

    private static readonly ActivitySource Source = new(ActivitySourceName, "1.0.0");

    public static Activity? StartPhase(string phase, long seasonId, long fixtureId)
    {
        if (string.IsNullOrWhiteSpace(phase))
        {
            throw new ArgumentException("Match execution phase is required.", nameof(phase));
        }

        var activity = Source.StartActivity(phase, ActivityKind.Internal);
        activity?.SetTag("fcs.season.id", seasonId);
        activity?.SetTag("fcs.fixture.id", fixtureId);
        return activity;
    }
}
