using FootballCareerSimulator.Application.TeamPreparation.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

internal static class TacticalPitchBoardUi
{
    public static Control BuildReadOnly(IReadOnlyList<SquadSelectionPlayerDigest> startingXi)
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
            _ => { },
            interactionEnabled: false)
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(440, 280),
        });
        return root;
    }

    public static Control Build(
        SquadSelectionBoardDigest board,
        int? selectedSlotIndex,
        Action<SquadSelectionPlayerDigest> selectPlayer,
        Action<int, int> swapPlayers,
        bool interactionEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(selectPlayer);
        ArgumentNullException.ThrowIfNull(swapPlayers);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        root.AddThemeConstantOverride("separation", 8);

        root.AddChild(BuildPitch(
            board,
            selectedSlotIndex,
            selectPlayer,
            interactionEnabled,
            new Vector2(480, 300),
            new Vector2(76, 48)));

        root.AddChild(BuildBench(
            board,
            selectedSlotIndex,
            selectPlayer,
            interactionEnabled,
            new Vector2(76, 48)));

        return root;
    }

    public static Control BuildPitch(
        SquadSelectionBoardDigest board,
        int? selectedSlotIndex,
        Action<SquadSelectionPlayerDigest> selectPlayer,
        bool interactionEnabled,
        Vector2 minimumSize,
        Vector2 playerButtonMinimumSize)
    {
        return new TacticalPitchBoard(
            board.StartingXi,
            selectedSlotIndex,
            selectPlayer,
            interactionEnabled,
            playerButtonMinimumSize)
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
        root.AddThemeConstantOverride("separation", 5);
        var benchTitle = new Label { Text = "YEDEK KULUBESI" };
        CareerUiTheme.StyleEyebrow(benchTitle, CareerUiTheme.Data);
        root.AddChild(benchTitle);

        var scroll = new MobileScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        root.AddChild(scroll);
        var bench = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        bench.AddThemeConstantOverride("separation", 6);
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
        var button = new Button
        {
            Text = $"{ShortName(player.DisplayName)}\n{player.PositionCode}  {player.Rating}",
            TooltipText = $"{player.DisplayName} · {player.PositionName} · Güç {player.Rating} · Fitness %{player.Fitness}",
            Disabled = !interactionEnabled || (!player.IsAvailable && !player.IsStarter),
        };
        if (selected)
        {
            CareerUiTheme.StylePrimaryButton(button);
        }
        else
        {
            CareerUiTheme.StyleSecondaryButton(button);
        }

        button.AddThemeFontSizeOverride("font_size", 11);
        button.CustomMinimumSize = minimumSize ?? new Vector2(76, 48);
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
    private static readonly Vector2[] Positions =
    [
        new(0.50f, 0.90f),
        new(0.17f, 0.72f), new(0.39f, 0.76f), new(0.61f, 0.76f), new(0.83f, 0.72f),
        new(0.17f, 0.48f), new(0.39f, 0.53f), new(0.61f, 0.53f), new(0.83f, 0.48f),
        new(0.35f, 0.24f), new(0.65f, 0.24f),
    ];

    private readonly IReadOnlyList<SquadSelectionPlayerDigest> _players;
    private readonly int? _selectedSlotIndex;
    private readonly Action<SquadSelectionPlayerDigest> _selectPlayer;
    private readonly bool _interactionEnabled;
    private readonly Vector2 _playerButtonMinimumSize;
    private readonly List<(Button Button, Vector2 Position)> _buttons = [];

    public TacticalPitchBoard(
        IReadOnlyList<SquadSelectionPlayerDigest> players,
        int? selectedSlotIndex,
        Action<SquadSelectionPlayerDigest> selectPlayer,
        bool interactionEnabled,
        Vector2? playerButtonMinimumSize = null)
    {
        _players = players;
        _selectedSlotIndex = selectedSlotIndex;
        _selectPlayer = selectPlayer;
        _interactionEnabled = interactionEnabled;
        _playerButtonMinimumSize = playerButtonMinimumSize ?? new Vector2(76, 48);
        ClipContents = true;
    }

    public override void _Ready()
    {
        foreach (var (player, index) in _players.Take(Positions.Length).Select((player, index) => (player, index)))
        {
            var button = TacticalPitchBoardUi.PlayerButton(
                player,
                _selectedSlotIndex == player.SlotIndex,
                _interactionEnabled,
                _playerButtonMinimumSize);
            button.Pressed += () => _selectPlayer(player);
            AddChild(button);
            _buttons.Add((button, Positions[index]));
        }

        Resized += LayoutPlayers;
        LayoutPlayers();
        QueueRedraw();
    }

    public override void _Draw()
    {
        var field = new Rect2(Vector2.Zero, Size);
        DrawRect(field, new Color(0.055f, 0.34f, 0.17f));

        var stripeHeight = Size.Y / 8f;
        for (var stripe = 0; stripe < 8; stripe += 2)
        {
            DrawRect(new Rect2(0, stripe * stripeHeight, Size.X, stripeHeight), new Color(0.08f, 0.42f, 0.22f));
        }

        var line = new Color(0.88f, 0.96f, 0.90f, 0.78f);
        var inset = Mathf.Max(10, Size.X * 0.025f);
        var inner = new Rect2(inset, inset, Size.X - (inset * 2), Size.Y - (inset * 2));
        DrawRect(inner, line, false, 2);
        DrawLine(new Vector2(inset, Size.Y * 0.5f), new Vector2(Size.X - inset, Size.Y * 0.5f), line, 2);
        DrawArc(new Vector2(Size.X * 0.5f, Size.Y * 0.5f), Mathf.Min(Size.X, Size.Y) * 0.11f, 0, Mathf.Tau, 36, line, 2);

        var boxWidth = Size.X * 0.38f;
        var boxHeight = Size.Y * 0.16f;
        DrawRect(new Rect2((Size.X - boxWidth) * 0.5f, inset, boxWidth, boxHeight), line, false, 2);
        DrawRect(new Rect2((Size.X - boxWidth) * 0.5f, Size.Y - inset - boxHeight, boxWidth, boxHeight), line, false, 2);
    }

    private void LayoutPlayers()
    {
        foreach (var (button, position) in _buttons)
        {
            var target = new Vector2(position.X * Size.X, position.Y * Size.Y);
            button.Position = target - (button.Size * 0.5f);
        }
    }
}
