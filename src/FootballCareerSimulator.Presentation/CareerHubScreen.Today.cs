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

        // ----------------------------------------------------
        // BENTO ROW 1: NEXT MATCH HERO CARD + MINI STANDINGS WIDGET
        // ----------------------------------------------------
        var row1 = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row1.AddThemeConstantOverride("separation", 12);
        page.AddChild(row1);

        // Next Match Hero Card — referans: güçlü neon çerçeve, glow efekti
        var matchNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        matchNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        matchNeonPanel.SizeFlagsStretchRatio = 1.4f;
        row1.AddChild(matchNeonPanel);

        var matchCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        matchCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        matchNeonPanel.AddChild(matchCard);

        // Başlık
        matchCard.AddChild(UiComponents.PageHeader("⚽", "SIRADAKİ MAÇ"));
        matchCard.AddChild(UiComponents.Divider(1));

        // Top meta row: Badge + Venue
        var matchMetaRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        matchMetaRow.AddThemeConstantOverride("separation", 8);
        matchCard.AddChild(matchMetaRow);

        _nextMatchCompetitionBadge = BodyLabel("NextMatchCompetitionBadge");
        _nextMatchCompetitionBadge.Text = "🏆 LİG MAÇI";
        _nextMatchCompetitionBadge.AddThemeColorOverride("font_color", CareerUiTheme.Data);
        CareerUiTheme.ApplyDisplayFont(_nextMatchCompetitionBadge);
        matchMetaRow.AddChild(_nextMatchCompetitionBadge);

        _nextMatchVenuePill = new PanelContainer();
        _nextMatchVenuePill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Stroke));
        _nextMatchVenueLabel = BodyLabel("NextMatchVenueLabel");
        _nextMatchVenueLabel.Text = "STADYUM";
        _nextMatchVenuePill.AddChild(_nextMatchVenueLabel);
        matchMetaRow.AddChild(_nextMatchVenuePill);

        // Opponent Hero Display
        _nextMatchOpponentLabel = TitleLabel("NextMatchOpponentLabel");
        _nextMatchOpponentLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _nextMatchOpponentLabel.Text = "vs —";
        matchCard.AddChild(_nextMatchOpponentLabel);

        // Sub-headline / Briefing
        _briefingLabel = BodyLabel("BriefingLabel", autowrap: true);
        matchCard.AddChild(_briefingLabel);

        // Selection / Approval status pill & hint
        var approvalRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        approvalRow.AddThemeConstantOverride("separation", 8);
        matchCard.AddChild(approvalRow);

        _nextMatchApprovalPill = new PanelContainer();
        _nextMatchApprovalPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning));
        _nextMatchApprovalLabel = BodyLabel("NextMatchApprovalLabel");
        _nextMatchApprovalLabel.Text = "⚠️ Kadro Onayı Bekliyor";
        _nextMatchApprovalLabel.AddThemeColorOverride("font_color", CareerUiTheme.ColorWarning);
        _nextMatchApprovalPill.AddChild(_nextMatchApprovalLabel);
        approvalRow.AddChild(_nextMatchApprovalPill);

        _selectionLabel = BodyLabel("SelectionLabel", muted: true, autowrap: true);
        _selectionLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _selectionLabel.Visible = false;
        approvalRow.AddChild(_selectionLabel);

        // Match Actions
        var primaryRow = ActionFlow();
        matchCard.AddChild(primaryRow);
        _approveSelectionButton = AccentButton("✓ ONAYLA");
        _approveSelectionButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        primaryRow.AddChild(_approveSelectionButton);
        _playButton = PrimaryButton("⚽ MAÇA GİT");
        _playButton.Pressed += OnPlayMatches;
        primaryRow.AddChild(_playButton);
        _swapSelectionButton = SecondaryButton("⇄ XI ↔ Yedek");
        _swapSelectionButton.Pressed += () => Apply(_controller.SwapLastStarterWithFirstBenchForNextDueMatch());
        primaryRow.AddChild(_swapSelectionButton);

        // Mini Standings Card — referans: neon kenarlık
        var standingsNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        standingsNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        standingsNeonPanel.SizeFlagsStretchRatio = 1.0f;
        row1.AddChild(standingsNeonPanel);

        var standingsCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        standingsCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        standingsNeonPanel.AddChild(standingsCard);
        standingsCard.AddChild(UiComponents.PageHeader("📊", "LİG DURUMU"));
        standingsCard.AddChild(UiComponents.Divider(1));

        _miniStandingsContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _miniStandingsContainer.AddThemeConstantOverride("separation", 4);
        standingsCard.AddChild(_miniStandingsContainer);

        _openFullStandingsButton = SecondaryButton("📊 Tüm Puan Durumu →");
        _openFullStandingsButton.Pressed += () => ShowPage(HubPage.World);
        standingsCard.AddChild(_openFullStandingsButton);

        // ----------------------------------------------------
        // BENTO ROW 2: TACTICS & FITNESS WIDGET + CLUB DISPATCH & PULSE
        // ----------------------------------------------------
        var row2 = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row2.AddThemeConstantOverride("separation", 12);
        page.AddChild(row2);

        // Tactics & Fitness Card — neon panel
        var tacticsNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        tacticsNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        tacticsNeonPanel.SizeFlagsStretchRatio = 1.0f;
        row2.AddChild(tacticsNeonPanel);

        var tacticsCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tacticsCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        tacticsNeonPanel.AddChild(tacticsCard);
        tacticsCard.AddChild(UiComponents.PageHeader("🎯", "TAKTİK"));
        tacticsCard.AddChild(UiComponents.Divider(1));

        _todayTacticsSummaryLabel = BodyLabel("TodayTacticsSummary", autowrap: true);
        _todayTacticsSummaryLabel.Text = "⚽ 4-3-3 · Dengeli · Dengeli Pres";
        tacticsCard.AddChild(_todayTacticsSummaryLabel);

        _todayFitnessSummaryLabel = BodyLabel("TodayFitnessSummary", muted: true, autowrap: true);
        _todayFitnessSummaryLabel.Text = "⚡ Kondisyon Özeti Hazırlanıyor";
        tacticsCard.AddChild(_todayFitnessSummaryLabel);

        _openTacticsButton = SecondaryButton("Taktiğe Git →");
        _openTacticsButton.Pressed += () => ShowPage(HubPage.Prep);
        tacticsCard.AddChild(_openTacticsButton);

        var recoveryActions = ActionFlow();
        tacticsCard.AddChild(recoveryActions);
        _restEntireTeamButton = AccentButton("🛌 Dinlendir");
        _restEntireTeamButton.TooltipText = "Yoğun takvimde ağır dinlenme planı uygular; sakatlık riskini ve yorgunluğu hedefler.";
        _restEntireTeamButton.Pressed += () => Apply(_controller.RestEntireTeam());
        recoveryActions.AddChild(_restEntireTeamButton);
        _lightRegenerationButton = SecondaryButton("♻ Hafif Çalışma");
        _lightRegenerationButton.TooltipText = "Maç ritmini koruyan düşük yoğunluklu toparlanma planı uygular.";
        _lightRegenerationButton.Pressed += () => Apply(_controller.ApplyLightRegeneration());
        recoveryActions.AddChild(_lightRegenerationButton);

        // Club Dispatch & Pulse Card — güçlü neon (emphasized)
        var newsNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        newsNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        newsNeonPanel.SizeFlagsStretchRatio = 1.4f;
        row2.AddChild(newsNeonPanel);

        var newsCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        newsCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        newsNeonPanel.AddChild(newsCard);
        newsCard.AddChild(UiComponents.PageHeader("📰", "GÜNDEM"));
        newsCard.AddChild(UiComponents.Divider(1));

        var fanPulseRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        fanPulseRow.AddThemeConstantOverride("separation", 8);
        newsCard.AddChild(fanPulseRow);
        _fanApprovalBadgePanel = new PanelContainer();
        _fanApprovalBadgePanel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
        _fanApprovalBadgeLabel = new Label { Text = "♥ %50" };
        CareerUiTheme.StyleHeadline(_fanApprovalBadgeLabel);
        _fanApprovalBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _fanApprovalBadgePanel.AddChild(_fanApprovalBadgeLabel);
        fanPulseRow.AddChild(_fanApprovalBadgePanel);
        _fanFeedLabel = BodyLabel("FanFeedLabel", autowrap: true);
        _fanFeedLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        fanPulseRow.AddChild(_fanFeedLabel);

        _blockerLabel = BodyLabel("BlockerLabel", autowrap: true);
        newsCard.AddChild(_blockerLabel);
        _pulseLabel = BodyLabel("PulseLabel", autowrap: true);
        newsCard.AddChild(_pulseLabel);

        _weekStoryLabel = BodyLabel("WeekStoryLabel", autowrap: true);
        _weekStoryLabel.Visible = false;
        newsCard.AddChild(_weekStoryLabel);

        _recoveryPathLabel = BodyLabel("RecoveryPathLabel", autowrap: true);
        _recoveryPathLabel.Visible = false;
        newsCard.AddChild(_recoveryPathLabel);

        _officeLabel = BodyLabel("OfficeLabel", muted: true, autowrap: true);
        _officeLabel.Text = CompactOfficeText(Application.Competition.Queries.PostMatchOfficeDigest.Quiet());
        newsCard.AddChild(_officeLabel);

        _officeNextStepButton = PrimaryButton("Sıradaki Adım");
        _officeNextStepButton.Visible = false;
        _officeNextStepButton.Pressed += OnOfficeNextStepPressed;
        newsCard.AddChild(_officeNextStepButton);

        // ----------------------------------------------------
        // BENTO ROW 3: MANAGEMENT OPERATIONS & TIMELINE ADVANCE
        // ----------------------------------------------------
        var row3 = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row3.AddThemeConstantOverride("separation", 12);
        page.AddChild(row3);

        // Decision Desk Card — neon panel
        var decisionNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        decisionNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        decisionNeonPanel.SizeFlagsStretchRatio = 1.4f;
        row3.AddChild(decisionNeonPanel);

        var decisionCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        decisionCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        decisionNeonPanel.AddChild(decisionCard);
        decisionCard.AddChild(UiComponents.PageHeader("⚖️", "KARARLAR"));
        decisionCard.AddChild(UiComponents.Divider(1));

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
        _grantDecisionButton = AccentButton("✓ KABUL ET");
        _grantDecisionButton.Pressed += () => ApplyDecision(
            () => _controller.AnswerOldestPendingDecision(grantPromise: true));
        deskRow.AddChild(_grantDecisionButton);
        _refuseDecisionButton = DangerButton("✕ Talebi Reddet");
        _refuseDecisionButton.Pressed += () => ApplyDecision(
            () => _controller.AnswerOldestPendingDecision(grantPromise: false));
        deskRow.AddChild(_refuseDecisionButton);
        _disciplineWarningButton = SecondaryButton("⚠ UYARI VER");
        _disciplineWarningButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionIssueWarning));
        deskRow.AddChild(_disciplineWarningButton);
        _disciplineFineButton = DangerButton("⛔ CEZA VER");
        _disciplineFineButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionIssueFine));
        deskRow.AddChild(_disciplineFineButton);
        _disciplineSupportButton = PrimaryButton("♥ DESTEKLE");
        _disciplineSupportButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionOfferSupport));
        deskRow.AddChild(_disciplineSupportButton);
        _boardCounterButton = AccentButton("⇄ KARŞI TEKLİF");
        _boardCounterButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(
                Domain.Interaction.DecisionRequest.OptionCounterBoardDemand));
        deskRow.AddChild(_boardCounterButton);
        _pressCriticizeButton = DangerButton("🎙 Kamuya Eleştir");
        _pressCriticizeButton.Pressed += () =>
            ApplyDecision(() => _controller.AnswerOldestPendingWithOption(
                Domain.Interaction.DecisionRequest.OptionPubliclyCriticize));
        deskRow.AddChild(_pressCriticizeButton);

        // Timeline & Season Card — neon panel
        var seasonTimeNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        seasonTimeNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        seasonTimeNeonPanel.SizeFlagsStretchRatio = 1.0f;
        row3.AddChild(seasonTimeNeonPanel);

        var seasonTimeCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        seasonTimeCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        seasonTimeNeonPanel.AddChild(seasonTimeCard);
        seasonTimeCard.AddChild(UiComponents.PageHeader("⏰", "İLERLET"));
        seasonTimeCard.AddChild(UiComponents.Divider(1));

        _seasonTransitionButton = AccentButton("🏁 Yeni Sezon");
        _seasonTransitionButton.Pressed += () => Apply(_controller.TransitionToNextSeason());
        seasonTimeCard.AddChild(_seasonTransitionButton);

        var timeRow = ActionFlow();
        seasonTimeCard.AddChild(timeRow);
        _advanceDayButton = SecondaryButton("▶ 1 Gün");
        _advanceDayButton.Pressed += () => Apply(_controller.AdvanceDays(1));
        timeRow.AddChild(_advanceDayButton);
        _advanceWeekButton = SecondaryButton("⏩ 7 Gün");
        _advanceWeekButton.Pressed += () => Apply(_controller.AdvanceDays(7));
        timeRow.AddChild(_advanceWeekButton);

        // First Week Guide (Conditional host at bottom of page)
        var guideHost = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        page.AddChild(guideHost);
        _firstWeekGuideCard = guideHost;
        var guideNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        guideNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        guideHost.AddChild(guideNeonPanel);
        var guideCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        guideCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        guideNeonPanel.AddChild(guideCard);
        guideCard.AddChild(UiComponents.PageHeader("📚", "BAŞLANGIÇ"));
        guideCard.AddChild(UiComponents.Divider(1));

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

        RegisterMobileFocusLayout(
            page,
            HubPage.Today,
            new Control[] { standingsNeonPanel, tacticsNeonPanel, guideNeonPanel },
            ("MAÇ", new Control[] { matchNeonPanel }),
            ("GÜNDEM", new Control[] { newsNeonPanel }),
            ("KARAR", new Control[] { decisionNeonPanel }),
            ("ZAMAN", new Control[] { seasonTimeNeonPanel }));

        return page;
    }

}
