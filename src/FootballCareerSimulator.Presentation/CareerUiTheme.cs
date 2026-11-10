using Godot;
using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Mobil kariyer yüzeyinin ortak görsel dili: gece stadyumu + kulüp operasyon odası.
/// Renk adları bütün Presentation ekranlarında aynı anlamı taşır.
/// </summary>
internal static class CareerUiTheme
{
    public static readonly Color BackgroundDeep = new(0.0275f, 0.0431f, 0.0706f, 1f); // #070B12
    public static readonly Color Background = new(0.0392f, 0.0627f, 0.1059f, 1f);     // #0A101B
    public static readonly Color BackgroundNavy = new(0.0510f, 0.0824f, 0.1333f, 1f); // #0D1522
    public static readonly Color Surface = new(0.0745f, 0.1176f, 0.1882f, 0.85f);     // #131E30 (%85 opaklık)
    public static readonly Color SurfaceRaised = new(0.0745f, 0.1176f, 0.1882f, 0.90f);
    public static readonly Color SurfaceSoft = new(0.0745f, 0.1176f, 0.1882f, 0.85f);
    public static readonly Color SpecularBorder = new(0.1647f, 0.2627f, 0.3961f, 1f); // #2A4365 parlak üst çizgi
    public static readonly Color Stroke = new(0.1647f, 0.2627f, 0.3961f, 0.85f);

    // Existing callers treat Ink as foreground, Accent as prestige and Action as primary CTA.
    private static GameExperiencePreferences _preferences = GameExperiencePreferences.Default;
    public static GameExperiencePreferences CurrentPreferences => _preferences;

    public static Color Ink => _preferences.HighContrast
        ? Colors.White
        : new Color(0.953f, 0.973f, 0.996f, 1f);

    public static Color InkMuted => _preferences.HighContrast
        ? new Color(0.79f, 0.86f, 0.94f, 1f)
        : new Color(0.588f, 0.682f, 0.808f, 1f);
    public static readonly Color Accent = new(1.000f, 0.7216f, 0.000f, 1f);       // #FFB800 Yayın Altını
    public static readonly Color Action = new(0.000f, 0.9608f, 0.6078f, 1f);      // #00F59B Neon Çim Yeşili
    public static readonly Color ActionBright = new(0.000f, 0.9608f, 0.6078f, 1f);// #00F59B Neon Çim Yeşili
    public static readonly Color ActionHover = new(0.200f, 0.985f, 0.695f, 1f);
    public static readonly Color DangerSoft = new(0.973f, 0.357f, 0.400f, 1f);
    public static readonly Color Data = new(0.220f, 0.741f, 0.973f, 1f);

    // ── Semantic Color Tokens ─────────────────────────────────────────────────
    // Backward-compatible alias layer. Original names remain unchanged.

    /// <summary>En derin arka plan — modal/overlay arkası.</summary>
    public static Color BgPrimary => BackgroundDeep;
    /// <summary>Standart sayfa zemini.</summary>
    public static Color BgSecondary => Background;
    /// <summary>Navy varyant — header/sidebar arka planı.</summary>
    public static Color BgTertiary => BackgroundNavy;
    /// <summary>Varsayılan yüzey paneli.</summary>
    public static Color SurfaceDefault => Surface;
    /// <summary>Yükseltilmiş yüzey — hero, modal.</summary>
    public static Color SurfaceElevated => SurfaceRaised;
    /// <summary>Hafif yüzey — separator, subtle bg.</summary>
    public static Color SurfaceMuted => SurfaceSoft;
    /// <summary>Standart border.</summary>
    public static Color BorderDefault => Stroke;
    /// <summary>İnce section separator.</summary>
    public static Color BorderSubtle => new(Stroke.R, Stroke.G, Stroke.B, 0.32f);
    /// <summary>Birincil metin.</summary>
    public static Color TextPrimary => Ink;
    /// <summary>İkincil metin.</summary>
    public static Color TextSecondary => InkMuted;
    /// <summary>Üçüncül metin — disabled, placeholder.</summary>
    public static Color TextMuted => new(InkMuted.R, InkMuted.G, InkMuted.B, 0.55f);
    /// <summary>Pozitif/başarı rengi.</summary>
    public static Color ColorPositive => Action;
    /// <summary>Bright pozitif — hover, vurgu.</summary>
    public static Color ColorPositiveBright => ActionBright;
    /// <summary>Negatif/hata — yaralanma, kayıp.</summary>
    public static Color ColorNegative => DangerSoft;
    /// <summary>Uyarı — sarı kart, yüksek yük.</summary>
    public static readonly Color ColorWarning = new(0.94f, 0.63f, 0.25f, 1f);
    /// <summary>Kritik — kırmızı kart, kovulma riski.</summary>
    public static readonly Color ColorCritical = new(0.91f, 0.25f, 0.25f, 1f);
    /// <summary>Veri/istatistik rengi.</summary>
    public static Color ColorData => Data;
    /// <summary>Seçili durum.</summary>
    public static Color ColorSelected => Action;
    /// <summary>Hover yüzeyi.</summary>
    public static Color ColorHover => new(Action.R, Action.G, Action.B, 0.12f);
    /// <summary>Devre dışı renk.</summary>
    public static Color ColorDisabled => new(InkMuted.R, InkMuted.G, InkMuted.B, 0.38f);

    // ── Spacing Scale (4-based) ───────────────────────────────────────────────

    public const int SpaceXs = 4;
    public const int SpaceS = 8;
    public const int SpaceM = 12;
    public const int SpaceL = 16;
    public const int SpaceXl = 24;
    public const int SpaceXxl = 32;
    public const int SpaceXxxl = 48;

    public const int SeparationSection = 16;
    public const int SeparationCard = 10;
    public const int SeparationAction = 8;
    public const int SeparationNav = 4;
    public const int SeparationTight = 6;

    // ── Corner Radius ────────────────────────────────────────────────────────

    public const int RadiusSmall = 6;
    public const int RadiusMedium = 8;
    public const int RadiusLarge = 10;
    public const int RadiusXl = 12;
    public const int RadiusPill = 999;
    public const int TouchTargetMin = 32;

    // Legacy spacing aliases (kept for any existing code)
    public const int SpacingXs = SpaceXs;
    public const int SpacingSm = SpaceS;
    public const int SpacingMd = SpaceL;
    public const int SpacingLg = SpaceXl;
    public const int SpacingXl = SpaceXxl;

    private static FontFile? _display;
    private static FontFile? _body;
    private static FontVariation? _displayBold;
    private static FontVariation? _eyebrowFont;
    private static FontVariation? _tabularFont;
    private static bool _loaded;

    public static bool ReducedMotion => _preferences.ReducedMotion;

    public static void Configure(GameExperiencePreferences preferences) =>
        _preferences = (preferences ?? GameExperiencePreferences.Default).Normalize();

    /// <summary>
    /// Okunabilirlik tabanı: rozetler ve tablo başlıkları için 11px taban korunur.
    /// </summary>
    public const int MinimumReadableFontSize = 11;

    public static int FontSize(int baseSize) =>
        _preferences.ScaleFont(Math.Max(baseSize, MinimumReadableFontSize));

    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _display = TryLoadFont("res://fonts/Syne-Variable.ttf");
        _body = TryLoadFont("res://fonts/Outfit-Variable.ttf");

        if (_display is not null)
        {
            var variation = new FontVariation { BaseFont = _display };
            variation.VariationEmbolden = 0.22f;
            variation.SpacingGlyph = 0;
            _displayBold = variation;
        }

        if (_body is not null)
        {
            var eyebrowVar = new FontVariation { BaseFont = _body };
            eyebrowVar.VariationEmbolden = 0.18f;
            eyebrowVar.SpacingGlyph = 1;
            _eyebrowFont = eyebrowVar;

            var tabularVar = new FontVariation { BaseFont = _body };
            tabularVar.VariationEmbolden = 0.10f;
            tabularVar.SpacingGlyph = 0;
            _tabularFont = tabularVar;
        }

        _loaded = true;
    }

    /// <summary>
    /// Önce import edilmiş FontFile; yoksa veya bozuksa doğrudan TTF (variable font dahil).
    /// </summary>
    private static FontFile? TryLoadFont(string path)
    {
        if (ResourceLoader.Exists(path))
        {
            var imported = GD.Load<FontFile>(path);
            if (imported is not null)
            {
                return imported;
            }
        }

        var dynamic = new FontFile();
        if (dynamic.LoadDynamicFont(path) == Error.Ok && dynamic.Data.Length > 0)
        {
            return dynamic;
        }

        GD.PushWarning($"CareerUiTheme: font yüklenemedi ({path}); tema varsayılanına düşülüyor.");
        return null;
    }

    public static void StyleBrand(Label label)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(36));
        label.AddThemeColorOverride("font_color", Ink);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.42f));
        label.AddThemeConstantOverride("shadow_offset_x", 0);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
    }

    public static void StyleHeadline(Label label)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(22));
        label.AddThemeColorOverride("font_color", Ink);
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.03f, 0.06f, 0.72f));
        label.AddThemeConstantOverride("outline_size", 1);
    }

    public static void StyleSection(Label label)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(14));
        label.AddThemeColorOverride("font_color", Accent);
    }

    public static void StyleEyebrow(Label label, Color? color = null)
    {
        ApplyEyebrowFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(12));
        label.AddThemeColorOverride("font_color", color ?? Accent);
    }

    public static void StyleBody(Label label, bool muted = false)
    {
        ApplyBodyFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(13));
        label.AddThemeColorOverride("font_color", muted ? InkMuted : Ink);
    }

    public static void StylePrimaryButton(Button button)
    {
        ApplyBodyFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(13));
        button.AddThemeColorOverride("font_color", BackgroundDeep);
        button.AddThemeColorOverride("font_hover_color", BackgroundDeep);
        button.AddThemeColorOverride("font_pressed_color", BackgroundDeep);
        button.AddThemeColorOverride("font_focus_color", BackgroundDeep);
        button.AddThemeColorOverride("font_disabled_color", new Color(InkMuted.R, InkMuted.G, InkMuted.B, 0.58f));
        button.AddThemeStyleboxOverride("normal", SolidButton(Action));
        button.AddThemeStyleboxOverride("hover", SolidButton(ActionHover));
        button.AddThemeStyleboxOverride("pressed", SolidButton(new Color(0.08f, 0.72f, 0.38f, 1f)));
        button.AddThemeStyleboxOverride("focus", FocusRing(Action));
        button.AddThemeStyleboxOverride("disabled", SolidButton(new Color(0.08f, 0.14f, 0.22f, 0.92f)));
        button.CustomMinimumSize = new Vector2(0, 32);
        AttachSpringPressJuice(button);
    }

    public static void StyleSecondaryButton(Button button)
    {
        ApplyBodyFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(13));
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeColorOverride("font_hover_color", Ink);
        button.AddThemeColorOverride("font_pressed_color", Ink);
        button.AddThemeColorOverride("font_focus_color", Ink);
        button.AddThemeColorOverride("font_disabled_color", new Color(InkMuted.R, InkMuted.G, InkMuted.B, 0.46f));
        button.AddThemeStyleboxOverride("normal", OutlineButton(Stroke));
        button.AddThemeStyleboxOverride("hover", OutlineButton(new Color(Data.R, Data.G, Data.B, 0.85f)));
        button.AddThemeStyleboxOverride("pressed", SolidButton(new Color(Data.R, Data.G, Data.B, 0.20f)));
        button.AddThemeStyleboxOverride("focus", FocusRing(Data));
        button.AddThemeStyleboxOverride("disabled", OutlineButton(new Color(Stroke.R, Stroke.G, Stroke.B, 0.34f)));
        button.CustomMinimumSize = new Vector2(0, 30);
        AttachSpringPressJuice(button);
    }

    public static void StyleTextInput(LineEdit input)
    {
        ApplyBodyFont(input);
        input.AddThemeFontSizeOverride("font_size", FontSize(13));
        input.AddThemeColorOverride("font_color", Ink);
        input.AddThemeColorOverride("font_placeholder_color", InkMuted);
        input.AddThemeStyleboxOverride("normal", InputField(Stroke));
        input.AddThemeStyleboxOverride("focus", InputField(Data));
        input.AddThemeStyleboxOverride("read_only", InputField(Stroke));
    }

    public static void StyleOptionSelector(OptionButton selector)
    {
        ApplyBodyFont(selector);
        selector.AddThemeFontSizeOverride("font_size", FontSize(13));
        selector.AddThemeColorOverride("font_color", Ink);
        selector.AddThemeColorOverride("font_hover_color", Ink);
        selector.AddThemeColorOverride("font_pressed_color", Ink);
        selector.AddThemeStyleboxOverride("normal", InputField(Stroke));
        selector.AddThemeStyleboxOverride("hover", InputField(Data));
        selector.AddThemeStyleboxOverride("pressed", InputField(Action));
        selector.AddThemeStyleboxOverride("focus", FocusRing(Data));
    }

    public static void StyleNavButton(Button button, bool selected)
    {
        ApplyDisplayFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(14));
        button.AddThemeColorOverride("font_color", selected ? BackgroundDeep : Ink);
        button.AddThemeColorOverride("font_hover_color", selected ? BackgroundDeep : Ink);
        button.AddThemeColorOverride("font_pressed_color", selected ? BackgroundDeep : Ink);
        if (selected)
        {
            button.AddThemeStyleboxOverride("normal", SolidButton(Action));
            button.AddThemeStyleboxOverride("hover", SolidButton(ActionHover));
            button.AddThemeStyleboxOverride("pressed", SolidButton(Action));
        }
        else
        {
            button.AddThemeStyleboxOverride("normal", NavButtonSurface());
            button.AddThemeStyleboxOverride("hover", OutlineButton(new Color(Data.R, Data.G, Data.B, 0.6f)));
            button.AddThemeStyleboxOverride("pressed", SolidButton(new Color(Data.R, Data.G, Data.B, 0.14f)));
        }

        button.AddThemeStyleboxOverride("focus", FocusRing(Data));
        button.CustomMinimumSize = new Vector2(0, 34);
    }

    public static void StyleList(ItemList list)
    {
        ApplyBodyFont(list);
        list.AddThemeFontSizeOverride("font_size", FontSize(13));
        list.AddThemeColorOverride("font_color", Ink);
        list.AddThemeColorOverride("font_hovered_color", ActionHover);
        list.AddThemeColorOverride("font_selected_color", BackgroundDeep);
        list.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.72f));
        list.AddThemeConstantOverride("outline_size", 1);
        list.AddThemeConstantOverride("v_separation", 5);
        list.AddThemeStyleboxOverride("panel", SoftPanel());
        list.AddThemeStyleboxOverride("focus", FocusRing(Data));
        list.AddThemeStyleboxOverride("selected", SolidButton(Action));
        list.AddThemeStyleboxOverride("selected_focus", SolidButton(ActionHover));
        list.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        list.SameColumnWidth = false;
        TouchScrollRepeater.Attach(list);
    }

    public static void StylePitchChip(Button button, bool selected)
    {
        ApplyBodyFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(12));
        button.AddThemeColorOverride("font_color", Colors.Transparent);
        button.AddThemeColorOverride("font_hover_color", Colors.Transparent);
        button.AddThemeColorOverride("font_pressed_color", Colors.Transparent);
        button.AddThemeColorOverride("font_focus_color", Colors.Transparent);
        button.AddThemeColorOverride("font_disabled_color", Colors.Transparent);

        var normal = PitchCardSurface(selected: selected);
        var hover = PitchCardSurface(selected: selected, border: selected ? ActionBright : Stroke);
        var pressed = PitchCardSurface(selected: true);

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("disabled", normal);
        button.AddThemeStyleboxOverride("focus", FocusRing(Data));
        button.CustomMinimumSize = new Vector2(88, 56);
    }

    /// <summary>
    /// Mevki grubuna göre özel renk kodlaması:
    /// KL/GK -> Canlı Zümrüt Yeşili
    /// DEF (STP, SLB, SĞB, KB vb.) -> Sıcak Kehribar/Turuncu
    /// ORT (DOS, MO, OOS vb.) -> Elektrik/Okyanus Mavisi
    /// HÜC (SLK, SĞK, SNT, FOR vb.) -> Parlak Ahududu/Fuçya
    /// </summary>
    public static Color PositionGroupColor(string? positionCode)
    {
        var code = (positionCode ?? string.Empty).Trim().ToUpperInvariant();
        if (code is "KL" or "GK")
        {
            return new Color(0.13f, 0.78f, 0.42f, 1f);
        }
        if (code is "STP" or "SLB" or "SĞB" or "SGB" or "KB" or "CB" or "LB" or "RB" or "LWB" or "RWB" or "SW" or "DF" or "DEF")
        {
            return new Color(0.98f, 0.52f, 0.18f, 1f);
        }
        if (code is "DOS" or "MO" or "OOS" or "DM" or "CM" or "AM" or "CAM" or "CDM" or "OS")
        {
            return new Color(0.18f, 0.62f, 0.92f, 1f);
        }
        return new Color(0.91f, 0.28f, 0.46f, 1f);
    }

    /// <summary>Reyting kademe rengi.</summary>
    public static Color RatingColor(int rating) =>
        rating >= 85
            ? Accent
            : rating >= 75
                ? ActionBright
                : new Color(0.82f, 0.88f, 0.85f, 1f);

    /// <summary>Kondisyon seviyesi rengi.</summary>
    public static Color FitnessColor(int fitness) =>
        fitness >= 88
            ? ActionBright
            : fitness >= 75
                ? ColorWarning
                : ColorCritical;

    /// <summary>Mevki rozeti paneli — referans tasarıma uygun tam dolgu.</summary>
    public static StyleBoxFlat PositionBadgePanel(string? positionCode)
    {
        var color = PositionGroupColor(positionCode);
        return new StyleBoxFlat
        {
            BgColor = new Color(color.R, color.G, color.B, 0.90f),
            BorderColor = new Color(color.R, color.G, color.B, 1f),
            BorderWidthLeft = 0,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };
    }

    /// <summary>
    /// Broadcast Stadium Noir (Şampiyonlar Ligi TV yayını + EA FC / Top Drives kart estetiği)
    /// Cam (Glassmorphism) kart şablonu: #131E30 (%85 opaklık), 14px yuvarlatılmış köşe,
    /// üst kenarında 1px #2A4365 parlak çizgi (specular highlight) ve yumuşak alt gölge (shadow_size = 12).
    /// </summary>
    public static StyleBoxFlat NeonGlowPanel(bool strongGlow = false) =>
        new()
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = strongGlow ? 2 : 0,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.56f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };

    /// <summary>
    /// Sayfa başlık ikon kutusu — kompakt spor yayını rozeti.
    /// </summary>
    public static StyleBoxFlat PageIconBoxPanel() =>
        new()
        {
            BgColor = new Color(Action.R, Action.G, Action.B, 0.16f),
            BorderColor = new Color(ActionBright.R, ActionBright.G, ActionBright.B, 0.65f),
            BorderWidthLeft = 0,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };

    /// <summary>
    /// Mini stat kart — Glassmorphism bento kutusu.
    /// </summary>
    public static StyleBoxFlat MiniStatCardPanel() =>
        new()
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = 0,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            ShadowColor = new Color(0f, 0f, 0f, 0.50f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };

    /// <summary>
    /// Liste satırı — seçili olan zümrüt sol kenarlık, diğerleri koyu slate.
    /// </summary>
    public static StyleBoxFlat ListRowPanel(bool isNext = false) =>
        isNext
            ? new StyleBoxFlat
            {
                BgColor = new Color(Action.R, Action.G, Action.B, 0.12f),
                BorderColor = new Color(Action.R, Action.G, Action.B, 0.65f),
                BorderWidthLeft = 3,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomRight = 6,
                CornerRadiusBottomLeft = 6,
                ContentMarginLeft = 10,
                ContentMarginRight = 10,
                ContentMarginTop = 6,
                ContentMarginBottom = 6,
            }
            : new StyleBoxFlat
            {
                BgColor = new Color(0.047f, 0.082f, 0.149f, 0.78f),
                BorderColor = new Color(0.165f, 0.275f, 0.435f, 0.28f),
                BorderWidthLeft = 0,
                BorderWidthTop = 0,
                BorderWidthRight = 0,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomRight = 4,
                CornerRadiusBottomLeft = 4,
                ContentMarginLeft = 10,
                ContentMarginRight = 10,
                ContentMarginTop = 6,
                ContentMarginBottom = 6,
            };

    /// <summary>Rating rozeti — referans tasarıma uygun tam dolgu.</summary>
    public static StyleBoxFlat FilledRatingBadge(int rating)
    {
        var color = RatingColor(rating);
        return new StyleBoxFlat
        {
            BgColor = new Color(color.R, color.G, color.B, 0.18f),
            BorderColor = new Color(color.R, color.G, color.B, 0.70f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };
    }

    /// <summary>Reyting rozeti paneli.</summary>
    public static StyleBoxFlat RatingBadgePanel(int rating, int margin = 4, int radius = 4, int borderWidth = 1)
    {
        var color = RatingColor(rating);
        return new StyleBoxFlat
        {
            BgColor = new Color(BackgroundDeep.R, BackgroundDeep.G, BackgroundDeep.B, 0.92f),
            BorderColor = new Color(color.R, color.G, color.B, 0.75f),
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomRight = radius,
            CornerRadiusBottomLeft = radius,
            ContentMarginLeft = margin,
            ContentMarginRight = margin,
            ContentMarginTop = margin,
            ContentMarginBottom = margin,
        };
    }

    /// <summary>İnce ilerleme çubuğu arka plan stili.</summary>
    public static StyleBoxFlat ProgressBarBackground() =>
        new()
        {
            BgColor = new Color(0f, 0f, 0f, 0.45f),
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusBottomLeft = 3,
        };

    /// <summary>İnce ilerleme çubuğu dolgu stili.</summary>
    public static StyleBoxFlat ProgressBarFill(Color color) =>
        new()
        {
            BgColor = color,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusBottomLeft = 3,
        };


    public static void StyleTable(Tree table)
    {
        ApplyBodyFont(table);
        table.AddThemeFontSizeOverride("font_size", FontSize(13));
        table.AddThemeColorOverride("font_color", Ink);
        table.AddThemeColorOverride("font_selected_color", BackgroundDeep);
        table.AddThemeColorOverride("title_button_color", InkMuted);
        table.AddThemeColorOverride("title_button_hover_color", Ink);
        table.AddThemeStyleboxOverride("panel", SoftPanel());
        table.AddThemeStyleboxOverride("focus", FocusRing(Data));
        table.AddThemeStyleboxOverride("selected", SolidButton(Action));
        table.AddThemeStyleboxOverride("selected_focus", SolidButton(ActionHover));
        table.ScrollHorizontalEnabled = true;
        table.ScrollVerticalEnabled = true;
        TouchScrollRepeater.Attach(table);
    }

    public static StyleBoxFlat SoftPanel() =>
        PanelStyle(Surface, SpecularBorder, radius: 14, contentMargin: 10, shadowSize: 12);

    public static StyleBoxFlat HeroPanel() =>
        PanelStyle(
            Surface,
            SpecularBorder,
            radius: 14,
            contentMargin: 12,
            shadowSize: 12);

    public static StyleBoxFlat CardPanel(bool emphasized = false) =>
        NeonGlowPanel(strongGlow: emphasized);

    public static StyleBoxFlat NavigationPanel() =>
        PanelStyle(
            Surface,
            SpecularBorder,
            radius: 14,
            contentMargin: 4,
            shadowSize: 12);

    /// <summary>
    /// Yatay kariyer navigasyonu: yayın grafiği gibi ince, her ekranda sabit bir şerit.
    /// </summary>
    public static StyleBoxFlat TopNavigationPanel() =>
        new()
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = 0,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            ShadowColor = new Color(0f, 0f, 0f, 0.52f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };

    public static StyleBoxFlat PillPanel() =>
        PanelStyle(
            new Color(Ink.R, Ink.G, Ink.B, 0.045f),
            new Color(Stroke.R, Stroke.G, Stroke.B, 0.72f),
            radius: 999,
            contentMargin: 7,
            shadowSize: 0);

    public static StyleBoxFlat LivePillPanel() =>
        PanelStyle(
            new Color(Action.R, Action.G, Action.B, 0.13f),
            new Color(ActionBright.R, ActionBright.G, ActionBright.B, 0.62f),
            radius: 999,
            contentMargin: 7,
            shadowSize: 0);

    public static StyleBoxFlat EmblemPanel() =>
        PanelStyle(
            new Color(Accent.R, Accent.G, Accent.B, 0.10f),
            new Color(Accent.R, Accent.G, Accent.B, 0.72f),
            radius: 18,
            contentMargin: 8,
            shadowSize: 8);

    public static StyleBoxFlat StatusPanel(Color? signal = null)
    {
        var color = signal ?? Data;
        return
        PanelStyle(
            new Color(color.R, color.G, color.B, 0.08f),
            new Color(color.R, color.G, color.B, 0.36f),
            radius: 10,
            contentMargin: 10,
            shadowSize: 0);
    }

    public static StyleBoxFlat BadgePanel(Color color) =>
        PanelStyle(
            new Color(color.R, color.G, color.B, 0.10f),
            new Color(color.R, color.G, color.B, 0.42f),
            radius: 999,
            contentMargin: 7,
            shadowSize: 0);

    public static StyleBoxFlat LineupChipPanel(bool isIn, bool isOut)
    {
        var background = isOut
            ? new Color(DangerSoft.R, DangerSoft.G, DangerSoft.B, 0.12f)
            : isIn
                ? new Color(Action.R, Action.G, Action.B, 0.14f)
                : SurfaceSoft;
        var border = isOut ? DangerSoft : isIn ? Action : Stroke;
        return PanelStyle(background, border, radius: 9, contentMargin: 8, shadowSize: 0);
    }

    // ── NEW: Extended Typography Style Methods ───────────────────────────────

    /// <summary>Büyük sayısal değer: skor, önemli metrik.</summary>
    public static void StyleStatValue(Label label, Color? color = null)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(28));
        label.AddThemeColorOverride("font_color", color ?? Ink);
    }

    /// <summary>Orta istatistik değeri: form, kondisyon.</summary>
    public static void StyleStatValueMedium(Label label, Color? color = null)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(20));
        label.AddThemeColorOverride("font_color", color ?? Ink);
    }

    /// <summary>Section title — headline ile eyebrow arası, 18px Syne.</summary>
    public static void StyleSectionTitle(Label label)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(18));
        label.AddThemeColorOverride("font_color", Ink);
    }

    /// <summary>Card title — 15px Syne, altın tonu.</summary>
    public static void StyleCardTitle(Label label)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(15));
        label.AddThemeColorOverride("font_color", new Color(Accent.R, Accent.G, Accent.B, 0.92f));
    }

    /// <summary>Tablo metni — 13px Outfit Tabular.</summary>
    public static void StyleTableText(Label label, bool muted = false)
    {
        ApplyTabularFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(13));
        label.AddThemeColorOverride("font_color", muted ? InkMuted : Ink);
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.03f, 0.06f, 0.65f));
        label.AddThemeConstantOverride("outline_size", 1);
    }

    /// <summary>Tablo kolon başlığı — 11px muted.</summary>
    public static void StyleTableHeader(Label label)
    {
        ApplyEyebrowFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(11));
        label.AddThemeColorOverride("font_color", InkMuted);
    }

    /// <summary>Pozitif değer etiketi — yeşil.</summary>
    public static void StylePositiveValue(Label label)
    {
        ApplyBodyFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(13));
        label.AddThemeColorOverride("font_color", ActionBright);
    }

    /// <summary>Negatif değer etiketi — kırmızı.</summary>
    public static void StyleNegativeValue(Label label)
    {
        ApplyBodyFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(13));
        label.AddThemeColorOverride("font_color", DangerSoft);
    }

    /// <summary>Uyarı değer etiketi — sarı/turuncu.</summary>
    public static void StyleWarningValue(Label label)
    {
        ApplyBodyFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(13));
        label.AddThemeColorOverride("font_color", ColorWarning);
    }

    /// <summary>Mevki tag metni — GK, CB, ST gibi.</summary>
    public static void StylePositionTag(Label label, Color? color = null)
    {
        ApplyBodyFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(11));
        label.AddThemeColorOverride("font_color", color ?? Data);
    }

    // ── NEW: Extended Button Styles ───────────────────────────────────────────

    /// <summary>
    /// Prestige/Continue CTA — altın aksan. Ana loop CTA'sı.
    /// Normal primary button'dan görsel olarak belirgin biçimde ayrışır.
    /// </summary>
    public static void StyleAccentButton(Button button)
    {
        ApplyDisplayFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(14));
        button.AddThemeColorOverride("font_color", BackgroundDeep);
        button.AddThemeColorOverride("font_hover_color", BackgroundDeep);
        button.AddThemeColorOverride("font_pressed_color", BackgroundDeep);
        button.AddThemeColorOverride("font_focus_color", BackgroundDeep);
        button.AddThemeColorOverride("font_disabled_color", new Color(InkMuted.R, InkMuted.G, InkMuted.B, 0.58f));
        button.AddThemeStyleboxOverride("normal", SolidButton(Accent));
        button.AddThemeStyleboxOverride("hover", SolidButton(new Color(Mathf.Min(Accent.R + 0.08f, 1f), Mathf.Min(Accent.G + 0.06f, 1f), Accent.B, 1f)));
        button.AddThemeStyleboxOverride("pressed", SolidButton(new Color(Mathf.Max(Accent.R - 0.06f, 0f), Mathf.Max(Accent.G - 0.05f, 0f), Accent.B, 1f)));
        button.AddThemeStyleboxOverride("focus", FocusRing(Accent));
        button.AddThemeStyleboxOverride("disabled", SolidButton(new Color(0.20f, 0.18f, 0.10f, 0.82f)));
        button.CustomMinimumSize = new Vector2(0, 34);
        AttachSpringPressJuice(button, downScale: 0.93f, upBounce: 1.045f);
    }

    /// <summary>
    /// Ana menüdeki sinematik sağ menü şeridi. Altın sol ray, koyu yayın paneli
    /// ve zümrüt hover durumu referans görseldeki konsol menüsü hissini taşır.
    /// </summary>
    public static void StyleMainMenuRailButton(Button button, bool prominent = false)
    {
        ApplyDisplayFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(16));
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeColorOverride("font_hover_color", Ink);
        button.AddThemeColorOverride("font_pressed_color", BackgroundDeep);
        button.AddThemeColorOverride("font_focus_color", Ink);
        button.AddThemeColorOverride("font_disabled_color", new Color(InkMuted.R, InkMuted.G, InkMuted.B, 0.52f));
        button.AddThemeStyleboxOverride("normal", MainMenuRailSurface(
            prominent ? Accent : new Color(Accent.R, Accent.G, Accent.B, 0.78f),
            new Color(0.015f, 0.036f, 0.075f, 0.96f)));
        button.AddThemeStyleboxOverride("hover", MainMenuRailSurface(
            ActionBright,
            new Color(0.025f, 0.125f, 0.145f, 0.98f),
            glow: true));
        button.AddThemeStyleboxOverride("pressed", MainMenuRailSurface(
            Accent,
            new Color(Accent.R, Accent.G, Accent.B, 0.92f)));
        button.AddThemeStyleboxOverride("focus", FocusRing(prominent ? Accent : Data));
        button.AddThemeStyleboxOverride("disabled", MainMenuRailSurface(
            new Color(Stroke.R, Stroke.G, Stroke.B, 0.38f),
            new Color(0.025f, 0.052f, 0.075f, 0.84f)));
        button.CustomMinimumSize = new Vector2(0, 52);
    }

    /// <summary>Ghost/tertiary — sadece hover yüzeyi, minimal görsel ağırlık.</summary>
    public static void StyleGhostButton(Button button)
    {
        ApplyBodyFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(13));
        button.AddThemeColorOverride("font_color", InkMuted);
        button.AddThemeColorOverride("font_hover_color", Ink);
        button.AddThemeColorOverride("font_pressed_color", Ink);
        button.AddThemeColorOverride("font_focus_color", Ink);
        button.AddThemeColorOverride("font_disabled_color", ColorDisabled);
        var transparent = new StyleBoxFlat { BgColor = Colors.Transparent };
        var hover = new StyleBoxFlat
        {
            BgColor = new Color(Ink.R, Ink.G, Ink.B, 0.06f),
            CornerRadiusTopLeft = RadiusMedium, CornerRadiusTopRight = RadiusMedium,
            CornerRadiusBottomRight = RadiusMedium, CornerRadiusBottomLeft = RadiusMedium,
            ContentMarginLeft = SpaceM, ContentMarginRight = SpaceM,
            ContentMarginTop = SpaceS, ContentMarginBottom = SpaceS,
        };
        button.AddThemeStyleboxOverride("normal", transparent);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeStyleboxOverride("focus", FocusRing(Data));
        button.AddThemeStyleboxOverride("disabled", transparent);
        button.CustomMinimumSize = new Vector2(0, TouchTargetMin);
    }

    /// <summary>Danger/destructive — kırmızı outline. Geri dönüşü zor işlemler.</summary>
    public static void StyleDangerButton(Button button)
    {
        ApplyBodyFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(14));
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeColorOverride("font_hover_color", Ink);
        button.AddThemeColorOverride("font_pressed_color", Ink);
        button.AddThemeColorOverride("font_focus_color", Ink);
        button.AddThemeColorOverride("font_disabled_color", ColorDisabled);
        button.AddThemeStyleboxOverride("normal", OutlineButton(new Color(DangerSoft.R, DangerSoft.G, DangerSoft.B, 0.72f)));
        button.AddThemeStyleboxOverride("hover", SolidButton(new Color(DangerSoft.R, DangerSoft.G, DangerSoft.B, 0.22f)));
        button.AddThemeStyleboxOverride("pressed", SolidButton(new Color(DangerSoft.R, DangerSoft.G, DangerSoft.B, 0.32f)));
        button.AddThemeStyleboxOverride("focus", FocusRing(DangerSoft));
        button.AddThemeStyleboxOverride("disabled", OutlineButton(new Color(Stroke.R, Stroke.G, Stroke.B, 0.28f)));
        button.CustomMinimumSize = new Vector2(0, TouchTargetMin);
    }

    /// <summary>
    /// Sidebar nav item — sol kenarda renkli indicator şeridi, seçili/seçilmemiş durum.
    /// </summary>
    public static void StyleSidebarNavButton(Button button, bool selected)
    {
        ApplyBodyFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(13));
        button.AddThemeColorOverride("font_color", selected ? Ink : InkMuted);
        button.AddThemeColorOverride("font_hover_color", Ink);
        button.AddThemeColorOverride("font_pressed_color", Ink);
        button.AddThemeColorOverride("font_focus_color", Ink);

        if (selected)
        {
            var sel = new StyleBoxFlat
            {
                BgColor = new Color(Action.R, Action.G, Action.B, 0.16f),
                BorderColor = Action,
                BorderWidthLeft = 3, BorderWidthTop = 0, BorderWidthRight = 0, BorderWidthBottom = 0,
                CornerRadiusTopLeft = 0, CornerRadiusTopRight = RadiusMedium,
                CornerRadiusBottomRight = RadiusMedium, CornerRadiusBottomLeft = 0,
                ContentMarginLeft = SpaceL - 3, ContentMarginRight = SpaceM,
                ContentMarginTop = SpaceM, ContentMarginBottom = SpaceM,
            };
            button.AddThemeStyleboxOverride("normal", sel);
            button.AddThemeStyleboxOverride("hover", sel);
            button.AddThemeStyleboxOverride("pressed", sel);
        }
        else
        {
            var norm = new StyleBoxFlat
            {
                BgColor = Colors.Transparent,
                CornerRadiusTopLeft = RadiusMedium, CornerRadiusTopRight = RadiusMedium,
                CornerRadiusBottomRight = RadiusMedium, CornerRadiusBottomLeft = RadiusMedium,
                ContentMarginLeft = SpaceL, ContentMarginRight = SpaceM,
                ContentMarginTop = SpaceM, ContentMarginBottom = SpaceM,
            };
            var hov = new StyleBoxFlat
            {
                BgColor = new Color(Ink.R, Ink.G, Ink.B, 0.06f),
                CornerRadiusTopLeft = RadiusMedium, CornerRadiusTopRight = RadiusMedium,
                CornerRadiusBottomRight = RadiusMedium, CornerRadiusBottomLeft = RadiusMedium,
                ContentMarginLeft = SpaceL, ContentMarginRight = SpaceM,
                ContentMarginTop = SpaceM, ContentMarginBottom = SpaceM,
            };
            button.AddThemeStyleboxOverride("normal", norm);
            button.AddThemeStyleboxOverride("hover", hov);
            button.AddThemeStyleboxOverride("pressed", hov);
        }

        button.AddThemeStyleboxOverride("focus", FocusRing(Data));
        button.CustomMinimumSize = new Vector2(0, TouchTargetMin);
    }

    /// <summary>
    /// Üst oyun navigasyonu — referans tasarıma uygun.
    /// Seçili: tam yeşil dolgu, büyük köşe yarıçapı.
    /// Seçisiz: transparan, hover'da yarı saydam yeşil.
    /// </summary>
    public static void StyleHubNavButton(Button button, bool selected)
    {
        ApplyDisplayFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(12));
        button.AddThemeColorOverride("font_color", selected ? BackgroundDeep : InkMuted);
        button.AddThemeColorOverride("font_hover_color", selected ? BackgroundDeep : Ink);
        button.AddThemeColorOverride("font_pressed_color", BackgroundDeep);
        button.AddThemeColorOverride("font_focus_color", selected ? BackgroundDeep : Ink);
        button.AddThemeColorOverride("font_disabled_color", ColorDisabled);

        var normal = new StyleBoxFlat
        {
            BgColor = selected
                ? Action
                : Colors.Transparent,
            BorderColor = selected
                ? ActionBright
                : Colors.Transparent,
            BorderWidthLeft = 0,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
        };
        var hover = new StyleBoxFlat
        {
            BgColor = selected
                ? ActionBright
                : new Color(Data.R, Data.G, Data.B, 0.14f),
            BorderColor = new Color(Data.R, Data.G, Data.B, 0.42f),
            BorderWidthLeft = 0,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
        };

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeStyleboxOverride("focus", FocusRing(Action));
        button.AddThemeStyleboxOverride("disabled", normal);
        button.CustomMinimumSize = new Vector2(0, 30);
    }

    /// <summary>
    /// Sayfa başlığı metin stili — kompakt yayın başlığı.
    /// </summary>
    public static void StylePageTitle(Label label)
    {
        ApplyDisplayFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(14));
        label.AddThemeColorOverride("font_color", Ink);
    }

    /// <summary>
    /// Tab bar butonu — seçili: zümrüt/altın dolgu; seçisiz: koyu lacivert yüzey.
    /// </summary>
    public static void StyleTabButton(Button button, bool selected)
    {
        ApplyDisplayFont(button);
        button.AddThemeFontSizeOverride("font_size", FontSize(12));
        button.AddThemeColorOverride("font_color", selected ? BackgroundDeep : InkMuted);
        button.AddThemeColorOverride("font_hover_color", selected ? BackgroundDeep : Ink);
        button.AddThemeColorOverride("font_pressed_color", BackgroundDeep);
        button.AddThemeColorOverride("font_focus_color", selected ? BackgroundDeep : Ink);
        button.AddThemeColorOverride("font_disabled_color", ColorDisabled);

        var normal = new StyleBoxFlat
        {
            BgColor = selected
                ? Action
                : new Color(0.055f, 0.094f, 0.165f, 0.92f),
            BorderColor = selected
                ? ActionBright
                : new Color(Stroke.R, Stroke.G, Stroke.B, 0.45f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
        };
        var hover = new StyleBoxFlat
        {
            BgColor = selected
                ? ActionBright
                : new Color(Data.R, Data.G, Data.B, 0.18f),
            BorderColor = new Color(Data.R, Data.G, Data.B, 0.55f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
        };

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeStyleboxOverride("focus", FocusRing(Action));
        button.AddThemeStyleboxOverride("disabled", normal);
        button.CustomMinimumSize = new Vector2(64, 26);
    }

    // ── NEW: StyleBox Factories ───────────────────────────────────────────────

    /// <summary>Status badge — inline yeşil/kırmızı/sarı etiket.</summary>
    public static StyleBoxFlat StatusBadge(Color signalColor) =>
        PanelStyle(
            new Color(signalColor.R, signalColor.G, signalColor.B, 0.14f),
            new Color(signalColor.R, signalColor.G, signalColor.B, 0.52f),
            radius: RadiusPill, contentMargin: SpaceS, shadowSize: 0);

    /// <summary>Mevki tag — GK, CB, ST etiketi.</summary>
    public static StyleBoxFlat PositionTagPanel() =>
        PanelStyle(
            new Color(Data.R, Data.G, Data.B, 0.10f),
            new Color(Data.R, Data.G, Data.B, 0.42f),
            radius: RadiusSmall, contentMargin: SpaceXs, shadowSize: 0);

    /// <summary>Tablo satırı arka planı — seçili/normal/zebra.</summary>
    public static StyleBoxFlat TableRowPanel(bool isSelected = false, bool isAlternate = false)
    {
        if (isSelected)
        {
            return new StyleBoxFlat
            {
                BgColor = new Color(Action.R, Action.G, Action.B, 0.18f),
                BorderColor = new Color(Action.R, Action.G, Action.B, 0.48f),
                BorderWidthLeft = 3, BorderWidthTop = 0, BorderWidthRight = 0, BorderWidthBottom = 0,
                ContentMarginLeft = SpaceM - 3, ContentMarginRight = SpaceM,
                ContentMarginTop = SpaceS, ContentMarginBottom = SpaceS,
            };
        }

        return new StyleBoxFlat
        {
            BgColor = isAlternate ? new Color(Ink.R, Ink.G, Ink.B, 0.022f) : Colors.Transparent,
            ContentMarginLeft = SpaceM, ContentMarginRight = SpaceM,
            ContentMarginTop = SpaceS, ContentMarginBottom = SpaceS,
        };
    }

    /// <summary>Mini puan durumu satırı paneli — yönetilen takım parlak yeşil kenarlık ve dolgu alır.</summary>
    public static StyleBoxFlat MiniStandingsRowPanel(bool isManaged) =>
        isManaged
            ? new StyleBoxFlat
            {
                BgColor = new Color(Action.R, Action.G, Action.B, 0.16f),
                BorderColor = ActionBright,
                BorderWidthLeft = 3,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomRight = 6,
                CornerRadiusBottomLeft = 6,
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 6,
                ContentMarginBottom = 6,
            }
            : new StyleBoxFlat
            {
                BgColor = new Color(0f, 0f, 0f, 0.22f),
                BorderColor = new Color(Stroke.R, Stroke.G, Stroke.B, 0.25f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomRight = 6,
                CornerRadiusBottomLeft = 6,
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 6,
            };

    /// <summary>Kompakt rozet / durum hapı paneli.</summary>
    public static StyleBoxFlat BadgePillPanel(Color strokeColor, float bgAlpha = 0.12f) =>
        new()
        {
            BgColor = new Color(strokeColor.R, strokeColor.G, strokeColor.B, bgAlpha),
            BorderColor = strokeColor,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 999,
            CornerRadiusTopRight = 999,
            CornerRadiusBottomRight = 999,
            CornerRadiusBottomLeft = 999,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
        };

    /// <summary>Alert/vurgu panel — kritik karar, önemli gelişme.</summary>
    public static StyleBoxFlat AlertPanel(Color signalColor) =>
        PanelStyle(
            new Color(signalColor.R, signalColor.G, signalColor.B, 0.08f),
            new Color(signalColor.R, signalColor.G, signalColor.B, 0.55f),
            radius: RadiusMedium, contentMargin: SpaceM, shadowSize: 0);

    /// <summary>Top context bar panel — koyu lacivert spor yayını üst barı.</summary>
    public static StyleBoxFlat TopBarPanel() =>
        new()
        {
            BgColor = new Color(0.035f, 0.063f, 0.118f, 0.99f),
            BorderColor = new Color(Stroke.R, Stroke.G, Stroke.B, 0.65f),
            BorderWidthLeft = 0,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 1,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            ShadowColor = new Color(0f, 0f, 0f, 0.50f),
            ShadowSize = 6,
            ShadowOffset = new Vector2(0, 2),
        };

    /// <summary>Yayıncı kuruluşu tarzı canlı maç üst skorbord paneli.</summary>
    public static StyleBoxFlat ScoreboardPanel() =>
        new()
        {
            BgColor = new Color(0.008f, 0.031f, 0.055f, 0.97f),
            BorderColor = new Color(Stroke.R, Stroke.G, Stroke.B, 0.75f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomRight = 12,
            CornerRadiusBottomLeft = 12,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            ShadowColor = new Color(0f, 0f, 0f, 0.5f),
            ShadowSize = 10,
        };

    /// <summary>Canlı skorbord merkez skor hapı — yüksek kontrast ve altın/neon çerçeve.</summary>
    public static StyleBoxFlat ScoreboardPill() =>
        new()
        {
            BgColor = new Color(0.004f, 0.020f, 0.039f, 0.99f),
            BorderColor = new Color(ActionBright.R, ActionBright.G, ActionBright.B, 0.85f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };

    /// <summary>Canlı maç olay akışı kartı paneli (gol, kart, kurtarış vb.).</summary>
    public static StyleBoxFlat EventFeedCardPanel(string kind)
    {
        var accent = kind switch
        {
            "Goal" => ActionBright,
            "YellowCard" => ColorWarning,
            "RedCard" => DangerSoft,
            "Save" => Data,
            "Injury" => DangerSoft,
            _ => Stroke,
        };
        return new StyleBoxFlat
        {
            BgColor = new Color(0.025f, 0.082f, 0.112f, 0.92f),
            BorderColor = accent,
            BorderWidthLeft = 3,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 5,
            ContentMarginBottom = 5,
        };
    }

    /// <summary>Lig istatistik liderleri için bento kutusu stili (Hücum, Savunma, Form vb.).</summary>
    public static StyleBoxFlat StatLeaderCardPanel(Color accentColor) =>
        new()
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = 2,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.50f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };

    /// <summary>Kadro yıldız kartı yüzeyi (Spotlight vitrini).</summary>
    public static StyleBoxFlat SquadStarCardPanel(Color? accentColor = null)
    {
        return new StyleBoxFlat
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = 2,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.50f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };
    }

    /// <summary>Kadro SWOT analizinde güçlü yönler paneli (Zümrüt vurgulu).</summary>
    public static StyleBoxFlat SwotStrengthPanel() =>
        new()
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = 2,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.50f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };

    /// <summary>Kadro SWOT analizinde zayıf yönler ve riskler paneli (Kehribar/Kırmızı vurgulu).</summary>
    public static StyleBoxFlat SwotWeaknessPanel() =>
        new()
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = 2,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.50f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };

    /// <summary>Maça özel antrenman öncelik kartı paneli (FM26 tarzı).</summary>
    public static StyleBoxFlat MatchPriorityCardPanel(bool selected = false, bool recommended = false)
    {
        var borderColor = selected
            ? ActionBright
            : recommended
                ? Accent
                : SpecularBorder;
        var bgColor = selected
            ? new Color(0.075f, 0.165f, 0.215f, 0.90f)
            : Surface;

        return new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderColor = borderColor,
            BorderWidthLeft = selected || recommended ? 2 : 0,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            ShadowColor = new Color(0f, 0f, 0f, 0.52f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };
    }

    /// <summary>Taktik senaryo oyun planı kartı paneli (FM26 tarzı).</summary>
    public static StyleBoxFlat ScenarioCardPanel(bool active = false)
    {
        var borderColor = active ? ActionBright : SpecularBorder;
        var bgColor = active
            ? new Color(0.075f, 0.165f, 0.215f, 0.90f)
            : Surface;

        return new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderColor = borderColor,
            BorderWidthLeft = active ? 2 : 0,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            ShadowColor = new Color(0f, 0f, 0f, 0.52f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };
    }

    /// <summary>Yönetim kurulu hedef kartı paneli.</summary>
    public static StyleBoxFlat BoardObjectiveCardPanel(Color statusColor)
    {
        return new StyleBoxFlat
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = 2,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.52f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };
    }

    /// <summary>Kulüp muhasebe ve finans sütun kartı paneli.</summary>
    public static StyleBoxFlat FinanceLedgerCardPanel(bool positive = true)
    {
        return new StyleBoxFlat
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = 2,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.52f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };
    }

    /// <summary>Menajer kariyer mirası ve istatistik kutusu paneli.</summary>
    public static StyleBoxFlat LegacyStatCardPanel(Color accentColor)
    {
        return new StyleBoxFlat
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = 2,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            ShadowColor = new Color(0f, 0f, 0f, 0.52f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };
    }

    /// <summary>Metric hero — büyük sayı gösterimi.</summary>
    public static StyleBoxFlat MetricHeroPanel() =>
        PanelStyle(
            new Color(Accent.R, Accent.G, Accent.B, 0.06f),
            new Color(Accent.R, Accent.G, Accent.B, 0.24f),
            radius: RadiusLarge, contentMargin: SpaceXl, shadowSize: 8);

    public static StyleBoxFlat GhostPanel() =>
        PanelStyle(Colors.Transparent, Stroke, radius: RadiusMedium, contentMargin: SpaceS, shadowSize: 0);

    public static StyleBoxFlat GlassPanel() =>
        NeonGlowPanel(strongGlow: false);

    // Legacy style aliases
    public static void StyleHeaderLabel(Label label) => StyleHeadline(label);
    public static void StyleSubtleLabel(Label label)
    {
        ApplyBodyFont(label);
        label.AddThemeColorOverride("font_color", InkMuted);
    }

    public static void StyleLineupChip(Label label, bool isIn, bool isOut)
    {
        ApplyBodyFont(label);
        label.AddThemeFontSizeOverride("font_size", FontSize(13));
        label.AddThemeColorOverride(
            "font_color",
            isOut ? DangerSoft : isIn ? ActionHover : Ink);
    }

    internal static void ApplyDisplayFont(Control control)
    {
        EnsureLoaded();
        if (_displayBold is not null)
        {
            control.AddThemeFontOverride("font", _displayBold);
        }
        else if (_display is not null)
        {
            control.AddThemeFontOverride("font", _display);
        }
    }

    internal static void ApplyEyebrowFont(Control control)
    {
        EnsureLoaded();
        if (_eyebrowFont is not null)
        {
            control.AddThemeFontOverride("font", _eyebrowFont);
        }
        else if (_body is not null)
        {
            control.AddThemeFontOverride("font", _body);
        }
    }

    internal static void ApplyTabularFont(Control control)
    {
        EnsureLoaded();
        if (_tabularFont is not null)
        {
            control.AddThemeFontOverride("font", _tabularFont);
        }
        else if (_body is not null)
        {
            control.AddThemeFontOverride("font", _body);
        }
    }

    internal static void ApplyBodyFont(Control control)
    {
        EnsureLoaded();
        if (_body is not null)
        {
            control.AddThemeFontOverride("font", _body);
        }
    }

    /// <summary>
    /// Yaylı basma hissi (Spring Press Juice): Butona basıldığında 0.94x ölçeğe iner,
    /// bırakıldığında Back/Out eğrisiyle 1.035x -> 1.0x yaylanarak oturur.
    /// </summary>
    public static void AttachSpringPressJuice(BaseButton button, float downScale = 0.94f, float upBounce = 1.035f)
    {
        if (button.HasMeta("spring_juice_attached"))
        {
            return;
        }
        button.SetMeta("spring_juice_attached", true);

        button.Resized += () => button.PivotOffset = button.Size * 0.5f;

        button.ButtonDown += () =>
        {
            if (ReducedMotion || button.Disabled || !button.IsInsideTree())
            {
                return;
            }
            button.PivotOffset = button.Size * 0.5f;
            var t = button.CreateTween();
            t.TweenProperty(button, "scale", new Vector2(downScale, downScale), 0.065f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        };

        button.ButtonUp += () =>
        {
            if (ReducedMotion || !button.IsInsideTree())
            {
                button.Scale = Vector2.One;
                return;
            }
            button.PivotOffset = button.Size * 0.5f;
            var t = button.CreateTween();
            t.TweenProperty(button, "scale", new Vector2(upBounce, upBounce), 0.085f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
            t.TweenProperty(button, "scale", Vector2.One, 0.11f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
        };
    }

    /// <summary>
    /// Skor artışı veya kritik olay anında kontrolü büyütüp eski haline getiren "Scale Punch" efekti.
    /// </summary>
    public static void PulseScale(Control control, float peakScale = 1.18f, float duration = 0.24f)
    {
        if (ReducedMotion || !control.IsInsideTree())
        {
            return;
        }
        control.PivotOffset = control.Size * 0.5f;
        var t = control.CreateTween();
        t.TweenProperty(control, "scale", new Vector2(peakScale, peakScale), duration * 0.42f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        t.TweenProperty(control, "scale", Vector2.One, duration * 0.58f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    /// <summary>
    /// Gol veya kırmızı kart gibi anlarda saha veya skorbordu hafifçe sarsan "Camera Shake" efekti.
    /// </summary>
    public static void ShakeControl(Control control, float intensity = 6f, float duration = 0.28f)
    {
        if (ReducedMotion || !control.IsInsideTree())
        {
            return;
        }
        var origin = control.Position;
        var t = control.CreateTween();
        var step = duration / 5f;
        t.TweenProperty(control, "position", origin + new Vector2(-intensity, intensity * 0.4f), step);
        t.TweenProperty(control, "position", origin + new Vector2(intensity * 0.85f, -intensity * 0.35f), step);
        t.TweenProperty(control, "position", origin + new Vector2(-intensity * 0.55f, -intensity * 0.25f), step);
        t.TweenProperty(control, "position", origin + new Vector2(intensity * 0.3f, intensity * 0.2f), step);
        t.TweenProperty(control, "position", origin, step);
    }

    private static StyleBoxFlat SolidButton(Color color) =>
        new()
        {
            BgColor = color,
            BorderColor = new Color(1f, 1f, 1f, 0.22f),
            BorderWidthTop = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 10,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 5,
            ContentMarginBottom = 5,
            ShadowColor = new Color(0f, 0f, 0f, 0.40f),
            ShadowSize = 6,
            ShadowOffset = new Vector2(0, 2),
        };

    private static StyleBoxFlat MainMenuRailSurface(Color rail, Color background, bool glow = false) =>
        new()
        {
            BgColor = background,
            BorderColor = rail,
            BorderWidthLeft = 7,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = RadiusPill,
            CornerRadiusTopRight = RadiusPill,
            CornerRadiusBottomRight = RadiusPill,
            CornerRadiusBottomLeft = RadiusPill,
            ContentMarginLeft = 22,
            ContentMarginRight = 22,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
            ShadowColor = glow
                ? new Color(Action.R, Action.G, Action.B, 0.34f)
                : new Color(0f, 0f, 0f, 0.54f),
            ShadowSize = glow ? 9 : 7,
            ShadowOffset = new Vector2(0, 4),
        };

    private static StyleBoxFlat OutlineButton(Color border) =>
        new()
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthLeft = 0,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 10,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 5,
            ContentMarginBottom = 5,
            ShadowColor = new Color(0f, 0f, 0f, 0.42f),
            ShadowSize = 8,
            ShadowOffset = new Vector2(0, 2),
        };

    private static StyleBoxFlat InputField(Color border) =>
        new()
        {
            BgColor = new Color(BackgroundDeep.R, BackgroundDeep.G, BackgroundDeep.B, 0.88f),
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 10,
            ContentMarginLeft = 10,
            ContentMarginTop = 4,
            ContentMarginRight = 10,
            ContentMarginBottom = 4,
        };

    public static StyleBoxFlat PitchCardSurface(bool selected = false, Color? border = null)
    {
        var bg = selected
            ? new Color(0.018f, 0.145f, 0.132f, 0.98f)
            : Surface;
        var borderColor = border ?? (selected ? ActionBright : SpecularBorder);
        var borderWidth = selected ? 2 : 1;

        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = borderColor,
            BorderWidthLeft = selected ? borderWidth : 0,
            BorderWidthTop = borderWidth,
            BorderWidthRight = selected ? borderWidth : 0,
            BorderWidthBottom = selected ? borderWidth : 0,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            ShadowColor = selected ? new Color(Action.R, Action.G, Action.B, 0.52f) : new Color(0f, 0f, 0f, 0.52f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0, 4),
        };
    }

    private static StyleBoxFlat PitchChipSurface(Color? border = null) => PitchCardSurface(false, border);

    private static StyleBoxFlat NavButtonSurface() =>
        new()
        {
            BgColor = Surface,
            BorderColor = SpecularBorder,
            BorderWidthTop = 1,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
        };

    private static StyleBoxFlat FocusRing(Color color) =>
        new()
        {
            BgColor = Colors.Transparent,
            BorderColor = color,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusBottomLeft = 14,
            ExpandMarginLeft = 2,
            ExpandMarginTop = 2,
            ExpandMarginRight = 2,
            ExpandMarginBottom = 2,
        };

    private static StyleBoxFlat PanelStyle(
        Color background,
        Color border,
        int radius,
        float contentMargin,
        int shadowSize)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = 0,
            BorderWidthTop = 1,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomRight = radius,
            CornerRadiusBottomLeft = radius,
            ContentMarginLeft = contentMargin,
            ContentMarginRight = contentMargin,
            ContentMarginTop = contentMargin,
            ContentMarginBottom = contentMargin,
            ShadowColor = new Color(0f, 0f, 0f, shadowSize > 0 ? 0.52f : 0f),
            ShadowSize = shadowSize,
            ShadowOffset = new Vector2(0, shadowSize > 0 ? 4 : 0),
        };
    }

    /// <summary>
    /// Harici görsel gerektirmeyen, koyu stadyum ışığı ve soyut saha çizgileri üreten arka plan.
    /// </summary>
    public static Control CreateAtmosphereBackground()
    {
        var root = new Control
        {
            Name = "Atmosphere",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        root.AddChild(FullRectTexture(
            "NightGradient",
            LinearGradient(
                new Vector2(0.04f, 0f),
                new Vector2(0.96f, 1f),
                [0f, 0.42f, 1f],
                [BackgroundNavy, Background, BackgroundDeep])));

        root.AddChild(FullRectTexture(
            "StadiumGlow",
            RadialGradient(
                new Vector2(0.58f, 0.01f),
                new Vector2(0.5f, 0.68f),
                [0f, 0.42f, 1f],
                [
                    new Color(Data.R, Data.G, Data.B, 0.24f),
                    new Color(Action.R, Action.G, Action.B, 0.07f),
                    Colors.Transparent,
                ])));

        var pitch = new ColorRect
        {
            Name = "PitchHaze",
            Color = new Color(Action.R, Action.G, Action.B, 0.026f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        pitch.SetAnchor(Side.Left, 0f);
        pitch.SetAnchor(Side.Right, 1f);
        pitch.SetAnchor(Side.Top, 0.56f);
        pitch.SetAnchor(Side.Bottom, 1f);
        root.AddChild(pitch);

        for (var index = 0; index < 6; index++)
        {
            var band = new ColorRect
            {
                Color = new Color(
                    index % 2 == 0 ? Action.R : Data.R,
                    index % 2 == 0 ? Action.G : Data.G,
                    index % 2 == 0 ? Action.B : Data.B,
                    index % 2 == 0 ? 0.024f : 0.012f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            var left = index / 6f;
            var right = (index + 1) / 6f;
            band.SetAnchor(Side.Left, left);
            band.SetAnchor(Side.Right, right);
            band.SetAnchor(Side.Top, 0.56f);
            band.SetAnchor(Side.Bottom, 1f);
            root.AddChild(band);
        }

        var halfwayLine = new ColorRect
        {
            Color = new Color(Data.R, Data.G, Data.B, 0.070f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        halfwayLine.SetAnchor(Side.Left, 0.5f);
        halfwayLine.SetAnchor(Side.Right, 0.5f);
        halfwayLine.SetAnchor(Side.Top, 0.56f);
        halfwayLine.SetAnchor(Side.Bottom, 1f);
        halfwayLine.OffsetLeft = -1;
        halfwayLine.OffsetRight = 1;
        root.AddChild(halfwayLine);

        var centreCircle = new Panel
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        centreCircle.SetAnchor(Side.Left, 0.5f);
        centreCircle.SetAnchor(Side.Right, 0.5f);
        centreCircle.SetAnchor(Side.Top, 0.76f);
        centreCircle.SetAnchor(Side.Bottom, 0.76f);
        centreCircle.OffsetLeft = -76;
        centreCircle.OffsetRight = 76;
        centreCircle.OffsetTop = -76;
        centreCircle.OffsetBottom = 76;
        centreCircle.AddThemeStyleboxOverride(
            "panel",
            new StyleBoxFlat
            {
                BgColor = Colors.Transparent,
                BorderColor = new Color(Data.R, Data.G, Data.B, 0.075f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 999,
                CornerRadiusTopRight = 999,
                CornerRadiusBottomRight = 999,
                CornerRadiusBottomLeft = 999,
            });
        root.AddChild(centreCircle);

        var prestigeLine = new ColorRect
        {
            Name = "PrestigeLine",
            Color = new Color(Accent.R, Accent.G, Accent.B, 0.72f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        prestigeLine.SetAnchor(Side.Left, 0f);
        prestigeLine.SetAnchor(Side.Right, 1f);
        prestigeLine.SetAnchor(Side.Top, 0f);
        prestigeLine.OffsetBottom = 3;
        root.AddChild(prestigeLine);

        root.AddChild(FullRectTexture(
            "LowerVignette",
            LinearGradient(
                new Vector2(0.5f, 0.42f),
                new Vector2(0.5f, 1f),
                [0f, 1f],
                [Colors.Transparent, new Color(BackgroundDeep.R, BackgroundDeep.G, BackgroundDeep.B, 0.64f)])));

        return root;
    }

    private static TextureRect FullRectTexture(string name, Texture2D texture)
    {
        var rect = new TextureRect
        {
            Name = name,
            Texture = texture,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return rect;
    }

    private static GradientTexture2D LinearGradient(
        Vector2 from,
        Vector2 to,
        float[] offsets,
        Color[] colors) =>
        new()
        {
            Width = 32,
            Height = 32,
            Fill = GradientTexture2D.FillEnum.Linear,
            FillFrom = from,
            FillTo = to,
            Gradient = new Gradient
            {
                Offsets = offsets,
                Colors = colors,
            },
        };

    private static GradientTexture2D RadialGradient(
        Vector2 from,
        Vector2 to,
        float[] offsets,
        Color[] colors) =>
        new()
        {
            Width = 64,
            Height = 64,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = from,
            FillTo = to,
            Gradient = new Gradient
            {
                Offsets = offsets,
                Colors = colors,
            },
        };
}
