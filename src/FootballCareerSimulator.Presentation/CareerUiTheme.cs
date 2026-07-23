using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Kariyer yüzeyi görsel dili — çim günü / kulüp ofisi (mor/krem klişesinden uzak).
/// </summary>
internal static class CareerUiTheme
{
    public static readonly Color Ink = new(0.07f, 0.16f, 0.13f, 1f);
    public static readonly Color InkMuted = new(0.22f, 0.32f, 0.28f, 1f);
    public static readonly Color Accent = new(0.78f, 0.58f, 0.12f, 1f);
    public static readonly Color Action = new(0.05f, 0.38f, 0.28f, 1f);
    public static readonly Color ActionHover = new(0.08f, 0.48f, 0.35f, 1f);
    public static readonly Color SurfaceSoft = new(1f, 1f, 1f, 0.35f);
    public static readonly Color DangerSoft = new(0.55f, 0.22f, 0.18f, 1f);

    private static FontFile? _display;
    private static FontFile? _body;
    private static bool _loaded;

    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _display = GD.Load<FontFile>("res://fonts/Syne-Variable.ttf");
        _body = GD.Load<FontFile>("res://fonts/Outfit-Variable.ttf");
        _loaded = true;
    }

    public static void StyleBrand(Label label)
    {
        EnsureLoaded();
        if (_display is not null)
        {
            label.AddThemeFontOverride("font", _display);
        }

        label.AddThemeFontSizeOverride("font_size", 42);
        label.AddThemeColorOverride("font_color", Ink);
    }

    public static void StyleHeadline(Label label)
    {
        EnsureLoaded();
        if (_display is not null)
        {
            label.AddThemeFontOverride("font", _display);
        }

        label.AddThemeFontSizeOverride("font_size", 22);
        label.AddThemeColorOverride("font_color", Ink);
    }

    public static void StyleSection(Label label)
    {
        EnsureLoaded();
        if (_display is not null)
        {
            label.AddThemeFontOverride("font", _display);
        }

        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", Accent);
    }

    public static void StyleBody(Label label, bool muted = false)
    {
        EnsureLoaded();
        if (_body is not null)
        {
            label.AddThemeFontOverride("font", _body);
        }

        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", muted ? InkMuted : Ink);
    }

    public static void StylePrimaryButton(Button button)
    {
        EnsureLoaded();
        if (_body is not null)
        {
            button.AddThemeFontOverride("font", _body);
        }

        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_disabled_color", new Color(1f, 1f, 1f, 0.45f));
        button.AddThemeStyleboxOverride("normal", SolidButton(Action));
        button.AddThemeStyleboxOverride("hover", SolidButton(ActionHover));
        button.AddThemeStyleboxOverride("pressed", SolidButton(new Color(Action.R, Action.G, Action.B, 0.92f)));
        button.AddThemeStyleboxOverride("disabled", SolidButton(new Color(0.45f, 0.5f, 0.48f, 0.55f)));
        button.CustomMinimumSize = new Vector2(0, 36);
    }

    public static void StyleSecondaryButton(Button button)
    {
        EnsureLoaded();
        if (_body is not null)
        {
            button.AddThemeFontOverride("font", _body);
        }

        button.AddThemeFontSizeOverride("font_size", 14);
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeColorOverride("font_hover_color", Ink);
        button.AddThemeColorOverride("font_disabled_color", new Color(Ink.R, Ink.G, Ink.B, 0.4f));
        button.AddThemeStyleboxOverride("normal", OutlineButton(InkMuted));
        button.AddThemeStyleboxOverride("hover", OutlineButton(Action));
        button.AddThemeStyleboxOverride("pressed", SolidButton(new Color(Action.R, Action.G, Action.B, 0.15f)));
        button.AddThemeStyleboxOverride("disabled", OutlineButton(new Color(Ink.R, Ink.G, Ink.B, 0.25f)));
        button.CustomMinimumSize = new Vector2(0, 32);
    }

    public static void StyleList(ItemList list)
    {
        EnsureLoaded();
        if (_body is not null)
        {
            list.AddThemeFontOverride("font", _body);
        }

        list.AddThemeFontSizeOverride("font_size", 13);
        list.AddThemeColorOverride("font_color", Ink);
        list.AddThemeColorOverride("font_hovered_color", Action);
        list.AddThemeStyleboxOverride("panel", SoftPanel());
    }

    public static StyleBoxFlat SoftPanel()
    {
        return new StyleBoxFlat
        {
            BgColor = SurfaceSoft,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            BorderWidthLeft = 0,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
        };
    }

    private static StyleBoxFlat SolidButton(Color color) =>
        new()
        {
            BgColor = color,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };

    private static StyleBoxFlat OutlineButton(Color border) =>
        new()
        {
            BgColor = new Color(1f, 1f, 1f, 0.2f),
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 7,
            ContentMarginBottom = 7,
        };

    public static Control CreateAtmosphereBackground()
    {
        var root = new Control
        {
            Name = "Atmosphere",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var texture = new GradientTexture2D
        {
            Width = 8,
            Height = 8,
            Fill = GradientTexture2D.FillEnum.Linear,
            FillFrom = new Vector2(0.05f, 0f),
            FillTo = new Vector2(0.95f, 1f),
            Gradient = new Gradient
            {
                Offsets = [0f, 0.55f, 1f],
                Colors =
                [
                    new Color(0.95f, 0.97f, 0.94f),
                    new Color(0.86f, 0.91f, 0.87f),
                    new Color(0.78f, 0.86f, 0.80f),
                ],
            },
        };

        var rect = new TextureRect
        {
            Texture = texture,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(rect);

        // İnce “saha çizgisi” hissi — dekoratif, kart değil
        var stripe = new ColorRect
        {
            Color = new Color(Accent.R, Accent.G, Accent.B, 0.12f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0, 4),
        };
        stripe.SetAnchor(Side.Left, 0f);
        stripe.SetAnchor(Side.Right, 1f);
        stripe.SetAnchor(Side.Top, 0f);
        stripe.OffsetBottom = 4;
        root.AddChild(stripe);

        return root;
    }
}
