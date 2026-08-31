namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Platformdan bağımsız oyun hissi ve erişilebilirlik tercihleri.
/// Presentation bu modeli kullanıcı klasöründe saklar.
/// </summary>
public sealed record GameExperiencePreferences(
    bool SoundEnabled,
    bool CrowdEnabled,
    bool HapticsEnabled,
    bool ReducedMotion,
    bool HighContrast,
    bool FirstWeekGuideEnabled,
    int TextScalePercent)
{
    public bool MusicEnabled { get; init; } = true;

    public int FirstWeekGuideStep { get; init; }

    /// <summary>
    /// Cihaz titreşim gücü için kullanıcı seviyesi. Android API'sinde gerçek genlik
    /// her cihazda aynı davranmadığı için Presentation bunu kısa/daha uzun darbe
    /// süresine dönüştürür.
    /// </summary>
    public int HapticsStrengthPercent { get; init; } = 100;

    public bool EffectiveMusicEnabled => SoundEnabled && MusicEnabled;

    public bool EffectiveCrowdEnabled => SoundEnabled && CrowdEnabled;

    public bool EffectiveSfxEnabled => SoundEnabled;

    public int EffectiveHapticsStrengthPercent =>
        HapticsEnabled ? Normalize().HapticsStrengthPercent : 0;

    public static GameExperiencePreferences Default { get; } = new(
        SoundEnabled: true,
        CrowdEnabled: true,
        HapticsEnabled: true,
        ReducedMotion: false,
        HighContrast: false,
        FirstWeekGuideEnabled: true,
        TextScalePercent: 100);

    public GameExperiencePreferences Normalize() => this with
    {
        TextScalePercent = TextScalePercent switch
        {
            <= 107 => 100,
            <= 122 => 115,
            _ => 130,
        },
        FirstWeekGuideStep = Math.Clamp(FirstWeekGuideStep, 0, FirstWeekGuideDigest.TotalSteps),
        HapticsStrengthPercent = HapticsStrengthPercent switch
        {
            <= 0 => 0,
            <= 75 => 50,
            _ => 100,
        },
    };

    public GameExperiencePreferences CycleTextScale() => this with
    {
        TextScalePercent = TextScalePercent switch
        {
            < 115 => 115,
            < 130 => 130,
            _ => 100,
        },
    };

    public GameExperiencePreferences CycleHapticsStrength()
    {
        var current = EffectiveHapticsStrengthPercent;
        return current switch
        {
            0 => this with { HapticsEnabled = true, HapticsStrengthPercent = 50 },
            50 => this with { HapticsEnabled = true, HapticsStrengthPercent = 100 },
            _ => this with { HapticsEnabled = false, HapticsStrengthPercent = 0 },
        };
    }

    public int ScaleFont(int baseSize) =>
        Math.Max(1, (int)Math.Round(baseSize * (Normalize().TextScalePercent / 100d)));
}
