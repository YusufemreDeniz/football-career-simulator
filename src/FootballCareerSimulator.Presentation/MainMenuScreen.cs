using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class MainMenuScreen : Control
{
    private const float MinimumContentWidth = 288f;
    private const float MaximumContentWidth = 1120f;
    private const float HorizontalSafeMargin = 18f;

    private Label _statusLabel = null!;
    private VBoxContainer _content = null!;
    private GridContainer _bodyGrid = null!;

    public event Action? NewCareerRequested;

    public event Action? ContinueRequested;

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();

        var backdrop = AddMenuBackdrop();
        AnimateBackdrop(backdrop);

        var safeArea = new MarginContainer();
        safeArea.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        safeArea.GrowHorizontal = GrowDirection.Both;
        safeArea.GrowVertical = GrowDirection.Both;
        safeArea.AddThemeConstantOverride("margin_left", (int)HorizontalSafeMargin);
        safeArea.AddThemeConstantOverride("margin_top", 12);
        safeArea.AddThemeConstantOverride("margin_right", (int)HorizontalSafeMargin);
        safeArea.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(safeArea);

        var center = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        safeArea.AddChild(center);

        _content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _content.AddThemeConstantOverride("separation", 10);
        center.AddChild(_content);
        UpdateContentWidth();
        Resized += UpdateContentWidth;

        _content.AddChild(BuildTopBar());

        _bodyGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _bodyGrid.AddThemeConstantOverride("h_separation", CareerUiTheme.SpaceL);
        _bodyGrid.AddThemeConstantOverride("v_separation", CareerUiTheme.SpaceM);
        _content.AddChild(_bodyGrid);

        var hero = BuildHero(out var brandLine, out var emblem);
        _bodyGrid.AddChild(hero);

        var actionCard = BuildActionCard();
        _bodyGrid.AddChild(actionCard);

        var footer = new Label
        {
            Text = "KARAR  →  SONUÇ  →  HAFIZA",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        CareerUiTheme.StyleEyebrow(footer, CareerUiTheme.Data);
        footer.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _content.AddChild(footer);

        AnimateEntry(brandLine, emblem, actionCard);
    }

    public void SetStatus(string message)
    {
        _statusLabel.Text = message;
        if (CareerUiTheme.ReducedMotion)
        {
            _statusLabel.Modulate = Colors.White;
            return;
        }

        _statusLabel.Modulate = new Color(1f, 1f, 1f, 0.42f);
        var tween = CreateTween();
        tween.TweenProperty(_statusLabel, "modulate:a", 1f, 0.24f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private void UpdateContentWidth()
    {
        if (_content is null)
        {
            return;
        }

        var viewportWidth = Size.X > 0 ? Size.X : GetViewportRect().Size.X;
        var availableWidth = Mathf.Max(MinimumContentWidth, viewportWidth - (HorizontalSafeMargin * 2f));
        _content.CustomMinimumSize = new Vector2(Mathf.Min(MaximumContentWidth, availableWidth), 0);
        if (_bodyGrid is not null)
        {
            var viewportHeight = Size.Y > 0 ? Size.Y : GetViewportRect().Size.Y;
            _bodyGrid.Columns = viewportWidth >= 700f && viewportWidth > viewportHeight ? 2 : 1;
        }
    }

    private static Control BuildTopBar()
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", 8);

        row.AddChild(BuildPill("●  FCS MANAGER", CareerUiTheme.ActionBright, live: true));
        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        row.AddChild(BuildPill("v1.1  •  YEREL KARİYER", CareerUiTheme.InkMuted));
        return row;
    }

    private static Control BuildHero(out ColorRect brandLine, out PanelContainer emblem)
    {
        var hero = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.End,
        };
        hero.AddThemeConstantOverride("separation", 4);

        hero.AddChild(new Control
        {
            CustomMinimumSize = new Vector2(0, 12),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        });

        emblem = new PanelContainer
        {
            CustomMinimumSize = new Vector2(50, 50),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        emblem.AddThemeStyleboxOverride("panel", CareerUiTheme.EmblemPanel());
        var emblemImage = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://assets/app-icon/launcher-adaptive-foreground-432.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(44, 44),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        emblem.AddChild(emblemImage);
        hero.AddChild(emblem);

        var eyebrow = new Label
        {
            Text = "KARİYERİNİ YÖNET • MİRASINI BIRAK",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        CareerUiTheme.StyleEyebrow(eyebrow, CareerUiTheme.Accent);
        hero.AddChild(eyebrow);

        var brand = new Label
        {
            Text = "FOOTBALL\nCAREER",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        CareerUiTheme.StyleBrand(brand);
        brand.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(31));
        brand.AddThemeConstantOverride("line_spacing", -7);
        hero.AddChild(brand);

        var brandSecondLine = new Label
        {
            Text = "SIMULATOR",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        CareerUiTheme.StyleHeadline(brandSecondLine);
        brandSecondLine.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(23));
        brandSecondLine.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        hero.AddChild(brandSecondLine);

        brandLine = new ColorRect
        {
            Color = new Color(
                CareerUiTheme.ActionBright.R,
                CareerUiTheme.ActionBright.G,
                CareerUiTheme.ActionBright.B,
                0.86f),
            CustomMinimumSize = new Vector2(34, 3),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        hero.AddChild(brandLine);

        var tagline = new Label
        {
            Text = "Takımını kur. Kararını ver. Hikâyeni sahaya yaz.",
            HorizontalAlignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(tagline, muted: true);
        tagline.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(14));
        hero.AddChild(tagline);

        var facts = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            Alignment = BoxContainer.AlignmentMode.Begin,
        };
        facts.AddThemeConstantOverride("separation", 6);
        facts.AddChild(BuildPill("KARARLAR", CareerUiTheme.InkMuted));
        facts.AddChild(BuildPill("İLİŞKİLER", CareerUiTheme.InkMuted));
        facts.AddChild(BuildPill("HAFIZA", CareerUiTheme.InkMuted));
        hero.AddChild(facts);

        var heroMargin = new MarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        heroMargin.AddThemeConstantOverride("margin_left", 10);
        heroMargin.AddThemeConstantOverride("margin_right", 10);
        heroMargin.AddThemeConstantOverride("margin_bottom", 8);
        heroMargin.AddChild(hero);
        return heroMargin;
    }

    private Control BuildActionCard()
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.GlassPanel());

        var actions = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        actions.AddThemeConstantOverride("separation", 9);
        panel.AddChild(actions);

        var title = new Label
        {
            Text = "MENAJER MERKEZİ",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        CareerUiTheme.StyleEyebrow(title, CareerUiTheme.Accent);
        title.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        actions.AddChild(title);

        var subtitle = new Label
        {
            Text = "Kariyerine kaldığın yerden devam et veya yeni bir dünya kur.",
            HorizontalAlignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(subtitle, muted: true);
        subtitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        actions.AddChild(subtitle);

        var continueButton = new Button
        {
            Name = "ContinueButton",
            Text = "▶   Kariyere Devam Et",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = "Son yerel kariyer kaydını yükle",
        };
        CareerUiTheme.StyleMainMenuRailButton(continueButton, prominent: true);
        var savePath = Path.Combine(OS.GetUserDataDir(), "career_save.db");
        continueButton.Disabled = !File.Exists(savePath);
        continueButton.Pressed += () => ContinueRequested?.Invoke();
        actions.AddChild(continueButton);

        var newButton = new Button
        {
            Name = "NewCareerButton",
            Text = "◎   Yeni Menajer Kariyeri",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = "Yeni bir teknik direktör kariyeri başlat",
        };
        CareerUiTheme.StyleMainMenuRailButton(newButton);
        newButton.Pressed += () => NewCareerRequested?.Invoke();
        actions.AddChild(newButton);

        var statusPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        statusPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatusPanel());
        actions.AddChild(statusPanel);

        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Text = continueButton.Disabled
                ? "Kayıt yok — Yeni Menajer Kariyeri ile başla."
                : "Yerel kayıt mevcut — Kariyere Devam Et.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBody(_statusLabel, muted: true);
        _statusLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        statusPanel.AddChild(_statusLabel);

        return panel;
    }

    private static PanelContainer BuildPill(string text, Color color, bool live = false)
    {
        var panel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            live ? CareerUiTheme.LivePillPanel() : CareerUiTheme.PillPanel());
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleEyebrow(label, color);
        label.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        panel.AddChild(label);
        return panel;
    }

    private TextureRect AddMenuBackdrop()
    {
        var texture = GD.Load<Texture2D>("res://assets/menu/main-menu-stadium-v1.png");
        var backdrop = new TextureRect
        {
            Name = "MainMenuStadiumBackdrop",
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var cinematicWash = new ColorRect
        {
            Color = new Color(0.005f, 0.018f, 0.040f, 0.20f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        cinematicWash.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(cinematicWash);

        var rightVeil = new ColorRect
        {
            Color = new Color(0.004f, 0.014f, 0.037f, 0.48f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        rightVeil.SetAnchor(Side.Left, 0.50f);
        rightVeil.SetAnchor(Side.Right, 1f);
        rightVeil.SetAnchor(Side.Top, 0f);
        rightVeil.SetAnchor(Side.Bottom, 1f);
        AddChild(rightVeil);

        var lowerVeil = new ColorRect
        {
            Color = new Color(0.002f, 0.012f, 0.028f, 0.30f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        lowerVeil.SetAnchor(Side.Left, 0f);
        lowerVeil.SetAnchor(Side.Right, 1f);
        lowerVeil.SetAnchor(Side.Top, 0.58f);
        lowerVeil.SetAnchor(Side.Bottom, 1f);
        AddChild(lowerVeil);

        return backdrop;
    }

    private void AnimateBackdrop(CanvasItem backdrop)
    {
        if (CareerUiTheme.ReducedMotion)
        {
            backdrop.Modulate = Colors.White;
            return;
        }

        backdrop.Modulate = new Color(0.90f, 0.92f, 0.96f, 1f);
        var reveal = CreateTween();
        reveal.TweenProperty(backdrop, "modulate", Colors.White, 1.2f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    private void AnimateEntry(ColorRect brandLine, PanelContainer emblem, Control actionCard)
    {
        if (CareerUiTheme.ReducedMotion)
        {
            _content.Modulate = Colors.White;
            emblem.Scale = Vector2.One;
            brandLine.CustomMinimumSize = new Vector2(150, brandLine.CustomMinimumSize.Y);
            actionCard.Modulate = Colors.White;
            return;
        }

        _content.Modulate = new Color(1f, 1f, 1f, 0f);
        emblem.PivotOffset = new Vector2(32, 32);
        emblem.Scale = new Vector2(0.88f, 0.88f);
        actionCard.Modulate = new Color(1f, 1f, 1f, 0f);

        var intro = CreateTween();
        intro.SetParallel(true);
        intro.TweenProperty(_content, "modulate:a", 1f, 0.42f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        intro.TweenProperty(emblem, "scale", Vector2.One, 0.55f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        intro.TweenProperty(brandLine, "custom_minimum_size:x", 150f, 0.62f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        intro.TweenProperty(actionCard, "modulate:a", 1f, 0.46f)
            .SetDelay(0.12f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }
}
