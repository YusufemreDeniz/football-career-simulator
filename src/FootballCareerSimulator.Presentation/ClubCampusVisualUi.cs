using System;
using System.Collections.Generic;
using FootballCareerSimulator.Application.CareerHub.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Broadcast Stadium Noir tasarım sistemi:
/// 2D görsel Kulüp Kampüsü çizimi ve 140x200px dikey Top Drives / FUT Koleksiyon Oyuncu Kartları
/// + Karta dokunulduğunda (Tap) açılan şık Alt Çekmece (Bottom Sheet Modal).
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
            title: "STADYUM & TRİBÜN",
            subtitle: $"{_customNorthStandName} · {stadiumCapacity:N0} Koltuk",
            bonusText: $"+%{attendancePercent} Doluluk",
            level: lvl,
            accent: CareerUiTheme.Accent));

        buildingsGrid.AddChild(BuildCampusBuildingTile(
            buildingKind: CampusBuildingKind.Training,
            title: "ANTRENMAN MERKEZİ",
            subtitle: "Taktik & Kondisyon Sahaları",
            bonusText: $"+%{Math.Max(8, growthPct)} Gelişim",
            level: lvl,
            accent: CareerUiTheme.ActionBright));

        buildingsGrid.AddChild(BuildCampusBuildingTile(
            buildingKind: CampusBuildingKind.Academy,
            title: "GENÇLİK AKADEMİSİ",
            subtitle: "U21 Yetenek Tarama Kampüsü",
            bonusText: $"+Lv.{lvl} Potansiyel",
            level: lvl,
            accent: CareerUiTheme.Data));

        buildingsGrid.AddChild(BuildCampusBuildingTile(
            buildingKind: CampusBuildingKind.Medical,
            title: "SAĞLIK & FİZYO SPA",
            subtitle: "Rehabilitasyon & Hiperbarik Oda",
            bonusText: $"+%{lvl * 6} Yenilenme",
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
            Text = "Tribün & Stad Adı",
            TooltipText = "Stadyum ve tribün isimlendirme ekranını aç",
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
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: false));

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

        var levelPill = new PanelContainer();
        levelPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.22f));
        var levelLbl = new Label { Text = $"SV.{level}/5" };
        CareerUiTheme.StyleTableText(levelLbl);
        levelLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        levelLbl.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        levelPill.AddChild(levelLbl);
        topRow.AddChild(levelPill);

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

    /// <summary>
    /// Kural 3: 140x200px dikey Top Drives / EA FC (FUT) Koleksiyon Oyuncu Kartı.
    /// Kart üzerindeki "Dosya" ve "Söz" metin butonları tamamen kaldırıldı.
    /// OVR gücüne göre Altın/Gümüş/Bronz çerçeve gradyanı, sol üstte dev OVR ve Mevki rozeti,
    /// ortada forma/oyuncu silüeti (_Draw), en altta ince entegre kondisyon çizgisi.
    /// Karta dokunulduğunda (Tap) detaylar alt çekmece (Bottom Sheet Modal) olarak açılır.
    /// </summary>
    public static Control BuildCollectiblePlayerCard(
        PlayerManagementLine player,
        int estimatedMarketValue,
        bool isSelected,
        bool isNewSigning,
        Action<long> onSelectCard,
        Action<long> onOpenProfile,
        Action<long> onQuickPromise)
    {
        var cardButton = new CollectibleFutCardButton(
            player,
            estimatedMarketValue,
            isSelected,
            isNewSigning);

        cardButton.Pressed += () =>
        {
            var tween = cardButton.CreateTween();
            tween.TweenProperty(cardButton, "scale", new Vector2(0.95f, 0.95f), 0.05d);
            tween.TweenProperty(cardButton, "scale", Vector2.One, 0.10d);
            onSelectCard(player.PlayerId);
        };

        return cardButton;
    }

    /// <summary>
    /// Karta dokunulduğunda (Tap) ekranın altından kayarak açılan şık Alt Çekmece (Bottom Sheet Modal).
    /// </summary>
    public static PanelContainer BuildPlayerBottomSheetModal(
        PlayerManagementLine player,
        int estimatedMarketValue,
        Action<long> onOpenDossier,
        Action<long> onPromiseStarter,
        Action<long> onPromiseMinutes,
        Action onClose)
    {
        var backdrop = new PanelContainer
        {
            Name = "PlayerBottomSheetModal",
            Visible = true,
            ZIndex = 135,
        };
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        backdrop.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.03f, 0.06f, 0.72f),
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 24,
            ContentMarginBottom = 0,
        });

        var alignBox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.End,
        };
        backdrop.AddChild(alignBox);

        var sheet = new PanelContainer
        {
            CustomMinimumSize = new Vector2(680, 230),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        sheet.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.075f, 0.118f, 0.188f, 0.96f),
            BorderColor = CareerUiTheme.SpecularBorder,
            BorderWidthTop = 2,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 20,
            CornerRadiusTopRight = 20,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 14,
            ContentMarginBottom = 18,
            ShadowColor = new Color(0f, 0f, 0f, 0.65f),
            ShadowSize = 18,
            ShadowOffset = new Vector2(0, -4),
        });
        alignBox.AddChild(sheet);

        var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 10);
        sheet.AddChild(root);

        // Drag handle indicator
        var handleWrap = new CenterContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var handleBar = new ColorRect
        {
            CustomMinimumSize = new Vector2(48, 4),
            Color = new Color(CareerUiTheme.InkMuted.R, CareerUiTheme.InkMuted.G, CareerUiTheme.InkMuted.B, 0.45f),
        };
        handleWrap.AddChild(handleBar);
        root.AddChild(handleWrap);

        // Top Player Identity Row
        var headerRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        headerRow.AddThemeConstantOverride("separation", 12);
        root.AddChild(headerRow);

        var tierColor = player.Rating >= 78
            ? CareerUiTheme.Accent
            : player.Rating >= 70
                ? new Color(0.80f, 0.86f, 0.94f, 1f)
                : new Color(0.80f, 0.52f, 0.30f, 1f);

        var ovrBadge = new PanelContainer();
        ovrBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(tierColor, 0.22f));
        var ovrLbl = new Label { Text = $"{player.Rating} OVR · {player.PositionCode}" };
        CareerUiTheme.StyleHeadline(ovrLbl);
        ovrLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(16));
        ovrLbl.AddThemeColorOverride("font_color", tierColor);
        ovrBadge.AddChild(ovrLbl);
        headerRow.AddChild(ovrBadge);

        var titleCol = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        titleCol.AddThemeConstantOverride("separation", 2);
        headerRow.AddChild(titleCol);

        var nameLbl = new Label { Text = $"#{player.SquadNumber}  {player.DisplayName}" };
        CareerUiTheme.StyleHeadline(nameLbl);
        nameLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(18));
        titleCol.AddChild(nameLbl);

        var valStr = estimatedMarketValue >= 1_000_000
            ? $"{(double)estimatedMarketValue / 1_000_000:0.0}M TRY"
            : $"{estimatedMarketValue:N0} TRY";
        var subLbl = new Label
        {
            Text = $"{player.Age} Yaş · Piyasa Değeri: {valStr} · Durum: {player.Availability} · İlişki: {player.RelationshipState}",
        };
        CareerUiTheme.StyleBody(subLbl, muted: true);
        subLbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        titleCol.AddChild(subLbl);

        var closeBtn = new Button
        {
            Text = "KAPAT",
            CustomMinimumSize = new Vector2(84, 34),
        };
        CareerUiTheme.StyleSecondaryButton(closeBtn);
        closeBtn.Pressed += () =>
        {
            backdrop.Visible = false;
            onClose();
        };
        headerRow.AddChild(closeBtn);

        // Metrics Strip
        var metricsGrid = UiComponents.MetricGrid(
            ("KONDİSYON", $"%{player.Fitness}", CareerUiTheme.FitnessColor(player.Fitness)),
            ("MEVCUT GÜÇ (CA)", player.CurrentAbility.ToString(), CareerUiTheme.ActionBright),
            ("POTANSİYEL (PA)", player.PotentialAbility.ToString(), CareerUiTheme.Accent),
            ("FORMA SÖZÜ", player.PromiseSummary, CareerUiTheme.Data));
        root.AddChild(metricsGrid);

        // Bottom Sheet Action Row
        var actionRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        actionRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(actionRow);

        var dossierBtn = new Button
        {
            Text = "Oyuncu Dosyası & Görüşme Aç",
            CustomMinimumSize = new Vector2(0, 38),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StylePrimaryButton(dossierBtn);
        dossierBtn.Pressed += () =>
        {
            backdrop.Visible = false;
            onOpenDossier(player.PlayerId);
        };
        actionRow.AddChild(dossierBtn);

        var starterPromiseBtn = new Button
        {
            Text = "İlk 11 Sözü Ver",
            CustomMinimumSize = new Vector2(0, 38),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleAccentButton(starterPromiseBtn);
        starterPromiseBtn.Pressed += () =>
        {
            backdrop.Visible = false;
            onPromiseStarter(player.PlayerId);
        };
        actionRow.AddChild(starterPromiseBtn);

        var timePromiseBtn = new Button
        {
            Text = "Süre Sözü Ver",
            CustomMinimumSize = new Vector2(0, 38),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleSecondaryButton(timePromiseBtn);
        timePromiseBtn.Pressed += () =>
        {
            backdrop.Visible = false;
            onPromiseMinutes(player.PlayerId);
        };
        actionRow.AddChild(timePromiseBtn);

        return backdrop;
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

        var title = new Label { Text = "STADYUM & TRİBÜN İSİMLENDİRME (KULÜP KÜLTÜRÜ)" };
        CareerUiTheme.StyleHeadline(title);
        title.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        title.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        vbox.AddChild(title);

        var desc = new Label
        {
            Text = "Stadyum kompleksine ve kale arkası taraftar tribününe kulüp efsanelerinin adını ver:",
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
            Text = "İsimleri Kaydet & Tabelaya As",
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
            Text = "Vazgeç",
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

    /// <summary>
    /// 140x200px dikey FUT / Top Drives koleksiyon oyuncu kartı çizimi (_Draw()).
    /// </summary>
    private sealed partial class CollectibleFutCardButton : Button
    {
        private readonly PlayerManagementLine _player;
        private readonly int _marketValue;
        private readonly bool _isSelected;
        private readonly bool _isNewSigning;

        public CollectibleFutCardButton(
            PlayerManagementLine player,
            int marketValue,
            bool isSelected,
            bool isNewSigning)
        {
            _player = player;
            _marketValue = marketValue;
            _isSelected = isSelected;
            _isNewSigning = isNewSigning;

            CustomMinimumSize = new Vector2(140, 200);
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            Flat = true;
            Text = string.Empty;
            PivotOffset = new Vector2(70, 100);
            TooltipText = $"{player.DisplayName} ({player.PositionCode}) — Detay çekmecesini açmak için dokun";
        }

        public override void _Draw()
        {
            var rect = new Rect2(Vector2.Zero, Size);
            if (rect.Size.X <= 4 || rect.Size.Y <= 4)
            {
                rect = new Rect2(0, 0, 140, 200);
            }

            // OVR kademesine göre Altın / Gümüş / Bronz çerçeve renkleri
            Color frameTop;
            Color frameBottom;
            Color badgeBg;
            if (_player.Rating >= 78)
            {
                // Altın (Broadcast Gold)
                frameTop = new Color(1.0f, 0.78f, 0.12f, 1f);
                frameBottom = new Color(0.68f, 0.45f, 0.04f, 1f);
                badgeBg = new Color(0.26f, 0.18f, 0.03f, 0.92f);
            }
            else if (_player.Rating >= 70)
            {
                // Gümüş (Metallic Silver)
                frameTop = new Color(0.84f, 0.90f, 0.98f, 1f);
                frameBottom = new Color(0.42f, 0.50f, 0.62f, 1f);
                badgeBg = new Color(0.14f, 0.19f, 0.27f, 0.92f);
            }
            else
            {
                // Bronz (Metallic Bronze)
                frameTop = new Color(0.85f, 0.55f, 0.34f, 1f);
                frameBottom = new Color(0.46f, 0.25f, 0.13f, 1f);
                badgeBg = new Color(0.22f, 0.12f, 0.07f, 0.92f);
            }

            // 1. Yumuşak alt gölge
            DrawRect(new Rect2(rect.Position.X + 3, rect.Position.Y + 5, rect.Size.X - 2, rect.Size.Y - 2), new Color(0f, 0f, 0f, 0.52f), true);

            // 2. Kart Ana Gövdesi (#131E30 %85 opaklık cam zemin)
            var bgRect = rect.Grow(-1.5f);
            DrawRect(bgRect, new Color(0.075f, 0.118f, 0.188f, 0.92f), true);

            // Üst metalik FUT kalkan aksanı (köşeli üst şerit)
            var topCrown = new Vector2[]
            {
                new(bgRect.Position.X, bgRect.Position.Y),
                new(bgRect.End.X, bgRect.Position.Y),
                new(bgRect.End.X, bgRect.Position.Y + 42f),
                new(bgRect.GetCenter().X, bgRect.Position.Y + 56f),
                new(bgRect.Position.X, bgRect.Position.Y + 42f),
            };
            DrawColoredPolygon(topCrown, new Color(frameTop.R, frameTop.G, frameTop.B, 0.16f));

            // 3. Merkezde Forma / Oyuncu Silüeti (_Draw)
            var posColor = CareerUiTheme.PositionGroupColor(_player.PositionCode);
            var center = new Vector2(rect.Size.X * 0.56f, rect.Size.Y * 0.46f);

            // Arka plan hale
            DrawCircle(center, 34f, new Color(posColor.R, posColor.G, posColor.B, 0.12f));
            DrawArc(center, 34f, 0f, Mathf.Tau, 28, new Color(frameTop.R, frameTop.G, frameTop.B, 0.28f), 1.4f, true);

            // Oyuncu baş silüeti
            DrawCircle(center + new Vector2(0, -22f), 9.5f, new Color(0.86f, 0.91f, 0.98f, 0.90f));

            // Forma (Jersey) gövde ve kollar poligonu
            var jerseyPoly = new Vector2[]
            {
                center + new Vector2(-12f, -10f),
                center + new Vector2(-26f, -2f),
                center + new Vector2(-21f, 10f),
                center + new Vector2(-13f, 6f),
                center + new Vector2(-13f, 26f),
                center + new Vector2(13f, 26f),
                center + new Vector2(13f, 6f),
                center + new Vector2(21f, 10f),
                center + new Vector2(26f, -2f),
                center + new Vector2(12f, -10f),
            };
            DrawColoredPolygon(jerseyPoly, new Color(posColor.R, posColor.G, posColor.B, 0.85f));
            DrawPolyline(
                new Vector2[]
                {
                    jerseyPoly[0], jerseyPoly[1], jerseyPoly[2], jerseyPoly[3], jerseyPoly[4],
                    jerseyPoly[5], jerseyPoly[6], jerseyPoly[7], jerseyPoly[8], jerseyPoly[9], jerseyPoly[0],
                },
                frameTop,
                1.5f,
                true);

            // Forma yaka detayı
            DrawArc(center + new Vector2(0, -10f), 5.5f, 0.15f, Mathf.Pi - 0.15f, 12, CareerUiTheme.BackgroundDeep, 2f, true);

            var font = ThemeDB.FallbackFont;
            if (font is not null)
            {
                // Forma numarası (göğüste)
                var numStr = _player.SquadNumber.ToString();
                DrawString(
                    font,
                    new Vector2(center.X - 14f, center.Y + 14f),
                    numStr,
                    HorizontalAlignment.Center,
                    28f,
                    14,
                    Colors.White);

                // 4. Sol Üstte Dev OVR ve Mevki Rozeti
                var badgeRect = new Rect2(8f, 8f, 44f, 54f);
                DrawRect(badgeRect, badgeBg, true);
                DrawRect(badgeRect, frameTop, false, 1.5f);

                DrawString(
                    font,
                    new Vector2(badgeRect.Position.X, badgeRect.Position.Y + 24f),
                    _player.Rating.ToString(),
                    HorizontalAlignment.Center,
                    badgeRect.Size.X,
                    22,
                    frameTop);

                var posPillRect = new Rect2(badgeRect.Position.X + 4f, badgeRect.Position.Y + 31f, badgeRect.Size.X - 8f, 17f);
                DrawRect(posPillRect, posColor, true);
                DrawString(
                    font,
                    new Vector2(posPillRect.Position.X, posPillRect.Position.Y + 13f),
                    _player.PositionCode,
                    HorizontalAlignment.Center,
                    posPillRect.Size.X,
                    11,
                    CareerUiTheme.BackgroundDeep);

                // 5. Alt İsim Plakası ve Değer Özeti
                var plateRect = new Rect2(6f, rect.Size.Y - 56f, rect.Size.X - 12f, 42f);
                DrawRect(plateRect, new Color(0.04f, 0.07f, 0.12f, 0.92f), true);
                DrawLine(
                    new Vector2(plateRect.Position.X, plateRect.Position.Y),
                    new Vector2(plateRect.End.X, plateRect.Position.Y),
                    frameTop,
                    1.5f);

                var shortName = FormatShortCardName(_player.DisplayName);
                DrawString(
                    font,
                    new Vector2(plateRect.Position.X + 4f, plateRect.Position.Y + 18f),
                    shortName,
                    HorizontalAlignment.Center,
                    plateRect.Size.X - 8f,
                    13,
                    CareerUiTheme.Ink);

                var valTag = _marketValue >= 1_000_000
                    ? $"{(double)_marketValue / 1_000_000:0.0}M"
                    : $"{_marketValue / 1_000:N0}B";
                var metaText = $"{_player.Age}Y · {valTag} · CA {_player.CurrentAbility}";
                DrawString(
                    font,
                    new Vector2(plateRect.Position.X + 4f, plateRect.Position.Y + 34f),
                    metaText,
                    HorizontalAlignment.Center,
                    plateRect.Size.X - 8f,
                    10,
                    CareerUiTheme.InkMuted);
            }

            // 6. En Altta İnce Entegre Kondisyon Çizgisi
            var barBgRect = new Rect2(6f, rect.Size.Y - 10f, rect.Size.X - 12f, 5f);
            DrawRect(barBgRect, new Color(0f, 0f, 0f, 0.65f), true);
            var fitRatio = Mathf.Clamp(_player.Fitness / 100f, 0.05f, 1f);
            var fitColor = CareerUiTheme.FitnessColor(_player.Fitness);
            DrawRect(new Rect2(barBgRect.Position.X, barBgRect.Position.Y, barBgRect.Size.X * fitRatio, barBgRect.Size.Y), fitColor, true);

            // 7. Altın/Gümüş/Bronz Çerçeve Gradyanı (Üst parlak, alt koyu metalik sınır)
            var borderWidth = _isSelected ? 3f : 2f;
            var activeFrameTop = _isSelected ? CareerUiTheme.ActionBright : (_isNewSigning ? CareerUiTheme.Accent : frameTop);
            DrawLine(new Vector2(bgRect.Position.X, bgRect.Position.Y), new Vector2(bgRect.End.X, bgRect.Position.Y), activeFrameTop, borderWidth + 0.5f);
            DrawLine(new Vector2(bgRect.Position.X, bgRect.Position.Y), new Vector2(bgRect.Position.X, bgRect.End.Y), activeFrameTop, borderWidth);
            DrawLine(new Vector2(bgRect.End.X, bgRect.Position.Y), new Vector2(bgRect.End.X, bgRect.End.Y), frameBottom, borderWidth);
            DrawLine(new Vector2(bgRect.Position.X, bgRect.End.Y), new Vector2(bgRect.End.X, bgRect.End.Y), frameBottom, borderWidth);
        }

        private static string FormatShortCardName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "OYUNCU";
            }

            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].ToUpperInvariant();
            }

            var last = parts[^1].ToUpperInvariant();
            return $"{ char.ToUpperInvariant(parts[0][0]) }. {last}";
        }
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
            DrawRect(rect, new Color(0.04f, 0.08f, 0.14f, 0.95f), filled: true);
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
            var bowlRect = r.Grow(-5f);
            DrawRect(bowlRect, new Color(0.18f, 0.24f, 0.34f, 0.95f), filled: true);
            DrawRect(bowlRect, _accent, filled: false, width: 2f);

            var pitchRect = r.Grow(-14f);
            DrawRect(pitchRect, new Color(0.05f, 0.22f, 0.17f, 1f), filled: true);
            DrawRect(pitchRect, new Color(1f, 1f, 1f, 0.75f), filled: false, width: 1f);
            DrawCircle(pitchRect.GetCenter(), 4f, new Color(1f, 1f, 1f, 0.65f));

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
            var turf = new Rect2(r.Position.X + 6, r.Position.Y + 22, r.Size.X - 12, r.Size.Y - 28);
            DrawRect(turf, new Color(0.05f, 0.22f, 0.17f, 0.95f), filled: true);
            DrawLine(new Vector2(turf.GetCenter().X, turf.Position.Y), new Vector2(turf.GetCenter().X, turf.End.Y), new Color(1f, 1f, 1f, 0.6f), 1f);

            var roof = new Rect2(r.Position.X + 8, r.Position.Y + 6, r.Size.X - 16, 13);
            DrawRect(roof, _accent, filled: true);

            for (var i = 0; i < Math.Min(_level + 1, 5); i++)
            {
                var cx = turf.Position.X + 6 + i * 8;
                DrawCircle(new Vector2(cx, turf.GetCenter().Y), 2.3f, new Color(1f, 0.72f, 0.15f));
            }
        }

        private void DrawAcademyIcon(Rect2 r)
        {
            var baseRect = new Rect2(r.Position.X + 8, r.Position.Y + 20, r.Size.X - 16, r.Size.Y - 26);
            DrawRect(baseRect, new Color(0.12f, 0.26f, 0.42f, 0.95f), filled: true);
            DrawRect(baseRect, _accent, filled: false, width: 1.5f);

            for (var row = 0; row < 2; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var win = new Rect2(baseRect.Position.X + 5 + col * 11, baseRect.Position.Y + 5 + row * 10, 7, 6);
                    DrawRect(win, new Color(0.55f, 0.88f, 1f, 0.85f), filled: true);
                }
            }

            DrawCircle(new Vector2(r.GetCenter().X, r.Position.Y + 12), 5.5f, CareerUiTheme.Accent);
        }

        private void DrawMedicalIcon(Rect2 r)
        {
            var clinic = r.Grow(-9f);
            DrawRect(clinic, new Color(0.08f, 0.24f, 0.24f, 0.95f), filled: true);
            DrawRect(clinic, _accent, filled: false, width: 1.5f);

            var c = clinic.GetCenter();
            var vBar = new Rect2(c.X - 4, c.Y - 12, 8, 24);
            var hBar = new Rect2(c.X - 12, c.Y - 4, 24, 8);
            DrawRect(vBar, _accent, filled: true);
            DrawRect(hBar, _accent, filled: true);
        }
    }
}
