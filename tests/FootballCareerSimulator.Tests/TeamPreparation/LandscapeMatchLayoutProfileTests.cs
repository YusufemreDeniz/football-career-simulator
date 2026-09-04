using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class LandscapeMatchLayoutProfileTests
{
    [Theory]
    [InlineData(844, 390, true, 240, 190, 42)]
    [InlineData(1280, 720, false, 310, 300, 48)]
    public void Resolve_KeepsThePitchAndDecisionDeskUsableOnLandscapeScreens(
        int width,
        int height,
        bool compact,
        int commandPanelWidth,
        int pitchMinimumHeight,
        int playerButtonHeight)
    {
        var profile = LandscapeMatchLayoutProfile.Resolve(width, height);

        Assert.Equal(compact, profile.IsCompact);
        Assert.Equal(commandPanelWidth, profile.CommandPanelWidth);
        Assert.Equal(pitchMinimumHeight, profile.PitchMinimumHeight);
        Assert.Equal(playerButtonHeight, profile.PlayerButtonHeight);
        Assert.True(profile.ActionButtonHeight >= 44);
    }
}
