using FootballCareerSimulator.Application.CareerHub.Queries;
using System.Text.Json;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class GameExperiencePreferencesTests
{
    [Theory]
    [InlineData(80, 100)]
    [InlineData(108, 115)]
    [InlineData(123, 130)]
    [InlineData(200, 130)]
    public void Normalize_UsesSupportedReadableTextScales(int requested, int expected)
    {
        var preferences = GameExperiencePreferences.Default with { TextScalePercent = requested };

        Assert.Equal(expected, preferences.Normalize().TextScalePercent);
    }

    [Fact]
    public void CycleTextScale_WrapsAcrossAllSupportedSizes()
    {
        var normal = GameExperiencePreferences.Default;
        var large = normal.CycleTextScale();
        var extraLarge = large.CycleTextScale();
        var wrapped = extraLarge.CycleTextScale();

        Assert.Equal(115, large.TextScalePercent);
        Assert.Equal(130, extraLarge.TextScalePercent);
        Assert.Equal(100, wrapped.TextScalePercent);
        Assert.Equal(15, normal.ScaleFont(15));
        Assert.Equal(20, extraLarge.ScaleFont(15));
    }

    [Fact]
    public void MusicPreference_IsIndependentFromMasterSound()
    {
        var musicMuted = GameExperiencePreferences.Default with { MusicEnabled = false };
        var allSoundMuted = musicMuted with { SoundEnabled = false };

        Assert.False(musicMuted.MusicEnabled);
        Assert.True(musicMuted.SoundEnabled);
        Assert.False(musicMuted.EffectiveMusicEnabled);
        Assert.True(musicMuted.EffectiveSfxEnabled);
        Assert.False(allSoundMuted.SoundEnabled);
        Assert.False(allSoundMuted.MusicEnabled);
        Assert.False(allSoundMuted.EffectiveMusicEnabled);
        Assert.False(allSoundMuted.EffectiveCrowdEnabled);
        Assert.False(allSoundMuted.EffectiveSfxEnabled);
        Assert.True(GameExperiencePreferences.Default.MusicEnabled);
    }

    [Fact]
    public void LegacyJson_WithoutMusicPreference_DefaultsMusicToEnabled()
    {
        const string json = """
            {
              "SoundEnabled": true,
              "CrowdEnabled": true,
              "HapticsEnabled": true,
              "ReducedMotion": false,
              "HighContrast": false,
              "FirstWeekGuideEnabled": true,
              "TextScalePercent": 100
            }
            """;

        var restored = JsonSerializer.Deserialize<GameExperiencePreferences>(json);

        Assert.NotNull(restored);
        Assert.True(restored.MusicEnabled);
    }
}
