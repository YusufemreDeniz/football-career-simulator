using FootballCareerSimulator.Application.TeamPreparation.Queries;
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
    private VBoxContainer _matchupPlanLines = null!;
    private VBoxContainer _lineupCompatibilityLines = null!;
    private Button _applyPrescriptionButton = null!;
    private Control _lineupHost = null!;
    private Control _selectionBoardHost = null!;
    private Label _statusLabel = null!;
    private Button _approveButton = null!;
    private Button _kickoffButton = null!;
    private int? _selectedSquadSlotIndex;
    private bool? _selectedSquadPlayerIsStarter;

    public event Action? BackRequested;

    public event Action? KickoffRequested;

    public MatchDayScreen(CareerSessionController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();
        var margin = MatchScreenUi.CreateStageRoot(this, new Color(CareerUiTheme.Data.R, CareerUiTheme.Data.G, CareerUiTheme.Data.B, 0.055f));

        var shell = MatchScreenUi.VerticalStack(12);
        shell.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(shell);

        shell.AddChild(MatchScreenUi.StageMarker("01  •  MAÇ GÜNÜ", "SON KONTROLLER", CareerUiTheme.Data));

        var scroll = MatchScreenUi.ScrollArea();
        shell.AddChild(scroll);

        var content = MatchScreenUi.VerticalStack(14);
        scroll.AddChild(content);

        var hero = MatchScreenUi.Card(emphasized: true);
        content.AddChild(hero);
        var heroContent = MatchScreenUi.VerticalStack(7);
        hero.AddChild(heroContent);

        var brand = new Label
        {
            Text = "MATCHDAY",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBrand(brand);
        brand.AddThemeFontSizeOverride("font_size", 31);
        heroContent.AddChild(brand);

        _fixtureLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(_fixtureLabel);
        _fixtureLabel.AddThemeFontSizeOverride("font_size", 24);
        heroContent.AddChild(_fixtureLabel);

        _headlineLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(_headlineLabel, muted: true);
        heroContent.AddChild(_headlineLabel);

        _tempoFlashLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(_tempoFlashLabel);
        _tempoFlashLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _tempoFlashLabel.Visible = false;
        heroContent.AddChild(_tempoFlashLabel);

        content.AddChild(MatchScreenUi.SectionTitle("MAÇ PLANI", "Son kontroller"));
        var briefingPanel = MatchScreenUi.Card();
        content.AddChild(briefingPanel);
        _briefingLines = MatchScreenUi.VerticalStack(7);
        briefingPanel.AddChild(_briefingLines);

        content.AddChild(MatchScreenUi.SectionTitle("RAKİP ANALİZİ", "Rakip dosyası"));
        var opponentPanel = MatchScreenUi.Card();
        content.AddChild(opponentPanel);
        _opponentDossierLines = MatchScreenUi.VerticalStack(7);
        opponentPanel.AddChild(_opponentDossierLines);

        content.AddChild(MatchScreenUi.SectionTitle("TAKIM", "Sahaya çıkacak kadro"));
        _lineupHost = MatchScreenUi.VerticalStack(8);
        content.AddChild(_lineupHost);

        _selectionBoardHost = MatchScreenUi.VerticalStack(8);
        content.AddChild(_selectionBoardHost);

        var selectionPanel = MatchScreenUi.Card();
        content.AddChild(selectionPanel);
        var selectionStack = MatchScreenUi.VerticalStack(8);
        selectionPanel.AddChild(selectionStack);
        _approveButton = PrimaryButton("Kadro Onayla");
        _approveButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        selectionStack.AddChild(_approveButton);

        content.AddChild(MatchScreenUi.SectionTitle("TAKTİK TAHTASI", "Formasyon"));
        var formationPanel = MatchScreenUi.Card();
        content.AddChild(formationPanel);
        var formationStack = MatchScreenUi.VerticalStack(8);
        formationPanel.AddChild(formationStack);
        formationStack.AddChild(ActionButton("4-4-2", () => _controller.SetTacticFormation(Formation.F442)));
        formationStack.AddChild(ActionButton("4-3-3", () => _controller.SetTacticFormation(Formation.F433)));
        formationStack.AddChild(ActionButton("3-5-2", () => _controller.SetTacticFormation(Formation.F352)));

        content.AddChild(MatchScreenUi.SectionTitle("KADRO UYUMU", "Formasyonun XI üzerindeki etkisi"));
        var compatibilityPanel = MatchScreenUi.Card();
        content.AddChild(compatibilityPanel);
        _lineupCompatibilityLines = MatchScreenUi.VerticalStack(7);
        compatibilityPanel.AddChild(_lineupCompatibilityLines);

        content.AddChild(MatchScreenUi.SectionTitle("OYUN PLANI", "Maç yaklaşımı"));
        var approachPanel = MatchScreenUi.Card();
        content.AddChild(approachPanel);
        var approachStack = MatchScreenUi.VerticalStack(8);
        approachPanel.AddChild(approachStack);
        approachStack.AddChild(ActionButton("Dengeli", () => _controller.SetTacticApproach(TacticalApproach.Balanced)));
        approachStack.AddChild(ActionButton("Hücum", () => _controller.SetTacticApproach(TacticalApproach.Attacking)));
        approachStack.AddChild(ActionButton("Savunma", () => _controller.SetTacticApproach(TacticalApproach.Defensive)));

        content.AddChild(MatchScreenUi.SectionTitle("EŞLEŞME", "Saha içi avantajlar"));
        var matchupPanel = MatchScreenUi.Card();
        content.AddChild(matchupPanel);
        var matchupStack = MatchScreenUi.VerticalStack(9);
        matchupPanel.AddChild(matchupStack);
        _matchupPlanLines = MatchScreenUi.VerticalStack(7);
        matchupStack.AddChild(_matchupPlanLines);
        _applyPrescriptionButton = PrimaryButton("Öneriyi Uygula");
        _applyPrescriptionButton.TooltipText = "Koç reçetesindeki formasyon ve yaklaşımı uygula";
        _applyPrescriptionButton.Visible = false;
        _applyPrescriptionButton.Pressed += () =>
            Apply(_controller.ApplyAlternativePlanPrescription());
        matchupStack.AddChild(_applyPrescriptionButton);

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
        shell.AddChild(statusPanel);

        var footer = MatchScreenUi.VerticalStack(8);
        shell.AddChild(footer);
        _kickoffButton = PrimaryButton("Düdüğü Çal");
        _kickoffButton.Pressed += () => Callable.From(() => KickoffRequested?.Invoke()).CallDeferred();
        footer.AddChild(_kickoffButton);
        var backButton = SecondaryButton("Ofise Dön");
        backButton.Pressed += () => Callable.From(() => BackRequested?.Invoke()).CallDeferred();
        footer.AddChild(backButton);

        RefreshBriefing();
        MatchScreenUi.FadeIn(content, this);
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

        var tempoFlash = _controller.BuildMatchDayTempoFlash();
        _tempoFlashLabel.Visible = tempoFlash is not null;
        if (tempoFlash is not null)
        {
            _tempoFlashLabel.Text = "●  " + tempoFlash.BeatLine;
            _tempoFlashLabel.Modulate = new Color(1f, 1f, 1f, 0.35f);
            var flashTween = CreateTween();
            flashTween.TweenProperty(_tempoFlashLabel, "modulate:a", 1f, 0.4f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
        }

        MatchScreenUi.ClearChildren(_briefingLines);
        foreach (var beat in briefing.BeatLines)
        {
            _briefingLines.AddChild(MatchScreenUi.BeatLine(beat));
        }

        RefreshOpponentDossier();
        RefreshMatchupPlan();
        RefreshLineupStrip();
        RefreshSquadSelectionBoard();
        RefreshLineupCompatibility();

        _approveButton.Disabled = !briefing.HasMatch || briefing.IsReadyToKickOff;
        _approveButton.Text = briefing is { HasMatch: true, HasInjuryPressure: true, IsReadyToKickOff: false }
            ? "Sakatsız Kadro Onayla"
            : "Kadro Onayla";
        _kickoffButton.Disabled = !briefing.IsReadyToKickOff;
        _kickoffButton.Text = briefing.IsReadyToKickOff
            ? "Düdüğü Çal"
            : "Düdüğü Çal (önce kadro)";
    }

    private void RefreshOpponentDossier()
    {
        MatchScreenUi.ClearChildren(_opponentDossierLines);

        var dossier = _controller.BuildOpponentDossier();
        if (dossier is null)
        {
            var empty = MatchScreenUi.BodyLine("Rakip verisi henüz hazır değil.", muted: true);
            _opponentDossierLines.AddChild(empty);
            return;
        }

        var headline = MatchScreenUi.BodyLine(dossier.Headline);
        headline.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _opponentDossierLines.AddChild(headline);

        foreach (var detail in dossier.DetailLines)
        {
            _opponentDossierLines.AddChild(MatchScreenUi.BeatLine(detail, muted: true));
        }
    }

    private void RefreshMatchupPlan()
    {
        MatchScreenUi.ClearChildren(_matchupPlanLines);
        var prescription = _controller.BuildAlternativePlanPrescription();
        _applyPrescriptionButton.Visible = prescription.HasPrescription;
        _applyPrescriptionButton.Disabled = !prescription.HasPrescription;

        var plan = _controller.BuildMatchupPlan();
        if (plan is null)
        {
            _matchupPlanLines.AddChild(MatchScreenUi.BodyLine(
                "Eşleşme değerlendirmesi için maç ve taktik verisi bekleniyor.",
                muted: true));
            return;
        }

        _matchupPlanLines.AddChild(MatchScreenUi.BodyLine(plan.SelectionLine, muted: true));

        var verdict = MatchScreenUi.BodyLine(plan.VerdictLine);
        verdict.AddThemeColorOverride(
            "font_color",
            plan.Signal switch
            {
                MatchupPlanSignal.Risk => CareerUiTheme.DangerSoft,
                MatchupPlanSignal.Opportunity => CareerUiTheme.ActionBright,
                _ => CareerUiTheme.Accent,
            });
        _matchupPlanLines.AddChild(verdict);

        if (prescription.HasPrescription)
        {
            var recommendation = MatchScreenUi.BodyLine(prescription.PrescriptionLine);
            recommendation.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
            _matchupPlanLines.AddChild(recommendation);
        }
    }

    private void RefreshLineupStrip()
    {
        MatchScreenUi.ClearChildren(_lineupHost);

        var strip = _controller.BuildMatchDayLineupStrip();
        if (!strip.HasMatch || strip.StartingXi.Count == 0)
        {
            _lineupHost.AddChild(MatchScreenUi.BodyLine(strip.Caption, muted: true));
            return;
        }

        _lineupHost.AddChild(LineupStripUi.BuildPanel(strip));
    }

    private void RefreshLineupCompatibility()
    {
        MatchScreenUi.ClearChildren(_lineupCompatibilityLines);

        var compatibility = _controller.BuildLineupCompatibility();
        if (!compatibility.HasLineup)
        {
            _lineupCompatibilityLines.AddChild(MatchScreenUi.BodyLine(
                compatibility.Headline,
                muted: true));
            return;
        }

        var headline = MatchScreenUi.BodyLine(compatibility.Headline);
        headline.AddThemeColorOverride(
            "font_color",
            compatibility.Signal switch
            {
                LineupCompatibilitySignal.Strong => CareerUiTheme.ActionBright,
                LineupCompatibilitySignal.Watch => CareerUiTheme.Accent,
                _ => CareerUiTheme.DangerSoft,
            });
        _lineupCompatibilityLines.AddChild(headline);
        _lineupCompatibilityLines.AddChild(MatchScreenUi.BodyLine(
            compatibility.BalanceLine,
            muted: true));
        _lineupCompatibilityLines.AddChild(MatchScreenUi.BeatLine(
            compatibility.DetailLine,
            muted: compatibility.Signal is LineupCompatibilitySignal.Strong));
    }

    private void RefreshSquadSelectionBoard()
    {
        MatchScreenUi.ClearChildren(_selectionBoardHost);
        var board = _controller.BuildSquadSelectionBoard();
        if (!board.HasMatch)
        {
            _selectedSquadSlotIndex = null;
            _selectedSquadPlayerIsStarter = null;
            _selectionBoardHost.AddChild(MatchScreenUi.BodyLine(
                "Kadro seçim panosu için yeterli uygun oyuncu yok.",
                muted: true));
            return;
        }

        var selectedStillExists = board.StartingXi
            .Concat(board.Bench)
            .Any(player => player.SlotIndex == _selectedSquadSlotIndex);
        if (!selectedStillExists)
        {
            _selectedSquadSlotIndex = null;
            _selectedSquadPlayerIsStarter = null;
        }

        _selectionBoardHost.AddChild(SquadSelectionBoardUi.Build(
            board,
            _selectedSquadSlotIndex,
            SelectSquadPlayer,
            SwapSquadPlayers));
    }

    private void SelectSquadPlayer(SquadSelectionPlayerDigest player)
    {
        if (_selectedSquadSlotIndex == player.SlotIndex)
        {
            _selectedSquadSlotIndex = null;
            _selectedSquadPlayerIsStarter = null;
            RefreshSquadSelectionBoard();
            return;
        }

        if (_selectedSquadSlotIndex is null
            || _selectedSquadPlayerIsStarter == player.IsStarter)
        {
            _selectedSquadSlotIndex = player.SlotIndex;
            _selectedSquadPlayerIsStarter = player.IsStarter;
            RefreshSquadSelectionBoard();
            return;
        }

        var starterSlot = player.IsStarter ? player.SlotIndex : _selectedSquadSlotIndex.Value;
        var benchSlot = player.IsStarter ? _selectedSquadSlotIndex.Value : player.SlotIndex;
        SwapSquadPlayers(starterSlot, benchSlot);
    }

    private void SwapSquadPlayers(int starterSlotIndex, int benchSlotIndex)
    {
        _selectedSquadSlotIndex = null;
        _selectedSquadPlayerIsStarter = null;
        Apply(_controller.SwapStarterWithBenchForNextDueMatch(starterSlotIndex, benchSlotIndex));
    }

    private Button ActionButton(string text, Func<UiActionResult> action)
    {
        var button = SecondaryButton(text);
        button.Pressed += () => Apply(action());
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

/// <summary>Maç gecesi ekranlarının küçük, kod-tabanlı mobil yerleşim yapı taşları.</summary>
internal static class MatchScreenUi
{
    public static MarginContainer CreateStageRoot(Control owner, Color stageWash)
    {
        owner.AddChild(CareerUiTheme.CreateAtmosphereBackground());

        var wash = new ColorRect
        {
            Color = stageWash,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        wash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        owner.AddChild(wash);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.GrowHorizontal = Control.GrowDirection.Both;
        margin.GrowVertical = Control.GrowDirection.Both;
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        owner.AddChild(margin);
        return margin;
    }

    public static VBoxContainer VerticalStack(int separation)
    {
        var stack = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        stack.AddThemeConstantOverride("separation", separation);
        return stack;
    }

    public static MobileScrollContainer ScrollArea()
    {
        return new MobileScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
    }

    public static PanelContainer Card(bool emphasized = false)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized));
        return panel;
    }

    public static PanelContainer StageMarker(string stage, string state, Color color)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePanel(color));

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", 8);
        panel.AddChild(row);

        var stageLabel = new Label
        {
            Text = stage,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleEyebrow(stageLabel, color);
        row.AddChild(stageLabel);

        var stateLabel = new Label
        {
            Text = state,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        CareerUiTheme.StyleEyebrow(stateLabel, CareerUiTheme.InkMuted);
        row.AddChild(stateLabel);
        return panel;
    }

    public static Control SectionTitle(string eyebrow, string title)
    {
        var stack = VerticalStack(2);
        var eyebrowLabel = new Label { Text = eyebrow };
        CareerUiTheme.StyleEyebrow(eyebrowLabel);
        stack.AddChild(eyebrowLabel);

        var titleLabel = new Label
        {
            Text = title,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(titleLabel);
        titleLabel.AddThemeFontSizeOverride("font_size", 19);
        stack.AddChild(titleLabel);
        return stack;
    }

    public static Label BodyLine(string text, bool muted = false, HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = alignment,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(label, muted);
        return label;
    }

    public static Label BeatLine(string text, bool muted = false)
    {
        var label = BodyLine("•  " + text, muted);
        return label;
    }

    public static void ClearChildren(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            child.QueueFree();
        }
    }

    public static void FadeIn(Control content, Node owner)
    {
        content.Modulate = new Color(1f, 1f, 1f, 0f);
        var tween = owner.CreateTween();
        tween.TweenProperty(content, "modulate:a", 1f, 0.35f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }
}
