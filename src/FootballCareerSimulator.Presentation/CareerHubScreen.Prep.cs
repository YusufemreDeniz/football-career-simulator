using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private Control BuildPrepPage()
    {
        var page = PageRoot();

        _prepReadinessBar = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _prepReadinessBar.AddThemeConstantOverride("separation", 8);
        page.AddChild(_prepReadinessBar);

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


        // Rakip analiz kartı
        var briefingNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        briefingNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        page.AddChild(briefingNeonPanel);
        var briefingCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        briefingCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        briefingNeonPanel.AddChild(briefingCard);
        briefingCard.AddChild(UiComponents.PageHeader("🔍", "RAKİP"));
        briefingCard.AddChild(UiComponents.Divider(1));

        var briefingCols = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        briefingCols.AddThemeConstantOverride("separation", 10);
        briefingCard.AddChild(briefingCols);

        var oppBox = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        oppBox.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        briefingCols.AddChild(oppBox);

        var oppVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        oppVBox.AddThemeConstantOverride("separation", 6);
        oppBox.AddChild(oppVBox);

        var oppTitle = new Label { Text = "TEHDİT" };
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

        var matchupBox = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        matchupBox.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        briefingCols.AddChild(matchupBox);

        var matchupVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        matchupVBox.AddThemeConstantOverride("separation", 6);
        matchupBox.AddChild(matchupVBox);

        var matchupTitle = new Label { Text = "EŞLEŞME" };
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

        // Saha kartı
        var pitchNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        pitchNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        page.AddChild(pitchNeonPanel);
        var pitchCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        pitchCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        pitchNeonPanel.AddChild(pitchCard);
        pitchCard.AddChild(UiComponents.PageHeader("🟩", "İLK 11"));
        pitchCard.AddChild(UiComponents.Divider(1));

        _prepPitchCaptionLabel = BodyLabel("PrepPitchCaption", muted: true, autowrap: true);
        _prepPitchCaptionLabel.Visible = false;
        pitchCard.AddChild(_prepPitchCaptionLabel);
        _prepPitchHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 390),
        };
        pitchCard.AddChild(_prepPitchHost);

        var pitchActionRow = ActionFlow();
        pitchCard.AddChild(pitchActionRow);
        _prepApproveSelectionButton = AccentButton("✓ Kadro Onayla");
        _prepApproveSelectionButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        pitchActionRow.AddChild(_prepApproveSelectionButton);
        _prepSwapSelectionButton = SecondaryButton("⇄ XI ↔ Yedek");
        _prepSwapSelectionButton.Pressed += () => Apply(_controller.SwapLastStarterWithFirstBenchForNextDueMatch());
        pitchActionRow.AddChild(_prepSwapSelectionButton);

        // Taktik kartı
        var tacticsNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tacticsNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        page.AddChild(tacticsNeonPanel);
        var tacticsCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tacticsCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        tacticsNeonPanel.AddChild(tacticsCard);
        tacticsCard.AddChild(UiComponents.PageHeader("🕹️", "MAÇ PLANI"));
        tacticsCard.AddChild(UiComponents.Divider(1));

        _tacticLabel = BodyLabel("TacticLabel", autowrap: true);
        tacticsCard.AddChild(_tacticLabel);

        var scenarioTitle = new Label { Text = "HAZIR PLANLAR" };
        CareerUiTheme.StyleHeadline(scenarioTitle);
        scenarioTitle.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        scenarioTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
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

        _scenarioFastCounterBtn = SecondaryButton("🎯 Kontra");
        _scenarioFastCounterBtn.TooltipText = "3-5-2, Dengeli, Önde Pres, Standart Hat";
        _scenarioFastCounterBtn.Pressed += () => ApplyScenarioGamePlan("FAST_COUNTER");
        scenarioRow.AddChild(_scenarioFastCounterBtn);

        _scenarioPatientControlBtn = SecondaryButton("⚽ Kontrol");
        _scenarioPatientControlBtn.TooltipText = "4-3-3, Dengeli, Dengeli Pres, Standart Hat";
        _scenarioPatientControlBtn.Pressed += () => ApplyScenarioGamePlan("PATIENT_CONTROL");
        scenarioRow.AddChild(_scenarioPatientControlBtn);

        var formationRow = ActionFlow();
        tacticsCard.AddChild(formationRow);

        _formationPickerButton = PrimaryButton("📋 Diziliş");
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

        var approachRow = ActionFlow();
        tacticsCard.AddChild(approachRow);

        _approachBalancedButton = SecondaryButton("Dengeli");
        _approachBalancedButton.Pressed += () => Apply(_controller.SetTacticApproach(TacticalApproach.Balanced));
        approachRow.AddChild(_approachBalancedButton);

        _approachAttackingButton = SecondaryButton("Hücum");
        _approachAttackingButton.Pressed += () => Apply(_controller.SetTacticApproach(TacticalApproach.Attacking));
        approachRow.AddChild(_approachAttackingButton);

        _approachDefensiveButton = SecondaryButton("Defans");
        _approachDefensiveButton.Pressed += () => Apply(_controller.SetTacticApproach(TacticalApproach.Defensive));
        approachRow.AddChild(_approachDefensiveButton);

        var pressingRow = ActionFlow();
        tacticsCard.AddChild(pressingRow);
        _pressingLowButton = SecondaryButton("Pres: Geri");
        _pressingLowButton.Pressed += () => Apply(_controller.SetTacticPressing(PressingIntensity.LowBlock));
        pressingRow.AddChild(_pressingLowButton);
        _pressingBalancedButton = SecondaryButton("Pres: Dengeli");
        _pressingBalancedButton.Pressed += () => Apply(_controller.SetTacticPressing(PressingIntensity.Balanced));
        pressingRow.AddChild(_pressingBalancedButton);
        _pressingHighButton = SecondaryButton("Pres: Önde");
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

        _dualPhaseTacticLabel = BodyLabel("DualPhaseTacticLabel", autowrap: true);
        _dualPhaseTacticLabel.Visible = false;
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

        // Antrenman kartı
        var trainingNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        trainingNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        page.AddChild(trainingNeonPanel);
        var trainingCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        trainingCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        trainingNeonPanel.AddChild(trainingCard);
        trainingCard.AddChild(UiComponents.PageHeader("⚡", "ANTRENMAN"));
        trainingCard.AddChild(UiComponents.Divider(1));

        _trainingLabel = BodyLabel("TrainingLabel", autowrap: true);
        trainingCard.AddChild(_trainingLabel);

        var intensityTitle = new Label { Text = "ŞİDDET" };
        CareerUiTheme.StyleBody(intensityTitle);
        intensityTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        intensityTitle.AddThemeColorOverride("font_color", CareerUiTheme.TextMuted);
        trainingCard.AddChild(intensityTitle);

        var trainingRow = ActionFlow();
        trainingCard.AddChild(trainingRow);

        _trainLowButton = SecondaryButton("Hafif");
        _trainLowButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.Low));
        trainingRow.AddChild(_trainLowButton);

        _trainMediumButton = SecondaryButton("Orta");
        _trainMediumButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.Medium));
        trainingRow.AddChild(_trainMediumButton);

        _trainHighButton = SecondaryButton("Yoğun");
        _trainHighButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.High));
        trainingRow.AddChild(_trainHighButton);

        var focusTitle = new Label { Text = "ODAK" };
        CareerUiTheme.StyleBody(focusTitle);
        focusTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        focusTitle.AddThemeColorOverride("font_color", CareerUiTheme.TextMuted);
        trainingCard.AddChild(focusTitle);

        var focusRow = ActionFlow();
        trainingCard.AddChild(focusRow);

        _focusGeneralButton = SecondaryButton("Odak: Dengeli");
        _focusGeneralButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.General));
        focusRow.AddChild(_focusGeneralButton);

        _focusFitnessButton = SecondaryButton("Kondisyon");
        _focusFitnessButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Fitness));
        focusRow.AddChild(_focusFitnessButton);

        _focusTacticalButton = SecondaryButton("Taktik");
        _focusTacticalButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Tactical));
        focusRow.AddChild(_focusTacticalButton);

        _focusRecoveryButton = SecondaryButton("Toparlanma");
        _focusRecoveryButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Recovery));
        focusRow.AddChild(_focusRecoveryButton);

        var restTitle = new Label { Text = "DİNLENME" };
        CareerUiTheme.StyleBody(restTitle);
        restTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        restTitle.AddThemeColorOverride("font_color", CareerUiTheme.TextMuted);
        trainingCard.AddChild(restTitle);

        var restRow = ActionFlow();
        trainingCard.AddChild(restRow);

        _restLightButton = SecondaryButton("Az dinlenme");
        _restLightButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingRest(RestApproach.Light));
        restRow.AddChild(_restLightButton);

        _restNormalButton = SecondaryButton("Normal dinlenme");
        _restNormalButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingRest(RestApproach.Normal));
        restRow.AddChild(_restNormalButton);

        _restHeavyButton = SecondaryButton("Bol dinlenme");
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

        var matchPriorityCard = AddCard(page, "⭐ MAÇA ÖZEL", emphasized: true);
        _matchTrainingPriorityLabel = BodyLabel("MatchTrainingPriorityLabel", autowrap: true);
        matchPriorityCard.AddChild(_matchTrainingPriorityLabel);

        var priorityBentoDesk = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        priorityBentoDesk.AddThemeConstantOverride("separation", 8);
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
            cardVBox.AddThemeConstantOverride("separation", 4);
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
            titleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
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
            button.CustomMinimumSize = new Vector2(0, 36);
            button.Pressed += () => Apply(_controller.SelectMatchTrainingPriority(captured));
            cardVBox.AddChild(button);
            _matchTrainingPriorityButtons[priority] = button;
        }

        RegisterMobileFocusLayout(
            page,
            HubPage.Prep,
            Array.Empty<Control>(),
            ("RAKİP", new Control[] { briefingNeonPanel }),
            ("İLK 11", new Control[] { pitchNeonPanel }),
            ("TAKTİK", new Control[] { tacticsNeonPanel }),
            ("ANTRENMAN", new Control[] { trainingNeonPanel, matchPriorityCard.GetParent<Control>() }));

        return page;
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
