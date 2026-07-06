using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// docs/18_SPIKE_EXECUTION_PLAN.md Kart 5 yer tutucu ekranı. Ana sahne artık PlayerListScreen'dir;
/// Production zaman kontrolü ekranı Kart 6'da eklenecektir.
/// </summary>
public partial class Shell : Control
{
    public override void _Ready()
    {
        var label = GetNode<Label>("Label");
        label.Text = "Football Career Simulator\n"
            + "Presentation kabuğu (Kart 5 yer tutucu ekranı)\n"
            + "World & Calendar Application katmanı Kart 3'te eklendi.";

        GD.Print("[Shell] Hazır. Yer tutucu kabuk ekranı.");
    }
}
