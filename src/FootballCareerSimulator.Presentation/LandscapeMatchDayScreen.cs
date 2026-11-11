using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Landscape match command center. The pitch is the primary control surface.
/// </summary>
public partial class LandscapeMatchDayScreen : Control
{
    private readonly CareerSessionController _controller;
    private Label _fixtureLabel = null!;
    private Label _headlineLabel = null!;
    private Label _statusLabel = null!;
    private Label _selectionHintLabel = null!;
    private Control _pitchHost = null!;
    private VBoxContainer _benchHost = null!;
    private VBoxContainer _tacticHost = null!;
    private Button _approveButton = null!;
    private Button _kickoffButton = null!;
    private LandscapeMatchLayoutProfile _layout = null!;
    private int? _selectedSquadSlotIndex;
    private bool? _selectedSquadPlayerIsStarter;

    private PanelContainer _readinessBadge = null!;
    private Label _readinessLabel = null!;
    private Label _opponentLabel = null!;

    public event Action? BackRequested;
    public event Action? KickoffRequested;

    public LandscapeMatchDayScreen(CareerSessionController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();
        var viewport = GetViewportRect().Size;
        _layout = LandscapeMatchLayoutProfile.Resolve(
            Mathf.RoundToInt(viewport.X),
            Mathf.RoundToInt(viewport.Y));
        var margin = MatchScreenUi.CreateStageRoot(
            this,
            new Color(CareerUiTheme.Data.R, CareerUiTheme.Data.G, CareerUiTheme.Data.B, 0.07f),
            _layout.HorizontalMargin,
            _layout.VerticalMargin);

        var shell = MatchScreenUi.VerticalStack(_layout.SectionSeparation);
        shell.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(shell);

        var headerPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headerPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized: false));
        shell.AddChild(headerPanel);

        var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddThemeConstantOverride("separation", 12);
        headerPanel.AddChild(header);
        var back = SecondaryButton("← Geri");
        back.CustomMinimumSize = new Vector2(78, _layout.ActionButtonHeight);
        back.Pressed += () => Callable.From(() => BackRequested?.Invoke()).CallDeferred();
        header.AddChild(back);

        var heading = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(heading);
        var stage = new Label { Text = "MAÇ GÜNÜ  •  TAKTİK & KADRO BRİFİNGİ" };
        CareerUiTheme.StyleEyebrow(stage, CareerUiTheme.Accent);
        heading.AddChild(stage);
        _fixtureLabel = new Label();
        CareerUiTheme.StyleHeadline(_fixtureLabel);
        _fixtureLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(22));
        heading.AddChild(_fixtureLabel);

        _readinessBadge = new PanelContainer();
        _readinessBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
        _readinessLabel = new Label { Text = "Kadro Onaylandı" };
        CareerUiTheme.StyleEyebrow(_readinessLabel, CareerUiTheme.ActionBright);
        _readinessBadge.AddChild(_readinessLabel);
        header.AddChild(_readinessBadge);

        // FM27 Sub-navigation tabs: Diziliş | Oyun Şekli | Savunma | Hücum | Duran Toplar | Kaptan
        var subTabsPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        subTabsPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Surface, 0.90f));
        shell.AddChild(subTabsPanel);

        var subTabsRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        subTabsRow.AddThemeConstantOverride("separation", 6);
        subTabsPanel.AddChild(subTabsRow);

        string[] tabNames = ["Diziliş", "Oyun Şekli", "Savunma", "Hücum", "Duran Toplar", "Kaptan"];
        for (var i = 0; i < tabNames.Length; i++)
        {
            var tabName = tabNames[i];
            var isDizilis = i == 0;
            var tabBtn = new Button
            {
                Text = tabName,
                CustomMinimumSize = new Vector2(0, 30),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            if (isDizilis)
            {
                CareerUiTheme.StylePrimaryButton(tabBtn);
            }
            else
            {
                CareerUiTheme.StyleSecondaryButton(tabBtn);
            }
            subTabsRow.AddChild(tabBtn);
        }

        var body = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        body.AddThemeConstantOverride("separation", _layout.SectionSeparation);
        shell.AddChild(body);

        var pitchPanel = MatchScreenUi.Card(emphasized: true);
        pitchPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        pitchPanel.SizeFlagsStretchRatio = 1.7f;
        pitchPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddChild(pitchPanel);
        var pitchStack = MatchScreenUi.VerticalStack(8);
        pitchPanel.AddChild(pitchStack);
        var pitchHeader = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        pitchHeader.AddThemeConstantOverride("separation", 8);
        var pitchIconBadge = new PanelContainer();
        pitchIconBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.22f));
        var pitchIconLabel = new Label { Text = "SAHA DİZİLİŞİ" };
        CareerUiTheme.StyleEyebrow(pitchIconLabel, CareerUiTheme.ActionBright);
        pitchIconBadge.AddChild(pitchIconLabel);
        pitchHeader.AddChild(pitchIconBadge);

        var pitchTitle = new Label { Text = "İLK 11 YERLEŞİMİ", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CareerUiTheme.ApplyDisplayFont(pitchTitle);
        pitchTitle.AddThemeColorOverride("font_color", CareerUiTheme.TextPrimary);
        pitchHeader.AddChild(pitchTitle);

        var autoAlignButton = SecondaryButton("Mevkilere Göre Diz");
        autoAlignButton.CustomMinimumSize = new Vector2(170, 32);
        autoAlignButton.TooltipText = "Sahadaki oyuncuları mevkilerine (GK, DEF, MID, FWD) en uygun diziliş konumlarına otomatik yerleştirir.";
        autoAlignButton.Pressed += () => Apply(_controller.AutoAlignStartingXiByFormationForNextDueMatch());
        pitchHeader.AddChild(autoAlignButton);

        pitchStack.AddChild(pitchHeader);
        _pitchHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        pitchStack.AddChild(_pitchHost);

        var commandPanel = MatchScreenUi.Card();
        commandPanel.CustomMinimumSize = new Vector2(_layout.CommandPanelWidth, 0);
        commandPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddChild(commandPanel);
        var commandScroll = MatchScreenUi.ScrollArea();
        commandPanel.AddChild(commandScroll);
        var commandStack = MatchScreenUi.VerticalStack(9);
        commandScroll.AddChild(commandStack);

        _headlineLabel = MatchScreenUi.BodyLine(string.Empty);
        _headlineLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        commandStack.AddChild(_headlineLabel);

        _selectionHintLabel = MatchScreenUi.BodyLine(string.Empty, muted: true);
        commandStack.AddChild(_selectionHintLabel);

        var benchCard = MatchScreenUi.Card(emphasized: false);
        commandStack.AddChild(benchCard);
        var benchStack = MatchScreenUi.VerticalStack(6);
        benchCard.AddChild(benchStack);
        var benchTitle = new Label { Text = "YEDEK KULÜBESİ" };
        CareerUiTheme.StyleEyebrow(benchTitle, CareerUiTheme.Data);
        benchStack.AddChild(benchTitle);

        _benchHost = MatchScreenUi.VerticalStack(0);
        benchStack.AddChild(_benchHost);

        var tacticsTitle = new Label { Text = "MAÇ PLANI" };
        CareerUiTheme.StyleEyebrow(tacticsTitle, CareerUiTheme.Data);
        commandStack.AddChild(tacticsTitle);
        _tacticHost = MatchScreenUi.VerticalStack(6);
        commandStack.AddChild(_tacticHost);

        var opponentCard = MatchScreenUi.Card(emphasized: true);
        commandStack.AddChild(opponentCard);
        var opponentStack = MatchScreenUi.VerticalStack(6);
        opponentCard.AddChild(opponentStack);
        var opponentTitle = new Label { Text = "RAKİP ANALİZİ & TEHLİKELER" };
        CareerUiTheme.StyleEyebrow(opponentTitle, CareerUiTheme.Accent);
        opponentStack.AddChild(opponentTitle);
        var dossier = _controller.BuildOpponentDossier();
        _opponentLabel = MatchScreenUi.BodyLine(
            dossier is null ? "Rakip analizi hazırlanıyor." : dossier.Headline,
            muted: true);
        opponentStack.AddChild(_opponentLabel);

        _statusLabel = MatchScreenUi.BodyLine(string.Empty, muted: true);
        var statusPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        statusPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatusPanel());
        statusPanel.AddChild(_statusLabel);
        commandStack.AddChild(statusPanel);

        var footer = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        footer.AddThemeConstantOverride("separation", 10);
        shell.AddChild(footer);
        _approveButton = PrimaryButton("Kadro Onayla");
        _approveButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _approveButton.CustomMinimumSize = new Vector2(0, _layout.ActionButtonHeight);
        _approveButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        footer.AddChild(_approveButton);
        _kickoffButton = PrimaryButton("Düdüğü Çal");
        _kickoffButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _kickoffButton.CustomMinimumSize = new Vector2(0, _layout.ActionButtonHeight);
        _kickoffButton.Pressed += () => Callable.From(() => KickoffRequested?.Invoke()).CallDeferred();
        footer.AddChild(_kickoffButton);

        RefreshScreen();
        MatchScreenUi.FadeIn(body, this);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationWMGoBackRequest)
        {
            return;
        }

        foreach (var child in GetChildren())
        {
            if (child is FormationPickerModal modal)
            {
                modal.QueueFree();
                return;
            }
        }

        Callable.From(() => BackRequested?.Invoke()).CallDeferred();
    }

    public void SetStatus(string message)
    {
        _statusLabel.Text = message;
        if (CareerUiTheme.ReducedMotion)
        {
            _statusLabel.Modulate = Colors.White;
            return;
        }

        _statusLabel.Modulate = new Color(1f, 1f, 1f, 0.35f);
        CreateTween().TweenProperty(_statusLabel, "modulate:a", 1f, 0.2f);
    }

    private void RefreshScreen()
    {
        var briefing = _controller.BuildNextMatchBriefing();
        _fixtureLabel.Text = briefing.HasMatch ? briefing.FixtureLine : "Mac bekleniyor";
        _headlineLabel.Text = briefing.Headline;
        _approveButton.Disabled = !briefing.HasMatch || briefing.IsReadyToKickOff;
        _approveButton.Text = briefing.IsReadyToKickOff ? "Kadro Onaylandı" : "Kadroyu Onayla";
        if (briefing.IsReadyToKickOff)
        {
            CareerUiTheme.StyleSecondaryButton(_approveButton);
        }
        else
        {
            CareerUiTheme.StyleAccentButton(_approveButton);
        }

        _kickoffButton.Disabled = !briefing.IsReadyToKickOff;
        _kickoffButton.Text = briefing.IsReadyToKickOff ? "MAÇA BAŞLA (CANLI YAYIN)" : "Önce kadroyu onayla";
        if (briefing.IsReadyToKickOff)
        {
            CareerUiTheme.StylePrimaryButton(_kickoffButton);
        }
        else
        {
            CareerUiTheme.StyleSecondaryButton(_kickoffButton);
        }

        _readinessBadge.Visible = briefing.HasMatch;
        if (briefing.IsReadyToKickOff)
        {
            _readinessBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
            _readinessLabel.Text = "Kadro Onaylandı";
            _readinessLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        }
        else
        {
            _readinessBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning, 0.20f));
            _readinessLabel.Text = "Onay Bekliyor";
            _readinessLabel.AddThemeColorOverride("font_color", CareerUiTheme.ColorWarning);
        }

        var dossier = _controller.BuildOpponentDossier();
        _opponentLabel.Text = dossier is null ? "Rakip analizi hazırlanıyor." : dossier.Headline;

        RefreshPitch();
        RefreshTactics();
    }

    private void RefreshPitch()
    {
        MatchScreenUi.ClearChildren(_pitchHost);
        MatchScreenUi.ClearChildren(_benchHost);
        var board = _controller.BuildSquadSelectionBoard();
        if (!board.HasMatch)
        {
            _pitchHost.AddChild(MatchScreenUi.BodyLine("Mac kadrosu hazir degil.", muted: true));
            return;
        }

        var selectedStillExists = board.StartingXi.Concat(board.Bench)
            .Any(player => player.SlotIndex == _selectedSquadSlotIndex);
        if (!selectedStillExists)
        {
            _selectedSquadSlotIndex = null;
            _selectedSquadPlayerIsStarter = null;
        }

        _selectionHintLabel.Text = _selectedSquadSlotIndex is null
            ? "Dokun ve yer değiştir: Saha ↔ Saha, Yedek ↔ Yedek veya Saha ↔ Yedek."
            : "Yer değiştirmek istediğin ikinci oyuncuya dokun (aynısına dokunursan iptal olur).";
        _benchHost.AddChild(TacticalPitchBoardUi.BuildBench(
            board,
            _selectedSquadSlotIndex,
            SelectSquadPlayer,
            true,
            new Vector2(_layout.PlayerButtonWidth, _layout.PlayerButtonHeight)));
        var tactic = _controller.GetManagedTacticPlan();
        _pitchHost.AddChild(TacticalPitchBoardUi.BuildPitch(
            board,
            _selectedSquadSlotIndex,
            SelectSquadPlayer,
            true,
            new Vector2(_layout.PitchMinimumWidth, _layout.PitchMinimumHeight),
            new Vector2(_layout.PlayerButtonWidth, _layout.PlayerButtonHeight),
            tactic.Formation));
    }

    private void RefreshTactics()
    {
        MatchScreenUi.ClearChildren(_tacticHost);
        var plan = _controller.GetManagedTacticPlan();

        var formationBlock = MatchScreenUi.VerticalStack(4);
        var formationTitle = new Label { Text = "DİZİLİŞ" };
        CareerUiTheme.StyleEyebrow(formationTitle);
        formationBlock.AddChild(formationTitle);

        var formationRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        formationRow.AddThemeConstantOverride("separation", 5);
        formationBlock.AddChild(formationRow);

        var changeFormationBtn = PrimaryButton($"{FormationPickerModal.FormatLabel(plan.Formation)} (14 Diziliş)");
        changeFormationBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        changeFormationBtn.CustomMinimumSize = new Vector2(0, 38);
        changeFormationBtn.Pressed += () =>
        {
            FormationPickerModal.Open(this, plan.Formation, formation =>
            {
                Apply(_controller.SetTacticFormation(formation));
            });
        };
        formationRow.AddChild(changeFormationBtn);
        _tacticHost.AddChild(formationBlock);

        _tacticHost.AddChild(BuildOptionRow("YAKLAŞIM", [
            ("Dengeli", plan.Approach == TacticalApproach.Balanced, () => _controller.SetTacticApproach(TacticalApproach.Balanced)),
            ("Hücum", plan.Approach == TacticalApproach.Attacking, () => _controller.SetTacticApproach(TacticalApproach.Attacking)),
            ("Savunma", plan.Approach == TacticalApproach.Defensive, () => _controller.SetTacticApproach(TacticalApproach.Defensive)),
        ]));
    }

    private Control BuildOptionRow(string title, IReadOnlyList<(string Text, bool Selected, Func<UiActionResult> Action)> options)
    {
        var root = MatchScreenUi.VerticalStack(4);
        var label = new Label { Text = title };
        CareerUiTheme.StyleEyebrow(label);
        root.AddChild(label);
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 5);
        root.AddChild(row);
        foreach (var option in options)
        {
            var button = option.Selected ? PrimaryButton(option.Text) : SecondaryButton(option.Text);
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            button.CustomMinimumSize = new Vector2(0, 38);
            button.Pressed += () => Apply(option.Action());
            row.AddChild(button);
        }
        return root;
    }

    private void SelectSquadPlayer(SquadSelectionPlayerDigest player)
    {
        if (_selectedSquadSlotIndex == player.SlotIndex)
        {
            _selectedSquadSlotIndex = null;
            _selectedSquadPlayerIsStarter = null;
            SetStatus("Seçim kaldırıldı.");
            RefreshPitch();
            return;
        }

        if (_selectedSquadSlotIndex is null)
        {
            _selectedSquadSlotIndex = player.SlotIndex;
            _selectedSquadPlayerIsStarter = player.IsStarter;
            var groupName = player.IsStarter ? "Sahadaki" : "Yedekteki";
            SetStatus($"{groupName} {player.DisplayName} ({player.PositionCode}) seçildi. Yer değiştirmek için ikinci bir oyuncuya dokun.");
            RefreshPitch();
            return;
        }

        var firstSlot = _selectedSquadSlotIndex.Value;
        var firstIsStarter = _selectedSquadPlayerIsStarter.GetValueOrDefault(true);
        var secondSlot = player.SlotIndex;
        var secondIsStarter = player.IsStarter;

        _selectedSquadSlotIndex = null;
        _selectedSquadPlayerIsStarter = null;

        if (firstIsStarter && secondIsStarter)
        {
            Apply(_controller.SwapTwoStartersForNextDueMatch(firstSlot, secondSlot));
        }
        else if (!firstIsStarter && !secondIsStarter)
        {
            Apply(_controller.SwapTwoBenchForNextDueMatch(firstSlot, secondSlot));
        }
        else
        {
            var starter = firstIsStarter ? firstSlot : secondSlot;
            var bench = firstIsStarter ? secondSlot : firstSlot;
            Apply(_controller.SwapStarterWithBenchForNextDueMatch(starter, bench));
        }
    }

    private void Apply(UiActionResult result)
    {
        SetStatus(result.Message);
        RefreshScreen();
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
