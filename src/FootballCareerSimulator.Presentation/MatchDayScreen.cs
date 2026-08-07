using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Hub ile sonuç ekranı arasındaki maç günü kontrol noktası.
/// Oyuncu kadro ve taktiğe son kez dokunur; simülasyon yalnızca düdük isteğiyle başlar.
/// </summary>
public partial class MatchDayScreen : Control
{
    private readonly CareerSessionController _controller;
    private Label _headlineLabel = null!;
    private Label _tempoFlashLabel = null!;
    private Label _fixtureLabel = null!;
    private VBoxContainer _briefingLines = null!;
    private VBoxContainer _opponentDossierLines = null!;
    private Control _lineupHost = null!;
    private Label _statusLabel = null!;
    private Button _approveButton = null!;
    private Button _swapButton = null!;
    private Button _kickoffButton = null!;

    public event Action? BackRequested;

    public event Action? KickoffRequested;

    public MatchDayScreen(CareerSessionController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
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
            Text = "MAÇ GÜNÜ",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBrand(brand);
        shell.AddChild(brand);

        _fixtureLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(_fixtureLabel);
        shell.AddChild(_fixtureLabel);

        _headlineLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(_headlineLabel, muted: true);
        shell.AddChild(_headlineLabel);

        _tempoFlashLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(_tempoFlashLabel, muted: true);
        _tempoFlashLabel.Visible = false;
        shell.AddChild(_tempoFlashLabel);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        shell.AddChild(scroll);

        var controls = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        controls.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(controls);

        controls.AddChild(SectionLabel("Son Kontroller"));
        var briefingPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        briefingPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        controls.AddChild(briefingPanel);
        _briefingLines = new VBoxContainer();
        _briefingLines.AddThemeConstantOverride("separation", 6);
        briefingPanel.AddChild(_briefingLines);

        controls.AddChild(SectionLabel("Rakip Dosyası"));
        var opponentPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        opponentPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        controls.AddChild(opponentPanel);
        _opponentDossierLines = new VBoxContainer();
        _opponentDossierLines.AddThemeConstantOverride("separation", 6);
        opponentPanel.AddChild(_opponentDossierLines);

        controls.AddChild(SectionLabel("Kadro"));
        _lineupHost = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        controls.AddChild(_lineupHost);

        var selectionRow = ActionRow();
        controls.AddChild(selectionRow);
        _approveButton = PrimaryButton("Kadro Onayla");
        _approveButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        selectionRow.AddChild(_approveButton);
        _swapButton = SecondaryButton("XI ↔ Yedek");
        _swapButton.Pressed += () => Apply(_controller.SwapLastStarterWithFirstBenchForNextDueMatch());
        selectionRow.AddChild(_swapButton);

        controls.AddChild(SectionLabel("Formasyon"));
        var formationRow = ActionRow();
        controls.AddChild(formationRow);
        formationRow.AddChild(ActionButton("4-4-2", () => _controller.SetTacticFormation(Formation.F442)));
        formationRow.AddChild(ActionButton("4-3-3", () => _controller.SetTacticFormation(Formation.F433)));
        formationRow.AddChild(ActionButton("3-5-2", () => _controller.SetTacticFormation(Formation.F352)));

        controls.AddChild(SectionLabel("Maç Yaklaşımı"));
        var approachRow = ActionRow();
        controls.AddChild(approachRow);
        approachRow.AddChild(ActionButton("Dengeli", () => _controller.SetTacticApproach(TacticalApproach.Balanced)));
        approachRow.AddChild(ActionButton("Hücum", () => _controller.SetTacticApproach(TacticalApproach.Attacking)));
        approachRow.AddChild(ActionButton("Savunma", () => _controller.SetTacticApproach(TacticalApproach.Defensive)));

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
        var backButton = SecondaryButton("Ofise Dön");
        backButton.Pressed += () => Callable.From(() => BackRequested?.Invoke()).CallDeferred();
        footer.AddChild(backButton);
        _kickoffButton = PrimaryButton("Düdüğü Çal");
        _kickoffButton.CustomMinimumSize = new Vector2(220, 42);
        _kickoffButton.Pressed += () => Callable.From(() => KickoffRequested?.Invoke()).CallDeferred();
        footer.AddChild(_kickoffButton);

        RefreshBriefing();
    }

    public void SetStatus(string message)
    {
        _statusLabel.Text = message;
        _statusLabel.Modulate = new Color(1f, 1f, 1f, 0.35f);
        var tween = CreateTween();
        tween.TweenProperty(_statusLabel, "modulate:a", 1f, 0.25f);
    }

    private void Apply(UiActionResult result)
    {
        SetStatus(result.Message);
        RefreshBriefing();
    }

    private void RefreshBriefing()
    {
        var briefing = _controller.BuildNextMatchBriefing();
        _fixtureLabel.Text = briefing.HasMatch ? briefing.FixtureLine : "Maç bekleniyor";
        _headlineLabel.Text = briefing.Headline;

        // Ofisteki tempo flash'ı düdük anına kadar taşır: "kadro kilitli, düdük yakın."
        var tempoFlash = _controller.BuildMatchDayTempoFlash();
        _tempoFlashLabel.Visible = tempoFlash is not null;
        if (tempoFlash is not null)
        {
            _tempoFlashLabel.Text = "· " + tempoFlash.BeatLine;
            _tempoFlashLabel.Modulate = new Color(1f, 1f, 1f, 0.35f);
            var flashTween = CreateTween();
            flashTween.TweenProperty(_tempoFlashLabel, "modulate:a", 1f, 0.4f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
        }

        foreach (var child in _briefingLines.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var beat in briefing.BeatLines)
        {
            var line = new Label
            {
                Text = "· " + beat,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleBody(line);
            _briefingLines.AddChild(line);
        }

        RefreshOpponentDossier();

        RefreshLineupStrip();

        _approveButton.Disabled = !briefing.HasMatch || briefing.IsReadyToKickOff;
        _approveButton.Text = briefing is { HasMatch: true, HasInjuryPressure: true, IsReadyToKickOff: false }
            ? "Sakatsız Kadro Onayla"
            : "Kadro Onayla";
        _swapButton.Disabled = !briefing.HasMatch;
        _kickoffButton.Disabled = !briefing.IsReadyToKickOff;
        _kickoffButton.Text = briefing.IsReadyToKickOff
            ? "Düdüğü Çal"
            : "Düdüğü Çal (önce kadro)";
    }

    private void RefreshOpponentDossier()
    {
        foreach (var child in _opponentDossierLines.GetChildren())
        {
            child.QueueFree();
        }

        var dossier = _controller.BuildOpponentDossier();
        if (dossier is null)
        {
            var empty = new Label
            {
                Text = "Rakip verisi henüz hazır değil.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleBody(empty, muted: true);
            _opponentDossierLines.AddChild(empty);
            return;
        }

        var headline = new Label
        {
            Text = dossier.Headline,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(headline);
        headline.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _opponentDossierLines.AddChild(headline);

        foreach (var detail in dossier.DetailLines)
        {
            var line = new Label
            {
                Text = "· " + detail,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleBody(line, muted: true);
            _opponentDossierLines.AddChild(line);
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
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleBody(empty, muted: true);
            _lineupHost.AddChild(empty);
            return;
        }

        _lineupHost.AddChild(LineupStripUi.BuildPanel(strip));
    }

    private Button ActionButton(string text, Func<UiActionResult> action)
    {
        var button = SecondaryButton(text);
        button.Pressed += () => Apply(action());
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
