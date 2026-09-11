using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Kariyer arayüzü için yeniden kullanılabilir Godot UI bileşen builder'ları.
/// Tüm method'lar statik — her biri bir Godot node ağacı döndürür; caller AddChild() ile bağlar.
/// Hard-coded renk/boyut/boşluk değeri içermez; CareerUiTheme token'larını kullanır.
/// </summary>
internal static class UiComponents
{
    // ── Section Header ───────────────────────────────────────────────────────

    /// <summary>
    /// Section başlığı: 18px Syne label + altında ince dekoratif separator.
    /// Kart içinde section'lar arası görsel hiyerarşi için.
    /// </summary>
    public static VBoxContainer SectionHeader(string title, bool withDivider = true)
    {
        var container = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        container.AddThemeConstantOverride("separation", CareerUiTheme.SpaceXs);

        var label = new Label
        {
            Text = title,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleSectionTitle(label);
        container.AddChild(label);

        if (withDivider)
        {
            container.AddChild(Divider());
        }

        return container;
    }

    /// <summary>
    /// Eyebrow + başlık çifti. Sayfa ve card hero'su için.
    /// </summary>
    public static VBoxContainer EyebrowHeader(string eyebrow, string title)
    {
        var container = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        container.AddThemeConstantOverride("separation", 2);

        var eyebrowLabel = new Label { Text = eyebrow };
        CareerUiTheme.StyleEyebrow(eyebrowLabel, CareerUiTheme.Accent);
        container.AddChild(eyebrowLabel);

        var titleLabel = new Label { Text = title };
        CareerUiTheme.StyleSectionTitle(titleLabel);
        container.AddChild(titleLabel);

        return container;
    }

    // ── Divider ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Yatay separator çizgisi. Section'lar ve kart içi bölümler arası.
    /// </summary>
    public static Control Divider(int marginVertical = 4)
    {
        var margin = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        margin.AddThemeConstantOverride("margin_top", marginVertical);
        margin.AddThemeConstantOverride("margin_bottom", marginVertical);
        margin.AddThemeConstantOverride("margin_left", 0);
        margin.AddThemeConstantOverride("margin_right", 0);

        var line = new ColorRect
        {
            Color = CareerUiTheme.BorderSubtle,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        margin.AddChild(line);
        return margin;
    }

    // ── Stat Block ───────────────────────────────────────────────────────────

    /// <summary>
    /// İstatistik bloğu: üstte küçük etiket, ortada büyük değer, altta opsiyonel birim.
    /// Metrik kart, performans özeti, maç sonucu için.
    /// </summary>
    public static VBoxContainer StatBlock(
        string label,
        string value,
        string? unit = null,
        Color? valueColor = null)
    {
        var container = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        container.AddThemeConstantOverride("separation", CareerUiTheme.SeparationTight);

        var labelNode = new Label { Text = label, HorizontalAlignment = HorizontalAlignment.Center };
        CareerUiTheme.StyleTableHeader(labelNode);
        container.AddChild(labelNode);

        var valueRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        valueRow.AddThemeConstantOverride("separation", 2);
        container.AddChild(valueRow);

        var valueLabel = new Label { Text = value, HorizontalAlignment = HorizontalAlignment.Center };
        CareerUiTheme.StyleStatValue(valueLabel, valueColor);
        valueRow.AddChild(valueLabel);

        if (unit is not null)
        {
            var unitLabel = new Label { Text = unit, SizeFlagsVertical = Control.SizeFlags.ShrinkEnd };
            CareerUiTheme.StyleTableText(unitLabel, muted: true);
            valueRow.AddChild(unitLabel);
        }

        return container;
    }

    /// <summary>
    /// Kompakt metrik satırı: sol label, sağ değer.
    /// Özet listeler, kart içi özet için.
    /// </summary>
    public static HBoxContainer MetricRow(
        string label,
        string value,
        Color? valueColor = null,
        bool muted = false)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", CareerUiTheme.SpaceS);

        var labelNode = new Label { Text = label, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        CareerUiTheme.StyleTableText(labelNode, muted: muted);
        row.AddChild(labelNode);

        var valueNode = new Label { Text = value, HorizontalAlignment = HorizontalAlignment.Right };
        CareerUiTheme.StyleTableText(valueNode, muted: muted);
        if (valueColor.HasValue)
        {
            valueNode.AddThemeColorOverride("font_color", valueColor.Value);
        }

        row.AddChild(valueNode);
        return row;
    }

    // ── Status Badge ─────────────────────────────────────────────────────────

    /// <summary>
    /// Inline renkli pill etiketi: form, durum, kategori bilgisi.
    /// </summary>
    public static PanelContainer StatusBadge(string text, Color? color = null)
    {
        var signalColor = color ?? CareerUiTheme.Data;
        var panel = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatusBadge(signalColor));

        var label = new Label { Text = text };
        CareerUiTheme.StylePositionTag(label, signalColor);
        panel.AddChild(label);

        return panel;
    }

    /// <summary>
    /// Mevki badge: GK, CB, ST gibi pozisyon kısaltması (mevki grup rengiyle otomatik stilize edilir).
    /// </summary>
    public static PanelContainer PositionBadge(string positionCode)
    {
        var color = CareerUiTheme.PositionGroupColor(positionCode);
        var panel = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.PositionTagPanel());

        var label = new Label { Text = positionCode };
        CareerUiTheme.StylePositionTag(label, color);
        panel.AddChild(label);

        return panel;
    }

    // ── Empty State ──────────────────────────────────────────────────────────

    /// <summary>
    /// Boş durum göstergesi: sembol, başlık ve açıklama.
    /// ItemList veya kart içeriği yokken gösterilir.
    /// </summary>
    public static VBoxContainer EmptyState(
        string title,
        string description = "",
        string symbol = "○")
    {
        var container = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        container.AddThemeConstantOverride("separation", CareerUiTheme.SpaceS);

        var symbolLabel = new Label { Text = symbol, HorizontalAlignment = HorizontalAlignment.Center };
        CareerUiTheme.StyleBrand(symbolLabel);
        symbolLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(32));
        symbolLabel.AddThemeColorOverride("font_color",
            new Color(CareerUiTheme.InkMuted.R, CareerUiTheme.InkMuted.G, CareerUiTheme.InkMuted.B, 0.38f));
        container.AddChild(symbolLabel);

        var titleLabel = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center };
        CareerUiTheme.StyleTableText(titleLabel, muted: true);
        container.AddChild(titleLabel);

        if (!string.IsNullOrEmpty(description))
        {
            var descLabel = new Label
            {
                Text = description,
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleTableText(descLabel, muted: true);
            descLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
            container.AddChild(descLabel);
        }

        return container;
    }

    // ── Player Row ───────────────────────────────────────────────────────────

    /// <summary>
    /// Futbolcu listesi satırı: mevki badge + isim + yaş + sağda metrik.
    /// ItemList yerine VBoxContainer içinde özel row olarak kullanılır.
    /// Seçili durum: sol kenarda yeşil border şeridi.
    /// Zebra stripe: isAlternate=true ile hafif alternatif arka plan.
    /// </summary>
    public static PanelContainer PlayerRow(
        string position,
        string name,
        int age,
        string metricLabel,
        Color? metricColor = null,
        string? statusText = null,
        Color? statusColor = null,
        bool isSelected = false,
        bool isAlternate = false)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel",
            CareerUiTheme.TableRowPanel(isSelected: isSelected, isAlternate: isAlternate));

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", CareerUiTheme.SpaceS);
        panel.AddChild(row);

        row.AddChild(PositionBadge(position));

        var nameCol = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        nameCol.AddThemeConstantOverride("separation", 1);
        row.AddChild(nameCol);

        var nameLabel = new Label
        {
            Text = name,
            ClipText = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleTableText(nameLabel);
        nameCol.AddChild(nameLabel);

        var ageLabel = new Label { Text = $"{age} yaş" };
        CareerUiTheme.StyleTableText(ageLabel, muted: true);
        ageLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        nameCol.AddChild(ageLabel);

        if (statusText is not null)
        {
            var badge = StatusBadge(statusText, statusColor);
            badge.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            row.AddChild(badge);
        }

        var metricNode = new Label
        {
            Text = metricLabel,
            HorizontalAlignment = HorizontalAlignment.Right,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        CareerUiTheme.StyleTableText(metricNode);
        metricNode.AddThemeColorOverride("font_color", metricColor ?? CareerUiTheme.Ink);
        row.AddChild(metricNode);

        return panel;
    }

    // ── Form Row ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Form satırı iskeleti: sol label + sağ input widget için HBoxContainer.
    /// OptionButton, SpinBox, LineEdit gibi widget'lar döndürülen container'a eklenir.
    /// </summary>
    public static HBoxContainer FormRow(string label, float labelWidthRatio = 0.42f)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", CareerUiTheme.SpaceM);

        var labelNode = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = labelWidthRatio,
            VerticalAlignment = VerticalAlignment.Center,
        };
        CareerUiTheme.StyleTableText(labelNode, muted: true);
        row.AddChild(labelNode);

        return row;
    }

    // ── Info Chip ────────────────────────────────────────────────────────────

    /// <summary>
    /// Compact bilgi chip: kısa metin + renkli pill arka plan.
    /// Top bar ve heading satırı için.
    /// </summary>
    public static PanelContainer InfoChip(string text, Color? bgColor = null)
    {
        var signalColor = bgColor ?? CareerUiTheme.Data;
        var panel = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(signalColor.R, signalColor.G, signalColor.B, 0.12f),
            BorderColor = new Color(signalColor.R, signalColor.G, signalColor.B, 0.42f),
            BorderWidthLeft = 1, BorderWidthTop = 1,
            BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = CareerUiTheme.RadiusPill,
            CornerRadiusTopRight = CareerUiTheme.RadiusPill,
            CornerRadiusBottomRight = CareerUiTheme.RadiusPill,
            CornerRadiusBottomLeft = CareerUiTheme.RadiusPill,
            ContentMarginLeft = CareerUiTheme.SpaceM,
            ContentMarginRight = CareerUiTheme.SpaceM,
            ContentMarginTop = CareerUiTheme.SpaceXs,
            ContentMarginBottom = CareerUiTheme.SpaceXs,
        });

        var label = new Label { Text = text };
        CareerUiTheme.StylePositionTag(label, signalColor);
        panel.AddChild(label);

        return panel;
    }

    // ── Alert Banner ─────────────────────────────────────────────────────────

    /// <summary>
    /// Tam genişlik uyarı/bilgi banner'ı.
    /// Önemli gelişme, kritik karar, engel bildirimi için.
    /// </summary>
    public static PanelContainer AlertBanner(string message, AlertKind kind = AlertKind.Neutral)
    {
        var signalColor = kind switch
        {
            AlertKind.Positive => CareerUiTheme.Action,
            AlertKind.Negative => CareerUiTheme.DangerSoft,
            AlertKind.Warning => CareerUiTheme.ColorWarning,
            _ => CareerUiTheme.Data,
        };

        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.AlertPanel(signalColor));

        var label = new Label
        {
            Text = message,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleBody(label);
        label.AddThemeColorOverride("font_color", signalColor);
        panel.AddChild(label);

        return panel;
    }

    // ── Metric Grid ──────────────────────────────────────────────────────────

    /// <summary>
    /// Yatay metrik grid: 2–4 StatBlock yan yana.
    /// Maç sonrası özeti, performans paneli için.
    /// </summary>
    public static HBoxContainer MetricGrid(params (string label, string value, Color? color)[] metrics)
    {
        var grid = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("separation", CareerUiTheme.SpaceS);

        foreach (var (label, value, color) in metrics)
        {
            var block = StatBlock(label, value, valueColor: color);
            block.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            grid.AddChild(block);
        }

        return grid;
    }

    // ── Progress Bar ─────────────────────────────────────────────────────────

    /// <summary>
    /// Basit yatay progress bar.
    /// Form, kondisyon, gelişim değeri görselleştirmesi için.
    /// </summary>
    /// <param name="value">0.0 – 1.0 arası değer.</param>
    /// <param name="color">Bar rengi — null ise Action (yeşil).</param>
    /// <param name="height">Bar yüksekliği piksel.</param>
    public static Control ProgressBar(float value, Color? color = null, int height = 6)
    {
        var clampedValue = Mathf.Clamp(value, 0f, 1f);
        var barColor = color ?? CareerUiTheme.Action;
        var r = CareerUiTheme.RadiusPill;

        var bg = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        bg.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(CareerUiTheme.Stroke.R, CareerUiTheme.Stroke.G, CareerUiTheme.Stroke.B, 0.32f),
            CornerRadiusTopLeft = r, CornerRadiusTopRight = r,
            CornerRadiusBottomRight = r, CornerRadiusBottomLeft = r,
        });
        bg.CustomMinimumSize = new Vector2(0, height);

        if (clampedValue > 0f)
        {
            var fillParent = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            bg.AddChild(fillParent);

            var fill = new Panel
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = clampedValue,
            };
            fill.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = barColor,
                CornerRadiusTopLeft = r, CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r, CornerRadiusBottomLeft = r,
            });
            fillParent.AddChild(fill);

            if (clampedValue < 1f)
            {
                var remainder = new Control
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsStretchRatio = 1f - clampedValue,
                };
                fillParent.AddChild(remainder);
            }
        }

        return bg;
    }

    // ── Neon Card ────────────────────────────────────────────────────────────

    /// <summary>
    /// Referans tasarımın imzası: neon yeşil glow çerçeveli kart container.
    /// İçeriği child olarak döndürülen VBoxContainer'a ekleyin.
    /// </summary>
    public static (PanelContainer panel, VBoxContainer content) NeonCard(bool strongGlow = false)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow));

        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", CareerUiTheme.SpaceM);
        panel.AddChild(content);

        return (panel, content);
    }

    // ── Page Header ──────────────────────────────────────────────────────────

    /// <summary>
    /// Referans tasarımdaki sayfa başlığı: yeşil ikon kutusu + büyük beyaz başlık.
    /// icon parametresi unicode sembol (örn. "⚽", "★", "⚙") veya kısa metin olabilir.
    /// </summary>
    public static HBoxContainer PageHeader(string icon, string title, Control? rightWidget = null)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", CareerUiTheme.SpaceM);

        // Yeşil ikon kutucuğu
        var iconBox = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        iconBox.AddThemeStyleboxOverride("panel", CareerUiTheme.PageIconBoxPanel());
        var iconLabel = new Label
        {
            Text = icon,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        CareerUiTheme.StyleHeadline(iconLabel);
        iconLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(16));
        iconLabel.AddThemeColorOverride("font_color", new Color(0.02f, 0.08f, 0.04f, 1f));
        iconBox.AddChild(iconLabel);
        row.AddChild(iconBox);

        // Başlık metni
        var titleLabel = new Label
        {
            Text = title,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StylePageTitle(titleLabel);
        row.AddChild(titleLabel);

        if (rightWidget is not null)
        {
            rightWidget.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            row.AddChild(rightWidget);
        }

        return row;
    }

    // ── Filled Position Badge ────────────────────────────────────────────────

    /// <summary>
    /// Referans tasarımına uygun tam dolgu pozisyon rozeti.
    /// KL=yeşil, DEF=turuncu, MO=cyan, HÜC=magenta.
    /// </summary>
    public static PanelContainer FilledPositionBadge(string positionCode)
    {
        var color = CareerUiTheme.PositionGroupColor(positionCode);
        var panel = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.PositionBadgePanel(positionCode));

        var label = new Label
        {
            Text = positionCode,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.ApplyBodyFont(label);
        label.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        // Koyu metin rengi — tam dolgu arka plan üzerinde okunabilir
        label.AddThemeColorOverride("font_color", new Color(0.02f, 0.05f, 0.03f, 1f));
        panel.AddChild(label);

        return panel;
    }

    // ── Mini Stat Card ───────────────────────────────────────────────────────

    /// <summary>
    /// Ana sayfadaki 6'lı stat kutusu: ikon + başlık + değer.
    /// </summary>
    public static PanelContainer MiniStatCard(
        string icon,
        string title,
        string value,
        string? subtitle = null,
        Color? valueColor = null)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.MiniStatCardPanel());

        var col = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", CareerUiTheme.SpaceXs);
        panel.AddChild(col);

        // İkon + başlık satırı
        var topRow = new HBoxContainer();
        topRow.AddThemeConstantOverride("separation", CareerUiTheme.SpaceXs);
        col.AddChild(topRow);

        var iconLabel = new Label { Text = icon };
        iconLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(16));
        topRow.AddChild(iconLabel);

        var titleLabel = new Label
        {
            Text = title,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        CareerUiTheme.StyleTableText(titleLabel, muted: true);
        titleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        topRow.AddChild(titleLabel);

        // Değer
        var valueLabel = new Label { Text = value };
        CareerUiTheme.StyleStatValueMedium(valueLabel, valueColor ?? CareerUiTheme.Ink);
        valueLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(16));
        col.AddChild(valueLabel);

        if (subtitle is not null)
        {
            var subLabel = new Label { Text = subtitle };
            CareerUiTheme.StyleTableText(subLabel, muted: true);
            subLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
            col.AddChild(subLabel);
        }

        return panel;
    }

    // ── News Row ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Haberler sayfası satırı: thumbnail alanı + başlık + özet + zaman.
    /// </summary>
    public static PanelContainer NewsRow(
        string headline,
        string summary,
        string timeAgo,
        bool isFirst = false)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.ListRowPanel(isNext: isFirst));

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", CareerUiTheme.SpaceM);
        panel.AddChild(row);

        // Thumbnail placeholder
        var thumb = new Panel
        {
            CustomMinimumSize = new Vector2(64, 48),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        thumb.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.14f, 0.14f, 0.9f),
            BorderColor = new Color(0.149f, 0.898f, 0.588f, 0.25f),
            BorderWidthLeft = 1, BorderWidthTop = 1,
            BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6, CornerRadiusBottomLeft = 6,
        });
        row.AddChild(thumb);

        // Metin bloğu
        var textCol = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        textCol.AddThemeConstantOverride("separation", 2);
        row.AddChild(textCol);

        var headlineLabel = new Label
        {
            Text = headline,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleBody(headlineLabel);
        headlineLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        textCol.AddChild(headlineLabel);

        var summaryLabel = new Label
        {
            Text = summary,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleTableText(summaryLabel, muted: true);
        summaryLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        textCol.AddChild(summaryLabel);

        // Zaman damgası
        var timeLabel = new Label
        {
            Text = timeAgo,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        CareerUiTheme.StyleTableText(timeLabel, muted: true);
        timeLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        row.AddChild(timeLabel);

        return panel;
    }

    // ── Fixture Row ──────────────────────────────────────────────────────────

    /// <summary>
    /// Takvim fikstür satırı: gün + gün kodu + rakip ismi + lig/kupa etiketi.
    /// </summary>
    public static PanelContainer FixtureRow(
        string day,
        string dayCode,
        string opponentName,
        string competitionLabel,
        bool isNext = false)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.ListRowPanel(isNext: isNext));

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", CareerUiTheme.SpaceM);
        panel.AddChild(row);

        // Gün numarası
        var dayLabel = new Label
        {
            Text = day,
            CustomMinimumSize = new Vector2(36, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        CareerUiTheme.StyleHeadline(dayLabel);
        dayLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(22));
        if (isNext)
        {
            dayLabel.AddThemeColorOverride("font_color", CareerUiTheme.Action);
        }
        row.AddChild(dayLabel);

        // Gün kodu (Cts, Çar, vb.)
        var dayCodeLabel = new Label
        {
            Text = dayCode,
            VerticalAlignment = VerticalAlignment.Center,
        };
        CareerUiTheme.StyleTableText(dayCodeLabel, muted: true);
        dayCodeLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        row.AddChild(dayCodeLabel);

        // Dikey ayırıcı
        var sep = new ColorRect
        {
            Color = new Color(0.149f, 0.898f, 0.588f, 0.30f),
            CustomMinimumSize = new Vector2(1, 24),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        row.AddChild(sep);

        // Rakip ismi
        var opponentLabel = new Label
        {
            Text = opponentName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        CareerUiTheme.StyleBody(opponentLabel);
        opponentLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(14));
        row.AddChild(opponentLabel);

        // Lig/kupa etiketi
        var compLabel = new Label
        {
            Text = competitionLabel,
            VerticalAlignment = VerticalAlignment.Center,
        };
        CareerUiTheme.StyleTableText(compLabel, muted: true);
        compLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        compLabel.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
        row.AddChild(compLabel);

        return panel;
    }

    // ── Squad Player Row ─────────────────────────────────────────────────────

    /// <summary>
    /// Kadro sayfası oyuncu satırı: No + İsim + Pozisyon rozeti (dolgu) + Genel puan.
    /// </summary>
    public static PanelContainer SquadPlayerRow(
        int number,
        string name,
        string position,
        int overall,
        bool isAlternate = false)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = isAlternate
                ? new Color(0.02f, 0.08f, 0.09f, 0.60f)
                : new Color(0.012f, 0.055f, 0.059f, 0.40f),
            BorderColor = new Color(0.149f, 0.898f, 0.588f, 0.08f),
            BorderWidthLeft = 0,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 1,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
        });

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", CareerUiTheme.SpaceM);
        panel.AddChild(row);

        // Forma numarası
        var numLabel = new Label
        {
            Text = number.ToString(),
            CustomMinimumSize = new Vector2(32, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        CareerUiTheme.StyleBody(numLabel, muted: true);
        numLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(14));
        row.AddChild(numLabel);

        // Avatar placeholder (küçük daire)
        var avatar = new Panel { CustomMinimumSize = new Vector2(32, 32), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        avatar.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.18f, 0.18f, 0.80f),
            CornerRadiusTopLeft = 999, CornerRadiusTopRight = 999,
            CornerRadiusBottomRight = 999, CornerRadiusBottomLeft = 999,
            BorderColor = new Color(0.149f, 0.898f, 0.588f, 0.30f),
            BorderWidthLeft = 1, BorderWidthTop = 1,
            BorderWidthRight = 1, BorderWidthBottom = 1,
        });
        row.AddChild(avatar);

        // İsim
        var nameLabel = new Label
        {
            Text = name,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
        };
        CareerUiTheme.StyleBody(nameLabel);
        nameLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(14));
        row.AddChild(nameLabel);

        // Pozisyon rozeti (tam dolgu)
        row.AddChild(FilledPositionBadge(position));

        // Genel puan
        var ovrLabel = new Label
        {
            Text = overall.ToString(),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(36, 0),
        };
        CareerUiTheme.StyleHeadline(ovrLabel);
        ovrLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(18));
        var ovrColor = CareerUiTheme.RatingColor(overall);
        ovrLabel.AddThemeColorOverride("font_color", ovrColor);
        row.AddChild(ovrLabel);

        return panel;
    }
}

/// <summary>Alert banner görsel kategorisi.</summary>
internal enum AlertKind
{
    Neutral,
    Positive,
    Negative,
    Warning,
}
