using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
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
    private Control _substitutionBoardHost = null!;
    private Control _liveTacticHost = null!;
    private Label _statusLabel = null!;
    private bool _subMade;
    private string? _substitutionBridgeLine;
    private int? _selectedSquadSlotIndex;
    private bool? _selectedSquadPlayerIsStarter;

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

        content.AddChild(MatchScreenUi.SectionTitle("CANLI TAKTİK", "Formasyon · pres"));
        _liveTacticHost = MatchScreenUi.VerticalStack(8);
        content.AddChild(_liveTacticHost);
        RefreshLiveTactics();

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
        _substitutionBoardHost = MatchScreenUi.VerticalStack(8);
        content.AddChild(_substitutionBoardHost);
        RefreshSubstitutionBoard();

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

    private void RefreshSubstitutionBoard()
    {
        MatchScreenUi.ClearChildren(_substitutionBoardHost);
        var board = _controller.BuildSquadSelectionBoard();
        if (!board.HasMatch)
        {
            _substitutionBoardHost.AddChild(MatchScreenUi.BodyLine(
                "Değişiklik için güncel maç kadrosu bulunamadı.",
                muted: true));
            return;
        }

        _substitutionBoardHost.AddChild(SquadSelectionBoardUi.Build(
            board,
            _selectedSquadSlotIndex,
            SelectSquadPlayer,
            SwapSquadPlayers,
            interactionEnabled: !_subMade));
    }

    private void SelectSquadPlayer(SquadSelectionPlayerDigest player)
    {
        if (_subMade)
        {
            return;
        }

        if (_selectedSquadSlotIndex == player.SlotIndex)
        {
            _selectedSquadSlotIndex = null;
            _selectedSquadPlayerIsStarter = null;
            RefreshSubstitutionBoard();
            return;
        }

        if (_selectedSquadSlotIndex is null
            || _selectedSquadPlayerIsStarter == player.IsStarter)
        {
            _selectedSquadSlotIndex = player.SlotIndex;
            _selectedSquadPlayerIsStarter = player.IsStarter;
            RefreshSubstitutionBoard();
            return;
        }

        var starterSlot = player.IsStarter ? player.SlotIndex : _selectedSquadSlotIndex.Value;
        var benchSlot = player.IsStarter ? _selectedSquadSlotIndex.Value : player.SlotIndex;
        SwapSquadPlayers(starterSlot, benchSlot);
    }

    private void SwapSquadPlayers(int starterSlotIndex, int benchSlotIndex)
    {
        if (_subMade)
        {
            return;
        }

        var result = _controller.SwapStarterWithBenchForNextDueMatch(
            starterSlotIndex,
            benchSlotIndex);
        _subMade = result.Succeeded;
        _selectedSquadSlotIndex = null;
        _selectedSquadPlayerIsStarter = null;
        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.NarrativeBridgeLine))
        {
            _substitutionBridgeLine = result.NarrativeBridgeLine;
        }

        SetStatus(
            result.Succeeded
                ? result.Message + "\nDeğişiklik ikinci yarıya yansır."
                : result.Message,
            result.Succeeded);
        RefreshLineupStrip();
        RefreshSubstitutionBoard();
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
        {
            if (delta == MatchHalfTimeDigest.DecisionAttack)
            {
                _controller.SetTacticApproach(TacticalApproach.Attacking);
            }
            else if (delta == MatchHalfTimeDigest.DecisionDefend)
            {
                _controller.SetTacticApproach(TacticalApproach.Defensive);
            }

            Callable.From(() => SecondHalfRequested?.Invoke(delta, _substitutionBridgeLine))
                .CallDeferred();
        };
        return button;
    }

    private void RefreshLiveTactics()
    {
        MatchScreenUi.ClearChildren(_liveTacticHost);
        var plan = _controller.GetManagedTacticPlan();
        var panel = MatchScreenUi.Card();
        var stack = MatchScreenUi.VerticalStack(8);
        panel.AddChild(stack);
        stack.AddChild(BuildOptionRow(
            "FORMASYON",
            [
                ("4-4-2", plan.Formation == Formation.F442,
                    () => _controller.SetTacticFormation(Formation.F442)),
                ("4-3-3", plan.Formation == Formation.F433,
                    () => _controller.SetTacticFormation(Formation.F433)),
                ("3-5-2", plan.Formation == Formation.F352,
                    () => _controller.SetTacticFormation(Formation.F352)),
            ]));
        stack.AddChild(BuildOptionRow(
            "PRES",
            [
                ("Geri", plan.Pressing == PressingIntensity.LowBlock,
                    () => _controller.SetTacticPressing(PressingIntensity.LowBlock)),
                ("Dengeli", plan.Pressing == PressingIntensity.Balanced,
                    () => _controller.SetTacticPressing(PressingIntensity.Balanced)),
                ("Önde", plan.Pressing == PressingIntensity.HighPress,
                    () => _controller.SetTacticPressing(PressingIntensity.HighPress)),
            ]));
        _liveTacticHost.AddChild(panel);
    }

    private Control BuildOptionRow(
        string title,
        IReadOnlyList<(string Label, bool Selected, Func<UiActionResult> Action)> options)
    {
        var stack = MatchScreenUi.VerticalStack(5);
        var label = new Label { Text = title };
        CareerUiTheme.StyleEyebrow(label);
        stack.AddChild(label);

        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 6);
        foreach (var option in options)
        {
            var button = new Button
            {
                Text = option.Label,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 42),
            };
            if (option.Selected)
            {
                CareerUiTheme.StylePrimaryButton(button);
            }
            else
            {
                CareerUiTheme.StyleSecondaryButton(button);
            }

            button.Pressed += () =>
            {
                var result = option.Action();
                SetStatus(result.Message, result.Succeeded);
                RefreshLiveTactics();
                RefreshLineupStrip();
            };
            row.AddChild(button);
        }

        stack.AddChild(row);
        return stack;
    }

    private void SetStatus(string message, bool succeeded)
    {
        _statusLabel.Text = message;
        _statusLabel.AddThemeColorOverride(
            "font_color",
            succeeded ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft);
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
