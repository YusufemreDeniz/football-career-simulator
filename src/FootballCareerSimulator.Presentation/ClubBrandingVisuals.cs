using System;
using System.Collections.Generic;
using System.Linq;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Simulation.DataPacks;
using Godot;

namespace FootballCareerSimulator.Presentation;

public enum KitVariantType
{
    Home = 0,
    Away = 1,
    Third = 2,
}

public sealed record ClubVisualIdentity(
    long ClubId,
    string OfficialName,
    string ShortCode,
    string LeagueName,
    Color PrimaryColor,
    Color SecondaryColor,
    Color ThirdColor,
    string KitPattern,
    string? CrestResourcePath,
    string? HomeKitResourcePath,
    string? AwayKitResourcePath,
    string? ThirdKitResourcePath);

public static class ClubBrandingVisuals
{
    private static readonly Dictionary<string, Texture2D?> TextureCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<long, (string Code, string PrimaryHex, string SecondaryHex, string ThirdHex, string Pattern)> DomesticPalette =
        new Dictionary<long, (string Code, string PrimaryHex, string SecondaryHex, string ThirdHex, string Pattern)>
        {
            [1] = ("GS", "#A90432", "#FDB912", "#FFFFFF", "Halves"),
            [2] = ("FB", "#002D72", "#FFED00", "#1E3A8A", "Stripes"),
            [3] = ("BJK", "#111111", "#FFFFFF", "#C8102E", "Stripes"),
            [4] = ("TS", "#800020", "#38BDF8", "#F8FAFC", "Stripes"),
            [5] = ("BFK", "#F97316", "#1E3A8A", "#FFFFFF", "Solid"),
            [6] = ("SAM", "#DC2626", "#FFFFFF", "#111111", "Stripes"),
            [7] = ("EYP", "#6D28D9", "#FACC15", "#FFFFFF", "Stripes"),
            [8] = ("GOZ", "#FACC15", "#DC2626", "#111111", "Halves"),
            [9] = ("RİZ", "#15803D", "#1D4ED8", "#FFFFFF", "Stripes"),
            [10] = ("KAS", "#1D4ED8", "#FFFFFF", "#0F172A", "Stripes"),
            [11] = ("KON", "#15803D", "#FFFFFF", "#111111", "Stripes"),
            [12] = ("ALN", "#F97316", "#15803D", "#FFFFFF", "Halves"),
            [13] = ("GFK", "#DC2626", "#111111", "#FFFFFF", "Stripes"),
            [14] = ("ANT", "#DC2626", "#FFFFFF", "#0F172A", "Stripes"),
            [15] = ("KAY", "#FACC15", "#DC2626", "#FFFFFF", "Stripes"),
            [16] = ("KOC", "#15803D", "#111111", "#FFFFFF", "Stripes"),
            [17] = ("AMD", "#15803D", "#DC2626", "#FFFFFF", "Stripes"),
            [18] = ("ÇOR", "#DC2626", "#111111", "#FFFFFF", "Stripes"),
        };

    public static Texture2D? TryLoadTexture(string? resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (TextureCache.TryGetValue(resourcePath, out var cached))
        {
            return cached;
        }

        try
        {
            if (ResourceLoader.Exists(resourcePath))
            {
                var tex = ResourceLoader.Load<Texture2D>(resourcePath);
                TextureCache[resourcePath] = tex;
                return tex;
            }
        }
        catch
        {
            // Ignore missing or unimported texture in headless/test runs
        }

        TextureCache[resourcePath] = null;
        return null;
    }

    public static ClubVisualIdentity ResolveClubIdentity(long? clubId, string? clubName = null)
    {
        if (clubId is long id && id > 0)
        {
            if (id < 100 && TurkeySuperLig202627DataPack.TryGetClub(new ClubId(id), out var trClub))
            {
                var (code, primaryHex, secondaryHex, thirdHex, pattern) =
                    DomesticPalette.GetValueOrDefault(id, ("SL", "#00F59B", "#0D1522", "#FFB800", "Stripes"));
                return new ClubVisualIdentity(
                    trClub.ClubId,
                    trClub.OfficialName,
                    code,
                    TurkeySuperLig202627DataPack.CompetitionName,
                    Color.FromHtml(primaryHex),
                    Color.FromHtml(secondaryHex),
                    Color.FromHtml(thirdHex),
                    pattern,
                    trClub.CrestResourcePath,
                    trClub.HomeKitResourcePath,
                    trClub.AwayKitResourcePath,
                    trClub.ThirdKitResourcePath);
            }

            if (EuropeanElite202627DataPack.TryGetClub(id, out var euroClub))
            {
                return BuildFromEuroClub(euroClub);
            }
        }

        if (!string.IsNullOrWhiteSpace(clubName))
        {
            var trimmed = clubName.Trim();
            foreach (var tr in TurkeySuperLig202627DataPack.AllClubs)
            {
                if (tr.OfficialName.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                    || trimmed.Contains(tr.OfficialName.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
                {
                    return ResolveClubIdentity(tr.ClubId, tr.OfficialName);
                }
            }

            if (EuropeanElite202627DataPack.TryFindByName(trimmed, out var euroByName))
            {
                return BuildFromEuroClub(euroByName);
            }
        }

        var fallbackName = string.IsNullOrWhiteSpace(clubName) ? "KULÜP" : clubName.Trim();
        var letters = new string(fallbackName.Where(char.IsLetter).Take(3).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(letters)) letters = "FCS";

        return new ClubVisualIdentity(
            clubId ?? 0,
            fallbackName,
            letters,
            "Futbol Ligi",
            CareerUiTheme.ActionBright,
            CareerUiTheme.Surface,
            CareerUiTheme.Accent,
            "Stripes",
            null,
            null,
            null,
            null);
    }

    private static ClubVisualIdentity BuildFromEuroClub(EuropeanEliteClubData euro)
    {
        var primary = SafeColor(euro.PrimaryColorHex, CareerUiTheme.ActionBright);
        var secondary = SafeColor(euro.SecondaryColorHex, CareerUiTheme.Accent);
        var third = new Color(0.10f, 0.16f, 0.26f, 1f);
        return new ClubVisualIdentity(
            euro.ClubId,
            euro.OfficialName,
            euro.ShortCode,
            euro.LeagueName,
            primary,
            secondary,
            third,
            euro.KitPattern,
            string.IsNullOrWhiteSpace(euro.CrestResourcePath) ? null : euro.CrestResourcePath,
            string.IsNullOrWhiteSpace(euro.HomeKitResourcePath) ? null : euro.HomeKitResourcePath,
            string.IsNullOrWhiteSpace(euro.AwayKitResourcePath) ? null : euro.AwayKitResourcePath,
            string.IsNullOrWhiteSpace(euro.ThirdKitResourcePath) ? null : euro.ThirdKitResourcePath);
    }

    private static Color SafeColor(string hex, Color fallback)
    {
        try
        {
            return Color.FromHtml(hex);
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>
    /// Kulüp armasını (Crest) + Ev Sahibi / Deplasman / Alternatif 3'lü forma setini yan yana gösteren Broadcast vitrin şeridi.
    /// </summary>
    public static PanelContainer BuildClubKitShowcaseStrip(long? clubId, string? clubName, string? subtitle = null)
    {
        var identity = ResolveClubIdentity(clubId, clubName);

        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized: true));

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        row.AddThemeConstantOverride("separation", 14);
        panel.AddChild(row);

        // Sol: Kulüp Arması + İsim & Lig Özeti
        var identityRow = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        identityRow.AddThemeConstantOverride("separation", 10);
        row.AddChild(identityRow);

        var crest = new ClubCrestControl(identity, new Vector2(54, 54));
        identityRow.AddChild(crest);

        var textCol = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        textCol.AddThemeConstantOverride("separation", 2);
        identityRow.AddChild(textCol);

        var eyebrow = new Label
        {
            Text = $"{identity.LeagueName.ToUpperInvariant()} · LİSANSLI KULÜP KİMLİĞİ & FORMALAR",
        };
        CareerUiTheme.StyleEyebrow(eyebrow, CareerUiTheme.Accent);
        textCol.AddChild(eyebrow);

        var title = new Label
        {
            Text = identity.OfficialName,
        };
        CareerUiTheme.StyleHeadline(title);
        title.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        textCol.AddChild(title);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            var subLbl = new Label
            {
                Text = subtitle,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            CareerUiTheme.StyleBody(subLbl, muted: true);
            subLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
            textCol.AddChild(subLbl);
        }

        // Sağ: Ev Sahibi, Deplasman, Alternatif Forma Seti
        var kitsRow = new HBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        kitsRow.AddThemeConstantOverride("separation", 10);
        row.AddChild(kitsRow);

        kitsRow.AddChild(BuildKitMiniCard(identity, KitVariantType.Home, "İÇ SAHA"));
        kitsRow.AddChild(BuildKitMiniCard(identity, KitVariantType.Away, "DEPLASMAN"));
        kitsRow.AddChild(BuildKitMiniCard(identity, KitVariantType.Third, "ALTERNATİF"));

        return panel;
    }

    private static VBoxContainer BuildKitMiniCard(ClubVisualIdentity identity, KitVariantType variant, string caption)
    {
        var box = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            CustomMinimumSize = new Vector2(64, 0),
        };
        box.AddThemeConstantOverride("separation", 2);

        var kitControl = new ClubKitControl(identity, variant, new Vector2(48, 48))
        {
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        box.AddChild(kitControl);

        var lbl = new Label
        {
            Text = caption,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleEyebrow(lbl, CareerUiTheme.InkMuted);
        lbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(9));
        box.AddChild(lbl);

        return box;
    }
}

/// <summary>
/// Kulüp armasını (Crest) PNG doku varsa dokudan, yoksa kulübün gerçek renkleri ve desenleriyle vektörel kalkan olarak çizer.
/// </summary>
public sealed partial class ClubCrestControl : Control
{
    private ClubVisualIdentity _identity;
    private Texture2D? _crestTexture;

    public ClubCrestControl() : this(ClubBrandingVisuals.ResolveClubIdentity(null, "FCS"), new Vector2(44, 44))
    {
    }

    public ClubCrestControl(ClubVisualIdentity identity, Vector2 size)
    {
        _identity = identity;
        CustomMinimumSize = size;
        MouseFilter = MouseFilterEnum.Ignore;
        _crestTexture = ClubBrandingVisuals.TryLoadTexture(identity.CrestResourcePath);
    }

    public void SetClub(long? clubId, string? clubName = null)
    {
        _identity = ClubBrandingVisuals.ResolveClubIdentity(clubId, clubName);
        _crestTexture = ClubBrandingVisuals.TryLoadTexture(_identity.CrestResourcePath);
        QueueRedraw();
    }

    public void SetIdentity(ClubVisualIdentity identity)
    {
        _identity = identity;
        _crestTexture = ClubBrandingVisuals.TryLoadTexture(_identity.CrestResourcePath);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        var center = rect.Size * 0.5f;
        var radius = Mathf.Min(rect.Size.X, rect.Size.Y) * 0.45f;

        // Yumuşak dış hale
        DrawCircle(center + new Vector2(0, 2f), radius + 2f, new Color(0f, 0f, 0f, 0.45f));
        DrawCircle(center, radius + 2f, new Color(_identity.PrimaryColor.R, _identity.PrimaryColor.G, _identity.PrimaryColor.B, 0.22f));

        if (_crestTexture is not null)
        {
            var texRect = new Rect2(
                center.X - radius * 0.92f,
                center.Y - radius * 0.92f,
                radius * 1.84f,
                radius * 1.84f);
            DrawTextureRect(_crestTexture, texRect, false);
            return;
        }

        // Vektörel Arma Kalkanı (Avrupa kulüpleri veya doku bulunmayan durumlar için)
        var shield = new Vector2[]
        {
            new(center.X, center.Y - radius * 0.94f),
            new(center.X + radius * 0.84f, center.Y - radius * 0.58f),
            new(center.X + radius * 0.72f, center.Y + radius * 0.36f),
            new(center.X, center.Y + radius * 0.96f),
            new(center.X - radius * 0.72f, center.Y + radius * 0.36f),
            new(center.X - radius * 0.84f, center.Y - radius * 0.58f),
        };

        DrawColoredPolygon(shield, _identity.PrimaryColor.Darkened(0.12f));

        // Sağ yarı veya dikey şerit efekti
        var rightHalf = new Vector2[]
        {
            new(center.X, center.Y - radius * 0.90f),
            new(center.X + radius * 0.78f, center.Y - radius * 0.54f),
            new(center.X + radius * 0.66f, center.Y + radius * 0.32f),
            new(center.X, center.Y + radius * 0.90f),
        };
        DrawColoredPolygon(rightHalf, new Color(_identity.SecondaryColor.R, _identity.SecondaryColor.G, _identity.SecondaryColor.B, 0.82f));

        // İç koyu bant (okunaklı kısaltma için)
        var bandHeight = radius * 0.56f;
        DrawRect(
            new Rect2(center.X - radius * 0.70f, center.Y - bandHeight * 0.5f, radius * 1.40f, bandHeight),
            new Color(0.04f, 0.07f, 0.12f, 0.82f),
            filled: true);

        // Kalkan dış çerçevesi
        for (var i = 0; i < shield.Length; i++)
        {
            DrawLine(shield[i], shield[(i + 1) % shield.Length], CareerUiTheme.Accent, 1.8f, true);
        }

        var font = ThemeDB.FallbackFont;
        if (font is not null)
        {
            var fontSize = Mathf.Clamp((int)(radius * 0.56f), 9, 16);
            var textSize = font.GetStringSize(_identity.ShortCode, HorizontalAlignment.Center, -1, fontSize);
            DrawString(
                font,
                new Vector2(center.X - textSize.X * 0.5f, center.Y + textSize.Y * 0.28f),
                _identity.ShortCode,
                HorizontalAlignment.Left,
                -1,
                fontSize,
                Colors.White);
        }
    }
}

/// <summary>
/// Kulüp formasını (Ev Sahibi / Deplasman / Alternatif) PNG doku varsa dokudan, yoksa kulübün gerçek renk ve desenleriyle (Çubuklu, Parçalı, Bantlı, Düz) çizer.
/// </summary>
public sealed partial class ClubKitControl : Control
{
    private ClubVisualIdentity _identity;
    private KitVariantType _variant;
    private Texture2D? _kitTexture;

    public ClubKitControl() : this(ClubBrandingVisuals.ResolveClubIdentity(null, "FCS"), KitVariantType.Home, new Vector2(46, 46))
    {
    }

    public ClubKitControl(ClubVisualIdentity identity, KitVariantType variant, Vector2 size)
    {
        _identity = identity;
        _variant = variant;
        CustomMinimumSize = size;
        MouseFilter = MouseFilterEnum.Ignore;
        RefreshTexture();
    }

    public void SetClubKit(long? clubId, string? clubName, KitVariantType variant = KitVariantType.Home)
    {
        _identity = ClubBrandingVisuals.ResolveClubIdentity(clubId, clubName);
        _variant = variant;
        RefreshTexture();
        QueueRedraw();
    }

    private void RefreshTexture()
    {
        var path = _variant switch
        {
            KitVariantType.Away => _identity.AwayKitResourcePath,
            KitVariantType.Third => _identity.ThirdKitResourcePath,
            _ => _identity.HomeKitResourcePath,
        };
        _kitTexture = ClubBrandingVisuals.TryLoadTexture(path);
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        var center = rect.Size * 0.5f;
        var scale = Mathf.Min(rect.Size.X, rect.Size.Y) / 48f;

        if (_kitTexture is not null)
        {
            var pad = 2f * scale;
            DrawTextureRect(
                _kitTexture,
                new Rect2(pad, pad, rect.Size.X - pad * 2f, rect.Size.Y - pad * 2f),
                false);
            return;
        }

        // Vektörel Forma Çizimi (Home / Away / Third varyasyonuna göre renk seçimi)
        var (bodyColor, accentColor, pattern) = _variant switch
        {
            KitVariantType.Away => (_identity.SecondaryColor, _identity.PrimaryColor, "Solid"),
            KitVariantType.Third => (_identity.ThirdColor, _identity.PrimaryColor, "Sash"),
            _ => (_identity.PrimaryColor, _identity.SecondaryColor, _identity.KitPattern),
        };

        // Gölge
        DrawCircle(center + new Vector2(0, 3f * scale), 18f * scale, new Color(0f, 0f, 0f, 0.35f));

        // Forma Gövde Poligonu
        var torso = new Vector2[]
        {
            new(center.X - 8f * scale, center.Y - 16f * scale),
            new(center.X + 8f * scale, center.Y - 16f * scale),
            new(center.X + 19f * scale, center.Y - 8f * scale),
            new(center.X + 15f * scale, center.Y - 1f * scale),
            new(center.X + 11f * scale, center.Y - 4f * scale),
            new(center.X + 11f * scale, center.Y + 17f * scale),
            new(center.X - 11f * scale, center.Y + 17f * scale),
            new(center.X - 11f * scale, center.Y - 4f * scale),
            new(center.X - 15f * scale, center.Y - 1f * scale),
            new(center.X - 19f * scale, center.Y - 8f * scale),
        };
        DrawColoredPolygon(torso, bodyColor);

        // Desen Uygulaması (Stripes, Halves, Hoops, Sash, Sleeves)
        if (string.Equals(pattern, "Stripes", StringComparison.OrdinalIgnoreCase))
        {
            DrawRect(new Rect2(center.X - 7.5f * scale, center.Y - 14f * scale, 3.8f * scale, 31f * scale), accentColor, true);
            DrawRect(new Rect2(center.X + 3.7f * scale, center.Y - 14f * scale, 3.8f * scale, 31f * scale), accentColor, true);
        }
        else if (string.Equals(pattern, "Halves", StringComparison.OrdinalIgnoreCase))
        {
            DrawRect(new Rect2(center.X, center.Y - 15f * scale, 11f * scale, 32f * scale), accentColor, true);
        }
        else if (string.Equals(pattern, "Hoops", StringComparison.OrdinalIgnoreCase))
        {
            DrawRect(new Rect2(center.X - 10.5f * scale, center.Y - 8f * scale, 21f * scale, 4.5f * scale), accentColor, true);
            DrawRect(new Rect2(center.X - 10.5f * scale, center.Y + 3f * scale, 21f * scale, 4.5f * scale), accentColor, true);
        }
        else if (string.Equals(pattern, "Sash", StringComparison.OrdinalIgnoreCase))
        {
            DrawLine(
                new Vector2(center.X - 8f * scale, center.Y - 13f * scale),
                new Vector2(center.X + 9f * scale, center.Y + 15f * scale),
                accentColor,
                4.2f * scale,
                true);
        }
        else if (string.Equals(pattern, "Sleeves", StringComparison.OrdinalIgnoreCase))
        {
            var leftSleeve = new Vector2[]
            {
                new(center.X - 8f * scale, center.Y - 16f * scale),
                new(center.X - 19f * scale, center.Y - 8f * scale),
                new(center.X - 15f * scale, center.Y - 1f * scale),
                new(center.X - 11f * scale, center.Y - 4f * scale),
            };
            var rightSleeve = new Vector2[]
            {
                new(center.X + 8f * scale, center.Y - 16f * scale),
                new(center.X + 19f * scale, center.Y - 8f * scale),
                new(center.X + 15f * scale, center.Y - 1f * scale),
                new(center.X + 11f * scale, center.Y - 4f * scale),
            };
            DrawColoredPolygon(leftSleeve, accentColor);
            DrawColoredPolygon(rightSleeve, accentColor);
        }

        // Yaka (V-Neck)
        DrawLine(
            new Vector2(center.X - 5.5f * scale, center.Y - 16f * scale),
            new Vector2(center.X, center.Y - 11f * scale),
            accentColor,
            2f * scale,
            true);
        DrawLine(
            new Vector2(center.X + 5.5f * scale, center.Y - 16f * scale),
            new Vector2(center.X, center.Y - 11f * scale),
            accentColor,
            2f * scale,
            true);

        // Göğüste mini arma rozeti
        DrawCircle(new Vector2(center.X + 4.8f * scale, center.Y - 6.5f * scale), 2.3f * scale, CareerUiTheme.Accent);

        // Dış Kontur
        for (var i = 0; i < torso.Length; i++)
        {
            DrawLine(torso[i], torso[(i + 1) % torso.Length], new Color(1f, 1f, 1f, 0.38f), 1.2f, true);
        }
    }
}
