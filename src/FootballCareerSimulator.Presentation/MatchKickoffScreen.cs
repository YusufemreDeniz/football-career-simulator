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
        var margin = MatchScreenUi.CreateStageRoot(
            this,
            new Color(CareerUiTheme.Action.R, CareerUiTheme.Action.G, CareerUiTheme.Action.B, 0.075f));

        var shell = MatchScreenUi.VerticalStack(12);
        shell.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(shell);

        shell.AddChild(MatchScreenUi.StageMarker("02  •  SAHAYA ÇIKIŞ", "CANLI", CareerUiTheme.ActionBright));

        var scroll = MatchScreenUi.ScrollArea();
        shell.AddChild(scroll);

        var content = MatchScreenUi.VerticalStack(16);
        scroll.AddChild(content);

        var hero = MatchScreenUi.Card(emphasized: true);
        content.AddChild(hero);
        var heroContent = MatchScreenUi.VerticalStack(9);
        hero.AddChild(heroContent);

        var live = new Label
        {
            Text = "●  DÜDÜK ÖNCESİ",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleEyebrow(live, CareerUiTheme.ActionBright);
        heroContent.AddChild(live);

        var brand = new Label
        {
            Text = _moment.BrandTitle.ToUpperInvariant(),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBrand(brand);
        brand.AddThemeFontSizeOverride("font_size", 30);
        heroContent.AddChild(brand);

        var fixture = new Label
        {
            Text = _moment.FixtureLine,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(fixture, muted: true);
        heroContent.AddChild(fixture);

        var divider = new ColorRect
        {
            Color = new Color(CareerUiTheme.Action.R, CareerUiTheme.Action.G, CareerUiTheme.Action.B, 0.64f),
            CustomMinimumSize = new Vector2(0, 2),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        heroContent.AddChild(divider);

        var headline = new Label
        {
            Text = _moment.Headline,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(headline);
        headline.AddThemeFontSizeOverride("font_size", 24);
        heroContent.AddChild(headline);

        content.AddChild(MatchScreenUi.SectionTitle("TÜNEL", "Maçın ilk nefesi"));
        var beatsPanel = MatchScreenUi.Card();
        content.AddChild(beatsPanel);
        var beats = MatchScreenUi.VerticalStack(10);
        beatsPanel.AddChild(beats);

        var beatIndex = 1;
        foreach (var beat in _moment.BeatLines)
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            row.AddThemeConstantOverride("separation", 10);
            beats.AddChild(row);

            var number = new Label
            {
                Text = beatIndex.ToString("00"),
                CustomMinimumSize = new Vector2(28, 0),
            };
            CareerUiTheme.StyleEyebrow(number, CareerUiTheme.ActionBright);
            row.AddChild(number);

            var line = MatchScreenUi.BodyLine(beat);
            line.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(line);
            beatIndex++;
        }

        if (_moment.BeatLines.Count == 0)
        {
            beats.AddChild(MatchScreenUi.BodyLine("Takımlar sahaya çıkmaya hazır.", muted: true));
        }

        var readinessPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        readinessPanel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.BadgePanel(_moment.IsReadyToKickOff ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft));
        content.AddChild(readinessPanel);

        var readiness = MatchScreenUi.BodyLine(
            _moment.IsReadyToKickOff
                ? "Kadro kilitli. Tribün hazır. Maç başlıyor."
                : "Başlama düdüğü için maç hazırlığının tamamlanması gerekiyor.",
            alignment: HorizontalAlignment.Center);
        readiness.AddThemeColorOverride(
            "font_color",
            _moment.IsReadyToKickOff ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft);
        readinessPanel.AddChild(readiness);

        var footer = MatchScreenUi.VerticalStack(8);
        shell.AddChild(footer);

        var proceedButton = PrimaryButton("Devam");
        proceedButton.Disabled = !_moment.IsReadyToKickOff;
        proceedButton.Text = _moment.IsReadyToKickOff
            ? "Maçı Başlat"
            : "Maçı Başlat (düdük kapalı)";
        proceedButton.Pressed += () => Callable.From(() => ProceedRequested?.Invoke()).CallDeferred();
        footer.AddChild(proceedButton);

        var backButton = SecondaryButton("Maç Gününe Dön");
        backButton.Pressed += () => Callable.From(() => BackRequested?.Invoke()).CallDeferred();
        footer.AddChild(backButton);

        MatchScreenUi.FadeIn(content, this);
        var pulseTween = CreateTween().SetLoops();
        pulseTween.TweenProperty(live, "modulate:a", 0.52f, 0.72f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        pulseTween.TweenProperty(live, "modulate:a", 1f, 0.72f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    private static Button PrimaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StylePrimaryButton(button);
        return button;
    }

    private static Button SecondaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleSecondaryButton(button);
        return button;
    }
}
