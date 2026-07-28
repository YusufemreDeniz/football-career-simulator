using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class MatchResultScreen : Control
{
    private readonly PlayMatchesUiResult _results;

    public event Action? ContinueRequested;

    public MatchResultScreen(PlayMatchesUiResult results)
    {
        _results = results;
    }

    public override void _Ready()
    {
        var margin = new MarginContainer();
        margin.AnchorRight = 1f;
        margin.AnchorBottom = 1f;
        margin.GrowHorizontal = GrowDirection.Both;
        margin.GrowVertical = GrowDirection.Both;
        margin.AddThemeConstantOverride("margin_left", 32);
        margin.AddThemeConstantOverride("margin_top", 32);
        margin.AddThemeConstantOverride("margin_right", 32);
        margin.AddThemeConstantOverride("margin_bottom", 32);
        AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 12);
        margin.AddChild(layout);

        layout.AddChild(new Label
        {
            Text = _results.Succeeded ? "Maç Sonuçları" : "Maç Oynatılamadı",
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        layout.AddChild(new Label
        {
            Text = _results.Message,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        var list = new ItemList
        {
            CustomMinimumSize = new Vector2(0, 140),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        foreach (var line in _results.MatchLines)
        {
            list.AddItem(line);
        }

        if (_results.MatchLines.Count == 0)
        {
            list.AddItem("(Sonuç satırı yok)");
        }

        layout.AddChild(list);

        var keyMoments = _results.KeyMomentLines ?? Array.Empty<string>();
        if (keyMoments.Count > 0)
        {
            layout.AddChild(new Label
            {
                Text = "Önemli anlar",
                HorizontalAlignment = HorizontalAlignment.Left,
            });

            var momentList = new ItemList
            {
                CustomMinimumSize = new Vector2(0, 120),
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            foreach (var line in keyMoments)
            {
                momentList.AddItem(line);
            }

            layout.AddChild(momentList);
        }

        var consequences = _results.ConsequenceLines ?? Array.Empty<string>();
        if (consequences.Count > 0)
        {
            layout.AddChild(new Label
            {
                Text = "Sonuçlar",
                HorizontalAlignment = HorizontalAlignment.Left,
            });

            var consequenceList = new ItemList
            {
                CustomMinimumSize = new Vector2(0, 120),
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            foreach (var line in consequences)
            {
                consequenceList.AddItem(line);
            }

            layout.AddChild(consequenceList);
        }

        var continueButton = new Button { Text = "Kariyere Dön" };
        continueButton.Pressed += () => ContinueRequested?.Invoke();
        layout.AddChild(continueButton);
    }
}
