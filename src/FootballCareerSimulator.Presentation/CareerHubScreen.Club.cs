using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private Control BuildClubPage()
    {
        var page = PageRoot();

        // Tab bar — referans tasarımındaki gibi yeşil dolgu seçili tab
        var tabRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tabRow.AddThemeConstantOverride("separation", CareerUiTheme.SpaceXs);
        page.AddChild(tabRow);
        _clubTabOverviewButton = new Button { Text = "ÖZET", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _clubTabOverviewButton.Pressed += () => ShowClubSubPage(ClubSubPage.Overview);
        CareerUiTheme.StyleTabButton(_clubTabOverviewButton, selected: true);
        tabRow.AddChild(_clubTabOverviewButton);
        _clubTabAllButton = new Button { Text = "OYUNCULAR", SizeFlagsHorizontal = SizeFlags.ExpandFill };
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
        _clubTabPlanButton = new Button { Text = "İLK 11", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _clubTabPlanButton.Pressed += () => ShowClubSubPage(ClubSubPage.Plan);
        CareerUiTheme.StyleTabButton(_clubTabPlanButton, selected: false);
        tabRow.AddChild(_clubTabPlanButton);

        _squadTelemetryRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _squadTelemetryRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(_squadTelemetryRow);

        _squadOvrBadgePanel = new PanelContainer();
        _squadOvrBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.20f));
        _squadOvrBadgeLabel = new Label { Text = "⭐ Güç —" };
        CareerUiTheme.StyleTableText(_squadOvrBadgeLabel);
        _squadOvrBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _squadOvrBadgePanel.AddChild(_squadOvrBadgeLabel);
        _squadTelemetryRow.AddChild(_squadOvrBadgePanel);

        _squadCapacityBadgePanel = new PanelContainer();
        _squadCapacityBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.16f));
        _squadCapacityBadgeLabel = new Label { Text = "👥 Kadro: — / 25" };
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

        _clubOverviewPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _clubOverviewPanel.AddThemeConstantOverride("separation", 10);
        page.AddChild(_clubOverviewPanel);

        var spotlightCard = AddCard(_clubOverviewPanel, "⭐ YILDIZLAR", emphasized: true);
        _starSpotlightHeadline = BodyLabel("StarSpotlightHeadline", muted: true, autowrap: true);
        _starSpotlightHeadline.Text = "Takımın en yüksek yetenek seviyesine ve piyasa değerine sahip kilit as oyuncuları.";
        _starSpotlightHeadline.Visible = false;
        spotlightCard.AddChild(_starSpotlightHeadline);

        _starSpotlightDesk = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _starSpotlightDesk.AddThemeConstantOverride("separation", 10);
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
            cardVBox.AddThemeConstantOverride("separation", 6);
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
            nameLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
            nameLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            cardVBox.AddChild(nameLabel);
            _starNameLabels[i] = nameLabel;

            var metaLabel = BodyLabel($"StarMeta{i}", muted: true, autowrap: true);
            metaLabel.Text = "— Yaş · %— Fit · — TRY";
            metaLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
            cardVBox.AddChild(metaLabel);
            _starMetaLabels[i] = metaLabel;

            var inspectBtn = SecondaryButton("Profil");
            inspectBtn.CustomMinimumSize = new Vector2(0, 36);
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

        var swotCard = AddCard(_clubOverviewPanel, "📋 TAKIM RAPORU", emphasized: true);
        _swotPanel = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _swotPanel.AddThemeConstantOverride("separation", 12);
        swotCard.AddChild(_swotPanel);

        var strengthsBox = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        strengthsBox.AddThemeStyleboxOverride("panel", CareerUiTheme.SwotStrengthPanel());
        _swotPanel.AddChild(strengthsBox);

        _swotStrengthsVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _swotStrengthsVBox.AddThemeConstantOverride("separation", 6);
        strengthsBox.AddChild(_swotStrengthsVBox);

        var strengthsTitle = new Label { Text = "GÜÇLÜ" };
        CareerUiTheme.StyleHeadline(strengthsTitle);
        strengthsTitle.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        strengthsTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        _swotStrengthsVBox.AddChild(strengthsTitle);

        var weaknessesBox = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        weaknessesBox.AddThemeStyleboxOverride("panel", CareerUiTheme.SwotWeaknessPanel());
        _swotPanel.AddChild(weaknessesBox);

        _swotWeaknessesVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _swotWeaknessesVBox.AddThemeConstantOverride("separation", 6);
        weaknessesBox.AddChild(_swotWeaknessesVBox);

        var weaknessesTitle = new Label { Text = "RİSKLER" };
        CareerUiTheme.StyleHeadline(weaknessesTitle);
        weaknessesTitle.AddThemeColorOverride("font_color", CareerUiTheme.DangerSoft);
        weaknessesTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        _swotWeaknessesVBox.AddChild(weaknessesTitle);

        var swotActionRow = ActionFlow();
        swotCard.AddChild(swotActionRow);

        _clubDeclareNeedButton = PrimaryButton("🎯 Transfer");
        _clubDeclareNeedButton.Pressed += () =>
        {
            Apply(_controller.DeclarePositionGapNeed());
            ShowPage(HubPage.Transfer);
        };
        swotActionRow.AddChild(_clubDeclareNeedButton);

        _swotRestFatiguedButton = SecondaryButton("⚡ Dinlendir");
        _swotRestFatiguedButton.Pressed += () =>
        {
            Apply(_controller.RestEntireTeam());
            ShowPage(HubPage.Prep);
        };
        swotActionRow.AddChild(_swotRestFatiguedButton);

        _swotViewAllSquadButton = SecondaryButton("👥 Tüm Kadro");
        _swotViewAllSquadButton.Pressed += () =>
        {
            _squadListFilterCode = null;
            _activeSquadCategoryFilter = "ALL";
            StyleSquadCategoryFilterButtons();
            ShowClubSubPage(ClubSubPage.AllPlayers);
            RefreshSquadList();
        };
        swotActionRow.AddChild(_swotViewAllSquadButton);

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

        var pitchCard = AddCard(_clubOverviewPanel, "İLK 11", emphasized: true);
        _clubPitchCaptionLabel = BodyLabel("ClubPitchCaption", muted: true, autowrap: true);
        pitchCard.AddChild(_clubPitchCaptionLabel);
        _clubPitchHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 340),
        };
        pitchCard.AddChild(_clubPitchHost);

        _clubAllPlayersPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
        };
        _clubAllPlayersPanel.AddThemeConstantOverride("separation", 10);
        page.AddChild(_clubAllPlayersPanel);

        var playerManagementCard = AddCard(_clubAllPlayersPanel, "OYUNCULAR", emphasized: true);
        _playerManagementHeadlineLabel = BodyLabel("PlayerManagementHeadlineLabel", autowrap: true);
        playerManagementCard.AddChild(_playerManagementHeadlineLabel);

        _squadFilterBar = new HFlowContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _squadFilterBar.AddThemeConstantOverride("h_separation", 6);
        _squadFilterBar.AddThemeConstantOverride("v_separation", 6);
        playerManagementCard.AddChild(_squadFilterBar);

        _squadFilterAllBtn = PrimaryButton("🌍 Tümü");
        _squadFilterAllBtn.Pressed += () => ApplySquadCategoryFilter("ALL");
        _squadFilterBar.AddChild(_squadFilterAllBtn);

        _squadFilterGkBtn = SecondaryButton("🧤 Kaleci");
        _squadFilterGkBtn.Pressed += () => ApplySquadCategoryFilter("GK");
        _squadFilterBar.AddChild(_squadFilterGkBtn);

        _squadFilterDefBtn = SecondaryButton("🛡️ Defans");
        _squadFilterDefBtn.Pressed += () => ApplySquadCategoryFilter("DEF");
        _squadFilterBar.AddChild(_squadFilterDefBtn);

        _squadFilterMidBtn = SecondaryButton("⚡ Orta");
        _squadFilterMidBtn.Pressed += () => ApplySquadCategoryFilter("MID");
        _squadFilterBar.AddChild(_squadFilterMidBtn);

        _squadFilterFwdBtn = SecondaryButton("🎯 Hücum");
        _squadFilterFwdBtn.Pressed += () => ApplySquadCategoryFilter("FWD");
        _squadFilterBar.AddChild(_squadFilterFwdBtn);

        _squadFilterYouthBtn = SecondaryButton("🌱 U21");
        _squadFilterYouthBtn.Pressed += () => ApplySquadCategoryFilter("YOUTH");
        _squadFilterBar.AddChild(_squadFilterYouthBtn);

        _squadFilterRiskBtn = SecondaryButton("⚠️ Riskli");
        _squadFilterRiskBtn.Pressed += () => ApplySquadCategoryFilter("RISK");
        _squadFilterBar.AddChild(_squadFilterRiskBtn);

        _squadList = new ItemList
        {
            Name = "SquadList",
            AutoHeight = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_squadList);
        _squadList.AllowReselect = true;
        _squadList.ItemSelected += OnSquadPlayerSelected;
        _squadList.ItemActivated += OnSquadPlayerActivated;
        playerManagementCard.AddChild(_squadList);

        var squadActionDirectRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        squadActionDirectRow.AddThemeConstantOverride("separation", 8);
        playerManagementCard.AddChild(squadActionDirectRow);

        _openSelectedPlayerDossierButton = PrimaryButton("🔍 Oyuncu Profili");
        _openSelectedPlayerDossierButton.TooltipText = "Seçili oyuncunun ayrıntılı profilini aç";
        _openSelectedPlayerDossierButton.Pressed += () =>
        {
            if (_selectedPlayerId is long id)
            {
                OpenPlayerDossier(id);
            }
        };
        squadActionDirectRow.AddChild(_openSelectedPlayerDossierButton);

        _playerDetailLabel = BodyLabel("PlayerDetailLabel", muted: true, autowrap: true);
        _playerDetailLabel.Text = "Bir kez dokunarak oyuncuyu seç; dosyayı çift dokunuşla veya profil düğmesiyle aç.";
        _playerDetailLabel.Visible = false;
        playerManagementCard.AddChild(_playerDetailLabel);

        var actionCard = AddCard(_clubAllPlayersPanel, "KADRO İŞLEMLERİ");
        var jobRow = ActionFlow();
        actionCard.AddChild(jobRow);
        _promoteOverflowButton = PrimaryButton("Taşanı Kadroya Al");
        _promoteOverflowButton.Pressed += () => Apply(_controller.PromoteOverflowPlayerToSquad());
        jobRow.AddChild(_promoteOverflowButton);
        _releaseCapacityButton = PrimaryButton("Yer Aç");
        _releaseCapacityButton.Pressed += () => Apply(_controller.ReleaseToFreeSquadCapacity());
        jobRow.AddChild(_releaseCapacityButton);
        _sellFringeButton = PrimaryButton("Satışa Çıkar");
        _sellFringeButton.Pressed += () => Apply(_controller.SellFringePlayerFromManagedClub());
        jobRow.AddChild(_sellFringeButton);
        _promiseStartButton = SecondaryButton("İlk 11 Sözü Ver");
        _promiseStartButton.Pressed += () =>
            ApplySelectedPlayer(_controller.PromiseStartingOpportunityToPlayer);
        jobRow.AddChild(_promiseStartButton);
        _promisePlayingTimeButton = SecondaryButton("Oyun Süresi Sözü");
        _promisePlayingTimeButton.Pressed += () =>
            ApplySelectedPlayer(_controller.PromisePlayingTimeToPlayer);
        jobRow.AddChild(_promisePlayingTimeButton);

        var academyCard = AddCard(_clubAllPlayersPanel, "ALTYAPI");
        _academyIntakeLabel = BodyLabel("AcademyIntakeLabel", autowrap: true);
        academyCard.AddChild(_academyIntakeLabel);
        _academyCandidateList = new ItemList
        {
            Name = "AcademyCandidateList",
            CustomMinimumSize = new Vector2(0, 180),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_academyCandidateList);
        _academyCandidateList.ItemSelected += OnAcademyCandidateSelected;
        academyCard.AddChild(_academyCandidateList);
        var academyRow = ActionFlow();
        academyCard.AddChild(academyRow);
        _academyAcceptButton = PrimaryButton("Akademide Tut");
        _academyAcceptButton.Pressed += () => ApplySelectedAcademyCandidate(accept: true);
        academyRow.AddChild(_academyAcceptButton);
        _academyRejectButton = SecondaryButton("Değerlendirmeyi Kapat");
        _academyRejectButton.Pressed += () => ApplySelectedAcademyCandidate(accept: false);
        academyRow.AddChild(_academyRejectButton);
        _academyLifecycleLabel = BodyLabel("AcademyLifecycleLabel", muted: true, autowrap: true);
        academyCard.AddChild(_academyLifecycleLabel);
        _academyLifecycleList = new ItemList
        {
            Name = "AcademyLifecycleList",
            CustomMinimumSize = new Vector2(0, 160),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_academyLifecycleList);
        _academyLifecycleList.ItemSelected += OnAcademyLifecyclePlayerSelected;
        academyCard.AddChild(_academyLifecycleList);
        _academyPromoteButton = SecondaryButton("A Takıma Çıkar");
        _academyPromoteButton.Pressed += PromoteSelectedAcademyPlayer;
        academyCard.AddChild(_academyPromoteButton);

        var teamDynamicsCard = AddCard(_clubAllPlayersPanel, "SOYUNMA ODASI");
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

        _clubPlanPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
        };
        _clubPlanPanel.AddThemeConstantOverride("separation", 10);
        page.AddChild(_clubPlanPanel);
        var planCard = AddCard(_clubPlanPanel, "İLK 11", emphasized: true);
        _clubPlanCaptionLabel = BodyLabel("ClubPlanCaption", autowrap: true);
        planCard.AddChild(_clubPlanCaptionLabel);
        _squadDepthGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _squadDepthGrid.AddThemeConstantOverride("h_separation", 10);
        _squadDepthGrid.AddThemeConstantOverride("v_separation", 10);
        planCard.AddChild(_squadDepthGrid);
        _clubPlanPitchHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 420),
        };
        planCard.AddChild(_clubPlanPitchHost);

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
