using System;
using System.Collections.Generic;
using System.Linq;
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
            playerButtonMinimumSize: new Vector2(68, 56),
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
            new Vector2(68, 56),
            formation)
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(360, 226),
        };

        // Canlı Faz Görselleştirme Anahtarı (Topla Hücum Fazı ↔ Standart ↔ Topsuz Savunma Blok)
        var phaseToggleBar = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        phaseToggleBar.AddThemeConstantOverride("separation", 4);
        root.AddChild(phaseToggleBar);

        var phaseStandardBtn = new Button
        {
            Text = "Standart Diziliş",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 26),
        };
        var phaseInPossessionBtn = new Button
        {
            Text = "Topla (Hücum Fazı)",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 26),
        };
        var phaseOutPossessionBtn = new Button
        {
            Text = "Topsuz (Kilit Blok)",
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
            new Vector2(68, 56)));

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
            CustomMinimumSize = new Vector2(0, 66),
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
            button.MouseFilter = Control.MouseFilterEnum.Pass;
            button.Pressed += () =>
            {
                if (MobileScrollContainer.WasRecentDragScroll())
                {
                    return;
                }
                selectPlayer(player);
            };
            bench.AddChild(button);
        }

        return root;
    }

    /// <summary>
    /// Kural 4: Oyuncular düz dikdörtgen/daire yerine; altında gölgesi olan, etrafında dairesel kondisyon
    /// halkası (arc) bulunan ve içinde forma numarası yazan katmanlı jetonlar (PitchKitTokenButton) olarak çizilir.
    /// </summary>
    internal static Button PlayerButton(
        SquadSelectionPlayerDigest player,
        bool selected,
        bool interactionEnabled,
        Vector2? minimumSize = null)
    {
        return new PitchKitTokenButton(player, selected, interactionEnabled, minimumSize ?? new Vector2(68, 56));
    }

    internal static string ShortName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[^1] : name;
    }
}

/// <summary>
/// Kural 4: Altında yumuşak gölgesi olan, etrafında dairesel kondisyon halkası (DrawArc) bulunan,
/// içinde forma numarası yazan ve altında cam isim plakası taşıyan katmanlı forma jetonu.
/// </summary>
internal sealed partial class PitchKitTokenButton : Button
{
    private readonly SquadSelectionPlayerDigest _player;
    private readonly bool _selected;

    public PitchKitTokenButton(
        SquadSelectionPlayerDigest player,
        bool selected,
        bool interactionEnabled,
        Vector2 minSize)
    {
        _player = player;
        _selected = selected;
        CustomMinimumSize = minSize;
        Flat = true;
        Text = string.Empty;
        Disabled = !interactionEnabled || (!player.IsAvailable && !player.IsStarter);
        TooltipText = $"{player.DisplayName} · {player.PositionName} · Güç {player.Rating} · Kondisyon %{player.Fitness} · Yorgunluk +{player.Fatigue}"
            + (player.IsAvailable ? string.Empty : " · [SAKAT]");
    }

    public override void _Draw()
    {
        var s = Size;
        if (s.X <= 4f || s.Y <= 4f)
        {
            s = CustomMinimumSize;
        }

        var tokenCenter = new Vector2(s.X * 0.5f, s.Y * 0.38f);
        var radius = Mathf.Min(s.X, s.Y) * 0.30f;
        var posColor = CareerUiTheme.PositionGroupColor(_player.PositionCode);
        var fitColor = _player.IsAvailable ? CareerUiTheme.FitnessColor(_player.Fitness) : CareerUiTheme.DangerSoft;

        // 1. Alt gölge (Drop Shadow)
        DrawCircle(tokenCenter + new Vector2(0f, 4f), radius + 3.5f, new Color(0f, 0f, 0f, 0.58f));

        // Seçili oyuncu parlak halka (Neon Glow)
        if (_selected)
        {
            DrawCircle(tokenCenter, radius + 6f, new Color(CareerUiTheme.ActionBright.R, CareerUiTheme.ActionBright.G, CareerUiTheme.ActionBright.B, 0.28f));
            DrawArc(tokenCenter, radius + 5.5f, 0f, Mathf.Tau, 32, CareerUiTheme.ActionBright, 2.2f, true);
        }

        // 2. Dış dairesel kondisyon halkası arka planı + aktif yay (Fitness Arc)
        DrawArc(tokenCenter, radius + 2.8f, 0f, Mathf.Tau, 28, new Color(0.03f, 0.06f, 0.10f, 0.88f), 3.0f, true);
        var fitSweep = Mathf.Clamp(_player.Fitness / 100f, 0.06f, 1f) * Mathf.Tau;
        DrawArc(tokenCenter, radius + 2.8f, -Mathf.Pi * 0.5f, -Mathf.Pi * 0.5f + fitSweep, 28, fitColor, 2.8f, true);

        // 3. İç Forma Jetonu (Katmanlı Kalkan / Forma Dairesi)
        DrawCircle(tokenCenter, radius, new Color(0.075f, 0.118f, 0.188f, 0.96f));
        DrawCircle(tokenCenter, radius - 2f, new Color(posColor.R, posColor.G, posColor.B, 0.88f));
        // Üst omuz/yaka parlaklığı
        DrawArc(tokenCenter, radius - 2.5f, Mathf.Pi * 1.15f, Mathf.Pi * 1.85f, 16, new Color(1f, 1f, 1f, 0.42f), 2.0f, true);

        var font = ThemeDB.FallbackFont;
        if (font is not null)
        {
            // 4. İçinde forma numarası (SlotIndex 1..11 veya 12..18)
            var shirtNo = (_player.SlotIndex + 1).ToString();
            DrawString(
                font,
                new Vector2(tokenCenter.X - radius, tokenCenter.Y + 4.5f),
                shirtNo,
                HorizontalAlignment.Center,
                radius * 2f,
                12,
                Colors.White);

            // Sağ üstte küçük OVR rozeti
            var ovrCenter = tokenCenter + new Vector2(radius * 0.85f, -radius * 0.72f);
            DrawCircle(ovrCenter, 7.5f, new Color(0.04f, 0.07f, 0.12f, 0.96f));
            DrawArc(ovrCenter, 7.5f, 0f, Mathf.Tau, 16, CareerUiTheme.RatingColor(_player.Rating), 1.3f, true);
            DrawString(
                font,
                new Vector2(ovrCenter.X - 8f, ovrCenter.Y + 3.5f),
                _player.Rating.ToString(),
                HorizontalAlignment.Center,
                16f,
                8,
                CareerUiTheme.RatingColor(_player.Rating));

            // 5. Alt cam isim plakası
            var plateRect = new Rect2(2f, s.Y - 16f, s.X - 4f, 14f);
            DrawRect(plateRect, new Color(0.05f, 0.09f, 0.15f, 0.90f), true);
            DrawLine(new Vector2(plateRect.Position.X, plateRect.Position.Y), new Vector2(plateRect.End.X, plateRect.Position.Y), _selected ? CareerUiTheme.ActionBright : CareerUiTheme.SpecularBorder, 1f);

            var labelText = $"{_player.PositionCode} {TacticalPitchBoardUi.ShortName(_player.DisplayName)}";
            DrawString(
                font,
                new Vector2(plateRect.Position.X + 2f, plateRect.Position.Y + 10.5f),
                labelText,
                HorizontalAlignment.Center,
                plateRect.Size.X - 4f,
                9,
                _selected ? CareerUiTheme.ActionBright : CareerUiTheme.Ink);
        }
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
    private int _phaseMode;

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
        _playerButtonMinimumSize = playerButtonMinimumSize ?? new Vector2(68, 56);
        _formation = formation;
        ClipContents = true;
    }

    /// <summary>
    /// Normalize saha koordinatını (0..1, 0..1) hafif perspektifli (trapezoid) stadyum sahasına izdüşürür.
    /// </summary>
    private Vector2 ProjectToTrapezoid(Vector2 norm)
    {
        var topInset = Size.X * 0.085f;
        var bottomInset = Size.X * 0.020f;
        var yPad = Mathf.Max(8f, Size.Y * 0.03f);
        var py = Mathf.Lerp(yPad, Size.Y - yPad, norm.Y);
        var leftX = Mathf.Lerp(topInset, bottomInset, norm.Y);
        var rightX = Mathf.Lerp(Size.X - topInset, Size.X - bottomInset, norm.Y);
        var px = Mathf.Lerp(leftX, rightX, norm.X);
        return new Vector2(px, py);
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
            var target = ProjectToTrapezoid(shifted) - (button.Size * 0.5f);
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
            var yShift = basePos.Y > 0.65f ? -0.11f : (basePos.Y > 0.40f ? -0.13f : -0.06f);
            var xSpread = basePos.X < 0.30f ? -0.04f : (basePos.X > 0.70f ? 0.04f : 0f);
            return new Vector2(
                Mathf.Clamp(basePos.X + xSpread, 0.10f, 0.90f),
                Mathf.Clamp(basePos.Y + yShift, 0.14f, 0.86f));
        }

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
        // Stadyum gece arka planı (#070B12)
        DrawRect(new Rect2(Vector2.Zero, Size), CareerUiTheme.BackgroundDeep, true);

        // Kural 4: Koyu zümrüt yeşili (#0A2E23 / #0D382B) çim şeritleri olan hafif perspektifli (trapezoid) stadyum sahası
        var turfDark = new Color(0.0392f, 0.1804f, 0.1373f, 1f);  // #0A2E23
        var turfLight = new Color(0.0510f, 0.2196f, 0.1686f, 1f); // #0D382B
        const int stripeCount = 10;

        for (var i = 0; i < stripeCount; i++)
        {
            var y0 = i / (float)stripeCount;
            var y1 = (i + 1) / (float)stripeCount;
            var pTopLeft = ProjectToTrapezoid(new Vector2(0f, y0));
            var pTopRight = ProjectToTrapezoid(new Vector2(1f, y0));
            var pBotRight = ProjectToTrapezoid(new Vector2(1f, y1));
            var pBotLeft = ProjectToTrapezoid(new Vector2(0f, y1));
            DrawColoredPolygon(
                new Vector2[] { pTopLeft, pTopRight, pBotRight, pBotLeft },
                i % 2 == 0 ? turfDark : turfLight);
        }

        // Faz Odak Bölgesi Görselleştirmesi
        if (_phaseMode == 1)
        {
            var h0 = ProjectToTrapezoid(new Vector2(0f, 0f));
            var h1 = ProjectToTrapezoid(new Vector2(1f, 0f));
            var h2 = ProjectToTrapezoid(new Vector2(1f, 0.42f));
            var h3 = ProjectToTrapezoid(new Vector2(0f, 0.42f));
            DrawColoredPolygon(new Vector2[] { h0, h1, h2, h3 }, new Color(1.0f, 0.72f, 0f, 0.14f));
            DrawLine(h3, h2, new Color(1.0f, 0.72f, 0f, 0.75f), 2f);
        }
        else if (_phaseMode == 2)
        {
            var b0 = ProjectToTrapezoid(new Vector2(0.12f, 0.46f));
            var b1 = ProjectToTrapezoid(new Vector2(0.88f, 0.46f));
            var b2 = ProjectToTrapezoid(new Vector2(0.88f, 0.86f));
            var b3 = ProjectToTrapezoid(new Vector2(0.12f, 0.86f));
            DrawColoredPolygon(new Vector2[] { b0, b1, b2, b3 }, new Color(0.22f, 0.74f, 0.97f, 0.15f));
            DrawPolyline(new Vector2[] { b0, b1, b2, b3, b0 }, new Color(0.22f, 0.74f, 0.97f, 0.65f), 2f, true);
        }

        // Kural 3: Saha Üstü "Kimya / Sinerji Çizgileri" (EA FC / SM27 Chemistry Links)
        var linkColor = _phaseMode == 1
            ? new Color(1.0f, 0.72f, 0.0f, 0.34f)
            : _phaseMode == 2
                ? new Color(0.22f, 0.74f, 0.97f, 0.34f)
                : new Color(0.0f, 0.96f, 0.61f, 0.26f);

        var goodLinks = 0;
        var totalLinks = 0;

        for (var i = 1; i < _buttons.Count; i++)
        {
            var p1 = ComputePhasePosition(_buttons[i].BasePosition, i, _phaseMode);
            var pl1 = i < _players.Count ? _players[i] : null;
            for (var j = i + 1; j < _buttons.Count; j++)
            {
                var p2 = ComputePhasePosition(_buttons[j].BasePosition, j, _phaseMode);
                if (p1.DistanceTo(p2) <= 0.34f)
                {
                    totalLinks++;
                    var pl2 = j < _players.Count ? _players[j] : null;
                    var pt1 = ProjectToTrapezoid(p1);
                    var pt2 = ProjectToTrapezoid(p2);

                    if (pl1 is not null && pl2 is not null)
                    {
                        if (!pl1.IsAvailable || !pl2.IsAvailable || pl1.Fitness < 55 || pl2.Fitness < 55)
                        {
                            // Uyumsuz / Sakat / Düşük Kondisyon: Kesikli Kırmızı (#FF2E54)
                            DrawDashedLine(pt1, pt2, new Color(1.0f, 0.18f, 0.33f, 0.78f), 2.0f, 5.0f);
                        }
                        else if (pl1.Fitness >= 70 && pl2.Fitness >= 70 && pl1.Fatigue <= 35 && pl2.Fatigue <= 35)
                        {
                            // Kusursuz EA FC Kimya Bağı: Parlak Neon Yeşili (#00F59B)
                            goodLinks++;
                            DrawLine(pt1, pt2, new Color(0.0f, 0.96f, 0.61f, 0.85f), 2.2f, true);
                        }
                        else
                        {
                            // Orta Uyum: Yayın Altını (#FFB800)
                            goodLinks++;
                            DrawLine(pt1, pt2, new Color(1.0f, 0.72f, 0.0f, 0.75f), 1.8f, true);
                        }
                    }
                    else
                    {
                        DrawLine(pt1, pt2, linkColor, 1.8f, true);
                    }
                }
            }
        }

        // Sağ Üstte Floating "TAKIM KİMYASI: %88" Rozeti
        var chemPercent = totalLinks > 0 ? Mathf.Clamp(Mathf.RoundToInt(60 + (goodLinks * 40f / totalLinks)), 60, 100) : 88;
        var chemPillRect = new Rect2(Size.X - 160f, 10f, 150f, 26f);
        DrawRect(chemPillRect, new Color(0.04f, 0.07f, 0.12f, 0.88f), true);
        DrawRect(chemPillRect, CareerUiTheme.Accent, false, 1.4f);
        var chemFont = ThemeDB.FallbackFont;
        if (chemFont is not null)
        {
            DrawString(chemFont, new Vector2(chemPillRect.Position.X, chemPillRect.Position.Y + 18f),
                $"TAKIM KİMYASI: %{chemPercent}", HorizontalAlignment.Center, chemPillRect.Size.X, 11, CareerUiTheme.Accent);
        }

        // Perspektif (Trapezoid) Beyaz-Neon Saha Çizgileri
        var line = new Color(0.85f, 0.96f, 0.91f, 0.68f);
        var tl = ProjectToTrapezoid(new Vector2(0f, 0f));
        var tr = ProjectToTrapezoid(new Vector2(1f, 0f));
        var br = ProjectToTrapezoid(new Vector2(1f, 1f));
        var bl = ProjectToTrapezoid(new Vector2(0f, 1f));
        DrawPolyline(new Vector2[] { tl, tr, br, bl, tl }, line, 2f, true);

        // Orta saha çizgisi
        var midLeft = ProjectToTrapezoid(new Vector2(0f, 0.5f));
        var midRight = ProjectToTrapezoid(new Vector2(1f, 0.5f));
        DrawLine(midLeft, midRight, line, 2f, true);

        // Orta yuvarlak (perspektif elips)
        var center = ProjectToTrapezoid(new Vector2(0.5f, 0.5f));
        var rx = (midRight.X - midLeft.X) * 0.14f;
        var ry = Size.Y * 0.11f;
        var ellipsePts = new Vector2[33];
        for (var i = 0; i <= 32; i++)
        {
            var a = i * Mathf.Tau / 32f;
            ellipsePts[i] = center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
        }
        DrawPolyline(ellipsePts, line, 1.8f, true);
        DrawCircle(center, 3f, line);

        // Üst ve Alt Ceza Sahaları (Trapezoid)
        DrawTrapezoidBox(new Vector2(0.22f, 0f), new Vector2(0.78f, 0.16f), line, 2f);
        DrawTrapezoidBox(new Vector2(0.22f, 0.84f), new Vector2(0.78f, 1f), line, 2f);

        // 6 Pas (Kale Sahaları)
        DrawTrapezoidBox(new Vector2(0.36f, 0f), new Vector2(0.64f, 0.06f), line, 1.5f);
        DrawTrapezoidBox(new Vector2(0.36f, 0.94f), new Vector2(0.64f, 1f), line, 1.5f);

        // Penaltı noktaları
        DrawCircle(ProjectToTrapezoid(new Vector2(0.5f, 0.11f)), 2.5f, line);
        DrawCircle(ProjectToTrapezoid(new Vector2(0.5f, 0.89f)), 2.5f, line);
    }

    private void DrawTrapezoidBox(Vector2 minNorm, Vector2 maxNorm, Color color, float width)
    {
        var p0 = ProjectToTrapezoid(new Vector2(minNorm.X, minNorm.Y));
        var p1 = ProjectToTrapezoid(new Vector2(maxNorm.X, minNorm.Y));
        var p2 = ProjectToTrapezoid(new Vector2(maxNorm.X, maxNorm.Y));
        var p3 = ProjectToTrapezoid(new Vector2(minNorm.X, maxNorm.Y));
        DrawPolyline(new Vector2[] { p0, p1, p2, p3, p0 }, color, width, true);
    }

    private void LayoutPlayers()
    {
        for (var i = 0; i < _buttons.Count; i++)
        {
            var (button, basePos) = _buttons[i];
            var shifted = ComputePhasePosition(basePos, i, _phaseMode);
            button.Size = _playerButtonMinimumSize;
            var target = ProjectToTrapezoid(shifted);
            button.Position = target - (button.Size * 0.5f);
        }
    }
}
