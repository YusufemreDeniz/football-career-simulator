using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;
using static FootballCareerSimulator.Presentation.CareerHubCopyFormatting;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private Control BuildTodayPage()
    {
        var page = PageRoot();
        page.SizeFlagsVertical = SizeFlags.ExpandFill;

        // ----------------------------------------------------
        // 3-COLUMN BROADCAST BENTO DASHBOARD (SM27 / FM27 MOBILE STYLE)
        // Col 1: Sıradaki Maç, Taktik Hazırlık & Takvim Akışı
        // Col 2: Karar Masası, Kulüp Gündemi & Taraftar Nabzı
        // Col 3: Canlı Süper Lig Puan Durumu Özeti
        // ----------------------------------------------------
        var bentoRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        bentoRow.AddThemeConstantOverride("separation", 10);
        page.AddChild(bentoRow);

        // ====================================================
        // COLUMN 1: NEXT MATCH HERO + TEAM READINESS + TIME FLOW
        // ====================================================
        var col1 = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.35f,
        };
        col1.AddThemeConstantOverride("separation", 8);
        bentoRow.AddChild(col1);

        var matchNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        matchNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        col1.AddChild(matchNeonPanel);

        var matchCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        matchCard.AddThemeConstantOverride("separation", 6);
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
        _nextMatchOpponentLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(19));
        _nextMatchOpponentLabel.Text = "vs —";
        matchCard.AddChild(_nextMatchOpponentLabel);

        // Sub-headline / Briefing
        _briefingLabel = BodyLabel("BriefingLabel", muted: true, autowrap: true);
        matchCard.AddChild(_briefingLabel);

        _selectionLabel = BodyLabel("SelectionLabel", muted: true, autowrap: true);
        _selectionLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _selectionLabel.Visible = false;
        matchCard.AddChild(_selectionLabel);

        // Integrated Tactical & Fitness Readiness Strip
        var tacticsStripPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tacticsStripPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        matchCard.AddChild(tacticsStripPanel);

        var tacticsCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tacticsCard.AddThemeConstantOverride("separation", 4);
        tacticsStripPanel.AddChild(tacticsCard);

        _todayTacticsSummaryLabel = BodyLabel("TodayTacticsSummary", autowrap: true);
        _todayTacticsSummaryLabel.Text = "⚽ 4-4-2 · Dengeli · Dengeli Pres";
        _todayTacticsSummaryLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        tacticsCard.AddChild(_todayTacticsSummaryLabel);

        _todayFitnessSummaryLabel = BodyLabel("TodayFitnessSummary", muted: true, autowrap: true);
        _todayFitnessSummaryLabel.Text = "⚡ Kondisyon Özeti Hazırlanıyor";
        tacticsCard.AddChild(_todayFitnessSummaryLabel);

        // Match & Tactical Quick Actions
        var primaryRow = ActionFlow();
        matchCard.AddChild(primaryRow);
        _openTacticsButton = SecondaryButton("🎯 Taktik & 11");
        _openTacticsButton.Pressed += () => ShowPage(HubPage.Prep);
        primaryRow.AddChild(_openTacticsButton);

        _approveSelectionButton = AccentButton("✓ Kadro Onayla");
        _approveSelectionButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        primaryRow.AddChild(_approveSelectionButton);

        _playButton = PrimaryButton("⚽ MAÇA GİT");
        _playButton.Pressed += OnPlayMatches;
        primaryRow.AddChild(_playButton);

        _swapSelectionButton = SecondaryButton("⇄ XI ↔ Yedek");
        _swapSelectionButton.Pressed += () => Apply(_controller.SwapLastStarterWithFirstBenchForNextDueMatch());
        primaryRow.AddChild(_swapSelectionButton);

        _restEntireTeamButton = SecondaryButton("🛌 Dinlendir");
        _restEntireTeamButton.TooltipText = "Yoğun takvimde ağır dinlenme planı uygular; sakatlık riskini ve yorgunluğu hedefler.";
        _restEntireTeamButton.Pressed += () => Apply(_controller.RestEntireTeam());
        primaryRow.AddChild(_restEntireTeamButton);

        _lightRegenerationButton = SecondaryButton("♻ Hafif İdman");
        _lightRegenerationButton.TooltipText = "Maç ritmini koruyan düşük yoğunluklu toparlanma planı uygular.";
        _lightRegenerationButton.Pressed += () => Apply(_controller.ApplyLightRegeneration());
        primaryRow.AddChild(_lightRegenerationButton);

        // Compact Next Step & Calendar Controls Card
        var continueNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        continueNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        col1.AddChild(continueNeonPanel);

        var continueCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        continueCard.AddThemeConstantOverride("separation", 6);
        continueNeonPanel.AddChild(continueCard);

        var continueHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        continueHeaderRow.AddThemeConstantOverride("separation", 6);
        continueCard.AddChild(continueHeaderRow);
        continueHeaderRow.AddChild(UiComponents.PageHeader("⏭", "TAKVİM & SIRADAKİ ADIM"));

        _continuePreviewLabel = BodyLabel("ContinuePreviewLabel", muted: true, autowrap: true);
        continueCard.AddChild(_continuePreviewLabel);

        var timeRow = ActionFlow();
        continueCard.AddChild(timeRow);

        _officeNextStepButton = PrimaryButton("▶ DEVAM ET");
        _officeNextStepButton.CustomMinimumSize = new Vector2(120, 32);
        _officeNextStepButton.Visible = false;
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
        // COLUMN 2: DECISION DESK + CLUB DISPATCH & FAN PULSE
        // ====================================================
        var col2 = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.2f,
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
        decisionCard.AddChild(UiComponents.PageHeader("⚖️", "KARAR MASASI"));

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

        // Club Dispatch & Fan Pulse Card
        var newsNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        newsNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        col2.AddChild(newsNeonPanel);

        var newsCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        newsCard.AddThemeConstantOverride("separation", 6);
        newsNeonPanel.AddChild(newsCard);

        var newsHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        newsHeaderRow.AddThemeConstantOverride("separation", 6);
        newsCard.AddChild(newsHeaderRow);
        newsHeaderRow.AddChild(UiComponents.PageHeader("📰", "GÜNDEM & KULÜP NABZI"));

        _fanApprovalBadgePanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _fanApprovalBadgePanel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.18f));
        _fanApprovalBadgeLabel = new Label { Text = "♥ %50" };
        CareerUiTheme.StyleTableText(_fanApprovalBadgeLabel);
        _fanApprovalBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _fanApprovalBadgePanel.AddChild(_fanApprovalBadgeLabel);
        newsHeaderRow.AddChild(_fanApprovalBadgePanel);

        _fanFeedLabel = BodyLabel("FanFeedLabel", autowrap: true);
        _fanFeedLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        newsCard.AddChild(_fanFeedLabel);

        _blockerLabel = BodyLabel("BlockerLabel", autowrap: true);
        newsCard.AddChild(_blockerLabel);
        _pulseLabel = BodyLabel("PulseLabel", muted: true, autowrap: true);
        newsCard.AddChild(_pulseLabel);

        _weekStoryLabel = BodyLabel("WeekStoryLabel", autowrap: true);
        _weekStoryLabel.Visible = false;
        newsCard.AddChild(_weekStoryLabel);

        _recoveryPathLabel = BodyLabel("RecoveryPathLabel", autowrap: true);
        _recoveryPathLabel.Visible = false;
        newsCard.AddChild(_recoveryPathLabel);

        _officeLabel = BodyLabel("OfficeLabel", muted: true, autowrap: true);
        _officeLabel.Text = CompactOfficeText(Application.Competition.Queries.PostMatchOfficeDigest.Quiet());
        _officeLabel.Visible = false;
        newsCard.AddChild(_officeLabel);

        // First Week Guide (Compact strip inside Col 2)
        var guideHost = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        col2.AddChild(guideHost);
        _firstWeekGuideCard = guideHost;
        var guideNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        guideNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        guideHost.AddChild(guideNeonPanel);
        var guideCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        guideCard.AddThemeConstantOverride("separation", 4);
        guideNeonPanel.AddChild(guideCard);
        guideCard.AddChild(UiComponents.PageHeader("📚", "MENAJER REHBERİ"));

        _firstWeekGuideLabel = BodyLabel("FirstWeekGuideLabel", autowrap: true);
        guideCard.AddChild(_firstWeekGuideLabel);
        var guideRow = ActionFlow();
        guideCard.AddChild(guideRow);
        _firstWeekGuideOpenButton = PrimaryButton("Ekranı Aç");
        _firstWeekGuideOpenButton.Pressed += OpenCurrentGuideStep;
        guideRow.AddChild(_firstWeekGuideOpenButton);
        _firstWeekGuideNextButton = SecondaryButton("Tamamlandı →");
        _firstWeekGuideNextButton.Pressed += CompleteCurrentGuideStep;
        guideRow.AddChild(_firstWeekGuideNextButton);

        // ====================================================
        // COLUMN 3: LIVE MINI STANDINGS WIDGET
        // ====================================================
        var standingsNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.05f,
        };
        standingsNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        bentoRow.AddChild(standingsNeonPanel);

        var standingsCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        standingsCard.AddThemeConstantOverride("separation", 6);
        standingsNeonPanel.AddChild(standingsCard);
        standingsCard.AddChild(UiComponents.PageHeader("📊", "SÜPER LİG TABLOSU"));

        _miniStandingsContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _miniStandingsContainer.AddThemeConstantOverride("separation", 4);
        standingsCard.AddChild(_miniStandingsContainer);

        _openFullStandingsButton = SecondaryButton("📊 Tam Tablo & Fikstür →");
        _openFullStandingsButton.Pressed += () => ShowPage(HubPage.World);
        standingsCard.AddChild(_openFullStandingsButton);

        return page;
    }
}
