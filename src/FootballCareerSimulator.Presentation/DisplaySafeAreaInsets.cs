using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// DisplayServer'in fiziksel güvenli alanını aktif Control koordinatlarına taşır.
/// Hub ve maç ekranları aynı dört-kenar hesabını kullanır.
/// </summary>
internal readonly record struct DisplaySafeAreaInsets(
    int Left,
    int Top,
    int Right,
    int Bottom)
{
    public static DisplaySafeAreaInsets Resolve(Vector2 logicalSize)
    {
        if (!OS.HasFeature("mobile"))
        {
            return default;
        }

        var display = DisplayServer.ScreenGetSize();
        var safe = DisplayServer.GetDisplaySafeArea();
        return FromMetrics(logicalSize, display, safe);
    }

    internal static DisplaySafeAreaInsets FromMetrics(
        Vector2 logicalSize,
        Vector2I display,
        Rect2I safe)
    {
        if (display.X <= 0
            || display.Y <= 0
            || safe.Size.X <= 0
            || safe.Size.Y <= 0)
        {
            return default;
        }

        var scaleX = logicalSize.X > 0 ? logicalSize.X / display.X : 1f;
        var scaleY = logicalSize.Y > 0 ? logicalSize.Y / display.Y : 1f;
        return new DisplaySafeAreaInsets(
            Mathf.RoundToInt(Math.Max(0, safe.Position.X) * scaleX),
            Mathf.RoundToInt(Math.Max(0, safe.Position.Y) * scaleY),
            Mathf.RoundToInt(Math.Max(0, display.X - safe.End.X) * scaleX),
            Mathf.RoundToInt(Math.Max(0, display.Y - safe.End.Y) * scaleY));
    }

    public static void ApplyTo(
        Control owner,
        MarginContainer margin,
        int horizontalMargin,
        int verticalMargin)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(margin);

        void ApplyMargins()
        {
            var logicalSize = owner.Size;
            if (logicalSize.X <= 0 || logicalSize.Y <= 0)
            {
                logicalSize = owner.GetViewportRect().Size;
            }

            var safe = Resolve(logicalSize);
            margin.AddThemeConstantOverride("margin_left", horizontalMargin + safe.Left);
            margin.AddThemeConstantOverride("margin_top", verticalMargin + safe.Top);
            margin.AddThemeConstantOverride("margin_right", horizontalMargin + safe.Right);
            margin.AddThemeConstantOverride("margin_bottom", verticalMargin + safe.Bottom);
        }

        ApplyMargins();
        owner.Resized += ApplyMargins;
    }
}
