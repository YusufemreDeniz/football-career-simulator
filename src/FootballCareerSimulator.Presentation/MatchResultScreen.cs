using FootballCareerSimulator.Application.Competition.Queries;
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
        CareerUiTheme.EnsureLoaded();
        AddChild(CareerUiTheme.CreateAtmosphereBackground());

        var narrative = _results.Narrative
            ?? MatchNightNarrative.Failure(_results.Message);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.GrowHorizontal = GrowDirection.Both;
        margin.GrowVertical = GrowDirection.Both;
        margin.AddThemeConstantOverride("margin_left", 40);
        margin.AddThemeConstantOverride("margin_top", 40);
        margin.AddThemeConstantOverride("margin_right", 40);
        margin.AddThemeConstantOverride("margin_bottom", 36);
        AddChild(margin);

        var layout = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        layout.AddThemeConstantOverride("separation", 10);
        margin.AddChild(layout);

        var brand = new Label
        {
            Text = narrative.BrandTitle,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBrand(brand);
        layout.AddChild(brand);

        var brandLine = new ColorRect
        {
            Color = new Color(CareerUiTheme.Accent.R, CareerUiTheme.Accent.G, CareerUiTheme.Accent.B, 0.55f),
            CustomMinimumSize = new Vector2(48, 3),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        layout.AddChild(brandLine);

        var score = new Label
        {
            Text = narrative.Scoreline,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        StyleScore(score);
        score.Modulate = new Color(1f, 1f, 1f, 0f);
        layout.AddChild(score);

        var tone = new Label
        {
            Text = narrative.OutcomeTone,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(tone);
        tone.Modulate = new Color(1f, 1f, 1f, 0f);
        layout.AddChild(tone);

        if (!string.IsNullOrWhiteSpace(narrative.SupportingLine))
        {
            var support = new Label
            {
                Text = narrative.SupportingLine,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            CareerUiTheme.StyleBody(support, muted: true);
            layout.AddChild(support);
        }

        if (narrative.KickoffLines.Count > 0)
        {
            layout.AddChild(SectionLabel("Maça böyle girdin"));
            foreach (var kickoff in narrative.KickoffLines)
            {
                var line = new Label
                {
                    Text = "· " + kickoff,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };
                CareerUiTheme.StyleBody(line, muted: true);
                layout.AddChild(line);
            }
        }

        if (narrative.BeatLines.Count > 0)
        {
            layout.AddChild(SectionLabel("Anlar"));
            foreach (var beat in narrative.BeatLines.Take(6))
            {
                var line = new Label
                {
                    Text = beat,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };
                CareerUiTheme.StyleBody(line);
                layout.AddChild(line);
            }
        }

        if (narrative.AfterWhistleLines.Count > 0)
        {
            layout.AddChild(SectionLabel("Düdük sonrası"));
            foreach (var lineText in narrative.AfterWhistleLines)
            {
                var line = new Label
                {
                    Text = lineText,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };
                CareerUiTheme.StyleBody(line);
                layout.AddChild(line);
            }
        }

        if (narrative.OtherScorelines.Count > 0)
        {
            layout.AddChild(SectionLabel("Diğer sonuçlar"));
            var list = new ItemList
            {
                CustomMinimumSize = new Vector2(0, 88),
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            CareerUiTheme.StyleList(list);
            foreach (var other in narrative.OtherScorelines)
            {
                list.AddItem(other);
            }

            layout.AddChild(list);
        }

        layout.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

        var continueButton = new Button { Text = "Kariyere Dön" };
        CareerUiTheme.StylePrimaryButton(continueButton);
        continueButton.Pressed += () => ContinueRequested?.Invoke();
        layout.AddChild(continueButton);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(score, "modulate:a", 1f, 0.4f).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(tone, "modulate:a", 1f, 0.55f).SetDelay(0.12f);
    }

    private static Label SectionLabel(string text)
    {
        var label = new Label { Text = text };
        CareerUiTheme.StyleSection(label);
        return label;
    }

    private static void StyleScore(Label label)
    {
        CareerUiTheme.EnsureLoaded();
        CareerUiTheme.StyleHeadline(label);
        label.AddThemeFontSizeOverride("font_size", 28);
        label.AddThemeColorOverride("font_color", CareerUiTheme.Ink);
    }
}
