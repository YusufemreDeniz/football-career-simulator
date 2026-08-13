using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Mobil kariyer yüzeyinin ortak görsel dili: gece stadyumu + kulüp operasyon odası.
/// Renk adları bütün Presentation ekranlarında aynı anlamı taşır.
/// </summary>
internal static class CareerUiTheme
{
    public static readonly Color BackgroundDeep = new(0.02f, 0.055f, 0.045f, 1f);
    public static readonly Color Background = new(0.035f, 0.105f, 0.085f, 1f);
    public static readonly Color BackgroundNavy = new(0.035f, 0.085f, 0.12f, 1f);
    public static readonly Color Surface = new(0.055f, 0.14f, 0.105f, 0.94f);
    public static readonly Color SurfaceRaised = new(0.075f, 0.19f, 0.145f, 0.96f);
    public static readonly Color SurfaceSoft = new(0.075f, 0.18f, 0.14f, 0.82f);
    public static readonly Color Stroke = new(0.22f, 0.38f, 0.31f, 0.58f);

    // Existing callers treat Ink as foreground, Accent as prestige and Action as primary CTA.
    public static readonly Color Ink = new(0.92f, 0.97f, 0.94f, 1f);
    public static readonly Color InkMuted = new(0.60f, 0.70f, 0.65f, 1f);
    public static readonly Color Accent = new(0.85f, 0.68f, 0.27f, 1f);
    public static readonly Color Action = new(0.26f, 0.82f, 0.48f, 1f);
    public static readonly Color ActionBright = new(0.36f, 0.91f, 0.57f, 1f);
    public static readonly Color ActionHover = ActionBright;
    public static readonly Color DangerSoft = new(1f, 0.42f, 0.39f, 1f);
    public static readonly Color Data = new(0.33f, 0.78f, 0.94f, 1f);

    private static FontFile? _display;
    private static FontFile? _body;
    private static bool _loaded;

    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _display = TryLoadFont("res://fonts/Syne-Variable.ttf");
        _body = TryLoadFont("res://fonts/Outfit-Variable.ttf");
        _loaded = true;
    }

    /// <summary>
    /// Önce import edilmiş FontFile; yoksa veya bozuksa doğrudan TTF (variable font dahil).
    /// </summary>
    private static FontFile? TryLoadFont(string path)
    {
        if (ResourceLoader.Exists(path))
        {
            var imported = GD.Load<FontFile>(path);
            if (imported is not null)
            {
                return imported;
            }
        }

        var dynamic = new FontFile();
        if (dynamic.LoadDynamicFont(path) == Error.Ok && dynamic.Data.Length > 0)
        {
            return dynamic;
        }

        GD.PushWarning($"CareerUiTheme: font yüklenemedi ({path}); tema varsayılanına düşülüyor.");
        return null;
    }

    public static void StyleBrand(Label label)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", 36);
        label.AddThemeColorOverride("font_color", Ink);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.42f));
        label.AddThemeConstantOverride("shadow_offset_x", 0);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
    }

    public static void StyleHeadline(Label label)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", 22);
        label.AddThemeColorOverride("font_color", Ink);
    }

    public static void StyleSection(Label label)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", Accent);
    }

    public static void StyleEyebrow(Label label, Color? color = null)
    {
        ApplyBodyFont(label);
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", color ?? Accent);
    }

    public static void StyleBody(Label label, bool muted = false)
    {
        ApplyBodyFont(label);
        label.AddThemeFontSizeOverride("font_size", 15);
        label.AddThemeColorOverride("font_color", muted ? InkMuted : Ink);
    }

    public static void StylePrimaryButton(Button button)
    {
        ApplyBodyFont(button);
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeColorOverride("font_color", BackgroundDeep);
        button.AddThemeColorOverride("font_hover_color", BackgroundDeep);
        button.AddThemeColorOverride("font_pressed_color", BackgroundDeep);
        button.AddThemeColorOverride("font_focus_color", BackgroundDeep);
        button.AddThemeColorOverride("font_disabled_color", new Color(InkMuted.R, InkMuted.G, InkMuted.B, 0.58f));
        button.AddThemeStyleboxOverride("normal", SolidButton(Action));
        button.AddThemeStyleboxOverride("hover", SolidButton(ActionHover));
        button.AddThemeStyleboxOverride("pressed", SolidButton(new Color(0.20f, 0.70f, 0.40f, 1f)));
        button.AddThemeStyleboxOverride("focus", FocusRing(Action));
        button.AddThemeStyleboxOverride("disabled", SolidButton(new Color(0.15f, 0.24f, 0.20f, 0.92f)));
        button.CustomMinimumSize = new Vector2(0, 50);
    }

    public static void StyleSecondaryButton(Button button)
    {
        ApplyBodyFont(button);
        button.AddThemeFontSizeOverride("font_size", 14);
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeColorOverride("font_hover_color", Ink);
        button.AddThemeColorOverride("font_pressed_color", Ink);
        button.AddThemeColorOverride("font_focus_color", Ink);
        button.AddThemeColorOverride("font_disabled_color", new Color(InkMuted.R, InkMuted.G, InkMuted.B, 0.46f));
        button.AddThemeStyleboxOverride("normal", OutlineButton(Stroke));
        button.AddThemeStyleboxOverride("hover", OutlineButton(new Color(Action.R, Action.G, Action.B, 0.82f)));
        button.AddThemeStyleboxOverride("pressed", SolidButton(new Color(Action.R, Action.G, Action.B, 0.18f)));
        button.AddThemeStyleboxOverride("focus", FocusRing(Data));
        button.AddThemeStyleboxOverride("disabled", OutlineButton(new Color(Stroke.R, Stroke.G, Stroke.B, 0.34f)));
        button.CustomMinimumSize = new Vector2(0, 48);
    }

    public static void StyleTextInput(LineEdit input)
    {
        ApplyBodyFont(input);
        input.AddThemeFontSizeOverride("font_size", 16);
        input.AddThemeColorOverride("font_color", Ink);
        input.AddThemeColorOverride("font_placeholder_color", InkMuted);
        input.AddThemeStyleboxOverride("normal", InputField(Stroke));
        input.AddThemeStyleboxOverride("focus", InputField(Data));
        input.AddThemeStyleboxOverride("read_only", InputField(Stroke));
    }

    public static void StyleOptionSelector(OptionButton selector)
    {
        ApplyBodyFont(selector);
        selector.AddThemeFontSizeOverride("font_size", 15);
        selector.AddThemeColorOverride("font_color", Ink);
        selector.AddThemeColorOverride("font_hover_color", Ink);
        selector.AddThemeColorOverride("font_pressed_color", Ink);
        selector.AddThemeStyleboxOverride("normal", InputField(Stroke));
        selector.AddThemeStyleboxOverride("hover", InputField(Action));
        selector.AddThemeStyleboxOverride("pressed", InputField(Data));
        selector.AddThemeStyleboxOverride("focus", FocusRing(Data));
    }

    public static void StyleNavButton(Button button, bool selected)
    {
        ApplyBodyFont(button);
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeColorOverride("font_color", selected ? BackgroundDeep : InkMuted);
        button.AddThemeColorOverride("font_hover_color", selected ? BackgroundDeep : Ink);
        button.AddThemeColorOverride("font_pressed_color", selected ? BackgroundDeep : Ink);
        if (selected)
        {
            button.AddThemeStyleboxOverride("normal", SolidButton(Action));
            button.AddThemeStyleboxOverride("hover", SolidButton(ActionHover));
            button.AddThemeStyleboxOverride("pressed", SolidButton(Action));
        }
        else
        {
            button.AddThemeStyleboxOverride("normal", NavButtonSurface());
            button.AddThemeStyleboxOverride("hover", OutlineButton(new Color(Action.R, Action.G, Action.B, 0.6f)));
            button.AddThemeStyleboxOverride("pressed", SolidButton(new Color(Action.R, Action.G, Action.B, 0.14f)));
        }

        button.AddThemeStyleboxOverride("focus", FocusRing(Data));
        button.CustomMinimumSize = new Vector2(0, 48);
    }

    public static void StyleList(ItemList list)
    {
        ApplyBodyFont(list);
        list.AddThemeFontSizeOverride("font_size", 14);
        list.AddThemeColorOverride("font_color", Ink);
        list.AddThemeColorOverride("font_hovered_color", ActionHover);
        list.AddThemeColorOverride("font_selected_color", BackgroundDeep);
        list.AddThemeStyleboxOverride("panel", SoftPanel());
        list.AddThemeStyleboxOverride("focus", FocusRing(Data));
        list.AddThemeStyleboxOverride("selected", SolidButton(Action));
        list.AddThemeStyleboxOverride("selected_focus", SolidButton(ActionHover));
    }

    public static void StyleTable(Tree table)
    {
        ApplyBodyFont(table);
        table.AddThemeFontSizeOverride("font_size", 13);
        table.AddThemeColorOverride("font_color", Ink);
        table.AddThemeColorOverride("font_selected_color", BackgroundDeep);
        table.AddThemeColorOverride("title_button_color", InkMuted);
        table.AddThemeColorOverride("title_button_hover_color", Ink);
        table.AddThemeStyleboxOverride("panel", SoftPanel());
        table.AddThemeStyleboxOverride("focus", FocusRing(Data));
        table.AddThemeStyleboxOverride("selected", SolidButton(Action));
        table.AddThemeStyleboxOverride("selected_focus", SolidButton(ActionHover));
    }

    public static StyleBoxFlat SoftPanel() =>
        PanelStyle(SurfaceSoft, Stroke, radius: 12, contentMargin: 12, shadowSize: 4);

    public static StyleBoxFlat HeroPanel() =>
        PanelStyle(
            SurfaceRaised,
            new Color(Action.R, Action.G, Action.B, 0.34f),
            radius: 16,
            contentMargin: 16,
            shadowSize: 10);

    public static StyleBoxFlat CardPanel(bool emphasized = false) =>
        emphasized
            ? HeroPanel()
            : PanelStyle(Surface, Stroke, radius: 14, contentMargin: 14, shadowSize: 6);

    public static StyleBoxFlat NavigationPanel() =>
        PanelStyle(
            new Color(BackgroundDeep.R, BackgroundDeep.G, BackgroundDeep.B, 0.96f),
            Stroke,
            radius: 16,
            contentMargin: 6,
            shadowSize: 10);

    public static StyleBoxFlat PillPanel() =>
        PanelStyle(
            new Color(Ink.R, Ink.G, Ink.B, 0.045f),
            new Color(Stroke.R, Stroke.G, Stroke.B, 0.72f),
            radius: 999,
            contentMargin: 7,
            shadowSize: 0);

    public static StyleBoxFlat LivePillPanel() =>
        PanelStyle(
            new Color(Action.R, Action.G, Action.B, 0.13f),
            new Color(ActionBright.R, ActionBright.G, ActionBright.B, 0.62f),
            radius: 999,
            contentMargin: 7,
            shadowSize: 0);

    public static StyleBoxFlat EmblemPanel() =>
        PanelStyle(
            new Color(Accent.R, Accent.G, Accent.B, 0.10f),
            new Color(Accent.R, Accent.G, Accent.B, 0.72f),
            radius: 18,
            contentMargin: 8,
            shadowSize: 8);

    public static StyleBoxFlat StatusPanel(Color? signal = null)
    {
        var color = signal ?? Data;
        return
        PanelStyle(
            new Color(color.R, color.G, color.B, 0.08f),
            new Color(color.R, color.G, color.B, 0.36f),
            radius: 10,
            contentMargin: 10,
            shadowSize: 0);
    }

    public static StyleBoxFlat BadgePanel(Color color) =>
        PanelStyle(
            new Color(color.R, color.G, color.B, 0.10f),
            new Color(color.R, color.G, color.B, 0.42f),
            radius: 999,
            contentMargin: 7,
            shadowSize: 0);

    public static StyleBoxFlat LineupChipPanel(bool isIn, bool isOut)
    {
        var background = isOut
            ? new Color(DangerSoft.R, DangerSoft.G, DangerSoft.B, 0.12f)
            : isIn
                ? new Color(Action.R, Action.G, Action.B, 0.14f)
                : SurfaceSoft;
        var border = isOut ? DangerSoft : isIn ? Action : Stroke;
        return PanelStyle(background, border, radius: 9, contentMargin: 8, shadowSize: 0);
    }

    public static void StyleLineupChip(Label label, bool isIn, bool isOut)
    {
        ApplyBodyFont(label);
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride(
            "font_color",
            isOut ? DangerSoft : isIn ? ActionHover : Ink);
    }

    private static void ApplyDisplayFont(Control control)
    {
        EnsureLoaded();
        if (_display is not null)
        {
            control.AddThemeFontOverride("font", _display);
        }
    }

    private static void ApplyBodyFont(Control control)
    {
        EnsureLoaded();
        if (_body is not null)
        {
            control.AddThemeFontOverride("font", _body);
        }
    }

    private static StyleBoxFlat SolidButton(Color color) =>
        new()
        {
            BgColor = color,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomRight = 12,
            CornerRadiusBottomLeft = 12,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
        };

    private static StyleBoxFlat OutlineButton(Color border) =>
        new()
        {
            BgColor = new Color(Surface.R, Surface.G, Surface.B, 0.82f),
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomRight = 12,
            CornerRadiusBottomLeft = 12,
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 11,
            ContentMarginBottom = 11,
        };

    private static StyleBoxFlat InputField(Color border) =>
        new()
        {
            BgColor = new Color(Surface.R, Surface.G, Surface.B, 0.94f),
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 14,
            ContentMarginTop = 8,
            ContentMarginRight = 14,
            ContentMarginBottom = 8,
        };

    private static StyleBoxFlat NavButtonSurface() =>
        new()
        {
            BgColor = new Color(Surface.R, Surface.G, Surface.B, 0.74f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 10,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
        };

    private static StyleBoxFlat FocusRing(Color color) =>
        new()
        {
            BgColor = Colors.Transparent,
            BorderColor = color,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 13,
            CornerRadiusTopRight = 13,
            CornerRadiusBottomRight = 13,
            CornerRadiusBottomLeft = 13,
            ExpandMarginLeft = 2,
            ExpandMarginTop = 2,
            ExpandMarginRight = 2,
            ExpandMarginBottom = 2,
        };

    private static StyleBoxFlat PanelStyle(
        Color background,
        Color border,
        int radius,
        float contentMargin,
        int shadowSize)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomRight = radius,
            CornerRadiusBottomLeft = radius,
            ContentMarginLeft = contentMargin,
            ContentMarginRight = contentMargin,
            ContentMarginTop = contentMargin,
            ContentMarginBottom = contentMargin,
            ShadowColor = new Color(0f, 0f, 0f, shadowSize > 0 ? 0.28f : 0f),
            ShadowSize = shadowSize,
            ShadowOffset = new Vector2(0, shadowSize > 0 ? 4 : 0),
        };
    }

    /// <summary>
    /// Harici görsel gerektirmeyen, koyu stadyum ışığı ve soyut saha çizgileri üreten arka plan.
    /// </summary>
    public static Control CreateAtmosphereBackground()
    {
        var root = new Control
        {
            Name = "Atmosphere",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        root.AddChild(FullRectTexture(
            "NightGradient",
            LinearGradient(
                new Vector2(0.08f, 0f),
                new Vector2(0.92f, 1f),
                [0f, 0.5f, 1f],
                [BackgroundNavy, Background, BackgroundDeep])));

        root.AddChild(FullRectTexture(
            "StadiumGlow",
            RadialGradient(
                new Vector2(0.5f, 0.02f),
                new Vector2(0.5f, 0.72f),
                [0f, 0.42f, 1f],
                [
                    new Color(Data.R, Data.G, Data.B, 0.20f),
                    new Color(Action.R, Action.G, Action.B, 0.08f),
                    Colors.Transparent,
                ])));

        var pitch = new ColorRect
        {
            Name = "PitchHaze",
            Color = new Color(Action.R, Action.G, Action.B, 0.035f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        pitch.SetAnchor(Side.Left, 0f);
        pitch.SetAnchor(Side.Right, 1f);
        pitch.SetAnchor(Side.Top, 0.56f);
        pitch.SetAnchor(Side.Bottom, 1f);
        root.AddChild(pitch);

        for (var index = 0; index < 6; index++)
        {
            var band = new ColorRect
            {
                Color = new Color(
                    index % 2 == 0 ? Action.R : Data.R,
                    index % 2 == 0 ? Action.G : Data.G,
                    index % 2 == 0 ? Action.B : Data.B,
                    index % 2 == 0 ? 0.018f : 0.010f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            var left = index / 6f;
            var right = (index + 1) / 6f;
            band.SetAnchor(Side.Left, left);
            band.SetAnchor(Side.Right, right);
            band.SetAnchor(Side.Top, 0.56f);
            band.SetAnchor(Side.Bottom, 1f);
            root.AddChild(band);
        }

        var halfwayLine = new ColorRect
        {
            Color = new Color(Ink.R, Ink.G, Ink.B, 0.055f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        halfwayLine.SetAnchor(Side.Left, 0.5f);
        halfwayLine.SetAnchor(Side.Right, 0.5f);
        halfwayLine.SetAnchor(Side.Top, 0.56f);
        halfwayLine.SetAnchor(Side.Bottom, 1f);
        halfwayLine.OffsetLeft = -1;
        halfwayLine.OffsetRight = 1;
        root.AddChild(halfwayLine);

        var centreCircle = new Panel
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        centreCircle.SetAnchor(Side.Left, 0.5f);
        centreCircle.SetAnchor(Side.Right, 0.5f);
        centreCircle.SetAnchor(Side.Top, 0.76f);
        centreCircle.SetAnchor(Side.Bottom, 0.76f);
        centreCircle.OffsetLeft = -76;
        centreCircle.OffsetRight = 76;
        centreCircle.OffsetTop = -76;
        centreCircle.OffsetBottom = 76;
        centreCircle.AddThemeStyleboxOverride(
            "panel",
            new StyleBoxFlat
            {
                BgColor = Colors.Transparent,
                BorderColor = new Color(Ink.R, Ink.G, Ink.B, 0.05f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 999,
                CornerRadiusTopRight = 999,
                CornerRadiusBottomRight = 999,
                CornerRadiusBottomLeft = 999,
            });
        root.AddChild(centreCircle);

        var prestigeLine = new ColorRect
        {
            Name = "PrestigeLine",
            Color = new Color(Accent.R, Accent.G, Accent.B, 0.72f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        prestigeLine.SetAnchor(Side.Left, 0f);
        prestigeLine.SetAnchor(Side.Right, 1f);
        prestigeLine.SetAnchor(Side.Top, 0f);
        prestigeLine.OffsetBottom = 3;
        root.AddChild(prestigeLine);

        root.AddChild(FullRectTexture(
            "LowerVignette",
            LinearGradient(
                new Vector2(0.5f, 0.42f),
                new Vector2(0.5f, 1f),
                [0f, 1f],
                [Colors.Transparent, new Color(BackgroundDeep.R, BackgroundDeep.G, BackgroundDeep.B, 0.64f)])));

        return root;
    }

    private static TextureRect FullRectTexture(string name, Texture2D texture)
    {
        var rect = new TextureRect
        {
            Name = name,
            Texture = texture,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return rect;
    }

    private static GradientTexture2D LinearGradient(
        Vector2 from,
        Vector2 to,
        float[] offsets,
        Color[] colors) =>
        new()
        {
            Width = 32,
            Height = 32,
            Fill = GradientTexture2D.FillEnum.Linear,
            FillFrom = from,
            FillTo = to,
            Gradient = new Gradient
            {
                Offsets = offsets,
                Colors = colors,
            },
        };

    private static GradientTexture2D RadialGradient(
        Vector2 from,
        Vector2 to,
        float[] offsets,
        Color[] colors) =>
        new()
        {
            Width = 64,
            Height = 64,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = from,
            FillTo = to,
            Gradient = new Gradient
            {
                Offsets = offsets,
                Colors = colors,
            },
        };
}
