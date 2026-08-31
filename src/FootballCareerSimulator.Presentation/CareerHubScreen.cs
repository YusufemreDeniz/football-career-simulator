using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Application.PlayerCareer.Queries;
using Godot;
using System.Text.RegularExpressions;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen : Control
{
    private readonly CareerSessionController _controller;

    private Label _dateLabel = null!;
    private Label _managerLabel = null!;
    private Label _seasonLabel = null!;
    private Label _progressLabel = null!;
    private Label _blockerLabel = null!;
    private Label _pulseLabel = null!;
    private Label _selectionLabel = null!;
    private Label _trainingLabel = null!;
    private Label _matchTrainingPriorityLabel = null!;
    private readonly Dictionary<MatchTrainingPriority, Button> _matchTrainingPriorityButtons = [];
    private Label _prepBriefingLabel = null!;
    private Label _developmentLabel = null!;
    private Label _contractLabel = null!;
    private Label _dressingRoomEchoLabel = null!;
    private Label _memoryLabel = null!;
    private Label _promiseLabel = null!;
    private Label _relationshipLabel = null!;
    private Label _deskLabel = null!;
    private Label _officeLabel = null!;
    private Label _recoveryPathLabel = null!;
    private Label _weekStoryLabel = null!;
    private Control _firstWeekGuideCard = null!;
    private Label _firstWeekGuideLabel = null!;
    private Button _firstWeekGuideOpenButton = null!;
    private Button _firstWeekGuideNextButton = null!;
    private Button _officeNextStepButton = null!;
    private HubPage _officeNextStepTarget = HubPage.Today;
    private string _officeNextStepAction = Application.CareerHub.Queries.OfficeNextStepGuide.ActionNavigate;
    private Label _briefingLabel = null!;
    private Label _decisionLabel = null!;
    private Button _openDecisionButton = null!;
    private Button _openStartingDecisionButton = null!;
    private Button _openTransferDecisionButton = null!;
    private Button _openDisciplineDecisionButton = null!;
    private Button _openBoardDemandDecisionButton = null!;
    private Button _openPressQuestionDecisionButton = null!;
    private Button _grantDecisionButton = null!;
    private Button _refuseDecisionButton = null!;
    private Button _disciplineWarningButton = null!;
    private Button _disciplineFineButton = null!;
    private Button _disciplineSupportButton = null!;
    private Button _boardCounterButton = null!;
    private Button _pressCriticizeButton = null!;
    private Label _transferWindowLabel = null!;
    private Label _transferBudgetLabel = null!;
    private Label _transferDeskLabel = null!;
    private Label _transferScoutReportLabel = null!;
    private Button _transferScoutToggle = null!;
    private Control _transferScoutDetails = null!;
    private Button _openTransferWindowButton = null!;
    private Button _closeTransferWindowButton = null!;
    private Button _transferNextStepButton = null!;
    private Button _transferNegotiationToggle = null!;
    private Control[] _transferNegotiationCards = [];
    private string _transferNextStepAction = Application.CareerHub.Queries.OfficeNextStepGuide.ActionNavigate;
    private string _transferNextStepTarget = Application.Transfer.Queries.TransferNextStep.TargetTransfer;
    private Label _transferNeedLabel = null!;
    private Label _scoutReportLabel = null!;
    private ItemList _scoutCandidateList = null!;
    private IReadOnlyList<ScoutCandidateLine> _scoutCandidates = Array.Empty<ScoutCandidateLine>();
    private long? _selectedScoutPlayerId;
    private Label _shortlistTargetLabel = null!;
    private Label _transferProcessLabel = null!;
    private Label _tacticLabel = null!;
    private Label _dualPhaseTacticLabel = null!;
    private Label _squadStatusLabel = null!;
    private Label _squadCapacityLabel = null!;
    private Label _academyIntakeLabel = null!;
    private ItemList _academyCandidateList = null!;
    private Button _academyAcceptButton = null!;
    private Button _academyRejectButton = null!;
    private IReadOnlyList<YouthAcademyCandidateReadModel> _academyCandidates = [];
    private long? _selectedAcademyCandidateId;
    private Label _academyLifecycleLabel = null!;
    private ItemList _academyLifecycleList = null!;
    private Button _academyPromoteButton = null!;
    private IReadOnlyList<YouthAcademyPlayerReadModel> _academyLifecyclePlayers = [];
    private long? _selectedAcademyLifecyclePlayerId;
    private TextureRect _clubCrest = null!;
    private TextureRect _homeKit = null!;
    private TextureRect _awayKit = null!;
    private TextureRect _thirdKit = null!;
    private Tree _standingsTable = null!;
    private Label _leagueBriefingLabel = null!;
    private Label _leagueStatisticsLabel = null!;
    private Label _managedLeagueStatisticsLabel = null!;
    private Label _statusLabel = null!;
    private Label _saveDeskLabel = null!;
    private Label _careerLegacyHeadlineLabel = null!;
    private Label _careerLegacyRecordLabel = null!;
    private Label _careerLegacyDevelopmentLabel = null!;
    private Label _deviceAcceptanceLabel = null!;
    private Label _runtimeTelemetryLabel = null!;
    private Label _experienceSettingsLabel = null!;
    private Label _clubEconomyLabel = null!;
    private ItemList _careerSeasonList = null!;
    private Button _saveGameButton = null!;
    private Button _loadGameButton = null!;
    private Button _soundSettingButton = null!;
    private Button _musicSettingButton = null!;
    private Button _crowdSettingButton = null!;
    private Button _hapticsSettingButton = null!;
    private Button _motionSettingButton = null!;
    private Button _contrastSettingButton = null!;
    private Button _textScaleSettingButton = null!;
    private Button _guideSettingButton = null!;
    private Button _gamepadSettingButton = null!;
    private SpinBox _roundSelector = null!;
    private ItemList _fixtureList = null!;
    private ItemList _squadList = null!;
    private Control _clubPitchHost = null!;
    private Label _playerManagementHeadlineLabel = null!;
    private Label _playerDetailLabel = null!;
    private Control _playerDossierOverlay = null!;
    private Label _playerDossierTitle = null!;
    private Label _playerDossierBody = null!;
    private Button _playerDossierSellButton = null!;
    private IReadOnlyList<PlayerManagementLine> _playerManagementPlayers = Array.Empty<PlayerManagementLine>();
    private long? _selectedPlayerId;
    private Button _approveSelectionButton = null!;
    private Button _swapSelectionButton = null!;
    private Button _generateOfferButton = null!;
    private Button _acceptOfferButton = null!;
    private Button _signFreeAgentButton = null!;
    private Button _promoteOverflowButton = null!;
    private Button _releaseCapacityButton = null!;
    private Button _sellFringeButton = null!;
    private Button _promiseStartButton = null!;
    private Button _promisePlayingTimeButton = null!;
    private Button _refreshTransferNeedsButton = null!;
    private Button _declareTransferNeedButton = null!;
    private Button _closeTransferNeedButton = null!;
    private Button _suggestTargetButton = null!;
    private Button _dropTargetButton = null!;
    private Button _openProcessButton = null!;
    private Button _withdrawProcessButton = null!;
    private Button _requestSportingApprovalButton = null!;
    private Button _grantSportingApprovalButton = null!;
    private Button _rejectSportingApprovalButton = null!;
    private Label _clubOfferLabel = null!;
    private Button _submitClubOfferButton = null!;
    private Button _acceptClubOfferButton = null!;
    private Button _rejectClubOfferButton = null!;
    private Button _counterClubOfferButton = null!;
    private Label _contractProposalLabel = null!;
    private Button _submitContractProposalButton = null!;
    private Button _acceptContractProposalButton = null!;
    private Button _rejectContractProposalButton = null!;
    private Button _counterContractProposalButton = null!;
    private Button _requestFinancialApprovalButton = null!;
    private Button _grantFinancialApprovalButton = null!;
    private Button _rejectFinancialApprovalButton = null!;
    private Button _completeTransferButton = null!;
    private Button _trainLowButton = null!;
    private Button _trainMediumButton = null!;
    private Button _trainHighButton = null!;
    private Button _focusGeneralButton = null!;
    private Button _focusFitnessButton = null!;
    private Button _focusRecoveryButton = null!;
    private Button _focusTacticalButton = null!;
    private Button _restLightButton = null!;
    private Button _restNormalButton = null!;
    private Button _restHeavyButton = null!;
    private Button _formation442Button = null!;
    private Button _formation433Button = null!;
    private Button _formation352Button = null!;
    private Button _approachBalancedButton = null!;
    private Button _approachAttackingButton = null!;
    private Button _approachDefensiveButton = null!;
    private Button _pressingLowButton = null!;
    private Button _pressingBalancedButton = null!;
    private Button _pressingHighButton = null!;
    private Button _lineDeepButton = null!;
    private Button _lineStandardButton = null!;
    private Button _lineHighButton = null!;
    private readonly List<Button> _dualPhaseTacticButtons = [];
    private Button _playButton = null!;
    private Button _seasonTransitionButton = null!;
    private Button _advanceDayButton = null!;
    private Button _advanceWeekButton = null!;
    private Label _pageTitleLabel = null!;
    private Label _pageSubtitleLabel = null!;
    private MobileScrollContainer _pageScroll = null!;
    private Button _careerButton = null!;
    private Control[] _pages = null!;
    private Button[] _navButtons = null!;
    private HubPage _currentPage = HubPage.Today;
    private MarginContainer _shellMargin = null!;
    private HBoxContainer _topBar = null!;
    private HBoxContainer _workspace = null!;
    private GridContainer _navGrid = null!;
    private PanelContainer _statusPanel = null!;
    private PanelContainer _datePanel = null!;
    private PanelContainer _liveChip = null!;
    private Label _brandLabel = null!;
    private bool _layoutBuilt;

    private static readonly Regex InternalIdentifierPattern = new(@"\(?[A-Za-z]*#\d+\)?", RegexOptions.Compiled);

    private enum HubPage
    {
        Today = 0,
        Club = 1,
        Transfer = 2,
        Prep = 3,
        World = 4,
        File = 5,
    }

    public event Action? BackToMenuRequested;

    public event Action? MatchDayRequested;

    public event Action? ExperienceSettingsChanged;

    public CareerHubScreen(CareerSessionController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public override void _Ready()
    {
        BuildLayout();
        RefreshUi();
        ApplyResponsiveLayout();
        Callable.From(ApplyResponsiveLayout).CallDeferred();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && _layoutBuilt)
        {
            ApplyResponsiveLayout();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!GameExperienceSettingsStore.Current.GamepadNavigationHintsEnabled
            || @event is not InputEventJoypadButton { Pressed: true } joy)
        {
            return;
        }

        // Godot standard mapping: L1/R1. Directional pad continues to use
        // the engine's native spatial focus navigation.
        var direction = (int)joy.ButtonIndex switch
        {
            9 => -1,
            10 => 1,
            _ => 0,
        };
        if (direction == 0)
        {
            return;
        }

        var pageCount = Enum.GetValues<HubPage>().Length;
        var next = ((int)_currentPage + direction + pageCount) % pageCount;
        ShowPage((HubPage)next);
        _navButtons[next].GrabFocus();
        GetViewport().SetInputAsHandled();
    }

    public void SetStatus(string message) => PulseStatus(message);

    public void ShowExperienceSettings() => ShowPage(HubPage.File);

    public void ApplyOfficeReturn(Application.Competition.Queries.PostMatchOfficeDigest digest)
    {
        ArgumentNullException.ThrowIfNull(digest);
        ShowPage(HubPage.Today);
        RefreshUi();
        _officeLabel.Text = CompactOfficeText(digest);
        // Sakatlık gecesi: nabız sakin kalsa bile Toparlanma birincil CTA kalsın.
        if (string.Equals(
                digest.NextFocusCode,
                Application.CareerHub.Queries.TodayPulseDigest.FocusPrep,
                StringComparison.Ordinal))
        {
            var suggestion = _controller.BuildPreparationBriefing().Suggestion
                ?? Application.TeamPreparation.Queries.PrepPlanSuggestion.RecoveryPlan();
            BindOfficeNextStep(new Application.CareerHub.Queries.OfficeNextStep(
                suggestion.ButtonLabel,
                Application.CareerHub.Queries.OfficeNextStepGuide.TargetPrep,
                Application.CareerHub.Queries.TodayPulseDigest.FocusPrep,
                Application.CareerHub.Queries.OfficeNextStepGuide.ActionApplyPrepSuggestion));
        }

        // RefreshUi nabız CTA'sını kurar; ofis metnini üstte tut.
        PulseStatus(digest.ToStatusMessage());
    }

    public void ApplyCareerResume(Application.CareerHub.Queries.CareerResumeDigest digest)
    {
        ArgumentNullException.ThrowIfNull(digest);
        ShowPage(HubPage.Today);
        RefreshUi();
        _officeLabel.Text = CompactOfficeText(digest);
        // RefreshUi nabız CTA'sını kurar; hikâye varsa birincil düğmeyi kariyere dönüşle senkron tut.
        var resumeStep = _controller.BuildOfficeNextStep();
        if (resumeStep is not null)
        {
            BindOfficeNextStep(resumeStep);
        }

        PulseStatus(digest.ToStatusMessage());
    }

    private void BindOfficeNextStep(Application.CareerHub.Queries.OfficeNextStep? step)
    {
        if (step is null)
        {
            _officeNextStepButton.Visible = false;
            SyncTodayActionVisibility();
            return;
        }

        _officeNextStepAction = step.ActionCode;
        _officeNextStepTarget = step.TargetPageCode switch
        {
            Application.CareerHub.Queries.OfficeNextStepGuide.TargetClub => HubPage.Club,
            Application.CareerHub.Queries.OfficeNextStepGuide.TargetTransfer => HubPage.Transfer,
            Application.CareerHub.Queries.OfficeNextStepGuide.TargetPrep => HubPage.Prep,
            Application.CareerHub.Queries.OfficeNextStepGuide.TargetWorld => HubPage.World,
            _ => HubPage.Today,
        };
        _officeNextStepButton.Text = step.ButtonLabel;
        _officeNextStepButton.Visible = true;
        SyncTodayActionVisibility();
    }

    private void OnOfficeNextStepPressed()
    {
        switch (_officeNextStepAction)
        {
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionApproveSelection:
                Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionOpenMatchDay:
                OnPlayMatches();
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionAdvanceDay:
                Apply(_controller.AdvanceToNextMeaningfulPoint());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionAdvanceToNext:
                Apply(_controller.AdvanceToNextMeaningfulPoint());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionTransitionSeason:
                Apply(_controller.TransitionToNextSeason());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionApplyPrepSuggestion:
                Apply(_controller.ApplySuggestedPreparationPlan());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionSellFringe:
                Apply(_controller.SellFringePlayerFromManagedClub());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionOpenTransferWindow:
                Apply(_controller.OpenTransferWindow());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionScanNeeds:
                Apply(_controller.RefreshTransferNeedSuggestions());
                ShowPage(HubPage.Transfer);
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionPickTarget:
                Apply(_controller.SuggestTransferTarget());
                ShowPage(HubPage.Transfer);
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionStartProcess:
                Apply(_controller.OpenTransferProcessFromOldestTarget());
                ShowPage(HubPage.Transfer);
                SetTransferNegotiationExpanded(true);
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionAdvanceProcess:
                Apply(_controller.AdvanceOldestTransferStep());
                ShowPage(HubPage.Transfer);
                SetTransferNegotiationExpanded(true);
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionAnswerOffers:
                ShowPage(HubPage.Transfer);
                SetTransferNegotiationExpanded(true);
                return;
            default:
                ShowPage(_officeNextStepTarget);
                return;
        }
    }

    private void BuildLayout()
    {
        CareerUiTheme.Configure(GameExperienceSettingsStore.Current);
        CareerUiTheme.EnsureLoaded();
        AddChild(CareerUiTheme.CreateAtmosphereBackground());

        _shellMargin = new MarginContainer();
        _shellMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _shellMargin.GrowHorizontal = GrowDirection.Both;
        _shellMargin.GrowVertical = GrowDirection.Both;
        AddChild(_shellMargin);

        var shell = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddThemeConstantOverride("separation", 10);
        _shellMargin.AddChild(shell);

        // Sabit kariyer kabuğu: kulüp kimliği + tarih, ardından aktif ekran başlığı.
        _topBar = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        _topBar.AddThemeConstantOverride("separation", 10);
        shell.AddChild(_topBar);

        _clubCrest = new TextureRect
        {
            Name = "ClubCrest",
            CustomMinimumSize = new Vector2(64, 64),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = "Kulüp arması",
            Visible = false,
        };
        _topBar.AddChild(_clubCrest);

        var brandLockup = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        brandLockup.AddThemeConstantOverride("separation", 0);
        _topBar.AddChild(brandLockup);

        _brandLabel = new Label { Text = "FCS  /  KARİYER" };
        CareerUiTheme.StyleSection(_brandLabel);
        brandLockup.AddChild(_brandLabel);

        _managerLabel = BodyLabel("ManagerLabel", autowrap: true);
        CareerUiTheme.StyleHeadline(_managerLabel);
        _managerLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(22));
        brandLockup.AddChild(_managerLabel);

        _datePanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _datePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.PillPanel());
        _topBar.AddChild(_datePanel);
        _dateLabel = BodyLabel("DateLabel");
        _dateLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _dateLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        _datePanel.AddChild(_dateLabel);

        _careerButton = SecondaryButton("DOSYA");
        _careerButton.CustomMinimumSize = new Vector2(58, 48);
        _careerButton.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _careerButton.TooltipText = "Kariyer dosyası, kayıt ve ana menü";
        _careerButton.Pressed += () => ShowPage(HubPage.File);
        _topBar.AddChild(_careerButton);

        var careerMeta = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        careerMeta.AddThemeConstantOverride("separation", 2);
        shell.AddChild(careerMeta);
        _seasonLabel = BodyLabel("SeasonLabel", muted: true, autowrap: true);
        careerMeta.AddChild(_seasonLabel);
        _progressLabel = BodyLabel("ProgressLabel", muted: true, autowrap: true);
        _progressLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        careerMeta.AddChild(_progressLabel);
        careerMeta.Visible = false;

        var screenHeading = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        screenHeading.AddThemeConstantOverride("separation", 10);
        shell.AddChild(screenHeading);
        var headingCopy = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headingCopy.AddThemeConstantOverride("separation", 0);
        screenHeading.AddChild(headingCopy);
        _pageTitleLabel = new Label();
        CareerUiTheme.StyleHeadline(_pageTitleLabel);
        _pageTitleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(26));
        headingCopy.AddChild(_pageTitleLabel);
        _pageSubtitleLabel = BodyLabel("PageSubtitle", muted: true, autowrap: true);
        _pageSubtitleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        _pageSubtitleLabel.Visible = false;
        headingCopy.AddChild(_pageSubtitleLabel);

        _liveChip = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _liveChip.AddThemeStyleboxOverride("panel", CareerUiTheme.LivePillPanel());
        var liveLabel = new Label { Text = "●  CANLI" };
        CareerUiTheme.StyleBody(liveLabel);
        liveLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        liveLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _liveChip.AddChild(liveLabel);
        _liveChip.Visible = false;
        screenHeading.AddChild(_liveChip);

        _workspace = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _workspace.AddThemeConstantOverride("separation", 10);
        shell.AddChild(_workspace);
        BuildNavBar(_workspace);

        _pageScroll = new MobileScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _workspace.AddChild(_pageScroll);

        var pageHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        pageHost.AddThemeConstantOverride("separation", 0);
        _pageScroll.AddChild(pageHost);

        _pages =
        [
            BuildTodayPage(),
            BuildClubPage(),
            BuildTransferPage(),
            BuildPrepPage(),
            BuildWorldPage(),
            BuildFilePage(),
        ];
        foreach (var page in _pages)
        {
            page.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            pageHost.AddChild(page);
        }

        _statusLabel = BodyLabel("StatusLabel", autowrap: true);
        _statusLabel.CustomMinimumSize = new Vector2(0, 42);
        _statusLabel.VerticalAlignment = VerticalAlignment.Center;
        _statusPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _statusPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatusPanel());
        _statusPanel.AddChild(_statusLabel);
        _statusPanel.Visible = false;
        shell.AddChild(_statusPanel);

        BuildPlayerDossierOverlay();
        _layoutBuilt = true;

        ShowPage(HubPage.Today);

        if (CareerUiTheme.ReducedMotion)
        {
            shell.Modulate = Colors.White;
        }
        else
        {
            shell.Modulate = new Color(1f, 1f, 1f, 0f);
            var fadeTween = CreateTween();
            fadeTween.TweenProperty(shell, "modulate:a", 1f, 0.28f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
        }
    }

    private void BuildNavBar(Control parent)
    {
        var navPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(112, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        navPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NavigationPanel());
        parent.AddChild(navPanel);

        _navGrid = new GridContainer
        {
            Columns = 1,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _navGrid.AddThemeConstantOverride("h_separation", 4);
        _navGrid.AddThemeConstantOverride("v_separation", 4);
        navPanel.AddChild(_navGrid);

        var labels = new[] { "MERKEZ", "KADRO", "TRANSFER", "TAKTİK", "LİG" };
        _navButtons = new Button[6];
        for (var i = 0; i < labels.Length; i++)
        {
            var page = (HubPage)i;
            var button = new Button
            {
                Text = labels[i],
                ToggleMode = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            button.Pressed += () => ShowPage(page);
            _navGrid.AddChild(button);
            _navButtons[i] = button;
        }

        _navButtons[(int)HubPage.File] = _careerButton;
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
            Color = new Color(0.01f, 0.03f, 0.02f, 0.72f),
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
            CustomMinimumSize = new Vector2(520, 460),
            MouseFilter = MouseFilterEnum.Stop,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.HeroPanel());
        center.AddChild(panel);

        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", 12);
        panel.AddChild(content);

        var title = SectionTitle("FUTBOLCU DOSYASI");
        content.AddChild(title);
        _playerDossierTitle = BodyLabel("PlayerDossierTitle");
        CareerUiTheme.StyleHeadline(_playerDossierTitle);
        content.AddChild(_playerDossierTitle);
        _playerDossierBody = BodyLabel("PlayerDossierBody", autowrap: true);
        _playerDossierBody.SizeFlagsVertical = SizeFlags.ExpandFill;
        content.AddChild(_playerDossierBody);

        _playerDossierSellButton = PrimaryButton("Satışa Çıkar");
        _playerDossierSellButton.Pressed += OnPlayerDossierSellPressed;
        content.AddChild(_playerDossierSellButton);

        var close = SecondaryButton("Kapat");
        close.Pressed += ClosePlayerDossier;
        content.AddChild(close);
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
            return;
        }

        _selectedPlayerId = playerId;
        _playerDossierTitle.Text = player.DisplayName;
        _playerDossierBody.Text = player.ToDossierText();
        _playerDossierOverlay.Visible = true;
        RefreshPlayerDetail();
        RefreshPlayerDossierSellButton();
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

    private void RefreshPlayerDossierSellButton()
    {
        if (_playerDossierSellButton is null)
        {
            return;
        }

        var employed = _controller.Host.ManagerModule.Queries.GetCareer().EmployedClubId is not null;
        var windowOpen = _controller.Host.WorldModule.Queries.GetTransferWindow().IsOpen;
        var hasPlayer = _selectedPlayerId is > 0;
        _playerDossierSellButton.Visible = employed && hasPlayer;
        _playerDossierSellButton.Disabled = !windowOpen || !hasPlayer;
        _playerDossierSellButton.Text = windowOpen
            ? "Satışa Çıkar"
            : "Satışa Çıkar (pencere kapalı)";
    }

    private void ClosePlayerDossier()
    {
        if (_playerDossierOverlay is not null)
        {
            _playerDossierOverlay.Visible = false;
        }
    }

    private void ShowPage(HubPage page)
    {
        ClosePlayerDossier();
        _currentPage = page;
        var titles = new[] { "Merkez", "Kadro", "Transfer Merkezi", "Taktik & Antrenman", "Lig Merkezi", "Kariyer" };
        var subtitles = new[]
        {
            "Bugünün kritik kararları ve sıradaki hamle",
            "Oyuncular, sözler ve soyunma odası",
            "İhtiyaçtan imzaya bütün transfer dosyaları",
            "Haftanın yükü ve maç planı",
            "Puan durumu, fikstür ve sezon akışı",
            "Menajer yolculuğu, kayıt ve kulüp geleceği",
        };
        _pageTitleLabel.Text = titles[(int)page];
        _pageSubtitleLabel.Text = subtitles[(int)page];
        for (var i = 0; i < _pages.Length; i++)
        {
            _pages[i].Visible = i == (int)page;
            CareerUiTheme.StyleNavButton(_navButtons[i], selected: i == (int)page);
        }

        var current = _pages[(int)page];
        current.Modulate = new Color(1f, 1f, 1f, 0.25f);
        if (CareerUiTheme.ReducedMotion)
        {
            current.Modulate = Colors.White;
        }
        else
        {
            CreateTween()
                .TweenProperty(current, "modulate:a", 1f, 0.2f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
        }
        Callable.From(() => _pageScroll.ScrollVertical = 0).CallDeferred();
    }

    private void ApplyResponsiveLayout()
    {
        if (!_layoutBuilt)
        {
            return;
        }

        var safe = DisplaySafeAreaInsets.Resolve(Size);
        var profile = MobileUiLayoutProfile.Resolve(
            Mathf.RoundToInt(Size.X),
            Mathf.RoundToInt(Size.Y),
            safe.Left,
            safe.Top,
            safe.Right,
            safe.Bottom);
        _shellMargin.AddThemeConstantOverride("margin_left", profile.LeftMargin);
        _shellMargin.AddThemeConstantOverride("margin_right", profile.RightMargin);
        _shellMargin.AddThemeConstantOverride("margin_top", profile.TopMargin);
        _shellMargin.AddThemeConstantOverride("margin_bottom", profile.BottomMargin);
        _topBar.AddThemeConstantOverride("separation", profile.IsCompact ? 6 : 10);
        _brandLabel.Visible = !profile.IsCompact;
        _clubCrest.CustomMinimumSize = new Vector2(profile.CrestSize, profile.CrestSize);
        _managerLabel.AddThemeFontSizeOverride(
            "font_size",
            CareerUiTheme.FontSize(profile.IsCompact ? 18 : 22));
        _pageTitleLabel.AddThemeFontSizeOverride(
            "font_size",
            CareerUiTheme.FontSize(profile.PageTitleFontSize));
        _liveChip.Visible = false;
        _dateLabel.AddThemeFontSizeOverride(
            "font_size",
            CareerUiTheme.FontSize(profile.IsCompact ? 11 : 12));
        _careerButton.CustomMinimumSize = new Vector2(profile.IsCompact ? 54 : 58, profile.TouchTargetHeight);
        _statusLabel.CustomMinimumSize = new Vector2(0, profile.IsCompact ? 46 : 42);
        _navGrid.Columns = Size.X > Size.Y ? 1 : profile.NavigationColumns;
        foreach (var button in _navButtons.Where(button => button is not null).Distinct())
        {
            button.CustomMinimumSize = new Vector2(0, profile.TouchTargetHeight);
        }

        _standingsTable.CustomMinimumSize = new Vector2(0, 500);
    }

    private VBoxContainer PageRoot()
    {
        var page = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        page.AddThemeConstantOverride("separation", 14);
        return page;
    }

    private static VBoxContainer AddCard(VBoxContainer page, string title, bool emphasized = false)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized));
        page.AddChild(panel);

        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", 9);
        panel.AddChild(content);
        content.AddChild(SectionTitle(title));
        return content;
    }

    private static HFlowContainer ActionFlow()
    {
        var flow = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        flow.AddThemeConstantOverride("h_separation", 8);
        flow.AddThemeConstantOverride("v_separation", 8);
        return flow;
    }

    private Control BuildTodayPage()
    {
        var page = PageRoot();
        var dashboard = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        dashboard.AddThemeConstantOverride("separation", 12);
        page.AddChild(dashboard);

        var matchColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.45f,
        };
        matchColumn.AddThemeConstantOverride("separation", 10);
        dashboard.AddChild(matchColumn);

        var sideColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        sideColumn.AddThemeConstantOverride("separation", 10);
        dashboard.AddChild(sideColumn);

        var matchCard = AddCard(matchColumn, "SIRADAKİ MAÇ", emphasized: true);
        _briefingLabel = BodyLabel("BriefingLabel", autowrap: true);
        matchCard.AddChild(_briefingLabel);
        _selectionLabel = BodyLabel("SelectionLabel", muted: true, autowrap: true);
        matchCard.AddChild(_selectionLabel);

        var primaryRow = ActionFlow();
        matchCard.AddChild(primaryRow);
        _approveSelectionButton = PrimaryButton("Kadro Onayla");
        _approveSelectionButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        primaryRow.AddChild(_approveSelectionButton);
        _playButton = PrimaryButton("Maç Merkezine Git");
        _playButton.Pressed += OnPlayMatches;
        primaryRow.AddChild(_playButton);
        _swapSelectionButton = SecondaryButton("XI↔Yedek");
        _swapSelectionButton.Pressed += () => Apply(_controller.SwapLastStarterWithFirstBenchForNextDueMatch());
        primaryRow.AddChild(_swapSelectionButton);

        _seasonTransitionButton = PrimaryButton("Sezonu Bitir → Yeni Sezon");
        _seasonTransitionButton.Pressed += () => Apply(_controller.TransitionToNextSeason());
        matchColumn.AddChild(_seasonTransitionButton);
        var timeRow = ActionFlow();
        matchColumn.AddChild(timeRow);
        _advanceDayButton = SecondaryButton("1 Gün İlerlet");
        _advanceDayButton.Pressed += () => Apply(_controller.AdvanceDays(1));
        timeRow.AddChild(_advanceDayButton);
        _advanceWeekButton = SecondaryButton("7 Gün İlerlet");
        _advanceWeekButton.Pressed += () => Apply(_controller.AdvanceDays(7));
        timeRow.AddChild(_advanceWeekButton);

        var priorityCard = AddCard(sideColumn, "BUGÜN", emphasized: true);
        _blockerLabel = BodyLabel("BlockerLabel", autowrap: true);
        priorityCard.AddChild(_blockerLabel);
        _pulseLabel = BodyLabel("PulseLabel", autowrap: true);
        priorityCard.AddChild(_pulseLabel);

        _weekStoryLabel = BodyLabel("WeekStoryLabel", autowrap: true);
        _weekStoryLabel.Visible = false;
        priorityCard.AddChild(_weekStoryLabel);

        _recoveryPathLabel = BodyLabel("RecoveryPathLabel", autowrap: true);
        _recoveryPathLabel.Visible = false;
        priorityCard.AddChild(_recoveryPathLabel);

        _officeNextStepButton = PrimaryButton("Sıradaki Adım");
        _officeNextStepButton.Visible = false;
        _officeNextStepButton.Pressed += OnOfficeNextStepPressed;
        priorityCard.AddChild(_officeNextStepButton);

        var officeCard = AddCard(sideColumn, "SON GELİŞME");
        _officeLabel = BodyLabel("OfficeLabel", autowrap: true);
        _officeLabel.Text = CompactOfficeText(Application.Competition.Queries.PostMatchOfficeDigest.Quiet());
        officeCard.AddChild(_officeLabel);

        var guideHost = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        sideColumn.AddChild(guideHost);
        _firstWeekGuideCard = guideHost;
        var guideCard = AddCard(guideHost, "İLK HAFTA REHBERİ", emphasized: true);
        _firstWeekGuideLabel = BodyLabel("FirstWeekGuideLabel", autowrap: true);
        guideCard.AddChild(_firstWeekGuideLabel);
        var guideRow = ActionFlow();
        guideCard.AddChild(guideRow);
        _firstWeekGuideOpenButton = PrimaryButton("Ekranı Aç");
        _firstWeekGuideOpenButton.Pressed += OpenCurrentGuideStep;
        guideRow.AddChild(_firstWeekGuideOpenButton);
        _firstWeekGuideNextButton = SecondaryButton("Tamamlandı →");
        _firstWeekGuideNextButton.Pressed += CompleteCurrentGuideStep;
        guideRow.AddChild(_firstWeekGuideNextButton);

        var decisionCard = AddCard(sideColumn, "KARAR MASASI");
        _deskLabel = BodyLabel("DeskLabel", autowrap: true);
        decisionCard.AddChild(_deskLabel);

        var deskRow = ActionFlow();
        decisionCard.AddChild(deskRow);
        _grantDecisionButton = PrimaryButton("Talebi Kabul Et");
        _grantDecisionButton.Pressed += () => Apply(_controller.AnswerOldestPendingDecision(grantPromise: true));
        deskRow.AddChild(_grantDecisionButton);
        _refuseDecisionButton = SecondaryButton("Talebi Reddet");
        _refuseDecisionButton.Pressed += () => Apply(_controller.AnswerOldestPendingDecision(grantPromise: false));
        deskRow.AddChild(_refuseDecisionButton);
        _disciplineWarningButton = SecondaryButton("Uyarı Ver");
        _disciplineWarningButton.Pressed += () =>
            Apply(_controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionIssueWarning));
        deskRow.AddChild(_disciplineWarningButton);
        _disciplineFineButton = SecondaryButton("Ceza Uygula");
        _disciplineFineButton.Pressed += () =>
            Apply(_controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionIssueFine));
        deskRow.AddChild(_disciplineFineButton);
        _disciplineSupportButton = SecondaryButton("Destekle");
        _disciplineSupportButton.Pressed += () =>
            Apply(_controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionOfferSupport));
        deskRow.AddChild(_disciplineSupportButton);
        _boardCounterButton = SecondaryButton("Karşı Teklif");
        _boardCounterButton.Pressed += () =>
            Apply(_controller.AnswerOldestPendingWithOption(
                Domain.Interaction.DecisionRequest.OptionCounterBoardDemand));
        deskRow.AddChild(_boardCounterButton);
        _pressCriticizeButton = SecondaryButton("Kamuya Eleştir");
        _pressCriticizeButton.Pressed += () =>
            Apply(_controller.AnswerOldestPendingWithOption(
                Domain.Interaction.DecisionRequest.OptionPubliclyCriticize));
        deskRow.AddChild(_pressCriticizeButton);

        return page;
    }

    private Control BuildClubPage()
    {
        var page = PageRoot();
        var squadCard = AddCard(page, "KADRO DURUMU", emphasized: true);
        _squadCapacityLabel = BodyLabel("SquadCapacityLabel", autowrap: true);
        squadCard.AddChild(_squadCapacityLabel);
        _squadStatusLabel = BodyLabel("SquadStatusLabel", autowrap: true);
        squadCard.AddChild(_squadStatusLabel);

        var pitchCard = AddCard(page, "SAHA YERLEŞİMİ", emphasized: true);
        _clubPitchHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 340),
        };
        pitchCard.AddChild(_clubPitchHost);
        _developmentLabel = BodyLabel("DevelopmentLabel", autowrap: true);
        _developmentLabel.Visible = false;
        squadCard.AddChild(_developmentLabel);
        _contractLabel = BodyLabel("ContractLabel", autowrap: true);
        _contractLabel.Visible = false;
        squadCard.AddChild(_contractLabel);

        var kitStrip = new HBoxContainer
        {
            Name = "ClubKitStrip",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        kitStrip.AddThemeConstantOverride("separation", 18);
        kitStrip.Visible = false;
        squadCard.AddChild(kitStrip);
        _homeKit = AddKitPreview(kitStrip, "İÇ SAHA", "Kulübün resmi iç saha forması");
        _awayKit = AddKitPreview(kitStrip, "DEPLASMAN", "Kulübün resmi deplasman forması");
        _thirdKit = AddKitPreview(kitStrip, "ÜÇÜNCÜ", "Kulübün resmi üçüncü forması");

        var academyCard = AddCard(page, "ALTYAPI DEĞERLENDİRME GÜNÜ", emphasized: true);
        _academyIntakeLabel = BodyLabel("AcademyIntakeLabel", autowrap: true);
        academyCard.AddChild(_academyIntakeLabel);
        _academyCandidateList = new ItemList
        {
            Name = "AcademyCandidateList",
            CustomMinimumSize = new Vector2(0, 250),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_academyCandidateList);
        _academyCandidateList.ItemSelected += OnAcademyCandidateSelected;
        academyCard.AddChild(_academyCandidateList);
        var academyRow = ActionFlow();
        academyCard.AddChild(academyRow);
        _academyAcceptButton = PrimaryButton("Akademide Tut");
        _academyAcceptButton.Pressed += () => ApplySelectedAcademyCandidate(accept: true);
        academyRow.AddChild(_academyAcceptButton);
        _academyRejectButton = SecondaryButton("Değerlendirmeyi Kapat");
        _academyRejectButton.Pressed += () => ApplySelectedAcademyCandidate(accept: false);
        academyRow.AddChild(_academyRejectButton);

        var academyDevelopmentCard = AddCard(page, "AKADEMİ GELİŞİM MERKEZİ", emphasized: true);
        _academyLifecycleLabel = BodyLabel("AcademyLifecycleLabel", autowrap: true);
        academyDevelopmentCard.AddChild(_academyLifecycleLabel);
        _academyLifecycleList = new ItemList
        {
            Name = "AcademyLifecycleList",
            CustomMinimumSize = new Vector2(0, 220),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_academyLifecycleList);
        _academyLifecycleList.ItemSelected += OnAcademyLifecyclePlayerSelected;
        academyDevelopmentCard.AddChild(_academyLifecycleList);
        _academyPromoteButton = PrimaryButton("A Takıma Çıkar");
        _academyPromoteButton.Pressed += PromoteSelectedAcademyPlayer;
        academyDevelopmentCard.AddChild(_academyPromoteButton);

        var teamDynamicsCard = AddCard(page, "SOYUNMA ODASI & SÖZLER");
        _dressingRoomEchoLabel = BodyLabel("DressingRoomEchoLabel", autowrap: true);
        _dressingRoomEchoLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        teamDynamicsCard.AddChild(_dressingRoomEchoLabel);
        _memoryLabel = BodyLabel("MemoryLabel", autowrap: true);
        teamDynamicsCard.AddChild(_memoryLabel);
        _promiseLabel = BodyLabel("PromiseLabel", autowrap: true);
        teamDynamicsCard.AddChild(_promiseLabel);
        _relationshipLabel = BodyLabel("RelationshipLabel", autowrap: true);
        teamDynamicsCard.AddChild(_relationshipLabel);
        _decisionLabel = BodyLabel("DecisionLabel", autowrap: true);
        teamDynamicsCard.AddChild(_decisionLabel);

        var playerManagementCard = AddCard(page, "FUTBOLCU YÖNETİMİ", emphasized: true);
        _playerManagementHeadlineLabel = BodyLabel("PlayerManagementHeadlineLabel", autowrap: true);
        playerManagementCard.AddChild(_playerManagementHeadlineLabel);
        _squadList = new ItemList
        {
            Name = "SquadList",
            CustomMinimumSize = new Vector2(0, 620),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_squadList);
        _squadList.AllowReselect = true;
        _squadList.ItemSelected += OnSquadPlayerSelected;
        _squadList.ItemClicked += OnSquadPlayerClicked;
        playerManagementCard.AddChild(_squadList);
        _playerDetailLabel = BodyLabel("PlayerDetailLabel", muted: true, autowrap: true);
        _playerDetailLabel.Text = "Oyuncu dosyası için isme dokun.";
        playerManagementCard.AddChild(_playerDetailLabel);

        var actionCard = AddCard(page, "KADRO & KARİYER AKSİYONLARI");
        var jobRow = ActionFlow();
        actionCard.AddChild(jobRow);

        _generateOfferButton = SecondaryButton("İş Teklifi Ara");
        _generateOfferButton.Pressed += () => Apply(_controller.GenerateJobOffer());
        jobRow.AddChild(_generateOfferButton);

        _acceptOfferButton = SecondaryButton("Teklifi Kabul Et");
        _acceptOfferButton.Pressed += () => Apply(_controller.AcceptJobOffer());
        jobRow.AddChild(_acceptOfferButton);

        _signFreeAgentButton = SecondaryButton("Serbesti Geri İmzala");
        _signFreeAgentButton.Pressed += () => Apply(_controller.SignNextFreeAgentToManagedClub());
        jobRow.AddChild(_signFreeAgentButton);

        _promoteOverflowButton = PrimaryButton("Taşanı Kadroya Al");
        _promoteOverflowButton.Pressed += () => Apply(_controller.PromoteOverflowPlayerToSquad());
        jobRow.AddChild(_promoteOverflowButton);

        _releaseCapacityButton = PrimaryButton("Yer Aç");
        _releaseCapacityButton.Pressed += () => Apply(_controller.ReleaseToFreeSquadCapacity());
        jobRow.AddChild(_releaseCapacityButton);

        _sellFringeButton = PrimaryButton("Satışa Çıkar");
        _sellFringeButton.Pressed += () => Apply(_controller.SellFringePlayerFromManagedClub());
        jobRow.AddChild(_sellFringeButton);

        _promiseStartButton = SecondaryButton("İlk 11 Sözü Ver");
        _promiseStartButton.Pressed += () =>
            ApplySelectedPlayer(_controller.PromiseStartingOpportunityToPlayer);
        jobRow.AddChild(_promiseStartButton);

        _promisePlayingTimeButton = SecondaryButton("Oyun Süresi Sözü");
        _promisePlayingTimeButton.Pressed += () =>
            ApplySelectedPlayer(_controller.PromisePlayingTimeToPlayer);
        jobRow.AddChild(_promisePlayingTimeButton);

        var decisionRow = ActionFlow();
        decisionRow.Visible = false; // Geliştirici tetikleri: gerçek oyuncu akışında olaylar domain'den açılır.
        teamDynamicsCard.AddChild(decisionRow);
        _openDecisionButton = SecondaryButton("Süre Talebi Aç");
        _openDecisionButton.Pressed += () =>
            ApplySelectedPlayer(_controller.OpenPlayingTimeDecisionForPlayer);
        decisionRow.AddChild(_openDecisionButton);
        _openStartingDecisionButton = SecondaryButton("İlk 11 Talebi Aç");
        _openStartingDecisionButton.Pressed += () =>
            ApplySelectedPlayer(_controller.OpenStartingOpportunityDecisionForPlayer);
        decisionRow.AddChild(_openStartingDecisionButton);
        _openTransferDecisionButton = SecondaryButton("Transfer Talebi Aç");
        _openTransferDecisionButton.Pressed += () =>
            ApplySelectedPlayer(_controller.OpenTransferDecisionForPlayer);
        decisionRow.AddChild(_openTransferDecisionButton);
        _openDisciplineDecisionButton = SecondaryButton("Disiplin Aç");
        _openDisciplineDecisionButton.Pressed += () =>
            ApplySelectedPlayer(_controller.OpenDisciplineDecisionForPlayer);
        decisionRow.AddChild(_openDisciplineDecisionButton);
        _openBoardDemandDecisionButton = SecondaryButton("Yönetim Talebi Aç");
        _openBoardDemandDecisionButton.Pressed += () => Apply(_controller.OpenBoardDemandDecision());
        decisionRow.AddChild(_openBoardDemandDecisionButton);
        _openPressQuestionDecisionButton = SecondaryButton("Basın Sorusu Aç");
        _openPressQuestionDecisionButton.Pressed += () =>
            Apply(_controller.OpenPressQuestionDecisionForOldestSquadPlayer());
        decisionRow.AddChild(_openPressQuestionDecisionButton);

        var clubDetailsToggle = SecondaryButton("Kadro ayrintilari");
        squadCard.AddChild(clubDetailsToggle);
        var optionalClubCards = new[]
        {
            teamDynamicsCard.GetParent<Control>(),
            actionCard.GetParent<Control>(),
        };
        foreach (var card in optionalClubCards)
        {
            card.Visible = false;
        }
        clubDetailsToggle.Pressed += () =>
        {
            var visible = !optionalClubCards[0].Visible;
            foreach (var card in optionalClubCards)
            {
                card.Visible = visible;
            }
            clubDetailsToggle.Text = visible ? "Kadro ayrintilarini gizle" : "Kadro ayrintilari";
        };
        return page;
    }

    private Control BuildTransferPage()
    {
        var page = PageRoot();
        var overviewCard = AddCard(page, "TRANSFER PENCERESİ", emphasized: true);
        _transferDeskLabel = BodyLabel("TransferDeskLabel", autowrap: true);
        overviewCard.AddChild(_transferDeskLabel);

        _transferNextStepButton = PrimaryButton("Sıradaki Adım");
        _transferNextStepButton.Visible = false;
        _transferNextStepButton.Pressed += OnTransferNextStepPressed;
        overviewCard.AddChild(_transferNextStepButton);

        _transferScoutToggle = SecondaryButton("Scout raporu al");
        _transferScoutToggle.Pressed += ToggleTransferScoutDetails;
        overviewCard.AddChild(_transferScoutToggle);

        _transferScoutDetails = new VBoxContainer
        {
            Name = "TransferScoutDetails",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        _transferScoutDetails.AddThemeConstantOverride("separation", 8);
        overviewCard.AddChild(_transferScoutDetails);
        _transferScoutReportLabel = BodyLabel("TransferScoutReportLabel", autowrap: true);
        _transferScoutDetails.AddChild(_transferScoutReportLabel);
        _transferWindowLabel = BodyLabel("TransferWindowLabel", muted: true, autowrap: true);
        _transferScoutDetails.AddChild(_transferWindowLabel);
        _transferBudgetLabel = BodyLabel("TransferBudgetLabel", muted: true, autowrap: true);
        _transferScoutDetails.AddChild(_transferBudgetLabel);

        var windowRow = ActionFlow();
        windowRow.Visible = false; // Pencere takvim/OfficeNextStep tarafından yönetilir.
        overviewCard.AddChild(windowRow);

        _openTransferWindowButton = SecondaryButton("Pencere Aç");
        _openTransferWindowButton.Pressed += () => Apply(_controller.OpenTransferWindow());
        windowRow.AddChild(_openTransferWindowButton);

        _closeTransferWindowButton = SecondaryButton("Pencere Kapat");
        _closeTransferWindowButton.Pressed += () => Apply(_controller.CloseTransferWindow());
        windowRow.AddChild(_closeTransferWindowButton);

        var scoutingCard = AddCard(page, "SCOUT EKİBİ");
        _scoutReportLabel = BodyLabel("ScoutReportLabel", autowrap: true);
        scoutingCard.AddChild(_scoutReportLabel);
        _scoutCandidateList = new ItemList
        {
            Name = "ScoutCandidateList",
            CustomMinimumSize = new Vector2(0, 300),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_scoutCandidateList);
        _scoutCandidateList.ItemSelected += OnScoutCandidateSelected;
        scoutingCard.AddChild(_scoutCandidateList);
        _transferNeedLabel = BodyLabel("TransferNeedLabel", autowrap: true);
        scoutingCard.AddChild(_transferNeedLabel);

        var needRow = ActionFlow();
        scoutingCard.AddChild(needRow);

        _refreshTransferNeedsButton = SecondaryButton("İhtiyaç Tara");
        _refreshTransferNeedsButton.Pressed += () => Apply(_controller.RefreshTransferNeedSuggestions());
        needRow.AddChild(_refreshTransferNeedsButton);

        _declareTransferNeedButton = SecondaryButton("Pozisyon İhtiyacı");
        _declareTransferNeedButton.Pressed += () => Apply(_controller.DeclarePositionGapNeed());
        needRow.AddChild(_declareTransferNeedButton);

        _closeTransferNeedButton = SecondaryButton("İhtiyacı Kapat");
        _closeTransferNeedButton.Pressed += () => Apply(_controller.CloseOldestOpenTransferNeed());
        needRow.AddChild(_closeTransferNeedButton);

        _shortlistTargetLabel = BodyLabel("ShortlistTargetLabel", autowrap: true);
        scoutingCard.AddChild(_shortlistTargetLabel);

        var targetRow = ActionFlow();
        scoutingCard.AddChild(targetRow);

        _suggestTargetButton = SecondaryButton("Seçileni Kısa Listeye Al");
        _suggestTargetButton.Pressed += AddSelectedScoutCandidate;
        targetRow.AddChild(_suggestTargetButton);

        _dropTargetButton = SecondaryButton("Hedefi Düşür");
        _dropTargetButton.Pressed += () => Apply(_controller.DropOldestListedTransferTarget());
        targetRow.AddChild(_dropTargetButton);

        var processCard = AddCard(page, "2  SÜREÇ & SPORTİF ONAY");
        _transferProcessLabel = BodyLabel("TransferProcessLabel", autowrap: true);
        processCard.AddChild(_transferProcessLabel);

        var processRow = ActionFlow();
        processCard.AddChild(processRow);

        _openProcessButton = SecondaryButton("Süreç Aç");
        _openProcessButton.Pressed += () => Apply(_controller.OpenTransferProcessFromOldestTarget());
        processRow.AddChild(_openProcessButton);

        _withdrawProcessButton = SecondaryButton("Süreci Geri Çek");
        _withdrawProcessButton.Pressed += () => Apply(_controller.WithdrawOldestActiveTransferProcess());
        processRow.AddChild(_withdrawProcessButton);

        var sportingRow = ActionFlow();
        processCard.AddChild(sportingRow);

        _requestSportingApprovalButton = SecondaryButton("Sportif Onay İste");
        _requestSportingApprovalButton.Pressed += () =>
            Apply(_controller.RequestSportingApprovalForOldestProcess());
        sportingRow.AddChild(_requestSportingApprovalButton);

        _grantSportingApprovalButton = SecondaryButton("Onayla");
        _grantSportingApprovalButton.Pressed += () =>
            Apply(_controller.GrantSportingApprovalForOldestPendingProcess());
        sportingRow.AddChild(_grantSportingApprovalButton);

        _rejectSportingApprovalButton = SecondaryButton("Reddet");
        _rejectSportingApprovalButton.Pressed += () =>
            Apply(_controller.RejectSportingApprovalForOldestPendingProcess());
        sportingRow.AddChild(_rejectSportingApprovalButton);

        var offerCard = AddCard(page, "3  KULÜP TEKLİFİ");
        _clubOfferLabel = BodyLabel("ClubOfferLabel", autowrap: true);
        offerCard.AddChild(_clubOfferLabel);

        var offerRow = ActionFlow();
        offerCard.AddChild(offerRow);

        _submitClubOfferButton = SecondaryButton("Teklif Sun");
        _submitClubOfferButton.Pressed += () => Apply(_controller.SubmitDefaultClubOffer());
        offerRow.AddChild(_submitClubOfferButton);

        _acceptClubOfferButton = SecondaryButton("Teklifi Kabul");
        _acceptClubOfferButton.Pressed += () => Apply(_controller.AcceptPendingClubOffer());
        offerRow.AddChild(_acceptClubOfferButton);

        _rejectClubOfferButton = SecondaryButton("Teklifi Ret");
        _rejectClubOfferButton.Pressed += () => Apply(_controller.RejectPendingClubOffer());
        offerRow.AddChild(_rejectClubOfferButton);

        _counterClubOfferButton = SecondaryButton("Karşı Teklif");
        _counterClubOfferButton.Pressed += () => Apply(_controller.CounterPendingClubOffer());
        offerRow.AddChild(_counterClubOfferButton);

        var contractCard = AddCard(page, "4  OYUNCU SÖZLEŞMESİ");
        _contractProposalLabel = BodyLabel("ContractProposalLabel", autowrap: true);
        contractCard.AddChild(_contractProposalLabel);

        var proposalRow = ActionFlow();
        contractCard.AddChild(proposalRow);

        _submitContractProposalButton = SecondaryButton("Sözleşme Teklif");
        _submitContractProposalButton.Pressed += () =>
            Apply(_controller.SubmitDefaultContractProposal());
        proposalRow.AddChild(_submitContractProposalButton);

        _acceptContractProposalButton = SecondaryButton("Sözleşme Kabul");
        _acceptContractProposalButton.Pressed += () =>
            Apply(_controller.AcceptPendingContractProposal());
        proposalRow.AddChild(_acceptContractProposalButton);

        _rejectContractProposalButton = SecondaryButton("Sözleşme Ret");
        _rejectContractProposalButton.Pressed += () =>
            Apply(_controller.RejectPendingContractProposal());
        proposalRow.AddChild(_rejectContractProposalButton);

        _counterContractProposalButton = SecondaryButton("Karşı Sözleşme");
        _counterContractProposalButton.Pressed += () =>
            Apply(_controller.CounterPendingContractProposal());
        proposalRow.AddChild(_counterContractProposalButton);

        var financeCard = AddCard(page, "5  MALİ ONAY & İMZA", emphasized: true);
        var financialRow = ActionFlow();
        financeCard.AddChild(financialRow);

        _requestFinancialApprovalButton = SecondaryButton("Mali Onay İste");
        _requestFinancialApprovalButton.Pressed += () =>
            Apply(_controller.RequestFinancialApprovalForOldestProcess());
        financialRow.AddChild(_requestFinancialApprovalButton);

        _grantFinancialApprovalButton = SecondaryButton("Mali Onayla");
        _grantFinancialApprovalButton.Pressed += () =>
            Apply(_controller.GrantFinancialApprovalForOldestPendingProcess());
        financialRow.AddChild(_grantFinancialApprovalButton);

        _rejectFinancialApprovalButton = SecondaryButton("Mali Reddet");
        _rejectFinancialApprovalButton.Pressed += () =>
            Apply(_controller.RejectFinancialApprovalForOldestPendingProcess());
        financialRow.AddChild(_rejectFinancialApprovalButton);

        _completeTransferButton = SecondaryButton("Transferi Tamamla");
        _completeTransferButton.Pressed += () =>
            Apply(_controller.CompleteOldestFinanciallyApprovedProcess());
        financialRow.AddChild(_completeTransferButton);

        _transferNegotiationToggle = SecondaryButton("Muzakere adimlari");
        overviewCard.AddChild(_transferNegotiationToggle);
        _transferNegotiationCards =
        [
            processCard.GetParent<Control>(),
            offerCard.GetParent<Control>(),
            contractCard.GetParent<Control>(),
            financeCard.GetParent<Control>(),
        ];
        foreach (var card in _transferNegotiationCards)
        {
            card.Visible = false;
        }

        _transferNegotiationToggle.Pressed += () =>
            SetTransferNegotiationExpanded(!_transferNegotiationCards[0].Visible);
        return page;
    }

    private void OnTransferNextStepPressed()
    {
        switch (_transferNextStepAction)
        {
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionSellFringe:
                Apply(_controller.SellFringePlayerFromManagedClub());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionOpenTransferWindow:
                Apply(_controller.OpenTransferWindow());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionScanNeeds:
                Apply(_controller.RefreshTransferNeedSuggestions());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionPickTarget:
                Apply(_controller.SuggestTransferTarget());
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionStartProcess:
                Apply(_controller.OpenTransferProcessFromOldestTarget());
                SetTransferNegotiationExpanded(true);
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionAdvanceProcess:
                Apply(_controller.AdvanceOldestTransferStep());
                SetTransferNegotiationExpanded(true);
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionAnswerOffers:
                SetTransferNegotiationExpanded(true);
                return;
            default:
                if (string.Equals(
                        _transferNextStepTarget,
                        Application.Transfer.Queries.TransferNextStep.TargetToday,
                        StringComparison.Ordinal))
                {
                    ShowPage(HubPage.Today);
                }

                return;
        }
    }

    private void SetTransferNegotiationExpanded(bool expanded)
    {
        if (_transferNegotiationCards.Length == 0)
        {
            return;
        }

        foreach (var card in _transferNegotiationCards)
        {
            card.Visible = expanded;
        }

        _transferNegotiationToggle.Text = expanded
            ? "Muzakere adimlarini gizle"
            : "Muzakere adimlari";
    }

    private Control BuildPrepPage()
    {
        var page = PageRoot();
        var briefingCard = AddCard(page, "HAFTANIN PLANI", emphasized: true);
        _prepBriefingLabel = BodyLabel("PrepBriefingLabel", autowrap: true);
        briefingCard.AddChild(_prepBriefingLabel);

        var trainingCard = AddCard(page, "ANTRENMAN");
        _trainingLabel = BodyLabel("TrainingLabel", autowrap: true);
        trainingCard.AddChild(_trainingLabel);

        var trainingRow = ActionFlow();
        trainingCard.AddChild(trainingRow);

        _trainLowButton = SecondaryButton("Hafif");
        _trainLowButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.Low));
        trainingRow.AddChild(_trainLowButton);

        _trainMediumButton = SecondaryButton("Orta");
        _trainMediumButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.Medium));
        trainingRow.AddChild(_trainMediumButton);

        _trainHighButton = SecondaryButton("Yoğun");
        _trainHighButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.High));
        trainingRow.AddChild(_trainHighButton);

        var focusRow = ActionFlow();
        trainingCard.AddChild(focusRow);

        _focusGeneralButton = SecondaryButton("Odak: Dengeli");
        _focusGeneralButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.General));
        focusRow.AddChild(_focusGeneralButton);

        _focusFitnessButton = SecondaryButton("Kondisyon");
        _focusFitnessButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Fitness));
        focusRow.AddChild(_focusFitnessButton);

        _focusTacticalButton = SecondaryButton("Taktik");
        _focusTacticalButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Tactical));
        focusRow.AddChild(_focusTacticalButton);

        _focusRecoveryButton = SecondaryButton("Toparlanma");
        _focusRecoveryButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Recovery));
        focusRow.AddChild(_focusRecoveryButton);

        var restRow = ActionFlow();
        trainingCard.AddChild(restRow);

        _restLightButton = SecondaryButton("Az dinlenme");
        _restLightButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingRest(RestApproach.Light));
        restRow.AddChild(_restLightButton);

        _restNormalButton = SecondaryButton("Normal dinlenme");
        _restNormalButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingRest(RestApproach.Normal));
        restRow.AddChild(_restNormalButton);

        _restHeavyButton = SecondaryButton("Bol dinlenme");
        _restHeavyButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingRest(RestApproach.Heavy));
        restRow.AddChild(_restHeavyButton);

        foreach (var option in new[]
                 {
                     _trainLowButton, _trainMediumButton, _trainHighButton,
                     _focusGeneralButton, _focusFitnessButton, _focusTacticalButton, _focusRecoveryButton,
                     _restLightButton, _restNormalButton, _restHeavyButton,
                 })
        {
            option.ToggleMode = true;
        }

        var matchPriorityCard = AddCard(page, "MAÇA ÖZEL ANTRENMAN", emphasized: true);
        _matchTrainingPriorityLabel = BodyLabel("MatchTrainingPriorityLabel", autowrap: true);
        matchPriorityCard.AddChild(_matchTrainingPriorityLabel);
        var matchPriorityRow = ActionFlow();
        matchPriorityCard.AddChild(matchPriorityRow);
        foreach (var priority in Enum.GetValues<MatchTrainingPriority>())
        {
            var captured = priority;
            var button = SecondaryButton(priority switch
            {
                MatchTrainingPriority.Recovery => "Toparlanma",
                MatchTrainingPriority.MatchSharpness => "Maç Keskinliği",
                MatchTrainingPriority.PressResistance => "Baskıdan Çıkış",
                MatchTrainingPriority.DefensiveTransitions => "Geçiş Savunması",
                MatchTrainingPriority.AttackingPatterns => "Hücum Otomasyonları",
                _ => priority.ToString(),
            });
            button.Pressed += () => Apply(_controller.SelectMatchTrainingPriority(captured));
            matchPriorityRow.AddChild(button);
            _matchTrainingPriorityButtons[priority] = button;
        }

        var tacticsCard = AddCard(page, "MAÇ PLANI", emphasized: true);
        _tacticLabel = BodyLabel("TacticLabel", autowrap: true);
        tacticsCard.AddChild(_tacticLabel);

        var formationRow = ActionFlow();
        tacticsCard.AddChild(formationRow);

        _formation442Button = SecondaryButton("4-4-2");
        _formation442Button.Pressed += () => Apply(_controller.SetTacticFormation(Formation.F442));
        formationRow.AddChild(_formation442Button);

        _formation433Button = SecondaryButton("4-3-3");
        _formation433Button.Pressed += () => Apply(_controller.SetTacticFormation(Formation.F433));
        formationRow.AddChild(_formation433Button);

        _formation352Button = SecondaryButton("3-5-2");
        _formation352Button.Pressed += () => Apply(_controller.SetTacticFormation(Formation.F352));
        formationRow.AddChild(_formation352Button);

        var approachRow = ActionFlow();
        tacticsCard.AddChild(approachRow);

        _approachBalancedButton = SecondaryButton("Dengeli");
        _approachBalancedButton.Pressed += () => Apply(_controller.SetTacticApproach(TacticalApproach.Balanced));
        approachRow.AddChild(_approachBalancedButton);

        _approachAttackingButton = SecondaryButton("Hücum");
        _approachAttackingButton.Pressed += () => Apply(_controller.SetTacticApproach(TacticalApproach.Attacking));
        approachRow.AddChild(_approachAttackingButton);

        _approachDefensiveButton = SecondaryButton("Defans");
        _approachDefensiveButton.Pressed += () => Apply(_controller.SetTacticApproach(TacticalApproach.Defensive));
        approachRow.AddChild(_approachDefensiveButton);

        var pressingRow = ActionFlow();
        tacticsCard.AddChild(pressingRow);
        _pressingLowButton = SecondaryButton("Pres: Geri");
        _pressingLowButton.Pressed += () => Apply(_controller.SetTacticPressing(PressingIntensity.LowBlock));
        pressingRow.AddChild(_pressingLowButton);
        _pressingBalancedButton = SecondaryButton("Pres: Dengeli");
        _pressingBalancedButton.Pressed += () => Apply(_controller.SetTacticPressing(PressingIntensity.Balanced));
        pressingRow.AddChild(_pressingBalancedButton);
        _pressingHighButton = SecondaryButton("Pres: Önde");
        _pressingHighButton.Pressed += () => Apply(_controller.SetTacticPressing(PressingIntensity.HighPress));
        pressingRow.AddChild(_pressingHighButton);

        var lineRow = ActionFlow();
        tacticsCard.AddChild(lineRow);
        _lineDeepButton = SecondaryButton("Hat: Derin");
        _lineDeepButton.Pressed += () => Apply(_controller.SetTacticDefensiveLine(DefensiveLine.Deep));
        lineRow.AddChild(_lineDeepButton);
        _lineStandardButton = SecondaryButton("Hat: Standart");
        _lineStandardButton.Pressed += () => Apply(_controller.SetTacticDefensiveLine(DefensiveLine.Standard));
        lineRow.AddChild(_lineStandardButton);
        _lineHighButton = SecondaryButton("Hat: Yüksek");
        _lineHighButton.Pressed += () => Apply(_controller.SetTacticDefensiveLine(DefensiveLine.High));
        lineRow.AddChild(_lineHighButton);

        foreach (var option in new[]
                 {
                     _formation442Button, _formation433Button, _formation352Button,
                     _approachBalancedButton, _approachAttackingButton, _approachDefensiveButton,
                     _pressingLowButton, _pressingBalancedButton, _pressingHighButton,
                     _lineDeepButton, _lineStandardButton, _lineHighButton,
                 })
        {
            option.ToggleMode = true;
        }

        _dualPhaseTacticLabel = BodyLabel("DualPhaseTacticLabel", autowrap: true);
        tacticsCard.AddChild(_dualPhaseTacticLabel);
        var phaseRow = ActionFlow();
        tacticsCard.AddChild(phaseRow);
        AddDualPhasePreset(
            phaseRow,
            "Dengeli Geçiş",
            Formation.F442,
            Formation.F442,
            TacticalPhaseRole.Balanced,
            TacticalPhaseRole.Balanced);
        AddDualPhasePreset(
            phaseRow,
            "Kanat + Kompakt",
            Formation.F433,
            Formation.F442,
            TacticalPhaseRole.WideOverloads,
            TacticalPhaseRole.CompactBlock);
        AddDualPhasePreset(
            phaseRow,
            "Merkez + Pres",
            Formation.F352,
            Formation.F442,
            TacticalPhaseRole.CentralOverloads,
            TacticalPhaseRole.AggressivePress);
        AddDualPhasePreset(
            phaseRow,
            "Direkt + Blok",
            Formation.F442,
            Formation.F442,
            TacticalPhaseRole.DirectRunners,
            TacticalPhaseRole.CompactBlock);
        return page;
    }

    private void AddDualPhasePreset(
        Control row,
        string label,
        Formation inFormation,
        Formation outFormation,
        TacticalPhaseRole inRole,
        TacticalPhaseRole outRole)
    {
        var button = SecondaryButton(label);
        button.Pressed += () => Apply(_controller.SetDualPhaseTactic(
            inFormation,
            outFormation,
            inRole,
            outRole));
        row.AddChild(button);
        _dualPhaseTacticButtons.Add(button);
    }

    private Control BuildWorldPage()
    {
        var page = PageRoot();
        var leagueCard = AddCard(page, "LİG NABZI", emphasized: true);
        _leagueBriefingLabel = BodyLabel("LeagueBriefingLabel", autowrap: true);
        leagueCard.AddChild(_leagueBriefingLabel);

        var statisticsCard = AddCard(page, "İSTATİSTİK MERKEZİ");
        _leagueStatisticsLabel = BodyLabel("LeagueStatisticsLabel", autowrap: true);
        statisticsCard.AddChild(_leagueStatisticsLabel);
        _managedLeagueStatisticsLabel = BodyLabel("ManagedLeagueStatisticsLabel", autowrap: true);
        statisticsCard.AddChild(_managedLeagueStatisticsLabel);

        var tableCard = AddCard(page, "PUAN DURUMU");
        _standingsTable = new Tree
        {
            Name = "StandingsTable",
            Columns = 11,
            ColumnTitlesVisible = true,
            HideRoot = true,
            CustomMinimumSize = new Vector2(0, 500),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        ConfigureStandingsColumns(_standingsTable);
        CareerUiTheme.StyleTable(_standingsTable);
        tableCard.AddChild(_standingsTable);

        var fixtureCard = AddCard(page, "HAFTA FİKSTÜRÜ");
        var roundRow = ActionFlow();
        fixtureCard.AddChild(roundRow);
        var weekLabel = new Label { Text = "Hafta" };
        CareerUiTheme.StyleBody(weekLabel, muted: true);
        roundRow.AddChild(weekLabel);
        _roundSelector = new SpinBox
        {
            MinValue = 1,
            MaxValue = CompetitionMvpConstraints.MaxLeagueFixtureRound,
            Value = 1,
        };
        _roundSelector.ValueChanged += _ => RefreshFixtureList();
        roundRow.AddChild(_roundSelector);

        _fixtureList = new ItemList
        {
            Name = "FixtureList",
            CustomMinimumSize = new Vector2(0, 220),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_fixtureList);
        fixtureCard.AddChild(_fixtureList);

        var advancedToggle = SecondaryButton("Gelişmiş sezon araçları");
        advancedToggle.Visible = false; // Manuel lifecycle tetikleri oyuncu yüzeyine ait değil.
        fixtureCard.AddChild(advancedToggle);

        var advanced = new VBoxContainer { Visible = false };
        advanced.AddThemeConstantOverride("separation", 8);
        fixtureCard.AddChild(advanced);
        advancedToggle.Pressed += () =>
        {
            advanced.Visible = !advanced.Visible;
            advancedToggle.Text = advanced.Visible
                ? "Gelişmiş sezon araçlarını gizle"
                : "Gelişmiş sezon araçları";
        };

        var seasonRow = ActionFlow();
        advanced.AddChild(seasonRow);
        AddActionButton(seasonRow, "Ligi Kur / Tamamla", () => Apply(_controller.EnsureLeagueReady()));
        AddActionButton(seasonRow, "Sezonu Kapat", () => Apply(_controller.CompleteSeason()));
        AddActionButton(seasonRow, "Sezonu Arşivle", () => Apply(_controller.ArchiveSeason()));
        AddActionButton(seasonRow, "Yeni Sezon", () => Apply(_controller.StartNewSeason()));

        var planningRow = ActionFlow();
        advanced.AddChild(planningRow);
        AddActionButton(planningRow, "Planlama Dönemi Aç", () => Apply(_controller.OpenPlanningPeriod()));
        AddActionButton(planningRow, "Planlama Dönemini Bitir", () => Apply(_controller.CompletePlanningPeriod()));
        return page;
    }

    private Control BuildFilePage()
    {
        var page = PageRoot();
        var economyCard = AddCard(page, "KULÜP EKONOMİSİ & YÖNETİM HEDEFLERİ", emphasized: true);
        _clubEconomyLabel = BodyLabel("ClubEconomyLabel", autowrap: true);
        economyCard.AddChild(_clubEconomyLabel);

        var legacyCard = AddCard(page, "TEKNİK DİREKTÖR KARİYERİ", emphasized: true);
        _careerLegacyHeadlineLabel = BodyLabel("CareerLegacyHeadlineLabel", autowrap: true);
        legacyCard.AddChild(_careerLegacyHeadlineLabel);
        _careerLegacyRecordLabel = BodyLabel("CareerLegacyRecordLabel", autowrap: true);
        legacyCard.AddChild(_careerLegacyRecordLabel);
        _careerLegacyDevelopmentLabel = BodyLabel("CareerLegacyDevelopmentLabel", autowrap: true);
        legacyCard.AddChild(_careerLegacyDevelopmentLabel);
        _careerSeasonList = new ItemList
        {
            Name = "CareerSeasonList",
            CustomMinimumSize = new Vector2(0, 260),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        CareerUiTheme.StyleList(_careerSeasonList);
        legacyCard.AddChild(_careerSeasonList);

        var experienceCard = AddCard(page, "OYUN DENEYİMİ & CİHAZ", emphasized: true);
        _deviceAcceptanceLabel = BodyLabel("DeviceAcceptanceLabel", autowrap: true);
        experienceCard.AddChild(_deviceAcceptanceLabel);
        _runtimeTelemetryLabel = BodyLabel("RuntimeTelemetryLabel", autowrap: true);
        experienceCard.AddChild(_runtimeTelemetryLabel);
        _experienceSettingsLabel = BodyLabel("ExperienceSettingsLabel", muted: true, autowrap: true);
        experienceCard.AddChild(_experienceSettingsLabel);

        var soundRow = ActionFlow();
        experienceCard.AddChild(soundRow);
        _soundSettingButton = SecondaryButton("Ses");
        _soundSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { SoundEnabled = !current.SoundEnabled });
        soundRow.AddChild(_soundSettingButton);
        _musicSettingButton = SecondaryButton("Müzik");
        _musicSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { MusicEnabled = !current.MusicEnabled });
        soundRow.AddChild(_musicSettingButton);
        _crowdSettingButton = SecondaryButton("Tribün");
        _crowdSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { CrowdEnabled = !current.CrowdEnabled });
        soundRow.AddChild(_crowdSettingButton);
        _hapticsSettingButton = SecondaryButton("Titreşim");
        _hapticsSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current.CycleHapticsStrength());
        soundRow.AddChild(_hapticsSettingButton);

        var deviceTestRow = ActionFlow();
        experienceCard.AddChild(deviceTestRow);
        var hapticTestButton = SecondaryButton("Titreşimi Dene");
        hapticTestButton.Pressed += TestHapticFeedback;
        deviceTestRow.AddChild(hapticTestButton);
        var resetTelemetryButton = SecondaryButton("Ölçümü Sıfırla");
        resetTelemetryButton.Pressed += ResetRuntimeTelemetry;
        deviceTestRow.AddChild(resetTelemetryButton);

        var accessibilityRow = ActionFlow();
        experienceCard.AddChild(accessibilityRow);
        _textScaleSettingButton = SecondaryButton("Yazı %100");
        _textScaleSettingButton.Pressed += () => UpdateExperienceSettings(current => current.CycleTextScale());
        accessibilityRow.AddChild(_textScaleSettingButton);
        _contrastSettingButton = SecondaryButton("Yüksek kontrast");
        _contrastSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { HighContrast = !current.HighContrast });
        accessibilityRow.AddChild(_contrastSettingButton);
        _motionSettingButton = SecondaryButton("Hareketi azalt");
        _motionSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { ReducedMotion = !current.ReducedMotion });
        accessibilityRow.AddChild(_motionSettingButton);
        _guideSettingButton = SecondaryButton("İlk hafta rehberi");
        _guideSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with { FirstWeekGuideEnabled = !current.FirstWeekGuideEnabled });
        accessibilityRow.AddChild(_guideSettingButton);
        _gamepadSettingButton = SecondaryButton("Gamepad ipuçları");
        _gamepadSettingButton.Pressed += () => UpdateExperienceSettings(
            current => current with
            {
                GamepadNavigationHintsEnabled = !current.GamepadNavigationHintsEnabled,
            });
        accessibilityRow.AddChild(_gamepadSettingButton);

        // Ayar değişiminde Hub yeniden kurulduğunda aynı Dosya sayfasının üstünde
        // kalınır; oyuncu peş peşe tercih değiştirmek için yeniden kaydırmaz.
        page.MoveChild(experienceCard.GetParent(), 0);

        var saveCard = AddCard(page, "KARİYER DOSYASI", emphasized: true);
        _saveDeskLabel = BodyLabel("SaveDeskLabel", autowrap: true);
        saveCard.AddChild(_saveDeskLabel);

        var saveLoadRow = ActionFlow();
        saveCard.AddChild(saveLoadRow);

        _saveGameButton = PrimaryButton("Kaydet");
        _saveGameButton.Pressed += () => Apply(_controller.SaveGame());
        saveLoadRow.AddChild(_saveGameButton);

        _loadGameButton = SecondaryButton("Yükle");
        _loadGameButton.Pressed += OnLoadGamePressed;
        saveLoadRow.AddChild(_loadGameButton);

        var menuButton = SecondaryButton("Ana Menü");
        menuButton.Pressed += () => BackToMenuRequested?.Invoke();
        saveLoadRow.AddChild(menuButton);
        return page;
    }

    private static Label SectionTitle(string text)
    {
        var label = new Label { Text = text };
        CareerUiTheme.StyleSection(label);
        return label;
    }

    private static Label BodyLabel(string name, bool muted = false, bool autowrap = false)
    {
        var label = new Label
        {
            Name = name,
            AutowrapMode = autowrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
        };
        CareerUiTheme.StyleBody(label, muted);
        return label;
    }

    private static TextureRect AddKitPreview(Container parent, string labelText, string tooltip)
    {
        var column = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(94, 150),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        column.AddThemeConstantOverride("separation", 4);
        parent.AddChild(column);

        var texture = new TextureRect
        {
            CustomMinimumSize = new Vector2(90, 124),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = tooltip,
            Visible = false,
        };
        column.AddChild(texture);

        var label = BodyLabel($"{labelText}KitLabel", muted: true);
        label.Text = labelText;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        column.AddChild(label);
        return texture;
    }

    private static void ConfigureStandingsColumns(Tree table)
    {
        var titles = new[] { "#", "TAKIM", "O", "G", "B", "M", "A", "Y", "AV", "FORM", "P" };
        for (var column = 0; column < titles.Length; column++)
        {
            table.SetColumnTitle(column, titles[column]);
            table.SetColumnExpand(column, column == 1);
            table.SetColumnCustomMinimumWidth(
                column,
                column == 1 ? 160 : column == 9 ? 68 : column == 0 ? 36 : 33);
        }
    }

    private void ShowEmptyStandings(string message)
    {
        _standingsTable.Clear();
        var root = _standingsTable.CreateItem();
        var row = _standingsTable.CreateItem(root);
        row.SetText(1, message);
        row.SetCustomColor(1, CareerUiTheme.InkMuted);
    }

    private static Button PrimaryButton(string text)
    {
        var button = new Button { Text = text };
        CareerUiTheme.StylePrimaryButton(button);
        return button;
    }

    private static Button SecondaryButton(string text)
    {
        var button = new Button { Text = text };
        CareerUiTheme.StyleSecondaryButton(button);
        return button;
    }

    private static void AddActionButton(Container row, string text, Action action)
    {
        var button = SecondaryButton(text);
        button.Pressed += action;
        row.AddChild(button);
    }

    private void PulseStatus(string message, bool succeeded = true)
    {
        _statusPanel.Visible = true;
        _statusLabel.Text = ToPlayerFacingText(message);
        var signal = succeeded ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft;
        _statusLabel.AddThemeColorOverride("font_color", signal);
        _statusPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatusPanel(signal));
        if (CareerUiTheme.ReducedMotion)
        {
            _statusLabel.Modulate = Colors.White;
            return;
        }

        _statusLabel.Modulate = new Color(1f, 1f, 1f, 0.35f);
        var tween = CreateTween();
        tween.TweenProperty(_statusLabel, "modulate:a", 1f, 0.28f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private static string ToPlayerFacingText(string text) =>
        Regex.Replace(InternalIdentifierPattern.Replace(text, string.Empty), @"[ ]{2,}", " ").Trim();

    private static string CompactOfficeText(
        Application.Competition.Queries.PostMatchOfficeDigest digest) =>
        CompactSummary(digest.Headline, digest.AdviceLine);

    private static string CompactOfficeText(
        Application.CareerHub.Queries.CareerResumeDigest digest) =>
        CompactSummary(digest.Headline, digest.AdviceLine);

    private static string CompactSummary(string headline, string supportingLine) =>
        CompactText(
            string.IsNullOrWhiteSpace(supportingLine)
                ? headline
                : $"{headline}\n{supportingLine}",
            maxLines: 2,
            maxCharacters: 220);

    private static string CompactText(string text, int maxLines, int maxCharacters = 180)
    {
        var compact = string.Join(
            "\n",
            text.Replace("\r", string.Empty, StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(maxLines));
        return compact.Length <= maxCharacters
            ? compact
            : compact[..(maxCharacters - 3)].TrimEnd() + "...";
    }

    private void OnPlayMatches()
    {
        Callable.From(() => MatchDayRequested?.Invoke()).CallDeferred();
    }

    private void Apply(UiActionResult result)
    {
        PulseStatus(result.Message, result.Succeeded);
        var haptics = GameExperienceSettingsStore.Current.EffectiveHapticsStrengthPercent;
        if (OS.HasFeature("mobile") && haptics > 0)
        {
            var baseDuration = result.Succeeded ? 18 : 34;
            Input.VibrateHandheld(Math.Max(1, baseDuration * haptics / 100));
        }
        RefreshUi();
        // Toparlanma onayı gibi ofis köprüleri nabız metninin üstüne yazılır.
        if (!string.IsNullOrWhiteSpace(result.NarrativeBridgeLine))
        {
            _officeLabel.Text = CompactText(result.NarrativeBridgeLine, 2);
        }

        if (!string.IsNullOrWhiteSpace(result.NextFocusCode))
        {
            BindOfficeNextStepForFocus(result.NextFocusCode);
        }
    }

    private void ApplySelectedPlayer(Func<long, UiActionResult> action)
    {
        if (_selectedPlayerId is not long playerId)
        {
            Apply(UiActionResult.Fail("Önce kadrodan bir futbolcu seç."));
            return;
        }

        Apply(action(playerId));
    }

    private void OnSquadPlayerSelected(long index)
    {
        if (index < 0 || index >= _playerManagementPlayers.Count)
        {
            return;
        }

        _selectedPlayerId = _playerManagementPlayers[(int)index].PlayerId;
        RefreshPlayerDetail();
    }

    private void OnAcademyCandidateSelected(long index)
    {
        if (index < 0 || index >= _academyCandidates.Count)
        {
            return;
        }

        _selectedAcademyCandidateId = _academyCandidates[(int)index].PlayerId;
        UpdateAcademyDecisionButtons();
    }

    private void ApplySelectedAcademyCandidate(bool accept)
    {
        if (_selectedAcademyCandidateId is not long playerId)
        {
            Apply(UiActionResult.Fail("Önce bir altyapı adayı seç."));
            return;
        }

        Apply(accept
            ? _controller.AcceptYouthAcademyCandidate(playerId)
            : _controller.RejectYouthAcademyCandidate(playerId));
    }

    private void OnAcademyLifecyclePlayerSelected(long index)
    {
        if (index < 0 || index >= _academyLifecyclePlayers.Count)
        {
            return;
        }

        _selectedAcademyLifecyclePlayerId = _academyLifecyclePlayers[(int)index].PlayerId;
        UpdateAcademyPromotionButton();
    }

    private void PromoteSelectedAcademyPlayer()
    {
        if (_selectedAcademyLifecyclePlayerId is not long playerId)
        {
            Apply(UiActionResult.Fail("Önce gelişim merkezinden bir oyuncu seç."));
            return;
        }

        Apply(_controller.PromoteYouthAcademyCandidate(playerId));
    }

    private void OnSquadPlayerClicked(long index, Vector2 atPosition, long mouseButtonIndex)
    {
        _ = atPosition;
        if (mouseButtonIndex != (long)MouseButton.Left
            || index < 0
            || index >= _playerManagementPlayers.Count)
        {
            return;
        }

        OpenPlayerDossier(_playerManagementPlayers[(int)index].PlayerId);
    }

    private void RefreshClubPitch(IReadOnlyList<PlayerManagementLine> players)
    {
        MatchScreenUi.ClearChildren(_clubPitchHost);
        var startingXi = players
            .Take(Domain.TeamPreparation.MatchSelection.StartingXiSize)
            .Select(player => new Application.TeamPreparation.Queries.SquadSelectionPlayerDigest(
                player.SlotIndex,
                player.DisplayName,
                player.PositionCode,
                player.Rating,
                player.Fitness,
                player.Fatigue,
                !player.Availability.StartsWith("Sakat", StringComparison.OrdinalIgnoreCase),
                true,
                player.PositionName))
            .ToArray();
        if (startingXi.Length != Domain.TeamPreparation.MatchSelection.StartingXiSize)
        {
            var empty = BodyLabel("ClubPitchEmpty", muted: true, autowrap: true);
            empty.Text = "Saha yerleşimi için 11 futbolcu bekleniyor.";
            _clubPitchHost.AddChild(empty);
            return;
        }

        _clubPitchHost.AddChild(TacticalPitchBoardUi.BuildReadOnly(startingXi, OnPitchPlayerSelected));
    }

    private void OnPitchPlayerSelected(Application.TeamPreparation.Queries.SquadSelectionPlayerDigest digest)
    {
        var player = _playerManagementPlayers.FirstOrDefault(candidate => candidate.SlotIndex == digest.SlotIndex);
        if (player is not null)
        {
            OpenPlayerDossier(player.PlayerId);
        }
    }

    private void OnScoutCandidateSelected(long index)
    {
        if (index < 0 || index >= _scoutCandidates.Count)
        {
            return;
        }

        _selectedScoutPlayerId = _scoutCandidates[(int)index].PlayerId;
        _suggestTargetButton.Disabled = _scoutCandidates[(int)index].IsListedTarget;
        _suggestTargetButton.Visible = !_suggestTargetButton.Disabled;
    }

    private void AddSelectedScoutCandidate()
    {
        if (_selectedScoutPlayerId is not long playerId)
        {
            Apply(UiActionResult.Fail("Önce scout listesinden bir futbolcu seç."));
            return;
        }

        Apply(_controller.AddScoutCandidateToShortlist(playerId));
    }

    private void RefreshPlayerDetail()
    {
        var selected = _selectedPlayerId is long playerId
            ? _playerManagementPlayers.FirstOrDefault(player => player.PlayerId == playerId)
            : null;
        _playerDetailLabel.Text = selected is null
            ? "Oyuncu dosyası için isme dokun."
            : $"{selected.DisplayName} seçili — dosya için tekrar dokun.";

        var disabled = selected is null;
        _promiseStartButton.Disabled = disabled;
        _promisePlayingTimeButton.Disabled = disabled;
        _promiseStartButton.Visible = !disabled;
        _promisePlayingTimeButton.Visible = !disabled;
        _openDecisionButton.Disabled = disabled;
        _openStartingDecisionButton.Disabled = disabled;
        _openTransferDecisionButton.Disabled = disabled;
        _openDisciplineDecisionButton.Disabled = disabled;
    }

    private void BindOfficeNextStepForFocus(string focusCode)
    {
        BindOfficeNextStep(_controller.BuildOfficeNextStep(focusCode));
    }

    private void OnLoadGamePressed()
    {
        var result = _controller.LoadGame();
        if (result.Succeeded && _controller.LastCareerResume is { } resume)
        {
            ApplyCareerResume(resume);
            return;
        }

        Apply(result);
    }

    private void RefreshUi()
    {
        var host = _controller.Host;
        var world = host.WorldModule;
        var competition = host.CompetitionModule;
        var current = world.Queries.GetCurrentGameDate();
        var season = competition.Queries.GetCurrentSeason();
        var manager = host.ManagerModule.Queries.GetCareer();
        var period = world.Queries.GetCurrentPlanningPeriod();

        _dateLabel.Text = $"{current.Day:D2}.{current.Month:D2}.{current.Year}";
        RefreshClubBranding();

        if (string.Equals(manager.EmploymentStatus, "Unemployed", StringComparison.Ordinal))
        {
            var lastClub = manager.LastClubId is long lastClubId
                ? _controller.GetClubDisplayName(lastClubId)
                : "—";
            var offerText = manager.PendingOfferClubId is long offerClubId
                ? $"Aktif teklif: {GetClubDisplayNameSafe(offerClubId)} (#{manager.PendingOfferId})"
                : "Aktif teklif yok · Kadro ekranından iş ara";
            _managerLabel.Text = "SERBEST MENAJER";
            _seasonLabel.Text = $"{manager.DisplayName} · Son kulüp: {lastClub} · İtibar {manager.ManagerReputation}";
            _progressLabel.Text = offerText;
        }
        else
        {
            var clubName = manager.EmployedClubId is long clubId
                ? _controller.GetClubDisplayName(clubId)
                : "—";
            var boardText = manager.BoardConfidence is int confidence
                ? $"Yönetim {confidence} · {TranslateRisk(manager.EmploymentRiskBand)} · {TranslateExpectation(manager.SeasonExpectation)}"
                : string.Empty;
            _managerLabel.Text = clubName.ToUpperInvariant();
            _seasonLabel.Text = $"{manager.DisplayName} · {boardText} · İtibar {manager.ManagerReputation}";
        }

        var periodText = period is null
            ? "Planlama dönemi: yok"
            : $"Planlama dönemi: #{period.PlanningPeriodId} ({period.Status})";

        if (season is null)
        {
            _progressLabel.Text = $"Lig sezonu yok · {periodText}";
            _leagueBriefingLabel.Text = Application.Competition.Queries.LeagueWorldBriefing.NoSeason().ToDisplayText();
            ShowEmptyStandings("Henüz aktif lig sezonu yok");
            _fixtureList.Clear();
            _blockerLabel.Text = _controller.FormatActiveBlockerSummary();
            RefreshTodayPulse();
            RefreshFirstWeekGuide();
            RefreshSelectionStatus();
            RefreshTrainingStatus();
            RefreshDevelopmentStatus();
            RefreshContractStatus();
            RefreshDressingRoomEcho();
            RefreshMemoryStatus();
            RefreshPromiseStatus();
            RefreshRelationshipStatus();
            RefreshDecisionStatus();
            RefreshTransferWindowStatus();
            RefreshTransferDesk();
            RefreshTransferBudgetStatus();
            RefreshTransferNeedStatus();
            RefreshShortlistTargetStatus();
            RefreshTransferProcessStatus();
            RefreshClubOfferStatus();
            RefreshContractProposalStatus();
            RefreshTacticStatus();
            RefreshSquadList();
            RefreshSaveDesk();
            UpdatePrimaryHints(dueMatchCount: 0, canAdvance: world.Queries.GetTimeAdvanceEligibility().CanAdvance);
            UpdateJobOfferButtons(manager);
            UpdateTransferNeedButtons(manager);
            UpdateTrainingButtons(manager);
            UpdateTacticButtons(manager);
            SanitizeScreenText(this);
            return;
        }

        var progress = competition.Queries.GetSeasonProgress(season.SeasonId);
        var progressText = progress is null
            ? "İlerleme: —"
            : $"İlerleme: {progress.AcceptedFixtureCount}/{progress.TotalFixtureCount} maç"
              + (progress.CanComplete ? " · sezon geçişine hazır" : string.Empty)
              + (progress.CanArchive ? " · arşiv + yeni sezon hazır" : string.Empty);
        _progressLabel.Text =
            $"Sezon #{season.SeasonId} · {progressText} · {periodText}";

        _blockerLabel.Text = _controller.FormatActiveBlockerSummary();
        RefreshTodayPulse();
        RefreshFirstWeekGuide();
        RefreshSelectionStatus();
        RefreshTrainingStatus();
        RefreshDevelopmentStatus();
        RefreshContractStatus();
        RefreshDressingRoomEcho();
        RefreshMemoryStatus();
        RefreshPromiseStatus();
        RefreshRelationshipStatus();
        RefreshDecisionStatus();
        RefreshTransferWindowStatus();
        RefreshTransferDesk();
        RefreshTransferBudgetStatus();
        RefreshTransferNeedStatus();
        RefreshShortlistTargetStatus();
        RefreshTransferProcessStatus();
        RefreshClubOfferStatus();
        RefreshContractProposalStatus();
        RefreshTacticStatus();
        RefreshStandings();
        RefreshFixtureList();
        RefreshSquadList();
        RefreshSaveDesk();

        var dueCount = competition.Queries
            .GetSeasonFixtures(season.SeasonId)
            .Count(fixture =>
                fixture.ScheduledDayNumber <= current.DayNumber
                && string.Equals(fixture.Status, nameof(FixtureStatus.Planned), StringComparison.Ordinal));

        UpdatePrimaryHints(dueCount, world.Queries.GetTimeAdvanceEligibility().CanAdvance);
        UpdateJobOfferButtons(manager);
        UpdateTransferNeedButtons(manager);
        UpdateTrainingButtons(manager);
        UpdateTacticButtons(manager);
        UpdateSaveDeskButtons();
        SanitizeScreenText(this);
    }

    private static void SanitizeScreenText(Node node)
    {
        if (node is Label label)
        {
            label.Text = ToPlayerFacingText(label.Text);
        }
        else if (node is Button button)
        {
            button.Text = ToPlayerFacingText(button.Text);
        }
        else if (node is ItemList list)
        {
            for (var index = 0; index < list.ItemCount; index++)
            {
                list.SetItemText(index, ToPlayerFacingText(list.GetItemText(index)));
            }
        }

        foreach (var child in node.GetChildren())
        {
            SanitizeScreenText(child);
        }
    }

    private void RefreshClubBranding()
    {
        var manager = _controller.Host.ManagerModule.Queries.GetCareer();
        var club = manager.EmployedClubId is long clubId
            ? _controller.Host.ClubModule.Queries.GetClub(clubId)
            : null;

        if (club is null)
        {
            SetClubTexture(_clubCrest, null);
            SetClubTexture(_homeKit, null);
            SetClubTexture(_awayKit, null);
            SetClubTexture(_thirdKit, null);
            return;
        }

        SetClubTexture(_clubCrest, club.CrestResourcePath);
        SetClubTexture(_homeKit, club.HomeKitResourcePath);
        SetClubTexture(_awayKit, club.AwayKitResourcePath);
        SetClubTexture(_thirdKit, club.ThirdKitResourcePath);
    }

    private static void SetClubTexture(TextureRect target, string? resourcePath)
    {
        target.Texture = !string.IsNullOrWhiteSpace(resourcePath) && ResourceLoader.Exists(resourcePath)
            ? GD.Load<Texture2D>(resourcePath)
            : null;
        target.Visible = target.Texture is not null;
    }

    private void RefreshSaveDesk()
    {
        RefreshExperienceSettings();
        RefreshClubEconomy();
        var legacy = _controller.BuildCareerLegacyDigest();
        _careerLegacyHeadlineLabel.Text = legacy.Headline;
        _careerLegacyRecordLabel.Text = $"{legacy.RecordLine}\n{legacy.NextMilestoneLine}";
        _careerLegacyDevelopmentLabel.Text = legacy.DevelopmentLine;
        _careerSeasonList.Clear();
        foreach (var employment in legacy.Employments)
        {
            _careerSeasonList.AddItem(employment.ToDisplayText());
        }

        foreach (var season in legacy.Seasons)
        {
            _careerSeasonList.AddItem(season.ToDisplayText());
        }

        var desk = _controller.BuildSaveDeskDigest();
        _saveDeskLabel.Text = desk.ToDisplayText();
        UpdateSaveDeskButtons();
    }

    private void RefreshClubEconomy()
    {
        var economy = _controller.BuildClubEconomy();
        if (economy is null)
        {
            _clubEconomyLabel.Text = "Kulüp ekonomisi: aktif görev bulunmuyor.";
            return;
        }

        var balanceTone = economy.ProjectedOperatingBalance >= 0 ? "fazla" : "açık";
        var realized = _controller.BuildRealizedClubFinance();
        var realizedLine = realized is null
            ? "Gerçekleşen muhasebe henüz başlamadı."
            : $"Gerçekleşen: gelir {realized.Revenue:N0} · gider {realized.Expenses:N0} "
              + $"· bakiye {realized.Balance:N0} {realized.CurrencyCode} "
              + $"· yönetim {FormatBoardFinancialStatus(realized.BoardOutcome.Status)}\n"
              + realized.BoardOutcome.Summary;
        var objectives = string.Join(
            "\n",
            economy.BoardObjectives.Select(objective =>
                $"• {objective.Title}: {objective.Current} / hedef {objective.Target} "
                + $"· %{objective.ProgressPercent} · {FormatBoardObjectiveStatus(objective.Status)}"));
        _clubEconomyLabel.Text =
            $"{economy.ClubName} · sezon projeksiyonu\n"
            + $"Maaş: {economy.CommittedWeeklyWage:N0} / {economy.WeeklyWageLimit:N0} {economy.CurrencyCode} "
            + $"· kullanım %{economy.WageUtilizationPercent} · boşluk {economy.WeeklyWageHeadroom:N0}\n"
            + $"Stadyum: {economy.ProjectedAverageAttendance:N0}/{economy.StadiumCapacity:N0} "
            + $"· doluluk %{economy.AttendancePercent} · bilet {economy.AverageTicketPrice:N0}\n"
            + $"Gelir: {economy.ProjectedOperatingRevenue:N0} · gider {economy.ProjectedOperatingCosts:N0} "
            + $"· {balanceTone} {Math.Abs(economy.ProjectedOperatingBalance):N0} {economy.CurrencyCode}\n"
            + realizedLine + "\n"
            + objectives;
    }

    private static string FormatBoardObjectiveStatus(
        Application.ClubGovernance.Queries.BoardObjectiveStatus status) => status switch
        {
            Application.ClubGovernance.Queries.BoardObjectiveStatus.NotStarted => "Başlamadı",
            Application.ClubGovernance.Queries.BoardObjectiveStatus.OnTrack => "Yolunda",
            Application.ClubGovernance.Queries.BoardObjectiveStatus.AtRisk => "Riskte",
            Application.ClubGovernance.Queries.BoardObjectiveStatus.OffTrack => "Geride",
            Application.ClubGovernance.Queries.BoardObjectiveStatus.Achieved => "Tamamlandı",
            Application.ClubGovernance.Queries.BoardObjectiveStatus.Failed => "Başarısız",
            _ => status.ToString(),
        };

    private static string FormatBoardFinancialStatus(
        Application.ClubGovernance.Services.BoardFinancialStatus status) => status switch
        {
            Application.ClubGovernance.Services.BoardFinancialStatus.Healthy => "Sağlıklı",
            Application.ClubGovernance.Services.BoardFinancialStatus.Watch => "Takipte",
            Application.ClubGovernance.Services.BoardFinancialStatus.Critical => "Kritik",
            _ => status.ToString(),
        };

    private void RefreshExperienceSettings()
    {
        var preferences = GameExperienceSettingsStore.Current;
        var safe = DisplaySafeAreaInsets.Resolve(Size);
        var profile = MobileUiLayoutProfile.Resolve(
            Mathf.RoundToInt(Size.X),
            Mathf.RoundToInt(Size.Y),
            safe.Left,
            safe.Top,
            safe.Right,
            safe.Bottom);
        var acceptance = MobileDeviceAcceptanceProfile.Evaluate(
            Mathf.RoundToInt(Size.X),
            Mathf.RoundToInt(Size.Y),
            safe.Left,
            safe.Top,
            safe.Right,
            safe.Bottom,
            profile.TouchTargetHeight,
            preferences.ScaleFont(15),
            touchInputAvailable: OS.HasFeature("mobile"));

        _deviceAcceptanceLabel.Text = acceptance.ToDisplayText();
        var telemetry = MobileRuntimeTelemetryMonitor.Active?.Snapshot();
        _runtimeTelemetryLabel.Text = telemetry is null
            ? "Canlı cihaz ölçümü henüz başlamadı."
            : $"{telemetry.Headline}\n{telemetry.DetailLine}";
        _experienceSettingsLabel.Text =
            "Gamepad: yön tuşlarıyla odak, A ile seçim, L1/R1 ile sayfa değişimi. "
            + "Otomatik eşikler gerçek cihaz sürükleme, titreşim, ses dengesi ve ısınma testinin yerine geçmez.";
        _soundSettingButton.Text = $"Ses: {OnOff(preferences.SoundEnabled)}";
        _musicSettingButton.Text = $"Müzik: {OnOff(preferences.MusicEnabled)}";
        _crowdSettingButton.Text = $"Tribün: {OnOff(preferences.CrowdEnabled)}";
        _hapticsSettingButton.Text = $"Titreşim: {HapticsLabel(preferences)}";
        _motionSettingButton.Text = $"Hareketi azalt: {OnOff(preferences.ReducedMotion)}";
        _contrastSettingButton.Text = $"Yüksek kontrast: {OnOff(preferences.HighContrast)}";
        _textScaleSettingButton.Text = $"Yazı: %{preferences.TextScalePercent}";
        _guideSettingButton.Text = $"İlk hafta rehberi: {OnOff(preferences.FirstWeekGuideEnabled)}";
        _gamepadSettingButton.Text =
            $"Gamepad ipuçları: {OnOff(preferences.GamepadNavigationHintsEnabled)}";
    }

    private void UpdateExperienceSettings(
        Func<GameExperiencePreferences, GameExperiencePreferences> update)
    {
        var before = GameExperienceSettingsStore.Current;
        var next = GameExperienceSettingsStore.Update(update);
        CareerUiTheme.Configure(next);
        if (next == before)
        {
            RefreshExperienceSettings();
            PulseStatus("Ayar cihaz depolamasına yazılamadı; önceki tercih korunuyor.");
            return;
        }

        ExperienceSettingsChanged?.Invoke();
    }

    private static string OnOff(bool enabled) => enabled ? "Açık" : "Kapalı";

    private static string HapticsLabel(GameExperiencePreferences preferences) =>
        preferences.EffectiveHapticsStrengthPercent switch
        {
            0 => "Kapalı",
            50 => "Düşük",
            _ => "Yüksek",
        };

    private void TestHapticFeedback()
    {
        var strength = GameExperienceSettingsStore.Current.EffectiveHapticsStrengthPercent;
        if (!OS.HasFeature("mobile"))
        {
            PulseStatus("Titreşim testi yalnız fiziksel mobil cihazda çalışır.");
            return;
        }

        if (strength == 0)
        {
            PulseStatus("Titreşim kapalı; önce Düşük veya Yüksek seviyeyi seç.");
            return;
        }

        Input.VibrateHandheld(strength == 50 ? 25 : 50);
        PulseStatus($"{HapticsLabel(GameExperienceSettingsStore.Current)} titreşim darbesi gönderildi.");
    }

    private void ResetRuntimeTelemetry()
    {
        MobileRuntimeTelemetryMonitor.Active?.ResetMeasurement();
        RefreshExperienceSettings();
        PulseStatus("Cihaz frame ölçümü sıfırlandı; 30 saniyelik yeni örnek başladı.");
    }

    private FirstWeekGuideDigest BuildFirstWeekGuide()
    {
        var preferences = GameExperienceSettingsStore.Current;
        var timeline = _controller.Host.WorldModule.TimelineStore.Timeline;
        var startedAt = _controller.Host.ManagerModule.Store.Career.ActiveEmployment?.StartedAt.DayNumber
            ?? timeline.CurrentDate.DayNumber;
        var daysSinceStart = Math.Max(0, timeline.CurrentDate.DayNumber - startedAt);
        return FirstWeekGuideDigest.Compose(
            preferences.FirstWeekGuideEnabled,
            preferences.FirstWeekGuideStep,
            daysSinceStart);
    }

    private void RefreshFirstWeekGuide()
    {
        var digest = BuildFirstWeekGuide();
        _firstWeekGuideCard.Visible = digest.IsVisible;
        if (!digest.IsVisible || digest.CurrentStep is null)
        {
            return;
        }

        var step = digest.CurrentStep;
        _firstWeekGuideLabel.Text =
            $"Adım {digest.StepNumber}/{digest.TotalStepCount}\n{step.Title}\n{step.Body}";
        _firstWeekGuideOpenButton.Text = step.ButtonLabel;
        _firstWeekGuideNextButton.Text = digest.StepNumber == digest.TotalStepCount
            ? "Rehberi Bitir"
            : "Tamamlandı →";
    }

    private void OpenCurrentGuideStep()
    {
        var step = BuildFirstWeekGuide().CurrentStep;
        if (step is null)
        {
            return;
        }

        ShowPage(step.TargetPageCode switch
        {
            FirstWeekGuideDigest.PageClub => HubPage.Club,
            FirstWeekGuideDigest.PageTransfer => HubPage.Transfer,
            FirstWeekGuideDigest.PagePrep => HubPage.Prep,
            FirstWeekGuideDigest.PageWorld => HubPage.World,
            _ => HubPage.Today,
        });
    }

    private void CompleteCurrentGuideStep()
    {
        GameExperienceSettingsStore.Update(current => current with
        {
            FirstWeekGuideStep = Math.Min(
                FirstWeekGuideDigest.TotalSteps,
                current.FirstWeekGuideStep + 1),
        });
        RefreshFirstWeekGuide();
        PulseStatus(
            GameExperienceSettingsStore.Current.FirstWeekGuideStep >= FirstWeekGuideDigest.TotalSteps
                ? "İlk hafta rehberi tamamlandı. Artık kulüp tamamen sende."
                : "Rehber adımı tamamlandı; sıradaki görev hazır.");
    }

    private void UpdateSaveDeskButtons()
    {
        var exists = _controller.SaveFileExists();
        _loadGameButton.Disabled = !exists;
        _loadGameButton.Text = exists ? "Yükle" : "Yükle (kayıt yok)";
    }

    private void RefreshTransferWindowStatus()
    {
        var window = _controller.Host.WorldModule.Queries.GetTransferWindow();
        var openText = window.OpenedOnDayNumber is { } openDay
            ? $" · açılış {GameDate.ToDisplayDateString(openDay)}"
            : string.Empty;
        var closeText = window.ClosesOnDayNumber is { } closeDay
            ? $" · kapanış {GameDate.ToDisplayDateString(closeDay)}"
            : string.Empty;
        _transferWindowLabel.Text = $"Transfer penceresi: {window.StatusName}{openText}{closeText}";
        _openTransferWindowButton.Disabled = window.IsOpen;
        _closeTransferWindowButton.Disabled = !window.IsOpen;
    }

    private void RefreshTransferDesk()
    {
        var desk = _controller.BuildTransferDeskBriefing();
        _transferDeskLabel.Text = desk.ToSummaryText();
        _transferScoutReportLabel.Text = desk.ToScoutReportText();
        BindTransferNextStep(desk);
    }

    private void BindTransferNextStep(Application.Transfer.Queries.TransferDeskBriefing desk)
    {
        if (desk.NextStep is null)
        {
            _transferNextStepButton.Visible = false;
            _transferNextStepAction = Application.CareerHub.Queries.OfficeNextStepGuide.ActionNavigate;
            _transferNextStepTarget = Application.Transfer.Queries.TransferNextStep.TargetTransfer;
            return;
        }

        var step = desk.NextStep;
        _transferNextStepButton.Text = step.ButtonLabel;
        _transferNextStepButton.Visible = true;
        _transferNextStepTarget = step.TargetPageCode;
        _transferNextStepAction = step.ActionCode switch
        {
            Application.Transfer.Queries.TransferNextStep.ActionSellFringe =>
                Application.CareerHub.Queries.OfficeNextStepGuide.ActionSellFringe,
            Application.Transfer.Queries.TransferNextStep.ActionOpenTransferWindow =>
                Application.CareerHub.Queries.OfficeNextStepGuide.ActionOpenTransferWindow,
            Application.Transfer.Queries.TransferNextStep.ActionScanNeeds =>
                Application.CareerHub.Queries.OfficeNextStepGuide.ActionScanNeeds,
            Application.Transfer.Queries.TransferNextStep.ActionPickTarget =>
                Application.CareerHub.Queries.OfficeNextStepGuide.ActionPickTarget,
            Application.Transfer.Queries.TransferNextStep.ActionStartProcess =>
                Application.CareerHub.Queries.OfficeNextStepGuide.ActionStartProcess,
            Application.Transfer.Queries.TransferNextStep.ActionAdvanceProcess =>
                Application.CareerHub.Queries.OfficeNextStepGuide.ActionAdvanceProcess,
            Application.Transfer.Queries.TransferNextStep.ActionAnswerOffers =>
                Application.CareerHub.Queries.OfficeNextStepGuide.ActionAnswerOffers,
            _ => Application.CareerHub.Queries.OfficeNextStepGuide.ActionNavigate,
        };

        var expandNegotiation = step.ReasonCode is
            Application.Transfer.Queries.TransferNextStep.ReasonAdvanceProcess
            or Application.Transfer.Queries.TransferNextStep.ReasonAnswerOffers
            or Application.Transfer.Queries.TransferNextStep.ReasonStartProcess;
        if (expandNegotiation)
        {
            SetTransferNegotiationExpanded(true);
        }
    }

    private void ToggleTransferScoutDetails()
    {
        var visible = !_transferScoutDetails.Visible;
        _transferScoutDetails.Visible = visible;
        _transferScoutToggle.Text = visible ? "Scout raporunu gizle" : "Scout raporu al";
    }

    private void RefreshTransferBudgetStatus()
    {
        var clubId = _controller.Host.ManagerModule.Store.Career.ActiveEmployment?.ClubId;
        if (clubId is null)
        {
            _transferBudgetLabel.Text = "Transfer bütçesi: işsiz — kayıt yok.";
            return;
        }

        var budget = _controller.Host.ClubModule.TransferBudget.Get(clubId.Value);
        var day = _controller.Host.WorldModule.TimelineStore.Timeline.CurrentDate;
        var wage = _controller.Host.ClubModule.WageBudget?.Get(clubId.Value, day);
        var wageText = wage is null
            ? string.Empty
            : $" · Maaş: kullanılabilir {wage.Available:N0} / limit {wage.Limit:N0}"
              + $" (taahhüt {wage.Committed:N0}, rezerve {wage.Reserved:N0})";
        _transferBudgetLabel.Text =
            $"Transfer bütçesi: kullanılabilir {budget.Available:N0} / limit {budget.Limit:N0}"
            + $" (rezerve {budget.Reserved:N0}, harcanan {budget.Spent:N0})"
            + wageText;
    }

    private void RefreshClubOfferStatus()
    {
        var offers = _controller.Host.TransferModule.Queries.GetManagedClubOffers();
        if (offers.ClubId is null)
        {
            _clubOfferLabel.Text = "Kulüp teklifi: işsiz — kayıt yok.";
            return;
        }

        if (offers.RecentOffers.Count == 0)
        {
            _clubOfferLabel.Text = "Kulüp teklifi: yok — sportif onay sonrası teklif sun.";
            return;
        }

        var latest = offers.RecentOffers[0];
        _clubOfferLabel.Text =
            $"Kulüp teklifi: bekleyen {offers.PendingCount}"
            + $" · son tur {latest.Round} ücret {latest.OfferedFee} ({latest.StatusName})";
    }

    private void RefreshContractProposalStatus()
    {
        var proposals = _controller.Host.TransferModule.Queries.GetManagedContractProposals();
        if (proposals.ClubId is null)
        {
            _contractProposalLabel.Text = "Sözleşme teklifi: işsiz — kayıt yok.";
            return;
        }

        if (proposals.RecentProposals.Count == 0)
        {
            _contractProposalLabel.Text =
                "Sözleşme teklifi: yok — kulüp anlaşması veya (FA) sportif onay sonrası sun.";
            return;
        }

        var latest = proposals.RecentProposals[0];
        _contractProposalLabel.Text =
            $"Sözleşme teklifi: bekleyen {proposals.PendingCount}"
            + $" · son tur {latest.Round}"
            + $" maaş {latest.WeeklyWage} × {latest.ContractYears}y ({latest.StatusName})";
    }

    private void RefreshTransferProcessStatus()
    {
        var view = _controller.Host.TransferModule.Queries.GetManagedClubProcesses();
        if (view.ClubId is null)
        {
            _transferProcessLabel.Text = "Transfer süreci: işsiz — kayıt yok.";
            return;
        }

        if (view.ActiveCount == 0)
        {
            _transferProcessLabel.Text = "Transfer süreci: aktif yok — hedeften süreç aç (müzakere yok).";
            return;
        }

        var preview = string.Join(
            " · ",
            view.ActiveProcesses.Take(2).Select(p =>
                $"{_controller.GetPlayerDisplayName(p.PlayerId)} {p.StatusName}"));
        _transferProcessLabel.Text = $"Transfer süreci: {view.ActiveCount} aktif · {preview}";
    }

    private void RefreshShortlistTargetStatus()
    {
        var scout = _controller.BuildScoutTransferDigest();
        _scoutCandidates = scout.Candidates;
        _scoutReportLabel.Text = scout.HasClub
            ? $"{scout.Headline}\n{scout.NeedLine}"
            : scout.Headline;
        _scoutCandidateList.Clear();
        foreach (var candidate in scout.Candidates)
        {
            _scoutCandidateList.AddItem(candidate.ToListLabel());
        }

        if (_selectedScoutPlayerId is not long selectedId
            || !scout.Candidates.Any(candidate => candidate.PlayerId == selectedId))
        {
            _selectedScoutPlayerId = scout.Candidates.FirstOrDefault()?.PlayerId;
        }

        if (_selectedScoutPlayerId is long activeId)
        {
            var selected = scout.Candidates
                .Select((candidate, index) => (candidate, index))
                .FirstOrDefault(item => item.candidate.PlayerId == activeId);
            _scoutCandidateList.Select(selected.index);
            _suggestTargetButton.Disabled = selected.candidate?.IsListedTarget ?? true;
        }

        var view = _controller.Host.TransferModule.Queries.GetManagedClubShortlistTargets();
        if (view.ClubId is null)
        {
            _shortlistTargetLabel.Text = "Shortlist/hedef: işsiz — kayıt yok.";
            return;
        }

        if (view.ActiveShortlistCount == 0 && view.ListedTargetCount == 0)
        {
            _shortlistTargetLabel.Text = "Shortlist/hedef: boş — hedef öner (Process açılmaz).";
            return;
        }

        var targetPreview = view.ListedTargets.Take(2)
            .Select(t => _controller.GetPlayerDisplayName(t.PlayerId))
            .ToArray();
        _shortlistTargetLabel.Text =
            $"Shortlist {view.ActiveShortlistCount} · hedef {view.ListedTargetCount}"
            + (targetPreview.Length == 0 ? string.Empty : $" · {string.Join(" · ", targetPreview)}");
    }

    private void RefreshTransferNeedStatus()
    {
        var needs = _controller.Host.TransferModule.Queries.GetManagedClubNeeds();
        if (needs.ClubId is null)
        {
            _transferNeedLabel.Text = "Transfer ihtiyacı: işsiz — kayıt yok.";
            return;
        }

        if (needs.OpenCount == 0)
        {
            _transferNeedLabel.Text = "Transfer ihtiyacı: açık yok — tara veya pozisyon ihtiyacı tanımla.";
            return;
        }

        var preview = string.Join(
            " · ",
            needs.OpenNeeds.Take(3).Select(n => $"{n.KindName} (öncelik {n.Priority})"));
        _transferNeedLabel.Text =
            $"Transfer ihtiyacı: {needs.OpenCount} açık · {preview}"
            + (needs.OpenCount > 3 ? "…" : string.Empty);
    }

    private void UpdateTransferNeedButtons(
        Application.ManagerCareer.Queries.ManagerCareerReadModel manager)
    {
        var employed = string.Equals(manager.EmploymentStatus, "Employed", StringComparison.Ordinal);
        var openCount = employed
            ? _controller.Host.TransferModule.Queries.GetManagedClubNeeds().OpenCount
            : 0;
        var listedCount = employed
            ? _controller.Host.TransferModule.Queries.GetManagedClubShortlistTargets().ListedTargetCount
            : 0;
        _refreshTransferNeedsButton.Disabled = !employed;
        _declareTransferNeedButton.Disabled = !employed;
        _closeTransferNeedButton.Disabled = !employed || openCount == 0;
        var windowOpen = _controller.Host.WorldModule.Queries.GetTransferWindow().IsOpen;
        // Boş pencere: İhtiyaç Tara görünür (D-364); Pozisyon İhtiyacı geliştirici API'sinde kalır.
        _refreshTransferNeedsButton.Visible = employed && windowOpen && openCount == 0;
        _declareTransferNeedButton.Visible = false;
        _closeTransferNeedButton.Visible = !_closeTransferNeedButton.Disabled;
        var activeProcessCount = employed
            ? _controller.Host.TransferModule.Queries.GetManagedClubProcesses().ActiveCount
            : 0;
        var selectedScout = _selectedScoutPlayerId is long selectedScoutId
            ? _scoutCandidates.FirstOrDefault(candidate => candidate.PlayerId == selectedScoutId)
            : null;
        _suggestTargetButton.Disabled = !employed || selectedScout is null || selectedScout.IsListedTarget;
        _dropTargetButton.Disabled = !employed || listedCount == 0;
        var nextStepPicksTarget = string.Equals(
            _transferNextStepAction,
            Application.CareerHub.Queries.OfficeNextStepGuide.ActionPickTarget,
            StringComparison.Ordinal);
        _suggestTargetButton.Visible = !_suggestTargetButton.Disabled && !nextStepPicksTarget;
        _dropTargetButton.Visible = !_dropTargetButton.Disabled;
        var processes = employed
            ? _controller.Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
            : [];
        var pendingSporting = processes.Any(p =>
            p.StatusCode == (int)Domain.Transfer.TransferProcessStatus.SportingApprovalPending);
        var canRequestSporting = processes.Any(p =>
            p.StatusCode == (int)Domain.Transfer.TransferProcessStatus.UnderEvaluation);
        _openProcessButton.Disabled = !employed || !windowOpen || listedCount == 0;
        _withdrawProcessButton.Disabled = !employed || activeProcessCount == 0;
        _requestSportingApprovalButton.Disabled = !canRequestSporting;
        _grantSportingApprovalButton.Disabled = !pendingSporting;
        _rejectSportingApprovalButton.Disabled = !pendingSporting;
        _openProcessButton.Visible = !_openProcessButton.Disabled;
        _withdrawProcessButton.Visible = !_withdrawProcessButton.Disabled;
        _requestSportingApprovalButton.Visible = !_requestSportingApprovalButton.Disabled;
        _grantSportingApprovalButton.Visible = !_grantSportingApprovalButton.Disabled;
        _rejectSportingApprovalButton.Visible = !_rejectSportingApprovalButton.Disabled;

        var canSubmitOffer = employed && windowOpen && processes.Any(p =>
            !p.IsFreeAgent
            && p.StatusCode is (int)Domain.Transfer.TransferProcessStatus.SportingApproved
                or (int)Domain.Transfer.TransferProcessStatus.ClubNegotiation);
        var pendingOffers = employed
            && _controller.Host.TransferModule.Queries.GetManagedClubOffers().PendingCount > 0;
        _submitClubOfferButton.Disabled = !canSubmitOffer || pendingOffers;
        _acceptClubOfferButton.Disabled = !pendingOffers;
        _rejectClubOfferButton.Disabled = !pendingOffers;
        _counterClubOfferButton.Disabled = !pendingOffers || !windowOpen;
        _submitClubOfferButton.Visible = !_submitClubOfferButton.Disabled;
        _acceptClubOfferButton.Visible = !_acceptClubOfferButton.Disabled;
        _rejectClubOfferButton.Visible = !_rejectClubOfferButton.Disabled;
        _counterClubOfferButton.Visible = !_counterClubOfferButton.Disabled;

        var canSubmitProposal = employed && windowOpen && processes.Any(p =>
            p.StatusCode is (int)Domain.Transfer.TransferProcessStatus.ClubAgreementReached
                or (int)Domain.Transfer.TransferProcessStatus.PlayerNegotiation
            || (p.IsFreeAgent
                && p.StatusCode == (int)Domain.Transfer.TransferProcessStatus.SportingApproved));
        var pendingProposals = employed
            && _controller.Host.TransferModule.Queries.GetManagedContractProposals().PendingCount > 0;
        _submitContractProposalButton.Disabled = !canSubmitProposal || pendingProposals;
        _acceptContractProposalButton.Disabled = !pendingProposals;
        _rejectContractProposalButton.Disabled = !pendingProposals;
        _counterContractProposalButton.Disabled = !pendingProposals || !windowOpen;
        _submitContractProposalButton.Visible = !_submitContractProposalButton.Disabled;
        _acceptContractProposalButton.Visible = !_acceptContractProposalButton.Disabled;
        _rejectContractProposalButton.Visible = !_rejectContractProposalButton.Disabled;
        _counterContractProposalButton.Visible = !_counterContractProposalButton.Disabled;

        var canRequestFinancial = employed && processes.Any(p =>
            p.StatusCode == (int)Domain.Transfer.TransferProcessStatus.PlayerAgreementReached);
        var pendingFinancial = employed && processes.Any(p =>
            p.StatusCode == (int)Domain.Transfer.TransferProcessStatus.FinancialApprovalPending);
        var canStartComplete = employed && windowOpen && processes.Any(p =>
            p.StatusCode == (int)Domain.Transfer.TransferProcessStatus.FinancialApproved);
        var canFinishComplete = employed && processes.Any(p =>
            p.StatusCode == (int)Domain.Transfer.TransferProcessStatus.CompletionPending);
        _requestFinancialApprovalButton.Disabled = !canRequestFinancial;
        _grantFinancialApprovalButton.Disabled = !pendingFinancial;
        _rejectFinancialApprovalButton.Disabled = !pendingFinancial;
        _completeTransferButton.Disabled = !canStartComplete && !canFinishComplete;
        _requestFinancialApprovalButton.Visible = !_requestFinancialApprovalButton.Disabled;
        _grantFinancialApprovalButton.Visible = !_grantFinancialApprovalButton.Disabled;
        _rejectFinancialApprovalButton.Visible = !_rejectFinancialApprovalButton.Disabled;
        _completeTransferButton.Visible = !_completeTransferButton.Disabled;
    }

    private void RefreshTrainingStatus()
    {
        RefreshMatchTrainingPriority();
        var training = _controller.GetTrainingSummary();
        if (training.ClubId is null)
        {
            _trainingLabel.Text = "Antrenman: işsiz — plan uygulanamaz.";
            RefreshPreparationBriefing();
            return;
        }

        if (!training.HasPlan)
        {
            var injuryHint = training.InjuredSlotCount > 0
                ? $" · sakat {training.InjuredSlotCount} (uygun değil {training.UnavailableSlotCount})"
                : string.Empty;
            _trainingLabel.Text =
                $"Antrenman: plan yok — yoğunluk / odak / dinlenme seç (maç gücünü etkiler){injuryHint}.";
            RefreshPreparationBriefing();
            return;
        }

        var injuryText = training.InjuredSlotCount > 0
            ? $" · sakat {training.InjuredSlotCount} (uygun değil {training.UnavailableSlotCount})"
            : string.Empty;
        _trainingLabel.Text =
            $"Antrenman: {FormatStoredIntensity(training.Intensity)}/{FormatStoredFocus(training.Focus)}"
            + $" · {FormatStoredRest(training.RestApproach)}"
            + $" · XI yorgunluk {training.AverageFatigue} · fitness {training.AverageFitness}{injuryText}";
        RefreshPreparationBriefing();
    }

    private void RefreshMatchTrainingPriority()
    {
        var digest = _controller.BuildMatchTrainingPriorityDigest();
        if (!digest.IsAvailable)
        {
            _matchTrainingPriorityLabel.Text = digest.Headline;
            foreach (var button in _matchTrainingPriorityButtons.Values)
            {
                button.Disabled = true;
            }

            return;
        }

        var options = string.Join(
            "\n",
            digest.Options.Take(3).Select(option =>
                $"{(option.IsRecommended ? "★" : "·")} {option.Title}: "
                + $"{option.BoostLine} {option.RiskLine}"));
        _matchTrainingPriorityLabel.Text =
            $"{digest.Headline}\n{digest.SquadStatusLine}\n{digest.StaffFeedback}\n{options}";
        var hasDueMatch = _controller.BuildNextMatchBriefing().HasMatch;
        foreach (var button in _matchTrainingPriorityButtons.Values)
        {
            button.Disabled = !hasDueMatch;
        }
    }

    private void RefreshPreparationBriefing()
    {
        _prepBriefingLabel.Text = _controller.BuildPreparationBriefing().ToDisplayText();
    }

    private static string FormatStoredFocus(int? focus) =>
        focus switch
        {
            (int)TrainingFocus.General => "Dengeli",
            (int)TrainingFocus.Fitness => "Kondisyon",
            (int)TrainingFocus.Recovery => "Toparlanma",
            (int)TrainingFocus.Tactical => "Taktik",
            _ => focus?.ToString() ?? "-",
        };

    private static string FormatStoredIntensity(int? intensity) =>
        intensity switch
        {
            (int)TrainingIntensity.Low => "Hafif",
            (int)TrainingIntensity.Medium => "Orta",
            (int)TrainingIntensity.High => "Yoğun",
            _ => intensity?.ToString() ?? "-",
        };

    private static string FormatStoredRest(int? rest) =>
        rest switch
        {
            (int)RestApproach.Light => "Az dinlenme",
            (int)RestApproach.Normal => "Normal dinlenme",
            (int)RestApproach.Heavy => "Bol dinlenme",
            _ => rest?.ToString() ?? "-",
        };

    private void UpdateTrainingButtons(
        Application.ManagerCareer.Queries.ManagerCareerReadModel manager)
    {
        var employed = string.Equals(manager.EmploymentStatus, "Employed", StringComparison.Ordinal);
        _trainLowButton.Disabled = !employed;
        _trainMediumButton.Disabled = !employed;
        _trainHighButton.Disabled = !employed;
        _focusGeneralButton.Disabled = !employed;
        _focusFitnessButton.Disabled = !employed;
        _focusTacticalButton.Disabled = !employed;
        _focusRecoveryButton.Disabled = !employed;
        _restLightButton.Disabled = !employed;
        _restNormalButton.Disabled = !employed;
        _restHeavyButton.Disabled = !employed;
        foreach (var button in _matchTrainingPriorityButtons.Values)
        {
            button.Disabled = !employed || !_controller.BuildNextMatchBriefing().HasMatch;
        }

        var training = _controller.GetTrainingSummary();
        _trainLowButton.ButtonPressed = training.Intensity == (int)TrainingIntensity.Low;
        _trainMediumButton.ButtonPressed = training.Intensity == (int)TrainingIntensity.Medium;
        _trainHighButton.ButtonPressed = training.Intensity == (int)TrainingIntensity.High;
        _focusGeneralButton.ButtonPressed = training.Focus == (int)TrainingFocus.General;
        _focusFitnessButton.ButtonPressed = training.Focus == (int)TrainingFocus.Fitness;
        _focusTacticalButton.ButtonPressed = training.Focus == (int)TrainingFocus.Tactical;
        _focusRecoveryButton.ButtonPressed = training.Focus == (int)TrainingFocus.Recovery;
        _restLightButton.ButtonPressed = training.RestApproach == (int)RestApproach.Light;
        _restNormalButton.ButtonPressed = training.RestApproach == (int)RestApproach.Normal;
        _restHeavyButton.ButtonPressed = training.RestApproach == (int)RestApproach.Heavy;
    }

    private void RefreshTacticStatus()
    {
        var tactic = _controller.Host.TeamPreparationModule.TacticQueries.GetManagedClubPlan();
        if (tactic.ClubId is null)
        {
            _tacticLabel.Text = "Taktik: işsiz — plan yok.";
            RefreshPreparationBriefing();
            return;
        }

        if (string.Equals(tactic.FormationName, "yok", StringComparison.Ordinal))
        {
            _tacticLabel.Text = "Taktik: henüz yok — lig kur / formasyon seç.";
            RefreshPreparationBriefing();
            return;
        }

        _tacticLabel.Text =
            $"Taktik: {tactic.FormationName} · {tactic.ApproachName}"
            + $" · {tactic.PressingName} · {tactic.DefensiveLineName}"
            + $" · maç {_controller.GetManagedTacticModifierLabel()}";
        var phase = _controller.BuildDualPhaseTacticDigest();
        _dualPhaseTacticLabel.Text = phase is null
            ? "Faz planı: klasik yerleşim kullanılıyor."
            : $"Faz planı: {phase.Headline}\n{phase.StaffNote}";
        RefreshPreparationBriefing();
    }

    private void UpdateTacticButtons(
        Application.ManagerCareer.Queries.ManagerCareerReadModel manager)
    {
        var employed = string.Equals(manager.EmploymentStatus, "Employed", StringComparison.Ordinal);
        _formation442Button.Disabled = !employed;
        _formation433Button.Disabled = !employed;
        _formation352Button.Disabled = !employed;
        _approachBalancedButton.Disabled = !employed;
        _approachAttackingButton.Disabled = !employed;
        _approachDefensiveButton.Disabled = !employed;
        _pressingLowButton.Disabled = !employed;
        _pressingBalancedButton.Disabled = !employed;
        _pressingHighButton.Disabled = !employed;
        _lineDeepButton.Disabled = !employed;
        _lineStandardButton.Disabled = !employed;
        _lineHighButton.Disabled = !employed;
        foreach (var button in _dualPhaseTacticButtons)
        {
            button.Disabled = !employed;
        }

        var tactic = _controller.Host.TeamPreparationModule.TacticQueries.GetManagedClubPlan();
        _formation442Button.ButtonPressed = tactic.ClubId is not null && tactic.Formation == Formation.F442;
        _formation433Button.ButtonPressed = tactic.ClubId is not null && tactic.Formation == Formation.F433;
        _formation352Button.ButtonPressed = tactic.ClubId is not null && tactic.Formation == Formation.F352;
        _approachBalancedButton.ButtonPressed =
            tactic.ClubId is not null && tactic.Approach == TacticalApproach.Balanced;
        _approachAttackingButton.ButtonPressed =
            tactic.ClubId is not null && tactic.Approach == TacticalApproach.Attacking;
        _approachDefensiveButton.ButtonPressed =
            tactic.ClubId is not null && tactic.Approach == TacticalApproach.Defensive;
        _pressingLowButton.ButtonPressed =
            tactic.ClubId is not null && tactic.Pressing == PressingIntensity.LowBlock;
        _pressingBalancedButton.ButtonPressed =
            tactic.ClubId is not null && tactic.Pressing == PressingIntensity.Balanced;
        _pressingHighButton.ButtonPressed =
            tactic.ClubId is not null && tactic.Pressing == PressingIntensity.HighPress;
        _lineDeepButton.ButtonPressed =
            tactic.ClubId is not null && tactic.DefensiveLine == DefensiveLine.Deep;
        _lineStandardButton.ButtonPressed =
            tactic.ClubId is not null && tactic.DefensiveLine == DefensiveLine.Standard;
        _lineHighButton.ButtonPressed =
            tactic.ClubId is not null && tactic.DefensiveLine == DefensiveLine.High;
    }

    private void RefreshDevelopmentStatus()
    {
        var development = _controller.Host.PlayerCareerModule.Queries.GetManagedClubSummary();
        if (development.ClubId is null)
        {
            _developmentLabel.Text = "Gelişim: işsiz — kadro profili yok.";
            return;
        }

        if (development.PlayerCount == 0)
        {
            _developmentLabel.Text = "Gelişim: henüz yok — antrenman veya maç sonrası oluşur.";
            return;
        }

        _developmentLabel.Text =
            $"Gelişim: ort. CA {development.AverageCurrentAbility} / PA {development.AveragePotentialAbility}"
            + $" · yaş {development.AverageAge}"
            + $" · {development.PlayerCount} oyuncu"
            + (development.DecliningCount > 0 ? $" · düşüşte {development.DecliningCount}" : string.Empty)
            + (development.DevelopedThisWeekCount > 0
                ? $" · bugün gelişen {development.DevelopedThisWeekCount}"
                : string.Empty);
    }

    private void RefreshContractStatus()
    {
        var contracts = _controller.Host.ContractModule.Queries.GetManagedClubSummary();
        if (contracts.ClubId is null)
        {
            _contractLabel.Text = "Sözleşme: işsiz — kayıt yok.";
            return;
        }

        if (contracts.ActiveCount == 0)
        {
            _contractLabel.Text = contracts.FreeAgentReleasedCount > 0
                ? $"Sözleşme: aktif yok · serbest {contracts.FreeAgentReleasedCount}"
                : "Sözleşme: aktif yok — antrenman/gün ilerletme ile oluşur.";
            return;
        }

        _contractLabel.Text =
            $"Sözleşme: {contracts.ActiveCount} aktif"
            + $" · ort. ücret {contracts.AverageWeeklyWage}"
            + (contracts.ExpiringWithinYearCount > 0
                ? $" · 1 yıl içinde biten {contracts.ExpiringWithinYearCount}"
                : string.Empty)
            + (contracts.FreeAgentReleasedCount > 0
                ? $" · serbest {contracts.FreeAgentReleasedCount}"
                : string.Empty);
    }

    private void RefreshMemoryStatus()
    {
        var manager = _controller.Host.ManagerModule.Queries.GetCareer();
        var memories = _controller.Host.SocialContinuityModule.Queries.GetActiveForActor(
            Domain.SocialContinuity.ActorKind.Manager,
            manager.ManagerId,
            take: 5);

        if (memories.ActiveCount == 0)
        {
            _memoryLabel.Text = "Hafıza: menajer için aktif kayıt yok.";
            return;
        }

        var preview = string.Join(
            " · ",
            memories.RecentActive.Select(m => $"{m.CategoryName}/{m.ValenceName}"));
        _memoryLabel.Text =
            $"Hafıza: {memories.ActiveCount} aktif"
            + (string.IsNullOrWhiteSpace(preview) ? string.Empty : $" — {preview}");
    }

    private void RefreshDressingRoomEcho()
    {
        var echo = _controller.BuildDressingRoomEcho();
        _dressingRoomEchoLabel.Visible = echo is not null;
        _dressingRoomEchoLabel.Text = echo?.ToDisplayText() ?? string.Empty;
    }

    private void RefreshPromiseStatus()
    {
        var manager = _controller.Host.ManagerModule.Queries.GetCareer();
        var promises = _controller.Host.SocialContinuityModule.PromiseQueries.GetActiveForPromisor(
            Domain.SocialContinuity.ActorKind.Manager,
            manager.ManagerId,
            take: 5);

        if (promises.ActiveCount == 0)
        {
            _promiseLabel.Text = "Sözler: menajer için aktif söz yok.";
            return;
        }

        var preview = string.Join(
            " · ",
            promises.RecentActive.Select(p =>
                $"{p.KindName} {_controller.GetPlayerDisplayName(p.PromiseeId)} {p.ProgressCount}/{p.TargetCount}"));
        _promiseLabel.Text =
            $"Sözler: {promises.ActiveCount} aktif"
            + (string.IsNullOrWhiteSpace(preview) ? string.Empty : $" — {preview}");
    }

    private void RefreshRelationshipStatus()
    {
        var manager = _controller.Host.ManagerModule.Queries.GetCareer();
        var relationships = _controller.Host.SocialContinuityModule.RelationshipQueries
            .GetActiveForManager(manager.ManagerId, take: 5);

        if (relationships.ActiveCount == 0)
        {
            _relationshipLabel.Text = "İlişki: oyuncu→menajer aktif kayıt yok.";
            return;
        }

        var preview = string.Join(
            " · ",
            relationships.RecentActive.Select(r =>
                $"{_controller.GetPlayerDisplayName(r.ObserverPlayerId)} G:{r.TrustLabel}/S:{r.RespectLabel}/U:{r.CompatibilityLabel}"));
        _relationshipLabel.Text =
            $"İlişki: {relationships.ActiveCount} aktif"
            + (string.IsNullOrWhiteSpace(preview) ? string.Empty : $" — {preview}");
    }

    private void RefreshDecisionStatus()
    {
        var pending = _controller.Host.InteractionModule.Queries.GetPending(take: 5);
        var desk = _controller.BuildDecisionDeskDigest();
        _deskLabel.Text = CompactSummary(desk.Headline, desk.SupportingLine ?? string.Empty);

        if (pending.OpenCount == 0)
        {
            _decisionLabel.Text = "Kararlar: masada bekleyen yok (cevaplar Bugün → Masada).";
            _grantDecisionButton.Disabled = true;
            _refuseDecisionButton.Disabled = true;
            _disciplineWarningButton.Disabled = true;
            _disciplineFineButton.Disabled = true;
            _disciplineSupportButton.Disabled = true;
            _boardCounterButton.Disabled = true;
            _pressCriticizeButton.Disabled = true;
            _grantDecisionButton.Text = "Talebi Kabul Et";
            _refuseDecisionButton.Text = "Talebi Reddet";
            _grantDecisionButton.Visible = false;
            _refuseDecisionButton.Visible = false;
            _disciplineWarningButton.Visible = false;
            _disciplineFineButton.Visible = false;
            _disciplineSupportButton.Visible = false;
            _boardCounterButton.Visible = false;
            _pressCriticizeButton.Visible = false;
            return;
        }

        var first = pending.OpenRequests[0];
        var options = _controller.Host.InteractionModule.DialogueOptions.GetForDecision(
            new Domain.Interaction.DecisionRequestId(first.DecisionRequestId));
        var grant = options.Options.FirstOrDefault(o =>
            o.OptionCode != Domain.Interaction.DecisionRequest.OptionRefuse
            && o.OptionCode is not (
                Domain.Interaction.DecisionRequest.OptionIssueWarning
                or Domain.Interaction.DecisionRequest.OptionIssueFine
                or Domain.Interaction.DecisionRequest.OptionOfferSupport
                or Domain.Interaction.DecisionRequest.OptionCounterBoardDemand
                or Domain.Interaction.DecisionRequest.OptionPubliclyCriticize));
        var refuse = options.Options.FirstOrDefault(o =>
            o.OptionCode == Domain.Interaction.DecisionRequest.OptionRefuse);
        var warning = options.Options.FirstOrDefault(o =>
            o.OptionCode == Domain.Interaction.DecisionRequest.OptionIssueWarning);
        var fine = options.Options.FirstOrDefault(o =>
            o.OptionCode == Domain.Interaction.DecisionRequest.OptionIssueFine);
        var support = options.Options.FirstOrDefault(o =>
            o.OptionCode == Domain.Interaction.DecisionRequest.OptionOfferSupport);
        var counter = options.Options.FirstOrDefault(o =>
            o.OptionCode == Domain.Interaction.DecisionRequest.OptionCounterBoardDemand);
        var criticize = options.Options.FirstOrDefault(o =>
            o.OptionCode == Domain.Interaction.DecisionRequest.OptionPubliclyCriticize);

        if (grant is not null)
        {
            _grantDecisionButton.Visible = true;
            _grantDecisionButton.Text = grant.DisplayText;
            _grantDecisionButton.Disabled = !grant.IsEligible;
        }
        else
        {
            _grantDecisionButton.Visible = false;
            _grantDecisionButton.Disabled = true;
        }

        if (refuse is not null)
        {
            _refuseDecisionButton.Visible = true;
            _refuseDecisionButton.Text = refuse.DisplayText;
            _refuseDecisionButton.Disabled = !refuse.IsEligible;
        }
        else
        {
            _refuseDecisionButton.Visible = false;
            _refuseDecisionButton.Disabled = true;
        }

        _disciplineWarningButton.Visible = warning is not null;
        _disciplineFineButton.Visible = fine is not null;
        _disciplineSupportButton.Visible = support is not null;
        _boardCounterButton.Visible = counter is not null;
        _pressCriticizeButton.Visible = criticize is not null;
        _disciplineWarningButton.Disabled = warning is null || !warning.IsEligible;
        _disciplineFineButton.Disabled = fine is null || !fine.IsEligible;
        _disciplineSupportButton.Disabled = support is null || !support.IsEligible;
        _boardCounterButton.Disabled = counter is null || !counter.IsEligible;
        _pressCriticizeButton.Disabled = criticize is null || !criticize.IsEligible;

        _decisionLabel.Text =
            $"Kararlar: {pending.OpenCount} açık — cevaplar Bugün → Masada. {desk.SupportingLine}";

        var awaiting = _controller.Host.InteractionModule.DialogueSessionStore.Sessions
            .Count(s => s.IsAwaitingPlayer);
        if (awaiting > 0)
        {
            _decisionLabel.Text += $" · diyalog:{awaiting}";
        }

        RefreshTodayPulse();
    }

    private void UpdateJobOfferButtons(
        Application.ManagerCareer.Queries.ManagerCareerReadModel manager)
    {
        var unemployed = string.Equals(manager.EmploymentStatus, "Unemployed", StringComparison.Ordinal);
        _generateOfferButton.Disabled = !unemployed;
        _acceptOfferButton.Disabled = !unemployed || manager.PendingOfferId is null;
        _generateOfferButton.Visible = unemployed && manager.PendingOfferId is null;
        _acceptOfferButton.Visible = !_acceptOfferButton.Disabled;

        var signable = !unemployed
            && _controller.Host.ContractModule.Queries.GetNextSignableFreeAgentForManagedClub() is not null;
        _signFreeAgentButton.Disabled = !signable;
        _signFreeAgentButton.Visible = signable;

        var capacity = _controller.BuildSquadCapacityDigest();
        _promoteOverflowButton.Disabled = !capacity.IsOverCapacity;
        _promoteOverflowButton.Visible = capacity.IsOverCapacity;
        _promoteOverflowButton.Text = capacity.IsOverCapacity && capacity.OverflowPlayerIds.Count > 0
            ? $"Taşanı Kadroya Al — {_controller.GetPlayerDisplayName(capacity.OverflowPlayerIds[0])}"
            : "Taşanı Kadroya Al";

        var releaseId = !unemployed
            ? _controller.SuggestReleaseCandidatePlayerId()
            : null;
        _releaseCapacityButton.Disabled = releaseId is null;
        _releaseCapacityButton.Visible =
            releaseId is not null && (capacity.IsFull || capacity.IsOverCapacity);
        _releaseCapacityButton.Text = releaseId is long rid
            ? (capacity.IsOverCapacity
                ? $"Taşanı Serbest Bırak — {_controller.GetPlayerDisplayName(rid)}"
                : $"Yer Aç — {_controller.GetPlayerDisplayName(rid)}")
            : "Yer Aç";

        _sellFringeButton.Visible = false;
        RefreshPlayerDossierSellButton();
    }

    private string GetClubDisplayNameSafe(long clubId) => _controller.GetClubDisplayName(clubId);

    private void RefreshTodayPulse()
    {
        var pulse = _controller.BuildTodayPulse();
        var pulseDetail = pulse.PulseLines.FirstOrDefault();
        _pulseLabel.Text = CompactSummary(pulse.Headline, pulseDetail ?? string.Empty);
        var weekStory = _controller.BuildWeekStory();
        var weekMood = _controller.BuildWeekMood(weekStoryActive: weekStory.IsActive);
        _weekStoryLabel.Visible = false;
        _weekStoryLabel.Text = string.Empty;
        var recoveryPath = _controller.BuildInjuryRecoveryPath();
        _recoveryPathLabel.Visible = false;
        _recoveryPathLabel.Text = string.Empty;

        var currentDay = _controller.Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        var duePlayable = pending is { IsApproved: true };
        var dueUnapproved = pending is { IsApproved: false };
        var blocker = _controller.BuildTimeAdvanceBlockerDigest();
        var archivePhase = _controller.IsSeasonArchivePhase();
        var prepBriefing = _controller.BuildPreparationBriefing();
        var prepSuggestion = prepBriefing.Suggestion;
        var leagueNextStep = _controller.BuildLeagueWorldBriefing().NextStep;
        var transferNextStep = _controller.BuildTransferDeskBriefing().NextStep;
        var nextStep = Application.CareerHub.Queries.OfficeNextStepGuide.ResolveFromPulse(
            pulse.PrimaryFocusCode,
            hasDueUnapprovedMatch: dueUnapproved,
            hasDuePlayableMatch: duePlayable,
            canAdvanceDay: blocker.CanAdvance,
            primaryBlockerCode: blocker.PrimaryBlockerCode,
            seasonTransitionReady: _controller.CanTransitionToNextSeason(),
            seasonArchivePhase: archivePhase,
            prepSuggestion: prepSuggestion,
            leagueNextStep: leagueNextStep,
            transferNextStep: transferNextStep,
            hasInjuryPressure: prepBriefing.HasInjuryPressure,
            recoveryPath: recoveryPath,
            weekStory: weekStory);
        var officeDigest = Application.Competition.Queries.PostMatchOfficeDigest
            .FromTodayPulse(pulse, weekMood, weekStory, nextStep?.ButtonLabel, currentDay);
        _officeLabel.Text = CompactOfficeText(officeDigest);
        BindOfficeNextStep(nextStep);
    }

    private void RefreshSelectionStatus()
    {
        var currentDay = _controller.Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);

        var briefing = _controller.BuildNextMatchBriefing();
        _briefingLabel.Text = CompactSummary(
            briefing.HasMatch && !string.IsNullOrWhiteSpace(briefing.FixtureLine)
                ? briefing.FixtureLine
                : briefing.Headline,
            briefing.HasMatch ? briefing.Headline : string.Empty);
        RefreshTodayPulse();

        if (pending is null)
        {
            _selectionLabel.Text = "Kadro: vadesi gelmiş maç yok.";
            _approveSelectionButton.Disabled = true;
            _swapSelectionButton.Disabled = true;
            return;
        }

        var autoSwapHint = briefing.BeatLines
            .FirstOrDefault(b => b.StartsWith("Sakat XI'de:", StringComparison.Ordinal));
        _selectionLabel.Text = pending.IsApproved
            ? "Kadro: onaylı — gerekirse XI↔Yedek ile dokun."
            : autoSwapHint is not null
                ? "Kadro: onayda sakatlar dışarı — " + autoSwapHint
                : "Kadro: onay gerekli — Sıradaki Adım ile XI'yi kilitle.";
        _approveSelectionButton.Disabled = pending.IsApproved;
        _swapSelectionButton.Disabled = false;
    }

    private void UpdatePrimaryHints(int dueMatchCount, bool canAdvance)
    {
        var currentDay = _controller.Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        var selectionBlocksPlay = pending is not null && !pending.IsApproved;

        _playButton.Disabled = dueMatchCount == 0 || selectionBlocksPlay;
        var tension = _controller.Host.TeamPreparationModule.PromiseTension
            .GetForNextDueMatch(currentDay);
        var atRisk = tension is
        {
            HasTension: true,
            ToneCode: Application.TeamPreparation.Services.PreMatchPromiseTensionQueryService.ToneAtRisk
        };

        _playButton.Text = dueMatchCount == 0
            ? "Maç Gününe Git"
            : selectionBlocksPlay
                ? "Maç Gününe Git (önce kadro)"
                : atRisk
                    ? $"Maç Gününe Git ({dueMatchCount}) · söz riski"
                    : $"Maç Gününe Git ({dueMatchCount})";

        _advanceDayButton.Disabled = !canAdvance;
        _advanceWeekButton.Disabled = !canAdvance;
        _advanceDayButton.Text = canAdvance ? "1 Gün İlerlet" : "1 Gün İlerlet (engelli)";
        _advanceWeekButton.Text = canAdvance ? "7 Gün İlerlet" : "7 Gün İlerlet (engelli)";

        var canTransition = _controller.CanTransitionToNextSeason();
        _seasonTransitionButton.Disabled = !canTransition;
        var season = _controller.Host.CompetitionModule.Queries.GetCurrentSeason();
        var progress = season is null
            ? null
            : _controller.Host.CompetitionModule.Queries.GetSeasonProgress(season.SeasonId);
        _seasonTransitionButton.Text = progress is { CanArchive: true, CanComplete: false }
            ? "Yeni Sezona Geç"
            : "Sezonu Bitir → Yeni Sezon";
        SyncTodayActionVisibility();
    }

    /// <summary>
    /// Merkez yüzeyi: tek birincil OfficeNextStep, en fazla bağlamsal XI↔Yedek ve
    /// çakışmayan 1 Gün yardımcı eylemi. Domain/debug tetikleri burada çoğaltılmaz.
    /// </summary>
    private void SyncTodayActionVisibility()
    {
        var hasOpenDecision = _controller.Host.InteractionModule.Queries
            .GetPending(take: 1)
            .OpenCount > 0;
        var currentDay = _controller.Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pendingMatch = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        var canAdvance = _controller.Host.WorldModule.Queries.GetTimeAdvanceEligibility().CanAdvance;

        // Bu eylemlerin tamamı OfficeNextStep tarafından zaten kapsanır.
        _approveSelectionButton.Visible = false;
        _playButton.Visible = false;
        _seasonTransitionButton.Visible = false;
        _advanceWeekButton.Visible = false;

        // Açık karar varken aynı kartta yalnızca gerçek diyalog seçenekleri kalır.
        _officeNextStepButton.Visible = _officeNextStepButton.Visible && !hasOpenDecision;
        _swapSelectionButton.Visible = !hasOpenDecision && pendingMatch is not null;
        _advanceDayButton.Visible =
            !hasOpenDecision
            && canAdvance
            && pendingMatch is null
            && (!_officeNextStepButton.Visible
                || (_officeNextStepAction
                    is not Application.CareerHub.Queries.OfficeNextStepGuide.ActionAdvanceDay
                    and not Application.CareerHub.Queries.OfficeNextStepGuide.ActionAdvanceToNext));
    }

    private void RefreshSquadList()
    {
        RefreshYouthAcademy();
        _squadList.Clear();
        var capacity = _controller.BuildSquadCapacityDigest();
        _squadCapacityLabel.Text = capacity.ToDisplayText();

        var manager = _controller.Host.ManagerModule.Queries.GetCareer();
        if (manager.EmployedClubId is not long clubId)
        {
            _squadStatusLabel.Text = "A takım: işsiz — kayıt yok.";
            _playerManagementPlayers = Array.Empty<PlayerManagementLine>();
            _playerManagementHeadlineLabel.Text = "Futbolcu yönetimi: kulüp görevi yok.";
            _selectedPlayerId = null;
            RefreshPlayerDetail();
            return;
        }

        var persisted = _controller.Host.TeamPreparationModule.SquadStore.Get(
            new Domain.Shared.ClubId(clubId));
        var clubName = _controller.GetClubDisplayName(clubId);
        _squadStatusLabel.Text = persisted is null || persisted.Members.Count == 0
            ? "A takım: henüz yok — lig kur / gün ilerle / antrenman ile oluşur."
            : $"A takım: {persisted.Members.Count} üye · {clubName}";

        var management = _controller.BuildPlayerManagementDigest();
        _playerManagementPlayers = management.Players;
        _playerManagementHeadlineLabel.Text = management.Headline;

        foreach (var player in management.Players)
        {
            _squadList.AddItem(player.ToListLabel());
        }

        if (_selectedPlayerId is not long selectedId
            || !management.Players.Any(player => player.PlayerId == selectedId))
        {
            _selectedPlayerId = management.Players.FirstOrDefault()?.PlayerId;
        }

        if (_selectedPlayerId is long activePlayerId)
        {
            var selectedIndex = management.Players
                .Select((player, index) => (player, index))
                .FirstOrDefault(item => item.player.PlayerId == activePlayerId).index;
            _squadList.Select(selectedIndex);
        }

        RefreshPlayerDetail();
        RefreshClubPitch(management.Players);

        if (_playerDossierOverlay is { Visible: true } && _selectedPlayerId is long openId)
        {
            if (management.Players.Any(player => player.PlayerId == openId))
            {
                OpenPlayerDossier(openId);
            }
            else
            {
                ClosePlayerDossier();
            }
        }

        if (capacity.IsOverCapacity)
        {
            foreach (var id in capacity.OverflowPlayerIds)
            {
                _squadList.AddItem($"[kadro dışı sözleşme] {_controller.GetPlayerDisplayName(id)}");
            }
        }
    }

    private void RefreshYouthAcademy()
    {
        RefreshYouthAcademyLifecycle();
        _academyCandidateList.Clear();
        var intake = _controller.BuildYouthAcademyIntake();
        if (intake is null)
        {
            _academyCandidates = [];
            _selectedAcademyCandidateId = null;
            _academyIntakeLabel.Text = "Akademi: aktif kulüp ve sezon bulunmuyor.";
            UpdateAcademyDecisionButtons();
            return;
        }

        if (!intake.IsRevealed)
        {
            _academyCandidates = [];
            _selectedAcademyCandidateId = null;
            _academyIntakeLabel.Text =
                $"Akademi ekibi adayları sezon öncesinde açıklayacak "
                + $"({GameDate.ToDisplayDateString(intake.RevealDayNumber)}).";
            UpdateAcademyDecisionButtons();
            return;
        }

        _academyCandidates = intake.Candidates;
        _academyIntakeLabel.Text = intake.IsComplete
            ? $"Sezon #{intake.SeasonId}: değerlendirme tamamlandı · akademide tutulan {intake.AcceptedCount}."
            : $"Sezon #{intake.SeasonId}: {intake.Candidates.Count} aday · karar bekleyen {intake.PendingCount}. "
              + "Akademide tutmak A takıma otomatik terfi değildir.";
        foreach (var candidate in intake.Candidates)
        {
            var status = candidate.DecisionStatus switch
            {
                YouthAcademyCandidateDecisionStatus.Accepted => "✓ AKADEMİDE",
                YouthAcademyCandidateDecisionStatus.Rejected => "× KAPANDI",
                _ => "KARAR BEKLİYOR",
            };
            _academyCandidateList.AddItem(
                $"{candidate.DisplayName} · {candidate.Age} · {candidate.PositionCode} "
                + $"· güç {candidate.CurrentAbility} / pot. {candidate.PotentialAbility} "
                + $"· {candidate.DevelopmentProfile} · {status}");
        }

        if (_selectedAcademyCandidateId is not long selected
            || !intake.Candidates.Any(candidate => candidate.PlayerId == selected))
        {
            _selectedAcademyCandidateId = intake.Candidates
                .FirstOrDefault(candidate =>
                    candidate.DecisionStatus == YouthAcademyCandidateDecisionStatus.Pending)
                ?.PlayerId;
        }

        if (_selectedAcademyCandidateId is long selectedId)
        {
            var index = intake.Candidates
                .Select((candidate, index) => (candidate, index))
                .First(item => item.candidate.PlayerId == selectedId)
                .index;
            _academyCandidateList.Select(index);
        }

        UpdateAcademyDecisionButtons();
    }

    private void UpdateAcademyDecisionButtons()
    {
        var selected = _selectedAcademyCandidateId is long id
            ? _academyCandidates.FirstOrDefault(candidate => candidate.PlayerId == id)
            : null;
        var canDecide = selected?.DecisionStatus == YouthAcademyCandidateDecisionStatus.Pending;
        _academyAcceptButton.Disabled = !canDecide;
        _academyRejectButton.Disabled = !canDecide;
    }

    private void RefreshYouthAcademyLifecycle()
    {
        _academyLifecycleList.Clear();
        var lifecycle = _controller.BuildYouthAcademyLifecycle();
        if (lifecycle is null || lifecycle.Players.Count == 0)
        {
            _academyLifecyclePlayers = [];
            _selectedAcademyLifecyclePlayerId = null;
            _academyLifecycleLabel.Text =
                "Akademide tutulan oyuncular sezon geçişleriyle gelişim raporuna girecek.";
            UpdateAcademyPromotionButton();
            return;
        }

        _academyLifecyclePlayers = lifecycle.Players;
        _academyLifecycleLabel.Text =
            $"Gelişen {lifecycle.DevelopingCount} · terfiye hazır {lifecycle.PromotionEligibleCount} "
            + $"· A takıma çıkan {lifecycle.PromotedCount}. "
            + "Dolu kadroda terfiye hazır en iyi genç, ilk emeklilik slotunda otomatik öncelik alır.";
        foreach (var player in lifecycle.Players)
        {
            var status = player.Status switch
            {
                YouthAcademyLifecycleStatus.Developing => "GELİŞİYOR",
                YouthAcademyLifecycleStatus.PromotionEligible => "TERFİYE HAZIR",
                YouthAcademyLifecycleStatus.PromotedToFirstTeam => "A TAKIMDA",
                _ => player.Status.ToString(),
            };
            _academyLifecycleList.AddItem(
                $"{player.DisplayName} · {player.Age} · {player.PositionCode} · "
                + $"güç {player.CurrentAbility} / pot. {player.PotentialAbility} · {status}");
        }

        if (_selectedAcademyLifecyclePlayerId is not long selected
            || !lifecycle.Players.Any(player => player.PlayerId == selected))
        {
            _selectedAcademyLifecyclePlayerId = lifecycle.Players
                .FirstOrDefault(player => player.Status == YouthAcademyLifecycleStatus.PromotionEligible)
                ?.PlayerId
                ?? lifecycle.Players.First().PlayerId;
        }

        var selectedIndex = lifecycle.Players
            .Select((player, index) => (player, index))
            .First(item => item.player.PlayerId == _selectedAcademyLifecyclePlayerId)
            .index;
        _academyLifecycleList.Select(selectedIndex);
        UpdateAcademyPromotionButton();
    }

    private void UpdateAcademyPromotionButton()
    {
        var selected = _selectedAcademyLifecyclePlayerId is long id
            ? _academyLifecyclePlayers.FirstOrDefault(player => player.PlayerId == id)
            : null;
        _academyPromoteButton.Disabled =
            selected?.Status != YouthAcademyLifecycleStatus.PromotionEligible;
    }

    private void RefreshFixtureList()
    {
        _fixtureList.Clear();
        var season = _controller.Host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null || season.FixtureCount == 0)
        {
            return;
        }

        var round = (int)_roundSelector.Value;
        var fixtures = _controller.Host.CompetitionModule.Queries.GetFixturesByRound(season.SeasonId, round);

        foreach (var fixture in fixtures)
        {
            var home = _controller.GetClubDisplayName(fixture.HomeClubId);
            var away = _controller.GetClubDisplayName(fixture.AwayClubId);
            var score = fixture.HomeGoals is int homeGoals && fixture.AwayGoals is int awayGoals
                ? $" {homeGoals}-{awayGoals}"
                : string.Empty;
            _fixtureList.AddItem(
                $"{home} vs {away}{score} · {fixture.ScheduledIsoDate} · {fixture.Status}");
        }
    }

    private void RefreshStandings()
    {
        _leagueBriefingLabel.Text = _controller.BuildLeagueWorldBriefing().ToDisplayText();
        var statistics = _controller.BuildLeagueStatisticsDigest();
        _leagueStatisticsLabel.Text = $"{statistics.Headline}\n{statistics.LeadersLine}";
        _managedLeagueStatisticsLabel.Text = statistics.ManagedClubLine;
        _standingsTable.Clear();

        var season = _controller.Host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null || season.FixtureCount == 0)
        {
            ShowEmptyStandings("Henüz fikstür oluşturulmadı");
            return;
        }

        var managedClubId = _controller.Host.ManagerModule.Queries.GetCareer().EmployedClubId;
        var standings = _controller.Host.CompetitionModule.Queries.GetStandings(season.SeasonId);
        if (standings.Count == 0)
        {
            ShowEmptyStandings("Henüz puan durumu oluşmadı");
            return;
        }

        var root = _standingsTable.CreateItem();
        for (var index = 0; index < standings.Count; index++)
        {
            var entry = standings[index];
            var row = _standingsTable.CreateItem(root);
            var values = new[]
            {
                (index + 1).ToString(),
                _controller.GetClubDisplayName(entry.ClubId),
                entry.Played.ToString(),
                entry.Won.ToString(),
                entry.Drawn.ToString(),
                entry.Lost.ToString(),
                entry.GoalsFor.ToString(),
                entry.GoalsAgainst.ToString(),
                entry.GoalDifference > 0 ? $"+{entry.GoalDifference}" : entry.GoalDifference.ToString(),
                statistics.GetForm(entry.ClubId),
                entry.Points.ToString(),
            };

            for (var column = 0; column < values.Length; column++)
            {
                row.SetText(column, values[column]);
                if (column != 1)
                {
                    row.SetTextAlignment(column, HorizontalAlignment.Center);
                }
            }

            var rankColor = index switch
            {
                0 => CareerUiTheme.Accent,
                <= 3 => CareerUiTheme.Data,
                >= 14 => CareerUiTheme.DangerSoft,
                _ => CareerUiTheme.InkMuted,
            };
            row.SetCustomColor(0, rankColor);
            row.SetCustomColor(9, CareerUiTheme.Data);
            row.SetCustomColor(10, CareerUiTheme.Ink);

            if (managedClubId == entry.ClubId)
            {
                for (var column = 0; column < values.Length; column++)
                {
                    row.SetCustomBgColor(
                        column,
                        new Color(
                            CareerUiTheme.Action.R,
                            CareerUiTheme.Action.G,
                            CareerUiTheme.Action.B,
                            0.16f));
                }
                row.SetCustomColor(1, CareerUiTheme.ActionBright);
            }
        }
    }

    private static string TranslateExpectation(string? code) =>
        code switch
        {
            "TitleChallenge" => "Şampiyonluk",
            "UpperHalf" => "Üst yarı",
            "MidTable" => "Orta sıra",
            "LowerHalf" => "Alt yarı",
            "Survival" => "Küme kurtarma",
            _ => code ?? "—",
        };

    private static string TranslateRisk(string? code) =>
        code switch
        {
            "Secure" => "güvende",
            "Stable" => "stabil",
            "UnderReview" => "incelemede",
            "Critical" => "kritik",
            _ => code ?? "—",
        };

    private static string TranslateReason(string? code) =>
        code switch
        {
            "WinOnTrack" => "galibiyet (hedefte)",
            "WinBehindExpectation" => "galibiyet (hedefin gerisinde)",
            "DrawOnTrack" => "beraberlik (hedefte)",
            "DrawBehindExpectation" => "beraberlik (hedefin gerisinde)",
            "LossOnTrack" => "mağlubiyet (hedefte)",
            "LossBehindExpectation" => "mağlubiyet (hedefin gerisinde)",
            _ => code ?? "—",
        };
}
