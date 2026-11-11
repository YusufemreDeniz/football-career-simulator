using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Builds a predictable keyboard/gamepad focus chain for code-generated screens.
/// Godot keeps spatial D-pad navigation; this adds deterministic next/previous
/// fallback and a visible initial focus ring.
/// </summary>
internal static class UiFocusCoordinator
{
    public static void Prepare(Control root, bool grabInitialFocus = true)
    {
        ArgumentNullException.ThrowIfNull(root);
        var focusables = EnumerateControls(root)
            .Where(IsFocusable)
            .ToArray();
        if (focusables.Length == 0)
        {
            return;
        }

        foreach (var control in focusables)
        {
            control.FocusMode = Control.FocusModeEnum.All;
        }

        for (var index = 0; index < focusables.Length; index++)
        {
            var current = focusables[index];
            var previous = focusables[(index - 1 + focusables.Length) % focusables.Length];
            var next = focusables[(index + 1) % focusables.Length];
            current.FocusPrevious = current.GetPathTo(previous);
            current.FocusNext = current.GetPathTo(next);
        }

        if (grabInitialFocus)
        {
            focusables[0].GrabFocus();
        }
    }

    public static void FocusFirst(Control root)
    {
        var first = EnumerateControls(root).FirstOrDefault(IsFocusable);
        first?.GrabFocus();
    }

    private static IEnumerable<Control> EnumerateControls(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Control control)
            {
                yield return control;
            }

            foreach (var descendant in EnumerateControls(child))
            {
                yield return descendant;
            }
        }
    }

    private static bool IsFocusable(Control control)
    {
        if (!control.IsVisibleInTree())
        {
            return false;
        }

        return control switch
        {
            BaseButton button => !button.Disabled,
            LineEdit edit => edit.Editable,
            SpinBox spin => spin.Editable,
            ItemList => true,
            Tree => true,
            _ => false,
        };
    }
}
