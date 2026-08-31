namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Gerçek cihaz kabulünü tahminle kapatmaz; ölçülen viewport, güvenli alan ve
/// erişilebilirlik tercihlerini tek, oyuncu-okunur kontrol listesine dönüştürür.
/// </summary>
public sealed record MobileDeviceAcceptanceProfile(
    string VerdictCode,
    string Headline,
    IReadOnlyList<string> Checks,
    int SafeWidth,
    int SafeHeight)
{
    public const string Ready = "ready";
    public const string Review = "review";

    public bool IsReady => string.Equals(VerdictCode, Ready, StringComparison.Ordinal);

    public bool PhysicalEvidencePending { get; init; }

    public string ToDisplayText() =>
        $"{Headline}\n{string.Join("\n", Checks.Select(check => $"• {check}"))}";

    public static MobileDeviceAcceptanceProfile Evaluate(
        int viewportWidth,
        int viewportHeight,
        int safeLeftInset,
        int safeTopInset,
        int safeRightInset,
        int safeBottomInset,
        int minimumTouchTarget,
        int bodyFontSize,
        bool touchInputAvailable,
        GameExperiencePreferences? preferences = null)
    {
        var width = Math.Max(0, viewportWidth);
        var height = Math.Max(0, viewportHeight);
        var safeWidth = Math.Max(0, width - Math.Max(0, safeLeftInset) - Math.Max(0, safeRightInset));
        var safeHeight = Math.Max(0, height - Math.Max(0, safeTopInset) - Math.Max(0, safeBottomInset));
        var prefs = (preferences ?? GameExperiencePreferences.Default).Normalize();
        var scaledBody = prefs.ScaleFont(bodyFontSize);
        var targetOk = minimumTouchTarget >= 48;
        var typeOk = scaledBody >= 15;
        var safeAreaOk = safeWidth >= 320 && safeHeight >= 320;
        var ready = targetOk && typeOk && safeAreaOk;
        var physicalPending = !touchInputAvailable;

        var checks = new List<string>
        {
            $"Güvenli görüntü alanı {safeWidth}×{safeHeight} px — {(safeAreaOk ? "uygun" : "dar; cihazda kontrol et")}",
            $"Ana dokunma hedefi {minimumTouchTarget} px — {(targetOk ? "uygun" : "48 px altı")}",
            $"Yazı ölçeği %{prefs.TextScalePercent}; gövde {scaledBody} px — {(typeOk ? "okunabilir taban" : "büyütülmeli")}",
            $"Ses {OnOff(prefs.SoundEnabled)}; müzik {OnOff(prefs.EffectiveMusicEnabled)}; tribün {OnOff(prefs.EffectiveCrowdEnabled)}",
            $"Titreşim tercihi {OnOff(prefs.HapticsEnabled)}{(physicalPending ? " — motor kanıtı cihazda" : "")}",
            $"Kontrast {(prefs.HighContrast ? "yüksek" : "standart")}; hareket {(prefs.ReducedMotion ? "azaltılmış" : "tam")}",
            physicalPending
                ? "Masaüstü/başsız koşu; hoparlör, titreşim motoru ve ısınma fiziksel release soak'tır."
                : "Dokunmatik giriş algılandı; sürükleme, hoparlör ve titreşim cihazda doğrulanabilir.",
        };

        return new MobileDeviceAcceptanceProfile(
            ready ? Ready : Review,
            ready
                ? physicalPending
                    ? "Otomatik cihaz eşikleri karşılandı; fiziksel soak açık"
                    : "Cihaz profili otomatik eşikleri karşılıyor"
                : "Cihaz profili inceleme istiyor",
            checks,
            safeWidth,
            safeHeight)
        {
            PhysicalEvidencePending = physicalPending,
        };
    }

    private static string OnOff(bool value) => value ? "açık" : "kapalı";
}
