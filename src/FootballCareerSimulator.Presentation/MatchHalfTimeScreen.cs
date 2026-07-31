using FootballCareerSimulator.Application.Competition.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Düdük ile sonuç arasında sınırlı maç içi karar: yaklaşım + isteğe bağlı bir değişiklik.
/// </summary>
public partial class MatchHalfTimeScreen : Control
{
    private readonly CareerSessionController _controller;
    private readonly MatchHalfTimeDigest _digest;
    private Control _lineupHost = null!;
    private Label _statusLabel = null!;
    private bool _subMade;

    public event Action? BackRequested;

    public event Action<int>? SecondHalfRequested;

    public MatchHalfTimeScreen(CareerSessionController controller, MatchHalfTimeDigest digest)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _digest = digest ?? throw new ArgumentNullException(nameof(digest));
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
            Text = _digest.BrandTitle.ToUpperInvariant(),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBrand(brand);
        shell.AddChild(brand);

        var fixture = new Label
        {
            Text = _digest.FixtureLine,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(fixture, muted: true);
        shell.AddChild(fixture);

        var score = new Label
        {
            Text = _digest.Scoreline,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleHeadline(score);
        score.AddThemeFontSizeOverride("font_size", 28);
        shell.AddChild(score);

        var headline = new Label
        {
            Text = _digest.Headline,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(headline);
        shell.AddChild(headline);

        var advice = new Label
        {
            Text = "Öneri: " + _digest.AdviceLine,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(advice, muted: true);
        shell.AddChild(advice);

        foreach (var beat in _digest.BeatLines)
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

        shell.AddChild(SectionLabel("Sahadaki XI"));
        _lineupHost = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        shell.AddChild(_lineupHost);
        RefreshLineupStrip();

        shell.AddChild(SectionLabel("İkinci yarı kararı"));
        var decisionRow = ActionRow();
        shell.AddChild(decisionRow);
        decisionRow.AddChild(DecisionButton(
            "Aynı plan",
            MatchHalfTimeDigest.DecisionContinue));
        decisionRow.AddChild(DecisionButton(
            "Hücuma geç",
            MatchHalfTimeDigest.DecisionAttack));
        decisionRow.AddChild(DecisionButton(
            "Savunmaya çek",
            MatchHalfTimeDigest.DecisionDefend));

        shell.AddChild(SectionLabel("Değişiklik"));
        var subRow = ActionRow();
        shell.AddChild(subRow);
        var subButton = SecondaryButton("Bir değişiklik (XI ↔ Yedek)");
        subButton.Pressed += OnSubstitutionPressed;
        subRow.AddChild(subButton);

        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(_statusLabel, muted: true);
        shell.AddChild(_statusLabel);

        var footer = ActionRow();
        footer.Alignment = BoxContainer.AlignmentMode.Center;
        shell.AddChild(footer);
        var back = SecondaryButton("Maç gününe dön");
        back.Pressed += () => Callable.From(() => BackRequested?.Invoke()).CallDeferred();
        footer.AddChild(back);
    }

    private void OnSubstitutionPressed()
    {
        var result = _controller.SwapLastStarterWithFirstBenchForNextDueMatch();
        _subMade = result.Succeeded || _subMade;
        _statusLabel.Text = _subMade
            ? result.Message + "\nDeğişiklik ikinci yarıya yansır — XI şeridi güncellendi."
            : result.Message;
        if (result.Succeeded)
        {
            RefreshLineupStrip();
        }
    }

    private void RefreshLineupStrip()
    {
        foreach (var child in _lineupHost.GetChildren())
        {
            child.QueueFree();
        }

        var strip = _controller.BuildMatchDayLineupStrip();
        if (!strip.HasMatch || strip.StartingXi.Count == 0)
        {
            var empty = new Label
            {
                Text = strip.Caption,
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleBody(empty, muted: true);
            _lineupHost.AddChild(empty);
            return;
        }

        _lineupHost.AddChild(LineupStripUi.BuildPanel(strip, strip.HalfTimeBridgeCaption));
    }

    private Button DecisionButton(string text, int delta)
    {
        var button = PrimaryButton(text);
        button.Pressed += () =>
            Callable.From(() => SecondHalfRequested?.Invoke(delta)).CallDeferred();
        return button;
    }

    private static HBoxContainer ActionRow()
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        row.AddThemeConstantOverride("separation", 9);
        return row;
    }

    private static Label SectionLabel(string text)
    {
        var label = new Label { Text = text };
        CareerUiTheme.StyleSection(label);
        return label;
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
