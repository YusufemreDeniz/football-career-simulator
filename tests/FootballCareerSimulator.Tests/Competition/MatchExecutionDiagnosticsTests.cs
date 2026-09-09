using System.Diagnostics;
using FootballCareerSimulator.Application.Competition.Services;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class MatchExecutionDiagnosticsTests
{
    [Fact]
    public void StartPhase_WithListener_EmitsPhaseAndCorrelationTags()
    {
        const long seasonId = 37;
        const long fixtureId = 913_771;
        Activity? stopped = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == MatchExecutionDiagnostics.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (Equals(activity.GetTagItem("fcs.fixture.id"), fixtureId))
                {
                    stopped = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        using (var activity = MatchExecutionDiagnostics.StartPhase("simulation.test", seasonId, fixtureId))
        {
            Assert.NotNull(activity);
        }

        Assert.NotNull(stopped);
        Assert.Equal("simulation.test", stopped.OperationName);
        Assert.Equal(seasonId, stopped.GetTagItem("fcs.season.id"));
        Assert.Equal(fixtureId, stopped.GetTagItem("fcs.fixture.id"));
        Assert.True(stopped.Duration >= TimeSpan.Zero);
    }
}
