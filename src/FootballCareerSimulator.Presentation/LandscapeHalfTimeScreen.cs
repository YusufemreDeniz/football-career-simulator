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
    private Button _formationBtn = null!;
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
        var scoreBlock = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(scoreBlock);
        var stage = new Label { Text = "⏸ DEVRE ARASI  •  SOYUNMA ODASI & TAKTİK MASASI" };
        CareerUiTheme.StyleEyebrow(stage, CareerUiTheme.Accent);
        scoreBlock.AddChild(stage);
        var fixture = new Label { Text = _digest.FixtureLine };
        CareerUiTheme.StyleBody(fixture, muted: true);
        scoreBlock.AddChild(fixture);

        var scorePill = new PanelContainer();
        scorePill.AddThemeStyleboxOverride("panel", CareerUiTheme.ScoreboardPill());
        var score = new Label { Text = _digest.Scoreline, HorizontalAlignment = HorizontalAlignment.Center };
        CareerUiTheme.ApplyDisplayFont(score);
        score.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(28));
        score.AddThemeColorOverride("font_color", Colors.White);
        scorePill.AddChild(score);
        header.AddChild(scorePill);

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

        var pitchHeader = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        pitchHeader.AddThemeConstantOverride("separation", 8);
        var subPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        subPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.16f));
        _substitutionLabel = new Label();
        CareerUiTheme.StyleEyebrow(_substitutionLabel, CareerUiTheme.ActionBright);
        subPanel.AddChild(_substitutionLabel);
        pitchHeader.AddChild(subPanel);

        var autoAlignBtn = SecondaryButton("⚡ Otomatik Diz");
        autoAlignBtn.CustomMinimumSize = new Vector2(140, 28);
        autoAlignBtn.TooltipText = "Sahadaki oyuncuları mevkilerine göre en mantıklı pozisyonlara dizer.";
        autoAlignBtn.Pressed += () =>
        {
            var result = _controller.AutoAlignStartingXiByFormationForNextDueMatch();
            _statusLabel.Text = result.Message;
            _statusLabel.AddThemeColorOverride("font_color", result.Succeeded ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft);
            RefreshPitch();
        };
        pitchHeader.AddChild(autoAlignBtn);

        _formationBtn = SecondaryButton("📋 Diziliş (14 Formasyon)");
        _formationBtn.CustomMinimumSize = new Vector2(140, 28);
        _formationBtn.TooltipText = "Devre arasında takımın saha dizilişini değiştir.";
        _formationBtn.Pressed += () =>
        {
            var currentFormation = _controller.GetManagedTacticPlan().Formation;
            FormationPickerModal.Open(this, currentFormation, selected =>
            {
                var change = _controller.SetTacticFormation(selected);
                _statusLabel.Text = change.Message;
                _statusLabel.AddThemeColorOverride("font_color", change.Succeeded ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft);
                RefreshPitch();
            });
        };
        pitchHeader.AddChild(_formationBtn);

        pitchStack.AddChild(pitchHeader);
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

        var benchCard = MatchScreenUi.Card(emphasized: false);
        deskStack.AddChild(benchCard);
        var benchStack = MatchScreenUi.VerticalStack(6);
        benchCard.AddChild(benchStack);
        var benchTitle = new Label { Text = "💺 YEDEK KULÜBESİ" };
        CareerUiTheme.StyleEyebrow(benchTitle, CareerUiTheme.Data);
        benchStack.AddChild(benchTitle);

        _benchHost = MatchScreenUi.VerticalStack(0);
        benchStack.AddChild(_benchHost);

        var tacticTitle = new Label { Text = "🎯 İKİNCİ YARI PLANI & SOYUNMA ODASI KONUŞMASI" };
        CareerUiTheme.StyleEyebrow(tacticTitle, CareerUiTheme.Data);
        deskStack.AddChild(tacticTitle);
        deskStack.AddChild(DecisionButton("▶ Aynı Planla Devam Et", MatchHalfTimeDigest.DecisionContinue, primary: false));
        deskStack.AddChild(DecisionButton("⚡ Hücuma Geç (Baskıyı Artır)", MatchHalfTimeDigest.DecisionAttack, primary: true));
        deskStack.AddChild(DecisionButton("🛡️ Savunmaya Çek (Skoru Koru)", MatchHalfTimeDigest.DecisionDefend, primary: false));

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
            ? "DEĞİŞİKLİK HAKKI DOLDU (5/5) · Saha içi taktik değişim serbest"
            : $"DEĞİŞİKLİK HAKKI: {_substitutionCount} / {MaxSecondHalfSubstitutions}";
        _benchHost.AddChild(TacticalPitchBoardUi.BuildBench(
            board,
            _selectedSquadSlotIndex,
            SelectSquadPlayer,
            true,
            new Vector2(_layout.PlayerButtonWidth, _layout.PlayerButtonHeight)));
        var currentFormation = _controller.GetManagedTacticPlan().Formation;
        if (_formationBtn is not null)
        {
            _formationBtn.Text = $"📋 {FormationPickerModal.FormatLabel(currentFormation)}";
        }

        _pitchHost.AddChild(TacticalPitchBoardUi.BuildPitch(
            board,
            _selectedSquadSlotIndex,
            SelectSquadPlayer,
            true,
            new Vector2(_layout.PitchMinimumWidth, _layout.PitchMinimumHeight),
            new Vector2(_layout.PlayerButtonWidth, _layout.PlayerButtonHeight),
            currentFormation));
    }

    private void SelectSquadPlayer(SquadSelectionPlayerDigest player)
    {
        if (_selectedSquadSlotIndex == player.SlotIndex)
        {
            _selectedSquadSlotIndex = null;
            _selectedSquadPlayerIsStarter = null;
            _statusLabel.Text = "Seçim kaldırıldı.";
            RefreshPitch();
            return;
        }

        if (_selectedSquadSlotIndex is null)
        {
            _selectedSquadSlotIndex = player.SlotIndex;
            _selectedSquadPlayerIsStarter = player.IsStarter;
            var groupName = player.IsStarter ? "Sahadaki" : "Yedekteki";
            _statusLabel.Text = $"{groupName} {player.DisplayName} ({player.PositionCode}) seçildi. Yer değiştirmek için ikinci bir oyuncuya dokun.";
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
            var result = _controller.SwapTwoStartersForNextDueMatch(firstSlot, secondSlot);
            _statusLabel.Text = result.Message;
            _statusLabel.AddThemeColorOverride("font_color", result.Succeeded ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft);
            RefreshPitch();
        }
        else if (!firstIsStarter && !secondIsStarter)
        {
            var result = _controller.SwapTwoBenchForNextDueMatch(firstSlot, secondSlot);
            _statusLabel.Text = result.Message;
            _statusLabel.AddThemeColorOverride("font_color", result.Succeeded ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft);
            RefreshPitch();
        }
        else
        {
            var starter = firstIsStarter ? firstSlot : secondSlot;
            var bench = firstIsStarter ? secondSlot : firstSlot;
            SwapSquadPlayers(starter, bench);
        }
    }

    private void SwapSquadPlayers(int starterSlotIndex, int benchSlotIndex)
    {
        if (_substitutionCount >= MaxSecondHalfSubstitutions)
        {
            _statusLabel.Text = "Maksimum 5 oyuncu değişikliği hakkı kullanıldı.";
            _statusLabel.AddThemeColorOverride("font_color", CareerUiTheme.DangerSoft);
            return;
        }

        var result = _controller.SwapStarterWithBenchForNextDueMatch(starterSlotIndex, benchSlotIndex);
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
