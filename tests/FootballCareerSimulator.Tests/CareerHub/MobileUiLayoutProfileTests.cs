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
}
