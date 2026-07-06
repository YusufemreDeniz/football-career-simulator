using FootballCareerSimulator.Simulation;

namespace FootballCareerSimulator.Application;

/// <summary>
/// Kart 0 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md) kapsamında Application katmanının Domain ve
/// Simulation'ı doğru bağımlılık yönünde orkestre edebildiğini kanıtlayan yer tutucu bir use case'tir.
/// </summary>
public sealed class AdvancePlaceholderSimulationUseCase
{
    private readonly PlaceholderWorldLoop _worldLoop;

    public AdvancePlaceholderSimulationUseCase(PlaceholderWorldLoop worldLoop)
    {
        _worldLoop = worldLoop ?? throw new ArgumentNullException(nameof(worldLoop));
    }

    public PlaceholderSimulationStep Execute(PlaceholderSimulationStep current, int stepCount) => _worldLoop.AdvanceSteps(current, stepCount);
}
