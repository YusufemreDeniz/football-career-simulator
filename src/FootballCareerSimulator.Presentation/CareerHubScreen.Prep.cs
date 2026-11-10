using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private Button _prepTabTacticsBtn = null!;
    private Button _prepTabOpponentBtn = null!;
    private Button _prepTabTrainingBtn = null!;
    private Control _prepTacticsPanelHost = null!;
    private Control _prepOpponentPanelHost = null!;
    private Control _prepTrainingPanelHost = null!;

    private Control BuildPrepPage()
    {
        var page = PageRoot();
        page.SizeFlagsVertical = SizeFlags.ExpandFill;

        // ----------------------------------------------------
        // SINGLE COMPACT TOP BAR: RIGHT-PANEL TABS (LEFT) + READINESS TELEMETRY (RIGHT)
        // ----------------------------------------------------
        var topControlRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topControlRow.AddThemeConstantOverride("separation", 6);
        page.AddChild(topControlRow);

        var tabRow = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        tabRow.AddThemeConstantOverride("separation", 4);
        topControlRow.AddChild(tabRow);

        _prepTabTacticsBtn = new Button { Text = "🕹️ TAKTİK & PLAN" };
        _prepTabTacticsBtn.Pressed += () => ShowPrepSubTab(0);
        CareerUiTheme.StyleTabButton(_prepTabTacticsBtn, selected: true);
        tabRow.AddChild(_prepTabTacticsBtn);

        _prepTabOpponentBtn = new Button { Text = "🔍 RAKİP ANALİZİ" };
        _prepTabOpponentBtn.Pressed += () => ShowPrepSubTab(1);
        CareerUiTheme.StyleTabButton(_prepTabOpponentBtn, selected: false);
        tabRow.AddChild(_prepTabOpponentBtn);

        _prepTabTrainingBtn = new Button { Text = "⚡ ANTRENMAN & MAÇ ÖNCELİĞİ" };
        _prepTabTrainingBtn.Pressed += () => ShowPrepSubTab(2);
        CareerUiTheme.StyleTabButton(_prepTabTrainingBtn, selected: false);
        tabRow.AddChild(_prepTabTrainingBtn);

        var topSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topControlRow.AddChild(topSpacer);

        _prepReadinessBar = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _prepReadinessBar.AddThemeConstantOverride("separation", 5);
        topControlRow.AddChild(_prepReadinessBar);

        _prepFitnessBadgePanel = new PanelContainer();
        _prepFitnessBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
        _prepFitnessBadgeLabel = new Label { Text = "⚡ XI Zindelik: %—" };
        CareerUiTheme.StyleTableText(_prepFitnessBadgeLabel);
        _prepFitnessBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _prepFitnessBadgePanel.AddChild(_prepFitnessBadgeLabel);
        _prepReadinessBar.AddChild(_prepFitnessBadgePanel);

        _prepSharpnessBadgePanel = new PanelContainer();
        _prepSharpnessBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.16f));
        _prepSharpnessBadgeLabel = new Label { Text = "🎯 Maç Modifiyesi: —" };
        CareerUiTheme.StyleTableText(_prepSharpnessBadgeLabel);
        _prepSharpnessBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _prepSharpnessBadgePanel.AddChild(_prepSharpnessBadgeLabel);
        _prepReadinessBar.AddChild(_prepSharpnessBadgePanel);

        _prepRiskBadgePanel = new PanelContainer();
        _prepRiskBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data, 0.16f));
        _prepRiskBadgeLabel = new Label { Text = "✓ Kadro Zinde" };
        CareerUiTheme.StyleTableText(_prepRiskBadgeLabel);
        _prepRiskBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Data);
        _prepRiskBadgePanel.AddChild(_prepRiskBadgeLabel);
        _prepReadinessBar.AddChild(_prepRiskBadgePanel);

        _prepApprovalBadgePanel = new PanelContainer();
        _prepApprovalBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Stroke, 0.20f));
        _prepApprovalBadgeLabel = new Label { Text = "⚠️ Onay Bekliyor" };
        CareerUiTheme.StyleTableText(_prepApprovalBadgeLabel);
        _prepApprovalBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.TextPrimary);
        _prepApprovalBadgePanel.AddChild(_prepApprovalBadgeLabel);
        _prepReadinessBar.AddChild(_prepApprovalBadgePanel);

        // ====================================================
        // WIDESCREEN 2-COLUMN SPLIT:
        // Left: Permanent 2D Tactical Pitch + Bench + Quick Selection Actions
        // Right: Active Sub-Tab (Tactics & Game Plan | Opponent Analysis | Training)
        // ====================================================
        var prepSplitRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        prepSplitRow.AddThemeConstantOverride("separation", 10);
        page.AddChild(prepSplitRow);

        // ── LEFT COLUMN: PERMANENT 2D PITCH & STARTING XI ──────────────────────
        var pitchNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.15f,
        };
        pitchNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        prepSplitRow.AddChild(pitchNeonPanel);

        var pitchCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        pitchCard.AddThemeConstantOverride("separation", 6);
        pitchNeonPanel.AddChild(pitchCard);

        var pitchHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        pitchHeaderRow.AddThemeConstantOverride("separation", 6);
        pitchCard.AddChild(pitchHeaderRow);
        pitchHeaderRow.AddChild(UiComponents.PageHeader("🟩", "SAHA DİZİLİŞİ & İLK 11"));

        _prepApproveSelectionButton = AccentButton("✓ Kadro Onayla");
        _prepApproveSelectionButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        pitchHeaderRow.AddChild(_prepApproveSelectionButton);

        _prepSwapSelectionButton = SecondaryButton("⇄ XI ↔ Yedek");
        _prepSwapSelectionButton.Pressed += () => Apply(_controller.SwapLastStarterWithFirstBenchForNextDueMatch());
        pitchHeaderRow.AddChild(_prepSwapSelectionButton);

        _prepPitchCaptionLabel = BodyLabel("PrepPitchCaption", muted: true, autowrap: true);
        _prepPitchCaptionLabel.Visible = false;
        pitchCard.AddChild(_prepPitchCaptionLabel);

        _prepPitchHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 290),
        };
        pitchCard.AddChild(_prepPitchHost);

        // ── RIGHT COLUMN: TABBED TACTICAL & PREPARATION INSPECTOR ──────────────
        var rightInspectorCol = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.2f,
        };
        rightInspectorCol.AddThemeConstantOverride("separation", 8);
        prepSplitRow.AddChild(rightInspectorCol);

        // TAB 0: TAKTİK & MAÇ PLANI (Default Active)
        var tacticsNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        tacticsNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        rightInspectorCol.AddChild(tacticsNeonPanel);
        _prepTacticsPanelHost = tacticsNeonPanel;

        var tacticsCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        tacticsCard.AddThemeConstantOverride("separation", 6);
        tacticsNeonPanel.AddChild(tacticsCard);
        tacticsCard.AddChild(UiComponents.PageHeader("🕹️", "TAKTİKSEL TALİMATLAR & DİZİLİŞ"));

        _tacticLabel = BodyLabel("TacticLabel", autowrap: true);
        _tacticLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        tacticsCard.AddChild(_tacticLabel);

        var formationTitle = new Label { Text = "DİZİLİŞ & FORMASYON" };
        CareerUiTheme.StyleHeadline(formationTitle);
        formationTitle.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        formationTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        tacticsCard.AddChild(formationTitle);

        var formationRow = ActionFlow();
        tacticsCard.AddChild(formationRow);

        _formationPickerButton = PrimaryButton("📋 Tüm 14 Diziliş");
        _formationPickerButton.TooltipText = "Görsel mini saha tahtası ile 14 farklı dizilişi inceleyin ve seçin.";
        _formationPickerButton.Pressed += () =>
        {
            var cur = _controller.GetManagedClubTacticPlan()?.Formation ?? Formation.F442;
            FormationPickerModal.Open(this, cur, f => Apply(_controller.SetTacticFormation(f)));
        };
        formationRow.AddChild(_formationPickerButton);

        _formation442Button = SecondaryButton("4-4-2");
        _formation442Button.Pressed += () => Apply(_controller.SetTacticFormation(Formation.F442));
        formationRow.AddChild(_formation442Button);

        _formation433Button = SecondaryButton("4-3-3");
        _formation433Button.Pressed += () => Apply(_controller.SetTacticFormation(Formation.F433));
        formationRow.AddChild(_formation433Button);

        _formation352Button = SecondaryButton("3-5-2");
        _formation352Button.Pressed += () => Apply(_controller.SetTacticFormation(Formation.F352));
        formationRow.AddChild(_formation352Button);

        var scenarioTitle = new Label { Text = "HAZIR MAÇ PLANLARI (PRESETS)" };
        CareerUiTheme.StyleHeadline(scenarioTitle);
        scenarioTitle.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        scenarioTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        tacticsCard.AddChild(scenarioTitle);

        var scenarioRow = ActionFlow();
        tacticsCard.AddChild(scenarioRow);

        _scenarioShockPressBtn = PrimaryButton("⚡ Şok Pres");
        _scenarioShockPressBtn.TooltipText = "4-3-3, Hücum, Önde Pres, Yüksek Hat";
        _scenarioShockPressBtn.Pressed += () => ApplyScenarioGamePlan("SHOCK_PRESS");
        scenarioRow.AddChild(_scenarioShockPressBtn);

        _scenarioParkTheBusBtn = SecondaryButton("🛡️ Skoru Koru");
        _scenarioParkTheBusBtn.TooltipText = "4-4-2, Defans, Geri Pres, Derin Hat";
        _scenarioParkTheBusBtn.Pressed += () => ApplyScenarioGamePlan("PARK_BUS");
        scenarioRow.AddChild(_scenarioParkTheBusBtn);

        _scenarioFastCounterBtn = SecondaryButton("🎯 Hızlı Kontra");
        _scenarioFastCounterBtn.TooltipText = "3-5-2, Dengeli, Önde Pres, Standart Hat";
        _scenarioFastCounterBtn.Pressed += () => ApplyScenarioGamePlan("FAST_COUNTER");
        scenarioRow.AddChild(_scenarioFastCounterBtn);

        _scenarioPatientControlBtn = SecondaryButton("⚽ Pas & Kontrol");
        _scenarioPatientControlBtn.TooltipText = "4-3-3, Dengeli, Dengeli Pres, Standart Hat";
        _scenarioPatientControlBtn.Pressed += () => ApplyScenarioGamePlan("PATIENT_CONTROL");
        scenarioRow.AddChild(_scenarioPatientControlBtn);

        var styleTitle = new Label { Text = "ANLAYIŞ · PRES ŞİDDETİ · SAVUNMA HATTI" };
        CareerUiTheme.StyleHeadline(styleTitle);
        styleTitle.AddThemeColorOverride("font_color", CareerUiTheme.Data);
        styleTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        tacticsCard.AddChild(styleTitle);

        var approachRow = ActionFlow();
        tacticsCard.AddChild(approachRow);

        _approachBalancedButton = SecondaryButton("Anlayış: Dengeli");
        _approachBalancedButton.Pressed += () => Apply(_controller.SetTacticApproach(TacticalApproach.Balanced));
        approachRow.AddChild(_approachBalancedButton);

        _approachAttackingButton = SecondaryButton("Anlayış: Hücum");
        _approachAttackingButton.Pressed += () => Apply(_controller.SetTacticApproach(TacticalApproach.Attacking));
        approachRow.AddChild(_approachAttackingButton);

        _approachDefensiveButton = SecondaryButton("Anlayış: Defans");
        _approachDefensiveButton.Pressed += () => Apply(_controller.SetTacticApproach(TacticalApproach.Defensive));
        approachRow.AddChild(_approachDefensiveButton);

        var pressingRow = ActionFlow();
        tacticsCard.AddChild(pressingRow);
        _pressingLowButton = SecondaryButton("Pres: Geri Blok");
        _pressingLowButton.Pressed += () => Apply(_controller.SetTacticPressing(PressingIntensity.LowBlock));
        pressingRow.AddChild(_pressingLowButton);
        _pressingBalancedButton = SecondaryButton("Pres: Dengeli");
        _pressingBalancedButton.Pressed += () => Apply(_controller.SetTacticPressing(PressingIntensity.Balanced));
        pressingRow.AddChild(_pressingBalancedButton);
        _pressingHighButton = SecondaryButton("Pres: Önde Baskı");
        _pressingHighButton.Pressed += () => Apply(_controller.SetTacticPressing(PressingIntensity.HighPress));
        pressingRow.AddChild(_pressingHighButton);

        var lineRow = ActionFlow();
        tacticsCard.AddChild(lineRow);
        _lineDeepButton = SecondaryButton("Hat: Derin");
        _lineDeepButton.Pressed += () => Apply(_controller.SetTacticDefensiveLine(DefensiveLine.Deep));
        lineRow.AddChild(_lineDeepButton);
        _lineStandardButton = SecondaryButton("Hat: Standart");
        _lineStandardButton.Pressed += () => Apply(_controller.SetTacticDefensiveLine(DefensiveLine.Standard));
        lineRow.AddChild(_lineStandardButton);
        _lineHighButton = SecondaryButton("Hat: Yüksek");
        _lineHighButton.Pressed += () => Apply(_controller.SetTacticDefensiveLine(DefensiveLine.High));
        lineRow.AddChild(_lineHighButton);

        foreach (var option in new[]
                 {
                     _formation442Button, _formation433Button, _formation352Button,
                     _approachBalancedButton, _approachAttackingButton, _approachDefensiveButton,
                     _pressingLowButton, _pressingBalancedButton, _pressingHighButton,
                     _lineDeepButton, _lineStandardButton, _lineHighButton,
                 })
        {
            option.ToggleMode = true;
        }

        _dualPhaseTacticLabel = BodyLabel("DualPhaseTacticLabel", muted: true, autowrap: true);
        tacticsCard.AddChild(_dualPhaseTacticLabel);
        var phaseRow = ActionFlow();
        tacticsCard.AddChild(phaseRow);
        AddDualPhasePreset(
            phaseRow,
            "Dengeli Geçiş",
            Formation.F442,
            Formation.F442,
            TacticalPhaseRole.Balanced,
            TacticalPhaseRole.Balanced);
        AddDualPhasePreset(
            phaseRow,
            "Kanat + Kompakt",
            Formation.F433,
            Formation.F442,
            TacticalPhaseRole.WideOverloads,
            TacticalPhaseRole.CompactBlock);
        AddDualPhasePreset(
            phaseRow,
            "Merkez + Pres",
            Formation.F352,
            Formation.F442,
            TacticalPhaseRole.CentralOverloads,
            TacticalPhaseRole.AggressivePress);
        AddDualPhasePreset(
            phaseRow,
            "Direkt + Blok",
            Formation.F442,
            Formation.F442,
            TacticalPhaseRole.DirectRunners,
            TacticalPhaseRole.CompactBlock);

        // TAB 1: RAKİP & EŞLEŞME ANALİZİ
        var briefingNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
        };
        briefingNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        rightInspectorCol.AddChild(briefingNeonPanel);
        _prepOpponentPanelHost = briefingNeonPanel;

        var briefingCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        briefingCard.AddThemeConstantOverride("separation", 8);
        briefingNeonPanel.AddChild(briefingCard);
        briefingCard.AddChild(UiComponents.PageHeader("🔍", "RAKİP & EŞLEŞME ANALİZİ"));

        var oppBox = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        oppBox.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        briefingCard.AddChild(oppBox);

        var oppVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        oppVBox.AddThemeConstantOverride("separation", 6);
        oppBox.AddChild(oppVBox);

        var oppTitle = new Label { Text = "RAKİP TEHDİT PROFİLİ" };
        CareerUiTheme.StyleHeadline(oppTitle);
        oppTitle.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        oppTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        oppVBox.AddChild(oppTitle);

        _prepOpponentThreatBadgePanel = new PanelContainer();
        _prepOpponentThreatBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.20f));
        _prepOpponentThreatBadgeLabel = new Label { Text = "🔥 Tehdit Tespiti Bekleniyor" };
        CareerUiTheme.StyleTableText(_prepOpponentThreatBadgeLabel);
        _prepOpponentThreatBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _prepOpponentThreatBadgePanel.AddChild(_prepOpponentThreatBadgeLabel);
        oppVBox.AddChild(_prepOpponentThreatBadgePanel);

        _prepOpponentLabel = BodyLabel("PrepOpponentLabel", autowrap: true);
        oppVBox.AddChild(_prepOpponentLabel);

        var matchupBox = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        matchupBox.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        briefingCard.AddChild(matchupBox);

        var matchupVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        matchupVBox.AddThemeConstantOverride("separation", 6);
        matchupBox.AddChild(matchupVBox);

        var matchupTitle = new Label { Text = "TAKTİKSEL EŞLEŞME FIRSATI" };
        CareerUiTheme.StyleHeadline(matchupTitle);
        matchupTitle.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        matchupTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        matchupVBox.AddChild(matchupTitle);

        _prepMatchupSignalBadgePanel = new PanelContainer();
        _prepMatchupSignalBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
        _prepMatchupSignalBadgeLabel = new Label { Text = "ℹ️ Dengeli Eşleşme" };
        CareerUiTheme.StyleTableText(_prepMatchupSignalBadgeLabel);
        _prepMatchupSignalBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _prepMatchupSignalBadgePanel.AddChild(_prepMatchupSignalBadgeLabel);
        matchupVBox.AddChild(_prepMatchupSignalBadgePanel);

        _prepMatchupLabel = BodyLabel("PrepMatchupLabel", autowrap: true);
        matchupVBox.AddChild(_prepMatchupLabel);

        _prepBriefingLabel = BodyLabel("PrepBriefingLabel", muted: true, autowrap: true);
        _prepBriefingLabel.Visible = false;
        briefingCard.AddChild(_prepBriefingLabel);

        // TAB 2: ANTRENMAN & MAÇA ÖZEL HAZIRLIK
        var trainingHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
        };
        trainingHost.AddThemeConstantOverride("separation", 8);
        rightInspectorCol.AddChild(trainingHost);
        _prepTrainingPanelHost = trainingHost;

        var trainingNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        trainingNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        trainingHost.AddChild(trainingNeonPanel);
        var trainingCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        trainingCard.AddThemeConstantOverride("separation", 5);
        trainingNeonPanel.AddChild(trainingCard);
        trainingCard.AddChild(UiComponents.PageHeader("⚡", "HAFTALIK ANTRENMAN PROGRAMI"));

        _trainingLabel = BodyLabel("TrainingLabel", autowrap: true);
        trainingCard.AddChild(_trainingLabel);

        var trainingRow = ActionFlow();
        trainingCard.AddChild(trainingRow);

        _trainLowButton = SecondaryButton("Şiddet: Hafif");
        _trainLowButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.Low));
        trainingRow.AddChild(_trainLowButton);

        _trainMediumButton = SecondaryButton("Şiddet: Orta");
        _trainMediumButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.Medium));
        trainingRow.AddChild(_trainMediumButton);

        _trainHighButton = SecondaryButton("Şiddet: Yoğun");
        _trainHighButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.High));
        trainingRow.AddChild(_trainHighButton);

        var focusRow = ActionFlow();
        trainingCard.AddChild(focusRow);

        _focusGeneralButton = SecondaryButton("Odak: Dengeli");
        _focusGeneralButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.General));
        focusRow.AddChild(_focusGeneralButton);

        _focusFitnessButton = SecondaryButton("Odak: Kondisyon");
        _focusFitnessButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Fitness));
        focusRow.AddChild(_focusFitnessButton);

        _focusTacticalButton = SecondaryButton("Odak: Taktik");
        _focusTacticalButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Tactical));
        focusRow.AddChild(_focusTacticalButton);

        _focusRecoveryButton = SecondaryButton("Odak: Toparlanma");
        _focusRecoveryButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Recovery));
        focusRow.AddChild(_focusRecoveryButton);

        var restRow = ActionFlow();
        trainingCard.AddChild(restRow);

        _restLightButton = SecondaryButton("Az Dinlenme");
        _restLightButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingRest(RestApproach.Light));
        restRow.AddChild(_restLightButton);

        _restNormalButton = SecondaryButton("Normal Dinlenme");
        _restNormalButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingRest(RestApproach.Normal));
        restRow.AddChild(_restNormalButton);

        _restHeavyButton = SecondaryButton("Bol Dinlenme");
        _restHeavyButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingRest(RestApproach.Heavy));
        restRow.AddChild(_restHeavyButton);

        foreach (var option in new[]
                 {
                     _trainLowButton, _trainMediumButton, _trainHighButton,
                     _focusGeneralButton, _focusFitnessButton, _focusTacticalButton, _focusRecoveryButton,
                     _restLightButton, _restNormalButton, _restHeavyButton,
                 })
        {
            option.ToggleMode = true;
        }

        var matchPriorityCard = AddCard(trainingHost, "⭐ MAÇA ÖZEL HAZIRLIK ÖNCELİĞİ", emphasized: true);
        _matchTrainingPriorityLabel = BodyLabel("MatchTrainingPriorityLabel", autowrap: true);
        matchPriorityCard.AddChild(_matchTrainingPriorityLabel);

        var priorityBentoDesk = new HFlowContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        priorityBentoDesk.AddThemeConstantOverride("h_separation", 6);
        priorityBentoDesk.AddThemeConstantOverride("v_separation", 6);
        matchPriorityCard.AddChild(priorityBentoDesk);

        foreach (var priority in Enum.GetValues<MatchTrainingPriority>())
        {
            var captured = priority;
            var card = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            card.AddThemeStyleboxOverride("panel", CareerUiTheme.MatchPriorityCardPanel());
            priorityBentoDesk.AddChild(card);
            _matchPriorityCardPanels[priority] = card;

            var cardVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            cardVBox.AddThemeConstantOverride("separation", 3);
            card.AddChild(cardVBox);

            var recBadge = new PanelContainer { Visible = false };
            recBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.25f));
            var recLabel = new Label { Text = "⭐ ÖNERİLEN" };
            CareerUiTheme.StyleTableText(recLabel);
            recLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
            recLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
            recBadge.AddChild(recLabel);
            cardVBox.AddChild(recBadge);
            _matchPriorityRecBadges[priority] = recBadge;

            var priorityTitle = priority switch
            {
                MatchTrainingPriority.Recovery => "Toparlanma",
                MatchTrainingPriority.MatchSharpness => "Maç Keskinliği",
                MatchTrainingPriority.PressResistance => "Baskıdan Çıkış",
                MatchTrainingPriority.DefensiveTransitions => "Geçiş Savunması",
                MatchTrainingPriority.AttackingPatterns => "Hücum Otomasyonu",
                _ => priority.ToString(),
            };

            var titleLabel = new Label { Text = priorityTitle };
            CareerUiTheme.StyleHeadline(titleLabel);
            titleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
            cardVBox.AddChild(titleLabel);

            var bonusLabel = new Label { Text = "+0 Güç" };
            CareerUiTheme.StyleTableText(bonusLabel);
            bonusLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
            cardVBox.AddChild(bonusLabel);
            _matchPriorityBonusLabels[priority] = bonusLabel;

            var costLabel = BodyLabel($"PriorityCost_{priority}", muted: true, autowrap: true);
            costLabel.Text = "-%0 Fit";
            costLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
            cardVBox.AddChild(costLabel);
            _matchPriorityCostLabels[priority] = costLabel;

            var button = SecondaryButton("Seç");
            button.CustomMinimumSize = new Vector2(0, 26);
            button.Pressed += () => Apply(_controller.SelectMatchTrainingPriority(captured));
            cardVBox.AddChild(button);
            _matchTrainingPriorityButtons[priority] = button;
        }

        ShowPrepSubTab(0);
        return page;
    }

    private void ShowPrepSubTab(int tabIndex)
    {
        if (_prepTacticsPanelHost is not null) _prepTacticsPanelHost.Visible = tabIndex == 0;
        if (_prepOpponentPanelHost is not null) _prepOpponentPanelHost.Visible = tabIndex == 1;
        if (_prepTrainingPanelHost is not null) _prepTrainingPanelHost.Visible = tabIndex == 2;

        if (_prepTabTacticsBtn is not null) CareerUiTheme.StyleTabButton(_prepTabTacticsBtn, tabIndex == 0);
        if (_prepTabOpponentBtn is not null) CareerUiTheme.StyleTabButton(_prepTabOpponentBtn, tabIndex == 1);
        if (_prepTabTrainingBtn is not null) CareerUiTheme.StyleTabButton(_prepTabTrainingBtn, tabIndex == 2);
    }

    private void AddDualPhasePreset(
        Control row,
        string label,
        Formation inFormation,
        Formation outFormation,
        TacticalPhaseRole inRole,
        TacticalPhaseRole outRole)
    {
        var button = SecondaryButton(label);
        button.Pressed += () => Apply(_controller.SetDualPhaseTactic(
            inFormation,
            outFormation,
            inRole,
            outRole));
        row.AddChild(button);
        _dualPhaseTacticButtons.Add(button);
    }

    private void ApplyScenarioGamePlan(string scenario)
    {
        switch (scenario)
        {
            case "SHOCK_PRESS":
                Apply(_controller.ApplyTacticalPreset(
                    Formation.F433,
                    TacticalApproach.Attacking,
                    PressingIntensity.HighPress,
                    DefensiveLine.High,
                    "Şok Pres & Erken Gol"));
                break;
            case "PARK_BUS":
                Apply(_controller.ApplyTacticalPreset(
                    Formation.F442,
                    TacticalApproach.Defensive,
                    PressingIntensity.LowBlock,
                    DefensiveLine.Deep,
                    "Skoru Koru & Kompakt"));
                break;
            case "FAST_COUNTER":
                Apply(_controller.ApplyTacticalPreset(
                    Formation.F352,
                    TacticalApproach.Balanced,
                    PressingIntensity.HighPress,
                    DefensiveLine.Standard,
                    "Hızlı Geçiş & Kontra"));
                break;
            case "PATIENT_CONTROL":
                Apply(_controller.ApplyTacticalPreset(
                    Formation.F433,
                    TacticalApproach.Balanced,
                    PressingIntensity.Balanced,
                    DefensiveLine.Standard,
                    "Sabırlı Pas & Kontrol"));
                break;
        }
    }
}
