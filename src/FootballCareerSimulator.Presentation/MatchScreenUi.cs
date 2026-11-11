using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Maç ekranları (maç günü, canlı akış, devre arası, maç sonucu) için ortak UI oluşturucu yardımcıları.
/// </summary>
internal static class MatchScreenUi
{
    public static MarginContainer CreateStageRoot(
        Control owner,
        Color stageWash,
        int horizontalMargin = 16,
        int verticalMargin = 16)
    {
        owner.AddChild(CareerUiTheme.CreateAtmosphereBackground());

        var wash = new ColorRect
        {
            Color = stageWash,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        wash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        owner.AddChild(wash);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.GrowHorizontal = Control.GrowDirection.Both;
        margin.GrowVertical = Control.GrowDirection.Both;
        owner.AddChild(margin);
        DisplaySafeAreaInsets.ApplyTo(owner, margin, horizontalMargin, verticalMargin);
        return margin;
    }

    public static VBoxContainer VerticalStack(int separation)
    {
        var stack = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        stack.AddThemeConstantOverride("separation", separation);
        return stack;
    }

    public static MobileScrollContainer ScrollArea()
    {
        return new MobileScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
    }

    public static PanelContainer Card(bool emphasized = false)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized));
        return panel;
    }

    public static PanelContainer StageMarker(string stage, string state, Color color)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePanel(color));

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", 8);
        panel.AddChild(row);

        var stageLabel = new Label
        {
            Text = stage,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleEyebrow(stageLabel, color);
        row.AddChild(stageLabel);

        var stateLabel = new Label
        {
            Text = state,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        CareerUiTheme.StyleEyebrow(stateLabel, CareerUiTheme.InkMuted);
        row.AddChild(stateLabel);
        return panel;
    }

    public static Control SectionTitle(string eyebrow, string title)
    {
        var stack = VerticalStack(2);
        var eyebrowLabel = new Label { Text = eyebrow };
        CareerUiTheme.StyleEyebrow(eyebrowLabel);
        stack.AddChild(eyebrowLabel);

        var titleLabel = new Label
        {
            Text = title,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(titleLabel);
        titleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(19));
        stack.AddChild(titleLabel);
        return stack;
    }

    public static Label BodyLine(string text, bool muted = false, HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = alignment,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(label, muted);
        return label;
    }

    public static Label BeatLine(string text, bool muted = false)
    {
        var label = BodyLine("•  " + text, muted);
        return label;
    }

    public static void ClearChildren(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            child.QueueFree();
        }
    }

    public static void FadeIn(Control content, Node owner)
    {
        if (CareerUiTheme.ReducedMotion)
        {
            content.Modulate = Colors.White;
            return;
        }

        content.Modulate = new Color(1f, 1f, 1f, 0f);
        var tween = owner.CreateTween();
        tween.TweenProperty(content, "modulate:a", 1f, 0.35f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }
}
