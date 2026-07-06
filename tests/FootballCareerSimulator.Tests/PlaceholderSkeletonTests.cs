using FootballCareerSimulator.Simulation;

namespace FootballCareerSimulator.Tests;

/// <summary>
/// docs/18_SPIKE_EXECUTION_PLAN.md Kart 0 kabul kriterini doğrular: Simulation katmanı bağımsız çalışır.
/// Application placeholder use case Production Kart 3'te kaldırıldı.
/// </summary>
public class PlaceholderSkeletonTests
{
    [Fact]
    public void AdvanceOneStep_IncrementsValueByOne()
    {
        var loop = new PlaceholderWorldLoop();

        var next = loop.AdvanceOneStep(PlaceholderSimulationStep.Zero);

        Assert.Equal(1, next.Value);
    }

    [Fact]
    public void PlaceholderSimulationStep_Constructor_RejectsNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlaceholderSimulationStep(-1));
    }
}
