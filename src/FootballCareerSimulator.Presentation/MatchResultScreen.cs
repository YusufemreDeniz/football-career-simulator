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

        var shell = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddThemeConstantOverride("separation", 14);
        margin.AddChild(shell);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        shell.AddChild(scroll);

        var layout = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        layout.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(layout);

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

        if (_results.Report is { } report)
        {
            layout.AddChild(SectionLabel("Maç Raporu"));

            var stats = new GridContainer
            {
                Columns = 3,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            stats.AddThemeConstantOverride("h_separation", 18);
            stats.AddThemeConstantOverride("v_separation", 8);
            stats.AddChild(ReportCell(report.HomeClubName, HorizontalAlignment.Left, emphasized: true));
            stats.AddChild(ReportCell("", HorizontalAlignment.Center));
            stats.AddChild(ReportCell(report.AwayClubName, HorizontalAlignment.Right, emphasized: true));
            foreach (var stat in report.StatLines)
            {
                stats.AddChild(ReportCell(stat.HomeValue, HorizontalAlignment.Left, emphasized: true));
                stats.AddChild(ReportCell(stat.Label, HorizontalAlignment.Center, muted: true));
                stats.AddChild(ReportCell(stat.AwayValue, HorizontalAlignment.Right, emphasized: true));
            }

            layout.AddChild(stats);

            if (!string.IsNullOrWhiteSpace(report.StandoutLine))
            {
                var standout = new Label
                {
                    Text = report.StandoutLine,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };
                CareerUiTheme.StyleBody(standout);
                standout.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
                layout.AddChild(standout);
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
                CustomMinimumSize = new Vector2(0, Math.Min(88, 28 + narrative.OtherScorelines.Count * 22)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            CareerUiTheme.StyleList(list);
            foreach (var other in narrative.OtherScorelines)
            {
                list.AddItem(other);
            }

            layout.AddChild(list);
        }

        var continueButton = new Button
        {
            Text = "Kariyere Dön",
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(220, 40),
        };
        CareerUiTheme.StylePrimaryButton(continueButton);
        continueButton.Pressed += OnContinuePressed;
        shell.AddChild(continueButton);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(score, "modulate:a", 1f, 0.4f).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(tone, "modulate:a", 1f, 0.55f).SetDelay(0.12f);
    }

    private void OnContinuePressed()
    {
        // Ekranı sinyal ortasında QueueFree etmemek için bir kare ertele.
        Callable.From(() => ContinueRequested?.Invoke()).CallDeferred();
    }

    private static Label SectionLabel(string text)
    {
        var label = new Label { Text = text };
        CareerUiTheme.StyleSection(label);
        return label;
    }

    private static Label ReportCell(
        string text,
        HorizontalAlignment alignment,
        bool muted = false,
        bool emphasized = false)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = alignment,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(label, muted);
        if (emphasized)
        {
            label.AddThemeFontSizeOverride("font_size", 16);
            label.AddThemeColorOverride("font_color", CareerUiTheme.Ink);
        }

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
