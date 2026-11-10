using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

internal static class TacticalPitchBoardUi
{
    public static Control BuildReadOnly(
        IReadOnlyList<SquadSelectionPlayerDigest> startingXi,
        Action<SquadSelectionPlayerDigest>? onPlayerSelected = null,
        Formation formation = Formation.F442)
    {
        var board = new SquadSelectionBoardDigest(
            HasMatch: true,
            IsApproved: true,
            StartingXi: startingXi,
            Bench: Array.Empty<SquadSelectionPlayerDigest>());
        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        root.AddChild(new TacticalPitchBoard(
            board.StartingXi,
            selectedSlotIndex: null,
            onPlayerSelected ?? (_ => { }),
            interactionEnabled: onPlayerSelected is not null,
            playerButtonMinimumSize: new Vector2(78, 48),
            formation: formation)
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(340, 210),
        });
        return root;
    }

    public static Control Build(
        SquadSelectionBoardDigest board,
        int? selectedSlotIndex,
        Action<SquadSelectionPlayerDigest> selectPlayer,
        Action<int, int> swapPlayers,
        bool interactionEnabled = true,
        Formation formation = Formation.F442)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(selectPlayer);
        ArgumentNullException.ThrowIfNull(swapPlayers);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        root.AddThemeConstantOverride("separation", 5);

        var pitchBoard = new TacticalPitchBoard(
            board.StartingXi,
            selectedSlotIndex,
            selectPlayer,
            interactionEnabled,
            new Vector2(72, 46),
            formation)
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(360, 226),
        };

        // FM26 / SM27 tarzı Canlı Faz Görselleştirme Anahtarı (Topla Hücum Fazı ↔ Standart ↔ Topsuz Savunma Blok)
        var phaseToggleBar = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        phaseToggleBar.AddThemeConstantOverride("separation", 4);
        root.AddChild(phaseToggleBar);

        var phaseStandardBtn = new Button
        {
            Text = "⚖️ Standart Diziliş",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 26),
        };
        var phaseInPossessionBtn = new Button
        {
            Text = "⚽ Topla (Hücum Fazı)",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 26),
        };
        var phaseOutPossessionBtn = new Button
        {
            Text = "🛡️ Topsuz (Kilit Blok)",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 26),
        };

        void UpdatePhaseButtonStyles(int mode)
        {
            CareerUiTheme.StyleTabButton(phaseStandardBtn, mode == 0);
            CareerUiTheme.StyleTabButton(phaseInPossessionBtn, mode == 1);
            CareerUiTheme.StyleTabButton(phaseOutPossessionBtn, mode == 2);
        }

        UpdatePhaseButtonStyles(0);

        phaseStandardBtn.Pressed += () =>
        {
            UpdatePhaseButtonStyles(0);
            pitchBoard.AnimateToPhaseMode(0);
        };
        phaseInPossessionBtn.Pressed += () =>
        {
            UpdatePhaseButtonStyles(1);
            pitchBoard.AnimateToPhaseMode(1);
        };
        phaseOutPossessionBtn.Pressed += () =>
        {
            UpdatePhaseButtonStyles(2);
            pitchBoard.AnimateToPhaseMode(2);
        };

        phaseToggleBar.AddChild(phaseStandardBtn);
        phaseToggleBar.AddChild(phaseInPossessionBtn);
        phaseToggleBar.AddChild(phaseOutPossessionBtn);

        root.AddChild(pitchBoard);

        root.AddChild(BuildBench(
            board,
            selectedSlotIndex,
            selectPlayer,
            interactionEnabled,
            new Vector2(72, 46)));

        return root;
    }

    public static Control BuildPitch(
        SquadSelectionBoardDigest board,
        int? selectedSlotIndex,
        Action<SquadSelectionPlayerDigest> selectPlayer,
        bool interactionEnabled,
        Vector2 minimumSize,
        Vector2 playerButtonMinimumSize,
        Formation formation = Formation.F442)
    {
        return new TacticalPitchBoard(
            board.StartingXi,
            selectedSlotIndex,
            selectPlayer,
            interactionEnabled,
            playerButtonMinimumSize,
            formation)
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = minimumSize,
        };
    }

    public static Control BuildBench(
        SquadSelectionBoardDigest board,
        int? selectedSlotIndex,
        Action<SquadSelectionPlayerDigest> selectPlayer,
        bool interactionEnabled,
        Vector2 playerButtonMinimumSize)
    {
        var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 6);

        var benchHeader = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        benchHeader.AddThemeConstantOverride("separation", 6);
        root.AddChild(benchHeader);

        var benchTitle = new Label { Text = "YEDEK KULÜBESİ" };
        CareerUiTheme.StyleEyebrow(benchTitle, CareerUiTheme.Data);
        benchHeader.AddChild(benchTitle);

        var countBadge = new Label
        {
            Text = $"({board.Bench.Count} OYUNCU)",
        };
        CareerUiTheme.StyleTableText(countBadge, muted: true);
        countBadge.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        benchHeader.AddChild(countBadge);

        var scroll = new MobileScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(0, 68),
        };
        root.AddChild(scroll);
        var bench = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        bench.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(bench);
        foreach (var player in board.Bench)
        {
            var button = PlayerButton(
                player,
                selectedSlotIndex == player.SlotIndex,
                interactionEnabled,
                playerButtonMinimumSize);
            button.Pressed += () => selectPlayer(player);
            bench.AddChild(button);
        }

        return root;
    }

    internal static Button PlayerButton(
        SquadSelectionPlayerDigest player,
        bool selected,
        bool interactionEnabled,
        Vector2? minimumSize = null)
    {
        var cardSize = minimumSize ?? new Vector2(88, 56);
        var button = new Button
        {
            TooltipText = $"{player.DisplayName} · {player.PositionName} · Güç {player.Rating} · Kondisyon %{player.Fitness} · Yorgunluk +{player.Fatigue}"
                + (player.IsAvailable ? string.Empty : " · [SAKAT]"),
            Disabled = !interactionEnabled || (!player.IsAvailable && !player.IsStarter),
            CustomMinimumSize = cardSize,
            ClipContents = true,
        };
        CareerUiTheme.StylePitchChip(button, selected);

        var margin = new MarginContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 3);
        margin.AddThemeConstantOverride("margin_top", 3);
        margin.AddThemeConstantOverride("margin_right", 3);
        margin.AddThemeConstantOverride("margin_bottom", 3);
        button.AddChild(margin);

        var cardBox = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        cardBox.AddThemeConstantOverride("separation", 2);
        margin.AddChild(cardBox);

        // Row 1: [Position Badge] [Condition/Status Icon] [Rating Badge]
        var topRow = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        topRow.AddThemeConstantOverride("separation", 2);
        cardBox.AddChild(topRow);

        // Position Badge with Role Color
        var posPanel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
        };
        posPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.PositionBadgePanel(player.PositionCode));
        var posLabel = new Label
        {
            Text = player.PositionCode,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.ApplyDisplayFont(posLabel);
        posLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        posLabel.AddThemeColorOverride("font_color", Colors.White);
        posPanel.AddChild(posLabel);
        topRow.AddChild(posPanel);

        // Middle Status / Fitness symbol
        var statusLabel = new Label
        {
            Text = !player.IsAvailable
                ? "🩹"
                : player.Fitness < 75
                    ? "⚠️"
                    : player.Fitness >= 90
                        ? "⚡"
                        : "•",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        statusLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(9));
        statusLabel.AddThemeColorOverride("font_color", CareerUiTheme.FitnessColor(player.Fitness));
        topRow.AddChild(statusLabel);

        // Rating Badge with Tier Color
        var ratingPanel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
        };
        ratingPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.RatingBadgePanel(player.Rating));
        var ratingLabel = new Label
        {
            Text = player.Rating.ToString(),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.ApplyDisplayFont(ratingLabel);
        ratingLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        ratingLabel.AddThemeColorOverride("font_color", CareerUiTheme.RatingColor(player.Rating));
        ratingPanel.AddChild(ratingLabel);
        topRow.AddChild(ratingPanel);

        // Row 2: Condition progress bar & percent
        var conditionRow = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        conditionRow.AddThemeConstantOverride("separation", 2);
        cardBox.AddChild(conditionRow);

        var fitnessBar = new ProgressBar
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            MinValue = 0,
            MaxValue = 100,
            Value = Math.Clamp(player.Fitness, 0, 100),
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(34, 3),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        var fitnessFill = new StyleBoxFlat
        {
            BgColor = CareerUiTheme.FitnessColor(player.Fitness),
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
        };
        var fitnessBg = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0.45f),
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
        };
        fitnessBar.AddThemeStyleboxOverride("fill", fitnessFill);
        fitnessBar.AddThemeStyleboxOverride("background", fitnessBg);
        conditionRow.AddChild(fitnessBar);

        var fitnessText = new Label
        {
            Text = $"%{player.Fitness}",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        CareerUiTheme.ApplyBodyFont(fitnessText);
        fitnessText.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(9));
        fitnessText.AddThemeColorOverride("font_color", CareerUiTheme.FitnessColor(player.Fitness));
        conditionRow.AddChild(fitnessText);

        // Row 3: Player Name Strip
        var nameLabel = new Label
        {
            Text = ShortName(player.DisplayName),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            ClipText = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.ApplyBodyFont(nameLabel);
        nameLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        nameLabel.AddThemeColorOverride("font_color", selected ? CareerUiTheme.ActionBright : CareerUiTheme.Ink);
        cardBox.AddChild(nameLabel);

        return button;
    }

    internal static string ShortName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[^1] : name;
    }
}

internal sealed partial class TacticalPitchBoard : Control
{
    private static readonly Vector2[] Positions442 =
    [
        new(0.50f, 0.89f), // GK
        new(0.16f, 0.72f), new(0.38f, 0.75f), new(0.62f, 0.75f), new(0.84f, 0.72f), // LB, CB, CB, RB
        new(0.16f, 0.48f), new(0.38f, 0.52f), new(0.62f, 0.52f), new(0.84f, 0.48f), // LM, CM, CM, RM
        new(0.35f, 0.24f), new(0.65f, 0.24f), // ST, ST
    ];

    private static readonly Vector2[] Positions433 =
    [
        new(0.50f, 0.89f), // GK
        new(0.16f, 0.73f), new(0.38f, 0.76f), new(0.62f, 0.76f), new(0.84f, 0.73f), // LB, CB, CB, RB
        new(0.28f, 0.50f), new(0.50f, 0.58f), new(0.72f, 0.50f), // LCM, DM, RCM
        new(0.18f, 0.26f), new(0.50f, 0.20f), new(0.82f, 0.26f), // LW, ST, RW
    ];

    private static readonly Vector2[] Positions352 =
    [
        new(0.50f, 0.89f), // GK
        new(0.26f, 0.75f), new(0.50f, 0.78f), new(0.74f, 0.75f), // LCB, CB, RCB
        new(0.12f, 0.50f), new(0.32f, 0.54f), new(0.50f, 0.48f), new(0.68f, 0.54f), new(0.88f, 0.50f), // LWB, LCM, DM, RCM, RWB
        new(0.36f, 0.24f), new(0.64f, 0.24f), // ST, ST
    ];

    private static readonly Vector2[] Positions4231 =
    [
        new(0.50f, 0.89f), // GK
        new(0.16f, 0.73f), new(0.38f, 0.76f), new(0.62f, 0.76f), new(0.84f, 0.73f), // LB, CB, CB, RB
        new(0.35f, 0.58f), new(0.65f, 0.58f), // LDM, RDM
        new(0.18f, 0.38f), new(0.50f, 0.35f), new(0.82f, 0.38f), // LAM, CAM, RAM
        new(0.50f, 0.20f), // ST
    ];

    private static readonly Vector2[] Positions4312 =
    [
        new(0.50f, 0.89f), // GK
        new(0.16f, 0.73f), new(0.38f, 0.76f), new(0.62f, 0.76f), new(0.84f, 0.73f), // LB, CB, CB, RB
        new(0.28f, 0.52f), new(0.50f, 0.60f), new(0.72f, 0.52f), // LCM, DM, RCM
        new(0.50f, 0.36f), // CAM
        new(0.35f, 0.22f), new(0.65f, 0.22f), // ST, ST
    ];

    private static readonly Vector2[] Positions4222 =
    [
        new(0.50f, 0.89f), // GK
        new(0.16f, 0.73f), new(0.38f, 0.76f), new(0.62f, 0.76f), new(0.84f, 0.73f), // LB, CB, CB, RB
        new(0.35f, 0.58f), new(0.65f, 0.58f), // LDM, RDM
        new(0.26f, 0.38f), new(0.74f, 0.38f), // LCAM, RCAM
        new(0.35f, 0.22f), new(0.65f, 0.22f), // ST, ST
    ];

    private static readonly Vector2[] Positions343 =
    [
        new(0.50f, 0.89f), // GK
        new(0.26f, 0.75f), new(0.50f, 0.78f), new(0.74f, 0.75f), // LCB, CB, RCB
        new(0.14f, 0.50f), new(0.38f, 0.53f), new(0.62f, 0.53f), new(0.86f, 0.50f), // LM, LCM, RCM, RM
        new(0.20f, 0.26f), new(0.50f, 0.20f), new(0.80f, 0.26f), // LW, ST, RW
    ];

    private static readonly Vector2[] Positions3241 =
    [
        new(0.50f, 0.89f), // GK
        new(0.26f, 0.76f), new(0.50f, 0.79f), new(0.74f, 0.76f), // LCB, CB, RCB
        new(0.37f, 0.60f), new(0.63f, 0.60f), // LDM, RDM
        new(0.14f, 0.38f), new(0.38f, 0.36f), new(0.62f, 0.36f), new(0.86f, 0.38f), // LM, LCAM, RCAM, RM
        new(0.50f, 0.20f), // ST
    ];

    private static readonly Vector2[] Positions3331 =
    [
        new(0.50f, 0.89f), // GK
        new(0.26f, 0.76f), new(0.50f, 0.79f), new(0.74f, 0.76f), // LCB, CB, RCB
        new(0.25f, 0.58f), new(0.50f, 0.62f), new(0.75f, 0.58f), // LDM, DM, RDM
        new(0.22f, 0.38f), new(0.50f, 0.35f), new(0.78f, 0.38f), // LAM, CAM, RAM
        new(0.50f, 0.20f), // ST
    ];

    private static readonly Vector2[] Positions4141 =
    [
        new(0.50f, 0.89f), // GK
        new(0.16f, 0.73f), new(0.38f, 0.76f), new(0.62f, 0.76f), new(0.84f, 0.73f), // LB, CB, CB, RB
        new(0.50f, 0.60f), // DM
        new(0.16f, 0.44f), new(0.38f, 0.46f), new(0.62f, 0.46f), new(0.84f, 0.44f), // LM, LCM, RCM, RM
        new(0.50f, 0.20f), // ST
    ];

    private static readonly Vector2[] Positions532 =
    [
        new(0.50f, 0.89f), // GK
        new(0.12f, 0.66f), new(0.30f, 0.75f), new(0.50f, 0.77f), new(0.70f, 0.75f), new(0.88f, 0.66f), // LWB, LCB, CB, RCB, RWB
        new(0.30f, 0.50f), new(0.50f, 0.56f), new(0.70f, 0.50f), // LCM, DM, RCM
        new(0.36f, 0.24f), new(0.64f, 0.24f), // ST, ST
    ];

    private static readonly Vector2[] Positions523 =
    [
        new(0.50f, 0.89f), // GK
        new(0.12f, 0.66f), new(0.30f, 0.75f), new(0.50f, 0.77f), new(0.70f, 0.75f), new(0.88f, 0.66f), // LWB, LCB, CB, RCB, RWB
        new(0.38f, 0.52f), new(0.62f, 0.52f), // LCM, RCM
        new(0.20f, 0.26f), new(0.50f, 0.20f), new(0.80f, 0.26f), // LW, ST, RW
    ];

    private static readonly Vector2[] Positions424 =
    [
        new(0.50f, 0.89f), // GK
        new(0.16f, 0.73f), new(0.38f, 0.76f), new(0.62f, 0.76f), new(0.84f, 0.73f), // LB, CB, CB, RB
        new(0.38f, 0.52f), new(0.62f, 0.52f), // LCM, RCM
        new(0.16f, 0.24f), new(0.38f, 0.20f), new(0.62f, 0.20f), new(0.84f, 0.24f), // LW, LST, RST, RW
    ];

    private static readonly Vector2[] Positions451 =
    [
        new(0.50f, 0.89f), // GK
        new(0.16f, 0.73f), new(0.38f, 0.76f), new(0.62f, 0.76f), new(0.84f, 0.73f), // LB, CB, CB, RB
        new(0.16f, 0.46f), new(0.50f, 0.57f), new(0.34f, 0.48f), new(0.66f, 0.48f), new(0.84f, 0.46f), // LM, DM, LCM, RCM, RM
        new(0.50f, 0.20f), // ST
    ];

    public static Vector2[] GetPositionsForFormation(Formation formation) =>
        formation switch
        {
            Formation.F442 => Positions442,
            Formation.F433 => Positions433,
            Formation.F352 => Positions352,
            Formation.F4231 => Positions4231,
            Formation.F4312 => Positions4312,
            Formation.F4222 => Positions4222,
            Formation.F343 => Positions343,
            Formation.F3241 => Positions3241,
            Formation.F3331 => Positions3331,
            Formation.F4141 => Positions4141,
            Formation.F532 => Positions532,
            Formation.F523 => Positions523,
            Formation.F424 => Positions424,
            Formation.F451 => Positions451,
            _ => Positions442,
        };

    private readonly IReadOnlyList<SquadSelectionPlayerDigest> _players;
    private readonly int? _selectedSlotIndex;
    private readonly Action<SquadSelectionPlayerDigest> _selectPlayer;
    private readonly bool _interactionEnabled;
    private readonly Vector2 _playerButtonMinimumSize;
    private readonly Formation _formation;
    private readonly List<(Button Button, Vector2 BasePosition)> _buttons = [];
    private int _phaseMode; // 0: Standard, 1: In-Possession (Attacking), 2: Out-of-Possession (Compact Block)

    public TacticalPitchBoard(
        IReadOnlyList<SquadSelectionPlayerDigest> players,
        int? selectedSlotIndex,
        Action<SquadSelectionPlayerDigest> selectPlayer,
        bool interactionEnabled,
        Vector2? playerButtonMinimumSize = null,
        Formation formation = Formation.F442)
    {
        _players = players;
        _selectedSlotIndex = selectedSlotIndex;
        _selectPlayer = selectPlayer;
        _interactionEnabled = interactionEnabled;
        _playerButtonMinimumSize = playerButtonMinimumSize ?? new Vector2(88, 56);
        _formation = formation;
        ClipContents = true;
    }

    public void AnimateToPhaseMode(int mode)
    {
        _phaseMode = Math.Clamp(mode, 0, 2);
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);

        for (var i = 0; i < _buttons.Count; i++)
        {
            var (button, basePos) = _buttons[i];
            var shifted = ComputePhasePosition(basePos, i, _phaseMode);
            var target = new Vector2(shifted.X * Size.X, shifted.Y * Size.Y) - (button.Size * 0.5f);
            tween.TweenProperty(button, "position", target, 0.28d);
        }

        QueueRedraw();
    }

    private static Vector2 ComputePhasePosition(Vector2 basePos, int index, int phaseMode)
    {
        if (index == 0 || phaseMode == 0)
        {
            return basePos;
        }

        if (phaseMode == 1)
        {
            // Topla Hücum Fazı (IP): Takım boyu öne çıkar, bekler ve kanatlar çizgiye/ileriye bindirir
            var yShift = basePos.Y > 0.65f ? -0.11f : (basePos.Y > 0.40f ? -0.13f : -0.06f);
            var xSpread = basePos.X < 0.30f ? -0.04f : (basePos.X > 0.70f ? 0.04f : 0f);
            return new Vector2(
                Mathf.Clamp(basePos.X + xSpread, 0.10f, 0.90f),
                Mathf.Clamp(basePos.Y + yShift, 0.14f, 0.86f));
        }

        // Topsuz Kilit Blok (OOP): Takım kendi yarı sahasında kompaktlaşır, hatlar birbirine yaklaşır
        var compactX = Mathf.Lerp(basePos.X, 0.50f, 0.22f);
        var compactY = Mathf.Clamp(basePos.Y + 0.12f, 0.32f, 0.84f);
        return new Vector2(compactX, compactY);
    }

    public override void _Ready()
    {
        var positions = GetPositionsForFormation(_formation);
        foreach (var (player, index) in _players.Take(positions.Length).Select((player, index) => (player, index)))
        {
            var button = TacticalPitchBoardUi.PlayerButton(
                player,
                _selectedSlotIndex == player.SlotIndex,
                _interactionEnabled,
                _playerButtonMinimumSize);
            button.Pressed += () => _selectPlayer(player);
            AddChild(button);
            _buttons.Add((button, positions[index]));
        }

        Resized += LayoutPlayers;
        LayoutPlayers();
        QueueRedraw();
    }

    public override void _Draw()
    {
        var field = new Rect2(Vector2.Zero, Size);
        // Zengin stadyum çimi arka planı
        DrawRect(field, new Color(0.045f, 0.16f, 0.10f));

        // Dönüşümlü çim biçim şeritleri
        var stripeCount = 10;
        var stripeHeight = Size.Y / stripeCount;
        for (var stripe = 0; stripe < stripeCount; stripe += 2)
        {
            DrawRect(new Rect2(0, stripe * stripeHeight, Size.X, stripeHeight), new Color(0.06f, 0.20f, 0.12f));
        }

        // Faz Odak Bölgesi Görselleştirmesi (SM27 / FM26 Taktiksel Isı Bölgeleri)
        if (_phaseMode == 1)
        {
            // Hücum 3. Bölge & Kanat Koridorları Isı Alanı
            DrawRect(new Rect2(0, 0, Size.X, Size.Y * 0.42f), new Color(0.98f, 0.68f, 0.16f, 0.13f), filled: true);
            DrawLine(new Vector2(0, Size.Y * 0.42f), new Vector2(Size.X, Size.Y * 0.42f), new Color(0.98f, 0.75f, 0.22f, 0.72f), 2f);
        }
        else if (_phaseMode == 2)
        {
            // Kompakt Savunma Bloğu Kalkan Alanı
            DrawRect(new Rect2(Size.X * 0.12f, Size.Y * 0.46f, Size.X * 0.76f, Size.Y * 0.40f), new Color(0.22f, 0.68f, 0.98f, 0.15f), filled: true);
            DrawRect(new Rect2(Size.X * 0.12f, Size.Y * 0.46f, Size.X * 0.76f, Size.Y * 0.40f), new Color(0.22f, 0.75f, 0.98f, 0.65f), filled: false, width: 2f);
        }

        // Taktiksel Pas & Uyum Bağlantı Çizgileri
        var linkColor = _phaseMode == 1
            ? new Color(0.98f, 0.78f, 0.24f, 0.32f)
            : _phaseMode == 2
                ? new Color(0.28f, 0.78f, 0.98f, 0.32f)
                : new Color(0.18f, 0.90f, 0.58f, 0.24f);

        for (var i = 1; i < _buttons.Count; i++)
        {
            var p1 = ComputePhasePosition(_buttons[i].BasePosition, i, _phaseMode);
            for (var j = i + 1; j < _buttons.Count; j++)
            {
                var p2 = ComputePhasePosition(_buttons[j].BasePosition, j, _phaseMode);
                if (p1.DistanceTo(p2) <= 0.34f)
                {
                    DrawLine(
                        new Vector2(p1.X * Size.X, p1.Y * Size.Y),
                        new Vector2(p2.X * Size.X, p2.Y * Size.Y),
                        linkColor,
                        1.8f);
                }
            }
        }

        // Net, taktiksel beyaz-nane saha çizgileri
        var line = new Color(0.85f, 0.95f, 0.90f, 0.65f);
        var inset = Mathf.Max(12, Size.X * 0.025f);
        var inner = new Rect2(inset, inset, Size.X - (inset * 2), Size.Y - (inset * 2));
        DrawRect(inner, line, false, 2);

        // Orta saha çizgisi
        var midY = Size.Y * 0.5f;
        DrawLine(new Vector2(inset, midY), new Vector2(Size.X - inset, midY), line, 2);

        // Orta yuvarlak ve orta nokta
        var centerRadius = Mathf.Min(Size.X, Size.Y) * 0.13f;
        var center = new Vector2(Size.X * 0.5f, midY);
        DrawArc(center, centerRadius, 0, Mathf.Tau, 48, line, 2);
        DrawCircle(center, 3f, line);

        // Ceza sahaları
        var boxWidth = Size.X * 0.44f;
        var boxHeight = Size.Y * 0.16f;
        var topBoxRect = new Rect2((Size.X - boxWidth) * 0.5f, inset, boxWidth, boxHeight);
        var bottomBoxRect = new Rect2((Size.X - boxWidth) * 0.5f, Size.Y - inset - boxHeight, boxWidth, boxHeight);
        DrawRect(topBoxRect, line, false, 2);
        DrawRect(bottomBoxRect, line, false, 2);

        // Kale sahaları (6 pas)
        var goalWidth = Size.X * 0.22f;
        var goalHeight = Size.Y * 0.06f;
        DrawRect(new Rect2((Size.X - goalWidth) * 0.5f, inset, goalWidth, goalHeight), line, false, 1.5f);
        DrawRect(new Rect2((Size.X - goalWidth) * 0.5f, Size.Y - inset - goalHeight, goalWidth, goalHeight), line, false, 1.5f);

        // Penaltı noktaları
        var topPenaltySpot = new Vector2(Size.X * 0.5f, inset + (boxHeight * 0.68f));
        var bottomPenaltySpot = new Vector2(Size.X * 0.5f, Size.Y - inset - (boxHeight * 0.68f));
        DrawCircle(topPenaltySpot, 2.5f, line);
        DrawCircle(bottomPenaltySpot, 2.5f, line);

        // Ceza yayı (D-box)
        DrawArc(topPenaltySpot, centerRadius * 0.6f, 0.25f * Mathf.Pi, 0.75f * Mathf.Pi, 24, line, 1.5f);
        DrawArc(bottomPenaltySpot, centerRadius * 0.6f, 1.25f * Mathf.Pi, 1.75f * Mathf.Pi, 24, line, 1.5f);

        // Korner yayları
        var cornerRadius = 8f;
        DrawArc(new Vector2(inset, inset), cornerRadius, 0, 0.5f * Mathf.Pi, 12, line, 1.5f);
        DrawArc(new Vector2(Size.X - inset, inset), cornerRadius, 0.5f * Mathf.Pi, Mathf.Pi, 12, line, 1.5f);
        DrawArc(new Vector2(inset, Size.Y - inset), cornerRadius, 1.5f * Mathf.Pi, Mathf.Tau, 12, line, 1.5f);
        DrawArc(new Vector2(Size.X - inset, Size.Y - inset), cornerRadius, Mathf.Pi, 1.5f * Mathf.Pi, 12, line, 1.5f);
    }

    private void LayoutPlayers()
    {
        for (var i = 0; i < _buttons.Count; i++)
        {
            var (button, basePos) = _buttons[i];
            var shifted = ComputePhasePosition(basePos, i, _phaseMode);
            button.Size = _playerButtonMinimumSize;
            var target = new Vector2(shifted.X * Size.X, shifted.Y * Size.Y);
            button.Position = target - (button.Size * 0.5f);
        }
    }
}
