using FootballCareerSimulator.Application;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation;

namespace FootballCareerSimulator.Tests;

/// <summary>
/// docs/18_SPIKE_EXECUTION_PLAN.md Kart 0 kabul kriterini doğrular: çözüm derlenir ve Domain,
/// Simulation ve Application katmanları doğru bağımlılık yönünde birlikte çalışır.
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
    public void Execute_ThroughApplicationLayer_AdvancesExpectedStepCount()
    {
        var useCase = new AdvancePlaceholderSimulationUseCase(new PlaceholderWorldLoop());

        var result = useCase.Execute(PlaceholderSimulationStep.Zero, stepCount: 10);

        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void PlaceholderSimulationStep_Constructor_RejectsNegativeValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlaceholderSimulationStep(-1));
    }
}
