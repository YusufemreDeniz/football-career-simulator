using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using Godot;

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
    private Label _prepBriefingLabel = null!;
    private Label _developmentLabel = null!;
    private Label _contractLabel = null!;
    private Label _memoryLabel = null!;
    private Label _promiseLabel = null!;
    private Label _relationshipLabel = null!;
    private Label _deskLabel = null!;
    private Label _officeLabel = null!;
    private Label _recoveryPathLabel = null!;
    private Label _weekStoryLabel = null!;
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
    private Button _openTransferWindowButton = null!;
    private Button _closeTransferWindowButton = null!;
    private Label _transferNeedLabel = null!;
    private Label _scoutReportLabel = null!;
    private ItemList _scoutCandidateList = null!;
    private IReadOnlyList<ScoutCandidateLine> _scoutCandidates = Array.Empty<ScoutCandidateLine>();
    private long? _selectedScoutPlayerId;
    private Label _shortlistTargetLabel = null!;
    private Label _transferProcessLabel = null!;
    private Label _tacticLabel = null!;
    private Label _squadStatusLabel = null!;
    private Label _squadCapacityLabel = null!;
    private TextureRect _clubCrest = null!;
    private TextureRect _homeKit = null!;
    private TextureRect _awayKit = null!;
    private TextureRect _thirdKit = null!;
    private Tree _standingsTable = null!;
    private Label _leagueBriefingLabel = null!;
    private Label _statusLabel = null!;
    private Label _saveDeskLabel = null!;
    private Button _saveGameButton = null!;
    private Button _loadGameButton = null!;
    private SpinBox _roundSelector = null!;
    private ItemList _fixtureList = null!;
    private ItemList _squadList = null!;
    private Label _playerManagementHeadlineLabel = null!;
    private Label _playerDetailLabel = null!;
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
    private Button _restLightButton = null!;
    private Button _restNormalButton = null!;
    private Button _restHeavyButton = null!;
    private Button _formation442Button = null!;
    private Button _formation433Button = null!;
    private Button _formation352Button = null!;
    private Button _approachBalancedButton = null!;
    private Button _approachAttackingButton = null!;
    private Button _approachDefensiveButton = null!;
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

    public CareerHubScreen(CareerSessionController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public override void _Ready()
    {
        BuildLayout();
        RefreshUi();
    }

    public void SetStatus(string message) => PulseStatus(message);

    public void ApplyOfficeReturn(Application.Competition.Queries.PostMatchOfficeDigest digest)
    {
        ArgumentNullException.ThrowIfNull(digest);
        ShowPage(HubPage.Today);
        RefreshUi();
        _officeLabel.Text = digest.ToDisplayText();
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
        _officeLabel.Text = digest.ToDisplayText();
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
                Apply(_controller.AdvanceDays(1));
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
            default:
                ShowPage(_officeNextStepTarget);
                return;
        }
    }

    private void BuildLayout()
    {
        CareerUiTheme.EnsureLoaded();
        AddChild(CareerUiTheme.CreateAtmosphereBackground());

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.GrowHorizontal = GrowDirection.Both;
        margin.GrowVertical = GrowDirection.Both;
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(margin);

        var shell = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddThemeConstantOverride("separation", 10);
        margin.AddChild(shell);

        // Sabit kariyer kabuğu: kulüp kimliği + tarih, ardından aktif ekran başlığı.
        var topBar = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        topBar.AddThemeConstantOverride("separation", 10);
        shell.AddChild(topBar);

        _clubCrest = new TextureRect
        {
            Name = "ClubCrest",
            CustomMinimumSize = new Vector2(64, 64),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = "Kulüp arması",
            Visible = false,
        };
        topBar.AddChild(_clubCrest);

        var brandLockup = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        brandLockup.AddThemeConstantOverride("separation", 0);
        topBar.AddChild(brandLockup);

        var brand = new Label { Text = "FCS  /  CAREER MODE" };
        CareerUiTheme.StyleSection(brand);
        brandLockup.AddChild(brand);

        _managerLabel = BodyLabel("ManagerLabel", autowrap: true);
        CareerUiTheme.StyleHeadline(_managerLabel);
        _managerLabel.AddThemeFontSizeOverride("font_size", 22);
        brandLockup.AddChild(_managerLabel);

        var datePanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        datePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.PillPanel());
        topBar.AddChild(datePanel);
        _dateLabel = BodyLabel("DateLabel");
        _dateLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _dateLabel.AddThemeFontSizeOverride("font_size", 12);
        datePanel.AddChild(_dateLabel);

        _careerButton = SecondaryButton("DOSYA");
        _careerButton.CustomMinimumSize = new Vector2(58, 48);
        _careerButton.AddThemeFontSizeOverride("font_size", 11);
        _careerButton.TooltipText = "Kariyer dosyası, kayıt ve ana menü";
        _careerButton.Pressed += () => ShowPage(HubPage.File);
        topBar.AddChild(_careerButton);

        var careerMeta = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        careerMeta.AddThemeConstantOverride("separation", 2);
        shell.AddChild(careerMeta);
        _seasonLabel = BodyLabel("SeasonLabel", muted: true, autowrap: true);
        careerMeta.AddChild(_seasonLabel);
        _progressLabel = BodyLabel("ProgressLabel", muted: true, autowrap: true);
        _progressLabel.AddThemeFontSizeOverride("font_size", 12);
        careerMeta.AddChild(_progressLabel);

        var screenHeading = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        screenHeading.AddThemeConstantOverride("separation", 10);
        shell.AddChild(screenHeading);
        var headingCopy = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headingCopy.AddThemeConstantOverride("separation", 0);
        screenHeading.AddChild(headingCopy);
        _pageTitleLabel = new Label();
        CareerUiTheme.StyleHeadline(_pageTitleLabel);
        _pageTitleLabel.AddThemeFontSizeOverride("font_size", 26);
        headingCopy.AddChild(_pageTitleLabel);
        _pageSubtitleLabel = BodyLabel("PageSubtitle", muted: true, autowrap: true);
        _pageSubtitleLabel.AddThemeFontSizeOverride("font_size", 12);
        headingCopy.AddChild(_pageSubtitleLabel);

        var liveChip = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        liveChip.AddThemeStyleboxOverride("panel", CareerUiTheme.LivePillPanel());
        var liveLabel = new Label { Text = "●  CANLI" };
        CareerUiTheme.StyleBody(liveLabel);
        liveLabel.AddThemeFontSizeOverride("font_size", 11);
        liveLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        liveChip.AddChild(liveLabel);
        screenHeading.AddChild(liveChip);

        _pageScroll = new MobileScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        shell.AddChild(_pageScroll);

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
        var statusPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        statusPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.StatusPanel());
        statusPanel.AddChild(_statusLabel);
        shell.AddChild(statusPanel);

        BuildNavBar(shell);

        ShowPage(HubPage.Today);

        shell.Modulate = new Color(1f, 1f, 1f, 0f);
        var fadeTween = CreateTween();
        fadeTween.TweenProperty(shell, "modulate:a", 1f, 0.28f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private void BuildNavBar(Control parent)
    {
        var navPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        navPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NavigationPanel());
        parent.AddChild(navPanel);

        var nav = new GridContainer
        {
            Columns = 5,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        nav.AddThemeConstantOverride("h_separation", 4);
        navPanel.AddChild(nav);

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
            nav.AddChild(button);
            _navButtons[i] = button;
        }

        _navButtons[(int)HubPage.File] = _careerButton;
    }

    private void ShowPage(HubPage page)
    {
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
        CreateTween()
            .TweenProperty(current, "modulate:a", 1f, 0.2f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        Callable.From(() => _pageScroll.ScrollVertical = 0).CallDeferred();
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
        var priorityCard = AddCard(page, "GÜNÜN NABZI", emphasized: true);
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

        var officeCard = AddCard(page, "OFİSTEN NOTLAR");
        _officeLabel = BodyLabel("OfficeLabel", autowrap: true);
        _officeLabel.Text = Application.Competition.Queries.PostMatchOfficeDigest.Quiet().ToDisplayText();
        officeCard.AddChild(_officeLabel);

        var decisionCard = AddCard(page, "KARAR MASASI");
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

        var matchCard = AddCard(page, "SIRADAKİ MAÇ", emphasized: true);
        _briefingLabel = BodyLabel("BriefingLabel", autowrap: true);
        matchCard.AddChild(_briefingLabel);

        _selectionLabel = BodyLabel("SelectionLabel", autowrap: true);
        matchCard.AddChild(_selectionLabel);

        var primaryRow = ActionFlow();
        matchCard.AddChild(primaryRow);

        _approveSelectionButton = PrimaryButton("Kadro Onayla");
        _approveSelectionButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        primaryRow.AddChild(_approveSelectionButton);

        _swapSelectionButton = SecondaryButton("XI↔Yedek");
        _swapSelectionButton.Pressed += () =>
            Apply(_controller.SwapLastStarterWithFirstBenchForNextDueMatch());
        primaryRow.AddChild(_swapSelectionButton);

        _playButton = PrimaryButton("Maç Gününe Git");
        _playButton.Pressed += OnPlayMatches;
        primaryRow.AddChild(_playButton);

        _seasonTransitionButton = PrimaryButton("Sezonu Bitir → Yeni Sezon");
        _seasonTransitionButton.Pressed += () => Apply(_controller.TransitionToNextSeason());
        primaryRow.AddChild(_seasonTransitionButton);

        _advanceDayButton = SecondaryButton("1 Gün İlerlet");
        _advanceDayButton.Pressed += () => Apply(_controller.AdvanceDays(1));
        primaryRow.AddChild(_advanceDayButton);

        _advanceWeekButton = SecondaryButton("7 Gün İlerlet");
        _advanceWeekButton.Pressed += () => Apply(_controller.AdvanceDays(7));
        primaryRow.AddChild(_advanceWeekButton);
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
        _developmentLabel = BodyLabel("DevelopmentLabel", autowrap: true);
        squadCard.AddChild(_developmentLabel);
        _contractLabel = BodyLabel("ContractLabel", autowrap: true);
        squadCard.AddChild(_contractLabel);

        var kitStrip = new HBoxContainer
        {
            Name = "ClubKitStrip",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        kitStrip.AddThemeConstantOverride("separation", 18);
        squadCard.AddChild(kitStrip);
        _homeKit = AddKitPreview(kitStrip, "İÇ SAHA", "Kulübün resmi iç saha forması");
        _awayKit = AddKitPreview(kitStrip, "DEPLASMAN", "Kulübün resmi deplasman forması");
        _thirdKit = AddKitPreview(kitStrip, "ÜÇÜNCÜ", "Kulübün resmi üçüncü forması");

        var teamDynamicsCard = AddCard(page, "SOYUNMA ODASI & SÖZLER");
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
            CustomMinimumSize = new Vector2(0, 340),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_squadList);
        _squadList.ItemSelected += OnSquadPlayerSelected;
        playerManagementCard.AddChild(_squadList);
        _playerDetailLabel = BodyLabel("PlayerDetailLabel", autowrap: true);
        _playerDetailLabel.CustomMinimumSize = new Vector2(0, 146);
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
        return page;
    }

    private Control BuildTransferPage()
    {
        var page = PageRoot();
        var overviewCard = AddCard(page, "TRANSFER PENCERESİ", emphasized: true);
        _transferDeskLabel = BodyLabel("TransferDeskLabel", autowrap: true);
        overviewCard.AddChild(_transferDeskLabel);
        _transferWindowLabel = BodyLabel("TransferWindowLabel", autowrap: true);
        overviewCard.AddChild(_transferWindowLabel);
        _transferBudgetLabel = BodyLabel("TransferBudgetLabel", autowrap: true);
        overviewCard.AddChild(_transferBudgetLabel);

        var windowRow = ActionFlow();
        overviewCard.AddChild(windowRow);

        _openTransferWindowButton = SecondaryButton("Pencere Aç");
        _openTransferWindowButton.Pressed += () => Apply(_controller.OpenTransferWindow());
        windowRow.AddChild(_openTransferWindowButton);

        _closeTransferWindowButton = SecondaryButton("Pencere Kapat");
        _closeTransferWindowButton.Pressed += () => Apply(_controller.CloseTransferWindow());
        windowRow.AddChild(_closeTransferWindowButton);

        var scoutingCard = AddCard(page, "1  İHTİYAÇ & HEDEF");
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
        return page;
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

        _focusGeneralButton = SecondaryButton("Odak: Genel");
        _focusGeneralButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.General));
        focusRow.AddChild(_focusGeneralButton);

        _focusFitnessButton = SecondaryButton("Kondisyon");
        _focusFitnessButton.Pressed += () =>
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Fitness));
        focusRow.AddChild(_focusFitnessButton);

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
        return page;
    }

    private Control BuildWorldPage()
    {
        var page = PageRoot();
        var leagueCard = AddCard(page, "LİG NABZI", emphasized: true);
        _leagueBriefingLabel = BodyLabel("LeagueBriefingLabel", autowrap: true);
        leagueCard.AddChild(_leagueBriefingLabel);

        var tableCard = AddCard(page, "PUAN DURUMU");
        _standingsTable = new Tree
        {
            Name = "StandingsTable",
            Columns = 10,
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
        label.AddThemeFontSizeOverride("font_size", 11);
        column.AddChild(label);
        return texture;
    }

    private static void ConfigureStandingsColumns(Tree table)
    {
        var titles = new[] { "#", "TAKIM", "O", "G", "B", "M", "A", "Y", "AV", "P" };
        for (var column = 0; column < titles.Length; column++)
        {
            table.SetColumnTitle(column, titles[column]);
            table.SetColumnExpand(column, column == 1);
            table.SetColumnCustomMinimumWidth(column, column == 1 ? 220 : column == 0 ? 38 : 46);
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

    private void PulseStatus(string message)
    {
        _statusLabel.Text = message;
        _statusLabel.Modulate = new Color(1f, 1f, 1f, 0.35f);
        var tween = CreateTween();
        tween.TweenProperty(_statusLabel, "modulate:a", 1f, 0.28f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private void OnPlayMatches()
    {
        Callable.From(() => MatchDayRequested?.Invoke()).CallDeferred();
    }

    private void Apply(UiActionResult result)
    {
        PulseStatus(result.Message);
        RefreshUi();
        // Toparlanma onayı gibi ofis köprüleri nabız metninin üstüne yazılır.
        if (!string.IsNullOrWhiteSpace(result.NarrativeBridgeLine))
        {
            _officeLabel.Text = result.NarrativeBridgeLine;
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

    private void OnScoutCandidateSelected(long index)
    {
        if (index < 0 || index >= _scoutCandidates.Count)
        {
            return;
        }

        _selectedScoutPlayerId = _scoutCandidates[(int)index].PlayerId;
        _suggestTargetButton.Disabled = _scoutCandidates[(int)index].IsListedTarget;
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
        _playerDetailLabel.Text = selected?.ToDetailText()
            ?? "Kariyer, fizik, sözleşme ve ilişki ayrıntıları için kadrodan bir futbolcu seç.";

        var disabled = selected is null;
        _promiseStartButton.Disabled = disabled;
        _promisePlayingTimeButton.Disabled = disabled;
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

        _dateLabel.Text = current.IsoDate;
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
            RefreshSelectionStatus();
            RefreshTrainingStatus();
            RefreshDevelopmentStatus();
            RefreshContractStatus();
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
        RefreshSelectionStatus();
        RefreshTrainingStatus();
        RefreshDevelopmentStatus();
        RefreshContractStatus();
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
        var desk = _controller.BuildSaveDeskDigest();
        _saveDeskLabel.Text = desk.ToDisplayText();
        UpdateSaveDeskButtons();
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
        var openText = window.OpenedOnDayNumber is { } openDay ? $" · açılış gün {openDay}" : string.Empty;
        var closeText = window.ClosesOnDayNumber is { } closeDay ? $" · kapanış gün {closeDay}" : string.Empty;
        _transferWindowLabel.Text = $"Transfer penceresi: {window.StatusName}{openText}{closeText}";
        _openTransferWindowButton.Disabled = window.IsOpen;
        _closeTransferWindowButton.Disabled = !window.IsOpen;
    }

    private void RefreshTransferDesk()
    {
        _transferDeskLabel.Text = _controller.BuildTransferDeskBriefing().ToDisplayText();
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
            + $" · son #{latest.OfferId} tur {latest.Round} ücret {latest.OfferedFee} ({latest.StatusName})";
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
            + $" · son #{latest.ProposalId} tur {latest.Round}"
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
            view.ActiveProcesses.Take(2).Select(p => $"#{p.ProcessId} {p.StatusName} P{p.PlayerId}"));
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
            .Select(t => $"T#{t.TargetId} P{t.PlayerId}")
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
            needs.OpenNeeds.Take(3).Select(n => $"#{n.NeedId} {n.KindName} (P{n.Priority})"));
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
        var activeProcessCount = employed
            ? _controller.Host.TransferModule.Queries.GetManagedClubProcesses().ActiveCount
            : 0;
        var windowOpen = _controller.Host.WorldModule.Queries.GetTransferWindow().IsOpen;
        var selectedScout = _selectedScoutPlayerId is long selectedScoutId
            ? _scoutCandidates.FirstOrDefault(candidate => candidate.PlayerId == selectedScoutId)
            : null;
        _suggestTargetButton.Disabled = !employed || selectedScout is null || selectedScout.IsListedTarget;
        _dropTargetButton.Disabled = !employed || listedCount == 0;
        var processes = employed
            ? _controller.Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
            : [];
        var pendingSporting = processes.Any(p =>
            p.StatusCode == (int)Domain.Transfer.TransferProcessStatus.SportingApprovalPending);
        var canRequestSporting = processes.Any(p =>
            p.StatusCode == (int)Domain.Transfer.TransferProcessStatus.UnderEvaluation);
        _openProcessButton.Disabled = !employed || !windowOpen;
        _withdrawProcessButton.Disabled = !employed || activeProcessCount == 0;
        _requestSportingApprovalButton.Disabled = !canRequestSporting;
        _grantSportingApprovalButton.Disabled = !pendingSporting;
        _rejectSportingApprovalButton.Disabled = !pendingSporting;

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
    }

    private void RefreshTrainingStatus()
    {
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

    private void RefreshPreparationBriefing()
    {
        _prepBriefingLabel.Text = _controller.BuildPreparationBriefing().ToDisplayText();
    }

    private static string FormatStoredFocus(int? focus) =>
        focus switch
        {
            (int)TrainingFocus.General => "Genel",
            (int)TrainingFocus.Fitness => "Kondisyon",
            (int)TrainingFocus.Recovery => "Toparlanma",
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
        _focusRecoveryButton.Disabled = !employed;
        _restLightButton.Disabled = !employed;
        _restNormalButton.Disabled = !employed;
        _restHeavyButton.Disabled = !employed;
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
            + $" · maç {_controller.GetManagedTacticModifierLabel()}";
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
                $"{p.KindName} oyuncu#{p.PromiseeId} {p.ProgressCount}/{p.TargetCount}"));
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
                $"oyuncu#{r.ObserverPlayerId} G:{r.TrustLabel}/S:{r.RespectLabel}/U:{r.CompatibilityLabel}"));
        _relationshipLabel.Text =
            $"İlişki: {relationships.ActiveCount} aktif"
            + (string.IsNullOrWhiteSpace(preview) ? string.Empty : $" — {preview}");
    }

    private void RefreshDecisionStatus()
    {
        var pending = _controller.Host.InteractionModule.Queries.GetPending(take: 5);
        var currentDay = _controller.Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var desk = Application.Interaction.Queries.DecisionDeskDigest.Compose(pending, currentDay);
        _deskLabel.Text = desk.ToDisplayText();

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
            _grantDecisionButton.Text = grant.DisplayText;
            _grantDecisionButton.Disabled = !grant.IsEligible;
        }
        else
        {
            _grantDecisionButton.Disabled = true;
        }

        if (refuse is not null)
        {
            _refuseDecisionButton.Text = refuse.DisplayText;
            _refuseDecisionButton.Disabled = !refuse.IsEligible;
        }
        else
        {
            _refuseDecisionButton.Disabled = true;
        }

        _disciplineWarningButton.Disabled = warning is null || !warning.IsEligible;
        _disciplineFineButton.Disabled = fine is null || !fine.IsEligible;
        _disciplineSupportButton.Disabled = support is null || !support.IsEligible;
        _boardCounterButton.Disabled = counter is null || !counter.IsEligible;
        _pressCriticizeButton.Disabled = criticize is null || !criticize.IsEligible;

        var preview = string.Join(
            " · ",
            pending.OpenRequests.Select(d =>
                $"{d.KindName}{(d.IsHardBlocker ? " [zorunlu]" : string.Empty)} son:{d.DeadlineDayNumber}"));
        _decisionLabel.Text =
            $"Kararlar: {pending.OpenCount} açık — cevaplar Bugün → Masada. {preview}";

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

        var signable = !unemployed
            && _controller.Host.ContractModule.Queries.GetNextSignableFreeAgentForManagedClub() is not null;
        _signFreeAgentButton.Disabled = !signable;

        var capacity = _controller.BuildSquadCapacityDigest();
        _promoteOverflowButton.Disabled = !capacity.IsOverCapacity;
        _promoteOverflowButton.Text = capacity.IsOverCapacity && capacity.OverflowPlayerIds.Count > 0
            ? $"Taşanı Kadroya Al (#{capacity.OverflowPlayerIds[0]})"
            : "Taşanı Kadroya Al";

        var releaseId = !unemployed
            ? _controller.SuggestReleaseCandidatePlayerId()
            : null;
        _releaseCapacityButton.Disabled = releaseId is null;
        _releaseCapacityButton.Text = releaseId is long rid
            ? (capacity.IsOverCapacity
                ? $"Taşanı Serbest Bırak (#{rid})"
                : $"Yer Aç (#{rid})")
            : "Yer Aç";

        var saleId = !unemployed
            ? _controller.SuggestSaleCandidatePlayerId()
            : null;
        var windowOpen = _controller.Host.WorldModule.Queries.GetTransferWindow().IsOpen;
        _sellFringeButton.Disabled = saleId is null || !windowOpen;
        _sellFringeButton.Text = saleId is long sid
            ? (windowOpen ? $"Satışa Çıkar (#{sid})" : "Satışa Çıkar (pencere kapalı)")
            : "Satışa Çıkar";
    }

    private string GetClubDisplayNameSafe(long clubId) => _controller.GetClubDisplayName(clubId);

    private void RefreshTodayPulse()
    {
        var pulse = _controller.BuildTodayPulse();
        _pulseLabel.Text = pulse.ToDisplayText();
        var weekStory = _controller.BuildWeekStory();
        var weekMood = _controller.BuildWeekMood(weekStoryActive: weekStory.IsActive);
        if (weekStory.IsActive)
        {
            _weekStoryLabel.Visible = true;
            _weekStoryLabel.Text = weekStory.ToDisplayText();
        }
        else if (weekMood.IsActive)
        {
            _weekStoryLabel.Visible = true;
            _weekStoryLabel.Text = weekMood.ToDisplayText();
        }
        else
        {
            _weekStoryLabel.Visible = false;
            _weekStoryLabel.Text = string.Empty;
        }
        var recoveryPath = _controller.BuildInjuryRecoveryPath();
        _recoveryPathLabel.Visible = recoveryPath.IsActive;
        _recoveryPathLabel.Text = recoveryPath.IsActive ? recoveryPath.ToDisplayText() : string.Empty;

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
        _officeLabel.Text = Application.Competition.Queries.PostMatchOfficeDigest
            .FromTodayPulse(pulse, weekMood, weekStory, nextStep?.ButtonLabel, currentDay)
            .ToDisplayText();
        BindOfficeNextStep(nextStep);
    }

    private void RefreshSelectionStatus()
    {
        var currentDay = _controller.Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);

        var briefing = _controller.BuildNextMatchBriefing();
        _briefingLabel.Text = briefing.ToDisplayText();
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
                : "Kadro: onay gerekli — aşağıdaki butonlarla XI'yi kilitle.";
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
    }

    private void RefreshSquadList()
    {
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

        if (capacity.IsOverCapacity)
        {
            foreach (var id in capacity.OverflowPlayerIds)
            {
                _squadList.AddItem($"[kadro dışı sözleşme] oyuncu#{id}");
            }
        }
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
            row.SetCustomColor(9, CareerUiTheme.Ink);

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
