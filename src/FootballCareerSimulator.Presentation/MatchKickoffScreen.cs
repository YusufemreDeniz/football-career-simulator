using FootballCareerSimulator.Application.Competition.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Düdük ile devre arası / sonuç arasındaki maç nabzı anı.
/// Ofisteki "kadro kilitli, düdük yakın" hissini sahaya girişe bağlar.
/// </summary>
public partial class MatchKickoffScreen : Control
{
    private readonly MatchKickoffMoment _moment;

    public event Action? BackRequested;

    /// <summary>Nabız anı okundu — devre arasına veya sonuca geç.</summary>
    public event Action? ProceedRequested;

    public MatchKickoffScreen(MatchKickoffMoment moment)
    {
        _moment = moment ?? throw new ArgumentNullException(nameof(moment));
    }

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();
        AddChild(CareerUiTheme.CreateAtmosphereBackground());

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.GrowHorizontal = GrowDirection.Both;
        margin.GrowVertical = GrowDirection.Both;
        margin.AddThemeConstantOverride("margin_left", 42);
        margin.AddThemeConstantOverride("margin_top", 34);
        margin.AddThemeConstantOverride("margin_right", 42);
        margin.AddThemeConstantOverride("margin_bottom", 30);
        AddChild(margin);

        var shell = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddThemeConstantOverride("separation", 12);
        margin.AddChild(shell);

        var brand = new Label
        {
            Text = _moment.BrandTitle.ToUpperInvariant(),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBrand(brand);
        shell.AddChild(brand);

        var fixture = new Label
        {
            Text = _moment.FixtureLine,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(fixture, muted: true);
        shell.AddChild(fixture);

        var headline = new Label
        {
            Text = _moment.Headline,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(headline);
        headline.AddThemeFontSizeOverride("font_size", 24);
        shell.AddChild(headline);

        foreach (var beat in _moment.BeatLines)
        {
            var line = new Label
            {
                Text = "· " + beat,
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleBody(line);
            shell.AddChild(line);
        }

        var spacer = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        shell.AddChild(spacer);

        var footer = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        footer.AddThemeConstantOverride("separation", 9);
        shell.AddChild(footer);

        var backButton = SecondaryButton("Maç gününe dön");
        backButton.Pressed += () => Callable.From(() => BackRequested?.Invoke()).CallDeferred();
        footer.AddChild(backButton);

        var proceedButton = PrimaryButton("Devam");
        proceedButton.CustomMinimumSize = new Vector2(220, 42);
        proceedButton.Disabled = !_moment.IsReadyToKickOff;
        proceedButton.Text = _moment.IsReadyToKickOff
            ? "Devam"
            : "Devam (düdük kapalı)";
        proceedButton.Pressed += () => Callable.From(() => ProceedRequested?.Invoke()).CallDeferred();
        footer.AddChild(proceedButton);
    }

    private static Button PrimaryButton(string text)
    {
        var button = new Button { Text = text };
        CareerUiTheme.StylePrimaryButton(button);
        return button;
    }

    private static Button SecondaryButton(string text)
    {
        var button = new Button { Text = text };
        CareerUiTheme.StyleSecondaryButton(button);
        return button;
    }
}
