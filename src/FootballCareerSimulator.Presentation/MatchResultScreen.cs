using FootballCareerSimulator.Application.Competition.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class MatchResultScreen : Control
{
    private readonly PlayMatchesUiResult _results;
    private readonly MatchNightNarrative _narrative;
    private readonly IReadOnlyList<MatchNightPage> _pages;
    private readonly ProceduralMatchAudioDirector? _audio;

    private int _pageIndex;
    private VBoxContainer _shell = null!;
    private Control? _stageMarker;
    private ScrollContainer _scroll = null!;
    private Button _continueButton = null!;

    public event Action? ContinueRequested;

    public MatchResultScreen(
        PlayMatchesUiResult results,
        ProceduralMatchAudioDirector? audio = null)
    {
        _results = results;
        _audio = audio;
        _narrative = results.Narrative
            ?? MatchNightNarrative.Failure(results.Message);
        _pages = MatchNightPagePlan.Build(
            _narrative,
            hasReport: results.Report is not null,
            hasTechnicalArea: results.TechnicalArea is not null,
            hasRoundup: results.Roundup is not null,
            hasDressingRoom: results.DressingRoom is not null);
    }

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();

        var margin = MatchScreenUi.CreateStageRoot(
            this,
            new Color(CareerUiTheme.Accent.R, CareerUiTheme.Accent.G, CareerUiTheme.Accent.B, 0.045f));

        _shell = MatchScreenUi.VerticalStack(12);
        _shell.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(_shell);

        _scroll = MatchScreenUi.ScrollArea();
        _shell.AddChild(_scroll);

        _continueButton = new Button
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StylePrimaryButton(_continueButton);
        _continueButton.Pressed += OnContinuePressed;
        _shell.AddChild(_continueButton);

        ShowPage(0, animateHero: true);
    }

    private void ShowPage(int index, bool animateHero)
    {
        _pageIndex = Math.Clamp(index, 0, _pages.Count - 1);
        var page = _pages[_pageIndex];

        if (_stageMarker is not null)
        {
            _stageMarker.QueueFree();
            _stageMarker = null;
        }

        _stageMarker = MatchScreenUi.StageMarker(
            $"{page.MarkerCode}  •  {page.MarkerTitle}",
            page.AccentLabel,
            CareerUiTheme.Accent);
        _shell.AddChild(_stageMarker);
        _shell.MoveChild(_stageMarker, 0);

        foreach (var child in _scroll.GetChildren())
        {
            child.QueueFree();
        }

        var content = MatchScreenUi.VerticalStack(15);
        _scroll.AddChild(content);
        BuildPageContent(content, page.Kind, animateHero);

        _continueButton.Text = page.ContinueLabel;
        _scroll.ScrollVertical = 0;
        MatchScreenUi.FadeIn(content, this);
    }

    private void BuildPageContent(VBoxContainer content, MatchNightPageKind kind, bool animateHero)
    {
        switch (kind)
        {
            case MatchNightPageKind.Score:
                BuildScorePage(content, animateHero);
                break;
            case MatchNightPageKind.Match:
                BuildMatchPage(content);
                break;
            case MatchNightPageKind.Aftermath:
                BuildAftermathPage(content);
                break;
        }
    }

    private void BuildScorePage(VBoxContainer content, bool animateHero)
    {
        var hero = MatchScreenUi.Card(emphasized: true);
        content.AddChild(hero);
        var heroContent = MatchScreenUi.VerticalStack(8);
        hero.AddChild(heroContent);

        var brand = new Label
        {
            Text = _narrative.BrandTitle.ToUpperInvariant(),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleEyebrow(brand, CareerUiTheme.Accent);
        heroContent.AddChild(brand);

        var score = new Label
        {
            Text = _narrative.Scoreline,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        StyleScore(score);
        heroContent.AddChild(score);

        var tone = new Label
        {
            Text = _narrative.OutcomeTone,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(tone);
        tone.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(24));
        heroContent.AddChild(tone);

        if (!string.IsNullOrWhiteSpace(_narrative.SupportingLine))
        {
            heroContent.AddChild(MatchScreenUi.BodyLine(
                _narrative.SupportingLine,
                muted: true,
                alignment: HorizontalAlignment.Center));
        }

        if (animateHero && !CareerUiTheme.ReducedMotion)
        {
            score.Modulate = new Color(1f, 1f, 1f, 0f);
            tone.Modulate = new Color(1f, 1f, 1f, 0f);
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(score, "modulate:a", 1f, 0.4f).SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(tone, "modulate:a", 1f, 0.55f).SetDelay(0.12f);
        }

        if (_narrative.Atmosphere is { } stadium)
        {
            content.AddChild(MatchScreenUi.SectionTitle("STADYUM", stadium.Headline));
            var atmospherePanel = MatchScreenUi.Card();
            content.AddChild(atmospherePanel);
            atmospherePanel.AddChild(MatchScreenUi.BodyLine(
                stadium.CrowdLine,
                muted: true,
                alignment: HorizontalAlignment.Center));
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

        if (_results.FormStreakVerdict is { } formVerdict)
        {
            content.AddChild(MatchScreenUi.SectionTitle("FORM", formVerdict.BrandTitle));
            var formPanel = MatchScreenUi.Card(emphasized: true);
            content.AddChild(formPanel);
            var verdict = MatchScreenUi.BodyLine(
                formVerdict.Headline,
                alignment: HorizontalAlignment.Center);
            verdict.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
            formPanel.AddChild(verdict);
        }
    }

    private void BuildMatchPage(VBoxContainer content)
    {
        if (_results.KeyMoments is { Count: > 0 } keyMoments)
        {
            content.AddChild(MatchScreenUi.SectionTitle("2D MAÇ AKIŞI", "Kritik anların sahadaki izi"));
            var pitchPanel = MatchScreenUi.Card(emphasized: true);
            content.AddChild(pitchPanel);
            var pitch = new MatchMomentPitchView
            {
                ReducedMotion = GameExperienceSettingsStore.Current.ReducedMotion,
                SecondsPerMoment = GameExperienceSettingsStore.Current.ReducedMotion ? 2.25f : 1.65f,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            pitch.MomentChanged += (_, frame) =>
                _audio?.TryPlayMoment(frame.Kind, frame.Minute, frame.PrimarySlotIndex);
            pitchPanel.AddChild(pitch);
            pitch.SetMoments(keyMoments, _results.MatchSequenceSeed, autoplay: true);
        }

        if (_narrative.KickoffLines.Count > 0)
        {
            content.AddChild(MatchScreenUi.SectionTitle("BAŞLAMA ANI", "Maça böyle girdin"));
            var kickoffPanel = MatchScreenUi.Card();
            content.AddChild(kickoffPanel);
            var kickoffStack = MatchScreenUi.VerticalStack(8);
            kickoffPanel.AddChild(kickoffStack);
            foreach (var kickoff in _narrative.KickoffLines)
            {
                kickoffStack.AddChild(MatchScreenUi.BeatLine(kickoff, muted: true));
            }
        }

        if (_narrative.LineupBridge is { StartingXi.Count: > 0 } lineup)
        {
            content.AddChild(MatchScreenUi.SectionTitle("KADRO", "Böyle çıktın"));
            content.AddChild(LineupStripUi.BuildPanel(lineup, lineup.ResultBridgeCaption));
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

        if (_results.MatchLines.Count > 0 && _results.Narrative is not null
            && _results.OpponentMatchPlan is { } opponentPlan)
        {
            content.AddChild(MatchScreenUi.SectionTitle("RAKİP KULÜBESİ", "AI maç planı"));
            var opponentPanel = MatchScreenUi.Card();
            content.AddChild(opponentPanel);
            opponentPanel.AddChild(MatchScreenUi.BodyLine(
                $"{opponentPlan.Headline} · maç etkisi {FormatSigned(opponentPlan.MatchStrengthModifier)}",
                muted: true,
                alignment: HorizontalAlignment.Center));
        }

        if (_narrative.BeatLines.Count > 0)
        {
            content.AddChild(MatchScreenUi.SectionTitle("MAÇ AKIŞI", "Kritik anlar"));
            var momentsPanel = MatchScreenUi.Card();
            content.AddChild(momentsPanel);
            var moments = MatchScreenUi.VerticalStack(8);
            momentsPanel.AddChild(moments);
            foreach (var beat in _narrative.BeatLines)
            {
                moments.AddChild(MatchScreenUi.BodyLine(beat));
            }
        }
    }

    private void BuildAftermathPage(VBoxContainer content)
    {
        if (_results.Report is { } report)
        {
            content.AddChild(MatchScreenUi.SectionTitle("VERİ MERKEZİ", "Maç raporu"));
            content.AddChild(BuildReportPanel(report));
        }

        if (_narrative.AfterWhistleLines.Count > 0)
        {
            content.AddChild(MatchScreenUi.SectionTitle("DÜDÜK SONRASI", "Saha kenarından"));
            var afterPanel = MatchScreenUi.Card();
            content.AddChild(afterPanel);
            var afterStack = MatchScreenUi.VerticalStack(8);
            afterPanel.AddChild(afterStack);
            foreach (var lineText in _narrative.AfterWhistleLines)
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

        if (_narrative.OtherScorelines.Count > 0)
        {
            content.AddChild(MatchScreenUi.SectionTitle("SKORBORD", "Diğer sonuçlar"));
            var scorelinesPanel = MatchScreenUi.Card();
            content.AddChild(scorelinesPanel);
            var scorelines = MatchScreenUi.VerticalStack(7);
            scorelinesPanel.AddChild(scorelines);
            foreach (var other in _narrative.OtherScorelines)
            {
                scorelines.AddChild(MatchScreenUi.BodyLine(other));
            }
        }
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
        var page = _pages[_pageIndex];
        if (page.IsFinal)
        {
            Callable.From(() => ContinueRequested?.Invoke()).CallDeferred();
            return;
        }

        ShowPage(_pageIndex + 1, animateHero: false);
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
            label.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(17));
            label.AddThemeColorOverride("font_color", CareerUiTheme.Ink);
        }

        return label;
    }

    private static void StyleScore(Label label)
    {
        CareerUiTheme.EnsureLoaded();
        CareerUiTheme.StyleBrand(label);
        label.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(42));
        label.AddThemeColorOverride("font_color", CareerUiTheme.Ink);
    }

    private static string FormatSigned(int value) => value > 0 ? $"+{value}" : value.ToString();
}
