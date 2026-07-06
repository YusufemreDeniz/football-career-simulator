using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Production Kart 6: yalnızca Application command/query contract'ları üzerinden çalışan minimum
/// zaman kontrol ekranı.
/// </summary>
public partial class TimeControlScreen : Control
{
    private WorldCalendarPresentationHost _host = null!;
    private WorldCalendarModule _module = null!;
    private long _nextPlanningPeriodId = 1;
    private Label _dateLabel = null!;
    private Label _periodLabel = null!;
    private Label _blockerLabel = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        _host = WorldCalendarPresentationHost.CreateDefault();
        _module = _host.Module;
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

        var advanceDayButton = new Button { Text = "1 Gün İlerlet" };
        advanceDayButton.Pressed += () => AdvanceDays(1);
        layout.AddChild(advanceDayButton);

        var advanceWeekButton = new Button { Text = "7 Gün İlerlet" };
        advanceWeekButton.Pressed += () => AdvanceDays(7);
        layout.AddChild(advanceWeekButton);

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
        var current = _module.Queries.GetCurrentGameDate();
        var result = _module.AdvanceSimulationTime.Handle(
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

    private void SaveGame()
    {
        try
        {
            var result = _host.GameSession.Save(_host.DefaultSavePath);
            _statusLabel.Text =
                $"Kayıt tamamlandı: gün {result.SavedDayNumber} ({result.SavePath})";
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
                ? $"Yükleme tamamlandı (migrate): gün {result.LoadedDayNumber}"
                : $"Yükleme tamamlandı: gün {result.LoadedDayNumber}";
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
            var current = _module.Queries.GetCurrentGameDate();
            var result = _module.OpenPlanningPeriod.Handle(
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
            var result = _module.CompletePlanningPeriod.Handle(
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
        var current = _module.Queries.GetCurrentGameDate();
        var eligibility = _module.Queries.GetTimeAdvanceEligibility();
        var period = _module.Queries.GetCurrentPlanningPeriod();

        _dateLabel.Text = $"Güncel tarih: {current.IsoDate} (DayNumber {current.DayNumber})";
        _periodLabel.Text = period is null
            ? "Aktif planlama dönemi: yok"
            : $"Aktif planlama dönemi: #{period.PlanningPeriodId} ({period.Status})";
        _blockerLabel.Text = eligibility.CanAdvance
            ? "İlerletme engeli: yok"
            : $"İlerletme engeli: {eligibility.Blockers[0].SourceContext} / {eligibility.Blockers[0].DescriptionCode}";
    }

    private void RunSelfCheck()
    {
        var passed = true;

        var before = _module.Queries.GetCurrentGameDate();
        passed &= LogCheck("Başlangıç tarihi", before.DayNumber > 0);

        var result = _module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), before.DayNumber + 1));
        passed &= LogCheck("1 gün ilerletme", result.Succeeded && result.NewDayNumber == before.DayNumber + 1);

        var after = _module.Queries.GetCurrentGameDate();
        passed &= LogCheck("Query güncel tarih", after.DayNumber == before.DayNumber + 1);

        var selfCheckSavePath = Path.Combine(OS.GetUserDataDir(), "world_calendar_selfcheck.db");
        try
        {
            var checkpointDay = after.DayNumber;
            var saveResult = _host.GameSession.Save(selfCheckSavePath);
            passed &= LogCheck("Kayıt", saveResult.Succeeded && saveResult.SavedDayNumber == checkpointDay);

            _module.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(Guid.NewGuid(), checkpointDay + 3));
            passed &= LogCheck(
                "İlerletme sonrası tarih değişti",
                _module.Queries.GetCurrentGameDate().DayNumber == checkpointDay + 3);

            var loadResult = _host.GameSession.Load(selfCheckSavePath);
            passed &= LogCheck(
                "Yükleme checkpoint",
                loadResult.Succeeded && loadResult.LoadedDayNumber == checkpointDay);
            passed &= LogCheck(
                "Yükleme sonrası query",
                _module.Queries.GetCurrentGameDate().DayNumber == checkpointDay);
        }
        catch (Exception ex)
        {
            GD.Print($"[SelfCheck] BAŞARISIZ: Kayıt/yükleme — {ex.Message}.");
            passed = false;
        }

        try
        {
            var current = _module.Queries.GetCurrentGameDate();
            var openResult = _module.OpenPlanningPeriod.Handle(
                new OpenPlanningPeriodCommand(Guid.NewGuid(), 99, current.DayNumber));
            passed &= LogCheck("Planlama dönemi aç", openResult.Succeeded);

            var period = _module.Queries.GetCurrentPlanningPeriod();
            passed &= LogCheck("Aktif dönem query", period is not null && period.PlanningPeriodId == 99);

            var completeResult = _module.CompletePlanningPeriod.Handle(
                new CompletePlanningPeriodCommand(Guid.NewGuid()));
            passed &= LogCheck("Planlama dönemi tamamla", completeResult.Succeeded);
            passed &= LogCheck("Tamamlama sonrası aktif dönem yok", _module.Queries.GetCurrentPlanningPeriod() is null);
        }
        catch (Exception ex)
        {
            GD.Print($"[SelfCheck] BAŞARISIZ: Planlama dönemi — {ex.Message}.");
            passed = false;
        }

        GD.Print(passed ? "[SelfCheck] TÜMÜ BAŞARILI." : "[SelfCheck] BİR VEYA DAHA FAZLA KONTROL BAŞARISIZ.");
        GD.Print(passed ? "WORLD_CALENDAR_UI_SMOKE_TEST_RESULT=PASS" : "WORLD_CALENDAR_UI_SMOKE_TEST_RESULT=FAIL");
        GD.Print(passed ? "SPIKE5_SMOKE_TEST_RESULT=PASS" : "SPIKE5_SMOKE_TEST_RESULT=FAIL");
    }

    private static bool LogCheck(string name, bool ok)
    {
        GD.Print(ok ? $"[SelfCheck] {name} OK." : $"[SelfCheck] BAŞARISIZ: {name}.");
        return ok;
    }
}
