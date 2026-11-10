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
            CustomMinimumSize = new Vector2(0, 48),
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

        var scoreBox = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        scoreBox.AddThemeStyleboxOverride("panel", CareerUiTheme.ScoreboardPill());
        var score = new Label
        {
            Text = _narrative.Scoreline,
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        StyleScore(score);
        scoreBox.AddChild(score);
        heroContent.AddChild(scoreBox);

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

        BuildBroadcastStudioWrapUp(content, animateHero);
    }

    private void BuildBroadcastStudioWrapUp(VBoxContainer content, bool animateHero)
    {
        content.AddChild(MatchScreenUi.SectionTitle("MAÇ SONU STÜDYO YAYINI", "Broadcast Wrap-Up · MOTM · Puan Kayması"));

        var bentoRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        bentoRow.AddThemeConstantOverride("separation", 10);
        content.AddChild(bentoRow);

        // ==========================================
        // 1. MOTM (Maçın Adamı) Vitrin Kartı
        // ==========================================
        var motmCard = MatchScreenUi.Card(emphasized: true);
        motmCard.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        motmCard.SizeFlagsStretchRatio = 0.95f;
        bentoRow.AddChild(motmCard);

        var motmStack = MatchScreenUi.VerticalStack(6);
        motmCard.AddChild(motmStack);

        var motmEyebrow = new Label { Text = "MAÇIN ADAMI (MOTM)" };
        CareerUiTheme.StyleEyebrow(motmEyebrow, CareerUiTheme.ActionBright);
        motmStack.AddChild(motmEyebrow);

        var motmName = _results.KeyMoments?.LastOrDefault(m => m.Kind == "Goal")?.PrimaryPlayerName
            ?? "Kaptan";
        var motmPlayer = new Label { Text = motmName.ToUpperInvariant() };
        CareerUiTheme.StyleHeadline(motmPlayer);
        motmPlayer.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(17));
        motmPlayer.AddThemeColorOverride("font_color", Colors.White);
        motmStack.AddChild(motmPlayer);

        var ratingRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        ratingRow.AddThemeConstantOverride("separation", 6);
        motmStack.AddChild(ratingRow);

        var ratingBadge = new PanelContainer();
        ratingBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.25f));
        var ratingLabel = new Label { Text = "REYTING: 8.9" };
        CareerUiTheme.StyleTableText(ratingLabel);
        ratingLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        ratingBadge.AddChild(ratingLabel);
        ratingRow.AddChild(ratingBadge);

        var motmDesc = MatchScreenUi.BodyLine(
            _results.Report?.StandoutLine ?? "Sahadaki liderliği ve kritik golüyle maçın kaderini belirledi.",
            muted: true);
        motmStack.AddChild(motmDesc);

        // ==========================================
        // 2. Canlı Puan Durumu Kayması (Standings Delta)
        // ==========================================
        var standingsCard = MatchScreenUi.Card(emphasized: false);
        standingsCard.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        standingsCard.SizeFlagsStretchRatio = 1.05f;
        bentoRow.AddChild(standingsCard);

        var standingsStack = MatchScreenUi.VerticalStack(6);
        standingsCard.AddChild(standingsStack);

        var standingsEyebrow = new Label { Text = "CANLI PUAN DURUMU HAREKETİ" };
        CareerUiTheme.StyleEyebrow(standingsEyebrow, CareerUiTheme.Accent);
        standingsStack.AddChild(standingsEyebrow);

        var deltaRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        deltaRow.AddThemeConstantOverride("separation", 8);
        standingsStack.AddChild(deltaRow);

        var wonMatch = _results.ManagedGoals > _results.OpponentGoals;
        var drawMatch = _results.ManagedGoals == _results.OpponentGoals;
        var ptsText = wonMatch ? "+3 PUAN (KAZANILDI)" : (drawMatch ? "+1 PUAN (BERABERLİK)" : "+0 PUAN");
        var ptsBadge = new PanelContainer();
        ptsBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(wonMatch ? CareerUiTheme.Accent : CareerUiTheme.Data, 0.22f));
        var ptsLabel = new Label { Text = ptsText };
        CareerUiTheme.StyleTableText(ptsLabel);
        ptsLabel.AddThemeColorOverride("font_color", wonMatch ? CareerUiTheme.Accent : CareerUiTheme.Data);
        ptsBadge.AddChild(ptsLabel);
        deltaRow.AddChild(ptsBadge);

        var posMovementLabel = new Label
        {
            Text = wonMatch ? "LİGDE YÜKSELİŞ · +2 SIRA" : "LİGDE KONUM KORUNDU",
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        CareerUiTheme.StyleTableText(posMovementLabel);
        deltaRow.AddChild(posMovementLabel);

        var miniTable = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        miniTable.AddThemeConstantOverride("separation", 3);
        standingsStack.AddChild(miniTable);

        var rows = new[]
        {
            ("1. Galatasaray", "19 P", false),
            ("2. Fenerbahçe", "18 P", false),
            ("3. Kulübümüz", wonMatch ? "17 P" : "15 P", true),
            ("4. Beşiktaş", "16 P", false),
        };
        foreach (var (team, pts, isManaged) in rows)
        {
            var rowPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            rowPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.MiniStandingsRowPanel(isManaged));
            var rowHBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            rowHBox.AddThemeConstantOverride("separation", 8);
            rowPanel.AddChild(rowHBox);

            var tLbl = new Label { Text = team, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            CareerUiTheme.StyleTableText(tLbl);
            if (isManaged) tLbl.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
            rowHBox.AddChild(tLbl);

            var pLbl = new Label { Text = pts };
            CareerUiTheme.StyleTableText(pLbl);
            if (isManaged) pLbl.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
            rowHBox.AddChild(pLbl);

            miniTable.AddChild(rowPanel);
        }

        // ==========================================
        // 3. Sosyal Medya / Taraftar & Yorumcu Akışı
        // ==========================================
        var socialCard = MatchScreenUi.Card(emphasized: false);
        socialCard.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        socialCard.SizeFlagsStretchRatio = 1.10f;
        bentoRow.AddChild(socialCard);

        var socialStack = MatchScreenUi.VerticalStack(6);
        socialCard.AddChild(socialStack);

        var socialEyebrow = new Label { Text = "TARAFTAR & MEDYA NABZI" };
        CareerUiTheme.StyleEyebrow(socialEyebrow, CareerUiTheme.Data);
        socialStack.AddChild(socialEyebrow);

        var feedItems = new[]
        {
            ("@TaraftarSesi", "80' sonrası kenardan gelen şok pres hamlesi maçı kopardı! Harika bir galibiyet!", CareerUiTheme.Accent),
            ("@SporYorumcusu", "Dondurulan kritik anda verilen soğukkanlı karar modern futbol dersi niteliğinde.", CareerUiTheme.ActionBright),
            ("@SoyunmaOdasıMuhabiri", "Takımda moraller zirvede, kaptan ve teknik heyet taraftarla galibiyeti kutluyor.", CareerUiTheme.Data),
        };

        foreach (var (author, text, col) in feedItems)
        {
            var itemPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            itemPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
            var itemVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            itemVBox.AddThemeConstantOverride("separation", 2);
            itemPanel.AddChild(itemVBox);

            var aLbl = new Label { Text = author };
            CareerUiTheme.StyleEyebrow(aLbl, col);
            aLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
            itemVBox.AddChild(aLbl);

            var tLbl = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            CareerUiTheme.StyleBody(tLbl);
            tLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
            itemVBox.AddChild(tLbl);

            socialStack.AddChild(itemPanel);
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
            var standoutCard = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            standoutCard.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.18f));
            var standoutStack = MatchScreenUi.VerticalStack(4);
            standoutCard.AddChild(standoutStack);

            var standoutHeader = new Label
            {
                Text = "MAÇIN OYUNCUSU & KRİTİK KATKI",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            CareerUiTheme.StyleEyebrow(standoutHeader, CareerUiTheme.ActionBright);
            standoutStack.AddChild(standoutHeader);

            var standout = MatchScreenUi.BodyLine(
                report.StandoutLine,
                alignment: HorizontalAlignment.Center);
            standout.AddThemeColorOverride("font_color", CareerUiTheme.TextPrimary);
            standoutStack.AddChild(standout);
            reportStack.AddChild(standoutCard);
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
        CareerUiTheme.ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(42));
        label.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
    }

    private static string FormatSigned(int value) => value > 0 ? $"+{value}" : value.ToString();
}
