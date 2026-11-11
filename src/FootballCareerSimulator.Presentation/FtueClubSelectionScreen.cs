using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Presentation;
using FootballCareerSimulator.Simulation.DataPacks;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class FtueClubSelectionScreen : Control
{
    private readonly int _rootSeed = 2026;
    private readonly GameDate _worldDate = ProductionCareerWorldConstraints.DefaultOpeningDate;
    private string _activeLeague = "Süper Lig";
    private EuropeanEliteClubData? _selectedClub;
    private LineEdit _managerNameInput = null!;
    private VBoxContainer _clubCardsContainer = null!;
    private PanelContainer _clubDetailPanel = null!;
    private Label _detailClubNameLabel = null!;
    private Label _detailStadiumLabel = null!;
    private Label _detailStarsLabel = null!;
    private Label _detailFinancesLabel = null!;
    private Button _signContractButton = null!;
    private readonly Dictionary<string, Button> _leagueButtons = [];

    public event Action<CareerStartConfiguration>? CareerConfirmed;
    public event Action? CancelRequested;

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();

        var bg = new ColorRect
        {
            Color = CareerUiTheme.BackgroundDeep,
        };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var rootVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        rootVBox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(rootVBox);

        // 1. SON DAKİKA / BREAKING NEWS YAYIN BANNERI
        var breakingNewsPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        breakingNewsPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        rootVBox.AddChild(breakingNewsPanel);

        var bnVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bnVBox.AddThemeConstantOverride("separation", 2);
        breakingNewsPanel.AddChild(bnVBox);

        var bnEyebrow = new Label { Text = "SON DAKİKA · CANLI YAYIN BASIN ODASI" };
        CareerUiTheme.StyleEyebrow(bnEyebrow, CareerUiTheme.ActionBright);
        bnEyebrow.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        bnVBox.AddChild(bnEyebrow);

        var bnHeadline = new Label
        {
            Text = "AVRUPA ŞAMPİYONLUĞU SONRASI TEKLİF YAĞMURU! HANGİ DEVİN BAŞINA GEÇİYORSUN?",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(bnHeadline);
        bnHeadline.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(15));
        bnHeadline.AddThemeColorOverride("font_color", Colors.White);
        bnVBox.AddChild(bnHeadline);

        // 2. Menajer Adı Girişi & Lig Filtreleme Satırı
        var managerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        managerRow.AddThemeConstantOverride("separation", 12);
        rootVBox.AddChild(managerRow);

        var nameLabel = new Label { Text = "MENAJER ADI:", SizeFlagsVertical = SizeFlags.ShrinkCenter };
        CareerUiTheme.StyleTableText(nameLabel);
        nameLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        managerRow.AddChild(nameLabel);

        _managerNameInput = new LineEdit
        {
            Text = "Yusuf Emre",
            PlaceholderText = "Menajer adını yaz...",
            CustomMinimumSize = new Vector2(200, 36),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _managerNameInput.AddThemeStyleboxOverride("normal", CareerUiTheme.SoftPanel());
        managerRow.AddChild(_managerNameInput);

        var leagueRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.End };
        leagueRow.AddThemeConstantOverride("separation", 6);
        managerRow.AddChild(leagueRow);

        var leagues = new[] { "Süper Lig", "Premier League", "La Liga", "Bundesliga" };
        foreach (var l in leagues)
        {
            var btn = new Button
            {
                Text = l.ToUpperInvariant(),
                CustomMinimumSize = new Vector2(100, 34),
            };
            btn.Pressed += () => SelectLeague(l);
            leagueRow.AddChild(btn);
            _leagueButtons[l] = btn;
        }

        // 3. Ana Bölüm: Sol Kulüp Vitrini Grid / Sağ Detay Kartı
        var split = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        split.AddThemeConstantOverride("separation", 14);
        rootVBox.AddChild(split);

        var scroll = new MobileScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.25f,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        split.AddChild(scroll);

        _clubCardsContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _clubCardsContainer.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_clubCardsContainer);

        // Sağ Taraf: Kulüp Vitrin & Sözleşme İmzala Kartı
        var rightSide = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 0.95f,
        };
        rightSide.AddThemeConstantOverride("separation", 10);
        split.AddChild(rightSide);

        _clubDetailPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _clubDetailPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized: true));
        rightSide.AddChild(_clubDetailPanel);

        var detailVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        detailVBox.AddThemeConstantOverride("separation", 8);
        _clubDetailPanel.AddChild(detailVBox);

        var detailTitle = new Label { Text = "SÖZLEŞME VE KULÜP DOSYASI" };
        CareerUiTheme.StyleEyebrow(detailTitle, CareerUiTheme.ActionBright);
        detailVBox.AddChild(detailTitle);

        _detailClubNameLabel = new Label { Text = "SEÇİLEN KULÜP" };
        CareerUiTheme.StyleHeadline(_detailClubNameLabel);
        _detailClubNameLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(20));
        detailVBox.AddChild(_detailClubNameLabel);

        _detailStadiumLabel = new Label { Text = "Stadyum: —", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        CareerUiTheme.StyleBody(_detailStadiumLabel, muted: true);
        detailVBox.AddChild(_detailStadiumLabel);

        _detailFinancesLabel = new Label { Text = "Finans & Bütçe: —", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        CareerUiTheme.StyleBody(_detailFinancesLabel);
        detailVBox.AddChild(_detailFinancesLabel);

        _detailStarsLabel = new Label { Text = "Kilit Oyuncular: —", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        CareerUiTheme.StyleBody(_detailStarsLabel);
        _detailStarsLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        detailVBox.AddChild(_detailStarsLabel);

        var bottomActions = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bottomActions.AddThemeConstantOverride("separation", 10);
        rightSide.AddChild(bottomActions);

        var backBtn = new Button
        {
            Text = "Ana Menü",
            CustomMinimumSize = new Vector2(100, 44),
        };
        CareerUiTheme.StyleSecondaryButton(backBtn);
        backBtn.Pressed += () => CancelRequested?.Invoke();
        bottomActions.AddChild(backBtn);

        _signContractButton = new Button
        {
            Text = "SÖZLEŞMEYİ İMZALA & GÖREVE BAŞLA",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 44),
        };
        CareerUiTheme.StyleAccentButton(_signContractButton);
        _signContractButton.Pressed += OnSignContractPressed;
        bottomActions.AddChild(_signContractButton);

        SelectLeague("Süper Lig");
    }

    private void SelectLeague(string leagueName)
    {
        _activeLeague = leagueName;
        foreach (var (l, btn) in _leagueButtons)
        {
            CareerUiTheme.StyleTabButton(btn, l == leagueName);
        }

        foreach (var child in _clubCardsContainer.GetChildren())
        {
            child.QueueFree();
        }

        var clubs = EuropeanElite202627DataPack.GetClubsByLeague(leagueName);
        if (clubs.Count == 0)
        {
            clubs = EuropeanElite202627DataPack.AllClubs.Take(6).ToList();
        }

        EuropeanEliteClubData? first = null;
        foreach (var club in clubs)
        {
            first ??= club;
            var isSelected = _selectedClub?.ClubId == club.ClubId;
            var card = BuildClubCard(club, isSelected);
            _clubCardsContainer.AddChild(card);
        }

        if (_selectedClub is null || !clubs.Any(c => c.ClubId == _selectedClub.ClubId))
        {
            if (first is not null)
            {
                SelectClub(first);
            }
        }
    }

    private Control BuildClubCard(EuropeanEliteClubData club, bool isSelected)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", isSelected ? CareerUiTheme.NeonGlowPanel(strongGlow: true) : CareerUiTheme.CardPanel(emphasized: false));

        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 10);
        panel.AddChild(row);

        var ident = ClubBrandingVisuals.ResolveClubIdentity(club.ClubId, club.OfficialName);
        var kit = new ClubKitControl(ident, KitVariantType.Home, new Vector2(34, 44));
        row.AddChild(kit);

        var infoVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        infoVBox.AddThemeConstantOverride("separation", 2);
        row.AddChild(infoVBox);

        var name = new Label { Text = club.OfficialName.ToUpperInvariant() };
        CareerUiTheme.StyleHeadline(name);
        name.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        infoVBox.AddChild(name);

        var meta = new Label { Text = $"{club.Country} · Güç: {club.SportiveStrength} · Bütçe: {club.TransferBudgetLimit / 1_000_000:N0}M TRY" };
        CareerUiTheme.StyleBody(meta, muted: true);
        meta.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        infoVBox.AddChild(meta);

        var btn = new Button
        {
            Text = isSelected ? "SEÇİLDİ" : "İNCELE",
            CustomMinimumSize = new Vector2(74, 32),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        if (isSelected)
        {
            CareerUiTheme.StyleAccentButton(btn);
        }
        else
        {
            CareerUiTheme.StyleSecondaryButton(btn);
        }
        btn.Pressed += () => SelectClub(club);
        row.AddChild(btn);

        return panel;
    }

    private void SelectClub(EuropeanEliteClubData club)
    {
        _selectedClub = club;
        _detailClubNameLabel.Text = club.OfficialName.ToUpperInvariant();
        _detailStadiumLabel.Text = $"Ülke: {club.Country} · Lig: {club.LeagueName} · Güç Seviyesi: {club.SportiveStrength}";
        _detailFinancesLabel.Text = $"Transfer Bütçesi: {club.TransferBudgetLimit / 1_000_000:N0}M TRY";

        var starsText = club.Players.Count > 0
            ? string.Join(", ", club.Players.OrderByDescending(p => p.CurrentAbility ?? 70).Take(4).Select(p => $"{p.DisplayName} ({p.CurrentAbility ?? 75})"))
            : "Kadrosu hazır";
        _detailStarsLabel.Text = $"Kilit Yıldızlar:\n{starsText}";

        SelectLeague(_activeLeague);
    }

    private void OnSignContractPressed()
    {
        if (_selectedClub is null)
        {
            return;
        }

        var managerName = string.IsNullOrWhiteSpace(_managerNameInput.Text) ? "Yusuf Emre" : _managerNameInput.Text.Trim();
        var background = StartingBackground.TacticalSpecialist;

        var config = CareerStartConfiguration.Create(
            managerName,
            _selectedClub.ClubId,
            _worldDate,
            _rootSeed,
            background);

        CareerConfirmed?.Invoke(config);
    }
}
