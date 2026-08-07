using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class MainMenuScreen : Control
{
    private const float MinimumContentWidth = 288f;
    private const float MaximumContentWidth = 430f;
    private const float HorizontalSafeMargin = 16f;

    private Label _statusLabel = null!;
    private VBoxContainer _content = null!;

    public event Action? NewCareerRequested;

    public event Action? ContinueRequested;

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();

        var atmosphere = CareerUiTheme.CreateAtmosphereBackground();
        AddChild(atmosphere);
        AnimateAtmosphere(atmosphere);

        var safeArea = new MarginContainer();
        safeArea.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        safeArea.GrowHorizontal = GrowDirection.Both;
        safeArea.GrowVertical = GrowDirection.Both;
        safeArea.AddThemeConstantOverride("margin_left", (int)HorizontalSafeMargin);
        safeArea.AddThemeConstantOverride("margin_top", 20);
        safeArea.AddThemeConstantOverride("margin_right", (int)HorizontalSafeMargin);
        safeArea.AddThemeConstantOverride("margin_bottom", 16);
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

        var upperSpacer = new Control
        {
            CustomMinimumSize = new Vector2(0, 8),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 0.65f,
        };
        _content.AddChild(upperSpacer);

        var hero = BuildHero(out var brandLine, out var emblem);
        _content.AddChild(hero);

        var lowerSpacer = new Control
        {
            CustomMinimumSize = new Vector2(0, 10),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1f,
        };
        _content.AddChild(lowerSpacer);

        var actionCard = BuildActionCard();
        _content.AddChild(actionCard);

        var footer = new Label
        {
            Text = "KARAR  →  SONUÇ  →  HAFIZA",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        CareerUiTheme.StyleEyebrow(footer, CareerUiTheme.Data);
        footer.AddThemeFontSizeOverride("font_size", 11);
        _content.AddChild(footer);

        AnimateEntry(brandLine, emblem, actionCard);
    }

    public void SetStatus(string message)
    {
        _statusLabel.Text = message;
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
    }

    private static Control BuildTopBar()
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", 8);

        row.AddChild(BuildPill("●  KARİYER MODU", CareerUiTheme.ActionBright, live: true));
        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        row.AddChild(BuildPill("YEREL KAYIT", CareerUiTheme.InkMuted));
        return row;
    }

    private static VBoxContainer BuildHero(out ColorRect brandLine, out PanelContainer emblem)
    {
        var hero = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        hero.AddThemeConstantOverride("separation", 7);

        emblem = new PanelContainer
        {
            CustomMinimumSize = new Vector2(64, 64),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        emblem.AddThemeStyleboxOverride("panel", CareerUiTheme.EmblemPanel());
        var emblemText = new Label
        {
            Text = "FCS",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        CareerUiTheme.StyleHeadline(emblemText);
        emblemText.AddThemeFontSizeOverride("font_size", 19);
        emblemText.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        emblem.AddChild(emblemText);
        hero.AddChild(emblem);

        var eyebrow = new Label
        {
            Text = "KARİYERİN SENİ HATIRLAR",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleEyebrow(eyebrow, CareerUiTheme.Accent);
        hero.AddChild(eyebrow);

        var brand = new Label
        {
            Text = "FOOTBALL",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBrand(brand);
        brand.AddThemeFontSizeOverride("font_size", 34);
        hero.AddChild(brand);

        var brandSecondLine = new Label
        {
            Text = "CAREER SIMULATOR",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleHeadline(brandSecondLine);
        brandSecondLine.AddThemeFontSizeOverride("font_size", 20);
        brandSecondLine.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
        hero.AddChild(brandSecondLine);

        brandLine = new ColorRect
        {
            Color = new Color(
                CareerUiTheme.ActionBright.R,
                CareerUiTheme.ActionBright.G,
                CareerUiTheme.ActionBright.B,
                0.86f),
            CustomMinimumSize = new Vector2(34, 3),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        hero.AddChild(brandLine);

        var tagline = new Label
        {
            Text = "Sportif kararların, insan ilişkilerin ve geçmişin\nyıllar boyunca aynı dünyada yaşamaya devam eder.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(tagline, muted: true);
        tagline.AddThemeFontSizeOverride("font_size", 14);
        hero.AddChild(tagline);

        var facts = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        facts.AddThemeConstantOverride("separation", 6);
        facts.AddChild(BuildPill("KARARLAR", CareerUiTheme.InkMuted));
        facts.AddChild(BuildPill("İLİŞKİLER", CareerUiTheme.InkMuted));
        facts.AddChild(BuildPill("HAFIZA", CareerUiTheme.InkMuted));
        hero.AddChild(facts);

        return hero;
    }

    private Control BuildActionCard()
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.HeroPanel());

        var actions = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        actions.AddThemeConstantOverride("separation", 9);
        panel.AddChild(actions);

        var title = new Label
        {
            Text = "Teknik alan seni bekliyor",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleHeadline(title);
        title.AddThemeFontSizeOverride("font_size", 18);
        actions.AddChild(title);

        var subtitle = new Label
        {
            Text = "Takımı hazırla. Kararını ver. Sonuçları sahiplen.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(subtitle, muted: true);
        subtitle.AddThemeFontSizeOverride("font_size", 13);
        actions.AddChild(subtitle);

        var newButton = new Button
        {
            Name = "NewCareerButton",
            Text = "Yeni Menajer Kariyeri",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = "Yeni bir teknik direktör kariyeri başlat",
        };
        CareerUiTheme.StylePrimaryButton(newButton);
        newButton.CustomMinimumSize = new Vector2(0, 52);
        newButton.Pressed += () => NewCareerRequested?.Invoke();
        actions.AddChild(newButton);

        var continueButton = new Button
        {
            Name = "ContinueButton",
            Text = "Kariyere Devam Et",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = "Son yerel kariyer kaydını yükle",
        };
        CareerUiTheme.StyleSecondaryButton(continueButton);
        continueButton.CustomMinimumSize = new Vector2(0, 48);
        var savePath = Path.Combine(OS.GetUserDataDir(), "career_save.db");
        continueButton.Disabled = !File.Exists(savePath);
        continueButton.Pressed += () => ContinueRequested?.Invoke();
        actions.AddChild(continueButton);

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
                : $"Kayıt bulundu:\n{savePath}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBody(_statusLabel, muted: true);
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
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
        label.AddThemeFontSizeOverride("font_size", 10);
        panel.AddChild(label);
        return panel;
    }

    private void AnimateAtmosphere(Control atmosphere)
    {
        var glow = atmosphere.GetNodeOrNull<CanvasItem>("StadiumGlow");
        if (glow is null)
        {
            return;
        }

        glow.Modulate = new Color(1f, 1f, 1f, 0.68f);
        var pulse = CreateTween().SetLoops();
        pulse.TweenProperty(glow, "modulate:a", 1f, 3.2f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        pulse.TweenProperty(glow, "modulate:a", 0.68f, 3.2f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    private void AnimateEntry(ColorRect brandLine, PanelContainer emblem, Control actionCard)
    {
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
