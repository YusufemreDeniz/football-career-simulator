using FootballCareerSimulator.Application.Spike4Placeholder;
using FootballCareerSimulator.Simulation;
using FootballCareerSimulator.Simulation.Spike1Placeholder;
using Godot;

namespace FootballCareerSimulator.Presentation.Prototypes;

/// <summary>
/// docs/18_SPIKE_EXECUTION_PLAN.md Kart 6 (Spike 4) için oluşturulmuş yer tutucu ekrandır. Godot
/// `Control`/`Tree` UI'sinin ~500 futbolculuk bir listede sıralama, filtreleme, seçim ve sayfalama
/// (virtualization/row-recycling yaklaşımlarından biri) ile başa çıkabildiğini kanıtlar. Tüm arayüz
/// düğümleri kodla oluşturulur (elle yazılmış karmaşık bir .tscn dosyası riskini azaltmak için).
///
/// UI, Domain/Simulation state'ini asla doğrudan değiştirmez; yalnızca
/// `FootballCareerSimulator.Application.Spike4Placeholder.PlayerListQuery` üzerinden okunan bir read
/// model'i render eder (`docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md` Bölüm 8.4 ile uyumlu).
/// </summary>
public partial class PlayerListScreen : Control
{
    private const int PageSize = 50;
    private const int AutoPageStressFrames = 300;
    private const int PerformanceReportIntervalFrames = 60;
    private const int RollingFrameWindowSize = 300;

    private IReadOnlyList<PlayerListRow> _allRows = Array.Empty<PlayerListRow>();
    private IReadOnlyList<PlayerListRow> _filteredSortedRows = Array.Empty<PlayerListRow>();
    private PlayerListSortColumn _sortColumn = PlayerListSortColumn.PlayerId;
    private bool _sortAscending = true;
    private string _searchText = string.Empty;
    private int _pageIndex;

    private LineEdit _filterInput = null!;
    private Tree _tree = null!;
    private Label _pageLabel = null!;
    private Label _selectionLabel = null!;
    private Label _filterTimingLabel = null!;
    private Label _performanceLabel = null!;

    private readonly List<double> _recentFrameTimesMs = new(RollingFrameWindowSize);
    private long _frameCounter;
    private bool _autoPageStressActive = true;

    public override void _Ready()
    {
        BuildLayout();

        var (world, _) = HeadlessSimulationRunner.CreateWorld(seed: 42);
        _allRows = PlayerListQuery.BuildRows(world);

        ApplyFilterAndSort(resetPage: true, logTiming: true);

        GD.Print($"[PlayerListScreen] Hazır. {_allRows.Count} futbolcu, {WorldFactory.ClubCount} kulüp yüklendi.");

        RunSelfCheck();
        RunSaveAndLogWriteCheck();
    }

    /// <summary>
    /// docs/18_SPIKE_EXECUTION_PLAN.md Kart 7 (Spike 5) başarı kriterlerinden "save ve log klasörlerine
    /// yazılabilir"i doğrudan kanıtlar: `user://` (paketlenmiş exe'de gerçek bir kullanıcı veri
    /// klasörüne karşılık gelir) altına küçük bir işaret dosyası yazar, geri okur ve doğrular.
    /// </summary>
    private void RunSaveAndLogWriteCheck()
    {
        const string relativePath = "user://spike5_write_check.txt";
        var marker = $"Spike5 write check - {DateTime.UtcNow:O}";

        try
        {
            using (var writeHandle = Godot.FileAccess.Open(relativePath, Godot.FileAccess.ModeFlags.Write))
            {
                if (writeHandle is null)
                {
                    GD.PrintErr($"[SelfCheck] BAŞARISIZ: save/log yazma - dosya açılamadı ({Godot.FileAccess.GetOpenError()}).");
                    return;
                }

                writeHandle.StoreString(marker);
            }

            using var readHandle = Godot.FileAccess.Open(relativePath, Godot.FileAccess.ModeFlags.Read);
            var readBack = readHandle?.GetAsText();

            var ok = readHandle is not null && readBack == marker;
            GD.Print(ok ? "[SelfCheck] Save/log dizinine yazma OK." : "[SelfCheck] BAŞARISIZ: Save/log dizinine yazma.");
            GD.Print($"[SelfCheck] Kullanıcı veri dizini: {OS.GetUserDataDir()}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SelfCheck] BAŞARISIZ: save/log yazma istisna fırlattı: {ex.Message}");
        }
    }

    /// <summary>
    /// Gerçek fare/klavye etkileşimi olmadan (headless dahil) çalıştırıldığında bile, filtre, sıralama
    /// ve seçim tellerinin (wiring) gerçekten Tree/Label'lara doğru yansıdığını kanıtlayan gömülü bir
    /// öz-kontroldür. `Spike4PlayerListQueryTests` yalnızca saf sorgu mantığını test eder; bu metod ise
    /// o mantığın Godot UI düğümlerine doğru bağlandığını doğrular.
    /// </summary>
    private void RunSelfCheck()
    {
        var passed = true;

        if (_filteredSortedRows.Count != _allRows.Count)
        {
            GD.PrintErr($"[SelfCheck] BAŞARISIZ: başlangıç sonuç sayısı {_filteredSortedRows.Count}, beklenen {_allRows.Count}.");
            passed = false;
        }

        _searchText = "Placeholder Club 05";
        ApplyFilterAndSort(resetPage: true, logTiming: false);
        var filteredOk = _filteredSortedRows.Count > 0 && _filteredSortedRows.All(row => row.ClubName == "Placeholder Club 05");
        passed &= LogCheck("Filtre", filteredOk);

        _searchText = string.Empty;
        OnSortRequested(PlayerListSortColumn.Age);
        passed &= LogCheck("Sıralama (artan)", IsSortedByAge(_filteredSortedRows, ascending: true));

        OnSortRequested(PlayerListSortColumn.Age);
        passed &= LogCheck("Sıralama (azalan, aynı kolona tekrar tıklama)", IsSortedByAge(_filteredSortedRows, ascending: false));

        _sortColumn = PlayerListSortColumn.PlayerId;
        _sortAscending = true;
        ApplyFilterAndSort(resetPage: true, logTiming: false);

        var root = _tree.GetRoot();
        var firstItem = root?.GetFirstChild();

        if (firstItem is not null)
        {
            firstItem.Select(0);
            OnTreeItemSelected();
            passed &= LogCheck("Seçim", _selectionLabel.Text.StartsWith("Seçili: Player#", StringComparison.Ordinal));
        }
        else
        {
            passed = LogCheck("Seçim", false);
        }

        GD.Print(passed ? "[SelfCheck] TÜMÜ BAŞARILI." : "[SelfCheck] BİR VEYA DAHA FAZLA KONTROL BAŞARISIZ.");

        // docs/18_SPIKE_EXECUTION_PLAN.md Kart 8: CI'daki "exported build smoke test" adımının konsol
        // kodlamasından (encoding) bağımsız, kararlı biçimde ayrıştırabilmesi için salt ASCII bir işaret
        // satırı da yazılır; Türkçe karakterli mesajların yerini almaz, yalnızca otomasyon için eklenir.
        GD.Print(passed ? "SPIKE5_SMOKE_TEST_RESULT=PASS" : "SPIKE5_SMOKE_TEST_RESULT=FAIL");
    }

    private static bool LogCheck(string name, bool ok)
    {
        GD.Print(ok ? $"[SelfCheck] {name} OK." : $"[SelfCheck] BAŞARISIZ: {name}.");
        return ok;
    }

    private static bool IsSortedByAge(IReadOnlyList<PlayerListRow> rows, bool ascending)
    {
        for (var i = 1; i < rows.Count; i++)
        {
            var comparison = rows[i - 1].Age.CompareTo(rows[i].Age);

            if (ascending ? comparison > 0 : comparison < 0)
            {
                return false;
            }
        }

        return true;
    }

    public override void _Process(double delta)
    {
        RecordFrameTime(delta * 1000.0);
        _frameCounter++;

        if (_autoPageStressActive)
        {
            if (_frameCounter <= AutoPageStressFrames)
            {
                if (_frameCounter % 5 == 0)
                {
                    // Gerçek fare tekerleği etkileşimini otomatikleştirilmiş biçimde simüle eder:
                    // manuel scroll'dan daha sık ve daha yoğun sayfa/render değişimi tetikler.
                    GoToPageWrapping(_pageIndex + 1);
                }
            }
            else
            {
                _autoPageStressActive = false;
                GD.Print($"[PlayerListScreen] Otomatik sayfa stres testi tamamlandı ({AutoPageStressFrames} frame).");
                ReportPerformance(label: "stres-sonu");
            }
        }

        if (_frameCounter % PerformanceReportIntervalFrames == 0)
        {
            ReportPerformance(label: _autoPageStressActive ? "stres-sırasında" : "durağan");
        }
    }

    private void BuildLayout()
    {
        AnchorRight = 1f;
        AnchorBottom = 1f;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        var layout = new VBoxContainer { Name = "Layout" };
        layout.AnchorRight = 1f;
        layout.AnchorBottom = 1f;
        layout.GrowHorizontal = GrowDirection.Both;
        layout.GrowVertical = GrowDirection.Both;
        AddChild(layout);

        var toolbar = new HBoxContainer { Name = "Toolbar" };
        layout.AddChild(toolbar);

        _filterInput = new LineEdit
        {
            Name = "FilterInput",
            PlaceholderText = "Filtrele (kulüp veya oyuncu)...",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _filterInput.TextChanged += OnFilterTextChanged;
        toolbar.AddChild(_filterInput);

        AddSortButton(toolbar, "Sırala: Id", PlayerListSortColumn.PlayerId);
        AddSortButton(toolbar, "Sırala: Kulüp", PlayerListSortColumn.ClubName);
        AddSortButton(toolbar, "Sırala: Yaş", PlayerListSortColumn.Age);
        AddSortButton(toolbar, "Sırala: Form", PlayerListSortColumn.Form);

        var pager = new HBoxContainer { Name = "Pager" };
        layout.AddChild(pager);

        var prevButton = new Button { Text = "< Önceki" };
        prevButton.Pressed += () => GoToPageWrapping(_pageIndex - 1);
        pager.AddChild(prevButton);

        _pageLabel = new Label { Name = "PageLabel" };
        pager.AddChild(_pageLabel);

        var nextButton = new Button { Text = "Sonraki >" };
        nextButton.Pressed += () => GoToPageWrapping(_pageIndex + 1);
        pager.AddChild(nextButton);

        _tree = new Tree
        {
            Name = "PlayerTree",
            Columns = 4,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SelectMode = Tree.SelectModeEnum.Single,
        };
        _tree.SetColumnTitlesVisible(true);
        _tree.SetColumnTitle(0, "Id");
        _tree.SetColumnTitle(1, "Kulüp");
        _tree.SetColumnTitle(2, "Yaş");
        _tree.SetColumnTitle(3, "Form");
        _tree.ItemSelected += OnTreeItemSelected;
        layout.AddChild(_tree);

        _selectionLabel = new Label { Name = "SelectionLabel", Text = "Seçili: -" };
        layout.AddChild(_selectionLabel);

        _filterTimingLabel = new Label { Name = "FilterTimingLabel" };
        layout.AddChild(_filterTimingLabel);

        _performanceLabel = new Label { Name = "PerformanceLabel" };
        layout.AddChild(_performanceLabel);
    }

    private void AddSortButton(HBoxContainer parent, string text, PlayerListSortColumn column)
    {
        var button = new Button { Text = text };
        button.Pressed += () => OnSortRequested(column);
        parent.AddChild(button);
    }

    private void OnSortRequested(PlayerListSortColumn column)
    {
        if (_sortColumn == column)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = column;
            _sortAscending = true;
        }

        ApplyFilterAndSort(resetPage: true, logTiming: true);
    }

    private void OnFilterTextChanged(string newText)
    {
        _searchText = newText;
        ApplyFilterAndSort(resetPage: true, logTiming: true);
    }

    private void ApplyFilterAndSort(bool resetPage, bool logTiming)
    {
        var stopwatch = logTiming ? System.Diagnostics.Stopwatch.StartNew() : null;

        var filtered = PlayerListQuery.Filter(_allRows, _searchText);
        _filteredSortedRows = PlayerListQuery.Sort(filtered, _sortColumn, _sortAscending);

        if (resetPage)
        {
            _pageIndex = 0;
        }

        stopwatch?.Stop();

        if (stopwatch is not null)
        {
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            _filterTimingLabel.Text = $"Filtre/sıralama süresi: {elapsedMs:F2} ms ({_filteredSortedRows.Count} sonuç)";

            if (elapsedMs >= 100)
            {
                GD.PrintErr($"[PlayerListScreen] UYARI: filtre/sıralama 100 ms hedefini aştı: {elapsedMs:F2} ms.");
            }
        }

        RenderCurrentPage();
    }

    private void GoToPageWrapping(int requestedPageIndex)
    {
        var pageCount = PlayerListQuery.GetPageCount(_filteredSortedRows.Count, PageSize);
        _pageIndex = ((requestedPageIndex % pageCount) + pageCount) % pageCount;
        RenderCurrentPage();
    }

    private void RenderCurrentPage()
    {
        var pageCount = PlayerListQuery.GetPageCount(_filteredSortedRows.Count, PageSize);
        var pageRows = PlayerListQuery.Page(_filteredSortedRows, _pageIndex, PageSize);

        _tree.Clear();
        var root = _tree.CreateItem();

        foreach (var row in pageRows)
        {
            var item = _tree.CreateItem(root);
            item.SetText(0, row.PlayerLabel);
            item.SetText(1, row.ClubName);
            item.SetText(2, row.Age.ToString());
            item.SetText(3, row.Form.ToString());
            item.SetMetadata(0, row.PlayerId);
        }

        _pageLabel.Text = $"Sayfa {_pageIndex + 1} / {pageCount} ({_filteredSortedRows.Count} sonuç, sayfa boyutu {PageSize})";
    }

    private void OnTreeItemSelected()
    {
        var selected = _tree.GetSelected();

        if (selected is null)
        {
            _selectionLabel.Text = "Seçili: -";
            return;
        }

        var playerId = selected.GetMetadata(0).AsInt32();
        var row = _filteredSortedRows.FirstOrDefault(r => r.PlayerId == playerId);

        _selectionLabel.Text = row is null
            ? "Seçili: -"
            : $"Seçili: {row.PlayerLabel} ({row.ClubName}, yaş {row.Age}, form {row.Form})";
    }

    private void RecordFrameTime(double frameTimeMs)
    {
        _recentFrameTimesMs.Add(frameTimeMs);

        if (_recentFrameTimesMs.Count > RollingFrameWindowSize)
        {
            _recentFrameTimesMs.RemoveAt(0);
        }
    }

    private void ReportPerformance(string label)
    {
        if (_recentFrameTimesMs.Count == 0)
        {
            return;
        }

        var sorted = _recentFrameTimesMs.OrderBy(ms => ms).ToArray();
        var p95Index = Math.Clamp((int)Math.Ceiling(0.95 * sorted.Length) - 1, 0, sorted.Length - 1);
        var p95 = sorted[p95Index];
        var average = sorted.Average();
        var min = sorted[0];
        var max = sorted[^1];
        var fps = Engine.GetFramesPerSecond();

        _performanceLabel.Text =
            $"FPS: {fps:F0} | frame ms (son {sorted.Length}): ort={average:F2} min={min:F2} max={max:F2} p95={p95:F2}";

        GD.Print($"[Perf:{label}] frame={_frameCounter} fps={fps:F1} avgMs={average:F2} p95Ms={p95:F2} minMs={min:F2} maxMs={max:F2}");

        if (p95 > 33.0)
        {
            GD.PrintErr($"[PlayerListScreen] UYARI: p95 frame süresi 33 ms hedefini aştı: {p95:F2} ms ({label}).");
        }
    }
}
