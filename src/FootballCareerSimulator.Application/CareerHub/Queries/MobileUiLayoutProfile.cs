namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed record MobileUiLayoutProfile(
    bool IsCompact,
    int HorizontalMargin,
    int TopMargin,
    int BottomMargin,
    int NavigationColumns,
    int TouchTargetHeight,
    int CrestSize,
    int PageTitleFontSize,
    int StandingsMinimumWidth)
{
    public static MobileUiLayoutProfile Resolve(int viewportWidth, int viewportHeight, int safeBottomInset)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return Resolve(390, 844, safeBottomInset);
        }

        var compact = viewportWidth < 720;
        return new MobileUiLayoutProfile(
            compact,
            HorizontalMargin: compact ? 8 : 16,
            TopMargin: compact ? 8 : 14,
            BottomMargin: (compact ? 8 : 10) + Math.Clamp(safeBottomInset, 0, 48),
            NavigationColumns: compact && viewportWidth < 380 ? 3 : 5,
            TouchTargetHeight: compact ? 52 : 48,
            CrestSize: compact ? 48 : 64,
            PageTitleFontSize: compact ? 22 : 26,
            StandingsMinimumWidth: compact ? 920 : Math.Max(920, viewportWidth - 64));
    }
}
