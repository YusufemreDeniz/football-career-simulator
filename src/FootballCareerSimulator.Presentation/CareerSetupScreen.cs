using FootballCareerSimulator.Application.ClubGovernance.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerSetupScreen : Control
{
    private readonly IReadOnlyList<ClubReadModel> _clubs;
    private LineEdit _managerNameInput = null!;
    private OptionButton _clubSelector = null!;
    private TextureRect _crest = null!;
    private Label _clubName = null!;
    private Label _clubSummary = null!;
    private Button _startButton = null!;

    public CareerSetupScreen(IReadOnlyList<ClubReadModel> clubs)
    {
        _clubs = clubs ?? throw new ArgumentNullException(nameof(clubs));
        if (_clubs.Count == 0)
        {
            throw new ArgumentException("At least one club is required.", nameof(clubs));
        }
    }

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
            Text = "Teknik direktorlugu kur",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleHeadline(title);
        title.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(27));
        content.AddChild(title);

        var date = new Label
        {
            Text = $"Baslangic tarihi: {DateTime.Today:dd.MM.yyyy}",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleBody(date, muted: true);
        date.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        content.AddChild(date);

        content.AddChild(BuildManagerSection());
        content.AddChild(BuildClubSection());

        _startButton = new Button
        {
            Text = "Kariyeri Baslat",
            TooltipText = "Menajerini ve kulubunu onaylayarak kariyeri baslat",
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

        UpdateClubPreview(0);
        UpdateStartAvailability();
    }

    private Control BuildManagerSection()
    {
        var section = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        section.AddThemeConstantOverride("separation", 6);

        var label = new Label { Text = "MENAJER ADI" };
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

    private Control BuildClubSection()
    {
        var section = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        section.AddThemeConstantOverride("separation", 8);

        var label = new Label { Text = "KULUP SECIMI" };
        CareerUiTheme.StyleSection(label);
        section.AddChild(label);

        _clubSelector = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 52),
            TooltipText = "Kariyerine baslayacagin kulubu sec",
        };
        CareerUiTheme.StyleOptionSelector(_clubSelector);
        foreach (var club in _clubs)
        {
            _clubSelector.AddItem(club.DisplayName, (int)club.ClubId);
        }

        _clubSelector.ItemSelected += selected => UpdateClubPreview((int)selected);
        section.AddChild(_clubSelector);

        var detail = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        detail.AddThemeConstantOverride("separation", 12);
        section.AddChild(detail);

        _crest = new TextureRect
        {
            CustomMinimumSize = new Vector2(72, 72),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        detail.AddChild(_crest);

        var information = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        information.AddThemeConstantOverride("separation", 3);
        detail.AddChild(information);

        _clubName = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        CareerUiTheme.StyleHeadline(_clubName);
        _clubName.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(19));
        information.AddChild(_clubName);

        _clubSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        CareerUiTheme.StyleBody(_clubSummary, muted: true);
        _clubSummary.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
        information.AddChild(_clubSummary);

        return section;
    }

    private void UpdateContentWidth(Control content)
    {
        var available = Mathf.Max(288f, Size.X - 32f);
        content.CustomMinimumSize = new Vector2(Mathf.Min(460f, available), 0);
    }

    private void UpdateContentWidthDeferred(Control content) => UpdateContentWidth(content);

    private void UpdateClubPreview(int index)
    {
        var club = _clubs[Mathf.Clamp(index, 0, _clubs.Count - 1)];
        _clubName.Text = club.DisplayName;
        _clubSummary.Text =
            $"Guc {club.SportiveStrength}/100\n" +
            $"Transfer butcesi: EUR {club.AvailableTransferFunds:N0}\n" +
            $"Haftalik maas limiti: EUR {club.WageBudgetLimit:N0}";

        _crest.Texture = ResourceLoader.Exists(club.CrestResourcePath)
            ? GD.Load<Texture2D>(club.CrestResourcePath)
            : null;
    }

    private void UpdateStartAvailability()
    {
        _startButton.Disabled = _managerNameInput.Text.Trim().Length < 2;
    }

    private void ConfirmCareer()
    {
        var selected = _clubs[Mathf.Clamp(_clubSelector.Selected, 0, _clubs.Count - 1)];
        CareerConfirmed?.Invoke(CareerStartConfiguration.Create(
            _managerNameInput.Text,
            selected.ClubId));
    }
}
