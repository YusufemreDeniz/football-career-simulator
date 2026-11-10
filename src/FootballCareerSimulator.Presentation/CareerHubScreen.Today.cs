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
    private HBoxContainer _tactileDeckHandRow = null!;
    private Label _matchHeroHomeNameLabel = null!;
    private Label _matchHeroAwayNameLabel = null!;
    private Label _matchHeroCountdownLabel = null!;
    private BroadcastCrestBadge _matchHeroHomeCrest = null!;
    private BroadcastCrestBadge _matchHeroAwayCrest = null!;
    private PanelContainer _matchHeroPanel = null!;
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
        // 1. ÜST ŞERİT: 7 GÜNLÜK GÖRSEL TAKVİM (BROADCAST STADIUM NOIR)
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
        // 2. EN ÜST ANA BÖLÜM: SİNEMATİK "MAÇ GÜNÜ HERO KARTI" (Rule 6)
        // Ev Sahibi vs Deplasman Armaları, Geri Sayım ve Parlayan MAÇA ÇIK Butonu
        // ====================================================
        var matchNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _matchHeroPanel = matchNeonPanel;
        matchNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        page.AddChild(matchNeonPanel);

        var matchCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        matchCard.AddThemeConstantOverride("separation", 8);
        matchNeonPanel.AddChild(matchCard);

        // Üst Yayın Meta Satırı: Turnuva + Saha + Geri Sayım + Kadro Onay Rozeti
        var matchMetaRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        matchMetaRow.AddThemeConstantOverride("separation", 8);
        matchCard.AddChild(matchMetaRow);

        matchMetaRow.AddChild(new BroadcastVectorIcon("trophy", CareerUiTheme.Accent, 16f));

        _nextMatchCompetitionBadge = BodyLabel("NextMatchCompetitionBadge");
        _nextMatchCompetitionBadge.Text = "SÜPER LİG YAYINI";
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

        var countdownPill = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        countdownPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.22f));
        _matchHeroCountdownLabel = BodyLabel("MatchHeroCountdownLabel");
        _matchHeroCountdownLabel.Text = "MAÇ GÜNÜ BEKLENİYOR";
        _matchHeroCountdownLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _matchHeroCountdownLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        countdownPill.AddChild(_matchHeroCountdownLabel);
        matchMetaRow.AddChild(countdownPill);

        var metaSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        matchMetaRow.AddChild(metaSpacer);

        _nextMatchApprovalPill = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _nextMatchApprovalPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning));
        _nextMatchApprovalLabel = BodyLabel("NextMatchApprovalLabel");
        _nextMatchApprovalLabel.Text = "Kadro Onayı";
        _nextMatchApprovalLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _nextMatchApprovalLabel.AddThemeColorOverride("font_color", CareerUiTheme.ColorWarning);
        _nextMatchApprovalPill.AddChild(_nextMatchApprovalLabel);
        matchMetaRow.AddChild(_nextMatchApprovalPill);

        // Sinematik Karşılaşma Sahnesi: Ev Sahibi Arması — Merkez VS & MAÇA ÇIK — Deplasman Arması
        var showdownRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        showdownRow.AddThemeConstantOverride("separation", 18);
        matchCard.AddChild(showdownRow);

        // Sol: Ev Sahibi Kulüp Arması + İsim
        var homeCol = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.End,
        };
        homeCol.AddThemeConstantOverride("separation", 12);
        showdownRow.AddChild(homeCol);

        var homeTextStack = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        homeTextStack.AddThemeConstantOverride("separation", 2);
        homeCol.AddChild(homeTextStack);

        var homeTag = new Label
        {
            Text = "EV SAHİBİ",
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        CareerUiTheme.StyleEyebrow(homeTag, CareerUiTheme.ActionBright);
        homeTag.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        homeTextStack.AddChild(homeTag);

        _matchHeroHomeNameLabel = new Label
        {
            Text = "KULÜBÜMÜZ",
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        CareerUiTheme.ApplyDisplayFont(_matchHeroHomeNameLabel);
        _matchHeroHomeNameLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(18));
        _matchHeroHomeNameLabel.AddThemeColorOverride("font_color", Colors.White);
        homeTextStack.AddChild(_matchHeroHomeNameLabel);

        _matchHeroHomeCrest = new BroadcastCrestBadge("EV", CareerUiTheme.ActionBright, CareerUiTheme.Accent);
        homeCol.AddChild(_matchHeroHomeCrest);

        // Merkez: VS Başlığı + Taktik Özeti + Parlayan MAÇA ÇIK Butonu
        var centerHeroCol = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        centerHeroCol.AddThemeConstantOverride("separation", 4);
        showdownRow.AddChild(centerHeroCol);

        _nextMatchOpponentLabel = TitleLabel("NextMatchOpponentLabel");
        _nextMatchOpponentLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _nextMatchOpponentLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _nextMatchOpponentLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(17));
        _nextMatchOpponentLabel.Text = "VS —";
        centerHeroCol.AddChild(_nextMatchOpponentLabel);

        _briefingLabel = BodyLabel("BriefingLabel", muted: true, autowrap: true);
        _briefingLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _briefingLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        centerHeroCol.AddChild(_briefingLabel);

        _selectionLabel = BodyLabel("SelectionLabel", muted: true, autowrap: true);
        _selectionLabel.Visible = false;
        centerHeroCol.AddChild(_selectionLabel);

        var tacticsLineRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        tacticsLineRow.AddThemeConstantOverride("separation", 12);
        centerHeroCol.AddChild(tacticsLineRow);

        _todayTacticsSummaryLabel = BodyLabel("TodayTacticsSummary", autowrap: false);
        _todayTacticsSummaryLabel.Text = "4-4-2 · Dengeli · Dengeli Pres";
        _todayTacticsSummaryLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _todayTacticsSummaryLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        tacticsLineRow.AddChild(_todayTacticsSummaryLabel);

        _todayFitnessSummaryLabel = BodyLabel("TodayFitnessSummary", muted: true, autowrap: false);
        _todayFitnessSummaryLabel.Text = "Kondisyon Özeti Hazırlanıyor";
        _todayFitnessSummaryLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        tacticsLineRow.AddChild(_todayFitnessSummaryLabel);

        var heroCtaRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        heroCtaRow.AddThemeConstantOverride("separation", 8);
        centerHeroCol.AddChild(heroCtaRow);

        _openTacticsButton = SecondaryButton("Taktik Tahtası");
        _openTacticsButton.CustomMinimumSize = new Vector2(120, 36);
        _openTacticsButton.Pressed += () => ShowPage(HubPage.Prep);
        heroCtaRow.AddChild(_openTacticsButton);

        _playButton = PrimaryButton("MAÇA ÇIK");
        _playButton.CustomMinimumSize = new Vector2(170, 38);
        _playButton.Pressed += () =>
        {
            var curDay = _controller.GetCurrentGameDate().DayNumber;
            if (_controller.GetNextDueManagedFixture(curDay) is not null || _controller.HasDueContinentalMatch())
            {
                OnPlayMatches();
            }
            else
            {
                OnOfficeNextStepPressed();
            }
        };
        heroCtaRow.AddChild(_playButton);

        _officeNextStepButton = AccentButton("DEVAM ET");
        _officeNextStepButton.CustomMinimumSize = new Vector2(124, 36);
        _officeNextStepButton.Pressed += OnOfficeNextStepPressed;
        heroCtaRow.AddChild(_officeNextStepButton);

        _approveSelectionButton = AccentButton("11'i Onayla");
        _approveSelectionButton.Visible = false;
        _approveSelectionButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        heroCtaRow.AddChild(_approveSelectionButton);

        _swapSelectionButton = SecondaryButton("XI / Yedek");
        _swapSelectionButton.Visible = false;
        _swapSelectionButton.Pressed += () => Apply(_controller.SwapLastStarterWithFirstBenchForNextDueMatch());
        heroCtaRow.AddChild(_swapSelectionButton);

        _restEntireTeamButton = SecondaryButton("Dinlendir");
        _restEntireTeamButton.Pressed += () => Apply(_controller.RestEntireTeam());
        heroCtaRow.AddChild(_restEntireTeamButton);

        _lightRegenerationButton = SecondaryButton("Hafif İdman");
        _lightRegenerationButton.Visible = false;
        _lightRegenerationButton.Pressed += () => Apply(_controller.ApplyLightRegeneration());
        heroCtaRow.AddChild(_lightRegenerationButton);

        _advanceDayButton = SecondaryButton("+1 Gün");
        _advanceDayButton.Pressed += () => Apply(_controller.AdvanceDays(1));
        heroCtaRow.AddChild(_advanceDayButton);

        _advanceWeekButton = SecondaryButton("+7 Gün");
        _advanceWeekButton.Visible = false;
        _advanceWeekButton.Pressed += () => Apply(_controller.AdvanceDays(7));
        heroCtaRow.AddChild(_advanceWeekButton);

        _seasonTransitionButton = AccentButton("Yeni Sezon");
        _seasonTransitionButton.Visible = false;
        _seasonTransitionButton.Pressed += () => Apply(_controller.TransitionToNextSeason());
        heroCtaRow.AddChild(_seasonTransitionButton);

        // Sağ: Deplasman Kulüp Arması + İsim
        var awayCol = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Begin,
        };
        awayCol.AddThemeConstantOverride("separation", 12);
        showdownRow.AddChild(awayCol);

        _matchHeroAwayCrest = new BroadcastCrestBadge("DEP", CareerUiTheme.Data, CareerUiTheme.SpecularBorder);
        awayCol.AddChild(_matchHeroAwayCrest);

        var awayTextStack = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        awayTextStack.AddThemeConstantOverride("separation", 2);
        awayCol.AddChild(awayTextStack);

        var awayTag = new Label
        {
            Text = "RAKİP / DEPLASMAN",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        CareerUiTheme.StyleEyebrow(awayTag, CareerUiTheme.Data);
        awayTag.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        awayTextStack.AddChild(awayTag);

        _matchHeroAwayNameLabel = new Label
        {
            Text = "RAKİP KULÜP",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        CareerUiTheme.ApplyDisplayFont(_matchHeroAwayNameLabel);
        _matchHeroAwayNameLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(18));
        _matchHeroAwayNameLabel.AddThemeColorOverride("font_color", Colors.White);
        awayTextStack.AddChild(_matchHeroAwayNameLabel);

        // ====================================================
        // 3. ORTA BÖLÜM: ZORUNLU KARAR MASASI & KOMPAKT PORTAL AKIŞI
        // ====================================================
        var decisionNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        decisionNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        _todayDecisionPanel = decisionNeonPanel;
        page.AddChild(decisionNeonPanel);

        var decisionCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        decisionCard.AddThemeConstantOverride("separation", 6);
        decisionNeonPanel.AddChild(decisionCard);
        decisionCard.AddChild(UiComponents.PageHeader("warning", "ZORUNLU KARAR MASASI"));

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
        _grantDecisionButton = AccentButton("Kabul Et");
        _grantDecisionButton.Pressed += () => ApplyDecision(
            () => _controller.AnswerOldestPendingDecision(grantPromise: true));
        deskRow.AddChild(_grantDecisionButton);
        _refuseDecisionButton = DangerButton("Reddet");
        _refuseDecisionButton.Pressed += () => ApplyDecision(
            () => _controller.AnswerOldestPendingDecision(grantPromise: false));
        deskRow.AddChild(_refuseDecisionButton);
        _disciplineWarningButton = SecondaryButton("Uyarı");
        _disciplineWarningButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionIssueWarning));
        deskRow.AddChild(_disciplineWarningButton);
        _disciplineFineButton = DangerButton("Ceza");
        _disciplineFineButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionIssueFine));
        deskRow.AddChild(_disciplineFineButton);
        _disciplineSupportButton = PrimaryButton("Destekle");
        _disciplineSupportButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionOfferSupport));
        deskRow.AddChild(_disciplineSupportButton);
        _boardCounterButton = AccentButton("Karşı Teklif");
        _boardCounterButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(
                Domain.Interaction.DecisionRequest.OptionCounterBoardDemand));
        deskRow.AddChild(_boardCounterButton);
        _pressCriticizeButton = DangerButton("Eleştir");
        _pressCriticizeButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(
                Domain.Interaction.DecisionRequest.OptionPubliclyCriticize));
        deskRow.AddChild(_pressCriticizeButton);

        // Tek Akışlı Portal & Günlük Hedefler Paneli
        var portalNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        portalNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        page.AddChild(portalNeonPanel);

        var portalCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        portalCard.AddThemeConstantOverride("separation", 6);
        portalNeonPanel.AddChild(portalCard);

        var portalHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        portalHeaderRow.AddThemeConstantOverride("separation", 8);
        portalCard.AddChild(portalHeaderRow);
        portalHeaderRow.AddChild(UiComponents.PageHeader("radar", "PORTAL AKIŞI & GÜNLÜK HEDEFLER"));

        var objBadgePanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        objBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.22f));
        _dailyObjectivesProgressBadge = new Label { Text = "0/3 Tamam" };
        CareerUiTheme.StyleTableText(_dailyObjectivesProgressBadge);
        _dailyObjectivesProgressBadge.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        objBadgePanel.AddChild(_dailyObjectivesProgressBadge);
        portalHeaderRow.AddChild(objBadgePanel);

        _fanApprovalBadgePanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _fanApprovalBadgePanel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.18f));
        _fanApprovalBadgeLabel = new Label { Text = "Taraftar %50" };
        CareerUiTheme.StyleTableText(_fanApprovalBadgeLabel);
        _fanApprovalBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _fanApprovalBadgePanel.AddChild(_fanApprovalBadgeLabel);
        portalHeaderRow.AddChild(_fanApprovalBadgePanel);

        _portalFilterAllBtn = new Button { Text = "Tümü", CustomMinimumSize = new Vector2(74, 26) };
        _portalFilterAllBtn.Pressed += () => SetPortalFilter(MobilePortalAndDeckDigest.FilterAll);
        portalHeaderRow.AddChild(_portalFilterAllBtn);

        _portalFilterUrgentBtn = new Button { Text = "Zorunlu", CustomMinimumSize = new Vector2(78, 26) };
        _portalFilterUrgentBtn.Pressed += () => SetPortalFilter(MobilePortalAndDeckDigest.FilterMustRespond);
        portalHeaderRow.AddChild(_portalFilterUrgentBtn);

        _portalFilterTasksBtn = new Button { Text = "Görevler", CustomMinimumSize = new Vector2(78, 26) };
        _portalFilterTasksBtn.Pressed += () => SetPortalFilter(MobilePortalAndDeckDigest.FilterTasks);
        portalHeaderRow.AddChild(_portalFilterTasksBtn);

        _portalFilterAdviceBtn = new Button { Text = "Tavsiye", CustomMinimumSize = new Vector2(78, 26) };
        _portalFilterAdviceBtn.Pressed += () => SetPortalFilter(MobilePortalAndDeckDigest.FilterAdvice);
        portalHeaderRow.AddChild(_portalFilterAdviceBtn);
        UpdatePortalFilterStyles();

        var portalScroll = new MobileScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        portalCard.AddChild(portalScroll);

        var portalInnerStack = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        portalInnerStack.AddThemeConstantOverride("separation", 6);
        portalScroll.AddChild(portalInnerStack);

        _dailyObjectivesBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _dailyObjectivesBox.AddThemeConstantOverride("separation", 4);
        portalInnerStack.AddChild(_dailyObjectivesBox);

        _portalStreamBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _portalStreamBox.AddThemeConstantOverride("separation", 4);
        portalInnerStack.AddChild(_portalStreamBox);

        _fanFeedLabel = BodyLabel("FanFeedLabel", muted: true, autowrap: true);
        _fanFeedLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        portalCard.AddChild(_fanFeedLabel);

        // ====================================================
        // 4. EN ALT BÖLÜM: "YÜZEN EL DESTESİ" (FLOATING CARD HAND — Rule 6)
        // 2x2 sağ sütun yerine ekranın altında hafif açılı duran koleksiyon el destesi
        // ====================================================
        var handWrapper = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        handWrapper.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        page.AddChild(handWrapper);

        var handStack = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        handStack.AddThemeConstantOverride("separation", 4);
        handWrapper.AddChild(handStack);

        var handHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        handHeaderRow.AddThemeConstantOverride("separation", 8);
        handStack.AddChild(handHeaderRow);
        handHeaderRow.AddChild(UiComponents.PageHeader("cards", "YÜZEN EL DESTESİ  ·  ANLIK MENAJER HAMLE KARTLARI"));

        _openFullStandingsButton = SecondaryButton("Tam Lig Tablosu & Fikstür");
        _openFullStandingsButton.CustomMinimumSize = new Vector2(180, 28);
        _openFullStandingsButton.Pressed += () => ShowPage(HubPage.World);
        handHeaderRow.AddChild(_openFullStandingsButton);

        _tactileDeckHandRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
            CustomMinimumSize = new Vector2(0, 152),
        };
        _tactileDeckHandRow.AddThemeConstantOverride("separation", 14);
        handStack.AddChild(_tactileDeckHandRow);

        // Arka planda controller bağlamalarını koruyan gizli uyumluluk düğümleri
        var hiddenCompatHost = new VBoxContainer { Visible = false };
        page.AddChild(hiddenCompatHost);

        _continuePreviewLabel = BodyLabel("ContinuePreviewLabel", muted: true, autowrap: true);
        hiddenCompatHost.AddChild(_continuePreviewLabel);
        _blockerLabel = BodyLabel("BlockerLabel", autowrap: true);
        hiddenCompatHost.AddChild(_blockerLabel);
        _pulseLabel = BodyLabel("PulseLabel", muted: true, autowrap: true);
        hiddenCompatHost.AddChild(_pulseLabel);
        _weekStoryLabel = BodyLabel("WeekStoryLabel", autowrap: true);
        hiddenCompatHost.AddChild(_weekStoryLabel);
        _recoveryPathLabel = BodyLabel("RecoveryPathLabel", autowrap: true);
        hiddenCompatHost.AddChild(_recoveryPathLabel);
        _officeLabel = BodyLabel("OfficeLabel", muted: true, autowrap: true);
        hiddenCompatHost.AddChild(_officeLabel);

        _firstWeekGuideCard = hiddenCompatHost;
        _firstWeekGuideLabel = BodyLabel("FirstWeekGuideLabel", autowrap: true);
        hiddenCompatHost.AddChild(_firstWeekGuideLabel);
        _firstWeekGuideOpenButton = PrimaryButton("Ekranı Aç");
        hiddenCompatHost.AddChild(_firstWeekGuideOpenButton);
        _firstWeekGuideNextButton = SecondaryButton("Tamamlandı");
        hiddenCompatHost.AddChild(_firstWeekGuideNextButton);

        _miniStandingsContainer = new VBoxContainer();
        hiddenCompatHost.AddChild(_miniStandingsContainer);

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
        if (_portalCalendarRow is null || _dailyObjectivesBox is null || _portalStreamBox is null || _tactileDeckHandRow is null)
        {
            return;
        }

        var portal = _controller.BuildMobilePortalAndDeck();

        // Hero Kartı Ev Sahibi vs Deplasman Armaları ve Geri Sayımı Güncelle
        RefreshMatchdayHeroShowdown(portal);

        // 1. Render 7-Day Visual Calendar Strip (Vektörel İkonlarla)
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
            tileBox.AddThemeConstantOverride("separation", 2);
            tilePanel.AddChild(tileBox);

            var topRow = new HBoxContainer
            {
                Alignment = BoxContainer.AlignmentMode.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            topRow.AddThemeConstantOverride("separation", 4);
            tileBox.AddChild(topRow);

            topRow.AddChild(new BroadcastVectorIcon(dayTile.Icon, tileColor, 12f));

            var topLabel = new Label
            {
                Text = dayTile.DayBadge,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            CareerUiTheme.StyleTableText(topLabel);
            topLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
            topLabel.AddThemeColorOverride("font_color", tileColor);
            topRow.AddChild(topLabel);

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

        // 2. Render Daily Objectives
        _dailyObjectivesProgressBadge.Text = $"Hedef {portal.CompletedObjectivesCount}/{portal.TotalObjectivesCount} Tamam";
        if (_topQuestBadgeLabel is not null)
        {
            _topQuestBadgeLabel.Text = $"Görev {portal.CompletedObjectivesCount}/{portal.TotalObjectivesCount}";
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

            row.AddChild(new BroadcastVectorIcon(obj.Icon, objColor, 18f));

            var copyBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            copyBox.AddThemeConstantOverride("separation", 2);
            row.AddChild(copyBox);

            var titleLbl = new Label
            {
                Text = $"{obj.Title}  ({obj.RewardBadge})",
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
                ? SecondaryButton("Tamamlandı")
                : PrimaryButton(obj.ActionButtonLabel);
            actionBtn.CustomMinimumSize = new Vector2(118, 28);
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

                itemRow.AddChild(new BroadcastVectorIcon(item.Icon, itemColor, 18f));

                var textCol = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                textCol.AddThemeConstantOverride("separation", 2);
                itemRow.AddChild(textCol);

                var badgeLbl = new Label { Text = item.RoleBadge };
                CareerUiTheme.StyleEyebrow(badgeLbl, itemColor);
                badgeLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
                textCol.AddChild(badgeLbl);

                var headLbl = new Label
                {
                    Text = item.Headline,
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
                btn.CustomMinimumSize = new Vector2(124, 30);
                btn.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                var actionCode = item.ActionCode;
                btn.Pressed += () => ExecutePortalTileAction(actionCode);
                itemRow.AddChild(btn);
            }
        }

        // 4. Render Floating Card Hand (Yüzen El Destesi — Rule 6)
        MatchScreenUi.ClearChildren(_tactileDeckHandRow);
        float[] fanAngles = [-5.5f, -1.8f, 1.8f, 5.5f];
        float[] fanOffsetsY = [6f, 0f, 0f, 6f];

        for (var i = 0; i < portal.TactileDeck.Count; i++)
        {
            var card = portal.TactileDeck[i];
            var angle = i < fanAngles.Length ? fanAngles[i] : 0f;
            var offsetY = i < fanOffsetsY.Length ? fanOffsetsY[i] : 0f;

            var cardColor = card.CardCode switch
            {
                MobilePortalAndDeckDigest.CardEnergyPack => CareerUiTheme.ActionBright,
                MobilePortalAndDeckDigest.CardPepTalk => CareerUiTheme.Accent,
                MobilePortalAndDeckDigest.CardPhysioSpa => CareerUiTheme.Data,
                _ => CareerUiTheme.ColorWarning,
            };

            var handSlot = new Control
            {
                CustomMinimumSize = new Vector2(215, 146),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _tactileDeckHandRow.AddChild(handSlot);

            var tierColor = card.RarityTier.Contains("ALTIN")
                ? CareerUiTheme.ActionBright
                : card.RarityTier.Contains("GÜMÜŞ")
                    ? new Color(0.84f, 0.90f, 0.98f)
                    : new Color(0.85f, 0.55f, 0.34f);

            var cardPanel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(210, 138),
                Position = CareerUiTheme.ReducedMotion ? new Vector2(0, offsetY) : new Vector2(0, offsetY + 18f),
                PivotOffset = new Vector2(105, 70),
                RotationDegrees = 0f,
                Modulate = CareerUiTheme.ReducedMotion ? Colors.White : new Color(1f, 1f, 1f, 0.25f),
            };
            cardPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized: card.RarityTier.Contains("ALTIN")));
            handSlot.AddChild(cardPanel);

            if (!CareerUiTheme.ReducedMotion)
            {
                var introTween = CreateTween().SetParallel(true);
                var delay = i * 0.055d;
                introTween.TweenProperty(cardPanel, "position:y", offsetY, 0.24d)
                    .SetDelay(delay)
                    .SetTrans(Tween.TransitionType.Back)
                    .SetEase(Tween.EaseType.Out);
                introTween.TweenProperty(cardPanel, "rotation_degrees", angle, 0.24d)
                    .SetDelay(delay)
                    .SetTrans(Tween.TransitionType.Back)
                    .SetEase(Tween.EaseType.Out);
                introTween.TweenProperty(cardPanel, "modulate:a", 1f, 0.18d)
                    .SetDelay(delay);
            }

            var cardBox = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            cardBox.AddThemeConstantOverride("separation", 4);
            cardPanel.AddChild(cardBox);

            var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            headerRow.AddThemeConstantOverride("separation", 6);
            cardBox.AddChild(headerRow);

            headerRow.AddChild(new BroadcastVectorIcon(card.Icon, cardColor, 15f));

            var rarityLbl = new Label
            {
                Text = card.RarityTier,
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
            var baseAngle = CareerUiTheme.ReducedMotion ? 0f : angle;
            var baseY = offsetY;

            playCardBtn.MouseEntered += () =>
            {
                if (!CareerUiTheme.ReducedMotion)
                {
                    targetPanel.RotationDegrees = 0f;
                    targetPanel.Position = new Vector2(0, baseY - 6f);
                }
            };
            playCardBtn.MouseExited += () =>
            {
                if (!CareerUiTheme.ReducedMotion)
                {
                    targetPanel.RotationDegrees = baseAngle;
                    targetPanel.Position = new Vector2(0, baseY);
                }
            };

            playCardBtn.Pressed += () =>
            {
                PlayInterfaceFeedback(MatchAudioCue.CardFlip, vibrationDurationMs: 24);
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

    private void RefreshMatchdayHeroShowdown(MobilePortalAndDeckDigest portal)
    {
        if (_matchHeroHomeNameLabel is null || _matchHeroAwayNameLabel is null)
        {
            return;
        }

        var manager = _controller.GetManagerCareer();
        var managedClubId = manager.EmployedClubId;
        var clubName = managedClubId is long cid
            ? _controller.GetClubDisplayName(cid)
            : "KULÜBÜMÜZ";

        var currentDay = _controller.GetCurrentGameDate().DayNumber;
        var nextFixture = _controller.GetNextDueManagedFixture(currentDay);
        var matchTile = portal.CalendarStrip.FirstOrDefault(t => t.IsMatchDay);

        if (nextFixture is not null)
        {
            var isHome = nextFixture.IsHome;
            var oppName = _controller.GetClubDisplayName(nextFixture.OpponentClubId);
            var homeId = isHome ? managedClubId : nextFixture.OpponentClubId;
            var awayId = isHome ? nextFixture.OpponentClubId : managedClubId;
            var homeName = isHome ? clubName : oppName;
            var awayName = isHome ? oppName : clubName;

            _matchHeroHomeNameLabel.Text = homeName.ToUpperInvariant();
            _matchHeroAwayNameLabel.Text = awayName.ToUpperInvariant();
            _matchHeroHomeCrest.SetClub(homeId, homeName, KitVariantType.Home);
            _matchHeroAwayCrest.SetClub(awayId, awayName, KitVariantType.Away);
            _matchHeroCountdownLabel.Text = "BUGÜN MAÇ GÜNÜ  ·  CANLI YAYIN HAZIR";

            var homeIdent = ClubBrandingVisuals.ResolveClubIdentity(homeId, homeName);
            var awayIdent = ClubBrandingVisuals.ResolveClubIdentity(awayId, awayName);
            _matchHeroPanel?.AddThemeStyleboxOverride("panel", CareerUiTheme.HeroShowdownPanel(homeIdent.PrimaryColor, awayIdent.PrimaryColor));
        }
        else if (matchTile is not null)
        {
            var oppRaw = matchTile.Title.Replace("MAÇ:", "", StringComparison.OrdinalIgnoreCase).Trim();
            _matchHeroHomeNameLabel.Text = clubName.ToUpperInvariant();
            _matchHeroAwayNameLabel.Text = oppRaw.ToUpperInvariant();
            _matchHeroHomeCrest.SetClub(managedClubId, clubName, KitVariantType.Home);
            _matchHeroAwayCrest.SetClub(null, oppRaw, KitVariantType.Away);
            _matchHeroCountdownLabel.Text = matchTile.DayOffset == 0
                ? "BUGÜN MAÇ GÜNÜ"
                : $"GERİ SAYIM: MAÇA {matchTile.DayOffset} GÜN";

            var homeIdent = ClubBrandingVisuals.ResolveClubIdentity(managedClubId, clubName);
            var awayIdent = ClubBrandingVisuals.ResolveClubIdentity(null, oppRaw);
            _matchHeroPanel?.AddThemeStyleboxOverride("panel", CareerUiTheme.HeroShowdownPanel(homeIdent.PrimaryColor, awayIdent.PrimaryColor));
        }
        else
        {
            _matchHeroHomeNameLabel.Text = clubName.ToUpperInvariant();
            _matchHeroAwayNameLabel.Text = "SIRADAKİ RAKİP";
            _matchHeroHomeCrest.SetClub(managedClubId, clubName, KitVariantType.Home);
            _matchHeroAwayCrest.SetBadgeText("SL");
            _matchHeroCountdownLabel.Text = "FİKSTÜR TAKİBİ";

            var homeIdent = ClubBrandingVisuals.ResolveClubIdentity(managedClubId, clubName);
            _matchHeroPanel?.AddThemeStyleboxOverride("panel", CareerUiTheme.HeroShowdownPanel(homeIdent.PrimaryColor, null));
        }
    }

    private static string ExtractInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "FK";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
        }

        return name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
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

    /// <summary>
    /// Maç Günü Hero Kartı için kulüp armasını (Crest) ve yanında mini maç formasını (Kit) gösteren Broadcast rozet kontrolü.
    /// </summary>
    private sealed partial class BroadcastCrestBadge : Control
    {
        private string _initials;
        private Color _primary;
        private Color _secondary;
        private string _pattern = "Stripes";
        private Texture2D? _crestTexture;
        private Texture2D? _kitTexture;

        public BroadcastCrestBadge(string initials, Color primary, Color secondary)
        {
            _initials = initials;
            _primary = primary;
            _secondary = secondary;
            CustomMinimumSize = new Vector2(86, 64);
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public void SetClub(long? clubId, string? clubName, KitVariantType kitVariant = KitVariantType.Home)
        {
            var id = ClubBrandingVisuals.ResolveClubIdentity(clubId, clubName);
            _initials = id.ShortCode;
            _primary = kitVariant == KitVariantType.Away ? id.SecondaryColor : id.PrimaryColor;
            _secondary = kitVariant == KitVariantType.Away ? id.PrimaryColor : id.SecondaryColor;
            _pattern = id.KitPattern;
            _crestTexture = ClubBrandingVisuals.TryLoadTexture(id.CrestResourcePath);
            var kitPath = kitVariant == KitVariantType.Away ? id.AwayKitResourcePath : id.HomeKitResourcePath;
            _kitTexture = ClubBrandingVisuals.TryLoadTexture(kitPath);
            QueueRedraw();
        }

        public void SetBadgeText(string initials)
        {
            _initials = initials;
            _crestTexture = null;
            _kitTexture = null;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var h = Size.Y;
            var crestW = 54f;

            if (_crestTexture is not null)
            {
                DrawCircle(new Vector2(crestW * 0.5f, h * 0.5f), 26f, new Color(_primary.R, _primary.G, _primary.B, 0.22f));
                DrawTextureRect(_crestTexture, new Rect2(4f, (h - 46f) * 0.5f, 46f, 46f), false);
            }
            else
            {
                Vector2[] outerShield =
                [
                    new Vector2(crestW * 0.50f, 3f),
                    new Vector2(crestW - 4f, h * 0.20f),
                    new Vector2(crestW - 7f, h * 0.66f),
                    new Vector2(crestW * 0.50f, h - 3f),
                    new Vector2(7f, h * 0.66f),
                    new Vector2(4f, h * 0.20f),
                ];
                DrawColoredPolygon(outerShield, _primary.Darkened(0.15f));

                Vector2[] leftHalf =
                [
                    new Vector2(crestW * 0.50f, 6f),
                    new Vector2(crestW * 0.50f, h - 6f),
                    new Vector2(9f, h * 0.64f),
                    new Vector2(6f, h * 0.22f),
                ];
                DrawColoredPolygon(leftHalf, new Color(_secondary.R, _secondary.G, _secondary.B, 0.65f));

                DrawRect(new Rect2(8f, h * 0.38f, crestW - 16f, 16f), new Color(0.04f, 0.07f, 0.12f, 0.85f), true);

                Vector2[] outline =
                [
                    outerShield[0], outerShield[1], outerShield[2],
                    outerShield[3], outerShield[4], outerShield[5], outerShield[0],
                ];
                DrawPolyline(outline, CareerUiTheme.Accent, 2.0f);

                var font = ThemeDB.FallbackFont;
                var textSize = font.GetStringSize(_initials, HorizontalAlignment.Center, -1, 13);
                DrawString(
                    font,
                    new Vector2((crestW - textSize.X) * 0.5f, (h * 0.50f) + (textSize.Y * 0.26f)),
                    _initials,
                    HorizontalAlignment.Center,
                    -1,
                    13,
                    Colors.White);
            }

            // Yanında Mini Maç Forması (Kit Preview)
            var kitCenter = new Vector2(69f, h * 0.54f);
            if (_kitTexture is not null)
            {
                DrawTextureRect(_kitTexture, new Rect2(54f, (h - 32f) * 0.5f, 30f, 32f), false);
            }
            else
            {
                var s = 0.68f;
                var torso = new Vector2[]
                {
                    kitCenter + new Vector2(-8f * s, -15f * s),
                    kitCenter + new Vector2(8f * s, -15f * s),
                    kitCenter + new Vector2(18f * s, -7f * s),
                    kitCenter + new Vector2(14f * s, 0f),
                    kitCenter + new Vector2(10f * s, -3f * s),
                    kitCenter + new Vector2(10f * s, 16f * s),
                    kitCenter + new Vector2(-10f * s, 16f * s),
                    kitCenter + new Vector2(-10f * s, -3f * s),
                    kitCenter + new Vector2(-14f * s, 0f),
                    kitCenter + new Vector2(-18f * s, -7f * s),
                };
                DrawColoredPolygon(torso, _primary);
                if (string.Equals(_pattern, "Stripes", StringComparison.OrdinalIgnoreCase))
                {
                    DrawRect(new Rect2(kitCenter.X - 6f * s, kitCenter.Y - 13f * s, 3.2f * s, 28f * s), _secondary, true);
                    DrawRect(new Rect2(kitCenter.X + 2.8f * s, kitCenter.Y - 13f * s, 3.2f * s, 28f * s), _secondary, true);
                }
                else if (string.Equals(_pattern, "Halves", StringComparison.OrdinalIgnoreCase))
                {
                    DrawRect(new Rect2(kitCenter.X, kitCenter.Y - 14f * s, 10f * s, 29f * s), _secondary, true);
                }
                for (var i = 0; i < torso.Length; i++)
                {
                    DrawLine(torso[i], torso[(i + 1) % torso.Length], new Color(1f, 1f, 1f, 0.45f), 1.1f, true);
                }
            }
        }
    }
}
