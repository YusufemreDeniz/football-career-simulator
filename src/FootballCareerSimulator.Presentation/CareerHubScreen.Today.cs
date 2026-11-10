using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;
using static FootballCareerSimulator.Presentation.CareerHubCopyFormatting;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private HBoxContainer _portalCalendarRow = null!;
    private VBoxContainer _dailyObjectivesBox = null!;
    private Label _dailyObjectivesProgressBadge = null!;
    private VBoxContainer _portalStreamBox = null!;
    private GridContainer _tactileDeckGrid = null!;
    private string _activePortalFilter = MobilePortalAndDeckDigest.FilterAll;
    private Button _portalFilterAllBtn = null!;
    private Button _portalFilterUrgentBtn = null!;
    private Button _portalFilterTasksBtn = null!;
    private Button _portalFilterAdviceBtn = null!;

    private Control BuildTodayPage()
    {
        var page = PageRoot();
        page.SizeFlagsVertical = SizeFlags.ExpandFill;

        // ====================================================
        // TOP STRIP: 7-DAY VISUAL CALENDAR CAROUSEL (FM26 / SM27 PORTAL)
        // ====================================================
        var calendarStripPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        calendarStripPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        page.AddChild(calendarStripPanel);

        _portalCalendarRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _portalCalendarRow.AddThemeConstantOverride("separation", 6);
        calendarStripPanel.AddChild(_portalCalendarRow);

        // ====================================================
        // 3-COLUMN PORTAL & TACTILE CARD DECK (FM26 + SM27 + NEW STAR MANAGER)
        // Col 1: Sıradaki Maç Showdown + Günlük Hedefler (5 Dk Oturum Döngüsü)
        // Col 2: Portal Merkezi (Zorunlu / Görev / Teknik Ekip Karoları) + Karar Masası
        // Col 3: Dokunsal Menajer Kart Destesi (4 Kart) + Canlı Süper Lig Tablosu
        // ====================================================
        var bentoRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        bentoRow.AddThemeConstantOverride("separation", 10);
        page.AddChild(bentoRow);

        // ====================================================
        // COLUMN 1: NEXT MATCH HERO + DAILY OBJECTIVES (SM27 5-MINUTE LOOP)
        // ====================================================
        var col1 = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.25f,
        };
        col1.AddThemeConstantOverride("separation", 8);
        bentoRow.AddChild(col1);

        var matchNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        matchNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        col1.AddChild(matchNeonPanel);

        var matchCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        matchCard.AddThemeConstantOverride("separation", 5);
        matchNeonPanel.AddChild(matchCard);

        // Top meta row: Header + Venue + Approval Pill
        var matchMetaRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        matchMetaRow.AddThemeConstantOverride("separation", 6);
        matchCard.AddChild(matchMetaRow);

        _nextMatchCompetitionBadge = BodyLabel("NextMatchCompetitionBadge");
        _nextMatchCompetitionBadge.Text = "🏆 LİG MAÇI";
        _nextMatchCompetitionBadge.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        CareerUiTheme.ApplyDisplayFont(_nextMatchCompetitionBadge);
        matchMetaRow.AddChild(_nextMatchCompetitionBadge);

        _nextMatchVenuePill = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _nextMatchVenuePill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data));
        _nextMatchVenueLabel = BodyLabel("NextMatchVenueLabel");
        _nextMatchVenueLabel.Text = "İÇ SAHA";
        _nextMatchVenueLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _nextMatchVenuePill.AddChild(_nextMatchVenueLabel);
        matchMetaRow.AddChild(_nextMatchVenuePill);

        var metaSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        matchMetaRow.AddChild(metaSpacer);

        _nextMatchApprovalPill = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _nextMatchApprovalPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning));
        _nextMatchApprovalLabel = BodyLabel("NextMatchApprovalLabel");
        _nextMatchApprovalLabel.Text = "⚠️ Kadro Onayı";
        _nextMatchApprovalLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _nextMatchApprovalLabel.AddThemeColorOverride("font_color", CareerUiTheme.ColorWarning);
        _nextMatchApprovalPill.AddChild(_nextMatchApprovalLabel);
        matchMetaRow.AddChild(_nextMatchApprovalPill);

        // Opponent Hero Display
        _nextMatchOpponentLabel = TitleLabel("NextMatchOpponentLabel");
        _nextMatchOpponentLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _nextMatchOpponentLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(18));
        _nextMatchOpponentLabel.Text = "vs —";
        matchCard.AddChild(_nextMatchOpponentLabel);

        _briefingLabel = BodyLabel("BriefingLabel", muted: true, autowrap: true);
        _briefingLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        matchCard.AddChild(_briefingLabel);

        _selectionLabel = BodyLabel("SelectionLabel", muted: true, autowrap: true);
        _selectionLabel.Visible = false;
        matchCard.AddChild(_selectionLabel);

        var tacticsStripPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tacticsStripPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        matchCard.AddChild(tacticsStripPanel);

        var tacticsCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tacticsCard.AddThemeConstantOverride("separation", 3);
        tacticsStripPanel.AddChild(tacticsCard);

        _todayTacticsSummaryLabel = BodyLabel("TodayTacticsSummary", autowrap: true);
        _todayTacticsSummaryLabel.Text = "⚽ 4-4-2 · Dengeli · Dengeli Pres";
        _todayTacticsSummaryLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _todayTacticsSummaryLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        tacticsCard.AddChild(_todayTacticsSummaryLabel);

        _todayFitnessSummaryLabel = BodyLabel("TodayFitnessSummary", muted: true, autowrap: true);
        _todayFitnessSummaryLabel.Text = "⚡ Kondisyon Özeti Hazırlanıyor";
        _todayFitnessSummaryLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        tacticsCard.AddChild(_todayFitnessSummaryLabel);

        var primaryRow = ActionFlow();
        matchCard.AddChild(primaryRow);
        _openTacticsButton = SecondaryButton("🟩 Saha & Taktik");
        _openTacticsButton.Pressed += () => ShowPage(HubPage.Prep);
        primaryRow.AddChild(_openTacticsButton);

        _approveSelectionButton = AccentButton("✓ 11'i Onayla");
        _approveSelectionButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        primaryRow.AddChild(_approveSelectionButton);

        _playButton = PrimaryButton("⚽ MAÇA GİT");
        _playButton.Pressed += OnPlayMatches;
        primaryRow.AddChild(_playButton);

        _swapSelectionButton = SecondaryButton("⇄ XI ↔ Yedek");
        _swapSelectionButton.Visible = false;
        _swapSelectionButton.Pressed += () => Apply(_controller.SwapLastStarterWithFirstBenchForNextDueMatch());
        primaryRow.AddChild(_swapSelectionButton);

        _restEntireTeamButton = SecondaryButton("🛌 Dinlendir");
        _restEntireTeamButton.Pressed += () => Apply(_controller.RestEntireTeam());
        primaryRow.AddChild(_restEntireTeamButton);

        _lightRegenerationButton = SecondaryButton("♻ Hafif İdman");
        _lightRegenerationButton.Visible = false;
        _lightRegenerationButton.Pressed += () => Apply(_controller.ApplyLightRegeneration());
        primaryRow.AddChild(_lightRegenerationButton);

        // Daily Objectives Card (SM27 / Top Eleven 5-Minute Loop)
        var objectivesNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        objectivesNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        col1.AddChild(objectivesNeonPanel);

        var objectivesCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        objectivesCard.AddThemeConstantOverride("separation", 6);
        objectivesNeonPanel.AddChild(objectivesCard);

        var objHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        objHeaderRow.AddThemeConstantOverride("separation", 6);
        objectivesCard.AddChild(objHeaderRow);
        objHeaderRow.AddChild(UiComponents.PageHeader("🎯", "GÜNLÜK HEDEFLER (5 DK OTURUM)"));

        var objBadgePanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        objBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.22f));
        _dailyObjectivesProgressBadge = new Label { Text = "0/3 Tamam" };
        CareerUiTheme.StyleTableText(_dailyObjectivesProgressBadge);
        _dailyObjectivesProgressBadge.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        objBadgePanel.AddChild(_dailyObjectivesProgressBadge);
        objHeaderRow.AddChild(objBadgePanel);

        _dailyObjectivesBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _dailyObjectivesBox.AddThemeConstantOverride("separation", 6);
        objectivesCard.AddChild(_dailyObjectivesBox);

        // Keep hidden legacy controls alive so controller bindings never null-ref
        _continuePreviewLabel = BodyLabel("ContinuePreviewLabel", muted: true, autowrap: true);
        _continuePreviewLabel.Visible = false;
        objectivesCard.AddChild(_continuePreviewLabel);

        var timeRow = ActionFlow();
        objectivesCard.AddChild(timeRow);

        _officeNextStepButton = PrimaryButton("▶ DEVAM ET");
        _officeNextStepButton.CustomMinimumSize = new Vector2(110, 30);
        _officeNextStepButton.Pressed += OnOfficeNextStepPressed;
        timeRow.AddChild(_officeNextStepButton);

        _advanceDayButton = SecondaryButton("+1 Gün");
        _advanceDayButton.Pressed += () => Apply(_controller.AdvanceDays(1));
        timeRow.AddChild(_advanceDayButton);

        _advanceWeekButton = SecondaryButton("+7 Gün");
        _advanceWeekButton.Pressed += () => Apply(_controller.AdvanceDays(7));
        timeRow.AddChild(_advanceWeekButton);

        _seasonTransitionButton = AccentButton("🏁 Yeni Sezon");
        _seasonTransitionButton.Pressed += () => Apply(_controller.TransitionToNextSeason());
        timeRow.AddChild(_seasonTransitionButton);

        // ====================================================
        // COLUMN 2: FM26 PORTAL STREAM (FILTERS + ACTION TILES + DECISION DESK)
        // ====================================================
        var col2 = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.25f,
        };
        col2.AddThemeConstantOverride("separation", 8);
        bentoRow.AddChild(col2);

        // Decision Desk Card (Visible when a decision is pending)
        var decisionNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        decisionNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        _todayDecisionPanel = decisionNeonPanel;
        col2.AddChild(decisionNeonPanel);

        var decisionCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        decisionCard.AddThemeConstantOverride("separation", 6);
        decisionNeonPanel.AddChild(decisionCard);
        decisionCard.AddChild(UiComponents.PageHeader("⚖️", "ZORUNLU KARAR MASASI"));

        _deskLabel = BodyLabel("DeskLabel", autowrap: true);
        decisionCard.AddChild(_deskLabel);

        _decisionCausalityPanel = new PanelContainer
        {
            Visible = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _decisionCausalityPanel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning, 0.22f));
        _decisionCausalityLabel = BodyLabel("DecisionCausalityLabel", autowrap: true);
        _decisionCausalityLabel.AddThemeColorOverride("font_color", CareerUiTheme.ColorWarning);
        _decisionCausalityPanel.AddChild(_decisionCausalityLabel);
        decisionCard.AddChild(_decisionCausalityPanel);

        var deskRow = ActionFlow();
        decisionCard.AddChild(deskRow);
        _grantDecisionButton = AccentButton("✓ Kabul Et");
        _grantDecisionButton.Pressed += () => ApplyDecision(
            () => _controller.AnswerOldestPendingDecision(grantPromise: true));
        deskRow.AddChild(_grantDecisionButton);
        _refuseDecisionButton = DangerButton("✕ Reddet");
        _refuseDecisionButton.Pressed += () => ApplyDecision(
            () => _controller.AnswerOldestPendingDecision(grantPromise: false));
        deskRow.AddChild(_refuseDecisionButton);
        _disciplineWarningButton = SecondaryButton("⚠ Uyarı");
        _disciplineWarningButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionIssueWarning));
        deskRow.AddChild(_disciplineWarningButton);
        _disciplineFineButton = DangerButton("⛔ Ceza");
        _disciplineFineButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionIssueFine));
        deskRow.AddChild(_disciplineFineButton);
        _disciplineSupportButton = PrimaryButton("♥ Destekle");
        _disciplineSupportButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionOfferSupport));
        deskRow.AddChild(_disciplineSupportButton);
        _boardCounterButton = AccentButton("⇄ Karşı Teklif");
        _boardCounterButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(
                Domain.Interaction.DecisionRequest.OptionCounterBoardDemand));
        deskRow.AddChild(_boardCounterButton);
        _pressCriticizeButton = DangerButton("🎙 Eleştir");
        _pressCriticizeButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(
                Domain.Interaction.DecisionRequest.OptionPubliclyCriticize));
        deskRow.AddChild(_pressCriticizeButton);

        // FM26 Unified Portal Card
        var portalNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        portalNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        col2.AddChild(portalNeonPanel);

        var portalCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        portalCard.AddThemeConstantOverride("separation", 6);
        portalNeonPanel.AddChild(portalCard);

        var portalHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        portalHeaderRow.AddThemeConstantOverride("separation", 6);
        portalCard.AddChild(portalHeaderRow);
        portalHeaderRow.AddChild(UiComponents.PageHeader("📬", "PORTAL & TEKNİK EKİP MASASI"));

        _fanApprovalBadgePanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _fanApprovalBadgePanel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.18f));
        _fanApprovalBadgeLabel = new Label { Text = "♥ %50" };
        CareerUiTheme.StyleTableText(_fanApprovalBadgeLabel);
        _fanApprovalBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _fanApprovalBadgePanel.AddChild(_fanApprovalBadgeLabel);
        portalHeaderRow.AddChild(_fanApprovalBadgePanel);

        // 4 Portal Filter Pills (FM26 Portal Spec)
        var portalFilterRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        portalFilterRow.AddThemeConstantOverride("separation", 4);
        portalCard.AddChild(portalFilterRow);

        _portalFilterAllBtn = new Button { Text = "📰 Tümü", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _portalFilterAllBtn.Pressed += () => SetPortalFilter(MobilePortalAndDeckDigest.FilterAll);
        portalFilterRow.AddChild(_portalFilterAllBtn);

        _portalFilterUrgentBtn = new Button { Text = "🔥 Zorunlu", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _portalFilterUrgentBtn.Pressed += () => SetPortalFilter(MobilePortalAndDeckDigest.FilterMustRespond);
        portalFilterRow.AddChild(_portalFilterUrgentBtn);

        _portalFilterTasksBtn = new Button { Text = "📋 Görevler", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _portalFilterTasksBtn.Pressed += () => SetPortalFilter(MobilePortalAndDeckDigest.FilterTasks);
        portalFilterRow.AddChild(_portalFilterTasksBtn);

        _portalFilterAdviceBtn = new Button { Text = "💡 Tavsiye", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _portalFilterAdviceBtn.Pressed += () => SetPortalFilter(MobilePortalAndDeckDigest.FilterAdvice);
        portalFilterRow.AddChild(_portalFilterAdviceBtn);
        UpdatePortalFilterStyles();

        _portalStreamBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _portalStreamBox.AddThemeConstantOverride("separation", 6);
        portalCard.AddChild(_portalStreamBox);

        // Compact Fan / Week Story / Injury strip at bottom of Portal
        _fanFeedLabel = BodyLabel("FanFeedLabel", muted: true, autowrap: true);
        _fanFeedLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        portalCard.AddChild(_fanFeedLabel);

        _blockerLabel = BodyLabel("BlockerLabel", autowrap: true);
        _blockerLabel.Visible = false;
        portalCard.AddChild(_blockerLabel);
        _pulseLabel = BodyLabel("PulseLabel", muted: true, autowrap: true);
        _pulseLabel.Visible = false;
        portalCard.AddChild(_pulseLabel);
        _weekStoryLabel = BodyLabel("WeekStoryLabel", autowrap: true);
        _weekStoryLabel.Visible = false;
        portalCard.AddChild(_weekStoryLabel);
        _recoveryPathLabel = BodyLabel("RecoveryPathLabel", autowrap: true);
        _recoveryPathLabel.Visible = false;
        portalCard.AddChild(_recoveryPathLabel);
        _officeLabel = BodyLabel("OfficeLabel", muted: true, autowrap: true);
        _officeLabel.Visible = false;
        portalCard.AddChild(_officeLabel);

        // Hidden First Week Guide container kept for compatibility
        var guideHost = new VBoxContainer { Visible = false };
        col2.AddChild(guideHost);
        _firstWeekGuideCard = guideHost;
        _firstWeekGuideLabel = BodyLabel("FirstWeekGuideLabel", autowrap: true);
        guideHost.AddChild(_firstWeekGuideLabel);
        _firstWeekGuideOpenButton = PrimaryButton("Ekranı Aç");
        guideHost.AddChild(_firstWeekGuideOpenButton);
        _firstWeekGuideNextButton = SecondaryButton("Tamamlandı →");
        guideHost.AddChild(_firstWeekGuideNextButton);

        // ====================================================
        // COLUMN 3: TACTILE MANAGER CARD DECK (NEW STAR MANAGER / TOP DRIVES) + STANDINGS
        // ====================================================
        var col3 = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.15f,
        };
        col3.AddThemeConstantOverride("separation", 8);
        bentoRow.AddChild(col3);

        var deckNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        deckNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        col3.AddChild(deckNeonPanel);

        var deckCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        deckCard.AddThemeConstantOverride("separation", 6);
        deckNeonPanel.AddChild(deckCard);
        deckCard.AddChild(UiComponents.PageHeader("🃏", "DOKUNSAL MENAJER KART DESTESİ"));

        _tactileDeckGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _tactileDeckGrid.AddThemeConstantOverride("h_separation", 6);
        _tactileDeckGrid.AddThemeConstantOverride("v_separation", 6);
        deckCard.AddChild(_tactileDeckGrid);

        var standingsNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        standingsNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        col3.AddChild(standingsNeonPanel);

        var standingsCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        standingsCard.AddThemeConstantOverride("separation", 5);
        standingsNeonPanel.AddChild(standingsCard);
        standingsCard.AddChild(UiComponents.PageHeader("📊", "SÜPER LİG TABLOSU"));

        _miniStandingsContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _miniStandingsContainer.AddThemeConstantOverride("separation", 3);
        standingsCard.AddChild(_miniStandingsContainer);

        _openFullStandingsButton = SecondaryButton("📊 Tam Tablo & Fikstür →");
        _openFullStandingsButton.Pressed += () => ShowPage(HubPage.World);
        standingsCard.AddChild(_openFullStandingsButton);

        return page;
    }

    private void SetPortalFilter(string filterCode)
    {
        _activePortalFilter = filterCode;
        UpdatePortalFilterStyles();
        RefreshPortalAndDeck();
    }

    private void UpdatePortalFilterStyles()
    {
        if (_portalFilterAllBtn is null)
        {
            return;
        }

        CareerUiTheme.StyleTabButton(_portalFilterAllBtn, _activePortalFilter == MobilePortalAndDeckDigest.FilterAll);
        CareerUiTheme.StyleTabButton(_portalFilterUrgentBtn, _activePortalFilter == MobilePortalAndDeckDigest.FilterMustRespond);
        CareerUiTheme.StyleTabButton(_portalFilterTasksBtn, _activePortalFilter == MobilePortalAndDeckDigest.FilterTasks);
        CareerUiTheme.StyleTabButton(_portalFilterAdviceBtn, _activePortalFilter == MobilePortalAndDeckDigest.FilterAdvice);
    }

    private void RefreshPortalAndDeck()
    {
        if (_portalCalendarRow is null || _dailyObjectivesBox is null || _portalStreamBox is null || _tactileDeckGrid is null)
        {
            return;
        }

        var portal = _controller.BuildMobilePortalAndDeck();

        // 1. Render 7-Day Visual Calendar Strip
        MatchScreenUi.ClearChildren(_portalCalendarRow);
        foreach (var dayTile in portal.CalendarStrip)
        {
            var tileColor = dayTile.IsMatchDay
                ? CareerUiTheme.Accent
                : dayTile.IsHighLoad
                    ? CareerUiTheme.ColorWarning
                    : CareerUiTheme.Data;

            var tilePanel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 48),
            };
            tilePanel.AddThemeStyleboxOverride(
                "panel",
                CareerUiTheme.BadgePillPanel(tileColor, dayTile.IsMatchDay ? 0.28f : 0.14f));
            _portalCalendarRow.AddChild(tilePanel);

            var tileBox = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            tileBox.AddThemeConstantOverride("separation", 1);
            tilePanel.AddChild(tileBox);

            var topLabel = new Label
            {
                Text = $"{dayTile.DayBadge} · {dayTile.Icon}",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            CareerUiTheme.StyleTableText(topLabel);
            topLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
            topLabel.AddThemeColorOverride("font_color", tileColor);
            tileBox.AddChild(topLabel);

            var titleLabel = new Label
            {
                Text = dayTile.Title,
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipText = true,
            };
            CareerUiTheme.StyleHeadline(titleLabel);
            titleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
            tileBox.AddChild(titleLabel);
        }

        // 2. Render 5-Minute Daily Objectives
        _dailyObjectivesProgressBadge.Text = $"🎯 {portal.CompletedObjectivesCount}/{portal.TotalObjectivesCount} Tamam";
        if (_topQuestBadgeLabel is not null)
        {
            _topQuestBadgeLabel.Text = $"🎯 Görev {portal.CompletedObjectivesCount}/{portal.TotalObjectivesCount}";
        }

        MatchScreenUi.ClearChildren(_dailyObjectivesBox);
        foreach (var obj in portal.DailyObjectives)
        {
            var objColor = obj.IsCompleted ? CareerUiTheme.ActionBright : CareerUiTheme.Accent;
            var objPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            objPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BoardObjectiveCardPanel(objColor));
            _dailyObjectivesBox.AddChild(objPanel);

            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 8);
            objPanel.AddChild(row);

            var copyBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            copyBox.AddThemeConstantOverride("separation", 2);
            row.AddChild(copyBox);

            var titleLbl = new Label
            {
                Text = $"{obj.Icon} {obj.Title}  ({obj.RewardBadge})",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleHeadline(titleLbl);
            titleLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
            titleLbl.AddThemeColorOverride("font_color", objColor);
            copyBox.AddChild(titleLbl);

            var prog = new ProgressBar
            {
                MinValue = 0,
                MaxValue = 100,
                Value = obj.IsCompleted ? 100 : 35,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0, 5),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            prog.AddThemeStyleboxOverride("background", CareerUiTheme.ProgressBarBackground());
            prog.AddThemeStyleboxOverride("fill", CareerUiTheme.ProgressBarFill(objColor));
            copyBox.AddChild(prog);

            var actionBtn = obj.IsCompleted
                ? SecondaryButton("✓ Tamam")
                : PrimaryButton(obj.ActionButtonLabel);
            actionBtn.CustomMinimumSize = new Vector2(118, 30);
            actionBtn.Disabled = obj.IsCompleted;
            var code = obj.ObjectiveCode;
            actionBtn.Pressed += () => ExecuteDailyObjectiveAction(code);
            row.AddChild(actionBtn);
        }

        // 3. Render Portal Stream Tiles
        MatchScreenUi.ClearChildren(_portalStreamBox);
        var filteredStream = portal.PortalStream
            .Where(item => _activePortalFilter == MobilePortalAndDeckDigest.FilterAll
                           || string.Equals(item.CategoryCode, _activePortalFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filteredStream.Count == 0)
        {
            var emptyLbl = BodyLabel("PortalEmptyLabel", muted: true, autowrap: true);
            emptyLbl.Text = "Bu filtrede bekleyen bildirim yok — tüm işler yolunda.";
            _portalStreamBox.AddChild(emptyLbl);
        }
        else
        {
            foreach (var item in filteredStream)
            {
                var itemColor = item.IsUrgent ? CareerUiTheme.ColorWarning : CareerUiTheme.Data;
                var itemPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                itemPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
                _portalStreamBox.AddChild(itemPanel);

                var itemRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                itemRow.AddThemeConstantOverride("separation", 8);
                itemPanel.AddChild(itemRow);

                var textCol = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                textCol.AddThemeConstantOverride("separation", 2);
                itemRow.AddChild(textCol);

                var badgeLbl = new Label { Text = $"{item.RoleBadge}" };
                CareerUiTheme.StyleEyebrow(badgeLbl, itemColor);
                badgeLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
                textCol.AddChild(badgeLbl);

                var headLbl = new Label
                {
                    Text = $"{item.Icon} {item.Headline}",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };
                CareerUiTheme.StyleHeadline(headLbl);
                headLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
                textCol.AddChild(headLbl);

                var detailLbl = BodyLabel("PortalItemDetail", muted: true, autowrap: true);
                detailLbl.Text = item.Detail;
                detailLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
                textCol.AddChild(detailLbl);

                var btn = item.IsUrgent ? AccentButton(item.ActionLabel) : SecondaryButton(item.ActionLabel);
                btn.CustomMinimumSize = new Vector2(124, 32);
                btn.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                var actionCode = item.ActionCode;
                btn.Pressed += () => ExecutePortalTileAction(actionCode);
                itemRow.AddChild(btn);
            }
        }

        // 4. Render Tactile Manager Card Deck (2x2 Collectible Cards)
        MatchScreenUi.ClearChildren(_tactileDeckGrid);
        foreach (var card in portal.TactileDeck)
        {
            var cardColor = card.CardCode switch
            {
                MobilePortalAndDeckDigest.CardEnergyPack => CareerUiTheme.ActionBright,
                MobilePortalAndDeckDigest.CardPepTalk => CareerUiTheme.Accent,
                MobilePortalAndDeckDigest.CardPhysioSpa => CareerUiTheme.Data,
                _ => CareerUiTheme.ColorWarning,
            };

            var cardPanel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            cardPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(cardColor, 0.18f));
            _tactileDeckGrid.AddChild(cardPanel);

            var cardBox = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            cardBox.AddThemeConstantOverride("separation", 3);
            cardPanel.AddChild(cardBox);

            var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            cardBox.AddChild(headerRow);

            var rarityLbl = new Label
            {
                Text = $"{card.Icon} {card.RarityTier}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            CareerUiTheme.StyleEyebrow(rarityLbl, cardColor);
            rarityLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
            headerRow.AddChild(rarityLbl);

            var chargeLbl = new Label { Text = $"x{card.AvailableCharges}" };
            CareerUiTheme.StyleTableText(chargeLbl);
            chargeLbl.AddThemeColorOverride("font_color", Colors.White);
            headerRow.AddChild(chargeLbl);

            var titleLbl = new Label
            {
                Text = card.Title,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleHeadline(titleLbl);
            titleLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
            cardBox.AddChild(titleLbl);

            var effectLbl = BodyLabel($"DeckEffect_{card.CardCode}", muted: true, autowrap: true);
            effectLbl.Text = card.EffectHeadline;
            effectLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
            cardBox.AddChild(effectLbl);

            var playCardBtn = PrimaryButton(card.ButtonLabel);
            playCardBtn.CustomMinimumSize = new Vector2(0, 28);
            var code = card.CardCode;
            var targetPanel = cardPanel;
            playCardBtn.Pressed += () =>
            {
                PlayInterfaceFeedback(MatchAudioCue.Whistle, vibrationDurationMs: 22);
                if (!CareerUiTheme.ReducedMotion)
                {
                    targetPanel.Scale = new Vector2(1.06f, 1.06f);
                    CreateTween()
                        .TweenProperty(targetPanel, "scale", Vector2.One, 0.22f)
                        .SetTrans(Tween.TransitionType.Back)
                        .SetEase(Tween.EaseType.Out);
                }

                Apply(_controller.PlayTactileManagerCard(code));
            };
            cardBox.AddChild(playCardBtn);
        }
    }

    private void ExecuteDailyObjectiveAction(string objectiveCode)
    {
        switch (objectiveCode)
        {
            case "OBJ_TRAINING":
                Apply(_controller.ApplyLightRegeneration());
                break;
            case "OBJ_ROTATION":
                Apply(_controller.ApplySmartRotationForNextMatch());
                break;
            case "OBJ_TACTIC_CARD":
                Apply(_controller.PlayTactileManagerCard(MobilePortalAndDeckDigest.CardEnergyPack));
                break;
        }
    }

    private void ExecutePortalTileAction(string actionCode)
    {
        switch (actionCode)
        {
            case "ACTION_ANSWER_DECISION":
                ApplyDecision(() => _controller.AnswerOldestPendingDecision(grantPromise: true));
                break;
            case "ACTION_SMART_ROTATION":
                Apply(_controller.ApplySmartRotationForNextMatch());
                break;
            case "ACTION_OPEN_TACTICS":
                ShowPage(HubPage.Prep);
                break;
            case "ACTION_OPEN_CAMPUS":
                ShowPage(HubPage.Club);
                break;
            case MobilePortalAndDeckDigest.CardEnergyPack:
            case MobilePortalAndDeckDigest.CardPepTalk:
            case MobilePortalAndDeckDigest.CardPhysioSpa:
                Apply(_controller.PlayTactileManagerCard(actionCode));
                break;
            case MobilePortalAndDeckDigest.CardScoutPack:
                Apply(_controller.PlayTactileManagerCard(actionCode));
                ShowPage(HubPage.Transfer);
                break;
        }
    }
}
