using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class MainMenuScreen : Control
{
    private Label _statusLabel = null!;

    public event Action? NewCareerRequested;

    public event Action? ContinueRequested;

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();
        AddChild(CareerUiTheme.CreateAtmosphereBackground());

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.GrowHorizontal = GrowDirection.Both;
        margin.GrowVertical = GrowDirection.Both;
        margin.AddThemeConstantOverride("margin_left", 48);
        margin.AddThemeConstantOverride("margin_top", 56);
        margin.AddThemeConstantOverride("margin_right", 48);
        margin.AddThemeConstantOverride("margin_bottom", 40);
        AddChild(margin);

        var layout = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        layout.AddThemeConstantOverride("separation", 14);
        margin.AddChild(layout);

        var brand = new Label
        {
            Text = "Football Career Simulator",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBrand(brand);
        layout.AddChild(brand);

        var brandLine = new ColorRect
        {
            Color = new Color(CareerUiTheme.Accent.R, CareerUiTheme.Accent.G, CareerUiTheme.Accent.B, 0.55f),
            CustomMinimumSize = new Vector2(40, 3),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        layout.AddChild(brandLine);

        var tagline = new Label
        {
            Text = "Teknik direktör kariyeri — kararların yıllarca hatırlanır",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBody(tagline, muted: true);
        layout.AddChild(tagline);

        var newButton = new Button { Text = "Yeni Menajer Kariyeri" };
        CareerUiTheme.StylePrimaryButton(newButton);
        newButton.CustomMinimumSize = new Vector2(240, 40);
        newButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        newButton.Pressed += () => NewCareerRequested?.Invoke();
        layout.AddChild(newButton);

        var continueButton = new Button { Text = "Devam Et" };
        CareerUiTheme.StyleSecondaryButton(continueButton);
        continueButton.CustomMinimumSize = new Vector2(240, 36);
        continueButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        var savePath = Path.Combine(OS.GetUserDataDir(), "career_save.db");
        continueButton.Disabled = !File.Exists(savePath);
        continueButton.Pressed += () => ContinueRequested?.Invoke();
        layout.AddChild(continueButton);

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
        layout.AddChild(_statusLabel);

        brandLine.CustomMinimumSize = new Vector2(24, 3);
        var brandTween = CreateTween();
        brandTween.TweenProperty(brandLine, "custom_minimum_size", new Vector2(180, 3), 0.6f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        layout.Modulate = new Color(1f, 1f, 1f, 0f);
        var fadeTween = CreateTween();
        fadeTween.TweenProperty(layout, "modulate:a", 1f, 0.45f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    public void SetStatus(string message) => _statusLabel.Text = message;
}
