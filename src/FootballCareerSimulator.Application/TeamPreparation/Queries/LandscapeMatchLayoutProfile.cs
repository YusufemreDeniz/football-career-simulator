namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

public sealed record LandscapeMatchLayoutProfile(
    bool IsCompact,
    int HorizontalMargin,
    int VerticalMargin,
    int SectionSeparation,
    int CommandPanelWidth,
    int PitchMinimumWidth,
    int PitchMinimumHeight,
    int PlayerButtonWidth,
    int PlayerButtonHeight,
    int ActionButtonHeight)
{
    public static LandscapeMatchLayoutProfile Resolve(int viewportWidth, int viewportHeight)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return Resolve(844, 390);
        }

        var compact = viewportHeight < 560 || viewportWidth < 1000;
        return compact
            ? new LandscapeMatchLayoutProfile(
                IsCompact: true,
                HorizontalMargin: 8,
                VerticalMargin: 6,
                SectionSeparation: 8,
                CommandPanelWidth: 240,
                PitchMinimumWidth: 340,
                PitchMinimumHeight: 190,
                PlayerButtonWidth: 62,
                PlayerButtonHeight: 42,
                ActionButtonHeight: 44)
            : new LandscapeMatchLayoutProfile(
                IsCompact: false,
                HorizontalMargin: 16,
                VerticalMargin: 16,
                SectionSeparation: 12,
                CommandPanelWidth: 310,
                PitchMinimumWidth: 480,
                PitchMinimumHeight: 300,
                PlayerButtonWidth: 76,
                PlayerButtonHeight: 48,
                ActionButtonHeight: 50);
    }
}
