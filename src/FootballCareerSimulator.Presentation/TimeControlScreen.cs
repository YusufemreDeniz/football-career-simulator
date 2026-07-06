using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Production Kart 6 + Competition Kart C6: Application command/query contract'ları üzerinden
/// zaman kontrolü, planlama dönemi ve lig sezonu/fikstür görüntüleme.
/// </summary>
public partial class TimeControlScreen : Control
{
    private CareerPresentationHost _host = null!;
    private long _nextPlanningPeriodId = 1;
    private const long DefaultSeasonId = 1;
    private Label _dateLabel = null!;
    private Label _periodLabel = null!;
    private Label _blockerLabel = null!;
    private Label _seasonLabel = null!;
    private Label _seasonProgressLabel = null!;
    private Label _managerLabel = null!;
    private Label _standingsLabel = null!;
    private SpinBox _roundSelector = null!;
    private ItemList _fixtureList = null!;
    private ItemList _squadList = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        _host = CareerPresentationHost.CreateDefault();
        BuildLayout();
        RefreshUi();

        GD.Print("[TimeControlScreen] Hazır.");

        RunSelfCheck();
    }

    private void BuildLayout()
    {
        AnchorRight = 1f;
        AnchorBottom = 1f;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        var layout = new VBoxContainer();
        layout.AnchorRight = 1f;
        layout.AnchorBottom = 1f;
        layout.GrowHorizontal = GrowDirection.Both;
        layout.GrowVertical = GrowDirection.Both;
        AddChild(layout);

        _dateLabel = new Label { Name = "DateLabel" };
        layout.AddChild(_dateLabel);

        _periodLabel = new Label { Name = "PeriodLabel" };
        layout.AddChild(_periodLabel);

        _blockerLabel = new Label { Name = "BlockerLabel" };
        layout.AddChild(_blockerLabel);

        _seasonLabel = new Label { Name = "SeasonLabel" };
        layout.AddChild(_seasonLabel);

        _seasonProgressLabel = new Label { Name = "SeasonProgressLabel" };
        layout.AddChild(_seasonProgressLabel);

        _managerLabel = new Label { Name = "ManagerLabel" };
        layout.AddChild(_managerLabel);

        _standingsLabel = new Label { Name = "StandingsLabel" };
        layout.AddChild(_standingsLabel);

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

        _squadList = new ItemList
        {
            Name = "SquadList",
            CustomMinimumSize = new Vector2(0, 100),
        };
        layout.AddChild(_squadList);

        var playTodayButton = new Button { Text = "Bugünün Maçlarını Oyna" };
        playTodayButton.Pressed += PlayTodayMatches;
        layout.AddChild(playTodayButton);

        var advanceDayButton = new Button { Text = "1 Gün İlerlet" };
        advanceDayButton.Pressed += () => AdvanceDays(1);
        layout.AddChild(advanceDayButton);

        var advanceWeekButton = new Button { Text = "7 Gün İlerlet" };
        advanceWeekButton.Pressed += () => AdvanceDays(7);
        layout.AddChild(advanceWeekButton);

        var setupLeagueButton = new Button { Text = "Lig Sezonu Kur (20 takım + fikstür)" };
        setupLeagueButton.Pressed += SetupLeagueSeason;
        layout.AddChild(setupLeagueButton);

        var completeSeasonButton = new Button { Text = "Sezonu Kapat" };
        completeSeasonButton.Pressed += CompleteSeason;
        layout.AddChild(completeSeasonButton);

        var archiveSeasonButton = new Button { Text = "Sezonu Arşivle" };
        archiveSeasonButton.Pressed += ArchiveSeason;
        layout.AddChild(archiveSeasonButton);

        var newSeasonButton = new Button { Text = "Yeni Sezon Başlat" };
        newSeasonButton.Pressed += StartNewSeason;
        layout.AddChild(newSeasonButton);

        var saveButton = new Button { Text = "Kaydet" };
        saveButton.Pressed += SaveGame;
        layout.AddChild(saveButton);

        var loadButton = new Button { Text = "Yükle" };
        loadButton.Pressed += LoadGame;
        layout.AddChild(loadButton);

        var openPeriodButton = new Button { Text = "Planlama Dönemi Aç" };
        openPeriodButton.Pressed += OpenPlanningPeriod;
        layout.AddChild(openPeriodButton);

        var completePeriodButton = new Button { Text = "Planlama Dönemini Tamamla" };
        completePeriodButton.Pressed += CompletePlanningPeriod;
        layout.AddChild(completePeriodButton);

        _statusLabel = new Label { Name = "StatusLabel" };
        layout.AddChild(_statusLabel);
    }

    private void AdvanceDays(int dayCount)
    {
        var world = _host.WorldModule;
        var current = world.Queries.GetCurrentGameDate();
        var result = world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), current.DayNumber + dayCount));

        if (result.WasBlocked)
        {
            var blocker = result.Blockers[0];
            _statusLabel.Text =
                $"İlerleme engellendi: {blocker.SourceContext} / {blocker.DescriptionCode}";
        }
        else
        {
            _statusLabel.Text =
                $"İlerleme başarılı: {result.PreviousDayNumber} -> {result.NewDayNumber}";
        }

        RefreshUi();
    }

    private void SetupLeagueSeason()
    {
        try
        {
            var competition = _host.CompetitionModule;
            var world = _host.WorldModule;
            var currentDay = world.Queries.GetCurrentGameDate().DayNumber;
            var season = competition.Queries.GetCurrentSeason();

            if (season is null)
            {
                competition.CreateSeason.Handle(
                    new CreateSeasonCommand(Guid.NewGuid(), DefaultSeasonId, currentDay));
                season = competition.Queries.GetCurrentSeason();
            }

            if (season is null)
            {
                throw new InvalidOperationException("Sezon oluşturulamadı.");
            }

            if (season.ParticipantCount < CompetitionMvpConstraints.LeagueTeamCount)
            {
                for (var clubId = season.ParticipantCount + 1L;
                     clubId <= CompetitionMvpConstraints.LeagueTeamCount;
                     clubId++)
                {
                    competition.RegisterSeasonParticipant.Handle(
                        new RegisterSeasonParticipantCommand(Guid.NewGuid(), DefaultSeasonId, clubId));
                }

                season = competition.Queries.GetCurrentSeason()!;
            }

            if (string.Equals(season.Status, nameof(SeasonStatus.Preseason), StringComparison.Ordinal))
            {
                competition.StartSeason.Handle(
                    new StartSeasonCommand(Guid.NewGuid(), DefaultSeasonId, currentDay));
                season = competition.Queries.GetCurrentSeason()!;
            }

            if (season.FixtureCount == 0)
            {
                var firstMatchday = ComputeFirstMatchdayDayNumber(currentDay);
                competition.PlanLeagueFixtures.Handle(
                    new PlanLeagueFixturesCommand(
                        Guid.NewGuid(),
                        DefaultSeasonId,
                        firstMatchday,
                        StartingFixtureId: 1));

                season = competition.Queries.GetCurrentSeason()!;
            }

            _statusLabel.Text =
                $"Lig hazır: sezon #{season.SeasonId}, {season.ParticipantCount} takım, {season.FixtureCount} maç.";
            RefreshUi();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Lig kurulumu başarısız: {ex.Message}";
        }
    }

    private void PlayTodayMatches()
    {
        try
        {
            var competition = _host.CompetitionModule;
            var world = _host.WorldModule;
            var playHandler = competition.PlayFixtureMatch
                ?? throw new InvalidOperationException("Maç oynatma servisi bağlı değil.");

            var season = competition.Queries.GetCurrentSeason()
                ?? throw new InvalidOperationException("Aktif sezon yok.");

            var currentDay = world.Queries.GetCurrentGameDate().DayNumber;
            var dueFixtures = competition.Queries
                .GetSeasonFixtures(season.SeasonId)
                .Where(fixture =>
                    fixture.ScheduledDayNumber <= currentDay
                    && string.Equals(fixture.Status, nameof(FixtureStatus.Planned), StringComparison.Ordinal))
                .ToArray();

            if (dueFixtures.Length == 0)
            {
                _statusLabel.Text = "Bugün oynanacak planlı maç yok.";
                return;
            }

            var played = 0;
            foreach (var fixture in dueFixtures)
            {
                playHandler.Handle(
                    new PlayFixtureMatchCommand(
                        Guid.NewGuid(),
                        season.SeasonId,
                        fixture.FixtureId,
                        currentDay));
                played++;
            }

            _statusLabel.Text = $"{played} maç oynandı (gün {currentDay}).";
            RefreshUi();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Maç oynatma hatası: {ex.Message}";
        }
    }

    private void CompleteSeason()
    {
        try
        {
            var competition = _host.CompetitionModule;
            var world = _host.WorldModule;
            var season = competition.Queries.GetCurrentSeason()
                ?? throw new InvalidOperationException("Aktif sezon yok.");

            var currentDay = world.Queries.GetCurrentGameDate().DayNumber;
            competition.CompleteSeason.Handle(
                new CompleteSeasonCommand(Guid.NewGuid(), season.SeasonId, currentDay));

            _statusLabel.Text = $"Sezon #{season.SeasonId} kapatıldı.";
            RefreshUi();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Sezon kapatma hatası: {ex.Message}";
        }
    }

    private void ArchiveSeason()
    {
        try
        {
            var competition = _host.CompetitionModule;
            var world = _host.WorldModule;
            var season = competition.Queries.GetCurrentSeason()
                ?? throw new InvalidOperationException("Aktif sezon yok.");

            var currentDay = world.Queries.GetCurrentGameDate().DayNumber;
            competition.ArchiveSeason.Handle(
                new ArchiveSeasonCommand(Guid.NewGuid(), season.SeasonId, currentDay));

            _statusLabel.Text = $"Sezon #{season.SeasonId} arşivlendi.";
            RefreshUi();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Sezon arşivleme hatası: {ex.Message}";
        }
    }

    private void StartNewSeason()
    {
        try
        {
            var competition = _host.CompetitionModule;
            var world = _host.WorldModule;
            var currentDay = world.Queries.GetCurrentGameDate().DayNumber;
            var nextSeasonId = competition.Queries.GetCurrentSeason()?.SeasonId + 1 ?? DefaultSeasonId;

            competition.CreateSeason.Handle(
                new CreateSeasonCommand(Guid.NewGuid(), nextSeasonId, currentDay));

            for (var clubId = 1L; clubId <= CompetitionMvpConstraints.LeagueTeamCount; clubId++)
            {
                competition.RegisterSeasonParticipant.Handle(
                    new RegisterSeasonParticipantCommand(Guid.NewGuid(), nextSeasonId, clubId));
            }

            competition.StartSeason.Handle(
                new StartSeasonCommand(Guid.NewGuid(), nextSeasonId, currentDay));

            var firstMatchday = ComputeFirstMatchdayDayNumber(currentDay);
            competition.PlanLeagueFixtures.Handle(
                new PlanLeagueFixturesCommand(
                    Guid.NewGuid(),
                    nextSeasonId,
                    firstMatchday,
                    StartingFixtureId: 1));

            _statusLabel.Text = $"Yeni sezon #{nextSeasonId} başlatıldı.";
            RefreshUi();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Yeni sezon hatası: {ex.Message}";
        }
    }

    private void RefreshSquadList()
    {
        _squadList.Clear();

        var manager = _host.ManagerModule.Queries.GetCareer();
        if (manager.EmployedClubId is not long clubId)
        {
            return;
        }

        var rootSeed = _host.WorldModule.TimelineStore.Timeline.RootSeed;
        var squad = _host.SquadQueries.GetClubSquad(clubId, rootSeed);
        var clubName = GetClubDisplayName(clubId);

        foreach (var player in squad.Take(10))
        {
            _squadList.AddItem($"{player.SquadNumber}. {player.DisplayName}");
        }

        if (squad.Count > 10)
        {
            _squadList.AddItem($"... ve {squad.Count - 10} oyuncu daha ({clubName})");
        }
    }

    private string GetClubDisplayName(long clubId) =>
        _host.ClubModule.Queries.GetClub(clubId)?.DisplayName ?? $"Kulüp {clubId}";

    private void RefreshFixtureList()
    {
        _fixtureList.Clear();

        var season = _host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null || season.FixtureCount == 0)
        {
            return;
        }

        var round = (int)_roundSelector.Value;
        var fixtures = _host.CompetitionModule.Queries.GetFixturesByRound(season.SeasonId, round);

        foreach (var fixture in fixtures)
        {
            var home = GetClubDisplayName(fixture.HomeClubId);
            var away = GetClubDisplayName(fixture.AwayClubId);
            var score = fixture.HomeGoals is int homeGoals && fixture.AwayGoals is int awayGoals
                ? $" {homeGoals}-{awayGoals}"
                : string.Empty;
            _fixtureList.AddItem(
                $"{home} vs {away}{score} ({fixture.ScheduledIsoDate}, {fixture.Status})");
        }
    }

    private void RefreshStandings()
    {
        var season = _host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null || season.FixtureCount == 0)
        {
            _standingsLabel.Text = "Puan durumu: —";
            return;
        }

        var standings = _host.CompetitionModule.Queries.GetStandings(season.SeasonId);
        if (standings.Count == 0)
        {
            _standingsLabel.Text = "Puan durumu: henüz maç oynanmadı";
            return;
        }

        var preview = string.Join(
            " | ",
            standings.Take(5).Select((entry, index) =>
                $"{index + 1}. {GetClubDisplayName(entry.ClubId)} {entry.Points}p ({entry.Played}M)"));

        _standingsLabel.Text = $"Puan durumu (ilk 5): {preview}";
    }

    private void SaveGame()
    {
        try
        {
            var result = _host.GameSession.Save(_host.DefaultSavePath);
            _statusLabel.Text =
                $"Kayıt tamamlandı: gün {result.SavedDayNumber}, {result.SavedFixtureCount} maç ({result.SavePath})";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Kayıt hatası: {ex.Message}";
        }
    }

    private void LoadGame()
    {
        try
        {
            var result = _host.GameSession.Load(_host.DefaultSavePath);
            _statusLabel.Text = result.WasMigrated
                ? $"Yükleme tamamlandı (migrate): gün {result.LoadedDayNumber}, {result.LoadedFixtureCount} maç"
                : $"Yükleme tamamlandı: gün {result.LoadedDayNumber}, {result.LoadedFixtureCount} maç";
            RefreshUi();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Yükleme hatası: {ex.Message}";
        }
    }

    private void OpenPlanningPeriod()
    {
        try
        {
            var current = _host.WorldModule.Queries.GetCurrentGameDate();
            var result = _host.WorldModule.OpenPlanningPeriod.Handle(
                new OpenPlanningPeriodCommand(
                    Guid.NewGuid(),
                    _nextPlanningPeriodId,
                    current.DayNumber));

            _nextPlanningPeriodId++;
            _statusLabel.Text =
                $"Planlama dönemi açıldı: #{result.PlanningPeriodId} ({result.Status})";
            RefreshUi();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Planlama dönemi açılamadı: {ex.Message}";
        }
    }

    private void CompletePlanningPeriod()
    {
        try
        {
            var result = _host.WorldModule.CompletePlanningPeriod.Handle(
                new CompletePlanningPeriodCommand(Guid.NewGuid()));

            _statusLabel.Text =
                $"Planlama dönemi tamamlandı: #{result.PlanningPeriodId} (gün {result.CompletedAtDayNumber})";
            RefreshUi();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Planlama dönemi tamamlanamadı: {ex.Message}";
        }
    }

    private void RefreshUi()
    {
        var world = _host.WorldModule;
        var competition = _host.CompetitionModule;
        var current = world.Queries.GetCurrentGameDate();
        var eligibility = world.Queries.GetTimeAdvanceEligibility();
        var period = world.Queries.GetCurrentPlanningPeriod();
        var season = competition.Queries.GetCurrentSeason();
        var manager = _host.ManagerModule.Queries.GetCareer();

        _dateLabel.Text = $"Güncel tarih: {current.IsoDate} (DayNumber {current.DayNumber})";
        _periodLabel.Text = period is null
            ? "Aktif planlama dönemi: yok"
            : $"Aktif planlama dönemi: #{period.PlanningPeriodId} ({period.Status})";
        _blockerLabel.Text = eligibility.CanAdvance
            ? "İlerletme engeli: yok"
            : $"İlerletme engeli: {eligibility.Blockers[0].SourceContext} / {eligibility.Blockers[0].DescriptionCode}";

        var clubName = manager.EmployedClubId is long clubId
            ? GetClubDisplayName(clubId)
            : "—";
        _managerLabel.Text =
            $"Menajer: {manager.DisplayName} — Kulüp: {clubName}";

        RefreshSquadList();

        if (season is null)
        {
            _seasonLabel.Text = "Lig sezonu: yok";
            _seasonProgressLabel.Text = "Sezon ilerlemesi: —";
            _standingsLabel.Text = "Puan durumu: —";
            _fixtureList.Clear();
            return;
        }

        _seasonLabel.Text =
            $"Lig sezonu: #{season.SeasonId} ({season.Status}) — {season.ParticipantCount} takım, {season.FixtureCount} maç";

        var progress = competition.Queries.GetSeasonProgress(season.SeasonId);
        _seasonProgressLabel.Text = progress is null
            ? "Sezon ilerlemesi: —"
            : $"Sezon ilerlemesi: {progress.AcceptedFixtureCount}/{progress.TotalFixtureCount} maç sonuçlandı"
              + (progress.CanComplete ? " — kapatılabilir" : string.Empty)
              + (progress.CanArchive ? " — arşivlenebilir" : string.Empty);

        if (season.FixtureCount == 0)
        {
            _standingsLabel.Text = "Puan durumu: —";
            _fixtureList.Clear();
            return;
        }

        RefreshStandings();
        RefreshFixtureList();
    }

    private static int ComputeFirstMatchdayDayNumber(int currentDayNumber) =>
        GameDate.FromDayNumber(currentDayNumber).AddDays(30).DayNumber;

    private void RunSelfCheck()
    {
        var passed = true;
        var world = _host.WorldModule;
        var competition = _host.CompetitionModule;

        var before = world.Queries.GetCurrentGameDate();
        passed &= LogCheck("Başlangıç tarihi", before.DayNumber > 0);

        var result = world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), before.DayNumber + 1));
        passed &= LogCheck("1 gün ilerletme", result.Succeeded && result.NewDayNumber == before.DayNumber + 1);

        var after = world.Queries.GetCurrentGameDate();
        passed &= LogCheck("Query güncel tarih", after.DayNumber == before.DayNumber + 1);

        var selfCheckSavePath = Path.Combine(OS.GetUserDataDir(), "career_ui_selfcheck.db");
        try
        {
            var checkpointDay = after.DayNumber;
            var saveResult = _host.GameSession.Save(selfCheckSavePath);
            passed &= LogCheck("Career kayıt", saveResult.Succeeded && saveResult.SavedDayNumber == checkpointDay);

            world.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(Guid.NewGuid(), checkpointDay + 3));
            passed &= LogCheck(
                "İlerletme sonrası tarih değişti",
                world.Queries.GetCurrentGameDate().DayNumber == checkpointDay + 3);

            var loadResult = _host.GameSession.Load(selfCheckSavePath);
            passed &= LogCheck(
                "Career yükleme checkpoint",
                loadResult.Succeeded && loadResult.LoadedDayNumber == checkpointDay);
            passed &= LogCheck(
                "Yükleme sonrası query",
                world.Queries.GetCurrentGameDate().DayNumber == checkpointDay);
        }
        catch (Exception ex)
        {
            GD.Print($"[SelfCheck] BAŞARISIZ: Kayıt/yükleme — {ex.Message}.");
            passed = false;
        }

        try
        {
            var current = world.Queries.GetCurrentGameDate();
            var openResult = world.OpenPlanningPeriod.Handle(
                new OpenPlanningPeriodCommand(Guid.NewGuid(), 99, current.DayNumber));
            passed &= LogCheck("Planlama dönemi aç", openResult.Succeeded);

            var period = world.Queries.GetCurrentPlanningPeriod();
            passed &= LogCheck("Aktif dönem query", period is not null && period.PlanningPeriodId == 99);

            var completeResult = world.CompletePlanningPeriod.Handle(
                new CompletePlanningPeriodCommand(Guid.NewGuid()));
            passed &= LogCheck("Planlama dönemi tamamla", completeResult.Succeeded);
            passed &= LogCheck("Tamamlama sonrası aktif dönem yok", world.Queries.GetCurrentPlanningPeriod() is null);
        }
        catch (Exception ex)
        {
            GD.Print($"[SelfCheck] BAŞARISIZ: Planlama dönemi — {ex.Message}.");
            passed = false;
        }

        try
        {
            SetupLeagueSeasonForSelfCheck(competition, world);
            var season = competition.Queries.GetCurrentSeason();
            passed &= LogCheck(
                "Lig sezonu kurulumu",
                season is not null
                && season.ParticipantCount == CompetitionMvpConstraints.LeagueTeamCount
                && season.FixtureCount == CompetitionMvpConstraints.TotalLeagueFixtures);

            var leagueSavePath = Path.Combine(OS.GetUserDataDir(), "career_league_selfcheck.db");
            var saveResult = _host.GameSession.Save(leagueSavePath);
            passed &= LogCheck(
                "Lig kayıt fixture sayısı",
                saveResult.SavedFixtureCount == CompetitionMvpConstraints.TotalLeagueFixtures);

            world.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(
                    Guid.NewGuid(),
                    world.Queries.GetCurrentGameDate().DayNumber + 5));

            var loadResult = _host.GameSession.Load(leagueSavePath);
            passed &= LogCheck(
                "Lig yükleme fixture sayısı",
                loadResult.LoadedFixtureCount == CompetitionMvpConstraints.TotalLeagueFixtures);
            passed &= LogCheck(
                "Yükleme sonrası sezon query",
                competition.Queries.GetCurrentSeason()?.FixtureCount
                == CompetitionMvpConstraints.TotalLeagueFixtures);

            var roundOne = competition.Queries.GetFixturesByRound(DefaultSeasonId, round: 1);
            passed &= LogCheck(
                "1. hafta maç sayısı",
                roundOne.Count == CompetitionMvpConstraints.LeagueFixturesPerRound);

            var firstMatchday = roundOne[0].ScheduledDayNumber;
            world.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(Guid.NewGuid(), firstMatchday));
            var playResult = competition.PlayFixtureMatch!.Handle(
                new PlayFixtureMatchCommand(
                    Guid.NewGuid(),
                    DefaultSeasonId,
                    roundOne[0].FixtureId,
                    firstMatchday));
            passed &= LogCheck("Maç oynatma", playResult.Succeeded);
            passed &= LogCheck(
                "Puan durumu güncellendi",
                competition.Queries.GetStandings(DefaultSeasonId).Count(entry => entry.Played > 0) == 2);
        }
        catch (Exception ex)
        {
            GD.Print($"[SelfCheck] BAŞARISIZ: Lig sezonu — {ex.Message}.");
            passed = false;
        }

        GD.Print(passed ? "[SelfCheck] TÜMÜ BAŞARILI." : "[SelfCheck] BİR VEYA DAHA FAZLA KONTROL BAŞARISIZ.");
        GD.Print(passed ? "WORLD_CALENDAR_UI_SMOKE_TEST_RESULT=PASS" : "WORLD_CALENDAR_UI_SMOKE_TEST_RESULT=FAIL");
        GD.Print(passed ? "CAREER_UI_SMOKE_TEST_RESULT=PASS" : "CAREER_UI_SMOKE_TEST_RESULT=FAIL");
        GD.Print(passed ? "SPIKE5_SMOKE_TEST_RESULT=PASS" : "SPIKE5_SMOKE_TEST_RESULT=FAIL");
    }

    private static void SetupLeagueSeasonForSelfCheck(
        CompetitionModule competition,
        WorldCalendarModule world)
    {
        var currentDay = world.Queries.GetCurrentGameDate().DayNumber;

        if (competition.Queries.GetCurrentSeason() is null)
        {
            competition.CreateSeason.Handle(
                new CreateSeasonCommand(Guid.NewGuid(), DefaultSeasonId, currentDay));
        }

        for (var clubId = 1L; clubId <= CompetitionMvpConstraints.LeagueTeamCount; clubId++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), DefaultSeasonId, clubId));
        }

        competition.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), DefaultSeasonId, currentDay));

        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                DefaultSeasonId,
                ComputeFirstMatchdayDayNumber(currentDay),
                StartingFixtureId: 1));
    }

    private static bool LogCheck(string name, bool ok)
    {
        GD.Print(ok ? $"[SelfCheck] {name} OK." : $"[SelfCheck] BAŞARISIZ: {name}.");
        return ok;
    }
}
