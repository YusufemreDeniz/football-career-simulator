using System;
using System.Collections.Generic;
using FootballCareerSimulator.Application.CareerHub.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Top Eleven / Football Chairman Pro 2 tarzı 2D görsel Kulüp Kampüsü çizimi (Stadyum, Antrenman, Akademi, Sağlık)
/// ve Top Drives / New Star Manager tarzı Koleksiyon Oyuncu Kartları (Collectible Trading Cards) bileşeni.
/// </summary>
public static partial class ClubCampusVisualUi
{
    private static string _customStadiumTitle = "ALİ SAMİ YEN SPOR KOMPLEKSİ";
    private static string _customNorthStandName = "KUZEY ULTRAS TRİBÜNÜ";

    public static string CustomStadiumTitle => _customStadiumTitle;
    public static string CustomNorthStandName => _customNorthStandName;

    public static void SetCustomStadiumNaming(string stadiumTitle, string northStandName)
    {
        if (!string.IsNullOrWhiteSpace(stadiumTitle))
        {
            _customStadiumTitle = stadiumTitle.Trim().ToUpperInvariant();
        }
        if (!string.IsNullOrWhiteSpace(northStandName))
        {
            _customNorthStandName = northStandName.Trim().ToUpperInvariant();
        }
    }

    public static Control BuildCampusGrid(
        string clubName,
        int facilityLevel,
        double trainingMultiplier,
        int attendancePercent,
        int stadiumCapacity,
        Action onUpgradePressed,
        Action onRenameStadiumPressed,
        bool canUpgrade,
        string upgradeButtonText)
    {
        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        root.AddThemeConstantOverride("separation", 6);

        var buildingsGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        buildingsGrid.AddThemeConstantOverride("h_separation", 8);
        buildingsGrid.AddThemeConstantOverride("v_separation", 8);
        root.AddChild(buildingsGrid);

        var lvl = Math.Clamp(facilityLevel, 1, 5);
        var growthPct = (int)Math.Round((trainingMultiplier - 1.0) * 100);

        buildingsGrid.AddChild(BuildCampusBuildingTile(
            buildingKind: CampusBuildingKind.Stadium,
            title: "🏟️ STADYUM & TRİBÜN",
            subtitle: $"{_customNorthStandName} · {stadiumCapacity:N0} Koltuk",
            bonusText: $"+🔥 %{attendancePercent} Doluluk",
            level: lvl,
            accent: CareerUiTheme.Accent));

        buildingsGrid.AddChild(BuildCampusBuildingTile(
            buildingKind: CampusBuildingKind.Training,
            title: "🏋️ ANTRENMAN MERKEZİ",
            subtitle: $"Taktik & Kondisyon Sahaları",
            bonusText: $"+⚡ %{Math.Max(8, growthPct)} Gelişim",
            level: lvl,
            accent: CareerUiTheme.ActionBright));

        buildingsGrid.AddChild(BuildCampusBuildingTile(
            buildingKind: CampusBuildingKind.Academy,
            title: "🌱 GENÇLİK AKADEMİSİ",
            subtitle: "U21 Yetenek Tarama Kampüsü",
            bonusText: $"+🌟 Lv.{lvl} Potansiyel",
            level: lvl,
            accent: CareerUiTheme.Data));

        buildingsGrid.AddChild(BuildCampusBuildingTile(
            buildingKind: CampusBuildingKind.Medical,
            title: "🏥 SAĞLIK & FİZYO SPA",
            subtitle: "Rehabilitasyon & Hiperbarik Oda",
            bonusText: $"+💚 %{lvl * 6} Yenilenme",
            level: lvl,
            accent: new Color(0.28f, 0.88f, 0.68f)));

        var actionsRow = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        actionsRow.AddThemeConstantOverride("separation", 6);
        root.AddChild(actionsRow);

        var upgradeBtn = new Button
        {
            Text = upgradeButtonText,
            Disabled = !canUpgrade,
            CustomMinimumSize = new Vector2(0, 34),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StylePrimaryButton(upgradeBtn);
        upgradeBtn.Pressed += onUpgradePressed;
        actionsRow.AddChild(upgradeBtn);

        var renameBtn = new Button
        {
            Text = "🏟️ Tribün & Stad Adı",
            TooltipText = "Football Chairman tarzı stadyum ve tribün isimlendirme ekranını aç",
            CustomMinimumSize = new Vector2(0, 34),
        };
        CareerUiTheme.StyleSecondaryButton(renameBtn);
        renameBtn.Pressed += onRenameStadiumPressed;
        actionsRow.AddChild(renameBtn);

        return root;
    }

    private static Control BuildCampusBuildingTile(
        CampusBuildingKind buildingKind,
        string title,
        string subtitle,
        string bonusText,
        int level,
        Color accent)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 86),
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.055f, 0.088f, 0.135f, 0.96f),
            BorderColor = new Color(accent.R, accent.G, accent.B, 0.58f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        hbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(hbox);

        var illustration = new CampusBuildingCanvas(buildingKind, level, accent)
        {
            CustomMinimumSize = new Vector2(58, 58),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        hbox.AddChild(illustration);

        var vbox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        vbox.AddThemeConstantOverride("separation", 2);
        hbox.AddChild(vbox);

        var topRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        topRow.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(topRow);

        var titleLbl = new Label
        {
            Text = title,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        CareerUiTheme.StyleHeadline(titleLbl);
        titleLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        titleLbl.AddThemeColorOverride("font_color", CareerUiTheme.TextPrimary);
        topRow.AddChild(titleLbl);

        var starsLbl = new Label
        {
            Text = new string('★', level) + new string('☆', Math.Max(0, 5 - level)),
        };
        CareerUiTheme.StyleTableText(starsLbl);
        starsLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        starsLbl.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        topRow.AddChild(starsLbl);

        var subLbl = new Label
        {
            Text = subtitle,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        CareerUiTheme.StyleBody(subLbl, muted: true);
        subLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        vbox.AddChild(subLbl);

        var bonusPill = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
        };
        bonusPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(accent, 0.22f));
        var bonusLbl = new Label { Text = bonusText };
        CareerUiTheme.StyleTableText(bonusLbl);
        bonusLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        bonusLbl.AddThemeColorOverride("font_color", accent);
        bonusPill.AddChild(bonusLbl);
        vbox.AddChild(bonusPill);

        return panel;
    }

    public static Control BuildCollectiblePlayerCard(
        PlayerManagementLine player,
        int estimatedMarketValue,
        bool isSelected,
        bool isNewSigning,
        Action<long> onSelectCard,
        Action<long> onOpenProfile,
        Action<long> onQuickPromise)
    {
        var posColor = CareerUiTheme.PositionGroupColor(player.PositionCode);
        var card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(168, 174),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };

        var borderColor = isSelected
            ? CareerUiTheme.Accent
            : isNewSigning
                ? CareerUiTheme.ActionBright
                : new Color(posColor.R, posColor.G, posColor.B, 0.65f);

        var cardStyle = new StyleBoxFlat
        {
            BgColor = isSelected
                ? new Color(0.09f, 0.14f, 0.21f, 0.98f)
                : new Color(0.055f, 0.085f, 0.13f, 0.96f),
            BorderColor = borderColor,
            BorderWidthLeft = isSelected ? 3 : 2,
            BorderWidthTop = isSelected ? 4 : 3,
            BorderWidthRight = isSelected ? 3 : 2,
            BorderWidthBottom = isSelected ? 3 : 2,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ContentMarginLeft = 9,
            ContentMarginRight = 9,
            ContentMarginTop = 7,
            ContentMarginBottom = 7,
            ShadowColor = new Color(0f, 0f, 0f, 0.42f),
            ShadowSize = isSelected ? 8 : 4,
        };
        card.AddThemeStyleboxOverride("panel", cardStyle);

        var vbox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        vbox.AddThemeConstantOverride("separation", 4);
        card.AddChild(vbox);

        // 1. Header Row: Position Pill + Shirt Number + Big OVR Badge
        var topRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        topRow.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(topRow);

        var posPill = new PanelContainer();
        posPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(posColor, 0.26f));
        var posLbl = new Label { Text = $"{player.PositionCode} #{player.SquadNumber}" };
        CareerUiTheme.StyleTableText(posLbl);
        posLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        posLbl.AddThemeColorOverride("font_color", posColor);
        posPill.AddChild(posLbl);
        topRow.AddChild(posPill);

        topRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        var ovrPill = new PanelContainer();
        var ovrColor = player.Rating >= 78
            ? CareerUiTheme.Accent
            : player.Rating >= 70
                ? CareerUiTheme.ActionBright
                : CareerUiTheme.Data;
        ovrPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(ovrColor, 0.28f));
        var ovrLbl = new Label { Text = $"★ {player.Rating}" };
        CareerUiTheme.StyleHeadline(ovrLbl);
        ovrLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        ovrLbl.AddThemeColorOverride("font_color", ovrColor);
        ovrPill.AddChild(ovrLbl);
        topRow.AddChild(ovrPill);

        // 2. Player Name & Age/Value Subtitle
        var nameBtn = new Button
        {
            Text = player.DisplayName,
            Flat = true,
            Alignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        CareerUiTheme.ApplyDisplayFont(nameBtn);
        nameBtn.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        nameBtn.AddThemeColorOverride("font_color", CareerUiTheme.TextPrimary);
        nameBtn.Pressed += () =>
        {
            var tween = card.CreateTween();
            tween.TweenProperty(card, "scale", new Vector2(0.96f, 0.96f), 0.05d);
            tween.TweenProperty(card, "scale", Vector2.One, 0.10d);
            onSelectCard(player.PlayerId);
        };
        vbox.AddChild(nameBtn);

        var valStr = estimatedMarketValue >= 1_000_000
            ? $"{(double)estimatedMarketValue / 1_000_000:0.0}M ₺"
            : $"{estimatedMarketValue / 1_000:N0}B ₺";
        var relBadge = player.RelationshipState switch
        {
            "Güçlü" => "🟢 Moral İyi",
            "Kırılgan" => "🔴 Kırılgan",
            _ => "⚪ Dengeli",
        };
        var metaLbl = new Label
        {
            Text = $"{player.Age} Yaş · 💎 {valStr} · {relBadge}",
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        CareerUiTheme.StyleBody(metaLbl, muted: true);
        metaLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        vbox.AddChild(metaLbl);

        // 3. Visual Fitness Bar (New Star Manager / Top Eleven style)
        var fitRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        fitRow.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(fitRow);

        var fitColor = player.Fitness >= 80
            ? CareerUiTheme.ActionBright
            : player.Fitness >= 62
                ? CareerUiTheme.ColorWarning
                : CareerUiTheme.DangerSoft;

        var fitLbl = new Label { Text = $"⚡ %{player.Fitness}" };
        CareerUiTheme.StyleTableText(fitLbl);
        fitLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        fitLbl.AddThemeColorOverride("font_color", fitColor);
        fitRow.AddChild(fitLbl);

        var fitBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = Math.Clamp(player.Fitness, 0, 100),
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 7),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        fitBar.AddThemeStyleboxOverride("background", CareerUiTheme.ProgressBarBackground());
        fitBar.AddThemeStyleboxOverride("fill", CareerUiTheme.ProgressBarFill(fitColor));
        fitRow.AddChild(fitBar);

        // 4. Status / Availability Pill
        var statusText = player.Availability == "Hazır"
            ? (player.PromiseSummary == "aktif söz yok" ? "✓ Maça Hazır" : $"🤝 {player.PromiseSummary}")
            : $"⚠ {player.Availability}";
        var statusLbl = new Label
        {
            Text = statusText,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        CareerUiTheme.StyleBody(statusLbl, muted: player.Availability == "Hazır");
        statusLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        if (player.Availability != "Hazır")
        {
            statusLbl.AddThemeColorOverride("font_color", CareerUiTheme.DangerSoft);
        }
        vbox.AddChild(statusLbl);

        // 5. Tactile 1-Tap Action Buttons on the Card
        var actionRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        actionRow.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(actionRow);

        var profileBtn = new Button
        {
            Text = "🔍 Dosya",
            CustomMinimumSize = new Vector2(0, 26),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleSecondaryButton(profileBtn);
        profileBtn.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        profileBtn.Pressed += () => onOpenProfile(player.PlayerId);
        actionRow.AddChild(profileBtn);

        var promiseBtn = new Button
        {
            Text = "🤝 Söz",
            TooltipText = "Oyuncuya süre sözü vererek moralini yükselt",
            CustomMinimumSize = new Vector2(0, 26),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StylePrimaryButton(promiseBtn);
        promiseBtn.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        promiseBtn.Pressed += () => onQuickPromise(player.PlayerId);
        actionRow.AddChild(promiseBtn);

        return card;
    }

    public static PanelContainer BuildStadiumNamingModal(string clubName, Action onSaved, Action onClose)
    {
        var backdrop = new PanelContainer
        {
            Name = "StadiumNamingModal",
            Visible = false,
            ZIndex = 130,
        };
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        backdrop.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.03f, 0.06f, 0.85f),
            ContentMarginLeft = 40,
            ContentMarginRight = 40,
            ContentMarginTop = 36,
            ContentMarginBottom = 36,
        });

        var card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(460, 250),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        card.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized: true));
        backdrop.AddChild(card);

        var vbox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        vbox.AddThemeConstantOverride("separation", 10);
        card.AddChild(vbox);

        var title = new Label { Text = "🏟️ STADYUM & TRİBÜN İSİMLENDİRME (KULÜP KÜLTÜRÜ)" };
        CareerUiTheme.StyleHeadline(title);
        title.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        title.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        vbox.AddChild(title);

        var desc = new Label
        {
            Text = "Football Chairman Pro tarzı stadyum kompleksine ve kale arkası taraftar tribününe kulüp efsanelerinin adını ver:",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(desc, muted: true);
        vbox.AddChild(desc);

        var stadInput = new LineEdit
        {
            Text = _customStadiumTitle,
            PlaceholderText = "Stadyum Kompleksi Adı",
            CustomMinimumSize = new Vector2(0, 36),
        };
        vbox.AddChild(stadInput);

        var standInput = new LineEdit
        {
            Text = _customNorthStandName,
            PlaceholderText = "Kale Arkası / Kuzey Tribünü Adı",
            CustomMinimumSize = new Vector2(0, 36),
        };
        vbox.AddChild(standInput);

        var btnRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        btnRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(btnRow);

        var saveBtn = new Button
        {
            Text = "✓ İsimleri Kaydet & Tabelaya As",
            CustomMinimumSize = new Vector2(0, 36),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StylePrimaryButton(saveBtn);
        saveBtn.Pressed += () =>
        {
            SetCustomStadiumNaming(stadInput.Text, standInput.Text);
            backdrop.Visible = false;
            onSaved();
        };
        btnRow.AddChild(saveBtn);

        var cancelBtn = new Button
        {
            Text = "✕ Vazgeç",
            CustomMinimumSize = new Vector2(0, 36),
        };
        CareerUiTheme.StyleSecondaryButton(cancelBtn);
        cancelBtn.Pressed += () =>
        {
            backdrop.Visible = false;
            onClose();
        };
        btnRow.AddChild(cancelBtn);

        return backdrop;
    }

    private enum CampusBuildingKind
    {
        Stadium,
        Training,
        Academy,
        Medical,
    }

    private sealed partial class CampusBuildingCanvas : Control
    {
        private readonly CampusBuildingKind _kind;
        private readonly int _level;
        private readonly Color _accent;

        public CampusBuildingCanvas(CampusBuildingKind kind, int level, Color accent)
        {
            _kind = kind;
            _level = Math.Clamp(level, 1, 5);
            _accent = accent;
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, Size);
            DrawRect(rect, new Color(0.04f, 0.09f, 0.08f, 0.95f), filled: true);
            DrawRect(rect, new Color(_accent.R, _accent.G, _accent.B, 0.45f), filled: false, width: 1.5f);

            switch (_kind)
            {
                case CampusBuildingKind.Stadium:
                    DrawStadiumIcon(rect);
                    break;
                case CampusBuildingKind.Training:
                    DrawTrainingIcon(rect);
                    break;
                case CampusBuildingKind.Academy:
                    DrawAcademyIcon(rect);
                    break;
                case CampusBuildingKind.Medical:
                    DrawMedicalIcon(rect);
                    break;
            }
        }

        private void DrawStadiumIcon(Rect2 r)
        {
            // Outer bowl / roof tiers scale with level
            var bowlRect = r.Grow(-5f);
            DrawRect(bowlRect, new Color(0.18f, 0.24f, 0.34f, 0.95f), filled: true);
            DrawRect(bowlRect, _accent, filled: false, width: 2f);

            // Inner green pitch
            var pitchRect = r.Grow(-14f);
            DrawRect(pitchRect, new Color(0.12f, 0.56f, 0.28f, 1f), filled: true);
            DrawRect(pitchRect, new Color(1f, 1f, 1f, 0.75f), filled: false, width: 1f);
            DrawCircle(pitchRect.GetCenter(), 4f, new Color(1f, 1f, 1f, 0.65f));

            // Floodlight towers at corners if level >= 2
            if (_level >= 2)
            {
                var lightColor = new Color(1f, 0.92f, 0.45f, 0.95f);
                DrawCircle(new Vector2(r.Position.X + 7, r.Position.Y + 7), 3f, lightColor);
                DrawCircle(new Vector2(r.End.X - 7, r.Position.Y + 7), 3f, lightColor);
                if (_level >= 4)
                {
                    DrawCircle(new Vector2(r.Position.X + 7, r.End.Y - 7), 3f, lightColor);
                    DrawCircle(new Vector2(r.End.X - 7, r.End.Y - 7), 3f, lightColor);
                }
            }
        }

        private void DrawTrainingIcon(Rect2 r)
        {
            // Twin training turf fields + gym building
            var turf = new Rect2(r.Position.X + 6, r.Position.Y + 22, r.Size.X - 12, r.Size.Y - 28);
            DrawRect(turf, new Color(0.10f, 0.52f, 0.26f, 0.95f), filled: true);
            DrawLine(new Vector2(turf.GetCenter().X, turf.Position.Y), new Vector2(turf.GetCenter().X, turf.End.Y), new Color(1f, 1f, 1f, 0.6f), 1f);

            // Gym roof at top
            var roof = new Rect2(r.Position.X + 8, r.Position.Y + 6, r.Size.X - 16, 13);
            DrawRect(roof, _accent, filled: true);

            // Training cones
            for (var i = 0; i < Math.Min(_level + 1, 5); i++)
            {
                var cx = turf.Position.X + 6 + i * 8;
                DrawCircle(new Vector2(cx, turf.GetCenter().Y), 2.3f, new Color(1f, 0.72f, 0.15f));
            }
        }

        private void DrawAcademyIcon(Rect2 r)
        {
            // Modern academy building + star crest
            var baseRect = new Rect2(r.Position.X + 8, r.Position.Y + 20, r.Size.X - 16, r.Size.Y - 26);
            DrawRect(baseRect, new Color(0.12f, 0.26f, 0.42f, 0.95f), filled: true);
            DrawRect(baseRect, _accent, filled: false, width: 1.5f);

            // Windows grid
            for (var row = 0; row < 2; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var win = new Rect2(baseRect.Position.X + 5 + col * 11, baseRect.Position.Y + 5 + row * 10, 7, 6);
                    DrawRect(win, new Color(0.55f, 0.88f, 1f, 0.85f), filled: true);
                }
            }

            // Rising star dome at top
            DrawCircle(new Vector2(r.GetCenter().X, r.Position.Y + 12), 5.5f, CareerUiTheme.Accent);
        }

        private void DrawMedicalIcon(Rect2 r)
        {
            var clinic = r.Grow(-9f);
            DrawRect(clinic, new Color(0.08f, 0.24f, 0.24f, 0.95f), filled: true);
            DrawRect(clinic, _accent, filled: false, width: 1.5f);

            // Emerald/White Medical Cross
            var c = clinic.GetCenter();
            var vBar = new Rect2(c.X - 4, c.Y - 12, 8, 24);
            var hBar = new Rect2(c.X - 12, c.Y - 4, 24, 8);
            DrawRect(vBar, _accent, filled: true);
            DrawRect(hBar, _accent, filled: true);
        }
    }
}
