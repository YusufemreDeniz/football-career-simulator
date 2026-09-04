using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// ScrollContainer with direct finger dragging. Godot's desktop scroll container
/// exposes the scrollbar drag, but touch screens need content drag semantics.
/// </summary>
public sealed partial class MobileScrollContainer : ScrollContainer
{
    private bool _dragging;

    public enum DragAxis
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
            {
                _dragging = GetGlobalRect().HasPoint(touch.Position);
            }
            else
            {
                _dragging = false;
            }

            return;
        }

        if (!_dragging || @event is not InputEventScreenDrag drag)
        {
            return;
        }

        var axis = ResolveDragAxis(
            drag.Relative,
            HorizontalScrollMode != ScrollMode.Disabled,
            VerticalScrollMode != ScrollMode.Disabled);
        if (axis == DragAxis.None)
        {
            return;
        }

        if (axis == DragAxis.Horizontal)
        {
            ScrollHorizontal = Mathf.Clamp(
                ScrollHorizontal - Mathf.RoundToInt(drag.Relative.X),
                0,
                Mathf.RoundToInt(Mathf.Max(0, GetHScrollBar().MaxValue - Size.X)));
        }
        else
        {
            ScrollVertical = Mathf.Clamp(
                ScrollVertical - Mathf.RoundToInt(drag.Relative.Y),
                0,
                Mathf.RoundToInt(Mathf.Max(0, GetVScrollBar().MaxValue - Size.Y)));
        }

        GetViewport().SetInputAsHandled();
    }

    public static DragAxis ResolveDragAxis(
        Vector2 relative,
        bool horizontalEnabled,
        bool verticalEnabled)
    {
        if (Mathf.Abs(relative.X) > Mathf.Abs(relative.Y))
        {
            return horizontalEnabled ? DragAxis.Horizontal : DragAxis.None;
        }

        return verticalEnabled ? DragAxis.Vertical : DragAxis.None;
    }
}
