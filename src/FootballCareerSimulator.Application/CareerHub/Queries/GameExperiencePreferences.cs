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

    public bool EffectiveMusicEnabled => SoundEnabled && MusicEnabled;

    public bool EffectiveCrowdEnabled => SoundEnabled && CrowdEnabled;

    public bool EffectiveSfxEnabled => SoundEnabled;

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

    public int ScaleFont(int baseSize) =>
        Math.Max(1, (int)Math.Round(baseSize * (Normalize().TextScalePercent / 100d)));
}
