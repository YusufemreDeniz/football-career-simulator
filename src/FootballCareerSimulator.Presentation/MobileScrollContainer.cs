using System;
using System.Linq;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// ScrollContainer with direct finger dragging. Godot's desktop scroll container
/// exposes the scrollbar drag, but touch screens need content drag semantics.
/// Also yields to inner scrollable controls (ItemList, Tree, nested MobileScrollContainer)
/// and suppresses accidental button taps when a drag gesture occurs.
/// </summary>
public sealed partial class MobileScrollContainer : ScrollContainer
{
    private bool _dragging;
    private Vector2 _touchStartPosition;
    private float _totalDragDistance;

    public static ulong LastDragScrollTimeMsec { get; private set; }

    public static void NotifyDragScrollCompleted() =>
        LastDragScrollTimeMsec = Time.GetTicksMsec();

    public static bool WasRecentDragScroll(ulong windowMs = 160) =>
        LastDragScrollTimeMsec > 0 && (Time.GetTicksMsec() - LastDragScrollTimeMsec) <= windowMs;

    public enum DragAxis
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsVisibleInTree())
        {
            _dragging = false;
            _totalDragDistance = 0f;
            return;
        }

        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
            {
                _touchStartPosition = touch.Position;
                _totalDragDistance = 0f;
                _dragging = GetGlobalRect().HasPoint(touch.Position);
            }
            else
            {
                if (_dragging && _totalDragDistance > 10f)
                {
                    NotifyDragScrollCompleted();
                    ReleasePressedButtons(this);
                    GetViewport().SetInputAsHandled();
                }

                _dragging = false;
                _totalDragDistance = 0f;
            }

            return;
        }

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseBtn)
        {
            if (mouseBtn.Pressed)
            {
                _touchStartPosition = mouseBtn.Position;
                _totalDragDistance = 0f;
                _dragging = GetGlobalRect().HasPoint(mouseBtn.Position);
            }
            else
            {
                if (_dragging && _totalDragDistance > 10f)
                {
                    NotifyDragScrollCompleted();
                    ReleasePressedButtons(this);
                    GetViewport().SetInputAsHandled();
                }

                _dragging = false;
                _totalDragDistance = 0f;
            }

            return;
        }

        Vector2 relative;
        if (@event is InputEventScreenDrag drag)
        {
            relative = drag.Relative;
        }
        else if (@event is InputEventMouseMotion motion && (motion.ButtonMask & MouseButtonMask.Left) != 0)
        {
            relative = motion.Relative;
        }
        else
        {
            return;
        }

        if (!_dragging)
        {
            return;
        }

        var axis = ResolveDragAxis(
            relative,
            HorizontalScrollMode != ScrollMode.Disabled,
            VerticalScrollMode != ScrollMode.Disabled);
        if (axis == DragAxis.None)
        {
            return;
        }

        // İçerideki bir liste (ItemList, Tree) veya iç ScrollContainer bu eksende kaydırılabiliyorsa,
        // dış kapsayıcı (_pageScroll vb.) dokunuşu çalmasın.
        if (HasInnerScrollableAt(this, _touchStartPosition, axis))
        {
            return;
        }

        var moved = false;
        if (axis == DragAxis.Horizontal)
        {
            var maxH = Mathf.RoundToInt(Mathf.Max(0, GetHScrollBar().MaxValue - Size.X));
            if (maxH <= 0)
            {
                return;
            }

            var before = ScrollHorizontal;
            ScrollHorizontal = Mathf.Clamp(
                before - Mathf.RoundToInt(relative.X),
                0,
                maxH);
            moved = ScrollHorizontal != before || maxH > 0;
        }
        else
        {
            var maxV = Mathf.RoundToInt(Mathf.Max(0, GetVScrollBar().MaxValue - Size.Y));
            if (maxV <= 0)
            {
                return;
            }

            var before = ScrollVertical;
            ScrollVertical = Mathf.Clamp(
                before - Mathf.RoundToInt(relative.Y),
                0,
                maxV);
            moved = ScrollVertical != before || maxV > 0;
        }

        if (moved)
        {
            _totalDragDistance += relative.Length();
            if (_totalDragDistance > 8f)
            {
                NotifyDragScrollCompleted();
                ReleasePressedButtons(this);
                GetViewport().SetInputAsHandled();
            }
        }
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

    private static bool HasInnerScrollableAt(Node root, Vector2 globalPoint, DragAxis axis)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is not Control ctrl || !ctrl.IsVisibleInTree())
            {
                continue;
            }

            if (ctrl.GetGlobalRect().HasPoint(globalPoint))
            {
                if (ctrl is ScrollContainer innerScroll)
                {
                    if (axis == DragAxis.Horizontal
                        && innerScroll.HorizontalScrollMode != ScrollMode.Disabled
                        && innerScroll.GetHScrollBar().MaxValue > innerScroll.Size.X + 1f)
                    {
                        return true;
                    }

                    if (axis == DragAxis.Vertical
                        && innerScroll.VerticalScrollMode != ScrollMode.Disabled
                        && innerScroll.GetVScrollBar().MaxValue > innerScroll.Size.Y + 1f)
                    {
                        return true;
                    }
                }
                else if (ctrl is ItemList or Tree)
                {
                    if (axis == DragAxis.Horizontal)
                    {
                        var hBar = (ctrl as ItemList)?.GetHScrollBar()
                            ?? ctrl.GetChildren(true).OfType<HScrollBar>().FirstOrDefault();
                        if (hBar is not null && hBar.MaxValue > hBar.Page + 1d)
                        {
                            return true;
                        }
                    }
                    else if (axis == DragAxis.Vertical)
                    {
                        var vBar = (ctrl as ItemList)?.GetVScrollBar()
                            ?? ctrl.GetChildren(true).OfType<VScrollBar>().FirstOrDefault();
                        if (vBar is not null && vBar.MaxValue > vBar.Page + 1d)
                        {
                            return true;
                        }
                    }
                }
            }

            if (HasInnerScrollableAt(ctrl, globalPoint, axis))
            {
                return true;
            }
        }

        return false;
    }

    internal static void ReleasePressedButtons(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is BaseButton { ActionMode: BaseButton.ActionModeEnum.Press }
                or BaseButton)
            {
                if (child is Control c)
                {
                    c.Scale = Vector2.One;
                }
            }

            ReleasePressedButtons(child);
        }
    }
}

/// <summary>
/// Godot 4'teki ItemList ve Tree bileşenlerine mobil/dokunmatik ekranda
/// doğrudan parmakla yukarı-aşağı (dikey) ve sağa-sola (yatay) kaydırma desteği ekler.
/// </summary>
internal sealed partial class TouchScrollRepeater : Node
{
    private Control? _target;
    private bool _dragging;
    private float _totalDragDistance;

    public static void Attach(Control target)
    {
        if (target.HasMeta("touch_scroll_repeater_attached"))
        {
            return;
        }

        target.SetMeta("touch_scroll_repeater_attached", true);
        var repeater = new TouchScrollRepeater { _target = target, Name = "TouchScrollRepeater" };
        target.AddChild(repeater);
    }

    public override void _Input(InputEvent @event)
    {
        if (_target is null || !IsInstanceValid(_target) || !_target.IsVisibleInTree())
        {
            _dragging = false;
            _totalDragDistance = 0f;
            return;
        }

        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
            {
                _dragging = _target.GetGlobalRect().HasPoint(touch.Position);
                _totalDragDistance = 0f;
            }
            else
            {
                if (_dragging && _totalDragDistance > 10f)
                {
                    MobileScrollContainer.NotifyDragScrollCompleted();
                    GetViewport().SetInputAsHandled();
                }

                _dragging = false;
                _totalDragDistance = 0f;
            }

            return;
        }

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseBtn)
        {
            if (mouseBtn.Pressed)
            {
                _dragging = _target.GetGlobalRect().HasPoint(mouseBtn.Position);
                _totalDragDistance = 0f;
            }
            else
            {
                if (_dragging && _totalDragDistance > 10f)
                {
                    MobileScrollContainer.NotifyDragScrollCompleted();
                    GetViewport().SetInputAsHandled();
                }

                _dragging = false;
                _totalDragDistance = 0f;
            }

            return;
        }

        if (!_dragging)
        {
            return;
        }

        Vector2 relative;
        if (@event is InputEventScreenDrag drag)
        {
            relative = drag.Relative;
        }
        else if (@event is InputEventMouseMotion motion && (motion.ButtonMask & MouseButtonMask.Left) != 0)
        {
            relative = motion.Relative;
        }
        else
        {
            return;
        }

        var vBar = (_target as ItemList)?.GetVScrollBar()
            ?? _target.GetChildren(true).OfType<VScrollBar>().FirstOrDefault();
        var hBar = (_target as ItemList)?.GetHScrollBar()
            ?? _target.GetChildren(true).OfType<HScrollBar>().FirstOrDefault();

        var scrolled = false;
        if (vBar is not null && vBar.MaxValue > vBar.Page + 0.5d && Mathf.Abs(relative.Y) > 0.25f)
        {
            var prev = vBar.Value;
            var maxVal = Math.Max(vBar.MinValue, vBar.MaxValue - vBar.Page);
            vBar.Value = Math.Clamp(prev - relative.Y, vBar.MinValue, maxVal);
            if (Math.Abs(vBar.Value - prev) > 0.01d)
            {
                scrolled = true;
            }
        }

        if (hBar is not null && hBar.MaxValue > hBar.Page + 0.5d && Mathf.Abs(relative.X) > 0.25f)
        {
            var prev = hBar.Value;
            var maxVal = Math.Max(hBar.MinValue, hBar.MaxValue - hBar.Page);
            hBar.Value = Math.Clamp(prev - relative.X, hBar.MinValue, maxVal);
            if (Math.Abs(hBar.Value - prev) > 0.01d)
            {
                scrolled = true;
            }
        }

        if (scrolled)
        {
            _totalDragDistance += relative.Length();
            _target.QueueRedraw();
            if (_totalDragDistance > 6f)
            {
                MobileScrollContainer.NotifyDragScrollCompleted();
                GetViewport().SetInputAsHandled();
            }
        }
    }
}
