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
    public int LeftMargin { get; init; } = HorizontalMargin;

    public int RightMargin { get; init; } = HorizontalMargin;

    public static MobileUiLayoutProfile Resolve(
        int viewportWidth,
        int viewportHeight,
        int safeBottomInset,
        bool mobileRuntime = false)
        => Resolve(
            viewportWidth,
            viewportHeight,
            safeLeftInset: 0,
            safeTopInset: 0,
            safeRightInset: 0,
            safeBottomInset,
            mobileRuntime);

    public static MobileUiLayoutProfile Resolve(
        int viewportWidth,
        int viewportHeight,
        int safeLeftInset,
        int safeTopInset,
        int safeRightInset,
        int safeBottomInset,
        bool mobileRuntime = false)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return Resolve(
                390,
                844,
                safeLeftInset,
                safeTopInset,
                safeRightInset,
                safeBottomInset,
                mobileRuntime);
        }

        // Godot'un yatay Android viewport'u çoğunlukla 1280x720 veya daha geniştir.
        // Yalnızca piksel genişliğine bakmak telefonu masaüstü sanıyordu.
        var compact = mobileRuntime || viewportWidth < 720;
        var horizontalMargin = compact ? 8 : 16;
        var topMargin = compact ? 8 : 14;
        var bottomMargin = compact ? 8 : 10;
        return new MobileUiLayoutProfile(
            compact,
            HorizontalMargin: horizontalMargin,
            TopMargin: topMargin + Math.Clamp(safeTopInset, 0, 64),
            BottomMargin: bottomMargin + Math.Clamp(safeBottomInset, 0, 64),
            NavigationColumns: compact && viewportWidth < 380 ? 3 : 5,
            TouchTargetHeight: compact ? 52 : 48,
            CrestSize: compact ? 48 : 64,
            PageTitleFontSize: compact ? 22 : 26,
            StandingsMinimumWidth: compact ? 920 : Math.Max(920, viewportWidth - 64))
        {
            LeftMargin = horizontalMargin + Math.Clamp(safeLeftInset, 0, 64),
            RightMargin = horizontalMargin + Math.Clamp(safeRightInset, 0, 64),
        };
    }
}
