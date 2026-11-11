using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Mini perspektif saha üzerinde 11 oyuncu noktasını ve taktiksel şablonu çizen görsel kart.
/// </summary>
internal sealed partial class FormationCard : Button
{
    public Formation Formation { get; }
    public bool IsSelected { get; set; }

    private readonly Vector2[] _positions;
    private readonly string _label;

    public FormationCard(Formation formation, bool isSelected)
    {
        Formation = formation;
        IsSelected = isSelected;
        _positions = TacticalPitchBoard.GetPositionsForFormation(formation);
        _label = FormatLabel(formation);

        CustomMinimumSize = new Vector2(136, 126);
        Flat = true;
        FocusMode = FocusModeEnum.None;
        MouseDefaultCursorShape = CursorShape.PointingHand;
    }

    public static string FormatLabel(Formation formation) => formation switch
    {
        Formation.F442 => "4-4-2",
        Formation.F433 => "4-3-3",
        Formation.F352 => "3-5-2",
        Formation.F4231 => "4-2-3-1",
        Formation.F4312 => "4-3-1-2",
        Formation.F4222 => "4-2-2-2",
        Formation.F343 => "3-4-3",
        Formation.F3241 => "3-2-4-1",
        Formation.F3331 => "3-3-3-1",
        Formation.F4141 => "4-1-4-1",
        Formation.F532 => "5-3-2",
        Formation.F523 => "5-2-3",
        Formation.F424 => "4-2-4",
        Formation.F451 => "4-5-1",
        _ => formation.ToString(),
    };

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);

        // 1. Kart Zemin Paneli
        var bg = IsSelected
            ? new Color(0.12f, 0.20f, 0.32f, 0.95f)
            : (IsHovered() ? new Color(0.09f, 0.16f, 0.26f, 0.90f) : new Color(0.06f, 0.11f, 0.19f, 0.85f));
        var border = IsSelected
            ? new Color(0.98f, 0.78f, 0.20f, 1f) // Parlak Altın Sarısı
            : (IsHovered() ? new Color(0.35f, 0.55f, 0.80f, 0.80f) : new Color(0.18f, 0.28f, 0.42f, 0.60f));

        var borderWidth = IsSelected ? 2.5f : 1.5f;
        DrawRect(rect, bg, filled: true);
        DrawRect(rect, border, filled: false, width: borderWidth);

        // 2. Perspektif Saha Çizimi (Üstü hafif dar, altı geniş perspektif)
        var pitchTopY = 10f;
        var pitchBottomY = 92f;

        var topMargin = 26f;
        var bottomMargin = 16f;

        var pTopLeft = new Vector2(topMargin, pitchTopY);
        var pTopRight = new Vector2(Size.X - topMargin, pitchTopY);
        var pBottomLeft = new Vector2(bottomMargin, pitchBottomY);
        var pBottomRight = new Vector2(Size.X - bottomMargin, pitchBottomY);

        // Saha Gövdesi (Lacivert/Yeşil Taktik Çimi)
        Vector2[] pitchPolygon = [pTopLeft, pTopRight, pBottomRight, pBottomLeft];
        Color[] colors = [
            new(0.05f, 0.14f, 0.24f, 0.95f),
            new(0.05f, 0.14f, 0.24f, 0.95f),
            new(0.07f, 0.18f, 0.30f, 0.95f),
            new(0.07f, 0.18f, 0.30f, 0.95f)
        ];
        DrawPolygon(pitchPolygon, colors);

        // Taktik Saha Çizgileri (Hafif saydam cam mavisi)
        var lineCol = new Color(0.36f, 0.58f, 0.82f, 0.55f);
        DrawPolyline([pTopLeft, pTopRight, pBottomRight, pBottomLeft, pTopLeft], lineCol, 1.2f);

        // Orta Saha Çizgisi
        var midT = 0.50f;
        var midLeft = pTopLeft.Lerp(pBottomLeft, midT);
        var midRight = pTopRight.Lerp(pBottomRight, midT);
        DrawLine(midLeft, midRight, lineCol, 1.2f);

        // Orta Yuvarlak (Perspektif elips)
        var centerPos = midLeft.Lerp(midRight, 0.5f);
        DrawArc(centerPos, 10f, 0, Mathf.Tau, 24, lineCol, 1.0f);

        // 3. 11 Oyuncu Noktası Çizimi (Altın Sarısı / Amber Noktalar)
        var dotColor = IsSelected ? new Color(1.0f, 0.88f, 0.25f) : new Color(0.96f, 0.74f, 0.18f);
        var dotGlow = new Color(dotColor.R, dotColor.G, dotColor.B, 0.30f);

        foreach (var pos in _positions)
        {
            // pos.X: [0..1] sol-sağ, pos.Y: [0..1] hücum-kale
            var yRatio = pos.Y;
            var currentLeft = pTopLeft.Lerp(pBottomLeft, yRatio);
            var currentRight = pTopRight.Lerp(pBottomRight, yRatio);
            var dotX = Mathf.Lerp(currentLeft.X, currentRight.X, pos.X);
            var dotY = Mathf.Lerp(pitchTopY, pitchBottomY, yRatio);
            var dotPos = new Vector2(dotX, dotY);

            // Parlama ve ana nokta
            DrawCircle(dotPos, 4.0f, dotGlow);
            DrawCircle(dotPos, 2.6f, dotColor);
        }

        // 4. Diziliş Adı Etiketi
        var font = ThemeDB.FallbackFont;
        var fontSize = 13;
        var labelText = _label;
        var labelSize = font.GetStringSize(labelText, HorizontalAlignment.Center, -1, fontSize);
        var labelPos = new Vector2((Size.X - labelSize.X) * 0.5f, pitchBottomY + 22f);
        var fontColor = IsSelected ? new Color(1f, 0.88f, 0.25f) : new Color(0.90f, 0.94f, 0.98f);
        DrawString(font, labelPos, labelText, HorizontalAlignment.Center, -1, fontSize, fontColor);
    }
}

/// <summary>
/// 14 farklı taktiksel dizilişi minyatür sahalarla gösteren ve seçimi yöneten modal panel.
/// </summary>
internal sealed partial class FormationPickerModal : PanelContainer
{
    public static string FormatLabel(Formation formation) => FormationCard.FormatLabel(formation);

    public static void Open(
        Control parent,
        Formation currentFormation,
        Action<Formation> onFormationSelected)
    {
        var modal = new FormationPickerModal(currentFormation, onFormationSelected);
        parent.AddChild(modal);
        modal.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MatchScreenUi.FadeIn(modal, parent);
    }

    private static readonly Formation[] AllFormations =
    [
        Formation.F442,
        Formation.F4312,
        Formation.F4231,
        Formation.F4222,
        Formation.F433,
        Formation.F343,
        Formation.F352,
        Formation.F3331,
        Formation.F3241,
        Formation.F4141,
        Formation.F532,
        Formation.F523,
        Formation.F424,
        Formation.F451,
    ];

    private readonly Formation _currentFormation;
    private readonly Action<Formation> _onFormationSelected;

    public FormationPickerModal(Formation currentFormation, Action<Formation> onFormationSelected)
    {
        _currentFormation = currentFormation;
        _onFormationSelected = onFormationSelected;

        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.04f, 0.08f, 0.82f),
        });

        BuildUi();
    }

    private void BuildUi()
    {
        var centerBox = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        centerBox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(centerBox);

        // Ana Modal Kartı
        var card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(800, 500),
        };
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.11f, 0.18f, 0.98f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderColor = new Color(0.24f, 0.38f, 0.56f, 0.70f),
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            ShadowColor = new Color(0, 0, 0, 0.65f),
            ShadowSize = 16,
        });
        centerBox.AddChild(card);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        card.AddChild(margin);

        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 14);
        margin.AddChild(stack);

        // 1. Header: Başlık ve Kapat (✕) Butonu
        var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        stack.AddChild(header);

        var titleBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        titleBox.AddThemeConstantOverride("separation", 10);
        header.AddChild(titleBox);

        titleBox.AddChild(new BroadcastVectorIcon("tactics", CareerUiTheme.Accent, 18f));

        var titleLabel = new Label { Text = "DİZİLİŞLER" };
        CareerUiTheme.ApplyDisplayFont(titleLabel);
        titleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(20));
        titleLabel.AddThemeColorOverride("font_color", Colors.White);
        titleBox.AddChild(titleLabel);

        var hintLabel = new Label { Text = "(Taktiğine uygun formasyonu seç, oyuncular otomatik olarak mevkilerine göre dizilir)" };
        CareerUiTheme.ApplyBodyFont(hintLabel);
        hintLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        hintLabel.AddThemeColorOverride("font_color", CareerUiTheme.TextMuted);
        hintLabel.VerticalAlignment = VerticalAlignment.Center;
        titleBox.AddChild(hintLabel);

        var closeButton = new Button
        {
            Text = "Kapat",
            CustomMinimumSize = new Vector2(56, 36),
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        CareerUiTheme.StyleSecondaryButton(closeButton);
        closeButton.Pressed += () => QueueFree();
        header.AddChild(closeButton);

        // 2. Formasyonlar Izgarası
        var scroll = new MobileScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            CustomMinimumSize = new Vector2(760, 410),
        };
        stack.AddChild(scroll);

        var grid = new GridContainer
        {
            Columns = 5,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        grid.AddThemeConstantOverride("h_separation", 14);
        grid.AddThemeConstantOverride("v_separation", 14);
        scroll.AddChild(grid);

        foreach (var formation in AllFormations)
        {
            var isSelected = formation == _currentFormation;
            var formationCard = new FormationCard(formation, isSelected);
            formationCard.Pressed += () =>
            {
                _onFormationSelected?.Invoke(formation);
                QueueFree();
            };
            grid.AddChild(formationCard);
        }
    }
}
