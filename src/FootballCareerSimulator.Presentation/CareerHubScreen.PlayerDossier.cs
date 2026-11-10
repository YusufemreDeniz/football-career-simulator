using FootballCareerSimulator.Application.CareerHub.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private Control _playerDossierOverlay = null!;
    private Label _playerDossierTitle = null!;
    private Label _playerDossierBody = null!;
    private Label _playerDossierNumberLabel = null!;
    private PanelContainer _playerDossierPositionPill = null!;
    private Label _playerDossierPositionLabel = null!;
    private Label _playerDossierSubtitleLabel = null!;
    private PanelContainer _playerDossierRatingBadge = null!;
    private Label _playerDossierRatingLabel = null!;
    private Label _playerDossierRatingCaption = null!;
    private Label _dossierAbilityLabel = null!;
    private ProgressBar _dossierAbilityBar = null!;
    private Label _dossierCareerPhaseLabel = null!;
    private Label _dossierFitnessLabel = null!;
    private ProgressBar _dossierFitnessBar = null!;
    private PanelContainer _dossierAvailabilityBadge = null!;
    private Label _dossierAvailabilityLabel = null!;
    private Label _dossierWorkloadLabel = null!;
    private Label _dossierContractWageLabel = null!;
    private Label _dossierContractEndLabel = null!;
    private Label _dossierRelationshipLabel = null!;
    private Label _dossierSocialMetricsLabel = null!;
    private Label _dossierPromiseLabel = null!;
    private Label _dossierCausalityLabel = null!;
    private Button _playerDossierSellButton = null!;
    private Button _playerDossierPromiseButton = null!;
    private Button _playerDossierDecisionButton = null!;
    private Button _playerDossierMeetingButton = null!;
    private PanelContainer _privateMeetingPanel = null!;
    private Label _privateMeetingResultLabel = null!;

    private static VBoxContainer AddDossierCard(Control parent, string title)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized: false));
        parent.AddChild(panel);

        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        content.AddThemeConstantOverride("separation", 6);
        panel.AddChild(content);

        var titleLabel = BodyLabel($"H_{title}");
        titleLabel.Text = title;
        CareerUiTheme.ApplyDisplayFont(titleLabel);
        titleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        titleLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        content.AddChild(titleLabel);

        return content;
    }

    private void BuildPlayerDossierOverlay()
    {
        _playerDossierOverlay = new Control
        {
            Name = "PlayerDossierOverlay",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _playerDossierOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_playerDossierOverlay);

        var dim = new ColorRect
        {
            Color = new Color(0.005f, 0.02f, 0.025f, 0.88f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        dim.GuiInput += OnPlayerDossierBackdropInput;
        _playerDossierOverlay.AddChild(dim);

        var center = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _playerDossierOverlay.AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(720, 540),
            MouseFilter = MouseFilterEnum.Stop,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        center.AddChild(panel);

        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", 10);
        panel.AddChild(content);

        // Header Section: Top Bar
        var topBar = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topBar.AddThemeConstantOverride("separation", 8);
        content.AddChild(topBar);

        topBar.AddChild(new BroadcastVectorIcon("book", CareerUiTheme.Accent, 16f));

        var headerLabel = BodyLabel("DossierHeaderLabel");
        headerLabel.Text = "FUTBOLCU DOSYASI & TEKNİK ANALİZ RAPORU";
        CareerUiTheme.ApplyDisplayFont(headerLabel);
        headerLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        headerLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        headerLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        topBar.AddChild(headerLabel);

        var topCloseBtn = SecondaryButton("Kapat");
        topCloseBtn.CustomMinimumSize = new Vector2(34, 26);
        topCloseBtn.Pressed += ClosePlayerDossier;
        topBar.AddChild(topCloseBtn);

        content.AddChild(UiComponents.Divider(2));

        // Hero Row: Left (Number + Position + Name + Subtitle), Right (Big Rating OVR Badge)
        var heroRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        heroRow.AddThemeConstantOverride("separation", 12);
        content.AddChild(heroRow);

        var heroLeft = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        heroLeft.AddThemeConstantOverride("separation", 4);
        heroRow.AddChild(heroLeft);

        var tagRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tagRow.AddThemeConstantOverride("separation", 6);
        heroLeft.AddChild(tagRow);

        _playerDossierNumberLabel = BodyLabel("DossierNumber");
        _playerDossierNumberLabel.Text = "#10";
        _playerDossierNumberLabel.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
        CareerUiTheme.ApplyDisplayFont(_playerDossierNumberLabel);
        tagRow.AddChild(_playerDossierNumberLabel);

        _playerDossierPositionPill = new PanelContainer();
        _playerDossierPositionPill.AddThemeStyleboxOverride("panel", CareerUiTheme.PositionTagPanel());
        _playerDossierPositionLabel = BodyLabel("DossierPosition");
        _playerDossierPositionLabel.Text = "SNT · SANTRFOR";
        _playerDossierPositionPill.AddChild(_playerDossierPositionLabel);
        tagRow.AddChild(_playerDossierPositionPill);

        _playerDossierTitle = TitleLabel("PlayerDossierTitle");
        _playerDossierTitle.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(22));
        heroLeft.AddChild(_playerDossierTitle);

        _playerDossierSubtitleLabel = BodyLabel("DossierSubtitle", muted: true);
        _playerDossierSubtitleLabel.Text = "26 Yaş · Zirve Dönemi";
        heroLeft.AddChild(_playerDossierSubtitleLabel);

        // Hero Right: Big OVR Badge
        _playerDossierRatingBadge = new PanelContainer
        {
            CustomMinimumSize = new Vector2(72, 64),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _playerDossierRatingBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.RatingBadgePanel(80, margin: 6, radius: 8, borderWidth: 2));
        heroRow.AddChild(_playerDossierRatingBadge);

        var ratingCol = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        ratingCol.AddThemeConstantOverride("separation", 0);
        _playerDossierRatingBadge.AddChild(ratingCol);

        _playerDossierRatingLabel = TitleLabel("DossierRatingLabel");
        _playerDossierRatingLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _playerDossierRatingLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(24));
        _playerDossierRatingLabel.Text = "85";
        ratingCol.AddChild(_playerDossierRatingLabel);

        _playerDossierRatingCaption = BodyLabel("DossierRatingCaption", muted: true);
        _playerDossierRatingCaption.HorizontalAlignment = HorizontalAlignment.Center;
        _playerDossierRatingCaption.Text = "GÜÇ";
        _playerDossierRatingCaption.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(10));
        ratingCol.AddChild(_playerDossierRatingCaption);

        // 2x2 Bento Telemetry Grid
        var gridContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        gridContainer.AddThemeConstantOverride("separation", 8);
        content.AddChild(gridContainer);

        // Row 1: Yetenek & Potansiyel (Sol) + Fizik & Kondisyon (Sağ)
        var row1 = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        row1.AddThemeConstantOverride("separation", 8);
        gridContainer.AddChild(row1);

        // Kart 1: Yetenek & Potansiyel
        var abilityCard = AddDossierCard(row1, "YETENEK & POTANSİYEL");
        _dossierAbilityLabel = BodyLabel("DossierAbility");
        _dossierAbilityLabel.Text = "Mevcut: 82 / Tavan: 88";
        abilityCard.AddChild(_dossierAbilityLabel);

        _dossierAbilityBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 82,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 8),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _dossierAbilityBar.AddThemeStyleboxOverride("background", CareerUiTheme.ProgressBarBackground());
        _dossierAbilityBar.AddThemeStyleboxOverride("fill", CareerUiTheme.ProgressBarFill(CareerUiTheme.Data));
        abilityCard.AddChild(_dossierAbilityBar);

        _dossierCareerPhaseLabel = BodyLabel("DossierCareerPhase", muted: true);
        _dossierCareerPhaseLabel.Text = "Kariyer Zirvesi Dönemi";
        abilityCard.AddChild(_dossierCareerPhaseLabel);

        // Kart 2: Fizik & Kondisyon
        var physicalCard = AddDossierCard(row1, "FİZİK & KONDİSYON");
        _dossierFitnessLabel = BodyLabel("DossierFitness");
        _dossierFitnessLabel.Text = "Kondisyon: %95 · Yorgunluk: %5";
        physicalCard.AddChild(_dossierFitnessLabel);

        _dossierFitnessBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 95,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 8),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _dossierFitnessBar.AddThemeStyleboxOverride("background", CareerUiTheme.ProgressBarBackground());
        _dossierFitnessBar.AddThemeStyleboxOverride("fill", CareerUiTheme.ProgressBarFill(CareerUiTheme.ActionBright));
        physicalCard.AddChild(_dossierFitnessBar);

        var availRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        availRow.AddThemeConstantOverride("separation", 6);
        physicalCard.AddChild(availRow);

        _dossierAvailabilityBadge = new PanelContainer();
        _dossierAvailabilityBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright));
        _dossierAvailabilityLabel = BodyLabel("DossierAvailLabel");
        _dossierAvailabilityLabel.Text = "Maça Hazır";
        _dossierAvailabilityLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _dossierAvailabilityBadge.AddChild(_dossierAvailabilityLabel);
        availRow.AddChild(_dossierAvailabilityBadge);

        _dossierWorkloadLabel = BodyLabel("DossierWorkload", muted: true);
        _dossierWorkloadLabel.Text = "Son 7 Günde: 180 dk";
        availRow.AddChild(_dossierWorkloadLabel);

        // Row 2: Sözleşme & Maliyet (Sol) + İlişki & Sözler (Sağ)
        var row2 = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        row2.AddThemeConstantOverride("separation", 8);
        gridContainer.AddChild(row2);

        // Kart 3: Sözleşme & Maliyet
        var contractCard = AddDossierCard(row2, "SÖZLEŞME & KULÜP");
        _dossierContractWageLabel = BodyLabel("DossierWage");
        _dossierContractWageLabel.Text = "Haftalık Ücret: 45.000 TRY";
        contractCard.AddChild(_dossierContractWageLabel);

        _dossierContractEndLabel = BodyLabel("DossierContractEnd", muted: true);
        _dossierContractEndLabel.Text = "Sözleşme Sonu: 30.06.2027";
        contractCard.AddChild(_dossierContractEndLabel);

        // Kart 4: İlişki & Soyunma Odası
        var socialCard = AddDossierCard(row2, "İLİŞKİ & SOYUNMA ODASI");
        _dossierRelationshipLabel = BodyLabel("DossierRel");
        _dossierRelationshipLabel.Text = "İlişki: Güçlü";
        socialCard.AddChild(_dossierRelationshipLabel);

        _dossierSocialMetricsLabel = BodyLabel("DossierSocialMetrics", muted: true);
        _dossierSocialMetricsLabel.Text = "Güven %85 · Saygı %90 · Uyum %80";
        socialCard.AddChild(_dossierSocialMetricsLabel);

        _dossierPromiseLabel = BodyLabel("DossierPromise");
        _dossierPromiseLabel.Text = "Sözler: aktif söz yok";
        socialCard.AddChild(_dossierPromiseLabel);

        _dossierCausalityLabel = BodyLabel("DossierCausality", muted: true, autowrap: true);
        _dossierCausalityLabel.Text = "Son maçta 90 dk süre aldı.";
        socialCard.AddChild(_dossierCausalityLabel);

        // Hidden dummy label for backwards compatibility
        _playerDossierBody = new Label { Visible = false };
        content.AddChild(_playerDossierBody);

        _privateMeetingPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        _privateMeetingPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        content.AddChild(_privateMeetingPanel);
        var meetingContent = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        meetingContent.AddThemeConstantOverride("separation", 8);
        _privateMeetingPanel.AddChild(meetingContent);
        meetingContent.AddChild(UiComponents.SectionHeader("ÖZEL GÖRÜŞME", withDivider: true));
        _privateMeetingResultLabel = BodyLabel("PrivateMeetingResult", muted: true, autowrap: true);
        _privateMeetingResultLabel.Text = "Konuyu seç; görüşmenin ilişkiye etkisi anında kaydedilir.";
        meetingContent.AddChild(_privateMeetingResultLabel);
        var meetingTopics = ActionFlow();
        meetingContent.AddChild(meetingTopics);
        AddPrivateMeetingButton(meetingTopics, "Destek", Application.SocialContinuity.Services.PrivateMeetingTopic.Reassurance);
        AddPrivateMeetingButton(meetingTopics, "Süre", Application.SocialContinuity.Services.PrivateMeetingTopic.PlayingTime);
        AddPrivateMeetingButton(meetingTopics, "Form", Application.SocialContinuity.Services.PrivateMeetingTopic.PerformanceWarning);
        AddPrivateMeetingButton(meetingTopics, "Kaptanlık", Application.SocialContinuity.Services.PrivateMeetingTopic.Captaincy);
        AddPrivateMeetingButton(meetingTopics, "Sözleşme", Application.SocialContinuity.Services.PrivateMeetingTopic.Contract);

        // Footer Action Row
        var actionRow = ActionFlow();
        content.AddChild(actionRow);

        _playerDossierMeetingButton = AccentButton("Özel Görüşme");
        _playerDossierMeetingButton.Pressed += () =>
            _privateMeetingPanel.Visible = !_privateMeetingPanel.Visible;
        actionRow.AddChild(_playerDossierMeetingButton);

        _playerDossierSellButton = DangerButton("Satışa Çıkar");
        _playerDossierSellButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _playerDossierSellButton.Pressed += OnPlayerDossierSellPressed;
        actionRow.AddChild(_playerDossierSellButton);

        _playerDossierPromiseButton = SecondaryButton("11 Sözü Ver");
        _playerDossierPromiseButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _playerDossierPromiseButton.Pressed += OnPlayerDossierPromisePressed;
        actionRow.AddChild(_playerDossierPromiseButton);

        _playerDossierDecisionButton = SecondaryButton("Karar Aç");
        _playerDossierDecisionButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _playerDossierDecisionButton.Pressed += OnPlayerDossierDecisionPressed;
        actionRow.AddChild(_playerDossierDecisionButton);

        var close = PrimaryButton("Kapat");
        close.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        close.Pressed += ClosePlayerDossier;
        actionRow.AddChild(close);
    }

    private void AddPrivateMeetingButton(
        Control parent,
        string text,
        Application.SocialContinuity.Services.PrivateMeetingTopic topic)
    {
        var button = SecondaryButton(text);
        button.Pressed += () => ConductPrivateMeeting(topic);
        parent.AddChild(button);
    }

    private void ConductPrivateMeeting(Application.SocialContinuity.Services.PrivateMeetingTopic topic)
    {
        if (_selectedPlayerId is not long playerId)
        {
            return;
        }

        var result = _controller.ConductPrivateMeeting(playerId, topic);
        _privateMeetingResultLabel.Text = result.Message;
        Apply(result);
        OpenPlayerDossier(playerId);
        _privateMeetingPanel.Visible = true;
        _privateMeetingResultLabel.Text = result.Message;
    }

    private void OnPlayerDossierBackdropInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            ClosePlayerDossier();
        }
    }

    private void OpenPlayerDossier(long playerId)
    {
        var player = _playerManagementPlayers.FirstOrDefault(candidate => candidate.PlayerId == playerId);
        if (player is null)
        {
            var scoutCandidate = _scoutCandidates.FirstOrDefault(candidate => candidate.PlayerId == playerId);
            if (scoutCandidate is not null)
            {
                OpenScoutPlayerDossier(scoutCandidate);
            }
            return;
        }

        _playerDossierSellButton.Visible = true;
        _playerDossierPromiseButton.Visible = true;
        _playerDossierDecisionButton.Visible = true;
        _playerDossierMeetingButton.Visible = true;
        _privateMeetingPanel.Visible = false;

        _selectedPlayerId = playerId;
        _playerDossierTitle.Text = player.DisplayName;
        _playerDossierNumberLabel.Text = $"#{player.SquadNumber}";

        var posColor = CareerUiTheme.PositionGroupColor(player.PositionCode);
        _playerDossierPositionPill.AddThemeStyleboxOverride("panel", CareerUiTheme.PositionBadgePanel(player.PositionCode));
        _playerDossierPositionLabel.Text = $"{player.PositionCode} · {player.PositionName.ToUpperInvariant()}";
        _playerDossierPositionLabel.AddThemeColorOverride("font_color", posColor);

        _playerDossierSubtitleLabel.Text = $"{player.Age} Yaş · {player.CareerPhase} · Sözleşme: {player.ContractEnd}";

        // Rating
        var rateColor = CareerUiTheme.RatingColor(player.Rating);
        _playerDossierRatingBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.RatingBadgePanel(player.Rating, margin: 6, radius: 8, borderWidth: 2));
        _playerDossierRatingLabel.Text = player.Rating.ToString();
        _playerDossierRatingLabel.AddThemeColorOverride("font_color", rateColor);

        // Ability
        _dossierAbilityLabel.Text = $"Mevcut (CA): {player.CurrentAbility}  ·  Tavan (PA): {player.PotentialAbility}";
        _dossierAbilityBar.Value = player.CurrentAbility;
        _dossierCareerPhaseLabel.Text = $"Gelişim Aşaması: {player.CareerPhase}";

        // Physical
        var fitColor = CareerUiTheme.FitnessColor(player.Fitness);
        _dossierFitnessLabel.Text = $"Kondisyon: %{player.Fitness}  ·  Yorgunluk: %{player.Fatigue} ({player.FatigueRiskBand})";
        _dossierFitnessBar.Value = player.Fitness;
        _dossierFitnessBar.AddThemeStyleboxOverride("fill", CareerUiTheme.ProgressBarFill(fitColor));

        if (player.IsInjured)
        {
            _dossierAvailabilityBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorCritical));
            _dossierAvailabilityLabel.Text = player.Availability;
            _dossierAvailabilityLabel.AddThemeColorOverride("font_color", CareerUiTheme.ColorCritical);
        }
        else
        {
            _dossierAvailabilityBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright));
            _dossierAvailabilityLabel.Text = "Maça Hazır";
            _dossierAvailabilityLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        }

        var workloadText = !string.IsNullOrWhiteSpace(player.WorkloadHint) ? $" · {player.WorkloadHint}" : string.Empty;
        _dossierWorkloadLabel.Text = $"Son 7 günde {player.MatchMinutesLast7Days} dk{workloadText}";

        // Contract
        _dossierContractWageLabel.Text = player.WeeklyWage is int wage ? $"Haftalık Maaş: {wage:N0} TRY" : "Aktif sözleşme kaydı yok";
        _dossierContractEndLabel.Text = $"Bitiş Tarihi: {player.ContractEnd}";

        // Social
        _dossierRelationshipLabel.Text = $"Menajerle İlişki: {player.RelationshipState}";
        if (player.Trust is int trust && player.Respect is int respect && player.Compatibility is int compat)
        {
            _dossierSocialMetricsLabel.Text = $"Güven: %{trust} · Saygı: %{respect} · Uyum: %{compat}";
        }
        else
        {
            _dossierSocialMetricsLabel.Text = "Bireysel ilişki kaydı henüz oluşmadı.";
        }

        _dossierPromiseLabel.Text = $"Sözler: {player.PromiseSummary}";
        _dossierCausalityLabel.Text = $"Not: {player.CausalitySummary}";

        _playerDossierOverlay.Visible = true;
        RefreshPlayerDetail();
        RefreshPlayerDossierSellButton();
    }

    private void OpenScoutPlayerDossier(ScoutCandidateLine candidate)
    {
        _selectedPlayerId = candidate.PlayerId;
        _playerDossierTitle.Text = candidate.DisplayName;
        _playerDossierNumberLabel.Text = "—";

        var posColor = CareerUiTheme.PositionGroupColor(candidate.PositionCode);
        _playerDossierPositionPill.AddThemeStyleboxOverride("panel", CareerUiTheme.PositionBadgePanel(candidate.PositionCode));
        _playerDossierPositionLabel.Text = $"{candidate.PositionCode} · {candidate.ClubName.ToUpperInvariant()}";
        _playerDossierPositionLabel.AddThemeColorOverride("font_color", posColor);

        var valueText = candidate.MarketValue == 0 ? "SERBEST OYUNCU" : $"{candidate.MarketValue:N0} TRY";
        _playerDossierSubtitleLabel.Text = $"{candidate.Age} Yaş · Değer: {valueText}";

        var avgRating = (candidate.EstimatedAbilityLow + candidate.EstimatedAbilityHigh) / 2;
        var rateColor = CareerUiTheme.RatingColor(avgRating);
        _playerDossierRatingBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.RatingBadgePanel(avgRating, margin: 6, radius: 8, borderWidth: 2));
        _playerDossierRatingLabel.Text = $"{candidate.EstimatedAbilityLow}-{candidate.EstimatedAbilityHigh}";
        _playerDossierRatingLabel.AddThemeColorOverride("font_color", rateColor);

        // Ability
        _dossierAbilityLabel.Text = $"Tahmini Güç: {candidate.EstimatedAbilityLow}-{candidate.EstimatedAbilityHigh}  ·  Tavan: {candidate.PotentialAbility}";
        _dossierAbilityBar.Value = avgRating;
        _dossierCareerPhaseLabel.Text = $"Gözlemci Bilgisi: %{candidate.KnowledgePercent}";

        // Physical & Availability
        _dossierFitnessLabel.Text = "Fiziksel Durum: Normal müsabaka yükü";
        _dossierFitnessBar.Value = 92;
        _dossierFitnessBar.AddThemeStyleboxOverride("fill", CareerUiTheme.ProgressBarFill(CareerUiTheme.ActionBright));

        _dossierAvailabilityBadge.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright));
        _dossierAvailabilityLabel.Text = "✓ Transfer Edilebilir";
        _dossierAvailabilityLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);

        _dossierWorkloadLabel.Text = $"Önerilen Rol: {candidate.RecommendedSquadRole}";

        // Contract
        _dossierContractWageLabel.Text = $"Önerilen Maaş: {candidate.SuggestedWeeklyWage:N0} TRY/hf";
        _dossierContractEndLabel.Text = candidate.MarketValue == 0
            ? "Kulüpsüz / Serbest Statü"
            : $"Beklenen Bonservis: {candidate.EstimatedFeeLow:N0}-{candidate.EstimatedFeeHigh:N0} TRY";

        // Social / Scout Interest
        _dossierRelationshipLabel.Text = $"Kulübe İlgi: {candidate.InterestLabel}";
        _dossierSocialMetricsLabel.Text = candidate.IsAffordable
            ? "Kulüp bütçesi bu transferi karşılayabilir."
            : "Bütçe Üstü: Teklifi düşür veya maaş bütçesini gözden geçir.";

        _dossierPromiseLabel.Text = $"Önerilen Kadro Rolü: {candidate.RecommendedSquadRole}";
        _dossierCausalityLabel.Text = $"Tavsiye Açılış Teklifi: {candidate.SuggestedOpeningFee:N0} TRY";

        // Non-managed squad players don't have direct squad actions
        _playerDossierSellButton.Visible = false;
        _playerDossierPromiseButton.Visible = false;
        _playerDossierDecisionButton.Visible = false;
        _playerDossierMeetingButton.Visible = false;
        _privateMeetingPanel.Visible = false;

        _playerDossierOverlay.Visible = true;
    }

    private void OnInspectCandidateDossierPressed()
    {
        if (_selectedScoutPlayerId is long id)
        {
            OpenPlayerDossier(id);
        }
    }

    private void OnPlayerDossierSellPressed()
    {
        if (_selectedPlayerId is not long playerId)
        {
            Apply(UiActionResult.Fail("Önce kadrodan bir futbolcu seç."));
            return;
        }

        ClosePlayerDossier();
        Apply(_controller.SellManagedClubPlayer(playerId));
    }

    private void OnPlayerDossierPromisePressed()
    {
        if (_selectedPlayerId is not long playerId)
        {
            return;
        }

        ApplySelectedPlayer(_controller.PromiseStartingOpportunityToPlayer);
        OpenPlayerDossier(playerId);
    }

    private void OnPlayerDossierDecisionPressed()
    {
        if (_selectedPlayerId is not long playerId)
        {
            return;
        }

        ApplySelectedPlayer(_controller.OpenStartingOpportunityDecisionForPlayer);
        OpenPlayerDossier(playerId);
    }

    private void RefreshPlayerDossierSellButton()
    {
        if (_playerDossierSellButton is null)
        {
            return;
        }

        var employed = _controller.IsManagerEmployed;
        var windowOpen = _controller.IsTransferWindowOpen;
        var hasPlayer = _selectedPlayerId is > 0;
        _playerDossierSellButton.Visible = employed && hasPlayer;
        _playerDossierSellButton.Disabled = !windowOpen || !hasPlayer;
        _playerDossierSellButton.Text = windowOpen
            ? "Satışa Çıkar"
            : "Satışa Çıkar (kapalı)";

        if (_playerDossierPromiseButton is not null)
        {
            _playerDossierPromiseButton.Visible = employed && hasPlayer;
            _playerDossierPromiseButton.Disabled = !employed || !hasPlayer;
        }

        if (_playerDossierDecisionButton is not null)
        {
            _playerDossierDecisionButton.Visible = employed && hasPlayer;
            _playerDossierDecisionButton.Disabled = !employed || !hasPlayer;
        }
    }

    private void ClosePlayerDossier()
    {
        if (_playerDossierOverlay is not null)
        {
            _playerDossierOverlay.Visible = false;
        }
    }
}
