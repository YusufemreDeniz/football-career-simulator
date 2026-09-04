namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Gerçek cihazdan toplanan kısa süreli frame ölçümünü oyuncu/QA tarafından
/// okunabilir bir kabul özetine dönüştürür. Sıcaklık sensörü yerine uzun koşu
/// sırasında frame kararlılığını ölçer; fiziksel ısınma yine elle doğrulanır.
/// </summary>
public sealed record MobileRuntimeTelemetryDigest(
    string VerdictCode,
    string Headline,
    string DetailLine,
    double ElapsedSeconds,
    double AverageFps,
    double P95FrameMilliseconds,
    double WorstFrameMilliseconds,
    double HitchPercent,
    long ManagedMemoryBytes)
{
    public const string WarmingUp = "warming-up";
    public const string Ready = "ready";
    public const string Review = "review";

    public bool HasEnoughEvidence => ElapsedSeconds >= 30;

    public bool MeetsFrameBudget => VerdictCode == Ready;

    public static MobileRuntimeTelemetryDigest Compose(
        double elapsedSeconds,
        long sampledFrames,
        double averageFrameSeconds,
        double p95FrameSeconds,
        double worstFrameSeconds,
        long hitchFrames,
        long managedMemoryBytes)
    {
        var elapsed = Math.Max(0, elapsedSeconds);
        var frames = Math.Max(0, sampledFrames);
        var averageFps = averageFrameSeconds > 0
            ? Math.Clamp(1d / averageFrameSeconds, 0, 999)
            : 0;
        var p95Ms = Math.Max(0, p95FrameSeconds * 1000d);
        var worstMs = Math.Max(0, worstFrameSeconds * 1000d);
        var hitchPercent = frames == 0
            ? 0
            : Math.Clamp(hitchFrames * 100d / frames, 0, 100);
        var enough = elapsed >= 30 && frames >= 300;
        var stable = averageFps >= 50 && p95Ms <= 25 && hitchPercent <= 2;
        var verdict = !enough ? WarmingUp : stable ? Ready : Review;
        var headline = verdict switch
        {
            Ready => "Gerçek zamanlı frame bütçesi kararlı",
            Review => "Cihaz performansı inceleme istiyor",
            _ => "Cihaz ölçümü ısınıyor",
        };
        var detail = $"{elapsed:0}s · ort. {averageFps:0.0} FPS · p95 {p95Ms:0.0} ms"
            + $" · en kötü {worstMs:0.0} ms · takılma %{hitchPercent:0.0}"
            + $" · yönetilen bellek {FormatMegabytes(managedMemoryBytes):0.0} MB";

        return new MobileRuntimeTelemetryDigest(
            verdict,
            headline,
            detail,
            elapsed,
            averageFps,
            p95Ms,
            worstMs,
            hitchPercent,
            Math.Max(0, managedMemoryBytes));
    }

    private static double FormatMegabytes(long bytes) =>
        Math.Max(0, bytes) / (1024d * 1024d);
}
