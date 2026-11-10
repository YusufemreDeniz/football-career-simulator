using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// FM26 FMPedia tarzı isteğe bağlı, aranabilir oyun içi rehber ve mekanik sözlüğü.
/// Oyuncuyu zorunlu metin turlarıyla boğmadan kuralları tek dokunuşla açıklar.
/// </summary>
internal sealed partial class FmPediaModal : ColorRect
{
    private LineEdit _searchInput = null!;
    private VBoxContainer _entriesBox = null!;
    private string _activeCategory = "ALL";

    private sealed record PediaEntry(
        string Category,
        string Icon,
        string Title,
        string Summary,
        string TacticalTip);

    private static readonly PediaEntry[] Entries =
    [
        new(
            "GAMEPLAY",
            "🃏",
            "Dokunsal Menajer Kart Ekonomisi",
            "Kulüp tesislerin ve teknik ekibin her hafta Enerji Paketi, Motivasyon Konuşması, Fizyoterapi ve Özel Gözlemci kartları üretir.",
            "Maç öncesi yorgun yıldızına Enerji Kartı basarak kondisyonunu +%12 artırabilir, soyunma odası krizlerini Motivasyon Kartı ile çözebilirsin."),
        new(
            "GAMEPLAY",
            "🎯",
            "Günlük Hedefler (5 Dakikalık Oturum Döngüsü)",
            "Portal ekranındaki 3 günlük hedefi tamamlamak (idman drili, taktik odak ayarı, kadro/kart hamlesi) anında Menajer XP ve takım morali kazandırır.",
            "Her gün kısa bir oturumda 3 görevi tamamlayarak menajer seviyeni hızla yükselt ve yeni kart hakları aç."),
        new(
            "TACTICS",
            "🟩",
            "Topla (IP) / Topsuz (OOP) Çift Fazlı Diziliş",
            "Takımın topa sahipken (Hücum Fazı) ve top rakipteyken (Savunma Fazı) sahada farklı diziliş şekilleri alır.",
            "Taktik ekranında 'Topla / Topsuz' anahtarına dokunarak oyuncularının hücumda nasıl yayıldığını ve savunmada nasıl kapandığını canlı gör."),
        new(
            "TACTICS",
            "🎚️",
            "Taktik Odak Kaydırıcıları (Focus Sliders)",
            "Hücum Odağı, Pres Şiddeti ve Savunma Hattı kaydırıcıları saha içi boşlukları ve kondisyon tüketimini doğrudan belirler.",
            "Deplasmanda güçlü rakiplere karşı Savunma Hattını 35'e çekip Hızlı Kontra presetini seçmek arkada boşluk bırakmanı önler."),
        new(
            "MATCH",
            "🔥",
            "Canlı Maç Aura Barı & Kritik Pozisyon Kararı",
            "Maç akarken takımının mücadelesi Kenar Momentum/Aura Barını doldurur. Kritik ataklarda oyun duraksar ve sana 3 dokunsal seçim sunar.",
            "Aura %40'ı geçtiğinde kenardan 'Şok Pres' veya 'Kanat Bindirmesi' tetikleyerek maçın kaderini değiştirebilirsin."),
        new(
            "SOCIAL",
            "🤝",
            "Forma Sözü Zinciri & Telafi Hafızası (Redemption)",
            "Bir oyuncuya verdiğin İlk 11 veya Süre sözünü tutarsan güveni fırlar; söz bozulursa soyunma odasında kulis söylentisi yayılır.",
            "Kırgın bir oyuncuyu üst üste maçlarda oynatarak 'Telafi Hafızası'nı tetikleyebilir ve güvenini yeniden kazanabilirsin."),
        new(
            "CLUB",
            "🏟️",
            "Görsel Kulüp Kampüsü & Kulüp DNA'sı",
            "Stadyum, Antrenman Merkezi, Akademi ve Sağlık Spa tesislerin hem gelir/gelişim çarpanı sağlar hem de dokunsal kart üretimini hızlandırır.",
            "Tribünlere kendi istediğin ismi verebilir, tesis seviyesini yükselterek sakatlık riskini kalıcı olarak düşürebilirsin."),
        new(
            "TRANSFER",
            "🔭",
            "Gözlem Güveni & Satıcı Pazarlık Hafızası",
            "Scout raporları yaşlandıkça bilgi güven kademesi düşer. Satıcı kulüpler ise önceki tekliflerini ve masadan kalkmalarını hatırlar.",
            "Kısa listedeki 3 adayı yan yana karşılaştırırken 'Tam Rapor' rozetli ve düşük riskli adaylara öncelik ver."),
    ];

    public static void Open(Control parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is FmPediaModal existing)
            {
                existing.QueueFree();
            }
        }

        var modal = new FmPediaModal();
        parent.AddChild(modal);
    }

    public override void _Ready()
    {
        Color = new Color(0.01f, 0.03f, 0.06f, 0.86f);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var center = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var cardPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(880, 520),
        };
        cardPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        center.AddChild(cardPanel);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddThemeConstantOverride("separation", 8);
        cardPanel.AddChild(root);

        // Header
        var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headerRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(headerRow);
        headerRow.AddChild(UiComponents.PageHeader("📖", "FMPEDIA — HIZLI OYUN REHBERİ & SÖZLÜK"));

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headerRow.AddChild(spacer);

        var closeBtn = new Button
        {
            Text = "✕ KAPAT",
            CustomMinimumSize = new Vector2(96, 36),
        };
        CareerUiTheme.StyleDangerButton(closeBtn);
        closeBtn.Pressed += QueueFree;
        headerRow.AddChild(closeBtn);

        // Search & Filter Bar
        var filterRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        filterRow.AddThemeConstantOverride("separation", 6);
        root.AddChild(filterRow);

        _searchInput = new LineEdit
        {
            PlaceholderText = "🔍 Mekanik, taktik, kart veya kural ara (ör. Aura, Söz, Kampüs, IP/OOP)...",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(260, 36),
        };
        _searchInput.TextChanged += _ => RefreshEntries();
        filterRow.AddChild(_searchInput);

        AddCategoryButton(filterRow, "🌍 Tümü", "ALL");
        AddCategoryButton(filterRow, "🃏 Kart & Görev", "GAMEPLAY");
        AddCategoryButton(filterRow, "🟩 Taktik", "TACTICS");
        AddCategoryButton(filterRow, "🔥 Canlı Maç", "MATCH");
        AddCategoryButton(filterRow, "🏛️ Kulüp & Sosyal", "SOCIAL");

        var scroll = new MobileScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 380),
        };
        root.AddChild(scroll);

        _entriesBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _entriesBox.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_entriesBox);

        RefreshEntries();
    }

    private void AddCategoryButton(HBoxContainer row, string label, string code)
    {
        var btn = new Button
        {
            Text = label,
            CustomMinimumSize = new Vector2(0, 34),
        };
        CareerUiTheme.StyleTabButton(btn, selected: _activeCategory == code);
        btn.Pressed += () =>
        {
            _activeCategory = code;
            RefreshEntries();
        };
        row.AddChild(btn);
    }

    private void RefreshEntries()
    {
        MatchScreenUi.ClearChildren(_entriesBox);
        var query = (_searchInput?.Text ?? string.Empty).Trim();

        foreach (var entry in Entries)
        {
            if (_activeCategory != "ALL"
                && !string.Equals(entry.Category, _activeCategory, StringComparison.OrdinalIgnoreCase)
                && !(_activeCategory == "SOCIAL" && entry.Category == "CLUB")
                && !(_activeCategory == "SOCIAL" && entry.Category == "TRANSFER"))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(query)
                && !entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !entry.Summary.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !entry.TacticalTip.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            panel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
            _entriesBox.AddChild(panel);

            var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            box.AddThemeConstantOverride("separation", 4);
            panel.AddChild(box);

            var title = new Label { Text = $"{entry.Icon}  {entry.Title}" };
            CareerUiTheme.StyleHeadline(title);
            title.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
            title.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(14));
            box.AddChild(title);

            var summary = new Label
            {
                Text = entry.Summary,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleBody(summary);
            summary.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
            box.AddChild(summary);

            var tip = new Label
            {
                Text = $"💡 İpucu: {entry.TacticalTip}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleBody(tip, muted: true);
            tip.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
            tip.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
            box.AddChild(tip);
        }
    }
}
