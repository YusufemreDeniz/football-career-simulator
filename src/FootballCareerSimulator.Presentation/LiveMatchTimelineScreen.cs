using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// FM27 Broadcast Live Match Arena.
/// Delivers an authentic 2D TV broadcast experience matching FM27 Mobile:
/// Full-stage pitch with stadium rafters, perimeter LED boards, team dugouts,
/// 22-player tokens with directional movement arrows, live clock (08:02),
/// top broadcast scoreboard bar, bottom commentary ticker, and slide-in tactical drawer.
/// </summary>
public sealed partial class LiveMatchTimelineScreen : Control
{
    private readonly PlayMatchesUiResult _results;
    private readonly ProceduralMatchAudioDirector? _audio;
    private readonly List<string> _eventLines = [];

    private LandscapeMatchLayoutProfile _layout = null!;
    private MatchMomentPitchView _pitch = null!;
    private Label _clockLabel = null!;
    private Label _homeClubCodeLabel = null!;
    private Label _awayClubCodeLabel = null!;
    private Label _homeScoreLabel = null!;
    private Label _awayScoreLabel = null!;
    private Label _phaseLabel = null!;
    private PanelContainer _scoreboardPill = null!;
    private ColorRect _homePossessionRect = null!;
    private ColorRect _awayPossessionRect = null!;
    private Button _playPauseActionBtn = null!;
    private Button _speedCycleBtn = null!;
    private Button _menuToggleBtn = null!;

    // Bottom commentary ticker
    private Label _tickerMinuteLabel = null!;
    private Label _tickerCommentaryLabel = null!;
    private Button _quickTacticsBtn = null!;

    // Slide-in Tactical & Telemetry Drawer
    private PanelContainer _telemetryDrawer = null!;
    private VBoxContainer _eventFeedContainer = null!;
    private MobileScrollContainer _eventFeedScroll = null!;
    private ProgressBar _momentumAuraBar = null!;
    private Label _momentumAuraLabel = null!;
    private Label _statsPossessionLabel = null!;
    private Label _statsShotsLabel = null!;
    private PanelContainer _halfTimeCard = null!;
    private Label _halfTimeScoreLabel = null!;
    private Label _halfTimeAdviceLabel = null!;
    private Button _planSameButton = null!;
    private Button _planAttackButton = null!;
    private Button _planDefendButton = null!;
    private Button _resumeSecondHalfButton = null!;
    private Button _resultsButton = null!;
    private Button _fastForwardButton = null!;

    private float _baseSecondsPerMoment;
    private float _speed = 1f;
    private int _liveHomeGoals;
    private int _liveAwayGoals;
    private string _homeClubName = "Crystal Palace";
    private string _awayClubName = "Leeds United";
    private int _momentumValue = 62;
    private bool _hasHalfTimeTriggered;
    private bool _hasHalfTimeEnded;
    private bool _hasKeyDecisionTriggered;
    private bool _hasKeyDecisionResolved;

    public event Action? ResultsRequested;

    public LiveMatchTimelineScreen(
        PlayMatchesUiResult results,
        ProceduralMatchAudioDirector? audio = null)
    {
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _audio = audio;
        if (results.KeyMoments is { Count: > 0 } km && km[0].Minute > 45)
        {
            _hasHalfTimeEnded = true;
        }
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

        var shell = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddThemeConstantOverride("separation", 4);
        margin.AddChild(shell);

        // 1. FM27 Broadcast Scoreboard Bar
        shell.AddChild(BuildBroadcastScoreboard());

        // 2. Full-Stage 2D Pitch Arena (Takes all remaining center space)
        var arenaLayer = new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ClipContents = true,
        };
        shell.AddChild(arenaLayer);

        _baseSecondsPerMoment = GameExperienceSettingsStore.Current.ReducedMotion ? 2.25f : 1.65f;
        _pitch = new MatchMomentPitchView
        {
            ReducedMotion = GameExperienceSettingsStore.Current.ReducedMotion,
            SecondsPerMoment = _baseSecondsPerMoment,
            HomeClubName = _homeClubName,
            AwayClubName = _awayClubName,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
        };
        _pitch.MomentChanged += OnMomentChanged;
        _pitch.SequenceFinished += OnSequenceFinished;
        _pitch.ClockUpdated += OnClockUpdated;
        _pitch.CommentaryGenerated += OnCommentaryGenerated;
        _pitch.ShouldPauseBeforeMoment = (nextIndex, nextFrame) =>
        {
            if (!_hasKeyDecisionTriggered && ((nextFrame.Minute >= 20 && nextFrame.Minute <= 42) || (nextFrame.Minute >= 87 && nextFrame.Minute <= 89)))
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

        var homeIdentity = ClubBrandingVisuals.ResolveClubIdentity(null, _homeClubName);
        var awayIdentity = ClubBrandingVisuals.ResolveClubIdentity(null, _awayClubName);
        _pitch.HomeTeamColor = homeIdentity.PrimaryColor;
        _pitch.HomeSecondaryColor = homeIdentity.SecondaryColor;
        _pitch.AwayTeamColor = awayIdentity.PrimaryColor.IsEqualApprox(homeIdentity.PrimaryColor)
            ? awayIdentity.SecondaryColor
            : awayIdentity.PrimaryColor;
        _pitch.AwaySecondaryColor = awayIdentity.SecondaryColor;

        arenaLayer.AddChild(_pitch);

        // 3. Slide-in Tactical & Telemetry Drawer (Overlaid on arenaLayer)
        BuildSlideInTelemetryDrawer(arenaLayer);

        // 4. Bottom Commentary Ticker Bar
        shell.AddChild(BuildBottomCommentaryTicker());

        _pitch.SetMoments(_results.KeyMoments, _results.MatchSequenceSeed, autoplay: true);
        PopulateTelemetryStats();
        RefreshControls();
    }

    private Control BuildBroadcastScoreboard()
    {
        var boardCard = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        boardCard.AddThemeStyleboxOverride("panel", CareerUiTheme.ScoreboardPanel());

        var stack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        stack.AddThemeConstantOverride("separation", 2);
        boardCard.AddChild(stack);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        row.AddThemeConstantOverride("separation", 8);
        stack.AddChild(row);

        // Left 1: [☰] Hamburger Match Menu / Drawer Toggle
        _menuToggleBtn = new Button
        {
            Text = "☰",
            CustomMinimumSize = new Vector2(38, 32),
            TooltipText = "Taktik ve Maç İstatistikleri Menüsü",
        };
        CareerUiTheme.StyleSecondaryButton(_menuToggleBtn);
        _menuToggleBtn.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        _menuToggleBtn.Pressed += ToggleTelemetryDrawer;
        row.AddChild(_menuToggleBtn);

        // Left 2: Live Clock Pill (08:02)
        var clockPill = new PanelContainer();
        clockPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.20f));
        clockPill.CustomMinimumSize = new Vector2(58, 28);
        _clockLabel = new Label
        {
            Text = "00:00",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.ApplyDisplayFont(_clockLabel);
        _clockLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        _clockLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        clockPill.AddChild(_clockLabel);
        row.AddChild(clockPill);

        // Center Spacer
        var leftSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(leftSpacer);

        // Center: High-Contrast FM27 Scoreboard Pill [CRY] 0 [LOGO] 0 [LEE]
        _scoreboardPill = new PanelContainer();
        _scoreboardPill.AddThemeStyleboxOverride("panel", CareerUiTheme.ScoreboardPill());
        var scoreRow = new HBoxContainer();
        scoreRow.AddThemeConstantOverride("separation", 6);
        _scoreboardPill.AddChild(scoreRow);

        // Home Club Pill [CRY]
        var homeIdentity = ClubBrandingVisuals.ResolveClubIdentity(null, _homeClubName);
        var homeCodePill = new PanelContainer();
        homeCodePill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(homeIdentity.PrimaryColor, 0.40f));
        _homeClubCodeLabel = new Label
        {
            Text = ResolveClubAcronym(_homeClubName),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.ApplyDisplayFont(_homeClubCodeLabel);
        _homeClubCodeLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        _homeClubCodeLabel.AddThemeColorOverride("font_color", Colors.White);
        homeCodePill.AddChild(_homeClubCodeLabel);
        scoreRow.AddChild(homeCodePill);

        scoreRow.AddChild(new ClubCrestControl(homeIdentity, new Vector2(24, 24)));

        // Home Score
        _homeScoreLabel = new Label { Text = "0" };
        CareerUiTheme.ApplyDisplayFont(_homeScoreLabel);
        _homeScoreLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(22));
        _homeScoreLabel.AddThemeColorOverride("font_color", Colors.White);
        scoreRow.AddChild(_homeScoreLabel);

        // League / Match Crest in center
        var dash = new Label { Text = " - " };
        CareerUiTheme.ApplyDisplayFont(dash);
        dash.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(16));
        dash.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
        scoreRow.AddChild(dash);

        // Away Score
        _awayScoreLabel = new Label { Text = "0" };
        CareerUiTheme.ApplyDisplayFont(_awayScoreLabel);
        _awayScoreLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(22));
        _awayScoreLabel.AddThemeColorOverride("font_color", Colors.White);
        scoreRow.AddChild(_awayScoreLabel);

        var awayIdentity = ClubBrandingVisuals.ResolveClubIdentity(null, _awayClubName);
        scoreRow.AddChild(new ClubCrestControl(awayIdentity, new Vector2(24, 24)));

        // Away Club Pill [LEE]
        var awayCodePill = new PanelContainer();
        awayCodePill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(awayIdentity.PrimaryColor, 0.40f));
        _awayClubCodeLabel = new Label
        {
            Text = ResolveClubAcronym(_awayClubName),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.ApplyDisplayFont(_awayClubCodeLabel);
        _awayClubCodeLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        _awayClubCodeLabel.AddThemeColorOverride("font_color", Colors.White);
        awayCodePill.AddChild(_awayClubCodeLabel);
        scoreRow.AddChild(awayCodePill);

        row.AddChild(_scoreboardPill);

        // Right Spacer
        var rightSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(rightSpacer);

        // Phase Pill
        var phasePanel = new PanelContainer();
        phasePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Stroke, 0.16f));
        _phaseLabel = new Label { Text = "CANLI" };
        CareerUiTheme.StyleEyebrow(_phaseLabel, CareerUiTheme.ActionBright);
        _phaseLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        phasePanel.AddChild(_phaseLabel);
        row.AddChild(phasePanel);

        // Quick Speed Settings [⚙]
        _speedCycleBtn = new Button
        {
            Text = "1.0x",
            CustomMinimumSize = new Vector2(46, 32),
            TooltipText = "Yayın Hızı (0.5x, 1.0x, 2.0x)",
        };
        CareerUiTheme.StyleSecondaryButton(_speedCycleBtn);
        _speedCycleBtn.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _speedCycleBtn.Pressed += CycleSpeed;
        row.AddChild(_speedCycleBtn);

        // Far Right: Primary Broadcast Action Button [DURDUR] / [DEVAM] / [SONUÇLAR]
        _playPauseActionBtn = new Button
        {
            Text = "Durdur",
            CustomMinimumSize = new Vector2(86, 32),
        };
        CareerUiTheme.StylePrimaryButton(_playPauseActionBtn);
        _playPauseActionBtn.AddThemeColorOverride("font_color", Colors.White);
        _playPauseActionBtn.Pressed += TogglePlayback;
        row.AddChild(_playPauseActionBtn);

        // Momentum / Possession Split Bar underneath
        var momentumRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 3),
        };
        momentumRow.AddThemeConstantOverride("separation", 2);
        stack.AddChild(momentumRow);

        _homePossessionRect = new ColorRect
        {
            Color = homeIdentity.PrimaryColor,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 50,
        };
        momentumRow.AddChild(_homePossessionRect);

        _awayPossessionRect = new ColorRect
        {
            Color = awayIdentity.PrimaryColor,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 50,
        };
        momentumRow.AddChild(_awayPossessionRect);

        return boardCard;
    }

    private Control BuildBottomCommentaryTicker()
    {
        var tickerCard = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 34),
        };
        tickerCard.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Surface, 0.90f));

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        row.AddThemeConstantOverride("separation", 8);
        tickerCard.AddChild(row);

        // Minute pill
        var minPill = new PanelContainer();
        minPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.25f));
        _tickerMinuteLabel = new Label { Text = "00'" };
        CareerUiTheme.ApplyDisplayFont(_tickerMinuteLabel);
        _tickerMinuteLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _tickerMinuteLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        minPill.AddChild(_tickerMinuteLabel);
        row.AddChild(minPill);

        // Commentary text
        _tickerCommentaryLabel = new Label
        {
            Text = "Karşılaşma hakemin başlama düdüğüyle başladı. İki takıma da başarılar!",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        CareerUiTheme.StyleBody(_tickerCommentaryLabel);
        _tickerCommentaryLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        _tickerCommentaryLabel.AddThemeColorOverride("font_color", Colors.White);
        row.AddChild(_tickerCommentaryLabel);

        // Quick tactics button
        _quickTacticsBtn = new Button
        {
            Text = "Taktikler",
            CustomMinimumSize = new Vector2(80, 26),
        };
        CareerUiTheme.StyleSecondaryButton(_quickTacticsBtn);
        _quickTacticsBtn.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _quickTacticsBtn.Pressed += ToggleTelemetryDrawer;
        row.AddChild(_quickTacticsBtn);

        return tickerCard;
    }

    private void BuildSlideInTelemetryDrawer(Control parent)
    {
        _telemetryDrawer = new PanelContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(310, 0),
        };
        _telemetryDrawer.SetAnchor(Side.Left, 1f);
        _telemetryDrawer.SetAnchor(Side.Top, 0f);
        _telemetryDrawer.SetAnchor(Side.Right, 1f);
        _telemetryDrawer.SetAnchor(Side.Bottom, 1f);
        _telemetryDrawer.OffsetLeft = -320;
        _telemetryDrawer.OffsetTop = 6;
        _telemetryDrawer.OffsetRight = -6;
        _telemetryDrawer.OffsetBottom = -6;
        _telemetryDrawer.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized: true));
        parent.AddChild(_telemetryDrawer);

        var stack = MatchScreenUi.VerticalStack(6);
        stack.SizeFlagsVertical = SizeFlags.ExpandFill;
        _telemetryDrawer.AddChild(stack);

        // Header + Close button
        var drawerHeader = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        stack.AddChild(drawerHeader);

        var title = new Label
        {
            Text = "MAÇ TAKTİKLERİ & TELEMETRİ",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleEyebrow(title, CareerUiTheme.ActionBright);
        drawerHeader.AddChild(title);

        var closeBtn = new Button
        {
            Text = "Kapat",
            CustomMinimumSize = new Vector2(60, 26),
        };
        CareerUiTheme.StyleSecondaryButton(closeBtn);
        closeBtn.Pressed += () => _telemetryDrawer.Visible = false;
        drawerHeader.AddChild(closeBtn);

        // Touchline Shouts (Dokunsal Kenar Talimatları)
        var shoutsHeader = new Label { Text = "KENAR TALİMATLARI (MOMENTUM AURA)" };
        CareerUiTheme.StyleEyebrow(shoutsHeader, CareerUiTheme.Accent);
        stack.AddChild(shoutsHeader);

        var shoutsGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        shoutsGrid.AddThemeConstantOverride("h_separation", 4);
        shoutsGrid.AddThemeConstantOverride("v_separation", 4);
        stack.AddChild(shoutsGrid);

        AddDrawerShoutButton(shoutsGrid, "Şok Pres", "+14% Aura", new Color(1f, 0.45f, 0.22f), 14);
        AddDrawerShoutButton(shoutsGrid, "Kanat Yükle", "+12% Aura", CareerUiTheme.ActionBright, 12);
        AddDrawerShoutButton(shoutsGrid, "Kilit Blok", "+10% Aura", CareerUiTheme.Data, 10);
        AddDrawerShoutButton(shoutsGrid, "Moral Ateşi", "+16% Aura", CareerUiTheme.Accent, 16);

        _momentumAuraBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = _momentumValue,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 6),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _momentumAuraBar.AddThemeStyleboxOverride("background", CareerUiTheme.ProgressBarBackground());
        _momentumAuraBar.AddThemeStyleboxOverride("fill", CareerUiTheme.ProgressBarFill(CareerUiTheme.Accent));
        stack.AddChild(_momentumAuraBar);

        _momentumAuraLabel = new Label { Text = $"KENAR MOMENTUM AURA: %{_momentumValue}" };
        CareerUiTheme.StyleTableText(_momentumAuraLabel);
        _momentumAuraLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        _momentumAuraLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        stack.AddChild(_momentumAuraLabel);

        // Telemetry Stats
        var statsHeader = new Label { Text = "ANLIK İSTATİSTİKLER" };
        CareerUiTheme.StyleEyebrow(statsHeader, CareerUiTheme.Data);
        stack.AddChild(statsHeader);

        _statsPossessionLabel = new Label
        {
            Text = "%50  ·  Topla Oynama  ·  %50",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBody(_statsPossessionLabel);
        _statsPossessionLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        stack.AddChild(_statsPossessionLabel);

        _statsShotsLabel = new Label
        {
            Text = "0  ·  Şut (İsabetli)  ·  0",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBody(_statsShotsLabel, muted: true);
        _statsShotsLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        stack.AddChild(_statsShotsLabel);

        // Half-Time Intermission Card
        _halfTimeCard = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Visible = false };
        _halfTimeCard.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePanel(CareerUiTheme.Accent));
        stack.AddChild(_halfTimeCard);

        var htStack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        htStack.AddThemeConstantOverride("separation", 4);
        _halfTimeCard.AddChild(htStack);

        var htTitle = new Label { Text = "DEVRE ARASI TAKTİK DİREKTİFİ" };
        CareerUiTheme.StyleEyebrow(htTitle, CareerUiTheme.Accent);
        htStack.AddChild(htTitle);

        _halfTimeScoreLabel = new Label { Text = "0 - 0", HorizontalAlignment = HorizontalAlignment.Center };
        CareerUiTheme.ApplyDisplayFont(_halfTimeScoreLabel);
        _halfTimeScoreLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(16));
        _halfTimeScoreLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        htStack.AddChild(_halfTimeScoreLabel);

        _halfTimeAdviceLabel = MatchScreenUi.BodyLine("İkinci yarı yaklaşımını seç.", muted: true);
        _halfTimeAdviceLabel.HorizontalAlignment = HorizontalAlignment.Center;
        htStack.AddChild(_halfTimeAdviceLabel);

        var plansBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        plansBox.AddThemeConstantOverride("separation", 4);
        htStack.AddChild(plansBox);

        _planSameButton = new Button { Text = "Aynı Plan", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CareerUiTheme.StyleSecondaryButton(_planSameButton);
        _planSameButton.Pressed += () => SetHalfTimeDirective(0);
        plansBox.AddChild(_planSameButton);

        _planAttackButton = new Button { Text = "Hücum", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CareerUiTheme.StyleSecondaryButton(_planAttackButton);
        _planAttackButton.Pressed += () => SetHalfTimeDirective(1);
        plansBox.AddChild(_planAttackButton);

        _planDefendButton = new Button { Text = "Savunma", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CareerUiTheme.StyleSecondaryButton(_planDefendButton);
        _planDefendButton.Pressed += () => SetHalfTimeDirective(2);
        plansBox.AddChild(_planDefendButton);

        _resumeSecondHalfButton = new Button
        {
            Text = "2. Yarıyı Başlat",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 32),
        };
        CareerUiTheme.StylePrimaryButton(_resumeSecondHalfButton);
        _resumeSecondHalfButton.Pressed += ResumeSecondHalf;
        htStack.AddChild(_resumeSecondHalfButton);

        // Scrollable Event Feed
        var feedHeader = new Label { Text = "MAÇ OLAYLARI AKIŞI" };
        CareerUiTheme.StyleEyebrow(feedHeader, CareerUiTheme.ActionBright);
        stack.AddChild(feedHeader);

        _eventFeedScroll = MatchScreenUi.ScrollArea();
        _eventFeedScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        stack.AddChild(_eventFeedScroll);

        _eventFeedContainer = MatchScreenUi.VerticalStack(3);
        _eventFeedScroll.AddChild(_eventFeedContainer);

        // Footer Actions
        var drawerFooter = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        drawerFooter.AddThemeConstantOverride("separation", 6);
        stack.AddChild(drawerFooter);

        _fastForwardButton = new Button
        {
            Text = "Hızlı Bitir",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 32),
        };
        CareerUiTheme.StyleSecondaryButton(_fastForwardButton);
        _fastForwardButton.Pressed += FastForwardToEnd;
        drawerFooter.AddChild(_fastForwardButton);

        _resultsButton = new Button
        {
            Text = "Maç Sonu Raporu",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 32),
            Disabled = true,
        };
        CareerUiTheme.StylePrimaryButton(_resultsButton);
        _resultsButton.Pressed += () => ResultsRequested?.Invoke();
        drawerFooter.AddChild(_resultsButton);
    }

    private void AddDrawerShoutButton(Control parent, string label, string hint, Color color, int delta)
    {
        var btn = new Button
        {
            Text = label,
            TooltipText = $"Kenar talimatı ({hint})",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 28),
        };
        CareerUiTheme.StyleSecondaryButton(btn);
        btn.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        btn.Pressed += () =>
        {
            _momentumValue = Math.Clamp(_momentumValue + delta, 0, 100);
            _momentumAuraBar.Value = _momentumValue;
            _momentumAuraLabel.Text = $"KENAR MOMENTUM AURA: %{_momentumValue}";
            _pitch.TriggerTouchlineShoutEffect(label, color);
            _audio?.TryPlayCue(MatchAudioCue.Button);

            var haptics = GameExperienceSettingsStore.Current.EffectiveHapticsStrengthPercent;
            if (OS.HasFeature("mobile") && haptics > 0)
            {
                Input.VibrateHandheld(Math.Max(1, 24 * haptics / 100));
            }

            var curMin = _pitch.CurrentFrame?.Minute ?? _pitch.CurrentMatchMinute;
            AppendCustomFeedLine($"{curMin}' Kenar Talimatı: {label} uygulandı!", color);
        };
        parent.AddChild(btn);
    }

    private void ToggleTelemetryDrawer()
    {
        _telemetryDrawer.Visible = !_telemetryDrawer.Visible;
    }

    private void CycleSpeed()
    {
        if (Math.Abs(_speed - 1.0f) < 0.05f)
        {
            SetSpeed(2.0f);
        }
        else if (Math.Abs(_speed - 2.0f) < 0.05f)
        {
            SetSpeed(0.5f);
        }
        else
        {
            SetSpeed(1.0f);
        }
    }

    private void SetSpeed(float speed)
    {
        _speed = speed;
        _pitch.SecondsPerMoment = _baseSecondsPerMoment / speed;
        _speedCycleBtn.Text = $"{_speed:0.0}x";
        RefreshControls();
    }

    private void OnClockUpdated(int minute, int second)
    {
        _clockLabel.Text = $"{minute:00}:{second:00}";
        _tickerMinuteLabel.Text = $"{minute:00}'";
    }

    private void OnCommentaryGenerated(string commentaryText)
    {
        _tickerCommentaryLabel.Text = commentaryText;
    }

    private void OnMomentChanged(int index, MatchMomentStoryboardFrame frame)
    {
        _phaseLabel.Text = frame.Minute < 45 ? "1. YARI" : frame.Minute == 45 ? "DEVRE" : "2. YARI";
        RecalculateLiveScore(index);
        AddEventChip(frame);

        if (string.Equals(frame.Kind, "Goal", StringComparison.OrdinalIgnoreCase))
        {
            CareerUiTheme.ShakeControl(_pitch, intensity: 7f, duration: 0.28f);
            CareerUiTheme.PulseScale(_scoreboardPill, peakScale: 1.12f, duration: 0.24f);
        }

        _audio?.TryPlayMoment(frame.Kind, frame.Minute, frame.PrimarySlotIndex);
        RefreshControls();
    }

    private void AddEventChip(MatchMomentStoryboardFrame frame)
    {
        var (iconKey, iconColor) = frame.Kind switch
        {
            "Goal" => ("ball", CareerUiTheme.ActionBright),
            "YellowCard" => ("cards", CareerUiTheme.Accent),
            "RedCard" => ("cards", CareerUiTheme.DangerSoft),
            "Injury" => ("medical", CareerUiTheme.DangerSoft),
            "Save" => ("shield", CareerUiTheme.Data),
            _ => ("bolt", CareerUiTheme.InkMuted),
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

        row.AddChild(new BroadcastVectorIcon(iconKey, iconColor, 14f));

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
        descLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
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
        _pitch.ActivateOnPitchKeyDecision(
            nextFrame,
            ResolveKeyMomentDecision,
            $"{nextFrame.Minute}' KRİTİK POZİSYON — SAHA ÜSTÜ HEDEF SEÇ");
        _audio?.TryPlayCue(MatchAudioCue.KeyMomentFreeze);

        var haptics = GameExperienceSettingsStore.Current.EffectiveHapticsStrengthPercent;
        if (OS.HasFeature("mobile") && haptics > 0)
        {
            Input.VibrateHandheld(Math.Max(1, 32 * haptics / 100));
        }

        _phaseLabel.Text = "KARAR ANI";
        RefreshControls();
    }

    private void ResolveKeyMomentDecision(string outcomeNarrative)
    {
        _hasKeyDecisionResolved = true;
        _pitch.SetKeyDecisionHighlight(false);
        _pitch.TriggerTouchlineShoutEffect("KRİTİK HAMLE!", CareerUiTheme.ActionBright);
        _audio?.TryPlayCue(MatchAudioCue.Goal);

        _momentumValue = Math.Clamp(_momentumValue + 22, 0, 100);
        _momentumAuraBar.Value = _momentumValue;
        _momentumAuraLabel.Text = $"KENAR MOMENTUM AURA: %{_momentumValue}";

        var curMin = _pitch.CurrentFrame?.Minute ?? 25;
        AppendCustomFeedLine($"{curMin}' {outcomeNarrative}", CareerUiTheme.ActionBright);

        _pitch.Play();
        RefreshControls();
    }

    private void TogglePlayback()
    {
        if (_hasKeyDecisionTriggered && !_hasKeyDecisionResolved)
        {
            ResolveKeyMomentDecision("Hızlı pas kombinasyonuyla pozisyon tamamlandı!");
            return;
        }

        if (_hasHalfTimeTriggered && !_hasHalfTimeEnded)
        {
            ResumeSecondHalf();
            return;
        }

        if (_resultsButton.Disabled == false)
        {
            ResultsRequested?.Invoke();
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
            _halfTimeCard.Visible = false;
            _resultsButton.Disabled = true;
        }
        else
        {
            _pitch.Play();
        }

        RefreshControls();
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
        _clockLabel.Text = "90:00";
        _resultsButton.Disabled = false;
        _playPauseActionBtn.Text = "Maç Raporu";
        CareerUiTheme.StylePrimaryButton(_playPauseActionBtn);
        RefreshControls();
    }

    private void RefreshControls()
    {
        if (_pitch is null)
        {
            return;
        }

        if (!_resultsButton.Disabled)
        {
            _playPauseActionBtn.Text = "Maç Raporu";
            _phaseLabel.Text = "MAÇ SONU";
        }
        else if (_hasHalfTimeTriggered && !_hasHalfTimeEnded)
        {
            _playPauseActionBtn.Text = "2. Yarı";
            _phaseLabel.Text = "DEVRE ARASI";
        }
        else
        {
            _playPauseActionBtn.Text = _pitch.IsPlaying ? "Durdur" : "Devam";
            _phaseLabel.Text = _pitch.IsPlaying ? "CANLI" : "DURDURULDU";
        }
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
                _planAttackButton.Text = "Hücum (Aktif)";
                _planSameButton.Text = "Aynı Plan";
                _planDefendButton.Text = "Savunma";
                _halfTimeAdviceLabel.Text = "Taktik direktif: İkinci yarıda tüm hatlarla hücuma çıkılacak!";
                break;
            case 2:
                CareerUiTheme.StylePrimaryButton(_planDefendButton);
                _planDefendButton.Text = "Savunma (Aktif)";
                _planSameButton.Text = "Aynı Plan";
                _planAttackButton.Text = "Hücum";
                _halfTimeAdviceLabel.Text = "Taktik direktif: İkinci yarıda savunma bloğu kurulacak!";
                break;
            default:
                CareerUiTheme.StylePrimaryButton(_planSameButton);
                _planSameButton.Text = "Aynı Plan (Aktif)";
                _planAttackButton.Text = "Hücum";
                _planDefendButton.Text = "Savunma";
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
        _phaseLabel.Text = "DEVRE ARASI";
        _clockLabel.Text = "45:00";

        _halfTimeScoreLabel.Text = $"{_homeClubName} {_liveHomeGoals} - {_liveAwayGoals} {_awayClubName}";
        _halfTimeCard.Visible = true;
        _telemetryDrawer.Visible = true;
        RefreshControls();
    }

    private void ResumeSecondHalf()
    {
        _hasHalfTimeEnded = true;
        _halfTimeCard.Visible = false;
        _telemetryDrawer.Visible = false;
        _audio?.TryPlayCue(MatchAudioCue.Whistle);

        _phaseLabel.Text = "2. YARI · CANLI";
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

    private void FastForwardToEnd()
    {
        if (_pitch is null || _pitch.Storyboard.Frames.Count == 0)
        {
            return;
        }

        _hasHalfTimeEnded = true;
        _hasKeyDecisionTriggered = true;
        _hasKeyDecisionResolved = true;
        _halfTimeCard.Visible = false;
        _pitch.SetKeyDecisionHighlight(false);

        _pitch.Pause();
        var last = _pitch.Storyboard.Frames.Count - 1;
        _pitch.ShowFrame(last);

        for (var i = 0; i < _pitch.Storyboard.Frames.Count; i++)
        {
            AddEventChip(_pitch.Storyboard.Frames[i]);
        }

        RecalculateLiveScore(last);
        OnSequenceFinished();
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

    private static string ResolveClubAcronym(string clubName)
    {
        if (string.IsNullOrWhiteSpace(clubName))
        {
            return "KUL";
        }

        var upper = clubName.ToUpperInvariant();
        if (upper.Contains("CRYSTAL") || upper.Contains("PALACE")) return "CRY";
        if (upper.Contains("LEEDS")) return "LEE";
        if (upper.Contains("LIVERPOOL")) return "LIV";
        if (upper.Contains("MANCHESTER CITY") || upper.Contains("MAN CITY")) return "MCI";
        if (upper.Contains("MANCHESTER UNITED") || upper.Contains("MAN UTD")) return "MUN";
        if (upper.Contains("ARSENAL")) return "ARS";
        if (upper.Contains("CHELSEA")) return "CHE";
        if (upper.Contains("BRIGHTON")) return "BHA";
        if (upper.Contains("NOTTINGHAM") || upper.Contains("FOREST")) return "NFO";
        if (upper.Contains("TOTTENHAM") || upper.Contains("SPURS")) return "TOT";
        if (upper.Contains("GALATASARAY")) return "GS";
        if (upper.Contains("FENERBAHCE") || upper.Contains("FENERBAHÇE")) return "FB";
        if (upper.Contains("BESIKTAS") || upper.Contains("BEŞİKTAŞ")) return "BJK";
        if (upper.Contains("TRABZONSPOR")) return "TS";
        if (upper.Contains("REAL MADRID")) return "RMA";
        if (upper.Contains("BARCELONA")) return "BAR";
        if (upper.Contains("BAYERN")) return "BAY";

        var words = clubName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
        {
            return $"{words[0][0]}{words[1][0]}".ToUpperInvariant();
        }

        return clubName.Length >= 3 ? clubName[..3].ToUpperInvariant() : clubName.ToUpperInvariant();
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
