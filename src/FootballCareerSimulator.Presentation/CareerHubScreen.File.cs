using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private static Label CreateLegacyStatCard(
        Control parent,
        string title,
        string initialValue,
        Color accentColor)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.LegacyStatCardPanel(accentColor));
        parent.AddChild(panel);

        var vbox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        vbox.AddThemeConstantOverride("separation", 3);
        panel.AddChild(vbox);

        var header = new Label { Text = title };
        CareerUiTheme.StyleEyebrow(header, accentColor);
        vbox.AddChild(header);

        var valLabel = new Label { Text = initialValue };
        CareerUiTheme.StyleHeadline(valLabel);
        valLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        vbox.AddChild(valLabel);

        return valLabel;
    }

    private Control BuildFilePage()
    {
        var page = PageRoot();

        // 1. Governance Telemetry Bar
        _govTelemetryBar = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _govTelemetryBar.AddThemeConstantOverride("h_separation", 8);
        _govTelemetryBar.AddThemeConstantOverride("v_separation", 8);
        page.AddChild(_govTelemetryBar);

        _boardConfidenceBadgePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _boardConfidenceBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
        _boardConfidenceBadgeLabel = new Label { Text = "🏛️ Güven %—" };
        CareerUiTheme.StyleTableText(_boardConfidenceBadgeLabel);
        _boardConfidenceBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _boardConfidenceBadgePanel.AddChild(_boardConfidenceBadgeLabel);
        _govTelemetryBar.AddChild(_boardConfidenceBadgePanel);

        _operatingBalanceBadgePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _operatingBalanceBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.16f));
        _operatingBalanceBadgeLabel = new Label { Text = "💰 Net —" };
        CareerUiTheme.StyleTableText(_operatingBalanceBadgeLabel);
        _operatingBalanceBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _operatingBalanceBadgePanel.AddChild(_operatingBalanceBadgeLabel);
        _govTelemetryBar.AddChild(_operatingBalanceBadgePanel);

        _stadiumAttendanceBadgePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _stadiumAttendanceBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data, 0.16f));
        _stadiumAttendanceBadgeLabel = new Label { Text = "🏟️ %—" };
        CareerUiTheme.StyleTableText(_stadiumAttendanceBadgeLabel);
        _stadiumAttendanceBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Data);
        _stadiumAttendanceBadgePanel.AddChild(_stadiumAttendanceBadgeLabel);
        _govTelemetryBar.AddChild(_stadiumAttendanceBadgePanel);

        _wageUtilizationBadgePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _wageUtilizationBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Stroke, 0.20f));
        _wageUtilizationBadgeLabel = new Label { Text = "💼 Maaş %—" };
        CareerUiTheme.StyleTableText(_wageUtilizationBadgeLabel);
        _wageUtilizationBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.TextPrimary);
        _wageUtilizationBadgePanel.AddChild(_wageUtilizationBadgeLabel);
        _govTelemetryBar.AddChild(_wageUtilizationBadgePanel);

        // ----------------------------------------------------
        // BENTO ROW 1: BOARD OBJECTIVES (1.4f) + JOB MARKET (1.0f)
        // ----------------------------------------------------
        _fileTopGrid = new GridContainer
        {
            Columns = 1,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _fileTopGrid.AddThemeConstantOverride("h_separation", 12);
        _fileTopGrid.AddThemeConstantOverride("v_separation", 12);
        page.AddChild(_fileTopGrid);

        // 2. Board Objectives Desk (1.4f)
        var objectivesCard = AddCard(_fileTopGrid, "🎯 HEDEFLER", emphasized: true, expandVertical: true);
        objectivesCard.GetParent<PanelContainer>().SizeFlagsStretchRatio = 1.4f;
        _boardObjectivesContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _boardObjectivesContainer.AddThemeConstantOverride("separation", 8);
        objectivesCard.AddChild(_boardObjectivesContainer);

        // 5. Job Market Desk (1.0f)
        var careerJobCard = AddCard(_fileTopGrid, "💼 İŞ TEKLİFLERİ", emphasized: false, expandVertical: true);
        careerJobCard.GetParent<PanelContainer>().SizeFlagsStretchRatio = 1.0f;
        var careerJobRow = ActionFlow();
        careerJobCard.AddChild(careerJobRow);
        _generateOfferButton = PrimaryButton("İş Teklifi Ara");
        _generateOfferButton.Pressed += () => Apply(_controller.GenerateJobOffer());
        careerJobRow.AddChild(_generateOfferButton);
        _acceptOfferButton = AccentButton("Teklifi Kabul Et");
        _acceptOfferButton.Pressed += () => Apply(_controller.AcceptJobOffer());
        careerJobRow.AddChild(_acceptOfferButton);
        _signFreeAgentButton = SecondaryButton("Serbest Oyuncu İmzala");
        _signFreeAgentButton.Pressed += () => CompleteTransferAndCelebrate(_controller.SignNextFreeAgentToManagedClub);
        careerJobRow.AddChild(_signFreeAgentButton);

        // ----------------------------------------------------
        // BENTO ROW 2: FINANCE & LEDGER DESK (Full Width)
        // ----------------------------------------------------
        var financeCard = AddCard(page, "💰 FİNANS", emphasized: true);
        _fileFinanceGrid = new GridContainer
        {
            Columns = 1,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _fileFinanceGrid.AddThemeConstantOverride("h_separation", 10);
        _fileFinanceGrid.AddThemeConstantOverride("v_separation", 10);
        financeCard.AddChild(_fileFinanceGrid);

        var projPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        projPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.FinanceLedgerCardPanel(true));
        _fileFinanceGrid.AddChild(projPanel);
        var projVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        projVBox.AddThemeConstantOverride("separation", 6);
        projPanel.AddChild(projVBox);
        var projLabel = new Label { Text = "TAHMİN" };
        CareerUiTheme.StyleHeadline(projLabel);
        projLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        projLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        projVBox.AddChild(projLabel);
        _financeProjectionLabel = BodyLabel("FinanceProjectionLabel", autowrap: true);
        projVBox.AddChild(_financeProjectionLabel);

        var realPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        realPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.FinanceLedgerCardPanel(true));
        _fileFinanceGrid.AddChild(realPanel);
        var realVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        realVBox.AddThemeConstantOverride("separation", 6);
        realPanel.AddChild(realVBox);
        var realHeader = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        realVBox.AddChild(realHeader);
        var realTitle = new Label { Text = "KASA", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CareerUiTheme.StyleHeadline(realTitle);
        realTitle.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        realTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        realHeader.AddChild(realTitle);

        _boardFinanceStatusBadgePanel = new PanelContainer();
        _boardFinanceStatusBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.20f));
        _boardFinanceStatusBadgeLabel = new Label { Text = "Sağlıklı" };
        CareerUiTheme.StyleTableText(_boardFinanceStatusBadgeLabel);
        _boardFinanceStatusBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _boardFinanceStatusBadgePanel.AddChild(_boardFinanceStatusBadgeLabel);
        realHeader.AddChild(_boardFinanceStatusBadgePanel);

        _realizedFinanceLabel = BodyLabel("RealizedFinanceLabel", autowrap: true);
        realVBox.AddChild(_realizedFinanceLabel);
        _boardOutcomeSummaryLabel = BodyLabel("BoardOutcomeSummaryLabel", muted: true, autowrap: true);
        _boardOutcomeSummaryLabel.Visible = false;
        realVBox.AddChild(_boardOutcomeSummaryLabel);

        _clubEconomyLabel = BodyLabel("ClubEconomyLabel", autowrap: true);
        _clubEconomyLabel.Visible = false;
        financeCard.AddChild(_clubEconomyLabel);

        // ----------------------------------------------------
        // BENTO ROW 3: MANAGER LEGACY (1.4f) + SAVE & RECORD (1.0f)
        // ----------------------------------------------------
        _fileBottomGrid = new GridContainer
        {
            Columns = 1,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _fileBottomGrid.AddThemeConstantOverride("h_separation", 12);
        _fileBottomGrid.AddThemeConstantOverride("v_separation", 12);
        page.AddChild(_fileBottomGrid);

        // 4. Manager Career Legacy (1.4f)
        var legacyCard = AddCard(_fileBottomGrid, "🎖️ KARİYER", emphasized: true, expandVertical: true);
        legacyCard.GetParent<PanelContainer>().SizeFlagsStretchRatio = 1.4f;
        var legacyHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        legacyHeaderRow.AddThemeConstantOverride("separation", 10);
        legacyCard.AddChild(legacyHeaderRow);

        var managerInfoVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        legacyHeaderRow.AddChild(managerInfoVBox);

        _careerLegacyHeadlineLabel = BodyLabel("CareerLegacyHeadlineLabel", autowrap: true);
        CareerUiTheme.StyleHeadline(_careerLegacyHeadlineLabel);
        _careerLegacyHeadlineLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        managerInfoVBox.AddChild(_careerLegacyHeadlineLabel);

        _legacyTenureLabel = BodyLabel("LegacyTenureLabel", muted: true, autowrap: true);
        managerInfoVBox.AddChild(_legacyTenureLabel);

        _legacyReputationBadgePanel = new PanelContainer();
        _legacyReputationBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.25f));
        _legacyReputationBadgeLabel = new Label { Text = "⭐ İtibar: —" };
        CareerUiTheme.StyleHeadline(_legacyReputationBadgeLabel);
        _legacyReputationBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _legacyReputationBadgeLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        _legacyReputationBadgePanel.AddChild(_legacyReputationBadgeLabel);
        legacyHeaderRow.AddChild(_legacyReputationBadgePanel);

        _fileLegacyStatsGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _fileLegacyStatsGrid.AddThemeConstantOverride("h_separation", 8);
        _fileLegacyStatsGrid.AddThemeConstantOverride("v_separation", 8);
        legacyCard.AddChild(_fileLegacyStatsGrid);

        _legacyStatMatchesLabel = CreateLegacyStatCard(_fileLegacyStatsGrid, "⚽ MAÇLAR", "—", CareerUiTheme.Data);
        _legacyStatWinRateLabel = CreateLegacyStatCard(_fileLegacyStatsGrid, "🏆 PERFORMANS", "—", CareerUiTheme.ActionBright);
        _legacyStatYouthLabel = CreateLegacyStatCard(_fileLegacyStatsGrid, "🌱 MİRAS", "—", CareerUiTheme.Accent);
        _legacyStatMilestoneLabel = CreateLegacyStatCard(_fileLegacyStatsGrid, "🎯 HEDEF", "—", CareerUiTheme.TextPrimary);

        _careerLegacyRecordLabel = BodyLabel("CareerLegacyRecordLabel", autowrap: true);
        _careerLegacyRecordLabel.Visible = false;
        legacyCard.AddChild(_careerLegacyRecordLabel);
        _careerLegacyDevelopmentLabel = BodyLabel("CareerLegacyDevelopmentLabel", autowrap: true);
        _careerLegacyDevelopmentLabel.Visible = false;
        legacyCard.AddChild(_careerLegacyDevelopmentLabel);

        var seasonListTitle = new Label { Text = "GEÇMİŞ" };
        CareerUiTheme.StyleHeadline(seasonListTitle);
        seasonListTitle.AddThemeColorOverride("font_color", CareerUiTheme.TextMuted);
        seasonListTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        legacyCard.AddChild(seasonListTitle);

        _careerSeasonList = new ItemList
        {
            Name = "CareerSeasonList",
            CustomMinimumSize = new Vector2(0, 180),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        CareerUiTheme.StyleList(_careerSeasonList);
        legacyCard.AddChild(_careerSeasonList);

        // 6. Save / Load Desk (1.0f)
        var saveCard = AddCard(_fileBottomGrid, "💾 KAYIT", emphasized: true, expandVertical: true);
        saveCard.GetParent<PanelContainer>().SizeFlagsStretchRatio = 1.0f;
        var saveTelemetryGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        saveTelemetryGrid.AddThemeConstantOverride("h_separation", 7);
        saveTelemetryGrid.AddThemeConstantOverride("v_separation", 7);
        saveCard.AddChild(saveTelemetryGrid);
        _saveLastWriteBadgeLabel = CreateSaveTelemetryBadge(saveTelemetryGrid, "🕒 Son Kayıt: —", CareerUiTheme.ActionBright);
        _saveActiveSlotBadgeLabel = CreateSaveTelemetryBadge(saveTelemetryGrid, "📁 Aktif Slot: —", CareerUiTheme.Data);
        _saveWorldSeedBadgeLabel = CreateSaveTelemetryBadge(saveTelemetryGrid, "🌍 Dünya Seed: —", CareerUiTheme.Accent);
        _saveFileSizeBadgeLabel = CreateSaveTelemetryBadge(saveTelemetryGrid, "💽 Dosya Boyutu: —", CareerUiTheme.ColorWarning);
        _saveDeskLabel = BodyLabel("SaveDeskLabel", autowrap: true);
        _saveDeskLabel.Visible = false;
        saveCard.AddChild(_saveDeskLabel);

        var saveLoadRow = ActionFlow();
        saveCard.AddChild(saveLoadRow);

        _saveGameButton = AccentButton("⚡ Hızlı Kaydet");
        _saveGameButton.Pressed += OnSaveGamePressed;
        saveLoadRow.AddChild(_saveGameButton);

        _loadGameButton = PrimaryButton("↻ Yeniden Yükle");
        _loadGameButton.Pressed += OnLoadGamePressed;
        saveLoadRow.AddChild(_loadGameButton);

        var menuButton = DangerButton("⏏ Ana Menüye Dön");
        menuButton.Pressed += () => BackToMenuRequested?.Invoke();
        saveLoadRow.AddChild(menuButton);

        // ----------------------------------------------------
        // BENTO ROW 4: EXPERIENCE & DEVICE SETTINGS
        // ----------------------------------------------------
        var experienceCard = AddCard(page, "⚙️ AYARLAR");
        _deviceAcceptanceLabel = BodyLabel("DeviceAcceptanceLabel", autowrap: true);
        _deviceAcceptanceLabel.Visible = false;
        experienceCard.AddChild(_deviceAcceptanceLabel);
        _runtimeTelemetryLabel = BodyLabel("RuntimeTelemetryLabel", autowrap: true);
        _runtimeTelemetryLabel.Visible = false;
        experienceCard.AddChild(_runtimeTelemetryLabel);
        _experienceSettingsLabel = BodyLabel("ExperienceSettingsLabel", muted: true, autowrap: true);
        _experienceSettingsLabel.Visible = false;
        experienceCard.AddChild(_experienceSettingsLabel);

        var soundRow = ActionFlow();
        experienceCard.AddChild(soundRow);
        _soundSettingButton = SecondaryButton("Ses");
        _soundSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { SoundEnabled = !current.SoundEnabled });
        soundRow.AddChild(_soundSettingButton);
        _musicSettingButton = SecondaryButton("Müzik");
        _musicSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { MusicEnabled = !current.MusicEnabled });
        soundRow.AddChild(_musicSettingButton);
        _crowdSettingButton = SecondaryButton("Tribün");
        _crowdSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { CrowdEnabled = !current.CrowdEnabled });
        soundRow.AddChild(_crowdSettingButton);
        _hapticsSettingButton = SecondaryButton("Titreşim");
        _hapticsSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current.CycleHapticsStrength());
        soundRow.AddChild(_hapticsSettingButton);

        var deviceTestRow = ActionFlow();
        experienceCard.AddChild(deviceTestRow);
        var hapticTestButton = SecondaryButton("Titreşimi Dene");
        hapticTestButton.Pressed += TestHapticFeedback;
        deviceTestRow.AddChild(hapticTestButton);
        var resetTelemetryButton = SecondaryButton("Ölçümü Sıfırla");
        resetTelemetryButton.Pressed += ResetRuntimeTelemetry;
        deviceTestRow.AddChild(resetTelemetryButton);

        var accessibilityRow = ActionFlow();
        experienceCard.AddChild(accessibilityRow);
        _textScaleSettingButton = SecondaryButton("Yazı %100");
        _textScaleSettingButton.Pressed += () => UpdateExperienceSettings(current => current.CycleTextScale());
        accessibilityRow.AddChild(_textScaleSettingButton);
        _contrastSettingButton = SecondaryButton("Yüksek kontrast");
        _contrastSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { HighContrast = !current.HighContrast });
        accessibilityRow.AddChild(_contrastSettingButton);
        _motionSettingButton = SecondaryButton("Hareketi azalt");
        _motionSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { ReducedMotion = !current.ReducedMotion });
        accessibilityRow.AddChild(_motionSettingButton);
        _guideSettingButton = SecondaryButton("İlk hafta rehberi");
        _guideSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { FirstWeekGuideEnabled = !current.FirstWeekGuideEnabled });
        accessibilityRow.AddChild(_guideSettingButton);
        _gamepadSettingButton = SecondaryButton("Gamepad ipuçları");
        _gamepadSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with
            {
                GamepadNavigationHintsEnabled = !current.GamepadNavigationHintsEnabled,
            });
        accessibilityRow.AddChild(_gamepadSettingButton);

        RegisterMobileFocusLayout(
            page,
            HubPage.File,
            Array.Empty<Control>(),
            ("HEDEF", new Control[] { _fileTopGrid }),
            ("FİNANS", new Control[] { _govTelemetryBar, financeCard.GetParent<Control>() }),
            ("KARİYER", new Control[] { legacyCard.GetParent<Control>(), saveCard.GetParent<Control>() }),
            ("AYAR", new Control[] { experienceCard.GetParent<Control>() }));

        return page;
    }

}
