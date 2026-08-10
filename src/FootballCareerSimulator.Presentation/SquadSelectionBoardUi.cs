using FootballCareerSimulator.Application.TeamPreparation.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

internal static class SquadSelectionBoardUi
{
    public static Control Build(
        SquadSelectionBoardDigest board,
        int? selectedSlotIndex,
        Action<SquadSelectionPlayerDigest> selectPlayer,
        Action<int, int> swapPlayers)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(selectPlayer);
        ArgumentNullException.ThrowIfNull(swapPlayers);

        var stack = MatchScreenUi.VerticalStack(10);
        stack.AddChild(BuildGroup(
            "İLK 11",
            board.StartingXi,
            selectedSlotIndex,
            selectPlayer,
            swapPlayers));
        stack.AddChild(BuildGroup(
            "YEDEKLER",
            board.Bench,
            selectedSlotIndex,
            selectPlayer,
            swapPlayers));
        return stack;
    }

    private static Control BuildGroup(
        string title,
        IReadOnlyList<SquadSelectionPlayerDigest> players,
        int? selectedSlotIndex,
        Action<SquadSelectionPlayerDigest> selectPlayer,
        Action<int, int> swapPlayers)
    {
        var panel = MatchScreenUi.Card();
        var stack = MatchScreenUi.VerticalStack(6);
        panel.AddChild(stack);

        var titleLabel = new Label { Text = title };
        CareerUiTheme.StyleEyebrow(titleLabel);
        stack.AddChild(titleLabel);

        foreach (var player in players)
        {
            var button = new SquadSelectionPlayerButton(player, swapPlayers)
            {
                Text = player.ButtonLabel,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 46),
                Disabled = !player.IsAvailable && !player.IsStarter,
                TooltipText = $"Yorgunluk %{player.Fatigue} · Fitness %{player.Fitness}",
                Alignment = HorizontalAlignment.Left,
            };
            if (selectedSlotIndex == player.SlotIndex)
            {
                CareerUiTheme.StylePrimaryButton(button);
            }
            else
            {
                CareerUiTheme.StyleSecondaryButton(button);
            }

            button.Pressed += () => selectPlayer(player);
            stack.AddChild(button);
        }

        return panel;
    }
}

internal sealed partial class SquadSelectionPlayerButton : Button
{
    private const string DragPrefix = "squad-player";
    private readonly SquadSelectionPlayerDigest _player;
    private readonly Action<int, int> _swapPlayers;

    public SquadSelectionPlayerButton(
        SquadSelectionPlayerDigest player,
        Action<int, int> swapPlayers)
    {
        _player = player;
        _swapPlayers = swapPlayers;
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Disabled)
        {
            return default;
        }

        var preview = new Label
        {
            Text = Text,
            CustomMinimumSize = new Vector2(Mathf.Min(Size.X, 340), 42),
        };
        CareerUiTheme.StyleBody(preview);
        SetDragPreview(preview);
        return $"{DragPrefix}|{_player.SlotIndex}|{_player.IsStarter}";
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        return !Disabled
            && TryReadDragData(data, out _, out var sourceIsStarter)
            && sourceIsStarter != _player.IsStarter;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (!TryReadDragData(data, out var sourceSlotIndex, out var sourceIsStarter)
            || sourceIsStarter == _player.IsStarter)
        {
            return;
        }

        var starterSlotIndex = sourceIsStarter ? sourceSlotIndex : _player.SlotIndex;
        var benchSlotIndex = sourceIsStarter ? _player.SlotIndex : sourceSlotIndex;
        _swapPlayers(starterSlotIndex, benchSlotIndex);
    }

    private static bool TryReadDragData(
        Variant data,
        out int slotIndex,
        out bool isStarter)
    {
        slotIndex = default;
        isStarter = default;
        var parts = data.AsString().Split('|');
        return parts.Length == 3
            && parts[0] == DragPrefix
            && int.TryParse(parts[1], out slotIndex)
            && bool.TryParse(parts[2], out isStarter);
    }
}
