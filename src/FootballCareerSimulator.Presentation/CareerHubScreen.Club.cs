using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private Control BuildClubPage()
    {
        var page = PageRoot();
        page.SizeFlagsVertical = SizeFlags.ExpandFill;

        // ----------------------------------------------------
        // SINGLE COMPACT TOP BAR: SUB-TABS (LEFT) + TELEMETRY PILLS (RIGHT)
        // ----------------------------------------------------
        var topControlRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topControlRow.AddThemeConstantOverride("separation", 6);
        page.AddChild(topControlRow);

        var tabRow = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        tabRow.AddThemeConstantOverride("separation", 4);
        topControlRow.AddChild(tabRow);

        _clubTabOverviewButton = new Button { Text = "🏛️ KULÜP ÖZETİ" };
        _clubTabOverviewButton.Pressed += () => ShowClubSubPage(ClubSubPage.Overview);
        CareerUiTheme.StyleTabButton(_clubTabOverviewButton, selected: true);
        tabRow.AddChild(_clubTabOverviewButton);

        _clubTabAllButton = new Button { Text = "👥 KADRO & ALTYAPI" };
        _clubTabAllButton.Pressed += () =>
        {
            _squadListFilterCode = null;
            _activeSquadCategoryFilter = "ALL";
            StyleSquadCategoryFilterButtons();
            ShowClubSubPage(ClubSubPage.AllPlayers);
            RefreshSquadList();
        };
        CareerUiTheme.StyleTabButton(_clubTabAllButton, selected: false);
        tabRow.AddChild(_clubTabAllButton);

        _clubTabPlanButton = new Button { Text = "📋 İLK 11 & DERİNLİK" };
        _clubTabPlanButton.Pressed += () => ShowClubSubPage(ClubSubPage.Plan);
        CareerUiTheme.StyleTabButton(_clubTabPlanButton, selected: false);
        tabRow.AddChild(_clubTabPlanButton);

        var topSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topControlRow.AddChild(topSpacer);

        _squadTelemetryRow = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _squadTelemetryRow.AddThemeConstantOverride("separation", 5);
        topControlRow.AddChild(_squadTelemetryRow);

        _squadOvrBadgePanel = new PanelContainer();
        _squadOvrBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.20f));
        _squadOvrBadgeLabel = new Label { Text = "⭐ Güç —" };
        CareerUiTheme.StyleTableText(_squadOvrBadgeLabel);
        _squadOvrBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _squadOvrBadgePanel.AddChild(_squadOvrBadgeLabel);
        _squadTelemetryRow.AddChild(_squadOvrBadgePanel);

        _squadCapacityBadgePanel = new PanelContainer();
        _squadCapacityBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.16f));
        _squadCapacityBadgeLabel = new Label { Text = "👥 —/25" };
        CareerUiTheme.StyleTableText(_squadCapacityBadgeLabel);
        _squadCapacityBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _squadCapacityBadgePanel.AddChild(_squadCapacityBadgeLabel);
        _squadTelemetryRow.AddChild(_squadCapacityBadgePanel);

        _squadAvgAgeBadgePanel = new PanelContainer();
        _squadAvgAgeBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data, 0.16f));
        _squadAvgAgeBadgeLabel = new Label { Text = "🎂 Yaş —" };
        CareerUiTheme.StyleTableText(_squadAvgAgeBadgeLabel);
        _squadAvgAgeBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Data);
        _squadAvgAgeBadgePanel.AddChild(_squadAvgAgeBadgeLabel);
        _squadTelemetryRow.AddChild(_squadAvgAgeBadgePanel);

        _squadTotalValueBadgePanel = new PanelContainer();
        _squadTotalValueBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.16f));
        _squadTotalValueBadgeLabel = new Label { Text = "💰 Değer —" };
        CareerUiTheme.StyleTableText(_squadTotalValueBadgeLabel);
        _squadTotalValueBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _squadTotalValueBadgePanel.AddChild(_squadTotalValueBadgeLabel);
        _squadTelemetryRow.AddChild(_squadTotalValueBadgePanel);

        _squadWageBillBadgePanel = new PanelContainer();
        _squadWageBillBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Stroke, 0.20f));
        _squadWageBillBadgeLabel = new Label { Text = "💵 Maaş —" };
        CareerUiTheme.StyleTableText(_squadWageBillBadgeLabel);
        _squadWageBillBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.TextPrimary);
        _squadWageBillBadgePanel.AddChild(_squadWageBillBadgeLabel);
        _squadTelemetryRow.AddChild(_squadWageBillBadgePanel);

        _squadFacilityBadgePanel = new PanelContainer();
        _squadFacilityBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning, 0.20f));
        _squadFacilityBadgeLabel = new Label { Text = "🏗️ Tesis: —" };
        CareerUiTheme.StyleTableText(_squadFacilityBadgeLabel);
        _squadFacilityBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ColorWarning);
        _squadFacilityBadgePanel.AddChild(_squadFacilityBadgeLabel);
        _squadTelemetryRow.AddChild(_squadFacilityBadgePanel);

        _squadChemistryBadgePanel = new PanelContainer();
        _squadChemistryBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
        _squadChemistryBadgeLabel = new Label { Text = "👥 Kimya: —" };
        CareerUiTheme.StyleTableText(_squadChemistryBadgeLabel);
        _squadChemistryBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _squadChemistryBadgePanel.AddChild(_squadChemistryBadgeLabel);
        _squadTelemetryRow.AddChild(_squadChemistryBadgePanel);

        // ====================================================
        // SUB-PAGE 1: KULÜP ÖZETİ (2-COLUMN WIDESCREEN BENTO)
        // Left: Top 3 Stars + SWOT Report | Right: Facilities + Active XI Pitch
        // ====================================================
        _clubOverviewPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _clubOverviewPanel.AddThemeConstantOverride("separation", 8);
        page.AddChild(_clubOverviewPanel);

        var overviewGrid = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        overviewGrid.AddThemeConstantOverride("separation", 10);
        _clubOverviewPanel.AddChild(overviewGrid);

        var overviewLeftCol = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.25f,
        };
        overviewLeftCol.AddThemeConstantOverride("separation", 8);
        overviewGrid.AddChild(overviewLeftCol);

        var overviewRightCol = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.0f,
        };
        overviewRightCol.AddThemeConstantOverride("separation", 8);
        overviewGrid.AddChild(overviewRightCol);

        // Left Top: ⭐ TAKIMIN YILDIZLARI (Top 3 Stars)
        var spotlightCard = AddCard(overviewLeftCol, "⭐ TAKIMIN YILDIZLARI", emphasized: true);
        _starSpotlightHeadline = BodyLabel("StarSpotlightHeadline", muted: true, autowrap: true);
        _starSpotlightHeadline.Visible = false;
        spotlightCard.AddChild(_starSpotlightHeadline);

        _starSpotlightDesk = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _starSpotlightDesk.AddThemeConstantOverride("separation", 8);
        spotlightCard.AddChild(_starSpotlightDesk);

        for (var i = 0; i < 3; i++)
        {
            var starIndex = i;
            var card = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            card.AddThemeStyleboxOverride("panel", CareerUiTheme.SquadStarCardPanel());
            _starSpotlightDesk.AddChild(card);
            _starCards[i] = card;

            var cardVBox = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            cardVBox.AddThemeConstantOverride("separation", 4);
            card.AddChild(cardVBox);

            var topRow = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            topRow.AddThemeConstantOverride("separation", 6);
            cardVBox.AddChild(topRow);

            var posBadge = new Label { Text = "[—]" };
            CareerUiTheme.StyleBody(posBadge);
            posBadge.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
            topRow.AddChild(posBadge);
            _starPosBadges[i] = posBadge;

            var ratingBadge = new Label { Text = "⭐ —" };
            CareerUiTheme.StyleTableText(ratingBadge);
            ratingBadge.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
            topRow.AddChild(ratingBadge);
            _starRatingBadges[i] = ratingBadge;

            var nameLabel = new Label { Text = "Oyuncu —" };
            CareerUiTheme.StyleHeadline(nameLabel);
            nameLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(14));
            nameLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            cardVBox.AddChild(nameLabel);
            _starNameLabels[i] = nameLabel;

            var metaLabel = BodyLabel($"StarMeta{i}", muted: true, autowrap: true);
            metaLabel.Text = "— Yaş · %— Fit · — TRY";
            metaLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
            cardVBox.AddChild(metaLabel);
            _starMetaLabels[i] = metaLabel;

            var inspectBtn = SecondaryButton("🔍 Profil");
            inspectBtn.CustomMinimumSize = new Vector2(0, 26);
            inspectBtn.Pressed += () =>
            {
                if (_starPlayerIds[starIndex] is long pid)
                {
                    OpenPlayerDossier(pid);
                }
            };
            cardVBox.AddChild(inspectBtn);
            _starInspectButtons[i] = inspectBtn;
        }

        // Left Bottom: 📋 TAKIM RAPORU (SWOT)
        var swotCard = AddCard(overviewLeftCol, "📋 KADRO ANALİZ RAPORU (SWOT)", emphasized: true, expandVertical: true);
        _swotPanel = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _swotPanel.AddThemeConstantOverride("separation", 8);
        swotCard.AddChild(_swotPanel);

        var strengthsBox = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        strengthsBox.AddThemeStyleboxOverride("panel", CareerUiTheme.SwotStrengthPanel());
        _swotPanel.AddChild(strengthsBox);

        _swotStrengthsVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _swotStrengthsVBox.AddThemeConstantOverride("separation", 4);
        strengthsBox.AddChild(_swotStrengthsVBox);

        var strengthsTitle = new Label { Text = "✓ GÜÇLÜ YÖNLER" };
        CareerUiTheme.StyleHeadline(strengthsTitle);
        strengthsTitle.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        strengthsTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        _swotStrengthsVBox.AddChild(strengthsTitle);

        var weaknessesBox = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        weaknessesBox.AddThemeStyleboxOverride("panel", CareerUiTheme.SwotWeaknessPanel());
        _swotPanel.AddChild(weaknessesBox);

        _swotWeaknessesVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _swotWeaknessesVBox.AddThemeConstantOverride("separation", 4);
        weaknessesBox.AddChild(_swotWeaknessesVBox);

        var weaknessesTitle = new Label { Text = "⚠️ RİSKLER & EKSİKLER" };
        CareerUiTheme.StyleHeadline(weaknessesTitle);
        weaknessesTitle.AddThemeColorOverride("font_color", CareerUiTheme.DangerSoft);
        weaknessesTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        _swotWeaknessesVBox.AddChild(weaknessesTitle);

        var swotActionRow = ActionFlow();
        swotCard.AddChild(swotActionRow);

        _clubDeclareNeedButton = PrimaryButton("🎯 Transfer İhtiyacı Bildir");
        _clubDeclareNeedButton.Pressed += () =>
        {
            Apply(_controller.DeclarePositionGapNeed());
            ShowPage(HubPage.Transfer);
        };
        swotActionRow.AddChild(_clubDeclareNeedButton);

        _swotRestFatiguedButton = SecondaryButton("⚡ Takımı Dinlendir");
        _swotRestFatiguedButton.Pressed += () =>
        {
            Apply(_controller.RestEntireTeam());
            ShowPage(HubPage.Prep);
        };
        swotActionRow.AddChild(_swotRestFatiguedButton);

        _swotViewAllSquadButton = SecondaryButton("👥 Tüm Kadroyu Aç →");
        _swotViewAllSquadButton.Pressed += () =>
        {
            _squadListFilterCode = null;
            _activeSquadCategoryFilter = "ALL";
            StyleSquadCategoryFilterButtons();
            ShowClubSubPage(ClubSubPage.AllPlayers);
            RefreshSquadList();
        };
        swotActionRow.AddChild(_swotViewAllSquadButton);

        // Right Top: 🏛️ KULÜP TESİSLERİ VE ALTYAPI KARTI
        var facilityCard = AddCard(overviewRightCol, "🏛️ KULÜP TESİSLERİ & KÜLTÜR", emphasized: true);
        var facilityInfoBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        facilityInfoBox.AddThemeConstantOverride("separation", 3);
        facilityCard.AddChild(facilityInfoBox);

        _clubFacilityLevelLabel = BodyLabel("ClubFacilityLevel", autowrap: true);
        CareerUiTheme.StyleHeadline(_clubFacilityLevelLabel);
        _clubFacilityLevelLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(14));
        facilityInfoBox.AddChild(_clubFacilityLevelLabel);

        _clubFacilityMultiplierLabel = BodyLabel("ClubFacilityMultiplier", muted: true, autowrap: true);
        _clubFacilityMultiplierLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        facilityInfoBox.AddChild(_clubFacilityMultiplierLabel);

        _clubCultureLabel = BodyLabel("ClubCulture", muted: true, autowrap: true);
        _clubCultureLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        facilityInfoBox.AddChild(_clubCultureLabel);

        _clubFacilityCostLabel = BodyLabel("ClubFacilityCost", muted: true, autowrap: true);
        _clubFacilityCostLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        facilityInfoBox.AddChild(_clubFacilityCostLabel);

        _upgradeFacilityButton = PrimaryButton("🏗️ Tesisleri Geliştir");
        _upgradeFacilityButton.Pressed += () =>
        {
            var res = _controller.UpgradeFacilities();
            Apply(res);
            RefreshUi();
            RefreshSquadList();
        };
        facilityCard.AddChild(_upgradeFacilityButton);

        // Right Bottom: ⚽ AKTİF İLK 11 SAHA GÖRÜNÜMÜ
        var pitchCard = AddCard(overviewRightCol, "⚽ AKTİF İLK 11 DİZİLİŞİ", emphasized: false, expandVertical: true);
        _clubPitchCaptionLabel = BodyLabel("ClubPitchCaption", muted: true, autowrap: true);
        pitchCard.AddChild(_clubPitchCaptionLabel);
        _clubPitchHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 210),
        };
        pitchCard.AddChild(_clubPitchHost);

        // Hidden legacy labels kept for controller/refresh compatibility
        _clubOverviewHeadlineLabel = BodyLabel("ClubOverviewHeadline", autowrap: true);
        _clubOverviewHeadlineLabel.Visible = false;
        _clubOverviewPanel.AddChild(_clubOverviewHeadlineLabel);
        _squadCapacityLabel = BodyLabel("SquadCapacityLabel", autowrap: true);
        _squadCapacityLabel.Visible = false;
        _clubOverviewPanel.AddChild(_squadCapacityLabel);
        _squadStatusLabel = BodyLabel("SquadStatusLabel", muted: true, autowrap: true);
        _squadStatusLabel.Visible = false;
        _clubOverviewPanel.AddChild(_squadStatusLabel);
        _clubDepthLabel = BodyLabel("ClubDepthLabel", autowrap: true);
        _clubDepthLabel.Visible = false;
        _clubOverviewPanel.AddChild(_clubDepthLabel);
        _clubSignalRow = new HBoxContainer { Visible = false };
        _clubOverviewPanel.AddChild(_clubSignalRow);

        // ====================================================
        // SUB-PAGE 2: KADRO & ALTYAPI (2-COLUMN WIDESCREEN TABLE + ACADEMY/DYNAMICS)
        // Left: Full Squad Table (8-10 visible rows) + Quick Actions
        // Right: Youth Academy + Dressing Room Dynamics
        // ====================================================
        _clubAllPlayersPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
        };
        _clubAllPlayersPanel.AddThemeConstantOverride("separation", 8);
        page.AddChild(_clubAllPlayersPanel);

        var playersSplitRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        playersSplitRow.AddThemeConstantOverride("separation", 10);
        _clubAllPlayersPanel.AddChild(playersSplitRow);

        // Left Column: A Takım Kadro Tablosu + Hızlı İşlemler
        var playerManagementCard = AddCard(playersSplitRow, "👥 A TAKIM KADROSU", emphasized: true, expandVertical: true);
        if (playerManagementCard.GetParent() is Control pmPanel)
        {
            pmPanel.SizeFlagsStretchRatio = 1.45f;
        }

        var filterHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        filterHeaderRow.AddThemeConstantOverride("separation", 6);
        playerManagementCard.AddChild(filterHeaderRow);

        _squadFilterBar = new HFlowContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _squadFilterBar.AddThemeConstantOverride("h_separation", 4);
        _squadFilterBar.AddThemeConstantOverride("v_separation", 4);
        filterHeaderRow.AddChild(_squadFilterBar);

        _squadFilterAllBtn = PrimaryButton("🌍 Tümü");
        _squadFilterAllBtn.Pressed += () => ApplySquadCategoryFilter("ALL");
        _squadFilterBar.AddChild(_squadFilterAllBtn);

        _squadFilterGkBtn = SecondaryButton("🧤 KL");
        _squadFilterGkBtn.Pressed += () => ApplySquadCategoryFilter("GK");
        _squadFilterBar.AddChild(_squadFilterGkBtn);

        _squadFilterDefBtn = SecondaryButton("🛡️ DEF");
        _squadFilterDefBtn.Pressed += () => ApplySquadCategoryFilter("DEF");
        _squadFilterBar.AddChild(_squadFilterDefBtn);

        _squadFilterMidBtn = SecondaryButton("⚡ ORT");
        _squadFilterMidBtn.Pressed += () => ApplySquadCategoryFilter("MID");
        _squadFilterBar.AddChild(_squadFilterMidBtn);

        _squadFilterFwdBtn = SecondaryButton("🎯 HÜC");
        _squadFilterFwdBtn.Pressed += () => ApplySquadCategoryFilter("FWD");
        _squadFilterBar.AddChild(_squadFilterFwdBtn);

        _squadFilterYouthBtn = SecondaryButton("🌱 U21");
        _squadFilterYouthBtn.Pressed += () => ApplySquadCategoryFilter("YOUTH");
        _squadFilterBar.AddChild(_squadFilterYouthBtn);

        _squadFilterRiskBtn = SecondaryButton("⚠️ Riskli");
        _squadFilterRiskBtn.Pressed += () => ApplySquadCategoryFilter("RISK");
        _squadFilterBar.AddChild(_squadFilterRiskBtn);

        _playerManagementHeadlineLabel = BodyLabel("PlayerManagementHeadlineLabel", muted: true);
        _playerManagementHeadlineLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        filterHeaderRow.AddChild(_playerManagementHeadlineLabel);

        _squadList = new ItemList
        {
            Name = "SquadList",
            AutoHeight = false,
            CustomMinimumSize = new Vector2(0, 250),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_squadList);
        _squadList.AllowReselect = true;
        _squadList.ItemSelected += OnSquadPlayerSelected;
        _squadList.ItemActivated += OnSquadPlayerActivated;
        playerManagementCard.AddChild(_squadList);

        _playerDetailLabel = BodyLabel("PlayerDetailLabel", muted: true, autowrap: true);
        _playerDetailLabel.Text = "Seçili oyuncu için hızlı işlemler:";
        _playerDetailLabel.Visible = false;
        playerManagementCard.AddChild(_playerDetailLabel);

        var jobRow = ActionFlow();
        playerManagementCard.AddChild(jobRow);

        _openSelectedPlayerDossierButton = AccentButton("🔍 Oyuncu Profili & Görüşme");
        _openSelectedPlayerDossierButton.TooltipText = "Seçili oyuncunun ayrıntılı analiz dosyasını ve özel görüşme ekranını aç";
        _openSelectedPlayerDossierButton.Pressed += () =>
        {
            if (_selectedPlayerId is long id)
            {
                OpenPlayerDossier(id);
            }
        };
        jobRow.AddChild(_openSelectedPlayerDossierButton);

        _promiseStartButton = SecondaryButton("🤝 İlk 11 Sözü");
        _promiseStartButton.Pressed += () =>
            ApplySelectedPlayer(_controller.PromiseStartingOpportunityToPlayer);
        jobRow.AddChild(_promiseStartButton);

        _promisePlayingTimeButton = SecondaryButton("⏱ Süre Sözü");
        _promisePlayingTimeButton.Pressed += () =>
            ApplySelectedPlayer(_controller.PromisePlayingTimeToPlayer);
        jobRow.AddChild(_promisePlayingTimeButton);

        _sellFringeButton = DangerButton("🏷 Satışa Çıkar");
        _sellFringeButton.Pressed += () => Apply(_controller.SellFringePlayerFromManagedClub());
        jobRow.AddChild(_sellFringeButton);

        _promoteOverflowButton = SecondaryButton("⬆ Taşanı Kadroya Al");
        _promoteOverflowButton.Pressed += () => Apply(_controller.PromoteOverflowPlayerToSquad());
        jobRow.AddChild(_promoteOverflowButton);

        _releaseCapacityButton = SecondaryButton("✂ Yer Aç");
        _releaseCapacityButton.Pressed += () => Apply(_controller.ReleaseToFreeSquadCapacity());
        jobRow.AddChild(_releaseCapacityButton);

        // Right Column: Altyapı Akademisi + Soyunma Odası Dinamikleri
        var playersRightCol = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.0f,
        };
        playersRightCol.AddThemeConstantOverride("separation", 8);
        playersSplitRow.AddChild(playersRightCol);

        var academyCard = AddCard(playersRightCol, "🌱 ALTYAPI AKADEMİSİ", expandVertical: true);
        _academyIntakeLabel = BodyLabel("AcademyIntakeLabel", autowrap: true);
        academyCard.AddChild(_academyIntakeLabel);
        _academyCandidateList = new ItemList
        {
            Name = "AcademyCandidateList",
            CustomMinimumSize = new Vector2(0, 74),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_academyCandidateList);
        _academyCandidateList.ItemSelected += OnAcademyCandidateSelected;
        academyCard.AddChild(_academyCandidateList);

        var academyRow = ActionFlow();
        academyCard.AddChild(academyRow);
        _academyAcceptButton = PrimaryButton("✓ Akademide Tut");
        _academyAcceptButton.Pressed += () => ApplySelectedAcademyCandidate(accept: true);
        academyRow.AddChild(_academyAcceptButton);
        _academyRejectButton = SecondaryButton("✕ Kapat");
        _academyRejectButton.Pressed += () => ApplySelectedAcademyCandidate(accept: false);
        academyRow.AddChild(_academyRejectButton);
        _academyPromoteButton = AccentButton("⬆ A Takıma Çıkar");
        _academyPromoteButton.Pressed += PromoteSelectedAcademyPlayer;
        academyRow.AddChild(_academyPromoteButton);

        _academyLifecycleLabel = BodyLabel("AcademyLifecycleLabel", muted: true, autowrap: true);
        academyCard.AddChild(_academyLifecycleLabel);
        _academyLifecycleList = new ItemList
        {
            Name = "AcademyLifecycleList",
            CustomMinimumSize = new Vector2(0, 68),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_academyLifecycleList);
        _academyLifecycleList.ItemSelected += OnAcademyLifecyclePlayerSelected;
        academyCard.AddChild(_academyLifecycleList);

        var teamDynamicsCard = AddCard(playersRightCol, "🤝 SOYUNMA ODASI & DİNAMİKLER");
        _dressingRoomEchoLabel = BodyLabel("DressingRoomEchoLabel", autowrap: true);
        _dressingRoomEchoLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        teamDynamicsCard.AddChild(_dressingRoomEchoLabel);
        _memoryLabel = BodyLabel("MemoryLabel", autowrap: true);
        teamDynamicsCard.AddChild(_memoryLabel);
        _promiseLabel = BodyLabel("PromiseLabel", autowrap: true);
        teamDynamicsCard.AddChild(_promiseLabel);
        _relationshipLabel = BodyLabel("RelationshipLabel", autowrap: true);
        teamDynamicsCard.AddChild(_relationshipLabel);
        _decisionLabel = BodyLabel("DecisionLabel", autowrap: true);
        teamDynamicsCard.AddChild(_decisionLabel);
        _developmentLabel = BodyLabel("DevelopmentLabel", autowrap: true);
        _developmentLabel.Visible = false;
        teamDynamicsCard.AddChild(_developmentLabel);
        _contractLabel = BodyLabel("ContractLabel", autowrap: true);
        _contractLabel.Visible = false;
        teamDynamicsCard.AddChild(_contractLabel);
        _homeKit = new TextureRect { Visible = false };
        _awayKit = new TextureRect { Visible = false };
        _thirdKit = new TextureRect { Visible = false };

        var decisionRow = ActionFlow();
        decisionRow.Visible = false;
        teamDynamicsCard.AddChild(decisionRow);
        _openDecisionButton = SecondaryButton("Süre Talebi Aç");
        _openDecisionButton.Pressed += () =>
            ApplySelectedPlayer(_controller.OpenPlayingTimeDecisionForPlayer);
        decisionRow.AddChild(_openDecisionButton);
        _openStartingDecisionButton = SecondaryButton("İlk 11 Talebi Aç");
        _openStartingDecisionButton.Pressed += () =>
            ApplySelectedPlayer(_controller.OpenStartingOpportunityDecisionForPlayer);
        decisionRow.AddChild(_openStartingDecisionButton);
        _openTransferDecisionButton = SecondaryButton("Transfer Talebi Aç");
        _openTransferDecisionButton.Pressed += () =>
            ApplySelectedPlayer(_controller.OpenTransferDecisionForPlayer);
        decisionRow.AddChild(_openTransferDecisionButton);
        _openDisciplineDecisionButton = SecondaryButton("Disiplin Aç");
        _openDisciplineDecisionButton.Pressed += () =>
            ApplySelectedPlayer(_controller.OpenDisciplineDecisionForPlayer);
        decisionRow.AddChild(_openDisciplineDecisionButton);
        _openBoardDemandDecisionButton = SecondaryButton("Yönetim Talebi Aç");
        _openBoardDemandDecisionButton.Pressed += () => Apply(_controller.OpenBoardDemandDecision());
        decisionRow.AddChild(_openBoardDemandDecisionButton);
        _openPressQuestionDecisionButton = SecondaryButton("Basın Sorusu Aç");
        _openPressQuestionDecisionButton.Pressed += () =>
            Apply(_controller.OpenPressQuestionDecisionForOldestSquadPlayer());
        decisionRow.AddChild(_openPressQuestionDecisionButton);

        // ====================================================
        // SUB-PAGE 3: İLK 11 & MEVKİ DERİNLİĞİ (SIDE-BY-SIDE)
        // ====================================================
        _clubPlanPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
        };
        _clubPlanPanel.AddThemeConstantOverride("separation", 8);
        page.AddChild(_clubPlanPanel);

        var planCard = AddCard(_clubPlanPanel, "📋 İLK 11 YERLEŞİMİ & MEVKİ DERİNLİĞİ", emphasized: true, expandVertical: true);
        var planActionRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        planActionRow.AddThemeConstantOverride("separation", 8);
        planCard.AddChild(planActionRow);
        _clubPlanCaptionLabel = BodyLabel("ClubPlanCaption", muted: true, autowrap: true);
        _clubPlanCaptionLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        planActionRow.AddChild(_clubPlanCaptionLabel);
        var clubSmartRotBtn = PrimaryButton("🎯 Akıllı Rotasyon 11'i Uygula");
        clubSmartRotBtn.Pressed += () =>
        {
            Apply(_controller.ApplySmartRotationForNextMatch());
            RefreshClubPlanBoard();
        };
        planActionRow.AddChild(clubSmartRotBtn);

        var planSplitRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        planSplitRow.AddThemeConstantOverride("separation", 10);
        planCard.AddChild(planSplitRow);

        _clubPlanPitchHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.25f,
            CustomMinimumSize = new Vector2(0, 290),
        };
        planSplitRow.AddChild(_clubPlanPitchHost);

        _squadDepthGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.0f,
        };
        _squadDepthGrid.AddThemeConstantOverride("h_separation", 8);
        _squadDepthGrid.AddThemeConstantOverride("v_separation", 8);
        planSplitRow.AddChild(_squadDepthGrid);

        ShowClubSubPage(ClubSubPage.Overview);
        return page;
    }

    private void ShowClubSubPage(ClubSubPage page)
    {
        _clubSubPage = page;
        _clubOverviewPanel.Visible = page == ClubSubPage.Overview;
        _clubAllPlayersPanel.Visible = page == ClubSubPage.AllPlayers;
        _clubPlanPanel.Visible = page == ClubSubPage.Plan;
        StyleClubTab(_clubTabOverviewButton, page == ClubSubPage.Overview);
        StyleClubTab(_clubTabAllButton, page == ClubSubPage.AllPlayers);
        StyleClubTab(_clubTabPlanButton, page == ClubSubPage.Plan);
        if (page == ClubSubPage.Plan)
        {
            RefreshClubPlanBoard();
        }
    }

    private static void StyleClubTab(Button button, bool active)
    {
        CareerUiTheme.StyleTabButton(button, active);
    }

    private void ApplySquadCategoryFilter(string category)
    {
        _activeSquadCategoryFilter = category;
        StyleSquadCategoryFilterButtons();
        RefreshSquadList();
    }

    private void StyleSquadCategoryFilterButtons()
    {
        StyleClubTab(_squadFilterAllBtn, _activeSquadCategoryFilter == "ALL");
        StyleClubTab(_squadFilterGkBtn, _activeSquadCategoryFilter == "GK");
        StyleClubTab(_squadFilterDefBtn, _activeSquadCategoryFilter == "DEF");
        StyleClubTab(_squadFilterMidBtn, _activeSquadCategoryFilter == "MID");
        StyleClubTab(_squadFilterFwdBtn, _activeSquadCategoryFilter == "FWD");
        StyleClubTab(_squadFilterYouthBtn, _activeSquadCategoryFilter == "YOUTH");
        StyleClubTab(_squadFilterRiskBtn, _activeSquadCategoryFilter == "RISK");
    }

    private static int EstimateSquadPlayerMarketValue(PlayerManagementLine player, int teamStrength)
    {
        var valuation = ScoutTransferValuationModel.Evaluate(
            player.Rating,
            player.PotentialAbility,
            player.Age,
            teamStrength,
            teamStrength,
            player.Rating);
        return valuation.MarketValue;
    }
}
