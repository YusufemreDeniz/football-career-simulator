using FootballCareerSimulator.Domain.Competition;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen : Control
{
    private readonly CareerSessionController _controller;

    private Label _dateLabel = null!;
    private Label _managerLabel = null!;
    private Label _seasonLabel = null!;
    private Label _progressLabel = null!;
    private Label _blockerLabel = null!;
    private Label _selectionLabel = null!;
    private Label _standingsLabel = null!;
    private Label _statusLabel = null!;
    private SpinBox _roundSelector = null!;
    private ItemList _fixtureList = null!;
    private ItemList _squadList = null!;
    private Button _approveSelectionButton = null!;
    private Button _playButton = null!;
    private Button _advanceDayButton = null!;
    private Button _advanceWeekButton = null!;

    public event Action? BackToMenuRequested;

    public event Action<PlayMatchesUiResult>? MatchResultsReady;

    public CareerHubScreen(CareerSessionController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public override void _Ready()
    {
        BuildLayout();
        RefreshUi();
    }

    public void SetStatus(string message) => _statusLabel.Text = message;

    private void BuildLayout()
    {
        var margin = new MarginContainer();
        margin.AnchorRight = 1f;
        margin.AnchorBottom = 1f;
        margin.GrowHorizontal = GrowDirection.Both;
        margin.GrowVertical = GrowDirection.Both;
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        margin.AddChild(scroll);

        var layout = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        layout.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(layout);

        layout.AddChild(new Label { Text = "Kariyer Merkezi" });

        _dateLabel = new Label { Name = "DateLabel" };
        layout.AddChild(_dateLabel);

        _managerLabel = new Label { Name = "ManagerLabel" };
        layout.AddChild(_managerLabel);

        _seasonLabel = new Label { Name = "SeasonLabel" };
        layout.AddChild(_seasonLabel);

        _progressLabel = new Label { Name = "ProgressLabel" };
        layout.AddChild(_progressLabel);

        _blockerLabel = new Label
        {
            Name = "BlockerLabel",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        layout.AddChild(_blockerLabel);

        _selectionLabel = new Label
        {
            Name = "SelectionLabel",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        layout.AddChild(_selectionLabel);

        _standingsLabel = new Label
        {
            Name = "StandingsLabel",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        layout.AddChild(_standingsLabel);

        layout.AddChild(new Label { Text = "Birincil eylemler" });

        var primaryRow = new HBoxContainer();
        primaryRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(primaryRow);

        _approveSelectionButton = new Button { Text = "Kadro Onayla" };
        _approveSelectionButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        primaryRow.AddChild(_approveSelectionButton);

        _playButton = new Button { Text = "Bugünün Maçlarını Oyna" };
        _playButton.Pressed += OnPlayMatches;
        primaryRow.AddChild(_playButton);

        _advanceDayButton = new Button { Text = "1 Gün İlerlet" };
        _advanceDayButton.Pressed += () => Apply(_controller.AdvanceDays(1));
        primaryRow.AddChild(_advanceDayButton);

        _advanceWeekButton = new Button { Text = "7 Gün İlerlet" };
        _advanceWeekButton.Pressed += () => Apply(_controller.AdvanceDays(7));
        primaryRow.AddChild(_advanceWeekButton);

        var saveLoadRow = new HBoxContainer();
        saveLoadRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(saveLoadRow);

        var saveButton = new Button { Text = "Kaydet" };
        saveButton.Pressed += () => Apply(_controller.SaveGame());
        saveLoadRow.AddChild(saveButton);

        var loadButton = new Button { Text = "Yükle" };
        loadButton.Pressed += () => Apply(_controller.LoadGame());
        saveLoadRow.AddChild(loadButton);

        var menuButton = new Button { Text = "Ana Menü" };
        menuButton.Pressed += () => BackToMenuRequested?.Invoke();
        saveLoadRow.AddChild(menuButton);

        layout.AddChild(new Label { Text = "Haftalık fikstür" });

        var roundRow = new HBoxContainer();
        roundRow.AddChild(new Label { Text = "Hafta:" });
        _roundSelector = new SpinBox
        {
            MinValue = 1,
            MaxValue = CompetitionMvpConstraints.MaxLeagueFixtureRound,
            Value = 1,
        };
        _roundSelector.ValueChanged += _ => RefreshFixtureList();
        roundRow.AddChild(_roundSelector);
        layout.AddChild(roundRow);

        _fixtureList = new ItemList
        {
            Name = "FixtureList",
            CustomMinimumSize = new Vector2(0, 140),
        };
        layout.AddChild(_fixtureList);

        layout.AddChild(new Label { Text = "Kadro (özet)" });
        _squadList = new ItemList
        {
            Name = "SquadList",
            CustomMinimumSize = new Vector2(0, 100),
        };
        layout.AddChild(_squadList);

        layout.AddChild(new Label { Text = "Sezon yönetimi" });

        var seasonRow = new HBoxContainer();
        seasonRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(seasonRow);

        AddActionButton(seasonRow, "Ligi Kur / Tamamla", () => Apply(_controller.EnsureLeagueReady()));
        AddActionButton(seasonRow, "Sezonu Kapat", () => Apply(_controller.CompleteSeason()));
        AddActionButton(seasonRow, "Sezonu Arşivle", () => Apply(_controller.ArchiveSeason()));
        AddActionButton(seasonRow, "Yeni Sezon", () => Apply(_controller.StartNewSeason()));

        var planningRow = new HBoxContainer();
        planningRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(planningRow);
        AddActionButton(planningRow, "Planlama Dönemi Aç", () => Apply(_controller.OpenPlanningPeriod()));
        AddActionButton(planningRow, "Planlama Dönemini Bitir", () => Apply(_controller.CompletePlanningPeriod()));

        _statusLabel = new Label
        {
            Name = "StatusLabel",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        layout.AddChild(_statusLabel);
    }

    private static void AddActionButton(HBoxContainer row, string text, Action action)
    {
        var button = new Button { Text = text };
        button.Pressed += action;
        row.AddChild(button);
    }

    private void OnPlayMatches()
    {
        var results = _controller.PlayDueMatches();
        RefreshUi();

        if (results.Succeeded && results.MatchLines.Count > 0)
        {
            MatchResultsReady?.Invoke(results);
            return;
        }

        _statusLabel.Text = results.Message;
    }

    private void Apply(UiActionResult result)
    {
        _statusLabel.Text = result.Message;
        RefreshUi();
    }

    private void RefreshUi()
    {
        var host = _controller.Host;
        var world = host.WorldModule;
        var competition = host.CompetitionModule;
        var current = world.Queries.GetCurrentGameDate();
        var season = competition.Queries.GetCurrentSeason();
        var manager = host.ManagerModule.Queries.GetCareer();
        var period = world.Queries.GetCurrentPlanningPeriod();

        _dateLabel.Text = $"Tarih: {current.IsoDate} (gün {current.DayNumber})";

        var clubName = manager.EmployedClubId is long clubId
            ? _controller.GetClubDisplayName(clubId)
            : "—";
        _managerLabel.Text = $"Menajer: {manager.DisplayName} · Kulüp: {clubName}";

        var periodText = period is null
            ? "Planlama dönemi: yok"
            : $"Planlama dönemi: #{period.PlanningPeriodId} ({period.Status})";

        if (season is null)
        {
            _seasonLabel.Text = "Lig sezonu: yok — 'Ligi Kur' ile başla.";
            _progressLabel.Text = periodText;
            _standingsLabel.Text = "Puan durumu: —";
            _fixtureList.Clear();
            _blockerLabel.Text = _controller.FormatActiveBlockerSummary();
            RefreshSelectionStatus();
            RefreshSquadList();
            UpdatePrimaryHints(dueMatchCount: 0, canAdvance: world.Queries.GetTimeAdvanceEligibility().CanAdvance);
            return;
        }

        _seasonLabel.Text =
            $"Sezon #{season.SeasonId} ({season.Status}) — {season.ParticipantCount} takım, {season.FixtureCount} maç";

        var progress = competition.Queries.GetSeasonProgress(season.SeasonId);
        var progressText = progress is null
            ? "İlerleme: —"
            : $"İlerleme: {progress.AcceptedFixtureCount}/{progress.TotalFixtureCount} maç"
              + (progress.CanComplete ? " · kapatılabilir" : string.Empty)
              + (progress.CanArchive ? " · arşivlenebilir" : string.Empty);
        _progressLabel.Text = $"{progressText} · {periodText}";

        _blockerLabel.Text = _controller.FormatActiveBlockerSummary();
        RefreshSelectionStatus();
        RefreshStandings();
        RefreshFixtureList();
        RefreshSquadList();

        var dueCount = competition.Queries
            .GetSeasonFixtures(season.SeasonId)
            .Count(fixture =>
                fixture.ScheduledDayNumber <= current.DayNumber
                && string.Equals(fixture.Status, nameof(FixtureStatus.Planned), StringComparison.Ordinal));

        UpdatePrimaryHints(dueCount, world.Queries.GetTimeAdvanceEligibility().CanAdvance);
    }

    private void RefreshSelectionStatus()
    {
        var currentDay = _controller.Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);

        if (pending is null)
        {
            _selectionLabel.Text = "Kadro onayı: vadesi gelmiş kendi maçın yok.";
            _approveSelectionButton.Disabled = true;
            return;
        }

        var opponent = _controller.GetClubDisplayName(pending.OpponentClubId);
        var venue = pending.IsHome ? "Ev" : "Dep";
        _selectionLabel.Text = pending.IsApproved
            ? $"Kadro onayı: hazır · fikstür #{pending.FixtureId} ({venue} vs {opponent})"
            : $"Kadro onayı: gerekli · fikstür #{pending.FixtureId} ({venue} vs {opponent}, {pending.ScheduledIsoDate})";
        _approveSelectionButton.Disabled = pending.IsApproved;
    }

    private void UpdatePrimaryHints(int dueMatchCount, bool canAdvance)
    {
        var currentDay = _controller.Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        var selectionBlocksPlay = pending is not null && !pending.IsApproved;

        _playButton.Disabled = dueMatchCount == 0 || selectionBlocksPlay;
        _playButton.Text = dueMatchCount == 0
            ? "Bugünün Maçlarını Oyna"
            : selectionBlocksPlay
                ? "Bugünün Maçlarını Oyna (önce kadro)"
                : $"Bugünün Maçlarını Oyna ({dueMatchCount})";

        _advanceDayButton.Disabled = !canAdvance;
        _advanceWeekButton.Disabled = !canAdvance;
    }

    private void RefreshSquadList()
    {
        _squadList.Clear();
        var manager = _controller.Host.ManagerModule.Queries.GetCareer();
        if (manager.EmployedClubId is not long clubId)
        {
            return;
        }

        var rootSeed = _controller.Host.WorldModule.TimelineStore.Timeline.RootSeed;
        var squad = _controller.Host.TeamPreparationModule.SquadQueries.GetClubSquad(clubId, rootSeed);
        var clubName = _controller.GetClubDisplayName(clubId);

        foreach (var player in squad.Take(11))
        {
            _squadList.AddItem($"{player.SquadNumber}. {player.DisplayName} ({player.Rating})");
        }

        if (squad.Count > 11)
        {
            _squadList.AddItem($"... +{squad.Count - 11} yedek/oyuncu ({clubName})");
        }
    }

    private void RefreshFixtureList()
    {
        _fixtureList.Clear();
        var season = _controller.Host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null || season.FixtureCount == 0)
        {
            return;
        }

        var round = (int)_roundSelector.Value;
        var fixtures = _controller.Host.CompetitionModule.Queries.GetFixturesByRound(season.SeasonId, round);

        foreach (var fixture in fixtures)
        {
            var home = _controller.GetClubDisplayName(fixture.HomeClubId);
            var away = _controller.GetClubDisplayName(fixture.AwayClubId);
            var score = fixture.HomeGoals is int homeGoals && fixture.AwayGoals is int awayGoals
                ? $" {homeGoals}-{awayGoals}"
                : string.Empty;
            _fixtureList.AddItem(
                $"{home} vs {away}{score} · {fixture.ScheduledIsoDate} · {fixture.Status}");
        }
    }

    private void RefreshStandings()
    {
        var season = _controller.Host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null || season.FixtureCount == 0)
        {
            _standingsLabel.Text = "Puan durumu: —";
            return;
        }

        var standings = _controller.Host.CompetitionModule.Queries.GetStandings(season.SeasonId);
        if (standings.Count == 0)
        {
            _standingsLabel.Text = "Puan durumu: henüz maç yok";
            return;
        }

        var preview = string.Join(
            " | ",
            standings.Take(5).Select((entry, index) =>
                $"{index + 1}. {_controller.GetClubDisplayName(entry.ClubId)} {entry.Points}p ({entry.Played}M)"));

        _standingsLabel.Text = $"Puan durumu (ilk 5): {preview}";
    }
}
