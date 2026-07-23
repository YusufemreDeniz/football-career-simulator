using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class MainMenuScreen : Control
{
    private Label _statusLabel = null!;

    public event Action? NewCareerRequested;

    public event Action? ContinueRequested;

    public override void _Ready()
    {
        var margin = new MarginContainer();
        margin.AnchorRight = 1f;
        margin.AnchorBottom = 1f;
        margin.GrowHorizontal = GrowDirection.Both;
        margin.GrowVertical = GrowDirection.Both;
        margin.AddThemeConstantOverride("margin_left", 40);
        margin.AddThemeConstantOverride("margin_top", 40);
        margin.AddThemeConstantOverride("margin_right", 40);
        margin.AddThemeConstantOverride("margin_bottom", 40);
        AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 16);
        margin.AddChild(layout);

        layout.AddChild(new Label
        {
            Text = "Football Career Simulator",
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        layout.AddChild(new Label
        {
            Text = "İnce kariyer döngüsü — menajer, lig, maç, kayıt",
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        var newButton = new Button { Text = "Yeni Kariyer" };
        newButton.Pressed += () => NewCareerRequested?.Invoke();
        layout.AddChild(newButton);

        var continueButton = new Button { Text = "Devam Et" };
        var savePath = Path.Combine(OS.GetUserDataDir(), "career_save.db");
        continueButton.Disabled = !File.Exists(savePath);
        continueButton.Pressed += () => ContinueRequested?.Invoke();
        layout.AddChild(continueButton);

        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Text = continueButton.Disabled
                ? "Kayıt yok — Yeni Kariyer ile başla."
                : $"Kayıt bulundu:\n{savePath}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        layout.AddChild(_statusLabel);
    }

    public void SetStatus(string message) => _statusLabel.Text = message;
}
