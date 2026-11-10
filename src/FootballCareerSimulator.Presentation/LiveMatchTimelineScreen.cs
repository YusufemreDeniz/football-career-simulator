using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Presents the deterministic match simulation as a spoiler-free, controllable
/// 2D live broadcast arena before the report and final score pages are revealed.
/// </summary>
public sealed partial class LiveMatchTimelineScreen : Control
{
    private readonly PlayMatchesUiResult _results;
    private readonly ProceduralMatchAudioDirector? _audio;
    private readonly List<string> _eventLines = [];

    private LandscapeMatchLayoutProfile _layout = null!;
    private MatchMomentPitchView _pitch = null!;
    private Label _homeClubLabel = null!;
    private Label _awayClubLabel = null!;
    private Label _homeScoreLabel = null!;
    private Label _awayScoreLabel = null!;
    private Label _minuteLabel = null!;
    private Label _phaseLabel = null!;
    private PanelContainer _scoreboardPill = null!;
    private ColorRect _homePossessionRect = null!;
    private ColorRect _awayPossessionRect = null!;
    private VBoxContainer _eventFeedContainer = null!;
    private MobileScrollContainer _eventFeedScroll = null!;
    private Button _playPauseButton = null!;
    private Button _previousButton = null!;
    private Button _nextButton = null!;
    private Button _speedHalfButton = null!;
    private Button _speed1xButton = null!;
    private Button _speed2xButton = null!;
    private Button _fastForwardButton = null!;
    private Button _resultsButton = null!;
    private Label _statsPossessionLabel = null!;
    private Label _statsShotsLabel = null!;
    private float _baseSecondsPerMoment;
    private float _speed = 1f;
    private int _liveHomeGoals;
    private int _liveAwayGoals;
    private string _homeClubName = "Ev Sahibi";
    private string _awayClubName = "Deplasman";
    private bool _hasHalfTimeTriggered;
    private bool _hasHalfTimeEnded;
    private PanelContainer _halfTimeCard = null!;
    private Label _halfTimeScoreLabel = null!;
    private Label _halfTimeAdviceLabel = null!;
    private Button _planSameButton = null!;
    private Button _planAttackButton = null!;
    private Button _planDefendButton = null!;
    private Button _resumeSecondHalfButton = null!;
    private ProgressBar _momentumAuraBar = null!;
    private Label _momentumAuraLabel = null!;
    private int _momentumValue = 62;
    private bool _hasKeyDecisionTriggered;
    private bool _hasKeyDecisionResolved;
    private PanelContainer _keyDecisionCard = null!;
    private Label _keyDecisionPromptLabel = null!;

    public event Action? ResultsRequested;

    public LiveMatchTimelineScreen(
        PlayMatchesUiResult results,
        ProceduralMatchAudioDirector? audio = null)
    {
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _audio = audio;
    }

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();
        var viewport = GetViewportRect().Size;
        _layout = LandscapeMatchLayoutProfile.Resolve(
            Mathf.RoundToInt(viewport.X),
            Mathf.RoundToInt(viewport.Y));

        var (homeName, awayName) = ResolveClubNames();
        _homeClubName = homeName;
        _awayClubName = awayName;

        var margin = MatchScreenUi.CreateStageRoot(
            this,
            new Color(CareerUiTheme.Accent.R, CareerUiTheme.Accent.G, CareerUiTheme.Accent.B, 0.04f),
            _layout.HorizontalMargin,
            _layout.VerticalMargin);

        var shell = MatchScreenUi.VerticalStack(_layout.SectionSeparation);
        shell.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(shell);

        // 1. Top Broadcast Scoreboard Bar
        shell.AddChild(BuildBroadcastScoreboard());

        // 2. Main 2-Column Split (Landscape Bento)
        var arenaBody = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        arenaBody.AddThemeConstantOverride("separation", _layout.SectionSeparation);
        shell.AddChild(arenaBody);

        // Left Column: 2D Pitch View + Playback Control Strip
        arenaBody.AddChild(BuildPitchArena());

        // Right Column: Live Match Desk & Telemetry
        arenaBody.AddChild(BuildTelemetryDesk());

        _pitch.SetMoments(_results.KeyMoments, _results.MatchSequenceSeed, autoplay: true);
        PopulateTelemetryStats();
        RefreshControls();
        UpdateSpeedButtonHighlights();
    }

    private Control BuildBroadcastScoreboard()
    {
        var boardCard = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        boardCard.AddThemeStyleboxOverride("panel", CareerUiTheme.ScoreboardPanel());

        var stack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        stack.AddThemeConstantOverride("separation", 4);
        boardCard.AddChild(stack);

        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 10);
        stack.AddChild(row);

        // Home Team Section (Right-aligned)
        var homeBox = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.End,
        };
        homeBox.AddThemeConstantOverride("separation", 8);
        row.AddChild(homeBox);

        _homeClubLabel = new Label
        {
            Text = _homeClubName.ToUpperInvariant(),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        CareerUiTheme.ApplyDisplayFont(_homeClubLabel);
        _homeClubLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        _homeClubLabel.AddThemeColorOverride("font_color", CareerUiTheme.Ink);
        homeBox.AddChild(_homeClubLabel);

        var homeBadge = new PanelContainer();
        homeBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
        var homeBadgeText = new Label { Text = "EV" };
        CareerUiTheme.StyleEyebrow(homeBadgeText, CareerUiTheme.ActionBright);
        homeBadgeText.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        homeBadge.AddChild(homeBadgeText);
        homeBox.AddChild(homeBadge);

        // Center Scoreboard Core
        var centerBox = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        centerBox.AddThemeConstantOverride("separation", 8);
        row.AddChild(centerBox);

        // Minute Pill
        var minutePanel = new PanelContainer();
        minutePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.18f));
        _minuteLabel = new Label { Text = "⏱ 0'" };
        CareerUiTheme.ApplyDisplayFont(_minuteLabel);
        _minuteLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        _minuteLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        minutePanel.AddChild(_minuteLabel);
        centerBox.AddChild(minutePanel);

        // High-Contrast Score Pill
        _scoreboardPill = new PanelContainer();
        _scoreboardPill.AddThemeStyleboxOverride("panel", CareerUiTheme.ScoreboardPill());
        var scoreRow = new HBoxContainer();
        scoreRow.AddThemeConstantOverride("separation", 8);
        _scoreboardPill.AddChild(scoreRow);

        _homeScoreLabel = new Label { Text = "0" };
        CareerUiTheme.ApplyDisplayFont(_homeScoreLabel);
        _homeScoreLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(24));
        _homeScoreLabel.AddThemeColorOverride("font_color", Colors.White);
        scoreRow.AddChild(_homeScoreLabel);

        var dash = new Label { Text = " : " };
        CareerUiTheme.ApplyDisplayFont(dash);
        dash.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(20));
        dash.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
        scoreRow.AddChild(dash);

        _awayScoreLabel = new Label { Text = "0" };
        CareerUiTheme.ApplyDisplayFont(_awayScoreLabel);
        _awayScoreLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(24));
        _awayScoreLabel.AddThemeColorOverride("font_color", Colors.White);
        scoreRow.AddChild(_awayScoreLabel);
        centerBox.AddChild(_scoreboardPill);

        // Phase Pill (CANLI, DEVRE ARASI vb.)
        var phasePanel = new PanelContainer();
        phasePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Stroke, 0.14f));
        _phaseLabel = new Label { Text = "CANLI · 1.0x" };
        CareerUiTheme.StyleEyebrow(_phaseLabel, CareerUiTheme.ActionBright);
        _phaseLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        phasePanel.AddChild(_phaseLabel);
        centerBox.AddChild(phasePanel);

        // Away Team Section (Left-aligned)
        var awayBox = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Begin,
        };
        awayBox.AddThemeConstantOverride("separation", 8);
        row.AddChild(awayBox);

        var awayBadge = new PanelContainer();
        awayBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data, 0.20f));
        var awayBadgeText = new Label { Text = "DEP" };
        CareerUiTheme.StyleEyebrow(awayBadgeText, CareerUiTheme.Data);
        awayBadgeText.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        awayBadge.AddChild(awayBadgeText);
        awayBox.AddChild(awayBadge);

        _awayClubLabel = new Label
        {
            Text = _awayClubName.ToUpperInvariant(),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        CareerUiTheme.ApplyDisplayFont(_awayClubLabel);
        _awayClubLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        _awayClubLabel.AddThemeColorOverride("font_color", CareerUiTheme.Ink);
        awayBox.AddChild(_awayClubLabel);

        // Momentum / Possession Split Bar
        var momentumRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 3),
        };
        momentumRow.AddThemeConstantOverride("separation", 2);
        stack.AddChild(momentumRow);

        _homePossessionRect = new ColorRect
        {
            Color = CareerUiTheme.ActionBright,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 50,
        };
        momentumRow.AddChild(_homePossessionRect);

        _awayPossessionRect = new ColorRect
        {
            Color = CareerUiTheme.Data,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 50,
        };
        momentumRow.AddChild(_awayPossessionRect);

        return boardCard;
    }

    private Control BuildPitchArena()
    {
        var pitchCard = MatchScreenUi.Card(emphasized: true);
        pitchCard.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        pitchCard.SizeFlagsStretchRatio = 1.9f;
        pitchCard.SizeFlagsVertical = SizeFlags.ExpandFill;

        var stack = MatchScreenUi.VerticalStack(6);
        stack.SizeFlagsVertical = SizeFlags.ExpandFill;
        pitchCard.AddChild(stack);

        _baseSecondsPerMoment = GameExperienceSettingsStore.Current.ReducedMotion ? 2.25f : 1.65f;
        _pitch = new MatchMomentPitchView
        {
            ReducedMotion = GameExperienceSettingsStore.Current.ReducedMotion,
            SecondsPerMoment = _baseSecondsPerMoment,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(_layout.PitchMinimumWidth, _layout.PitchMinimumHeight),
        };
        _pitch.MomentChanged += OnMomentChanged;
        _pitch.SequenceFinished += OnSequenceFinished;
        _pitch.ShouldPauseBeforeMoment = (nextIndex, nextFrame) =>
        {
            if (!_hasKeyDecisionTriggered && nextFrame.Minute >= 20 && nextFrame.Minute <= 42)
            {
                TriggerKeyMomentDecision(nextIndex, nextFrame);
                return true;
            }
            if (!_hasHalfTimeEnded && nextFrame.Minute > 45)
            {
                TriggerHalfTime();
                return true;
            }
            return false;
        };
        stack.AddChild(_pitch);

        // Top Eleven / FM26 tarzı Canlı Kenar Momentum & Aura Barı + Dokunsal Kenar Talimatları (Touchline Shouts)
        var touchlinePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        touchlinePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.16f));
        stack.AddChild(touchlinePanel);

        var touchlineVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        touchlineVBox.AddThemeConstantOverride("separation", 4);
        touchlinePanel.AddChild(touchlineVBox);

        var auraRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        auraRow.AddThemeConstantOverride("separation", 8);
        touchlineVBox.AddChild(auraRow);

        _momentumAuraLabel = new Label { Text = "🔥 KENAR MOMENTUM AURA: %62" };
        CareerUiTheme.StyleTableText(_momentumAuraLabel);
        _momentumAuraLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        _momentumAuraLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        auraRow.AddChild(_momentumAuraLabel);

        _momentumAuraBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = _momentumValue,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 7),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _momentumAuraBar.AddThemeStyleboxOverride("background", CareerUiTheme.ProgressBarBackground());
        _momentumAuraBar.AddThemeStyleboxOverride("fill", CareerUiTheme.ProgressBarFill(CareerUiTheme.Accent));
        auraRow.AddChild(_momentumAuraBar);

        var shoutsRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        shoutsRow.AddThemeConstantOverride("separation", 4);
        touchlineVBox.AddChild(shoutsRow);

        AddTouchlineShoutButton(shoutsRow, "🔥 Şok Pres", "+14% Aura", new Color(1f, 0.45f, 0.22f), 14);
        AddTouchlineShoutButton(shoutsRow, "⚡ Kanat Yükle", "+12% Aura", CareerUiTheme.ActionBright, 12);
        AddTouchlineShoutButton(shoutsRow, "🛡️ Kilit Blok", "+10% Aura", CareerUiTheme.Data, 10);
        AddTouchlineShoutButton(shoutsRow, "🗣️ Moral Ateşi", "+16% Aura", CareerUiTheme.Accent, 16);

        // Playback & Timing Controls Bar
        var controlsBar = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 38),
        };
        controlsBar.AddThemeConstantOverride("separation", 6);
        stack.AddChild(controlsBar);

        _previousButton = AddControlButton(controlsBar, "⏮ Önceki", () =>
        {
            _pitch.Pause();
            _pitch.Retreat();
            RecalculateLiveScore(_pitch.CurrentFrameIndex);
            RefreshControls();
        }, minWidth: 72);

        _playPauseButton = AddControlButton(controlsBar, "⏸ Duraklat", TogglePlayback, minWidth: 88);

        _nextButton = AddControlButton(controlsBar, "Sonraki ⏭", () =>
        {
            _pitch.Pause();
            if (!_hasHalfTimeEnded && _pitch.CurrentFrameIndex + 1 < _pitch.Storyboard.Frames.Count)
            {
                var nextFrame = _pitch.Storyboard.Frames[_pitch.CurrentFrameIndex + 1];
                if (nextFrame.Minute > 45)
                {
                    TriggerHalfTime();
                    return;
                }
            }

            if (!_pitch.Advance()
                || _pitch.CurrentFrameIndex + 1 >= _pitch.Storyboard.Frames.Count)
            {
                OnSequenceFinished();
            }
            RecalculateLiveScore(_pitch.CurrentFrameIndex);
            RefreshControls();
        }, minWidth: 72);

        // Spacer
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        controlsBar.AddChild(spacer);

        // Speed multipliers
        _speedHalfButton = AddControlButton(controlsBar, "0.5x", () => SetSpeed(0.5f), minWidth: 44);
        _speed1xButton = AddControlButton(controlsBar, "1.0x", () => SetSpeed(1f), minWidth: 44);
        _speed2xButton = AddControlButton(controlsBar, "2.0x", () => SetSpeed(2f), minWidth: 44);

        // Fast forward / Skip to end
        _fastForwardButton = AddControlButton(controlsBar, "⏩ Hızlı Bitir", FastForwardToEnd, minWidth: 92);
        CareerUiTheme.StyleAccentButton(_fastForwardButton);

        return pitchCard;
    }

    private Control BuildTelemetryDesk()
    {
        var deskCard = MatchScreenUi.Card();
        deskCard.CustomMinimumSize = new Vector2(_layout.CommandPanelWidth, 0);
        deskCard.SizeFlagsVertical = SizeFlags.ExpandFill;

        var stack = MatchScreenUi.VerticalStack(8);
        stack.SizeFlagsVertical = SizeFlags.ExpandFill;
        deskCard.AddChild(stack);

        // 1. Live Commentary Feed
        var feedHeader = new Label { Text = "📻 CANLI MAÇ ANLATIMI" };
        CareerUiTheme.StyleEyebrow(feedHeader, CareerUiTheme.ActionBright);
        stack.AddChild(feedHeader);

        _eventFeedScroll = MatchScreenUi.ScrollArea();
        _eventFeedScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _eventFeedScroll.SizeFlagsStretchRatio = 1.4f;
        stack.AddChild(_eventFeedScroll);

        _eventFeedContainer = MatchScreenUi.VerticalStack(4);
        _eventFeedScroll.AddChild(_eventFeedContainer);

        // Initial welcome event
        var kickoffChip = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        kickoffChip.AddThemeStyleboxOverride("panel", CareerUiTheme.EventFeedCardPanel("Kickoff"));
        var kickoffLabel = new Label
        {
            Text = "0' ⚽ Karşılaşma hakemin düdüğüyle başladı.",
        };
        CareerUiTheme.StyleBody(kickoffLabel, muted: true);
        kickoffLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        kickoffChip.AddChild(kickoffLabel);
        _eventFeedContainer.AddChild(kickoffChip);

        // 2. In-Match Telemetry
        var statsHeader = new Label { Text = "📊 ANLIK MAÇ İSTATİSTİKLERİ" };
        CareerUiTheme.StyleEyebrow(statsHeader, CareerUiTheme.Data);
        stack.AddChild(statsHeader);

        var statsPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        statsPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized: false));
        stack.AddChild(statsPanel);

        var statsStack = MatchScreenUi.VerticalStack(4);
        statsPanel.AddChild(statsStack);

        _statsPossessionLabel = new Label
        {
            Text = "%50  ·  Topla Oynama  ·  %50",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBody(_statsPossessionLabel);
        _statsPossessionLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        statsStack.AddChild(_statsPossessionLabel);

        _statsShotsLabel = new Label
        {
            Text = "0  ·  Şut (İsabetli)  ·  0",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBody(_statsShotsLabel, muted: true);
        _statsShotsLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        statsStack.AddChild(_statsShotsLabel);

        // 2.5. New Star Manager tarzı Kritik Pozisyon Kararı Kartı (Saha İçi Dondurma)
        _keyDecisionCard = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _keyDecisionCard.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePanel(CareerUiTheme.ActionBright));
        _keyDecisionCard.Visible = false;
        stack.AddChild(_keyDecisionCard);

        var kdStack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        kdStack.AddThemeConstantOverride("separation", 5);
        _keyDecisionCard.AddChild(kdStack);

        var kdTitle = new Label { Text = "🎯 KRİTİK POZİSYON KARARI! (TOP AYAKTA)" };
        CareerUiTheme.StyleEyebrow(kdTitle, CareerUiTheme.ActionBright);
        kdStack.AddChild(kdTitle);

        _keyDecisionPromptLabel = MatchScreenUi.BodyLine(
            "Ceza sahası yayında boşluk yakaladık! Oyuncunun yapacağı hamleyi seç:",
            muted: false);
        _keyDecisionPromptLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        kdStack.AddChild(_keyDecisionPromptLabel);

        var kdChoicesCol = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        kdChoicesCol.AddThemeConstantOverride("separation", 4);
        kdStack.AddChild(kdChoicesCol);

        var shotChoiceBtn = new Button
        {
            Text = "🎯 Köşeye Plase Şut Çek",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 30),
        };
        CareerUiTheme.StylePrimaryButton(shotChoiceBtn);
        shotChoiceBtn.Pressed += () => ResolveKeyMomentDecision("🎯 Uzak köşeye sert plase şut denemesi! Tribünler ayakta!");
        kdChoicesCol.AddChild(shotChoiceBtn);

        var passChoiceBtn = new Button
        {
            Text = "⚡ Arka Direğe Kesme Pas Çiz",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 30),
        };
        CareerUiTheme.StyleAccentButton(passChoiceBtn);
        passChoiceBtn.Pressed += () => ResolveKeyMomentDecision("⚡ Savunma arkasına jilet gibi kesme pas! Tehlike büyüdü!");
        kdChoicesCol.AddChild(passChoiceBtn);

        var dribbleChoiceBtn = new Button
        {
            Text = "🔥 Çalımla Ceza Sahasına Dal",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 30),
        };
        CareerUiTheme.StyleSecondaryButton(dribbleChoiceBtn);
        dribbleChoiceBtn.Pressed += () => ResolveKeyMomentDecision("🔥 Bire birde şık çalımla ceza sahasına dalış! Savunma dengesini kaybetti!");
        kdChoicesCol.AddChild(dribbleChoiceBtn);

        // 3. Half-Time Intermission Card
        _halfTimeCard = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _halfTimeCard.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePanel(CareerUiTheme.Accent));
        _halfTimeCard.Visible = false;
        stack.AddChild(_halfTimeCard);

        var htStack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        htStack.AddThemeConstantOverride("separation", 6);
        _halfTimeCard.AddChild(htStack);

        var htTitle = new Label { Text = "⏸ DEVRE ARASI  •  İLK YARI SONUCU" };
        CareerUiTheme.StyleEyebrow(htTitle, CareerUiTheme.Accent);
        htStack.AddChild(htTitle);

        _halfTimeScoreLabel = new Label
        {
            Text = "0 - 0",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.ApplyDisplayFont(_halfTimeScoreLabel);
        _halfTimeScoreLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(18));
        _halfTimeScoreLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        htStack.AddChild(_halfTimeScoreLabel);

        _halfTimeAdviceLabel = MatchScreenUi.BodyLine("İlk yarı tamamlandı. İkinci yarı yaklaşımını seç.", muted: true);
        _halfTimeAdviceLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _halfTimeAdviceLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        htStack.AddChild(_halfTimeAdviceLabel);

        var plansBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        plansBox.AddThemeConstantOverride("separation", 4);
        htStack.AddChild(plansBox);

        _planSameButton = new Button { Text = "Aynı Plan", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CareerUiTheme.StyleSecondaryButton(_planSameButton);
        _planSameButton.Pressed += () => SetHalfTimeDirective(0);
        plansBox.AddChild(_planSameButton);

        _planAttackButton = new Button { Text = "⚡ Hücum", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CareerUiTheme.StyleSecondaryButton(_planAttackButton);
        _planAttackButton.Pressed += () => SetHalfTimeDirective(1);
        plansBox.AddChild(_planAttackButton);

        _planDefendButton = new Button { Text = "🛡️ Savunma", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CareerUiTheme.StyleSecondaryButton(_planDefendButton);
        _planDefendButton.Pressed += () => SetHalfTimeDirective(2);
        plansBox.AddChild(_planDefendButton);

        _resumeSecondHalfButton = new Button
        {
            Text = "▶ 2. Yarıyı Başlat",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, _layout.ActionButtonHeight),
        };
        CareerUiTheme.StylePrimaryButton(_resumeSecondHalfButton);
        _resumeSecondHalfButton.Pressed += ResumeSecondHalf;
        htStack.AddChild(_resumeSecondHalfButton);

        // 4. Action Footer
        _resultsButton = new Button
        {
            Text = "Maç Devam Ediyor…",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, _layout.ActionButtonHeight),
            Disabled = true,
        };
        CareerUiTheme.StyleSecondaryButton(_resultsButton);
        _resultsButton.Pressed += () => ResultsRequested?.Invoke();
        stack.AddChild(_resultsButton);

        return deskCard;
    }

    private Button AddControlButton(Control parent, string text, Action action, int minWidth = 50)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(minWidth, 34),
        };
        CareerUiTheme.StyleSecondaryButton(button);
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    private void AddTouchlineShoutButton(
        Control parent,
        string label,
        string bonusHint,
        Color color,
        int momentumDelta)
    {
        var btn = new Button
        {
            Text = label,
            TooltipText = $"Kenar talimatı ver ({bonusHint})",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 30),
        };
        CareerUiTheme.StyleSecondaryButton(btn);
        btn.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        btn.Pressed += () =>
        {
            _momentumValue = Math.Clamp(_momentumValue + momentumDelta, 0, 100);
            _momentumAuraBar.Value = _momentumValue;
            _momentumAuraLabel.Text = $"🔥 KENAR MOMENTUM AURA: %{_momentumValue}";
            _pitch.TriggerTouchlineShoutEffect(label, color);
            _audio?.TryPlayCue(MatchAudioCue.Button);

            var haptics = GameExperienceSettingsStore.Current.EffectiveHapticsStrengthPercent;
            if (OS.HasFeature("mobile") && haptics > 0)
            {
                Input.VibrateHandheld(Math.Max(1, 24 * haptics / 100));
            }

            var curMin = _pitch.CurrentFrame?.Minute ?? 15;
            AppendCustomFeedLine($"{curMin}' 🗣️ Kenar Talimatı: {label} uygulandı! (Takım Momentum Aurası: %{_momentumValue})", color);
        };
        parent.AddChild(btn);
    }

    private void AppendCustomFeedLine(string text, Color accent)
    {
        var chip = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        chip.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(accent, 0.22f));
        var lbl = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(lbl);
        lbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        lbl.AddThemeColorOverride("font_color", accent);
        chip.AddChild(lbl);
        _eventFeedContainer.AddChild(chip);

        Callable.From(() =>
        {
            if (IsInstanceValid(_eventFeedScroll))
            {
                _eventFeedScroll.ScrollVertical = 99999;
            }
        }).CallDeferred();
    }

    private void TriggerKeyMomentDecision(int nextIndex, MatchMomentStoryboardFrame nextFrame)
    {
        _hasKeyDecisionTriggered = true;
        _pitch.Pause();
        _pitch.ShowFrame(nextIndex);
        _pitch.SetKeyDecisionHighlight(true, $"🎯 {nextFrame.Minute}' KRİTİK POZİSYON — HAMLENİ SEÇ!");
        _audio?.TryPlayCue(MatchAudioCue.Whistle);

        var haptics = GameExperienceSettingsStore.Current.EffectiveHapticsStrengthPercent;
        if (OS.HasFeature("mobile") && haptics > 0)
        {
            Input.VibrateHandheld(Math.Max(1, 32 * haptics / 100));
        }

        _phaseLabel.Text = "🎯 KARAR ANI";
        _minuteLabel.Text = $"⏱ {nextFrame.Minute}'";
        _keyDecisionPromptLabel.Text =
            $"{nextFrame.Minute}'. dakikada {nextFrame.PrimaryPlayerName} topla tehlikeli bölgede buluştu! Kararını ver:";
        _keyDecisionCard.Visible = true;
        RefreshControls();
    }

    private void ResolveKeyMomentDecision(string outcomeNarrative)
    {
        _hasKeyDecisionResolved = true;
        _keyDecisionCard.Visible = false;
        _pitch.SetKeyDecisionHighlight(false);
        _pitch.TriggerTouchlineShoutEffect("KRİTİK HAMLE!", CareerUiTheme.ActionBright);
        _audio?.TryPlayCue(MatchAudioCue.Goal);

        _momentumValue = Math.Clamp(_momentumValue + 22, 0, 100);
        _momentumAuraBar.Value = _momentumValue;
        _momentumAuraLabel.Text = $"🔥 KENAR MOMENTUM AURA: %{_momentumValue}";

        var curMin = _pitch.CurrentFrame?.Minute ?? 25;
        AppendCustomFeedLine($"{curMin}' {outcomeNarrative}", CareerUiTheme.ActionBright);

        _pitch.Play();
        RefreshControls();
    }

    private void TogglePlayback()
    {
        if (_hasKeyDecisionTriggered && !_hasKeyDecisionResolved && _keyDecisionCard.Visible)
        {
            ResolveKeyMomentDecision("⚡ Hızlı pas kombinasyonuyla pozisyon tamamlandı!");
            return;
        }

        if (_hasHalfTimeTriggered && !_hasHalfTimeEnded)
        {
            ResumeSecondHalf();
            return;
        }

        if (_pitch.IsPlaying)
        {
            _pitch.Pause();
        }
        else if (_pitch.CurrentFrameIndex + 1 >= _pitch.Storyboard.Frames.Count)
        {
            _pitch.Restart(autoplay: true);
            _eventLines.Clear();
            _liveHomeGoals = 0;
            _liveAwayGoals = 0;
            _homeScoreLabel.Text = "0";
            _awayScoreLabel.Text = "0";
            _hasHalfTimeTriggered = false;
            _hasHalfTimeEnded = false;
            _hasKeyDecisionTriggered = false;
            _hasKeyDecisionResolved = false;
            if (_halfTimeCard is not null)
            {
                _halfTimeCard.Visible = false;
            }
            if (_keyDecisionCard is not null)
            {
                _keyDecisionCard.Visible = false;
            }
        }
        else
        {
            _pitch.Play();
        }

        RefreshControls();
    }

    private void SetSpeed(float speed)
    {
        _speed = speed;
        _pitch.SecondsPerMoment = _baseSecondsPerMoment / speed;
        UpdateSpeedButtonHighlights();
        RefreshControls();
    }

    private void UpdateSpeedButtonHighlights()
    {
        ResetButtonStyle(_speedHalfButton, Math.Abs(_speed - 0.5f) < 0.05f);
        ResetButtonStyle(_speed1xButton, Math.Abs(_speed - 1f) < 0.05f);
        ResetButtonStyle(_speed2xButton, Math.Abs(_speed - 2f) < 0.05f);
    }

    private static void ResetButtonStyle(Button button, bool active)
    {
        if (active)
        {
            CareerUiTheme.StyleAccentButton(button);
        }
        else
        {
            CareerUiTheme.StyleSecondaryButton(button);
        }
    }

    private void FastForwardToEnd()
    {
        if (_pitch is null || _pitch.Storyboard.Frames.Count == 0)
        {
            return;
        }

        _hasHalfTimeEnded = true;
        _hasKeyDecisionTriggered = true;
        _hasKeyDecisionResolved = true;
        if (_halfTimeCard is not null)
        {
            _halfTimeCard.Visible = false;
        }
        if (_keyDecisionCard is not null)
        {
            _keyDecisionCard.Visible = false;
        }
        _pitch.SetKeyDecisionHighlight(false);

        _pitch.Pause();
        var last = _pitch.Storyboard.Frames.Count - 1;
        _pitch.ShowFrame(last);

        // Add all events to feed
        for (var i = 0; i < _pitch.Storyboard.Frames.Count; i++)
        {
            AddEventChip(_pitch.Storyboard.Frames[i]);
        }

        RecalculateLiveScore(last);
        OnSequenceFinished();
    }

    private void OnMomentChanged(int index, MatchMomentStoryboardFrame frame)
    {
        _minuteLabel.Text = $"⏱ {frame.Minute}'";
        _phaseLabel.Text = frame.Minute < 45 ? "1. YARI" : frame.Minute == 45 ? "DEVRE" : "2. YARI";

        RecalculateLiveScore(index);
        AddEventChip(frame);

        _audio?.TryPlayMoment(frame.Kind, frame.Minute, frame.PrimarySlotIndex);
        RefreshControls();
    }

    private void AddEventChip(MatchMomentStoryboardFrame frame)
    {
        var icon = frame.Kind switch
        {
            "Goal" => "⚽",
            "YellowCard" => "🟨",
            "RedCard" => "🟥",
            "Injury" => "🩹",
            "Save" => "🧤",
            _ => "⚡",
        };

        var key = $"{frame.Minute}_{frame.PrimaryPlayerName}_{frame.Kind}";
        if (_eventLines.Contains(key, StringComparer.Ordinal))
        {
            return;
        }
        _eventLines.Add(key);

        var chip = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        chip.AddThemeStyleboxOverride("panel", CareerUiTheme.EventFeedCardPanel(frame.Kind));

        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 6);
        chip.AddChild(row);

        var minuteLabel = new Label { Text = $"{frame.Minute}'" };
        CareerUiTheme.ApplyDisplayFont(minuteLabel);
        minuteLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        minuteLabel.AddThemeColorOverride("font_color", frame.Kind == "Goal" ? CareerUiTheme.ActionBright : CareerUiTheme.InkMuted);
        row.AddChild(minuteLabel);

        var iconLabel = new Label { Text = icon };
        row.AddChild(iconLabel);

        var text = frame.AssistPlayerName is { Length: > 0 } assist
            ? $"{frame.PrimaryPlayerName} (Asist: {assist})"
            : $"{frame.PrimaryPlayerName} · {FormatMoment(frame.Kind)}";

        var descLabel = new Label
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(descLabel);
        descLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        if (frame.Kind == "Goal")
        {
            descLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        }
        row.AddChild(descLabel);

        _eventFeedContainer.AddChild(chip);

        Callable.From(() =>
        {
            if (IsInstanceValid(_eventFeedScroll))
            {
                _eventFeedScroll.ScrollVertical = 99999;
            }
        }).CallDeferred();
    }

    private void RecalculateLiveScore(int currentFrameIndex)
    {
        if (_pitch?.Storyboard?.Frames is null)
        {
            return;
        }

        var homeGoals = 0;
        var awayGoals = 0;
        for (var i = 0; i <= currentFrameIndex && i < _pitch.Storyboard.Frames.Count; i++)
        {
            var f = _pitch.Storyboard.Frames[i];
            if (string.Equals(f.Kind, "Goal", StringComparison.OrdinalIgnoreCase))
            {
                if (f.IsHomeSide)
                {
                    homeGoals++;
                }
                else
                {
                    awayGoals++;
                }
            }
        }

        var hadScoreChange = homeGoals != _liveHomeGoals || awayGoals != _liveAwayGoals;
        _liveHomeGoals = homeGoals;
        _liveAwayGoals = awayGoals;
        _homeScoreLabel.Text = _liveHomeGoals.ToString();
        _awayScoreLabel.Text = _liveAwayGoals.ToString();

        if (hadScoreChange && !CareerUiTheme.ReducedMotion)
        {
            _scoreboardPill.Modulate = CareerUiTheme.ActionBright;
            var tween = CreateTween();
            tween.TweenProperty(_scoreboardPill, "modulate", Colors.White, 0.4f);
        }
    }

    private void OnSequenceFinished()
    {
        if (!_hasHalfTimeEnded)
        {
            TriggerHalfTime();
            return;
        }

        _pitch.Pause();
        RecalculateLiveScore(_pitch.Storyboard.Frames.Count - 1);
        _phaseLabel.Text = "MAÇ SONU";
        _minuteLabel.Text = "90+'";
        _resultsButton.Disabled = false;
        _resultsButton.Text = "🏆 Maç Raporunu Aç";
        CareerUiTheme.StylePrimaryButton(_resultsButton);
        _resultsButton.GrabFocus();
        RefreshControls();
    }

    private void RefreshControls()
    {
        if (_pitch is null || _pitch.Storyboard.Frames.Count == 0)
        {
            return;
        }

        if (_hasHalfTimeTriggered && !_hasHalfTimeEnded)
        {
            _playPauseButton.Text = "▶ 2. Yarı";
            _phaseLabel.Text = "⏸️ DEVRE ARASI";
            _minuteLabel.Text = "⏱ 45'";
        }
        else
        {
            _playPauseButton.Text = _pitch.IsPlaying ? "⏸ Duraklat" : "▶ Devam Et";
            if (!_resultsButton.Disabled)
            {
                _phaseLabel.Text = "MAÇ SONU";
            }
            else
            {
                _phaseLabel.Text = $"{(_pitch.IsPlaying ? "CANLI" : "DURAKLATILDI")} · {_speed:0.0}x";
            }
        }

        _previousButton.Disabled = _pitch.CurrentFrameIndex <= 0;
        _nextButton.Disabled = _pitch.CurrentFrameIndex + 1 >= _pitch.Storyboard.Frames.Count;
    }

    private void SetHalfTimeDirective(int directive)
    {
        CareerUiTheme.StyleSecondaryButton(_planSameButton);
        CareerUiTheme.StyleSecondaryButton(_planAttackButton);
        CareerUiTheme.StyleSecondaryButton(_planDefendButton);

        switch (directive)
        {
            case 1:
                CareerUiTheme.StylePrimaryButton(_planAttackButton);
                _planAttackButton.Text = "✓ ⚡ Hücum";
                _planSameButton.Text = "Aynı Plan";
                _planDefendButton.Text = "🛡️ Savunma";
                _halfTimeAdviceLabel.Text = "Taktik direktif: İkinci yarıda tüm hatlarla hücuma çıkılacak!";
                break;
            case 2:
                CareerUiTheme.StylePrimaryButton(_planDefendButton);
                _planDefendButton.Text = "✓ 🛡️ Savunma";
                _planSameButton.Text = "Aynı Plan";
                _planAttackButton.Text = "⚡ Hücum";
                _halfTimeAdviceLabel.Text = "Taktik direktif: İkinci yarıda savunma bloğu kurulacak!";
                break;
            default:
                CareerUiTheme.StylePrimaryButton(_planSameButton);
                _planSameButton.Text = "✓ Aynı Plan";
                _planAttackButton.Text = "⚡ Hücum";
                _planDefendButton.Text = "🛡️ Savunma";
                _halfTimeAdviceLabel.Text = "Taktik direktif: İlk yarıdaki plan korunuyor.";
                break;
        }
    }

    private void TriggerHalfTime()
    {
        if (_hasHalfTimeTriggered)
        {
            return;
        }
        _hasHalfTimeTriggered = true;
        _pitch.Pause();
        _audio?.TryPlayCue(MatchAudioCue.Whistle);
        _phaseLabel.Text = "⏸️ DEVRE ARASI";
        _minuteLabel.Text = "⏱ 45'";

        _halfTimeScoreLabel.Text = $"{_homeClubName} {_liveHomeGoals} - {_liveAwayGoals} {_awayClubName}";

        if (_liveHomeGoals > _liveAwayGoals)
        {
            _halfTimeAdviceLabel.Text = $"{_homeClubName} ilk yarıyı önde kapattı. İkinci yarı yaklaşımını seç.";
        }
        else if (_liveHomeGoals < _liveAwayGoals)
        {
            _halfTimeAdviceLabel.Text = $"{_awayClubName} ilk yarıyı önde kapattı. İkinci yarı yaklaşımını seç.";
        }
        else
        {
            _halfTimeAdviceLabel.Text = "İlk yarıda eşitlik bozulmadı. İkinci yarı yaklaşımını seç.";
        }

        SetHalfTimeDirective(0);

        var htChip = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        htChip.AddThemeStyleboxOverride("panel", CareerUiTheme.EventFeedCardPanel("Kickoff"));
        var htLabel = new Label
        {
            Text = $"45' ⏸️ İlk yarı sona erdi ({_liveHomeGoals} - {_liveAwayGoals}).",
        };
        CareerUiTheme.StyleBody(htLabel, muted: true);
        htLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        htChip.AddChild(htLabel);
        _eventFeedContainer.AddChild(htChip);

        Callable.From(() =>
        {
            if (IsInstanceValid(_eventFeedScroll))
            {
                _eventFeedScroll.ScrollVertical = 99999;
            }
        }).CallDeferred();

        _halfTimeCard.Visible = true;
        RefreshControls();
    }

    private void ResumeSecondHalf()
    {
        _hasHalfTimeEnded = true;
        _halfTimeCard.Visible = false;
        _audio?.TryPlayCue(MatchAudioCue.Whistle);

        var resumeChip = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        resumeChip.AddThemeStyleboxOverride("panel", CareerUiTheme.EventFeedCardPanel("Kickoff"));
        var resumeLabel = new Label
        {
            Text = "45' ⏱️ İkinci yarı hakemin düdüğüyle başladı.",
        };
        CareerUiTheme.StyleBody(resumeLabel, muted: true);
        resumeLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        resumeChip.AddChild(resumeLabel);
        _eventFeedContainer.AddChild(resumeChip);

        Callable.From(() =>
        {
            if (IsInstanceValid(_eventFeedScroll))
            {
                _eventFeedScroll.ScrollVertical = 99999;
            }
        }).CallDeferred();

        _phaseLabel.Text = "2. YARI · CANLI";
        _minuteLabel.Text = "⏱ 46'";

        if (_pitch.CurrentFrameIndex + 1 < _pitch.Storyboard.Frames.Count)
        {
            _pitch.Advance();
            _pitch.Play();
        }
        else
        {
            OnSequenceFinished();
        }

        RefreshControls();
    }

    private void PopulateTelemetryStats()
    {
        if (_results.Report is not { } report)
        {
            return;
        }

        var homePoss = 50;
        var awayPoss = 50;
        var homeShots = "—";
        var awayShots = "—";

        foreach (var stat in report.StatLines)
        {
            if (stat.Label.Contains("Topa", StringComparison.OrdinalIgnoreCase))
            {
                var hStr = stat.HomeValue.Replace("%", "").Trim();
                var aStr = stat.AwayValue.Replace("%", "").Trim();
                if (int.TryParse(hStr, out var hVal) && int.TryParse(aStr, out var aVal))
                {
                    homePoss = hVal;
                    awayPoss = aVal;
                }
            }
            else if (stat.Label.Equals("Şut", StringComparison.OrdinalIgnoreCase))
            {
                homeShots = stat.HomeValue;
                awayShots = stat.AwayValue;
            }
            else if (stat.Label.Contains("İsabetli", StringComparison.OrdinalIgnoreCase))
            {
                homeShots += $" ({stat.HomeValue})";
                awayShots += $" ({stat.AwayValue})";
            }
        }

        _statsPossessionLabel.Text = $"%{homePoss}  ·  Topla Oynama  ·  %{awayPoss}";
        _statsShotsLabel.Text = $"{homeShots}  ·  Şut (İsabetli)  ·  {awayShots}";

        _homePossessionRect.SizeFlagsStretchRatio = Math.Max(10, homePoss);
        _awayPossessionRect.SizeFlagsStretchRatio = Math.Max(10, awayPoss);
    }

    private (string HomeName, string AwayName) ResolveClubNames()
    {
        if (_results.Report is { } report
            && !string.IsNullOrWhiteSpace(report.HomeClubName)
            && !string.IsNullOrWhiteSpace(report.AwayClubName))
        {
            return (report.HomeClubName, report.AwayClubName);
        }

        if (_results.Narrative?.Scoreline is { } scoreline && scoreline.Contains(" - "))
        {
            var parts = scoreline.Split(" - ");
            if (parts.Length == 2)
            {
                var home = System.Text.RegularExpressions.Regex.Replace(parts[0], @"\s*\d+\s*$", "").Trim();
                var away = System.Text.RegularExpressions.Regex.Replace(parts[1], @"^\s*\d+\s*", "").Trim();
                if (!string.IsNullOrWhiteSpace(home) && !string.IsNullOrWhiteSpace(away))
                {
                    return (home, away);
                }
            }
        }

        if (_results.MatchLines.Count > 0)
        {
            var firstLine = _results.MatchLines[0];
            var parts = firstLine.Split(" - ");
            if (parts.Length == 2)
            {
                var home = System.Text.RegularExpressions.Regex.Replace(parts[0], @"\s*\d+\s*$", "").Trim();
                var away = System.Text.RegularExpressions.Regex.Replace(parts[1], @"^\s*\d+\s*", "").Trim();
                if (!string.IsNullOrWhiteSpace(home) && !string.IsNullOrWhiteSpace(away))
                {
                    return (home, away);
                }
            }
        }

        return ("Ev Sahibi", "Deplasman");
    }

    private static string FormatMoment(string kind) => kind switch
    {
        "Goal" => "Gol",
        "YellowCard" => "Sarı kart",
        "RedCard" => "Kırmızı kart",
        "Injury" => "Sakatlık",
        "Save" => "Kurtarış",
        _ => "Kritik an",
    };
}
