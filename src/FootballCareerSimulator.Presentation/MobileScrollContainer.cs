using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// ScrollContainer with direct finger dragging. Godot's desktop scroll container
/// exposes the scrollbar drag, but touch screens need content drag semantics.
/// </summary>
public sealed partial class MobileScrollContainer : ScrollContainer
{
    private bool _dragging;

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

        ScrollVertical = Mathf.Clamp(
            ScrollVertical - Mathf.RoundToInt(drag.Relative.Y),
            0,
            Mathf.RoundToInt(Mathf.Max(0, GetVScrollBar().MaxValue - Size.Y)));
        GetViewport().SetInputAsHandled();
    }
}
