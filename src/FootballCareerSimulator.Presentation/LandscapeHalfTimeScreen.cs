using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Landscape half-time desk. Supports up to five deliberate substitutions.
/// </summary>
public partial class LandscapeHalfTimeScreen : Control
{
    private const int MaxSecondHalfSubstitutions = 5;
    private readonly CareerSessionController _controller;
    private readonly MatchHalfTimeDigest _digest;
    private Control _pitchHost = null!;
    private VBoxContainer _benchHost = null!;
    private Label _substitutionLabel = null!;
    private Label _statusLabel = null!;
    private int _substitutionCount;
    private int? _selectedSquadSlotIndex;
    private bool? _selectedSquadPlayerIsStarter;
    private readonly List<string> _substitutionBridgeLines = [];
    private LandscapeMatchLayoutProfile _layout = null!;

    public event Action? BackRequested;
    public event Action<int, string?>? SecondHalfRequested;

    public LandscapeHalfTimeScreen(CareerSessionController controller, MatchHalfTimeDigest digest)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _digest = digest ?? throw new ArgumentNullException(nameof(digest));
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
            new Color(CareerUiTheme.Accent.R, CareerUiTheme.Accent.G, CareerUiTheme.Accent.B, 0.08f),
            _layout.HorizontalMargin,
            _layout.VerticalMargin);
        var shell = MatchScreenUi.VerticalStack(_layout.SectionSeparation);
        shell.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(shell);

        var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddThemeConstantOverride("separation", 10);
        shell.AddChild(header);
        var back = SecondaryButton("Geri");
        back.CustomMinimumSize = new Vector2(74, _layout.ActionButtonHeight);
        back.Pressed += () => Callable.From(() => BackRequested?.Invoke()).CallDeferred();
        header.AddChild(back);
        var scoreBlock = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(scoreBlock);
        var stage = new Label { Text = "DEVRE ARASI  /  MAC KOMUTA MERKEZI" };
        CareerUiTheme.StyleEyebrow(stage, CareerUiTheme.Accent);
        scoreBlock.AddChild(stage);
        var fixture = new Label { Text = _digest.FixtureLine };
        CareerUiTheme.StyleBody(fixture, muted: true);
        scoreBlock.AddChild(fixture);
        var score = new Label { Text = _digest.Scoreline, HorizontalAlignment = HorizontalAlignment.Right };
        CareerUiTheme.StyleBrand(score);
        score.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(34));
        header.AddChild(score);

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
        var pitchStack = MatchScreenUi.VerticalStack(7);
        pitchPanel.AddChild(pitchStack);
        _substitutionLabel = new Label();
        CareerUiTheme.StyleEyebrow(_substitutionLabel, CareerUiTheme.ActionBright);
        pitchStack.AddChild(_substitutionLabel);
        _pitchHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        pitchStack.AddChild(_pitchHost);

        var desk = MatchScreenUi.Card();
        desk.CustomMinimumSize = new Vector2(_layout.CommandPanelWidth, 0);
        desk.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddChild(desk);
        var deskScroll = MatchScreenUi.ScrollArea();
        desk.AddChild(deskScroll);
        var deskStack = MatchScreenUi.VerticalStack(10);
        deskScroll.AddChild(deskStack);
        var headline = MatchScreenUi.BodyLine(_digest.Headline);
        headline.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        deskStack.AddChild(headline);
        deskStack.AddChild(MatchScreenUi.BodyLine(_digest.AdviceLine, muted: true));
        foreach (var beat in _digest.BeatLines.Take(2))
        {
            deskStack.AddChild(MatchScreenUi.BeatLine(beat, muted: true));
        }

        _benchHost = MatchScreenUi.VerticalStack(0);
        deskStack.AddChild(_benchHost);

        var tacticTitle = new Label { Text = "IKINCI YARI PLANI" };
        CareerUiTheme.StyleEyebrow(tacticTitle, CareerUiTheme.Data);
        deskStack.AddChild(tacticTitle);
        deskStack.AddChild(DecisionButton("Ayni plan", MatchHalfTimeDigest.DecisionContinue, primary: false));
        deskStack.AddChild(DecisionButton("Hucuma gec", MatchHalfTimeDigest.DecisionAttack, primary: true));
        deskStack.AddChild(DecisionButton("Savunmaya cek", MatchHalfTimeDigest.DecisionDefend, primary: false));

        _statusLabel = MatchScreenUi.BodyLine(string.Empty, muted: true);
        var status = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        status.AddThemeStyleboxOverride("panel", CareerUiTheme.StatusPanel());
        status.AddChild(_statusLabel);
        deskStack.AddChild(status);

        RefreshPitch();
        MatchScreenUi.FadeIn(body, this);
    }

    private void RefreshPitch()
    {
        MatchScreenUi.ClearChildren(_pitchHost);
        MatchScreenUi.ClearChildren(_benchHost);
        var board = _controller.BuildSquadSelectionBoard();
        if (!board.HasMatch)
        {
            _pitchHost.AddChild(MatchScreenUi.BodyLine("Canli mac kadrosu bulunamadi.", muted: true));
            return;
        }

        _substitutionLabel.Text = _substitutionCount >= MaxSecondHalfSubstitutions
            ? "DEGISIKLIK HAKKI KALMADI  /  5-5"
            : $"DEGISIKLIK HAKKI  /  {_substitutionCount}-{MaxSecondHalfSubstitutions}";
        _benchHost.AddChild(TacticalPitchBoardUi.BuildBench(
            board,
            _selectedSquadSlotIndex,
            SelectSquadPlayer,
            _substitutionCount < MaxSecondHalfSubstitutions,
            new Vector2(_layout.PlayerButtonWidth, _layout.PlayerButtonHeight)));
        _pitchHost.AddChild(TacticalPitchBoardUi.BuildPitch(
            board,
            _selectedSquadSlotIndex,
            SelectSquadPlayer,
            _substitutionCount < MaxSecondHalfSubstitutions,
            new Vector2(_layout.PitchMinimumWidth, _layout.PitchMinimumHeight),
            new Vector2(_layout.PlayerButtonWidth, _layout.PlayerButtonHeight)));
    }

    private void SelectSquadPlayer(SquadSelectionPlayerDigest player)
    {
        if (_substitutionCount >= MaxSecondHalfSubstitutions)
        {
            return;
        }

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
        if (_substitutionCount >= MaxSecondHalfSubstitutions)
        {
            return;
        }

        var result = _controller.SwapStarterWithBenchForNextDueMatch(starterSlotIndex, benchSlotIndex);
        _selectedSquadSlotIndex = null;
        _selectedSquadPlayerIsStarter = null;
        if (result.Succeeded)
        {
            _substitutionCount++;
            if (!string.IsNullOrWhiteSpace(result.NarrativeBridgeLine))
            {
                _substitutionBridgeLines.Add(result.NarrativeBridgeLine);
            }
        }

        _statusLabel.Text = result.Succeeded
            ? result.Message + $"  /  {_substitutionCount}-{MaxSecondHalfSubstitutions}"
            : result.Message;
        _statusLabel.AddThemeColorOverride("font_color", result.Succeeded ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft);
        RefreshPitch();
    }

    private Button DecisionButton(string text, int delta, bool primary)
    {
        var button = primary ? PrimaryButton(text) : SecondaryButton(text);
        button.Pressed += () =>
        {
            if (delta == MatchHalfTimeDigest.DecisionAttack)
            {
                _controller.SetTacticApproach(TacticalApproach.Attacking);
            }
            else if (delta == MatchHalfTimeDigest.DecisionDefend)
            {
                _controller.SetTacticApproach(TacticalApproach.Defensive);
            }

            Callable.From(() => SecondHalfRequested?.Invoke(
                delta,
                _substitutionBridgeLines.Count == 0 ? null : string.Join(" · ", _substitutionBridgeLines))).CallDeferred();
        };
        return button;
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
