using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Eski giriş sahnesi — CareerAppRoot'a yönlendirir (smoke + menü/hub akışı).
/// </summary>
public partial class TimeControlScreen : Control
{
    public override void _Ready()
    {
        var root = new CareerAppRoot();
        root.AnchorRight = 1f;
        root.AnchorBottom = 1f;
        root.GrowHorizontal = GrowDirection.Both;
        root.GrowVertical = GrowDirection.Both;
        AddChild(root);
    }
}
