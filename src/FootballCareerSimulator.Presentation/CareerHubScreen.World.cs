using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private Button _worldTabFixturesBtn = null!;
    private Button _worldTabDossierBtn = null!;
    private Control _worldFixturesHost = null!;
    private Control _worldDossierHost = null!;

    private Control BuildWorldPage()
    {
        var page = PageRoot();
        page.SizeFlagsVertical = SizeFlags.ExpandFill;

        // ----------------------------------------------------
        // SINGLE COMPACT TOP BAR: COMPETITION + SUB-TABS (LEFT) + ROUND TELEMETRY (RIGHT)
        // ----------------------------------------------------
        var topControlRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topControlRow.AddThemeConstantOverride("separation", 6);
        page.AddChild(topControlRow);

        var compSwitchRow = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        compSwitchRow.AddThemeConstantOverride("separation", 4);
        topControlRow.AddChild(compSwitchRow);

        _leagueCompButton = new Button { Text = "Süper Lig" };
        _leagueCompButton.Pressed += () =>
        {
            _showingContinentalLeague = false;
            UpdateCompetitionTabButtons();
            RefreshStandings();
            RefreshFixtureList();
        };
        CareerUiTheme.StyleTabButton(_leagueCompButton, selected: true);
        compSwitchRow.AddChild(_leagueCompButton);

        _euroCompButton = new Button { Text = "Şampiyonlar Kupası" };
        _euroCompButton.Pressed += () =>
        {
            _showingContinentalLeague = true;
            UpdateCompetitionTabButtons();
            RefreshStandings();
            RefreshFixtureList();
        };
        CareerUiTheme.StyleTabButton(_euroCompButton, selected: false);
        compSwitchRow.AddChild(_euroCompButton);

        _worldTabFixturesBtn = new Button { Text = "FİKSTÜR & MAÇLAR" };
        _worldTabFixturesBtn.Pressed += () => ShowWorldSubTab(0);
        CareerUiTheme.StyleTabButton(_worldTabFixturesBtn, selected: true);
        compSwitchRow.AddChild(_worldTabFixturesBtn);

        _worldTabDossierBtn = new Button { Text = "LİDERLER & KULÜP DOSYASI" };
        _worldTabDossierBtn.Pressed += () => ShowWorldSubTab(1);
        CareerUiTheme.StyleTabButton(_worldTabDossierBtn, selected: false);
        compSwitchRow.AddChild(_worldTabDossierBtn);

        var topSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topControlRow.AddChild(topSpacer);

        var leagueTelemetryRow = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        leagueTelemetryRow.AddThemeConstantOverride("separation", 5);
        topControlRow.AddChild(leagueTelemetryRow);

        _leagueRoundBadgePanel = new PanelContainer();
        _leagueRoundBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.18f));
        _leagueRoundBadgeLabel = new Label { Text = "Hafta: —" };
        CareerUiTheme.StyleTableText(_leagueRoundBadgeLabel);
        _leagueRoundBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _leagueRoundBadgePanel.AddChild(_leagueRoundBadgeLabel);
        leagueTelemetryRow.AddChild(_leagueRoundBadgePanel);

        _leagueMatchesBadgePanel = new PanelContainer();
        _leagueMatchesBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data, 0.18f));
        _leagueMatchesBadgeLabel = new Label { Text = "Maçlar: —" };
        CareerUiTheme.StyleTableText(_leagueMatchesBadgeLabel);
        _leagueMatchesBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Data);
        _leagueMatchesBadgePanel.AddChild(_leagueMatchesBadgeLabel);
        leagueTelemetryRow.AddChild(_leagueMatchesBadgePanel);

        // ====================================================
        // WIDESCREEN 2-COLUMN SPLIT:
        // Left: Permanent Full Standings Table (18 Clubs)
        // Right: Round Fixtures & Match Preview (Tab 0) OR Stat Leaders & Club Dossier (Tab 1)
        // ====================================================
        var worldSplitRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        worldSplitRow.AddThemeConstantOverride("separation", 10);
        page.AddChild(worldSplitRow);

        // ── LEFT COLUMN: PERMANENT LEAGUE STANDINGS TABLE ──────────────────────
        var tableCard = AddCard(worldSplitRow, "PUAN DURUMU", emphasized: true, expandVertical: true);
        if (tableCard.GetParent() is Control tablePanel)
        {
            tablePanel.SizeFlagsStretchRatio = 1.45f;
        }

        var zoneLegend = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        zoneLegend.AddThemeConstantOverride("separation", 6);
        tableCard.AddChild(zoneLegend);
        CreateStandingsLegendBadge(zoneLegend, "▌ Şampiyonlar Ligi", CareerUiTheme.Accent);
        CreateStandingsLegendBadge(zoneLegend, "▌ Avrupa Kupaları", CareerUiTheme.Data);
        CreateStandingsLegendBadge(zoneLegend, "▌ Küme Düşme", CareerUiTheme.DangerSoft);

        var standingsHint = BodyLabel("StandingsHintLabel", muted: true);
        standingsHint.Text = "Takım analizi için satıra tıkla";
        standingsHint.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        standingsHint.HorizontalAlignment = HorizontalAlignment.Right;
        zoneLegend.AddChild(standingsHint);

        _standingsTable = new Tree
        {
            Name = "StandingsTable",
            Columns = 11,
            ColumnTitlesVisible = true,
            HideRoot = true,
            CustomMinimumSize = new Vector2(0, 270),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        ConfigureStandingsColumns(_standingsTable);
        CareerUiTheme.StyleTable(_standingsTable);
        _standingsTable.ItemSelected += () =>
        {
            OnStandingsClubSelected();
            ShowWorldSubTab(1);
        };
        tableCard.AddChild(_standingsTable);

        _compactStandingsList = new ItemList
        {
            Name = "CompactStandingsList",
            CustomMinimumSize = new Vector2(0, 240),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
            AllowReselect = true,
        };
        CareerUiTheme.StyleList(_compactStandingsList);
        _compactStandingsList.ItemSelected += index =>
        {
            OnCompactStandingsClubSelected(index);
            ShowWorldSubTab(1);
        };
        tableCard.AddChild(_compactStandingsList);

        // ── RIGHT COLUMN: FIXTURES (TAB 0) & STATS/CLUB DOSSIER (TAB 1) ────────
        var rightCol = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.1f,
        };
        rightCol.AddThemeConstantOverride("separation", 8);
        worldSplitRow.AddChild(rightCol);

        // TAB 0: HAFTALIK FİKSTÜR & MAÇ ÖNİZLEME
        var fixtureCard = AddCard(rightCol, "HAFTALIK FİKSTÜR", emphasized: false, expandVertical: true);
        _worldFixturesHost = fixtureCard.GetParent<Control>();

        var roundRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        roundRow.AddThemeConstantOverride("separation", 6);
        fixtureCard.AddChild(roundRow);

        _fixturePrevRoundButton = SecondaryButton("Önceki");
        _fixturePrevRoundButton.Pressed += () =>
        {
            if (_roundSelector.Value > _roundSelector.MinValue)
            {
                _roundSelector.Value--;
            }
        };
        roundRow.AddChild(_fixturePrevRoundButton);

        var weekLabel = new Label { Text = "Hafta:" };
        CareerUiTheme.StyleBody(weekLabel, muted: true);
        weekLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        roundRow.AddChild(weekLabel);

        _roundSelector = new SpinBox
        {
            MinValue = 1,
            MaxValue = CompetitionMvpConstraints.MaxLeagueFixtureRound,
            Value = 1,
            CustomMinimumSize = new Vector2(90, 30),
        };
        _roundSelector.ValueChanged += _ => RefreshFixtureList();
        roundRow.AddChild(_roundSelector);

        _fixtureNextRoundButton = SecondaryButton("Sonraki");
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
            CustomMinimumSize = new Vector2(0, 175),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
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
        fixtureDetailContent.AddThemeConstantOverride("separation", 4);
        _worldFixtureDetailCard.AddChild(fixtureDetailContent);

        _worldFixtureHeadlineLabel = TitleLabel("WorldFixtureHeadlineLabel");
        _worldFixtureHeadlineLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(14));
        fixtureDetailContent.AddChild(_worldFixtureHeadlineLabel);

        _worldFixtureDetailLabel = BodyLabel("WorldFixtureDetailLabel", autowrap: true);
        fixtureDetailContent.AddChild(_worldFixtureDetailLabel);

        var fixtureActionRow = ActionFlow();
        fixtureDetailContent.AddChild(fixtureActionRow);

        _worldFixturePrepButton = AccentButton("Maç Hazırlığına Git");
        _worldFixturePrepButton.Pressed += () => ShowPage(HubPage.Prep);
        fixtureActionRow.AddChild(_worldFixturePrepButton);

        _worldFixtureHomeInspectButton = SecondaryButton("Ev Sahibi");
        _worldFixtureHomeInspectButton.Pressed += () =>
        {
            if (_selectedFixtureHomeClubId.HasValue)
            {
                InspectClub(_selectedFixtureHomeClubId.Value);
                ShowWorldSubTab(1);
            }
        };
        fixtureActionRow.AddChild(_worldFixtureHomeInspectButton);

        _worldFixtureAwayInspectButton = SecondaryButton("Deplasman");
        _worldFixtureAwayInspectButton.Pressed += () =>
        {
            if (_selectedFixtureAwayClubId.HasValue)
            {
                InspectClub(_selectedFixtureAwayClubId.Value);
                ShowWorldSubTab(1);
            }
        };
        fixtureActionRow.AddChild(_worldFixtureAwayInspectButton);

        // Hidden advanced tools kept for compatibility
        var advanced = new VBoxContainer { Visible = false };
        fixtureCard.AddChild(advanced);
        var seasonRow = ActionFlow();
        advanced.AddChild(seasonRow);
        AddActionButton(seasonRow, "Ligi Kur / Tamamla", () => Apply(_controller.EnsureLeagueReady()));
        AddActionButton(seasonRow, "Sezonu Kapat", () => Apply(_controller.CompleteSeason()));
        AddActionButton(seasonRow, "Sezonu Arşivle", () => Apply(_controller.ArchiveSeason()));
        AddActionButton(seasonRow, "Yeni Sezon", () => Apply(_controller.StartNewSeason()));

        // TAB 1: LİG LİDERLERİ & SEÇİLİ KULÜP İSTİHBARAT DOSYASI
        var dossierHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
        };
        dossierHost.AddThemeConstantOverride("separation", 8);
        rightCol.AddChild(dossierHost);
        _worldDossierHost = dossierHost;

        var statisticsCard = AddCard(dossierHost, "LİG LİDERLERİ & SEZON ÖZETİ");
        _leagueBriefingLabel = BodyLabel("LeagueBriefingLabel", muted: true, autowrap: true);
        statisticsCard.AddChild(_leagueBriefingLabel);

        _leagueStatisticsLabel = BodyLabel("LeagueStatisticsLabel", autowrap: true);
        _leagueStatisticsLabel.Visible = false;
        statisticsCard.AddChild(_leagueStatisticsLabel);

        _statLeadersDesk = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _statLeadersDesk.AddThemeConstantOverride("separation", 6);
        statisticsCard.AddChild(_statLeadersDesk);

        var attackPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        attackPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatLeaderCardPanel(CareerUiTheme.Accent));
        var attackBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        attackBox.AddThemeConstantOverride("separation", 2);
        var attackHeader = new Label { Text = "EN İYİ HÜCUM" };
        CareerUiTheme.StyleEyebrow(attackHeader, CareerUiTheme.Accent);
        attackBox.AddChild(attackHeader);
        _attackLeaderClubLabel = new Label { Text = "—" };
        CareerUiTheme.StyleHeadline(_attackLeaderClubLabel);
        _attackLeaderClubLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        attackBox.AddChild(_attackLeaderClubLabel);
        _attackLeaderStatLabel = new Label { Text = "— Gol" };
        CareerUiTheme.StyleEyebrow(_attackLeaderStatLabel, CareerUiTheme.Accent);
        attackBox.AddChild(_attackLeaderStatLabel);
        attackPanel.AddChild(attackBox);
        _statLeadersDesk.AddChild(attackPanel);

        var defensePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        defensePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatLeaderCardPanel(CareerUiTheme.Data));
        var defenseBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        defenseBox.AddThemeConstantOverride("separation", 2);
        var defenseHeader = new Label { Text = "EN SIKI SAVUNMA" };
        CareerUiTheme.StyleEyebrow(defenseHeader, CareerUiTheme.Data);
        defenseBox.AddChild(defenseHeader);
        _defenseLeaderClubLabel = new Label { Text = "—" };
        CareerUiTheme.StyleHeadline(_defenseLeaderClubLabel);
        _defenseLeaderClubLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        defenseBox.AddChild(_defenseLeaderClubLabel);
        _defenseLeaderStatLabel = new Label { Text = "— Gol Yenilen" };
        CareerUiTheme.StyleEyebrow(_defenseLeaderStatLabel, CareerUiTheme.Data);
        defenseBox.AddChild(_defenseLeaderStatLabel);
        defensePanel.AddChild(defenseBox);
        _statLeadersDesk.AddChild(defensePanel);

        var formPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        formPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatLeaderCardPanel(CareerUiTheme.ActionBright));
        var formBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        formBox.AddThemeConstantOverride("separation", 2);
        var formHeader = new Label { Text = "FORM LİDERİ" };
        CareerUiTheme.StyleEyebrow(formHeader, CareerUiTheme.ActionBright);
        formBox.AddChild(formHeader);
        _formLeaderClubLabel = new Label { Text = "—" };
        CareerUiTheme.StyleHeadline(_formLeaderClubLabel);
        _formLeaderClubLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        formBox.AddChild(_formLeaderClubLabel);
        _formLeaderStatLabel = new Label { Text = "—/15 Puan" };
        CareerUiTheme.StyleEyebrow(_formLeaderStatLabel, CareerUiTheme.ActionBright);
        formBox.AddChild(_formLeaderStatLabel);
        formPanel.AddChild(formBox);
        _statLeadersDesk.AddChild(formPanel);

        _managedLeagueStatisticsLabel = BodyLabel("ManagedLeagueStatisticsLabel", autowrap: true);
        CareerUiTheme.StyleEyebrow(_managedLeagueStatisticsLabel, CareerUiTheme.Ink);
        statisticsCard.AddChild(_managedLeagueStatisticsLabel);

        _worldClubDossierCard = AddCard(dossierHost, "SEÇİLİ KULÜP İSTİHBARAT RAPORU", emphasized: true, expandVertical: true);

        var dossierHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        dossierHeaderRow.AddThemeConstantOverride("separation", 8);
        _worldClubDossierCard.AddChild(dossierHeaderRow);

        _worldClubHeadlineLabel = TitleLabel("WorldClubHeadlineLabel");
        _worldClubHeadlineLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _worldClubHeadlineLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(16));
        dossierHeaderRow.AddChild(_worldClubHeadlineLabel);

        _worldClubRatingBadgePanel = new PanelContainer();
        _worldClubRatingBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.RatingBadgePanel(75));
        _worldClubRatingBadgeLabel = new Label { Text = "GÜÇ 75" };
        CareerUiTheme.StyleEyebrow(_worldClubRatingBadgeLabel, CareerUiTheme.RatingColor(75));
        _worldClubRatingBadgePanel.AddChild(_worldClubRatingBadgeLabel);
        dossierHeaderRow.AddChild(_worldClubRatingBadgePanel);

        var dossierBadgeRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        dossierBadgeRow.AddThemeConstantOverride("separation", 6);
        _worldClubDossierCard.AddChild(dossierBadgeRow);

        _worldClubBadgeLabel = BodyLabel("WorldClubBadgeLabel");
        dossierBadgeRow.AddChild(_worldClubBadgeLabel);

        _worldClubThreatBadgePanel = new PanelContainer { Visible = false };
        _worldClubThreatBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning, 0.18f));
        _worldClubThreatBadgeLabel = new Label { Text = "Tehdit" };
        CareerUiTheme.StyleEyebrow(_worldClubThreatBadgeLabel, CareerUiTheme.ColorWarning);
        _worldClubThreatBadgePanel.AddChild(_worldClubThreatBadgeLabel);
        dossierBadgeRow.AddChild(_worldClubThreatBadgePanel);

        var dossierColumns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        dossierColumns.AddThemeConstantOverride("separation", 8);
        _worldClubDossierCard.AddChild(dossierColumns);

        var leftCard = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leftCard.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        var leftColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leftColumn.AddThemeConstantOverride("separation", 4);
        leftCard.AddChild(leftColumn);
        dossierColumns.AddChild(leftCard);

        var rightCard = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        rightCard.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        var rightColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        rightColumn.AddThemeConstantOverride("separation", 4);
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

        _worldClubPrepButton = AccentButton("Maç Hazırlığı");
        _worldClubPrepButton.Pressed += () => ShowPage(HubPage.Prep);
        clubActionRow.AddChild(_worldClubPrepButton);

        _worldClubTransferButton = SecondaryButton("Transfer Adayları");
        _worldClubTransferButton.Pressed += () => ShowPage(HubPage.Transfer);
        clubActionRow.AddChild(_worldClubTransferButton);

        _worldClubFixtureButton = SecondaryButton("Fikstürde Bul");
        _worldClubFixtureButton.Pressed += () =>
        {
            OnWorldClubFixtureJumpPressed();
            ShowWorldSubTab(0);
        };
        clubActionRow.AddChild(_worldClubFixtureButton);

        ShowWorldSubTab(0);
        return page;
    }

    private void ShowWorldSubTab(int tabIndex)
    {
        if (_worldFixturesHost is not null) _worldFixturesHost.Visible = tabIndex == 0;
        if (_worldDossierHost is not null) _worldDossierHost.Visible = tabIndex == 1;

        if (_worldTabFixturesBtn is not null) CareerUiTheme.StyleTabButton(_worldTabFixturesBtn, tabIndex == 0);
        if (_worldTabDossierBtn is not null) CareerUiTheme.StyleTabButton(_worldTabDossierBtn, tabIndex == 1);
    }
}
