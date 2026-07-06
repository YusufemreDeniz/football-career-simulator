namespace FootballCareerSimulator.Simulation;

/// <summary>
/// Kart 0 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md) kapsamında Simulation katmanının yalnızca Domain'e
/// bağımlı, motor ve UI'dan bağımsız çalışabildiğini kanıtlayan yer tutucu bir sabit-adım döngüsüdür.
/// Spike 1'deki gerçek dünya ilerletme mantığının yerine geçmez.
/// </summary>
public sealed class PlaceholderWorldLoop
{
    public PlaceholderSimulationStep AdvanceOneStep(PlaceholderSimulationStep current) => current.Next();

    public PlaceholderSimulationStep AdvanceSteps(PlaceholderSimulationStep start, int stepCount)
    {
        if (stepCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stepCount), stepCount, "Step count cannot be negative.");
        }

        var result = start;
        for (var i = 0; i < stepCount; i++)
        {
            result = AdvanceOneStep(result);
        }

        return result;
    }
}
