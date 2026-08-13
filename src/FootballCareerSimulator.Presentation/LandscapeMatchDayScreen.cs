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
    private VBoxContainer _tacticHost = null!;
    private Button _approveButton = null!;
    private Button _kickoffButton = null!;
    private int? _selectedSquadSlotIndex;
    private bool? _selectedSquadPlayerIsStarter;

    public event Action? BackRequested;
    public event Action? KickoffRequested;

    public LandscapeMatchDayScreen(CareerSessionController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();
        var margin = MatchScreenUi.CreateStageRoot(
            this,
            new Color(CareerUiTheme.Data.R, CareerUiTheme.Data.G, CareerUiTheme.Data.B, 0.07f));

        var shell = MatchScreenUi.VerticalStack(10);
        shell.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(shell);

        var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddThemeConstantOverride("separation", 10);
        shell.AddChild(header);
        var back = SecondaryButton("Geri");
        back.CustomMinimumSize = new Vector2(74, 42);
        back.Pressed += () => Callable.From(() => BackRequested?.Invoke()).CallDeferred();
        header.AddChild(back);

        var heading = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(heading);
        var stage = new Label { Text = "MAC GUNU  /  TAKTIK TAHTASI" };
        CareerUiTheme.StyleEyebrow(stage, CareerUiTheme.Data);
        heading.AddChild(stage);
        _fixtureLabel = new Label();
        CareerUiTheme.StyleHeadline(_fixtureLabel);
        _fixtureLabel.AddThemeFontSizeOverride("font_size", 22);
        heading.AddChild(_fixtureLabel);

        var body = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        body.AddThemeConstantOverride("separation", 12);
        shell.AddChild(body);

        var pitchPanel = MatchScreenUi.Card(emphasized: true);
        pitchPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        pitchPanel.SizeFlagsStretchRatio = 1.7f;
        pitchPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddChild(pitchPanel);
        var pitchStack = MatchScreenUi.VerticalStack(7);
        pitchPanel.AddChild(pitchStack);
        var pitchTitle = new Label { Text = "SAHADAKI ILK 11" };
        CareerUiTheme.StyleEyebrow(pitchTitle, CareerUiTheme.ActionBright);
        pitchStack.AddChild(pitchTitle);
        _pitchHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        pitchStack.AddChild(_pitchHost);

        var commandPanel = MatchScreenUi.Card();
        commandPanel.CustomMinimumSize = new Vector2(310, 0);
        commandPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddChild(commandPanel);
        var commandStack = MatchScreenUi.VerticalStack(9);
        commandPanel.AddChild(commandStack);

        _headlineLabel = MatchScreenUi.BodyLine(string.Empty);
        _headlineLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        commandStack.AddChild(_headlineLabel);

        _selectionHintLabel = MatchScreenUi.BodyLine(string.Empty, muted: true);
        commandStack.AddChild(_selectionHintLabel);

        var tacticsTitle = new Label { Text = "MAC PLANI" };
        CareerUiTheme.StyleEyebrow(tacticsTitle, CareerUiTheme.Data);
        commandStack.AddChild(tacticsTitle);
        _tacticHost = MatchScreenUi.VerticalStack(6);
        commandStack.AddChild(_tacticHost);

        var dossier = _controller.BuildOpponentDossier();
        var opponent = MatchScreenUi.BodyLine(
            dossier is null ? "Rakip analizi hazirlaniyor." : dossier.Headline,
            muted: true);
        commandStack.AddChild(opponent);

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
        _approveButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        footer.AddChild(_approveButton);
        _kickoffButton = PrimaryButton("Dudugu Cal");
        _kickoffButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _kickoffButton.Pressed += () => Callable.From(() => KickoffRequested?.Invoke()).CallDeferred();
        footer.AddChild(_kickoffButton);

        RefreshScreen();
        MatchScreenUi.FadeIn(body, this);
    }

    public void SetStatus(string message)
    {
        _statusLabel.Text = message;
        _statusLabel.Modulate = new Color(1f, 1f, 1f, 0.35f);
        CreateTween().TweenProperty(_statusLabel, "modulate:a", 1f, 0.2f);
    }

    private void RefreshScreen()
    {
        var briefing = _controller.BuildNextMatchBriefing();
        _fixtureLabel.Text = briefing.HasMatch ? briefing.FixtureLine : "Mac bekleniyor";
        _headlineLabel.Text = briefing.Headline;
        _approveButton.Disabled = !briefing.HasMatch || briefing.IsReadyToKickOff;
        _approveButton.Text = briefing.IsReadyToKickOff ? "Kadro Onaylandi" : "Kadro Onayla";
        _kickoffButton.Disabled = !briefing.IsReadyToKickOff;
        _kickoffButton.Text = briefing.IsReadyToKickOff ? "Dudugu Cal" : "Once kadroyu onayla";
        RefreshPitch();
        RefreshTactics();
    }

    private void RefreshPitch()
    {
        MatchScreenUi.ClearChildren(_pitchHost);
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
            ? "Bir oyuncuya, sonra karsisindaki gruptan degisecek oyuncuya dokun."
            : "Degisiklik icin karsisindaki gruptan bir oyuncu sec.";
        _pitchHost.AddChild(TacticalPitchBoardUi.Build(
            board,
            _selectedSquadSlotIndex,
            SelectSquadPlayer,
            SwapSquadPlayers));
    }

    private void RefreshTactics()
    {
        MatchScreenUi.ClearChildren(_tacticHost);
        var plan = _controller.GetManagedTacticPlan();
        _tacticHost.AddChild(BuildOptionRow("DIZILIS", [
            ("4-4-2", plan.Formation == Formation.F442, () => _controller.SetTacticFormation(Formation.F442)),
            ("4-3-3", plan.Formation == Formation.F433, () => _controller.SetTacticFormation(Formation.F433)),
            ("3-5-2", plan.Formation == Formation.F352, () => _controller.SetTacticFormation(Formation.F352)),
        ]));
        _tacticHost.AddChild(BuildOptionRow("YAKLASIM", [
            ("Dengeli", plan.Approach == TacticalApproach.Balanced, () => _controller.SetTacticApproach(TacticalApproach.Balanced)),
            ("Hucum", plan.Approach == TacticalApproach.Attacking, () => _controller.SetTacticApproach(TacticalApproach.Attacking)),
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
        }
        else if (_selectedSquadSlotIndex is null || _selectedSquadPlayerIsStarter == player.IsStarter)
        {
            _selectedSquadSlotIndex = player.SlotIndex;
            _selectedSquadPlayerIsStarter = player.IsStarter;
        }
        else
        {
            var starter = player.IsStarter ? player.SlotIndex : _selectedSquadSlotIndex.Value;
            var bench = player.IsStarter ? _selectedSquadSlotIndex.Value : player.SlotIndex;
            SwapSquadPlayers(starter, bench);
            return;
        }

        RefreshPitch();
    }

    private void SwapSquadPlayers(int starterSlotIndex, int benchSlotIndex)
    {
        _selectedSquadSlotIndex = null;
        _selectedSquadPlayerIsStarter = null;
        Apply(_controller.SwapStarterWithBenchForNextDueMatch(starterSlotIndex, benchSlotIndex));
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
