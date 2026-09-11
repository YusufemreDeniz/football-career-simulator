using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class MobileUiLayoutProfileTests
{
    [Theory]
    [InlineData(360, 800, 24, true, 3, 52, 32)]
    [InlineData(390, 844, 0, true, 5, 52, 8)]
    [InlineData(1280, 720, 0, false, 5, 48, 10)]
    public void Resolve_ProtectsTouchTargetsAndSafeArea(
        int width,
        int height,
        int safeBottom,
        bool compact,
        int columns,
        int touchHeight,
        int bottomMargin)
    {
        var profile = MobileUiLayoutProfile.Resolve(width, height, safeBottom);

        Assert.Equal(compact, profile.IsCompact);
        Assert.Equal(columns, profile.NavigationColumns);
        Assert.Equal(touchHeight, profile.TouchTargetHeight);
        Assert.Equal(bottomMargin, profile.BottomMargin);
        Assert.True(profile.StandingsMinimumWidth >= 920);
    }

    [Fact]
    public void Resolve_AppliesEverySafeAreaEdgeIndependently()
    {
        var profile = MobileUiLayoutProfile.Resolve(
            844,
            390,
            safeLeftInset: 31,
            safeTopInset: 5,
            safeRightInset: 17,
            safeBottomInset: 12);

        Assert.Equal(47, profile.LeftMargin);
        Assert.Equal(33, profile.RightMargin);
        Assert.Equal(19, profile.TopMargin);
        Assert.Equal(22, profile.BottomMargin);
    }

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1600, 720)]
    [InlineData(844, 390)]
    public void Resolve_UsesCompactLayoutForLandscapeMobileRuntime(int width, int height)
    {
        var profile = MobileUiLayoutProfile.Resolve(
            width,
            height,
            safeLeftInset: 0,
            safeTopInset: 0,
            safeRightInset: 0,
            safeBottomInset: 0,
            mobileRuntime: true);

        Assert.True(profile.IsCompact);
        Assert.Equal(52, profile.TouchTargetHeight);
        Assert.Equal(8, profile.HorizontalMargin);
    }

    [Fact]
    public void DeviceAcceptance_DistinguishesAutomatedLayoutFromRealTouchEvidence()
    {
        var profile = MobileDeviceAcceptanceProfile.Evaluate(
            844,
            390,
            safeLeftInset: 31,
            safeTopInset: 0,
            safeRightInset: 17,
            safeBottomInset: 12,
            minimumTouchTarget: 48,
            bodyFontSize: 15,
            touchInputAvailable: false);

        Assert.True(profile.IsReady);
        Assert.True(profile.PhysicalEvidencePending);
        Assert.Equal(796, profile.SafeWidth);
        Assert.Contains(profile.Checks, check => check.Contains("fiziksel release soak", StringComparison.Ordinal));
    }

    [Fact]
    public void DeviceAcceptance_IncludesAccessibilityAndAudioMixContract()
    {
        var preferences = GameExperiencePreferences.Default with
        {
            SoundEnabled = true,
            MusicEnabled = false,
            CrowdEnabled = false,
            HapticsEnabled = false,
            HighContrast = true,
            ReducedMotion = true,
            TextScalePercent = 130,
        };

        var profile = MobileDeviceAcceptanceProfile.Evaluate(
            360,
            800,
            safeLeftInset: 12,
            safeTopInset: 24,
            safeRightInset: 8,
            safeBottomInset: 32,
            minimumTouchTarget: 52,
            bodyFontSize: 16,
            touchInputAvailable: true,
            preferences);

        Assert.True(profile.IsReady);
        Assert.False(profile.PhysicalEvidencePending);
        Assert.Contains(profile.Checks, check => check.Contains("müzik kapalı", StringComparison.Ordinal));
        Assert.Contains(profile.Checks, check => check.Contains("tribün kapalı", StringComparison.Ordinal));
        Assert.Contains(profile.Checks, check => check.Contains("Titreşim tercihi kapalı", StringComparison.Ordinal));
        Assert.Contains(profile.Checks, check => check.Contains("Kontrast yüksek", StringComparison.Ordinal));
        Assert.Contains(profile.Checks, check => check.Contains("Yazı ölçeği %130", StringComparison.Ordinal));
        Assert.Contains(profile.Checks, check => check.Contains("Dokunmatik giriş algılandı", StringComparison.Ordinal));
    }
}
