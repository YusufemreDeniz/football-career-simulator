using FootballCareerSimulator.Application.CareerWorld;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerSetupScreen : Control
{
    private ProductionCareerWorldSummary? _worldSummary;
    private GameDate _worldDate = ProductionCareerWorldConstraints.DefaultOpeningDate;
    private StartingBackground? _selectedBackground;
    private StartingClubOfferDigest? _selectedOffer;
    private IReadOnlyList<StartingClubOfferDigest> _offers = Array.Empty<StartingClubOfferDigest>();

    private LineEdit _managerNameInput = null!;
    private LineEdit _seedInput = null!;
    private Label _worldSummaryLabel = null!;
    private VBoxContainer _backgroundList = null!;
    private Label _backgroundPitch = null!;
    private VBoxContainer _offerList = null!;
    private Label _offerHint = null!;
    private Button _startButton = null!;

    public event Action<CareerStartConfiguration>? CareerConfirmed;

    public event Action? CancelRequested;

    public override void _Ready()
    {
        CareerUiTheme.EnsureLoaded();

        AddChild(CareerUiTheme.CreateAtmosphereBackground());

        var safeArea = new MarginContainer();
        safeArea.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        safeArea.AddThemeConstantOverride("margin_left", 16);
        safeArea.AddThemeConstantOverride("margin_top", 20);
        safeArea.AddThemeConstantOverride("margin_right", 16);
        safeArea.AddThemeConstantOverride("margin_bottom", 20);
        AddChild(safeArea);

        var scroll = new MobileScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        safeArea.AddChild(scroll);

        var center = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        scroll.AddChild(center);

        var content = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        content.AddThemeConstantOverride("separation", 12);
        center.AddChild(content);
        Resized += () => UpdateContentWidth(content);
        CallDeferred(nameof(UpdateContentWidthDeferred), content);

        var back = new Button
        {
            Text = "Geri",
            TooltipText = "Ana menuye don",
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        CareerUiTheme.StyleSecondaryButton(back);
        back.CustomMinimumSize = new Vector2(94, 46);
        back.Pressed += () => CancelRequested?.Invoke();
        content.AddChild(back);

        var eyebrow = new Label { Text = "YENI KARIYER", HorizontalAlignment = HorizontalAlignment.Center };
        CareerUiTheme.StyleEyebrow(eyebrow, CareerUiTheme.Accent);
        content.AddChild(eyebrow);

        var title = new Label
        {
            Text = "Kariyerine basla",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(title);
        title.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(27));
        content.AddChild(title);

        var intro = new Label
        {
            Text = "Adini ve gecmisini sec. Yirmi kulubun hepsi kapini calmaz; yalniz uygun tekliflerden birini kabul edersin.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(intro, muted: true);
        intro.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        content.AddChild(intro);

        content.AddChild(BuildWorldSection());
        content.AddChild(BuildManagerSection());
        content.AddChild(BuildBackgroundSection());
        content.AddChild(BuildOfferSection());

        _startButton = new Button
        {
            Text = "Teklifi Kabul Et",
            TooltipText = "Secilen kulup teklifini kabul edip ClubEmployment baslat",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Disabled = true,
        };
        CareerUiTheme.StylePrimaryButton(_startButton);
        _startButton.CustomMinimumSize = new Vector2(0, 54);
        _startButton.Pressed += ConfirmCareer;
        content.AddChild(_startButton);

        var note = new Label
        {
            Text = "Yeni kariyer, onceki yerel kaydi temizleyerek farkli bir sezon akisi baslatir.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(note, muted: true);
        note.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        content.AddChild(note);

        _seedInput.Text = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, int.MaxValue).ToString();
        RebuildWorldFromSeed();
        RefreshBackgroundCards();
        UpdateStartAvailability();
    }

    private Control BuildWorldSection()
    {
        var section = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        section.AddThemeConstantOverride("separation", 6);

        var label = new Label { Text = "DUNYA SEED" };
        CareerUiTheme.StyleSection(label);
        section.AddChild(label);

        _seedInput = new LineEdit
        {
            PlaceholderText = "Ornek: 741852",
            MaxLength = 10,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 50),
            TooltipText = "Ayni seed ayni ligi, kulupleri ve futbolculari uretir",
        };
        CareerUiTheme.StyleTextInput(_seedInput);
        _seedInput.TextChanged += _ => RebuildWorldFromSeed();
        section.AddChild(_seedInput);

        var summaryPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        summaryPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatusPanel());
        section.AddChild(summaryPanel);

        _worldSummaryLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBody(_worldSummaryLabel, muted: true);
        _worldSummaryLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        summaryPanel.AddChild(_worldSummaryLabel);

        return section;
    }

    private Control BuildManagerSection()
    {
        var section = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        section.AddThemeConstantOverride("separation", 6);

        var label = new Label { Text = "TEKNIK DIREKTOR ADI" };
        CareerUiTheme.StyleSection(label);
        section.AddChild(label);

        _managerNameInput = new LineEdit
        {
            PlaceholderText = "Ornek: Yusuf Deniz",
            MaxLength = 48,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 50),
        };
        CareerUiTheme.StyleTextInput(_managerNameInput);
        _managerNameInput.TextChanged += _ => UpdateStartAvailability();
        section.AddChild(_managerNameInput);

        return section;
    }

    private Control BuildBackgroundSection()
    {
        var section = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        section.AddThemeConstantOverride("separation", 8);

        var label = new Label { Text = "BASLANGIC GECMISI" };
        CareerUiTheme.StyleSection(label);
        section.AddChild(label);

        var hint = new Label
        {
            Text = "Gecmisin hangi kuluplerin kapisini calacagini belirler. Kalici bir super guc degildir.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(hint, muted: true);
        hint.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        section.AddChild(hint);

        _backgroundList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _backgroundList.AddThemeConstantOverride("separation", 6);
        section.AddChild(_backgroundList);

        _backgroundPitch = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        CareerUiTheme.StyleBody(_backgroundPitch, muted: true);
        _backgroundPitch.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        section.AddChild(_backgroundPitch);

        return section;
    }

    private Control BuildOfferSection()
    {
        var section = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        section.AddThemeConstantOverride("separation", 8);

        var label = new Label { Text = "IS TEKLIFLERI" };
        CareerUiTheme.StyleSection(label);
        section.AddChild(label);

        _offerHint = new Label
        {
            Text = "Once bir baslangic gecmisi sec. Ligdeki yirmi kulubu listeden sahiplenemezsin.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(_offerHint, muted: true);
        _offerHint.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        section.AddChild(_offerHint);

        _offerList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _offerList.AddThemeConstantOverride("separation", 8);
        section.AddChild(_offerList);

        return section;
    }

    private void RebuildWorldFromSeed()
    {
        if (!int.TryParse(_seedInput.Text.Trim(), out var seed) || seed <= 0)
        {
            _worldSummary = null;
            _worldSummaryLabel.Text = "Gecerli bir seed gir. Bos birakma; ayni sayi ayni dunyayi acar.";
            RefreshOffers();
            UpdateStartAvailability();
            return;
        }

        var world = ProductionCareerWorldBootstrap.Create(seed);
        _worldDate = world.WorldDate;
        _worldSummary = ProductionCareerWorldBootstrap.ToSummary(world);
        _worldSummaryLabel.Text =
            $"{_worldSummary.CountryName} · {_worldSummary.LeagueName}\n" +
            $"{_worldSummary.ClubCount} kulup · {_worldSummary.ActivePlayerCount} futbolcu " +
            $"({_worldSummary.ContractedPlayerCount} kadrolu, {_worldSummary.FreeAgentCount} serbest)\n" +
            $"Sezon acilisi: {_worldSummary.OpeningDateDisplay} · Seed {_worldSummary.RootSeed}";
        RefreshOffers();
        UpdateStartAvailability();
    }

    private void RefreshBackgroundCards()
    {
        foreach (var child in _backgroundList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var background in StartingBackgroundCatalog.All)
        {
            var selected = _selectedBackground == background;
            var button = new Button
            {
                Text = StartingBackgroundCatalog.DisplayName(background),
                TooltipText = StartingBackgroundCatalog.Pitch(background),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            button.CustomMinimumSize = new Vector2(0, 48);
            if (selected)
            {
                CareerUiTheme.StylePrimaryButton(button);
            }
            else
            {
                CareerUiTheme.StyleSecondaryButton(button);
            }

            var captured = background;
            button.Pressed += () => SelectBackground(captured);
            _backgroundList.AddChild(button);
        }

        _backgroundPitch.Text = _selectedBackground is { } chosen
            ? StartingBackgroundCatalog.Pitch(chosen)
            : "Bir gecmis sec; ilk tekliflerin o yoldan gelir.";
    }

    private void SelectBackground(StartingBackground background)
    {
        _selectedBackground = background;
        _selectedOffer = null;
        RefreshBackgroundCards();
        RefreshOffers();
        UpdateStartAvailability();
    }

    private void RefreshOffers()
    {
        foreach (var child in _offerList.GetChildren())
        {
            child.QueueFree();
        }

        if (_worldSummary is null || _selectedBackground is not { } background)
        {
            _offers = Array.Empty<StartingClubOfferDigest>();
            _offerHint.Text = _worldSummary is null
                ? "Gecerli bir dunya seed'i olmadan teklif uretilmez."
                : "Once bir baslangic gecmisi sec. Ligdeki yirmi kulubu listeden sahiplenemezsin.";
            return;
        }

        _offers = CareerPresentationHost.GetStartingJobOffers(_worldSummary.RootSeed, background, _worldDate);
        if (_selectedOffer is { } previous
            && !_offers.Any(offer => offer.ClubId == previous.ClubId))
        {
            _selectedOffer = null;
        }

        _offerHint.Text = _offers.Count == 0
            ? "Bu gecmis icin teklif uretilemedi."
            : $"{_offers.Count} kulup kapini caldi. Yildiz kulup her zaman teklif vermez.";

        foreach (var offer in _offers)
        {
            _offerList.AddChild(BuildOfferCard(offer));
        }
    }

    private Control BuildOfferCard(StartingClubOfferDigest offer)
    {
        var selected = _selectedOffer?.ClubId == offer.ClubId;
        var button = new Button
        {
            Text =
                (selected ? $"{offer.DisplayName}  ·  secildi\n" : $"{offer.DisplayName}\n") +
                $"{offer.LeagueLevelSummary} · guc {offer.SportiveStrength}/100\n" +
                $"{offer.SquadSummary}\n" +
                $"Yonetim beklentisi: {offer.BoardExpectation}\n" +
                $"Transfer butcesi: EUR {offer.TransferBudget:N0}\n" +
                offer.WhyOffered,
            TooltipText = "Bu teklifi kabul etmek icin sec",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Alignment = HorizontalAlignment.Left,
        };
        if (selected)
        {
            CareerUiTheme.StylePrimaryButton(button);
        }
        else
        {
            CareerUiTheme.StyleSecondaryButton(button);
        }

        button.CustomMinimumSize = new Vector2(0, 128);
        button.Pressed += () => SelectOffer(offer);
        return button;
    }

    private void SelectOffer(StartingClubOfferDigest offer)
    {
        _selectedOffer = offer;
        RefreshOffers();
        UpdateStartAvailability();
    }

    private void UpdateContentWidth(Control content)
    {
        var available = Mathf.Max(288f, Size.X - 32f);
        content.CustomMinimumSize = new Vector2(Mathf.Min(520f, available), 0);
    }

    private void UpdateContentWidthDeferred(Control content) => UpdateContentWidth(content);

    private void UpdateStartAvailability()
    {
        _startButton.Disabled =
            _worldSummary is null
            || _selectedBackground is null
            || _selectedOffer is null
            || _managerNameInput.Text.Trim().Length < 2;
    }

    private void ConfirmCareer()
    {
        if (_worldSummary is null
            || _selectedBackground is not { } background
            || _selectedOffer is not { } offer)
        {
            return;
        }

        CareerConfirmed?.Invoke(CareerStartConfiguration.Create(
            _managerNameInput.Text,
            offer.ClubId,
            _worldDate,
            _worldSummary.RootSeed,
            background));
    }
}
