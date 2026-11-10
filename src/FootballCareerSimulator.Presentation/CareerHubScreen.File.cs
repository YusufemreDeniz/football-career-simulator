using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.DataPacks;
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

    private static Button TabButton(string text, bool selected = false)
    {
        var button = new Button
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleTabButton(button, selected);
        return button;
    }

    private Control BuildFilePage()
    {
        var page = PageRoot();

        // -----------------------------------------------------------------
        // SINGLE TOP BAR: Sub-tabs on left + Governance Telemetry on right
        // -----------------------------------------------------------------
        var topSubNavRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        topSubNavRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(topSubNavRow);

        var tabBtnRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        tabBtnRow.AddThemeConstantOverride("separation", 6);
        topSubNavRow.AddChild(tabBtnRow);

        var tabObjectivesBtn = TabButton("HEDEFLER & FİNANS", selected: true);
        var tabCareerBtn = TabButton("MENAJER KARİYERİ & BECERİLER", selected: false);
        var tabSaveSettingsBtn = TabButton("KAYIT & AYARLAR", selected: false);
        tabBtnRow.AddChild(tabObjectivesBtn);
        tabBtnRow.AddChild(tabCareerBtn);
        tabBtnRow.AddChild(tabSaveSettingsBtn);

        _govTelemetryBar = new GridContainer
        {
            Columns = 4,
        };
        _govTelemetryBar.AddThemeConstantOverride("h_separation", 6);
        _govTelemetryBar.AddThemeConstantOverride("v_separation", 6);
        topSubNavRow.AddChild(_govTelemetryBar);

        _boardConfidenceBadgePanel = new PanelContainer();
        _boardConfidenceBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
        _boardConfidenceBadgeLabel = new Label { Text = "Güven %—" };
        CareerUiTheme.StyleTableText(_boardConfidenceBadgeLabel);
        _boardConfidenceBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _boardConfidenceBadgePanel.AddChild(_boardConfidenceBadgeLabel);
        _govTelemetryBar.AddChild(_boardConfidenceBadgePanel);

        _operatingBalanceBadgePanel = new PanelContainer();
        _operatingBalanceBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.16f));
        _operatingBalanceBadgeLabel = new Label { Text = "Net —" };
        CareerUiTheme.StyleTableText(_operatingBalanceBadgeLabel);
        _operatingBalanceBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _operatingBalanceBadgePanel.AddChild(_operatingBalanceBadgeLabel);
        _govTelemetryBar.AddChild(_operatingBalanceBadgePanel);

        _stadiumAttendanceBadgePanel = new PanelContainer();
        _stadiumAttendanceBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data, 0.16f));
        _stadiumAttendanceBadgeLabel = new Label { Text = "Doluluk %—" };
        CareerUiTheme.StyleTableText(_stadiumAttendanceBadgeLabel);
        _stadiumAttendanceBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Data);
        _stadiumAttendanceBadgePanel.AddChild(_stadiumAttendanceBadgeLabel);
        _govTelemetryBar.AddChild(_stadiumAttendanceBadgePanel);

        _wageUtilizationBadgePanel = new PanelContainer();
        _wageUtilizationBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Stroke, 0.20f));
        _wageUtilizationBadgeLabel = new Label { Text = "Maaş %—" };
        CareerUiTheme.StyleTableText(_wageUtilizationBadgeLabel);
        _wageUtilizationBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.TextPrimary);
        _wageUtilizationBadgePanel.AddChild(_wageUtilizationBadgeLabel);
        _govTelemetryBar.AddChild(_wageUtilizationBadgePanel);

        // =================================================================
        // TAB 0: HEDEFLER & FİNANS (2-Column Widescreen Split)
        // =================================================================
        var tab0Split = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        tab0Split.AddThemeConstantOverride("separation", 10);
        page.AddChild(tab0Split);

        _fileTopGrid = new GridContainer
        {
            Columns = 1,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.05f,
        };
        _fileTopGrid.AddThemeConstantOverride("h_separation", 8);
        _fileTopGrid.AddThemeConstantOverride("v_separation", 8);
        tab0Split.AddChild(_fileTopGrid);

        var objectivesCard = AddCard(_fileTopGrid, "YÖNETİM HEDEFLERİ", emphasized: true, expandVertical: true);
        _boardObjectivesContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _boardObjectivesContainer.AddThemeConstantOverride("separation", 6);
        objectivesCard.AddChild(_boardObjectivesContainer);

        var rightFinanceCol = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.0f,
        };
        rightFinanceCol.AddThemeConstantOverride("separation", 8);
        tab0Split.AddChild(rightFinanceCol);

        var financeCard = AddCard(rightFinanceCol, "KULÜP FİNANSI & KASASI", emphasized: true, expandVertical: true);
        _fileFinanceGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _fileFinanceGrid.AddThemeConstantOverride("h_separation", 8);
        _fileFinanceGrid.AddThemeConstantOverride("v_separation", 8);
        financeCard.AddChild(_fileFinanceGrid);

        var realPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        realPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.FinanceLedgerCardPanel(true));
        _fileFinanceGrid.AddChild(realPanel);
        var realVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        realVBox.AddThemeConstantOverride("separation", 4);
        realPanel.AddChild(realVBox);
        var realHeader = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        realVBox.AddChild(realHeader);
        var realTitle = new Label { Text = "KASA & GERÇEKLEŞEN", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CareerUiTheme.StyleHeadline(realTitle);
        realTitle.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        realTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
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

        var projPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        projPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.FinanceLedgerCardPanel(true));
        _fileFinanceGrid.AddChild(projPanel);
        var projVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        projVBox.AddThemeConstantOverride("separation", 4);
        projPanel.AddChild(projVBox);
        var projLabel = new Label { Text = "SEZON SONU TAHMİNİ" };
        CareerUiTheme.StyleHeadline(projLabel);
        projLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        projLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        projVBox.AddChild(projLabel);
        _financeProjectionLabel = BodyLabel("FinanceProjectionLabel", autowrap: true);
        projVBox.AddChild(_financeProjectionLabel);

        _clubEconomyLabel = BodyLabel("ClubEconomyLabel", autowrap: true);
        _clubEconomyLabel.Visible = false;
        financeCard.AddChild(_clubEconomyLabel);

        var careerJobCard = AddCard(rightFinanceCol, "KARİYER & İŞ TEKLİFLERİ", emphasized: false);
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

        // =================================================================
        // TAB 1: MENAJER KARİYERİ & BECERİLER (2-Column Widescreen Split)
        // =================================================================
        var tab1Split = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
        };
        tab1Split.AddThemeConstantOverride("separation", 10);
        page.AddChild(tab1Split);

        _fileBottomGrid = new GridContainer
        {
            Columns = 1,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.15f,
        };
        _fileBottomGrid.AddThemeConstantOverride("h_separation", 8);
        _fileBottomGrid.AddThemeConstantOverride("v_separation", 8);
        tab1Split.AddChild(_fileBottomGrid);

        var legacyCard = AddCard(_fileBottomGrid, "MENAJER PROFİLİ & BECERİLER", emphasized: true, expandVertical: true);
        var legacyHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        legacyHeaderRow.AddThemeConstantOverride("separation", 8);
        legacyCard.AddChild(legacyHeaderRow);

        var managerInfoVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        legacyHeaderRow.AddChild(managerInfoVBox);

        _careerLegacyHeadlineLabel = BodyLabel("CareerLegacyHeadlineLabel", autowrap: true);
        CareerUiTheme.StyleHeadline(_careerLegacyHeadlineLabel);
        _careerLegacyHeadlineLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(14));
        managerInfoVBox.AddChild(_careerLegacyHeadlineLabel);

        _legacyTenureLabel = BodyLabel("LegacyTenureLabel", muted: true, autowrap: true);
        managerInfoVBox.AddChild(_legacyTenureLabel);

        _legacyReputationBadgePanel = new PanelContainer();
        _legacyReputationBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.25f));
        _legacyReputationBadgeLabel = new Label { Text = "İtibar: —" };
        CareerUiTheme.StyleHeadline(_legacyReputationBadgeLabel);
        _legacyReputationBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _legacyReputationBadgeLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _legacyReputationBadgePanel.AddChild(_legacyReputationBadgeLabel);
        legacyHeaderRow.AddChild(_legacyReputationBadgePanel);

        _fileManagerLevelBadgePanel = new PanelContainer();
        _fileManagerLevelBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.25f));
        _fileManagerLevelBadgeLabel = new Label { Text = "Seviye 1 (0 XP)" };
        CareerUiTheme.StyleHeadline(_fileManagerLevelBadgeLabel);
        _fileManagerLevelBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _fileManagerLevelBadgeLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _fileManagerLevelBadgePanel.AddChild(_fileManagerLevelBadgeLabel);
        legacyHeaderRow.AddChild(_fileManagerLevelBadgePanel);

        _managerSkillsGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _managerSkillsGrid.AddThemeConstantOverride("h_separation", 8);
        _managerSkillsGrid.AddThemeConstantOverride("v_separation", 8);
        legacyCard.AddChild(_managerSkillsGrid);

        _managerSkillTacticalLabel = CreateLegacyStatCard(_managerSkillsGrid, "TAKTİKSEL ZEKA", "—", CareerUiTheme.ActionBright);
        _managerSkillYouthLabel = CreateLegacyStatCard(_managerSkillsGrid, "GENÇ GELİŞTİRME", "—", CareerUiTheme.Accent);
        _managerSkillManMgmtLabel = CreateLegacyStatCard(_managerSkillsGrid, "İNSAN YÖNETİMİ", "—", CareerUiTheme.Data);
        _managerSkillNegotiationLabel = CreateLegacyStatCard(_managerSkillsGrid, "TRANSFER PAZARLIĞI", "—", CareerUiTheme.ColorWarning);

        _fileLegacyStatsGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _fileLegacyStatsGrid.AddThemeConstantOverride("h_separation", 8);
        _fileLegacyStatsGrid.AddThemeConstantOverride("v_separation", 8);
        legacyCard.AddChild(_fileLegacyStatsGrid);

        _legacyStatMatchesLabel = CreateLegacyStatCard(_fileLegacyStatsGrid, "MAÇLAR", "—", CareerUiTheme.Data);
        _legacyStatWinRateLabel = CreateLegacyStatCard(_fileLegacyStatsGrid, "PERFORMANS", "—", CareerUiTheme.ActionBright);
        _legacyStatYouthLabel = CreateLegacyStatCard(_fileLegacyStatsGrid, "MİRAS", "—", CareerUiTheme.Accent);
        _legacyStatMilestoneLabel = CreateLegacyStatCard(_fileLegacyStatsGrid, "HEDEF", "—", CareerUiTheme.TextPrimary);

        _careerLegacyRecordLabel = BodyLabel("CareerLegacyRecordLabel", autowrap: true);
        _careerLegacyRecordLabel.Visible = false;
        legacyCard.AddChild(_careerLegacyRecordLabel);
        _careerLegacyDevelopmentLabel = BodyLabel("CareerLegacyDevelopmentLabel", autowrap: true);
        _careerLegacyDevelopmentLabel.Visible = false;
        legacyCard.AddChild(_careerLegacyDevelopmentLabel);

        var historyCard = AddCard(tab1Split, "KARİYER GEÇMİŞİ & SEZON ARŞİVİ", emphasized: false, expandVertical: true);
        historyCard.GetParent<PanelContainer>().SizeFlagsStretchRatio = 0.85f;
        _careerSeasonList = new ItemList
        {
            Name = "CareerSeasonList",
            CustomMinimumSize = new Vector2(0, 260),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        CareerUiTheme.StyleList(_careerSeasonList);
        historyCard.AddChild(_careerSeasonList);

        // =================================================================
        // TAB 2: KAYIT & AYARLAR (2-Column Widescreen Split)
        // =================================================================
        var tab2Split = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
        };
        tab2Split.AddThemeConstantOverride("separation", 10);
        page.AddChild(tab2Split);

        var saveCard = AddCard(tab2Split, "KAYIT & OYUN DOSYASI", emphasized: true, expandVertical: true);
        saveCard.GetParent<PanelContainer>().SizeFlagsStretchRatio = 0.95f;
        var saveTelemetryGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        saveTelemetryGrid.AddThemeConstantOverride("h_separation", 7);
        saveTelemetryGrid.AddThemeConstantOverride("v_separation", 7);
        saveCard.AddChild(saveTelemetryGrid);
        _saveLastWriteBadgeLabel = CreateSaveTelemetryBadge(saveTelemetryGrid, "Son Kayıt: —", CareerUiTheme.ActionBright);
        _saveActiveSlotBadgeLabel = CreateSaveTelemetryBadge(saveTelemetryGrid, "Aktif Slot: —", CareerUiTheme.Data);
        _saveWorldSeedBadgeLabel = CreateSaveTelemetryBadge(saveTelemetryGrid, "Dünya Seed: —", CareerUiTheme.Accent);
        _saveFileSizeBadgeLabel = CreateSaveTelemetryBadge(saveTelemetryGrid, "Dosya Boyutu: —", CareerUiTheme.ColorWarning);
        _saveDeskLabel = BodyLabel("SaveDeskLabel", autowrap: true);
        _saveDeskLabel.Visible = false;
        saveCard.AddChild(_saveDeskLabel);

        var saveLoadRow = ActionFlow();
        saveCard.AddChild(saveLoadRow);

        _saveGameButton = AccentButton("Hızlı Kaydet");
        _saveGameButton.Pressed += OnSaveGamePressed;
        saveLoadRow.AddChild(_saveGameButton);

        _loadGameButton = PrimaryButton("Yeniden Yükle");
        _loadGameButton.Pressed += OnLoadGamePressed;
        saveLoadRow.AddChild(_loadGameButton);

        var menuButton = DangerButton("Ana Menüye Dön");
        menuButton.Pressed += () => BackToMenuRequested?.Invoke();
        saveLoadRow.AddChild(menuButton);

        var experienceCard = AddCard(tab2Split, "DENEYİM & ERİŞİLEBİLİRLİK AYARLARI", emphasized: false, expandVertical: true);
        experienceCard.GetParent<PanelContainer>().SizeFlagsStretchRatio = 1.05f;
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

        // Kural 1: Mağaza Telif Kalkanı ve Harici JSON Veri Paketi İçe/Dışa Aktarımı
        var dataPackCard = AddCard(tab2Split, "VERİ PAKETİ & TELİF AYARLARI", emphasized: false, expandVertical: true);
        dataPackCard.GetParent<PanelContainer>().SizeFlagsStretchRatio = 0.95f;

        var dataPackInfo = BodyLabel("DataPackInfoLabel", muted: true, autowrap: true);
        dataPackInfo.Text = "Google Play / App Store lisans uyumluluğu için Mağaza Telif Kalkanı modunu açabilir veya topluluk .json veri paketini dışa/içe aktarabilirsiniz.";
        dataPackCard.AddChild(dataPackInfo);

        var dataPackStatusLabel = BodyLabel("DataPackStatusLabel");
        dataPackStatusLabel.Text = EuropeanElite202627DataPack.StoreSafeMode
            ? "LİSANS MODU: GÜVENLİ TAKMA ADLAR (Store-Safe)"
            : "LİSANS MODU: RESMİ 2026/27 AVRUPA VERİ PAKETİ";
        dataPackStatusLabel.AddThemeColorOverride("font_color", EuropeanElite202627DataPack.StoreSafeMode ? CareerUiTheme.ActionBright : CareerUiTheme.Accent);
        dataPackCard.AddChild(dataPackStatusLabel);

        var dataPackActionRow = ActionFlow();
        dataPackCard.AddChild(dataPackActionRow);

        var toggleStoreSafeBtn = SecondaryButton(EuropeanElite202627DataPack.StoreSafeMode ? "Telif Kalkanı: Açık" : "Telif Kalkanı: Kapalı");
        toggleStoreSafeBtn.Pressed += () =>
        {
            EuropeanElite202627DataPack.StoreSafeMode = !EuropeanElite202627DataPack.StoreSafeMode;
            toggleStoreSafeBtn.Text = EuropeanElite202627DataPack.StoreSafeMode ? "Telif Kalkanı: Açık" : "Telif Kalkanı: Kapalı";
            dataPackStatusLabel.Text = EuropeanElite202627DataPack.StoreSafeMode
                ? "LİSANS MODU: GÜVENLİ TAKMA ADLAR (Store-Safe)"
                : "LİSANS MODU: RESMİ 2026/27 AVRUPA VERİ PAKETİ";
            dataPackStatusLabel.AddThemeColorOverride("font_color", EuropeanElite202627DataPack.StoreSafeMode ? CareerUiTheme.ActionBright : CareerUiTheme.Accent);
        };
        dataPackActionRow.AddChild(toggleStoreSafeBtn);

        var exportDataPackBtn = SecondaryButton("JSON Paketi Dışa Aktar");
        exportDataPackBtn.Pressed += () =>
        {
            try
            {
                var json = EuropeanElite202627DataPack.ExportToJson();
                using var file = Godot.FileAccess.Open("user://custom_datapack.json", Godot.FileAccess.ModeFlags.Write);
                if (file is not null)
                {
                    file.StoreString(json);
                    dataPackInfo.Text = "Topluluk veri paketi 'user://custom_datapack.json' dosyasına başarıyla kaydedildi.";
                }
            }
            catch (Exception ex)
            {
                dataPackInfo.Text = $"Dışa aktarma hatası: {ex.Message}";
            }
        };
        dataPackActionRow.AddChild(exportDataPackBtn);

        var importDataPackBtn = SecondaryButton("JSON Paketi İçe Aktar");
        importDataPackBtn.Pressed += () =>
        {
            try
            {
                if (Godot.FileAccess.FileExists("user://custom_datapack.json"))
                {
                    using var file = Godot.FileAccess.Open("user://custom_datapack.json", Godot.FileAccess.ModeFlags.Read);
                    if (file is not null)
                    {
                        var json = file.GetAsText();
                        var count = EuropeanElite202627DataPack.ImportFromJson(json);
                        dataPackInfo.Text = $"Topluluk paketinden {count} kulüp ve güncel kadroları başarıyla yüklendi!";
                    }
                }
                else
                {
                    dataPackInfo.Text = "'user://custom_datapack.json' dosyası bulunamadı. Lütfen önce bir paket dışa aktarın veya bu konuma dosya yerleştirin.";
                }
            }
            catch (Exception ex)
            {
                dataPackInfo.Text = $"İçe aktarma hatası: {ex.Message}";
            }
        };
        dataPackActionRow.AddChild(importDataPackBtn);

        void SelectFileTab(int index)
        {
            tab0Split.Visible = index == 0;
            tab1Split.Visible = index == 1;
            tab2Split.Visible = index == 2;
            CareerUiTheme.StyleTabButton(tabObjectivesBtn, index == 0);
            CareerUiTheme.StyleTabButton(tabCareerBtn, index == 1);
            CareerUiTheme.StyleTabButton(tabSaveSettingsBtn, index == 2);
        }

        tabObjectivesBtn.Pressed += () => SelectFileTab(0);
        tabCareerBtn.Pressed += () => SelectFileTab(1);
        tabSaveSettingsBtn.Pressed += () => SelectFileTab(2);

        return page;
    }
}
