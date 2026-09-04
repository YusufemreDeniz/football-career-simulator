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

        var label = new Label { Text = title };
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
    /// Mevki badge: GK, CB, ST gibi pozisyon kısaltması.
    /// </summary>
    public static PanelContainer PositionBadge(string positionCode)
    {
        var panel = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.PositionTagPanel());

        var label = new Label { Text = positionCode };
        CareerUiTheme.StylePositionTag(label, CareerUiTheme.Data);
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
}

/// <summary>Alert banner görsel kategorisi.</summary>
internal enum AlertKind
{
    Neutral,
    Positive,
    Negative,
    Warning,
}
