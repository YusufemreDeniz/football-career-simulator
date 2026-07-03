using FootballCareerSimulator.Application;
using FootballCareerSimulator.Domain;
using FootballCareerSimulator.Simulation;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// docs/18_SPIKE_EXECUTION_PLAN.md Kart 5 için oluşturulmuş yer tutucu tek ekrandır. Amaç, gerçek bir
/// oyun ekranı sunmak değil; Presentation katmanının Domain/Simulation state'ini doğrudan değiştirmeden,
/// yalnızca Application katmanındaki bir use case'i çağırıp sonucu (bir "read model") görüntüleyerek
/// çalıştığını kanıtlamaktır (`docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 8.4).
/// </summary>
public partial class Shell : Control
{
    public override void _Ready()
    {
        var useCase = new AdvancePlaceholderSimulationUseCase(new PlaceholderWorldLoop());
        var result = useCase.Execute(SimulationStep.Zero, stepCount: 10);

        var label = GetNode<Label>("Label");
        label.Text = "Football Career Simulator\n"
            + "Presentation kabuğu (Kart 5 yer tutucu ekranı)\n"
            + $"Application katmanından okunan yer tutucu sonuç: {result.Value}";

        // Görsel çıktı olmadan (headless) çalıştırıldığında da smoke test edilebilmesi için konsola yazılır.
        GD.Print($"[Shell] Hazır. Yer tutucu sonuç: {result.Value}");
    }
}
