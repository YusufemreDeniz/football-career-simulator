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
    private string? _substitutionBridgeLine;

    public event Action? BackRequested;

    /// <summary>İkinci yarı delta + isteğe bağlı isimli HT değişim köprüsü.</summary>
    public event Action<int, string?>? SecondHalfRequested;

    public MatchHalfTimeScreen(CareerSessionController controller, MatchHalfTimeDigest digest)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _digest = digest ?? throw new ArgumentNullException(nameof(digest));
    }

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();
        var margin = MatchScreenUi.CreateStageRoot(
            this,
            new Color(CareerUiTheme.Accent.R, CareerUiTheme.Accent.G, CareerUiTheme.Accent.B, 0.075f));

        var shell = MatchScreenUi.VerticalStack(12);
        shell.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(shell);

        shell.AddChild(MatchScreenUi.StageMarker("03  •  DEVRE ARASI", "SOYUNMA ODASI", CareerUiTheme.Accent));

        var scroll = MatchScreenUi.ScrollArea();
        shell.AddChild(scroll);

        var content = MatchScreenUi.VerticalStack(15);
        scroll.AddChild(content);

        var scorePanel = MatchScreenUi.Card(emphasized: true);
        content.AddChild(scorePanel);
        var scoreContent = MatchScreenUi.VerticalStack(8);
        scorePanel.AddChild(scoreContent);

        var brand = new Label
        {
            Text = _digest.BrandTitle.ToUpperInvariant(),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleEyebrow(brand, CareerUiTheme.Accent);
        scoreContent.AddChild(brand);

        var fixture = new Label
        {
            Text = _digest.FixtureLine,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(fixture, muted: true);
        scoreContent.AddChild(fixture);

        var score = new Label
        {
            Text = _digest.Scoreline,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBrand(score);
        score.AddThemeFontSizeOverride("font_size", 38);
        scoreContent.AddChild(score);

        var halfLabel = new Label
        {
            Text = "45'  •  İLK YARI TAMAMLANDI",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleEyebrow(halfLabel, CareerUiTheme.Accent);
        scoreContent.AddChild(halfLabel);

        var headline = new Label
        {
            Text = _digest.Headline,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(headline);
        headline.AddThemeFontSizeOverride("font_size", 23);
        scoreContent.AddChild(headline);

        content.AddChild(MatchScreenUi.SectionTitle("TEKNİK EKİP", "Devre arası notları"));
        var analysisPanel = MatchScreenUi.Card();
        content.AddChild(analysisPanel);
        var analysis = MatchScreenUi.VerticalStack(9);
        analysisPanel.AddChild(analysis);

        var advice = MatchScreenUi.BodyLine("Öneri: " + _digest.AdviceLine);
        advice.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        analysis.AddChild(advice);

        foreach (var beat in _digest.BeatLines)
        {
            analysis.AddChild(MatchScreenUi.BeatLine(beat));
        }

        content.AddChild(MatchScreenUi.SectionTitle("SAHADAKİ XI", "Mevcut kadro"));
        _lineupHost = MatchScreenUi.VerticalStack(8);
        content.AddChild(_lineupHost);
        RefreshLineupStrip();

        content.AddChild(MatchScreenUi.SectionTitle("İKİNCİ YARI", "Oyun planını seç"));
        var decisionPanel = MatchScreenUi.Card(emphasized: true);
        content.AddChild(decisionPanel);
        var decisionStack = MatchScreenUi.VerticalStack(8);
        decisionPanel.AddChild(decisionStack);

        decisionStack.AddChild(DecisionButton(
            "Aynı Planla Devam Et",
            MatchHalfTimeDigest.DecisionContinue));
        decisionStack.AddChild(DecisionButton(
            "Hücuma Geç",
            MatchHalfTimeDigest.DecisionAttack));
        decisionStack.AddChild(DecisionButton(
            "Savunmaya Çek",
            MatchHalfTimeDigest.DecisionDefend));

        content.AddChild(MatchScreenUi.SectionTitle("DEĞİŞİKLİK", "Kulübeye dokun"));
        var substitutionPanel = MatchScreenUi.Card();
        content.AddChild(substitutionPanel);
        var substitutionStack = MatchScreenUi.VerticalStack(8);
        substitutionPanel.AddChild(substitutionStack);

        var substitutionHint = MatchScreenUi.BodyLine(
            "Bir oyuncu değişikliği yapabilir ve güncel XI'i ikinci yarıya taşıyabilirsin.",
            muted: true);
        substitutionStack.AddChild(substitutionHint);

        var subButton = SecondaryButton("Bir Değişiklik Yap  •  XI ↔ Yedek");
        subButton.Pressed += OnSubstitutionPressed;
        substitutionStack.AddChild(subButton);

        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(_statusLabel, muted: true);
        var statusPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        statusPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatusPanel());
        statusPanel.AddChild(_statusLabel);
        content.AddChild(statusPanel);

        var back = SecondaryButton("Maç Gününe Dön");
        back.Pressed += () => Callable.From(() => BackRequested?.Invoke()).CallDeferred();
        shell.AddChild(back);

        MatchScreenUi.FadeIn(content, this);
    }

    private void OnSubstitutionPressed()
    {
        var result = _controller.SwapLastStarterWithFirstBenchForNextDueMatch();
        _subMade = result.Succeeded || _subMade;
        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.NarrativeBridgeLine))
        {
            _substitutionBridgeLine = result.NarrativeBridgeLine;
        }

        _statusLabel.Text = _subMade
            ? result.Message + "\nDeğişiklik ikinci yarıya yansır — XI şeridi güncellendi."
            : result.Message;
        _statusLabel.AddThemeColorOverride(
            "font_color",
            result.Succeeded ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft);
        if (result.Succeeded)
        {
            RefreshLineupStrip();
        }
    }

    private void RefreshLineupStrip()
    {
        MatchScreenUi.ClearChildren(_lineupHost);

        var strip = _controller.BuildMatchDayLineupStrip();
        if (!strip.HasMatch || strip.StartingXi.Count == 0)
        {
            _lineupHost.AddChild(MatchScreenUi.BodyLine(
                strip.Caption,
                muted: true,
                alignment: HorizontalAlignment.Center));
            return;
        }

        _lineupHost.AddChild(LineupStripUi.BuildPanel(strip, strip.HalfTimeBridgeCaption));
    }

    private Button DecisionButton(string text, int delta)
    {
        var button = PrimaryButton(text);
        button.Pressed += () =>
            Callable.From(() => SecondHalfRequested?.Invoke(delta, _substitutionBridgeLine))
                .CallDeferred();
        return button;
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
