using FootballCareerSimulator.Application.CareerHub.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Son bir dakikanın frame sürelerini tutan düşük maliyetli cihaz ölçüm düğümü.
/// Release davranışını değiştirmez; Dosya ekranında oyuncu/QA için özet üretir.
/// </summary>
internal sealed partial class MobileRuntimeTelemetryMonitor : Node
{
    private const int TailSampleCapacity = 3_600;
    private const double HitchThresholdSeconds = 1d / 30d;

    private readonly Queue<double> _tailFrameSeconds = new(TailSampleCapacity);
    private double _elapsedSeconds;
    private double _sumFrameSeconds;
    private double _worstFrameSeconds;
    private long _sampledFrames;
    private long _hitchFrames;

    public static MobileRuntimeTelemetryMonitor? Active { get; private set; }

    public override void _Ready()
    {
        Active = this;
        SetProcess(true);
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(Active, this))
        {
            Active = null;
        }
    }

    public override void _Process(double delta)
    {
        if (delta <= 0 || delta > 1)
        {
            return;
        }

        _elapsedSeconds += delta;
        _sumFrameSeconds += delta;
        _sampledFrames++;
        _worstFrameSeconds = Math.Max(_worstFrameSeconds, delta);
        if (delta > HitchThresholdSeconds)
        {
            _hitchFrames++;
        }

        _tailFrameSeconds.Enqueue(delta);
        if (_tailFrameSeconds.Count > TailSampleCapacity)
        {
            _tailFrameSeconds.Dequeue();
        }
    }

    public MobileRuntimeTelemetryDigest Snapshot()
    {
        var average = _sampledFrames == 0 ? 0 : _sumFrameSeconds / _sampledFrames;
        var ordered = _tailFrameSeconds.OrderBy(value => value).ToArray();
        var p95 = ordered.Length == 0
            ? 0
            : ordered[Math.Clamp(
                (int)Math.Ceiling(ordered.Length * 0.95d) - 1,
                0,
                ordered.Length - 1)];
        return MobileRuntimeTelemetryDigest.Compose(
            _elapsedSeconds,
            _sampledFrames,
            average,
            p95,
            _worstFrameSeconds,
            _hitchFrames,
            GC.GetTotalMemory(forceFullCollection: false));
    }

    public void ResetMeasurement()
    {
        _tailFrameSeconds.Clear();
        _elapsedSeconds = 0;
        _sumFrameSeconds = 0;
        _worstFrameSeconds = 0;
        _sampledFrames = 0;
        _hitchFrames = 0;
    }
}
