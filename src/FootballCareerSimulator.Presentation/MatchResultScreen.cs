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
        var narrative = _results.Narrative
            ?? MatchNightNarrative.Failure(_results.Message);

        var margin = MatchScreenUi.CreateStageRoot(
            this,
            new Color(CareerUiTheme.Accent.R, CareerUiTheme.Accent.G, CareerUiTheme.Accent.B, 0.045f));

        var shell = MatchScreenUi.VerticalStack(12);
        shell.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(shell);

        shell.AddChild(MatchScreenUi.StageMarker("04  •  MAÇ SONU", "RAPOR", CareerUiTheme.Accent));

        var scroll = MatchScreenUi.ScrollArea();
        shell.AddChild(scroll);

        var content = MatchScreenUi.VerticalStack(15);
        scroll.AddChild(content);

        var hero = MatchScreenUi.Card(emphasized: true);
        content.AddChild(hero);
        var heroContent = MatchScreenUi.VerticalStack(8);
        hero.AddChild(heroContent);

        var brand = new Label
        {
            Text = narrative.BrandTitle.ToUpperInvariant(),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleEyebrow(brand, CareerUiTheme.Accent);
        heroContent.AddChild(brand);

        var score = new Label
        {
            Text = narrative.Scoreline,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        StyleScore(score);
        score.Modulate = new Color(1f, 1f, 1f, 0f);
        heroContent.AddChild(score);

        var tone = new Label
        {
            Text = narrative.OutcomeTone,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(tone);
        tone.AddThemeFontSizeOverride("font_size", 24);
        tone.Modulate = new Color(1f, 1f, 1f, 0f);
        heroContent.AddChild(tone);

        if (!string.IsNullOrWhiteSpace(narrative.SupportingLine))
        {
            heroContent.AddChild(MatchScreenUi.BodyLine(
                narrative.SupportingLine,
                muted: true,
                alignment: HorizontalAlignment.Center));
        }

        if (narrative.Atmosphere is { } stadium)
        {
            content.AddChild(MatchScreenUi.SectionTitle("STADYUM", stadium.Headline));
            var atmospherePanel = MatchScreenUi.Card();
            content.AddChild(atmospherePanel);
            atmospherePanel.AddChild(MatchScreenUi.BodyLine(
                stadium.CrowdLine,
                muted: true,
                alignment: HorizontalAlignment.Center));
        }

        if (narrative.KickoffLines.Count > 0)
        {
            content.AddChild(MatchScreenUi.SectionTitle("BAŞLAMA ANI", "Maça böyle girdin"));
            var kickoffPanel = MatchScreenUi.Card();
            content.AddChild(kickoffPanel);
            var kickoffStack = MatchScreenUi.VerticalStack(8);
            kickoffPanel.AddChild(kickoffStack);
            foreach (var kickoff in narrative.KickoffLines)
            {
                kickoffStack.AddChild(MatchScreenUi.BeatLine(kickoff, muted: true));
            }
        }

        if (narrative.LineupBridge is { StartingXi.Count: > 0 } lineup)
        {
            content.AddChild(MatchScreenUi.SectionTitle("KADRO", "Böyle çıktın"));
            content.AddChild(LineupStripUi.BuildPanel(lineup, lineup.ResultBridgeCaption));
        }

        if (_results.Report is { } report)
        {
            content.AddChild(MatchScreenUi.SectionTitle("VERİ MERKEZİ", "Maç raporu"));
            content.AddChild(BuildReportPanel(report));
        }

        if (_results.TechnicalArea is { } technicalArea)
        {
            content.AddChild(MatchScreenUi.SectionTitle("TEKNİK ALAN", technicalArea.BrandTitle));
            var technicalPanel = MatchScreenUi.Card();
            content.AddChild(technicalPanel);
            var technicalStack = MatchScreenUi.VerticalStack(9);
            technicalPanel.AddChild(technicalStack);

            technicalStack.AddChild(MatchScreenUi.BodyLine(
                technicalArea.DecisionLine,
                muted: true,
                alignment: HorizontalAlignment.Center));
            technicalStack.AddChild(MatchScreenUi.BodyLine(
                technicalArea.ScoreFlowLine,
                alignment: HorizontalAlignment.Center));

            var verdict = MatchScreenUi.BodyLine(
                technicalArea.VerdictLine,
                alignment: HorizontalAlignment.Center);
            verdict.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
            technicalStack.AddChild(verdict);
        }

        if (narrative.BeatLines.Count > 0)
        {
            content.AddChild(MatchScreenUi.SectionTitle("MAÇ AKIŞI", "Kritik anlar"));
            var momentsPanel = MatchScreenUi.Card();
            content.AddChild(momentsPanel);
            var moments = MatchScreenUi.VerticalStack(8);
            momentsPanel.AddChild(moments);
            foreach (var beat in narrative.BeatLines)
            {
                moments.AddChild(MatchScreenUi.BodyLine(beat));
            }
        }

        if (narrative.AfterWhistleLines.Count > 0)
        {
            content.AddChild(MatchScreenUi.SectionTitle("DÜDÜK SONRASI", "Saha kenarından"));
            var afterPanel = MatchScreenUi.Card();
            content.AddChild(afterPanel);
            var afterStack = MatchScreenUi.VerticalStack(8);
            afterPanel.AddChild(afterStack);
            foreach (var lineText in narrative.AfterWhistleLines)
            {
                afterStack.AddChild(MatchScreenUi.BodyLine(lineText));
            }
        }

        if (_results.Roundup is { } roundup)
        {
            content.AddChild(MatchScreenUi.SectionTitle("LİG NABZI", roundup.Headline));
            var roundupPanel = MatchScreenUi.Card();
            content.AddChild(roundupPanel);
            var roundupStack = MatchScreenUi.VerticalStack(8);
            roundupPanel.AddChild(roundupStack);
            foreach (var beat in roundup.BeatLines)
            {
                roundupStack.AddChild(MatchScreenUi.BeatLine(beat));
            }
        }

        if (narrative.OtherScorelines.Count > 0)
        {
            content.AddChild(MatchScreenUi.SectionTitle("SKORBORD", "Diğer sonuçlar"));
            var scorelinesPanel = MatchScreenUi.Card();
            content.AddChild(scorelinesPanel);
            var scorelines = MatchScreenUi.VerticalStack(7);
            scorelinesPanel.AddChild(scorelines);
            foreach (var other in narrative.OtherScorelines)
            {
                scorelines.AddChild(MatchScreenUi.BodyLine(other));
            }
        }

        if (_results.DressingRoom is { } dressingRoom)
        {
            content.AddChild(MatchScreenUi.SectionTitle("SOYUNMA ODASI", dressingRoom.BrandTitle));
            var dressingPanel = MatchScreenUi.Card(emphasized: true);
            content.AddChild(dressingPanel);
            var voice = MatchScreenUi.BodyLine(
                dressingRoom.VoiceLine,
                alignment: HorizontalAlignment.Center);
            voice.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
            dressingPanel.AddChild(voice);
        }

        var continueButton = new Button
        {
            Text = "Kariyere Dön",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StylePrimaryButton(continueButton);
        continueButton.Pressed += OnContinuePressed;
        shell.AddChild(continueButton);

        MatchScreenUi.FadeIn(content, this);
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(score, "modulate:a", 1f, 0.4f).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(tone, "modulate:a", 1f, 0.55f).SetDelay(0.12f);
    }

    private static Control BuildReportPanel(MatchReportDigest report)
    {
        var panel = MatchScreenUi.Card();
        var reportStack = MatchScreenUi.VerticalStack(11);
        panel.AddChild(reportStack);

        var clubs = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        clubs.AddThemeConstantOverride("h_separation", 10);
        reportStack.AddChild(clubs);
        clubs.AddChild(ReportCell(report.HomeClubName, HorizontalAlignment.Left, emphasized: true));
        clubs.AddChild(ReportCell(report.AwayClubName, HorizontalAlignment.Right, emphasized: true));

        foreach (var stat in report.StatLines)
        {
            var statPanel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            statPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.PillPanel());
            reportStack.AddChild(statPanel);

            var statStack = MatchScreenUi.VerticalStack(3);
            statPanel.AddChild(statStack);
            statStack.AddChild(ReportCell(stat.Label, HorizontalAlignment.Center, muted: true));

            var values = new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            values.AddThemeConstantOverride("h_separation", 10);
            statStack.AddChild(values);
            values.AddChild(ReportCell(stat.HomeValue, HorizontalAlignment.Left, emphasized: true));
            values.AddChild(ReportCell(stat.AwayValue, HorizontalAlignment.Right, emphasized: true));
        }

        if (!string.IsNullOrWhiteSpace(report.HalfTimeNoteLine))
        {
            reportStack.AddChild(MatchScreenUi.BodyLine(
                report.HalfTimeNoteLine,
                muted: true,
                alignment: HorizontalAlignment.Center));
        }

        if (!string.IsNullOrWhiteSpace(report.StandoutLine))
        {
            var standout = MatchScreenUi.BodyLine(
                report.StandoutLine,
                alignment: HorizontalAlignment.Center);
            standout.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
            reportStack.AddChild(standout);
        }

        if (!string.IsNullOrWhiteSpace(report.InjuryLine))
        {
            var injury = MatchScreenUi.BodyLine(
                report.InjuryLine,
                alignment: HorizontalAlignment.Center);
            injury.AddThemeColorOverride("font_color", CareerUiTheme.DangerSoft);
            reportStack.AddChild(injury);
        }

        return panel;
    }

    private void OnContinuePressed()
    {
        Callable.From(() => ContinueRequested?.Invoke()).CallDeferred();
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
            label.AddThemeFontSizeOverride("font_size", 17);
            label.AddThemeColorOverride("font_color", CareerUiTheme.Ink);
        }

        return label;
    }

    private static void StyleScore(Label label)
    {
        CareerUiTheme.EnsureLoaded();
        CareerUiTheme.StyleBrand(label);
        label.AddThemeFontSizeOverride("font_size", 42);
        label.AddThemeColorOverride("font_color", CareerUiTheme.Ink);
    }
}
