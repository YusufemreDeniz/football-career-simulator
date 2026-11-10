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
    /// Boş durum göstergesi: vektörel ikon, başlık ve açıklama.
    /// ItemList veya kart içeriği yokken gösterilir.
    /// </summary>
    public static VBoxContainer EmptyState(
        string title,
        string description = "",
        string symbol = "radar")
    {
        var container = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        container.AddThemeConstantOverride("separation", CareerUiTheme.SpaceS);

        var iconWrap = new CenterContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        iconWrap.AddChild(new BroadcastVectorIcon(
            symbol,
            new Color(CareerUiTheme.InkMuted.R, CareerUiTheme.InkMuted.G, CareerUiTheme.InkMuted.B, 0.45f),
            28f));
        container.AddChild(iconWrap);

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
    /// Broadcast Stadium Noir sayfa başlığı: özel _Draw() vektörel ikon kutusu + büyük beyaz başlık.
    /// </summary>
    public static HBoxContainer PageHeader(string icon, string title, Control? rightWidget = null)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", CareerUiTheme.SpaceS);

        // Kompakt vektörel ikon rozeti
        var iconBox = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        iconBox.AddThemeStyleboxOverride("panel", CareerUiTheme.PageIconBoxPanel());
        var iconCenter = new CenterContainer
        {
            CustomMinimumSize = new Vector2(18, 18),
        };
        iconCenter.AddChild(new BroadcastVectorIcon(icon, CareerUiTheme.ActionBright, 16f));
        iconBox.AddChild(iconCenter);
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
    /// Ana sayfadaki stat kutusu: _Draw() vektör ikon + başlık + değer.
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

        var iconNode = new BroadcastVectorIcon(icon, valueColor ?? CareerUiTheme.Accent, 14f)
        {
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        topRow.AddChild(iconNode);

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

/// <summary>
/// Broadcast Stadium Noir tasarım sistemi için sıfır-emoji özel vektörel ikon/rozet çizici (_Draw()).
/// </summary>
internal sealed partial class BroadcastVectorIcon : Control
{
    private readonly string _kind;
    private readonly Color _color;
    private readonly float _size;

    public BroadcastVectorIcon(string? iconOrKey, Color color, float size = 18f)
    {
        _kind = ResolveKind(iconOrKey);
        _color = color;
        _size = size;
        CustomMinimumSize = new Vector2(size, size);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    private static string ResolveKind(string? raw)
    {
        var key = (raw ?? string.Empty).Trim().ToLowerInvariant();
        return key switch
        {
            "home" or "portal" or "today" or "bugun" => "home",
            "campus" or "club" or "kulup" or "pillars" or "building" => "campus",
            "pitch" or "prep" or "tactic" or "saha" or "field" => "pitch",
            "radar" or "scout" or "transfer" or "globe" => "radar",
            "trophy" or "cup" or "world" or "lig" or "award" => "trophy",
            "user" or "player" or "dossier" or "manager" or "profile" => "user",
            "bolt" or "energy" or "shock" or "flash" or "training" => "bolt",
            "flame" or "fire" or "hot" or "form" or "morale" => "flame",
            "medical" or "health" or "injury" or "rehab" or "cross" => "medical",
            "search" or "find" or "inspect" or "lens" => "search",
            "handshake" or "promise" or "deal" or "contract" or "talk" => "handshake",
            "ball" or "goal" or "match" or "football" => "ball",
            "star" or "elite" or "rating" or "wonderkid" => "star",
            "coin" or "money" or "budget" or "finance" or "wage" => "coin",
            "target" or "reticle" or "focus" or "shot" => "target",
            "cards" or "deck" or "hand" => "cards",
            "sliders" or "settings" or "file" or "tune" => "sliders",
            "book" or "pedia" or "guide" or "rules" => "book",
            "calendar" or "date" or "schedule" => "calendar",
            "shield" or "defense" or "crest" or "lock" => "shield",
            "warning" or "alert" or "risk" => "warning",
            "check" or "ok" or "done" or "approved" => "check",
            _ => "shield",
        };
    }

    public override void _Draw()
    {
        var s = Size;
        if (s.X <= 2f || s.Y <= 2f)
        {
            s = new Vector2(_size, _size);
        }

        var c = s * 0.5f;
        var r = Mathf.Min(s.X, s.Y) * 0.42f;
        var stroke = Mathf.Max(1.6f, r * 0.22f);
        var softFill = new Color(_color.R, _color.G, _color.B, 0.20f);

        switch (_kind)
        {
            case "home":
            {
                var roof = new Vector2[]
                {
                    new(c.X, c.Y - r * 0.95f),
                    new(c.X + r * 0.95f, c.Y - r * 0.10f),
                    new(c.X - r * 0.95f, c.Y - r * 0.10f),
                };
                DrawColoredPolygon(roof, softFill);
                DrawPolyline(new Vector2[] { roof[2], roof[0], roof[1] }, _color, stroke, true);
                var body = new Rect2(c.X - r * 0.65f, c.Y - r * 0.10f, r * 1.30f, r * 0.95f);
                DrawRect(body, softFill, true);
                DrawRect(body, _color, false, stroke);
                break;
            }
            case "campus":
            {
                var pediment = new Vector2[]
                {
                    new(c.X, c.Y - r * 0.95f),
                    new(c.X + r * 0.95f, c.Y - r * 0.35f),
                    new(c.X - r * 0.95f, c.Y - r * 0.35f),
                };
                DrawColoredPolygon(pediment, softFill);
                DrawPolyline(new Vector2[] { pediment[2], pediment[0], pediment[1], pediment[2] }, _color, stroke, true);
                for (var i = -1; i <= 1; i++)
                {
                    var px = c.X + i * r * 0.52f;
                    DrawLine(new Vector2(px, c.Y - r * 0.30f), new Vector2(px, c.Y + r * 0.65f), _color, stroke, true);
                }
                DrawLine(new Vector2(c.X - r * 0.95f, c.Y + r * 0.75f), new Vector2(c.X + r * 0.95f, c.Y + r * 0.75f), _color, stroke * 1.2f, true);
                break;
            }
            case "pitch":
            {
                var poly = new Vector2[]
                {
                    new(c.X - r * 0.70f, c.Y - r * 0.80f),
                    new(c.X + r * 0.70f, c.Y - r * 0.80f),
                    new(c.X + r * 0.95f, c.Y + r * 0.80f),
                    new(c.X - r * 0.95f, c.Y + r * 0.80f),
                };
                DrawColoredPolygon(poly, softFill);
                DrawPolyline(new Vector2[] { poly[0], poly[1], poly[2], poly[3], poly[0] }, _color, stroke, true);
                DrawLine(new Vector2(c.X - r * 0.82f, c.Y), new Vector2(c.X + r * 0.82f, c.Y), _color, stroke * 0.8f, true);
                DrawArc(c, r * 0.28f, 0f, Mathf.Tau, 16, _color, stroke * 0.8f, true);
                break;
            }
            case "radar":
            {
                DrawCircle(c, r * 0.90f, softFill);
                DrawArc(c, r * 0.90f, 0f, Mathf.Tau, 24, _color, stroke, true);
                DrawArc(c, r * 0.48f, 0f, Mathf.Tau, 20, new Color(_color.R, _color.G, _color.B, 0.65f), stroke * 0.8f, true);
                DrawLine(c, new Vector2(c.X + r * 0.65f, c.Y - r * 0.60f), _color, stroke, true);
                DrawCircle(new Vector2(c.X + r * 0.38f, c.Y - r * 0.34f), stroke * 1.1f, _color);
                break;
            }
            case "trophy":
            {
                var cup = new Vector2[]
                {
                    new(c.X - r * 0.65f, c.Y - r * 0.85f),
                    new(c.X + r * 0.65f, c.Y - r * 0.85f),
                    new(c.X + r * 0.42f, c.Y + r * 0.05f),
                    new(c.X - r * 0.42f, c.Y + r * 0.05f),
                };
                DrawColoredPolygon(cup, softFill);
                DrawPolyline(new Vector2[] { cup[0], cup[1], cup[2], cup[3], cup[0] }, _color, stroke, true);
                DrawLine(new Vector2(c.X, c.Y + r * 0.05f), new Vector2(c.X, c.Y + r * 0.65f), _color, stroke, true);
                DrawLine(new Vector2(c.X - r * 0.50f, c.Y + r * 0.70f), new Vector2(c.X + r * 0.50f, c.Y + r * 0.70f), _color, stroke * 1.2f, true);
                break;
            }
            case "user":
            {
                DrawCircle(new Vector2(c.X, c.Y - r * 0.35f), r * 0.36f, softFill);
                DrawArc(new Vector2(c.X, c.Y - r * 0.35f), r * 0.36f, 0f, Mathf.Tau, 18, _color, stroke, true);
                DrawArc(new Vector2(c.X, c.Y + r * 0.85f), r * 0.72f, Mathf.Pi * 1.12f, Mathf.Pi * 1.88f, 18, _color, stroke, true);
                break;
            }
            case "bolt":
            {
                var bolt = new Vector2[]
                {
                    new(c.X + r * 0.15f, c.Y - r * 0.95f),
                    new(c.X - r * 0.55f, c.Y + r * 0.08f),
                    new(c.X + r * 0.05f, c.Y + r * 0.08f),
                    new(c.X - r * 0.18f, c.Y + r * 0.95f),
                    new(c.X + r * 0.58f, c.Y - r * 0.10f),
                    new(c.X - r * 0.02f, c.Y - r * 0.10f),
                };
                DrawColoredPolygon(bolt, _color);
                break;
            }
            case "flame":
            {
                var flame = new Vector2[]
                {
                    new(c.X, c.Y - r * 0.95f),
                    new(c.X + r * 0.65f, c.Y - r * 0.10f),
                    new(c.X + r * 0.48f, c.Y + r * 0.70f),
                    new(c.X, c.Y + r * 0.92f),
                    new(c.X - r * 0.48f, c.Y + r * 0.70f),
                    new(c.X - r * 0.65f, c.Y - r * 0.10f),
                };
                DrawColoredPolygon(flame, softFill);
                DrawPolyline(new Vector2[] { flame[0], flame[1], flame[2], flame[3], flame[4], flame[5], flame[0] }, _color, stroke, true);
                DrawCircle(new Vector2(c.X, c.Y + r * 0.32f), r * 0.26f, _color);
                break;
            }
            case "medical":
            {
                var w = r * 0.36f;
                var l = r * 0.85f;
                DrawRect(new Rect2(c.X - w, c.Y - l, w * 2f, l * 2f), _color, true);
                DrawRect(new Rect2(c.X - l, c.Y - w, l * 2f, w * 2f), _color, true);
                break;
            }
            case "search":
            {
                var sc = c - new Vector2(r * 0.16f, r * 0.16f);
                DrawCircle(sc, r * 0.55f, softFill);
                DrawArc(sc, r * 0.55f, 0f, Mathf.Tau, 20, _color, stroke, true);
                DrawLine(sc + new Vector2(r * 0.40f, r * 0.40f), c + new Vector2(r * 0.82f, r * 0.82f), _color, stroke * 1.25f, true);
                break;
            }
            case "handshake":
            {
                var leftLink = new Rect2(c.X - r * 0.85f, c.Y - r * 0.38f, r * 1.05f, r * 0.76f);
                var rightLink = new Rect2(c.X - r * 0.20f, c.Y - r * 0.38f, r * 1.05f, r * 0.76f);
                DrawRect(leftLink, softFill, true);
                DrawRect(leftLink, _color, false, stroke);
                DrawRect(rightLink, _color, false, stroke);
                break;
            }
            case "ball":
            {
                DrawCircle(c, r * 0.88f, softFill);
                DrawArc(c, r * 0.88f, 0f, Mathf.Tau, 24, _color, stroke, true);
                var pent = new Vector2[5];
                for (var i = 0; i < 5; i++)
                {
                    var a = -Mathf.Pi * 0.5f + i * Mathf.Tau / 5f;
                    pent[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (r * 0.42f);
                }
                DrawColoredPolygon(pent, _color);
                break;
            }
            case "star":
            {
                var pts = new Vector2[10];
                for (var i = 0; i < 10; i++)
                {
                    var rad = i % 2 == 0 ? r * 0.92f : r * 0.40f;
                    var a = -Mathf.Pi * 0.5f + i * Mathf.Tau / 10f;
                    pts[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                }
                DrawColoredPolygon(pts, _color);
                break;
            }
            case "coin":
            {
                var diamond = new Vector2[]
                {
                    new(c.X, c.Y - r * 0.90f),
                    new(c.X + r * 0.90f, c.Y),
                    new(c.X, c.Y + r * 0.90f),
                    new(c.X - r * 0.90f, c.Y),
                };
                DrawColoredPolygon(diamond, softFill);
                DrawPolyline(new Vector2[] { diamond[0], diamond[1], diamond[2], diamond[3], diamond[0] }, _color, stroke, true);
                DrawCircle(c, r * 0.28f, _color);
                break;
            }
            case "target":
            {
                DrawArc(c, r * 0.85f, 0f, Mathf.Tau, 24, _color, stroke, true);
                DrawCircle(c, r * 0.30f, _color);
                DrawLine(new Vector2(c.X - r, c.Y), new Vector2(c.X - r * 0.45f, c.Y), _color, stroke, true);
                DrawLine(new Vector2(c.X + r * 0.45f, c.Y), new Vector2(c.X + r, c.Y), _color, stroke, true);
                DrawLine(new Vector2(c.X, c.Y - r), new Vector2(c.X, c.Y - r * 0.45f), _color, stroke, true);
                DrawLine(new Vector2(c.X, c.Y + r * 0.45f), new Vector2(c.X, c.Y + r), _color, stroke, true);
                break;
            }
            case "cards":
            {
                var cardBack = new Rect2(c.X - r * 0.72f, c.Y - r * 0.82f, r * 0.95f, r * 1.35f);
                var cardFront = new Rect2(c.X - r * 0.22f, c.Y - r * 0.55f, r * 0.95f, r * 1.35f);
                DrawRect(cardBack, softFill, true);
                DrawRect(cardBack, new Color(_color.R, _color.G, _color.B, 0.55f), false, stroke * 0.85f);
                DrawRect(cardFront, CareerUiTheme.BackgroundDeep, true);
                DrawRect(cardFront, _color, false, stroke);
                break;
            }
            case "sliders":
            {
                for (var i = -1; i <= 1; i++)
                {
                    var y = c.Y + i * r * 0.55f;
                    DrawLine(new Vector2(c.X - r * 0.85f, y), new Vector2(c.X + r * 0.85f, y), new Color(_color.R, _color.G, _color.B, 0.55f), stroke * 0.8f, true);
                    var kx = c.X + (i == 0 ? r * 0.35f : -r * 0.30f);
                    DrawCircle(new Vector2(kx, y), stroke * 1.35f, _color);
                }
                break;
            }
            case "book":
            {
                var rect = new Rect2(c.X - r * 0.82f, c.Y - r * 0.72f, r * 1.64f, r * 1.44f);
                DrawRect(rect, softFill, true);
                DrawRect(rect, _color, false, stroke);
                DrawLine(new Vector2(c.X, rect.Position.Y), new Vector2(c.X, rect.End.Y), _color, stroke, true);
                break;
            }
            case "calendar":
            {
                var rect = new Rect2(c.X - r * 0.82f, c.Y - r * 0.68f, r * 1.64f, r * 1.48f);
                DrawRect(rect, softFill, true);
                DrawRect(rect, _color, false, stroke);
                DrawLine(new Vector2(rect.Position.X, c.Y - r * 0.22f), new Vector2(rect.End.X, c.Y - r * 0.22f), _color, stroke, true);
                break;
            }
            case "warning":
            {
                var tri = new Vector2[]
                {
                    new(c.X, c.Y - r * 0.90f),
                    new(c.X + r * 0.90f, c.Y + r * 0.82f),
                    new(c.X - r * 0.90f, c.Y + r * 0.82f),
                };
                DrawColoredPolygon(tri, softFill);
                DrawPolyline(new Vector2[] { tri[0], tri[1], tri[2], tri[0] }, _color, stroke, true);
                DrawLine(new Vector2(c.X, c.Y - r * 0.30f), new Vector2(c.X, c.Y + r * 0.25f), _color, stroke * 1.2f, true);
                DrawCircle(new Vector2(c.X, c.Y + r * 0.52f), stroke * 0.8f, _color);
                break;
            }
            case "check":
            {
                var pts = new Vector2[]
                {
                    new(c.X - r * 0.68f, c.Y + r * 0.05f),
                    new(c.X - r * 0.15f, c.Y + r * 0.58f),
                    new(c.X + r * 0.75f, c.Y - r * 0.52f),
                };
                DrawPolyline(pts, _color, stroke * 1.35f, true);
                break;
            }
            default: // shield / crest
            {
                var shield = new Vector2[]
                {
                    new(c.X, c.Y - r * 0.92f),
                    new(c.X + r * 0.80f, c.Y - r * 0.55f),
                    new(c.X + r * 0.65f, c.Y + r * 0.30f),
                    new(c.X, c.Y + r * 0.92f),
                    new(c.X - r * 0.65f, c.Y + r * 0.30f),
                    new(c.X - r * 0.80f, c.Y - r * 0.55f),
                };
                DrawColoredPolygon(shield, softFill);
                DrawPolyline(new Vector2[] { shield[0], shield[1], shield[2], shield[3], shield[4], shield[5], shield[0] }, _color, stroke, true);
                break;
            }
        }
    }
}
