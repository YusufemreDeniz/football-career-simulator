using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private Control BuildWorldPage()
    {
        var page = PageRoot();

        // Kompetisyon sekme çubuğu
        var compSwitchRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        compSwitchRow.AddThemeConstantOverride("separation", CareerUiTheme.SpaceXs);
        page.AddChild(compSwitchRow);

        _leagueCompButton = new Button { Text = "🇹🇷 Trendyol Süper Lig", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _leagueCompButton.Pressed += () =>
        {
            _showingContinentalLeague = false;
            UpdateCompetitionTabButtons();
            RefreshStandings();
            RefreshFixtureList();
        };
        CareerUiTheme.StyleTabButton(_leagueCompButton, selected: true);
        compSwitchRow.AddChild(_leagueCompButton);

        _euroCompButton = new Button { Text = "⭐ Avrupa Şampiyonlar Kupası", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _euroCompButton.Pressed += () =>
        {
            _showingContinentalLeague = true;
            UpdateCompetitionTabButtons();
            RefreshStandings();
            RefreshFixtureList();
        };
        CareerUiTheme.StyleTabButton(_euroCompButton, selected: false);
        compSwitchRow.AddChild(_euroCompButton);

        var leagueTelemetryRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leagueTelemetryRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(leagueTelemetryRow);

        _leagueRoundBadgePanel = new PanelContainer();
        _leagueRoundBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.16f));
        _leagueRoundBadgeLabel = new Label { Text = "📅 Hafta: —" };
        CareerUiTheme.StyleEyebrow(_leagueRoundBadgeLabel, CareerUiTheme.Accent);
        _leagueRoundBadgePanel.AddChild(_leagueRoundBadgeLabel);
        leagueTelemetryRow.AddChild(_leagueRoundBadgePanel);

        _leagueMatchesBadgePanel = new PanelContainer();
        _leagueMatchesBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data, 0.16f));
        _leagueMatchesBadgeLabel = new Label { Text = "⚽ Maçlar: —" };
        CareerUiTheme.StyleEyebrow(_leagueMatchesBadgeLabel, CareerUiTheme.Data);
        _leagueMatchesBadgePanel.AddChild(_leagueMatchesBadgeLabel);
        leagueTelemetryRow.AddChild(_leagueMatchesBadgePanel);

        // Lig nabzı kartı
        var leagueNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leagueNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        page.AddChild(leagueNeonPanel);
        var leagueCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leagueCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        leagueNeonPanel.AddChild(leagueCard);
        leagueCard.AddChild(UiComponents.PageHeader("📊", "LİG"));
        leagueCard.AddChild(UiComponents.Divider(1));

        _leagueBriefingLabel = BodyLabel("LeagueBriefingLabel", autowrap: true);
        leagueCard.AddChild(_leagueBriefingLabel);

        // İstatistik kartı
        var statsNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        statsNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        page.AddChild(statsNeonPanel);
        var statisticsCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        statisticsCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        statsNeonPanel.AddChild(statisticsCard);
        statisticsCard.AddChild(UiComponents.PageHeader("🏆", "LİDERLER"));
        statisticsCard.AddChild(UiComponents.Divider(1));

        _leagueStatisticsLabel = BodyLabel("LeagueStatisticsLabel", autowrap: true);
        _leagueStatisticsLabel.Visible = false;
        statisticsCard.AddChild(_leagueStatisticsLabel);

        _statLeadersDesk = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _statLeadersDesk.AddThemeConstantOverride("separation", 10);
        statisticsCard.AddChild(_statLeadersDesk);

        // 1. En İyi Hücum
        var attackPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        attackPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatLeaderCardPanel(CareerUiTheme.Accent));
        var attackBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        attackBox.AddThemeConstantOverride("separation", 4);
        var attackHeader = new Label { Text = "⚡ EN İYİ HÜCUM" };
        CareerUiTheme.StyleEyebrow(attackHeader, CareerUiTheme.Accent);
        attackBox.AddChild(attackHeader);
        _attackLeaderClubLabel = new Label { Text = "—" };
        CareerUiTheme.StyleHeadline(_attackLeaderClubLabel);
        _attackLeaderClubLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        attackBox.AddChild(_attackLeaderClubLabel);
        _attackLeaderStatLabel = new Label { Text = "— Gol" };
        CareerUiTheme.StyleEyebrow(_attackLeaderStatLabel, CareerUiTheme.Accent);
        attackBox.AddChild(_attackLeaderStatLabel);
        attackPanel.AddChild(attackBox);
        _statLeadersDesk.AddChild(attackPanel);

        // 2. En Sıkı Savunma
        var defensePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        defensePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatLeaderCardPanel(CareerUiTheme.Data));
        var defenseBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        defenseBox.AddThemeConstantOverride("separation", 4);
        var defenseHeader = new Label { Text = "🛡️ EN SIKI SAVUNMA" };
        CareerUiTheme.StyleEyebrow(defenseHeader, CareerUiTheme.Data);
        defenseBox.AddChild(defenseHeader);
        _defenseLeaderClubLabel = new Label { Text = "—" };
        CareerUiTheme.StyleHeadline(_defenseLeaderClubLabel);
        _defenseLeaderClubLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        defenseBox.AddChild(_defenseLeaderClubLabel);
        _defenseLeaderStatLabel = new Label { Text = "— Gol Yenilen" };
        CareerUiTheme.StyleEyebrow(_defenseLeaderStatLabel, CareerUiTheme.Data);
        defenseBox.AddChild(_defenseLeaderStatLabel);
        defensePanel.AddChild(defenseBox);
        _statLeadersDesk.AddChild(defensePanel);

        // 3. Form Lideri
        var formPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        formPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatLeaderCardPanel(CareerUiTheme.ActionBright));
        var formBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        formBox.AddThemeConstantOverride("separation", 4);
        var formHeader = new Label { Text = "🔥 FORM LİDERİ" };
        CareerUiTheme.StyleEyebrow(formHeader, CareerUiTheme.ActionBright);
        formBox.AddChild(formHeader);
        _formLeaderClubLabel = new Label { Text = "—" };
        CareerUiTheme.StyleHeadline(_formLeaderClubLabel);
        _formLeaderClubLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        formBox.AddChild(_formLeaderClubLabel);
        _formLeaderStatLabel = new Label { Text = "—/15 Puan" };
        CareerUiTheme.StyleEyebrow(_formLeaderStatLabel, CareerUiTheme.ActionBright);
        formBox.AddChild(_formLeaderStatLabel);
        formPanel.AddChild(formBox);
        _statLeadersDesk.AddChild(formPanel);

        _managedLeagueStatisticsLabel = BodyLabel("ManagedLeagueStatisticsLabel", autowrap: true);
        CareerUiTheme.StyleEyebrow(_managedLeagueStatisticsLabel, CareerUiTheme.Ink);
        statisticsCard.AddChild(_managedLeagueStatisticsLabel);

        var tableCard = AddCard(page, "PUAN DURUMU");
        var standingsHint = BodyLabel("StandingsHintLabel", autowrap: true);
        standingsHint.Text = "Tablodan bir takıma tıklayarak form grafiği, iç/dış saha performansı, kadro gücü ve sezon karşılaşmalarını inceleyin.";
        standingsHint.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
        tableCard.AddChild(standingsHint);

        var zoneLegend = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        zoneLegend.AddThemeConstantOverride("separation", 8);
        tableCard.AddChild(zoneLegend);
        CreateStandingsLegendBadge(zoneLegend, "▌ Şampiyonlar Ligi", CareerUiTheme.Accent);
        CreateStandingsLegendBadge(zoneLegend, "▌ Avrupa Kupaları", CareerUiTheme.Data);
        CreateStandingsLegendBadge(zoneLegend, "▌ Küme Düşme", CareerUiTheme.DangerSoft);

        _standingsTable = new Tree
        {
            Name = "StandingsTable",
            Columns = 11,
            ColumnTitlesVisible = true,
            HideRoot = true,
            CustomMinimumSize = new Vector2(0, 500),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        ConfigureStandingsColumns(_standingsTable);
        CareerUiTheme.StyleTable(_standingsTable);
        _standingsTable.ItemSelected += OnStandingsClubSelected;
        tableCard.AddChild(_standingsTable);

        _compactStandingsList = new ItemList
        {
            Name = "CompactStandingsList",
            CustomMinimumSize = new Vector2(0, 300),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
            AllowReselect = true,
        };
        CareerUiTheme.StyleList(_compactStandingsList);
        _compactStandingsList.ItemSelected += OnCompactStandingsClubSelected;
        tableCard.AddChild(_compactStandingsList);

        _worldClubDossierCard = AddCard(page, "KULÜPLER", emphasized: true);

        var dossierHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        dossierHeaderRow.AddThemeConstantOverride("separation", 10);
        _worldClubDossierCard.AddChild(dossierHeaderRow);

        _worldClubHeadlineLabel = TitleLabel("WorldClubHeadlineLabel");
        _worldClubHeadlineLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        dossierHeaderRow.AddChild(_worldClubHeadlineLabel);

        _worldClubRatingBadgePanel = new PanelContainer();
        _worldClubRatingBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.RatingBadgePanel(75));
        _worldClubRatingBadgeLabel = new Label { Text = "⭐ 75" };
        CareerUiTheme.StyleEyebrow(_worldClubRatingBadgeLabel, CareerUiTheme.RatingColor(75));
        _worldClubRatingBadgePanel.AddChild(_worldClubRatingBadgeLabel);
        dossierHeaderRow.AddChild(_worldClubRatingBadgePanel);

        var dossierBadgeRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        dossierBadgeRow.AddThemeConstantOverride("separation", 8);
        _worldClubDossierCard.AddChild(dossierBadgeRow);

        _worldClubBadgeLabel = BodyLabel("WorldClubBadgeLabel");
        dossierBadgeRow.AddChild(_worldClubBadgeLabel);

        _worldClubThreatBadgePanel = new PanelContainer { Visible = false };
        _worldClubThreatBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning, 0.18f));
        _worldClubThreatBadgeLabel = new Label { Text = "⚠️ Tehdit" };
        CareerUiTheme.StyleEyebrow(_worldClubThreatBadgeLabel, CareerUiTheme.ColorWarning);
        _worldClubThreatBadgePanel.AddChild(_worldClubThreatBadgeLabel);
        dossierBadgeRow.AddChild(_worldClubThreatBadgePanel);

        var dossierColumns = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        dossierColumns.AddThemeConstantOverride("separation", 16);
        _worldClubDossierCard.AddChild(dossierColumns);

        var leftCard = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leftCard.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        var leftColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leftColumn.AddThemeConstantOverride("separation", 8);
        leftCard.AddChild(leftColumn);
        dossierColumns.AddChild(leftCard);

        var rightCard = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        rightCard.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        var rightColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        rightColumn.AddThemeConstantOverride("separation", 8);
        rightCard.AddChild(rightColumn);
        dossierColumns.AddChild(rightCard);

        _worldClubPerformanceLabel = BodyLabel("WorldClubPerformanceLabel", autowrap: true);
        leftColumn.AddChild(_worldClubPerformanceLabel);

        _worldClubThreatLabel = BodyLabel("WorldClubThreatLabel", autowrap: true);
        rightColumn.AddChild(_worldClubThreatLabel);

        _worldClubHeadToHeadLabel = BodyLabel("WorldClubHeadToHeadLabel", autowrap: true);
        _worldClubDossierCard.AddChild(_worldClubHeadToHeadLabel);

        var clubActionRow = ActionFlow();
        _worldClubDossierCard.AddChild(clubActionRow);

        _worldClubPrepButton = AccentButton("🎯 Bu Maça Hazırlan (Taktik & Analiz)");
        _worldClubPrepButton.Pressed += () => ShowPage(HubPage.Prep);
        clubActionRow.AddChild(_worldClubPrepButton);

        _worldClubTransferButton = SecondaryButton("🔍 Kulüp Transfer Adayları");
        _worldClubTransferButton.Pressed += () => ShowPage(HubPage.Transfer);
        clubActionRow.AddChild(_worldClubTransferButton);

        _worldClubFixtureButton = SecondaryButton("📅 Fikstürde Bu Takıma Git");
        _worldClubFixtureButton.Pressed += OnWorldClubFixtureJumpPressed;
        clubActionRow.AddChild(_worldClubFixtureButton);

        var fixtureCard = AddCard(page, "FİKSTÜR");
        var roundRow = ActionFlow();
        fixtureCard.AddChild(roundRow);

        _fixturePrevRoundButton = SecondaryButton("◀ Önceki Hafta");
        _fixturePrevRoundButton.Pressed += () =>
        {
            if (_roundSelector.Value > _roundSelector.MinValue)
            {
                _roundSelector.Value--;
            }
        };
        roundRow.AddChild(_fixturePrevRoundButton);

        var weekLabel = new Label { Text = "Hafta" };
        CareerUiTheme.StyleBody(weekLabel, muted: true);
        roundRow.AddChild(weekLabel);
        _roundSelector = new SpinBox
        {
            MinValue = 1,
            MaxValue = CompetitionMvpConstraints.MaxLeagueFixtureRound,
            Value = 1,
        };
        _roundSelector.ValueChanged += _ => RefreshFixtureList();
        roundRow.AddChild(_roundSelector);

        _fixtureNextRoundButton = SecondaryButton("Sonraki Hafta ▶");
        _fixtureNextRoundButton.Pressed += () =>
        {
            if (_roundSelector.Value < _roundSelector.MaxValue)
            {
                _roundSelector.Value++;
            }
        };
        roundRow.AddChild(_fixtureNextRoundButton);

        _fixtureList = new ItemList
        {
            Name = "FixtureList",
            CustomMinimumSize = new Vector2(0, 240),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_fixtureList);
        _fixtureList.ItemSelected += OnFixtureSelected;
        fixtureCard.AddChild(_fixtureList);

        _worldFixtureDetailCard = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        _worldFixtureDetailCard.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        fixtureCard.AddChild(_worldFixtureDetailCard);

        var fixtureDetailContent = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        fixtureDetailContent.AddThemeConstantOverride("separation", 6);
        _worldFixtureDetailCard.AddChild(fixtureDetailContent);

        _worldFixtureHeadlineLabel = TitleLabel("WorldFixtureHeadlineLabel");
        fixtureDetailContent.AddChild(_worldFixtureHeadlineLabel);

        _worldFixtureDetailLabel = BodyLabel("WorldFixtureDetailLabel", autowrap: true);
        fixtureDetailContent.AddChild(_worldFixtureDetailLabel);

        var fixtureActionRow = ActionFlow();
        fixtureDetailContent.AddChild(fixtureActionRow);

        _worldFixturePrepButton = AccentButton("🎯 Maç Hazırlığına Git");
        _worldFixturePrepButton.Pressed += () => ShowPage(HubPage.Prep);
        fixtureActionRow.AddChild(_worldFixturePrepButton);

        _worldFixtureHomeInspectButton = SecondaryButton("🏠 Ev Sahibini İncele");
        _worldFixtureHomeInspectButton.Pressed += () =>
        {
            if (_selectedFixtureHomeClubId.HasValue)
            {
                InspectClub(_selectedFixtureHomeClubId.Value);
            }
        };
        fixtureActionRow.AddChild(_worldFixtureHomeInspectButton);

        _worldFixtureAwayInspectButton = SecondaryButton("✈️ Deplasmanı İncele");
        _worldFixtureAwayInspectButton.Pressed += () =>
        {
            if (_selectedFixtureAwayClubId.HasValue)
            {
                InspectClub(_selectedFixtureAwayClubId.Value);
            }
        };
        fixtureActionRow.AddChild(_worldFixtureAwayInspectButton);

        var advancedToggle = SecondaryButton("Gelişmiş sezon araçları");
        advancedToggle.Visible = false; // Manuel lifecycle tetikleri oyuncu yüzeyine ait değil.
        fixtureCard.AddChild(advancedToggle);

        var advanced = new VBoxContainer { Visible = false };
        advanced.AddThemeConstantOverride("separation", 8);
        fixtureCard.AddChild(advanced);
        advancedToggle.Pressed += () =>
        {
            advanced.Visible = !advanced.Visible;
            advancedToggle.Text = advanced.Visible
                ? "Gelişmiş sezon araçlarını gizle"
                : "Gelişmiş sezon araçları";
        };

        var seasonRow = ActionFlow();
        advanced.AddChild(seasonRow);
        AddActionButton(seasonRow, "Ligi Kur / Tamamla", () => Apply(_controller.EnsureLeagueReady()));
        AddActionButton(seasonRow, "Sezonu Kapat", () => Apply(_controller.CompleteSeason()));
        AddActionButton(seasonRow, "Sezonu Arşivle", () => Apply(_controller.ArchiveSeason()));
        AddActionButton(seasonRow, "Yeni Sezon", () => Apply(_controller.StartNewSeason()));

        var planningRow = ActionFlow();
        advanced.AddChild(planningRow);
        AddActionButton(planningRow, "Planlama Dönemi Aç", () => Apply(_controller.OpenPlanningPeriod()));
        AddActionButton(planningRow, "Planlama Dönemini Bitir", () => Apply(_controller.CompletePlanningPeriod()));

        RegisterMobileFocusLayout(
            page,
            HubPage.World,
            new Control[] { leagueTelemetryRow, leagueNeonPanel, statsNeonPanel, standingsHint, zoneLegend },
            ("TABLO", new Control[] { tableCard.GetParent<Control>() }),
            ("FİKSTÜR", new Control[] { fixtureCard.GetParent<Control>() }),
            ("KULÜP", new Control[] { _worldClubDossierCard.GetParent<Control>() }));

        return page;
    }

}
