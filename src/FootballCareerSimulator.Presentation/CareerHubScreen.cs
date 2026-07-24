using FootballCareerSimulator.Domain.Competition;
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
    private Label _selectionLabel = null!;
    private Label _trainingLabel = null!;
    private Label _developmentLabel = null!;
    private Label _contractLabel = null!;
    private Label _memoryLabel = null!;
    private Label _promiseLabel = null!;
    private Label _relationshipLabel = null!;
    private Label _decisionLabel = null!;
    private Button _openDecisionButton = null!;
    private Button _openStartingDecisionButton = null!;
    private Button _openTransferDecisionButton = null!;
    private Button _openDisciplineDecisionButton = null!;
    private Button _openBoardDemandDecisionButton = null!;
    private Button _grantDecisionButton = null!;
    private Button _refuseDecisionButton = null!;
    private Button _disciplineWarningButton = null!;
    private Button _disciplineFineButton = null!;
    private Button _disciplineSupportButton = null!;
    private Button _boardCounterButton = null!;
    private Label _transferWindowLabel = null!;
    private Label _transferBudgetLabel = null!;
    private Button _openTransferWindowButton = null!;
    private Button _closeTransferWindowButton = null!;
    private Label _transferNeedLabel = null!;
    private Label _shortlistTargetLabel = null!;
    private Label _transferProcessLabel = null!;
    private Label _tacticLabel = null!;
    private Label _squadStatusLabel = null!;
    private Label _standingsLabel = null!;
    private Label _statusLabel = null!;
    private SpinBox _roundSelector = null!;
    private ItemList _fixtureList = null!;
    private ItemList _squadList = null!;
    private Button _approveSelectionButton = null!;
    private Button _generateOfferButton = null!;
    private Button _acceptOfferButton = null!;
    private Button _signFreeAgentButton = null!;
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
    private Button _formation442Button = null!;
    private Button _formation433Button = null!;
    private Button _formation352Button = null!;
    private Button _approachBalancedButton = null!;
    private Button _approachAttackingButton = null!;
    private Button _approachDefensiveButton = null!;
    private Button _playButton = null!;
    private Button _advanceDayButton = null!;
    private Button _advanceWeekButton = null!;
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

    public event Action<PlayMatchesUiResult>? MatchResultsReady;

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

    private void BuildLayout()
    {
        CareerUiTheme.EnsureLoaded();
        AddChild(CareerUiTheme.CreateAtmosphereBackground());

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.GrowHorizontal = GrowDirection.Both;
        margin.GrowVertical = GrowDirection.Both;
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var shell = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddThemeConstantOverride("separation", 14);
        margin.AddChild(shell);

        // —— Marka / üst şerit (sabit) ——
        var brand = new Label
        {
            Text = "Football Career Simulator",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        CareerUiTheme.StyleBrand(brand);
        shell.AddChild(brand);

        var brandLine = new ColorRect
        {
            Color = new Color(CareerUiTheme.Accent.R, CareerUiTheme.Accent.G, CareerUiTheme.Accent.B, 0.55f),
            CustomMinimumSize = new Vector2(120, 3),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        shell.AddChild(brandLine);

        var hubTitle = new Label { Text = "Kariyer Merkezi" };
        CareerUiTheme.StyleHeadline(hubTitle);
        shell.AddChild(hubTitle);

        _dateLabel = BodyLabel("DateLabel");
        shell.AddChild(_dateLabel);
        _managerLabel = BodyLabel("ManagerLabel", autowrap: true);
        shell.AddChild(_managerLabel);
        _seasonLabel = BodyLabel("SeasonLabel", muted: true);
        shell.AddChild(_seasonLabel);
        _progressLabel = BodyLabel("ProgressLabel", muted: true);
        shell.AddChild(_progressLabel);

        BuildNavBar(shell);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddChild(scroll);

        var pageHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        pageHost.AddThemeConstantOverride("separation", 0);
        scroll.AddChild(pageHost);

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
        shell.AddChild(_statusLabel);

        ShowPage(HubPage.Today);

        brandLine.CustomMinimumSize = new Vector2(24, 3);
        var brandTween = CreateTween();
        brandTween.TweenProperty(brandLine, "custom_minimum_size", new Vector2(160, 3), 0.55f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        shell.Modulate = new Color(1f, 1f, 1f, 0f);
        var fadeTween = CreateTween();
        fadeTween.TweenProperty(shell, "modulate:a", 1f, 0.4f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private void BuildNavBar(Control parent)
    {
        var nav = new HBoxContainer();
        nav.AddThemeConstantOverride("separation", 8);
        parent.AddChild(nav);

        var labels = new[] { "Bugün", "Kulüp", "Transfer", "Hazırlık", "Dünya", "Dosya" };
        _navButtons = new Button[labels.Length];
        for (var i = 0; i < labels.Length; i++)
        {
            var page = (HubPage)i;
            var button = new Button { Text = labels[i], ToggleMode = false };
            button.Pressed += () => ShowPage(page);
            nav.AddChild(button);
            _navButtons[i] = button;
        }
    }

    private void ShowPage(HubPage page)
    {
        _currentPage = page;
        for (var i = 0; i < _pages.Length; i++)
        {
            _pages[i].Visible = i == (int)page;
            CareerUiTheme.StyleNavButton(_navButtons[i], selected: i == (int)page);
        }
    }

    private VBoxContainer PageRoot()
    {
        var page = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        page.AddThemeConstantOverride("separation", 12);
        return page;
    }

    private Control BuildTodayPage()
    {
        var page = PageRoot();
        page.AddChild(SectionTitle("Bugün"));
        _blockerLabel = BodyLabel("BlockerLabel", autowrap: true);
        page.AddChild(_blockerLabel);
        _selectionLabel = BodyLabel("SelectionLabel", autowrap: true);
        page.AddChild(_selectionLabel);

        var primaryRow = new HBoxContainer();
        primaryRow.AddThemeConstantOverride("separation", 10);
        page.AddChild(primaryRow);

        _approveSelectionButton = PrimaryButton("Kadro Onayla");
        _approveSelectionButton.Pressed += () => Apply(_controller.ApproveDefaultSelectionForNextDueMatch());
        primaryRow.AddChild(_approveSelectionButton);

        _playButton = PrimaryButton("Bugünün Maçlarını Oyna");
        _playButton.Pressed += OnPlayMatches;
        primaryRow.AddChild(_playButton);

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
        page.AddChild(SectionTitle("Kulüp"));
        _squadStatusLabel = BodyLabel("SquadStatusLabel", autowrap: true);
        page.AddChild(_squadStatusLabel);
        _developmentLabel = BodyLabel("DevelopmentLabel", autowrap: true);
        page.AddChild(_developmentLabel);
        _contractLabel = BodyLabel("ContractLabel", autowrap: true);
        page.AddChild(_contractLabel);
        _memoryLabel = BodyLabel("MemoryLabel", autowrap: true);
        page.AddChild(_memoryLabel);
        _promiseLabel = BodyLabel("PromiseLabel", autowrap: true);
        page.AddChild(_promiseLabel);
        _relationshipLabel = BodyLabel("RelationshipLabel", autowrap: true);
        page.AddChild(_relationshipLabel);
        _decisionLabel = BodyLabel("DecisionLabel", autowrap: true);
        page.AddChild(_decisionLabel);

        _squadList = new ItemList
        {
            Name = "SquadList",
            CustomMinimumSize = new Vector2(0, 180),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_squadList);
        page.AddChild(_squadList);

        var jobRow = new HBoxContainer();
        jobRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(jobRow);

        _generateOfferButton = SecondaryButton("İş Teklifi Ara");
        _generateOfferButton.Pressed += () => Apply(_controller.GenerateJobOffer());
        jobRow.AddChild(_generateOfferButton);

        _acceptOfferButton = SecondaryButton("Teklifi Kabul Et");
        _acceptOfferButton.Pressed += () => Apply(_controller.AcceptJobOffer());
        jobRow.AddChild(_acceptOfferButton);

        _signFreeAgentButton = SecondaryButton("Serbesti Geri İmzala");
        _signFreeAgentButton.Pressed += () => Apply(_controller.SignNextFreeAgentToManagedClub());
        jobRow.AddChild(_signFreeAgentButton);

        _promiseStartButton = SecondaryButton("İlk 11 Sözü Ver");
        _promiseStartButton.Pressed += () => Apply(_controller.PromiseStartingOpportunityToOldestSquadPlayer());
        jobRow.AddChild(_promiseStartButton);

        _promisePlayingTimeButton = SecondaryButton("Oyun Süresi Sözü");
        _promisePlayingTimeButton.Pressed += () => Apply(_controller.PromisePlayingTimeToOldestSquadPlayer());
        jobRow.AddChild(_promisePlayingTimeButton);

        var decisionRow = new HBoxContainer();
        decisionRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(decisionRow);
        _openDecisionButton = SecondaryButton("Süre Talebi Aç");
        _openDecisionButton.Pressed += () => Apply(_controller.OpenPlayingTimeDecisionForOldestSquadPlayer());
        decisionRow.AddChild(_openDecisionButton);
        _openStartingDecisionButton = SecondaryButton("İlk 11 Talebi Aç");
        _openStartingDecisionButton.Pressed += () =>
            Apply(_controller.OpenStartingOpportunityDecisionForOldestSquadPlayer());
        decisionRow.AddChild(_openStartingDecisionButton);
        _openTransferDecisionButton = SecondaryButton("Transfer Talebi Aç");
        _openTransferDecisionButton.Pressed += () =>
            Apply(_controller.OpenTransferDecisionForOldestSquadPlayer());
        decisionRow.AddChild(_openTransferDecisionButton);
        _openDisciplineDecisionButton = SecondaryButton("Disiplin Aç");
        _openDisciplineDecisionButton.Pressed += () =>
            Apply(_controller.OpenDisciplineDecisionForOldestSquadPlayer());
        decisionRow.AddChild(_openDisciplineDecisionButton);
        _openBoardDemandDecisionButton = SecondaryButton("Yönetim Talebi Aç");
        _openBoardDemandDecisionButton.Pressed += () => Apply(_controller.OpenBoardDemandDecision());
        decisionRow.AddChild(_openBoardDemandDecisionButton);
        _grantDecisionButton = SecondaryButton("Talebi Kabul Et");
        _grantDecisionButton.Pressed += () => Apply(_controller.AnswerOldestPendingDecision(grantPromise: true));
        decisionRow.AddChild(_grantDecisionButton);
        _refuseDecisionButton = SecondaryButton("Talebi Reddet");
        _refuseDecisionButton.Pressed += () => Apply(_controller.AnswerOldestPendingDecision(grantPromise: false));
        decisionRow.AddChild(_refuseDecisionButton);

        var disciplineRow = new HBoxContainer();
        disciplineRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(disciplineRow);
        _disciplineWarningButton = SecondaryButton("Uyarı Ver");
        _disciplineWarningButton.Pressed += () =>
            Apply(_controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionIssueWarning));
        disciplineRow.AddChild(_disciplineWarningButton);
        _disciplineFineButton = SecondaryButton("Ceza Uygula");
        _disciplineFineButton.Pressed += () =>
            Apply(_controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionIssueFine));
        disciplineRow.AddChild(_disciplineFineButton);
        _disciplineSupportButton = SecondaryButton("Destekle");
        _disciplineSupportButton.Pressed += () =>
            Apply(_controller.AnswerOldestPendingWithOption(Domain.Interaction.DecisionRequest.OptionOfferSupport));
        disciplineRow.AddChild(_disciplineSupportButton);
        _boardCounterButton = SecondaryButton("Karşı Teklif");
        _boardCounterButton.Pressed += () =>
            Apply(_controller.AnswerOldestPendingWithOption(
                Domain.Interaction.DecisionRequest.OptionCounterBoardDemand));
        disciplineRow.AddChild(_boardCounterButton);
        return page;
    }

    private Control BuildTransferPage()
    {
        var page = PageRoot();
        page.AddChild(SectionTitle("Transfer"));
        _transferWindowLabel = BodyLabel("TransferWindowLabel", autowrap: true);
        page.AddChild(_transferWindowLabel);
        _transferBudgetLabel = BodyLabel("TransferBudgetLabel", autowrap: true);
        page.AddChild(_transferBudgetLabel);

        var windowRow = new HBoxContainer();
        windowRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(windowRow);

        _openTransferWindowButton = SecondaryButton("Pencere Aç");
        _openTransferWindowButton.Pressed += () => Apply(_controller.OpenTransferWindow());
        windowRow.AddChild(_openTransferWindowButton);

        _closeTransferWindowButton = SecondaryButton("Pencere Kapat");
        _closeTransferWindowButton.Pressed += () => Apply(_controller.CloseTransferWindow());
        windowRow.AddChild(_closeTransferWindowButton);

        _transferNeedLabel = BodyLabel("TransferNeedLabel", autowrap: true);
        page.AddChild(_transferNeedLabel);

        var needRow = new HBoxContainer();
        needRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(needRow);

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
        page.AddChild(_shortlistTargetLabel);

        var targetRow = new HBoxContainer();
        targetRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(targetRow);

        _suggestTargetButton = SecondaryButton("Hedef Öner");
        _suggestTargetButton.Pressed += () => Apply(_controller.SuggestTransferTarget());
        targetRow.AddChild(_suggestTargetButton);

        _dropTargetButton = SecondaryButton("Hedefi Düşür");
        _dropTargetButton.Pressed += () => Apply(_controller.DropOldestListedTransferTarget());
        targetRow.AddChild(_dropTargetButton);

        _transferProcessLabel = BodyLabel("TransferProcessLabel", autowrap: true);
        page.AddChild(_transferProcessLabel);

        var processRow = new HBoxContainer();
        processRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(processRow);

        _openProcessButton = SecondaryButton("Süreç Aç");
        _openProcessButton.Pressed += () => Apply(_controller.OpenTransferProcessFromOldestTarget());
        processRow.AddChild(_openProcessButton);

        _withdrawProcessButton = SecondaryButton("Süreci Geri Çek");
        _withdrawProcessButton.Pressed += () => Apply(_controller.WithdrawOldestActiveTransferProcess());
        processRow.AddChild(_withdrawProcessButton);

        var sportingRow = new HBoxContainer();
        sportingRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(sportingRow);

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

        _clubOfferLabel = BodyLabel("ClubOfferLabel", autowrap: true);
        page.AddChild(_clubOfferLabel);

        var offerRow = new HBoxContainer();
        offerRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(offerRow);

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

        _contractProposalLabel = BodyLabel("ContractProposalLabel", autowrap: true);
        page.AddChild(_contractProposalLabel);

        var proposalRow = new HBoxContainer();
        proposalRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(proposalRow);

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

        var financialRow = new HBoxContainer();
        financialRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(financialRow);

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
        page.AddChild(SectionTitle("Hazırlık"));
        _trainingLabel = BodyLabel("TrainingLabel", autowrap: true);
        page.AddChild(_trainingLabel);

        var trainingRow = new HBoxContainer();
        trainingRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(trainingRow);

        _trainLowButton = SecondaryButton("Hafif");
        _trainLowButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.Low));
        trainingRow.AddChild(_trainLowButton);

        _trainMediumButton = SecondaryButton("Orta");
        _trainMediumButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.Medium));
        trainingRow.AddChild(_trainMediumButton);

        _trainHighButton = SecondaryButton("Yoğun");
        _trainHighButton.Pressed += () => Apply(_controller.SetWeeklyTraining(TrainingIntensity.High));
        trainingRow.AddChild(_trainHighButton);

        _tacticLabel = BodyLabel("TacticLabel", autowrap: true);
        page.AddChild(_tacticLabel);

        var formationRow = new HBoxContainer();
        formationRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(formationRow);

        _formation442Button = SecondaryButton("4-4-2");
        _formation442Button.Pressed += () => Apply(_controller.SetTacticFormation(Formation.F442));
        formationRow.AddChild(_formation442Button);

        _formation433Button = SecondaryButton("4-3-3");
        _formation433Button.Pressed += () => Apply(_controller.SetTacticFormation(Formation.F433));
        formationRow.AddChild(_formation433Button);

        _formation352Button = SecondaryButton("3-5-2");
        _formation352Button.Pressed += () => Apply(_controller.SetTacticFormation(Formation.F352));
        formationRow.AddChild(_formation352Button);

        var approachRow = new HBoxContainer();
        approachRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(approachRow);

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
        page.AddChild(SectionTitle("Dünya"));
        _standingsLabel = BodyLabel("StandingsLabel", autowrap: true);
        page.AddChild(_standingsLabel);

        var roundRow = new HBoxContainer();
        roundRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(roundRow);
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
        page.AddChild(_fixtureList);

        var advancedToggle = SecondaryButton("Gelişmiş sezon araçları");
        page.AddChild(advancedToggle);

        var advanced = new VBoxContainer { Visible = false };
        advanced.AddThemeConstantOverride("separation", 8);
        page.AddChild(advanced);
        advancedToggle.Pressed += () =>
        {
            advanced.Visible = !advanced.Visible;
            advancedToggle.Text = advanced.Visible
                ? "Gelişmiş sezon araçlarını gizle"
                : "Gelişmiş sezon araçları";
        };

        var seasonRow = new HBoxContainer();
        seasonRow.AddThemeConstantOverride("separation", 8);
        advanced.AddChild(seasonRow);
        AddActionButton(seasonRow, "Ligi Kur / Tamamla", () => Apply(_controller.EnsureLeagueReady()));
        AddActionButton(seasonRow, "Sezonu Kapat", () => Apply(_controller.CompleteSeason()));
        AddActionButton(seasonRow, "Sezonu Arşivle", () => Apply(_controller.ArchiveSeason()));
        AddActionButton(seasonRow, "Yeni Sezon", () => Apply(_controller.StartNewSeason()));

        var planningRow = new HBoxContainer();
        planningRow.AddThemeConstantOverride("separation", 8);
        advanced.AddChild(planningRow);
        AddActionButton(planningRow, "Planlama Dönemi Aç", () => Apply(_controller.OpenPlanningPeriod()));
        AddActionButton(planningRow, "Planlama Dönemini Bitir", () => Apply(_controller.CompletePlanningPeriod()));
        return page;
    }

    private Control BuildFilePage()
    {
        var page = PageRoot();
        page.AddChild(SectionTitle("Dosya"));
        var saveLoadRow = new HBoxContainer();
        saveLoadRow.AddThemeConstantOverride("separation", 8);
        page.AddChild(saveLoadRow);

        var saveButton = SecondaryButton("Kaydet");
        saveButton.Pressed += () => Apply(_controller.SaveGame());
        saveLoadRow.AddChild(saveButton);

        var loadButton = SecondaryButton("Yükle");
        loadButton.Pressed += () => Apply(_controller.LoadGame());
        saveLoadRow.AddChild(loadButton);

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

    private static void AddActionButton(HBoxContainer row, string text, Action action)
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
        var results = _controller.PlayDueMatches();
        RefreshUi();

        if (results.Succeeded && results.MatchLines.Count > 0)
        {
            MatchResultsReady?.Invoke(results);
            return;
        }

        PulseStatus(results.Message);
    }

    private void Apply(UiActionResult result)
    {
        PulseStatus(result.Message);
        RefreshUi();
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

        _dateLabel.Text = $"Tarih: {current.IsoDate} (gün {current.DayNumber})";

        if (string.Equals(manager.EmploymentStatus, "Unemployed", StringComparison.Ordinal))
        {
            var lastClub = manager.LastClubId is long lastClubId
                ? _controller.GetClubDisplayName(lastClubId)
                : "—";
            var offerText = manager.PendingOfferClubId is long offerClubId
                ? $" · Teklif: {GetClubDisplayNameSafe(offerClubId)} (#{manager.PendingOfferId})"
                : " · Teklif: yok — 'İş Teklifi Ara'";
            _managerLabel.Text =
                $"Menajer: {manager.DisplayName} · İŞSİZ (kovuldu: {lastClub}){offerText}";
        }
        else
        {
            var clubName = manager.EmployedClubId is long clubId
                ? _controller.GetClubDisplayName(clubId)
                : "—";
            var boardText = manager.BoardConfidence is int confidence
                ? $" · Yönetim: {confidence} ({TranslateRisk(manager.EmploymentRiskBand)}) · Beklenti: {TranslateExpectation(manager.SeasonExpectation)}"
                : string.Empty;
            var reasonText = string.IsNullOrWhiteSpace(manager.LastAssessmentReasonCode)
                ? string.Empty
                : $" · Son değerlendirme: {TranslateReason(manager.LastAssessmentReasonCode)}";
            _managerLabel.Text =
                $"Menajer: {manager.DisplayName} · Kulüp: {clubName}{boardText}{reasonText}";
        }

        var periodText = period is null
            ? "Planlama dönemi: yok"
            : $"Planlama dönemi: #{period.PlanningPeriodId} ({period.Status})";

        if (season is null)
        {
            _seasonLabel.Text = "Lig sezonu: yok — 'Ligi Kur' ile başla.";
            _progressLabel.Text = periodText;
            _standingsLabel.Text = "Puan durumu: —";
            _fixtureList.Clear();
            _blockerLabel.Text = _controller.FormatActiveBlockerSummary();
            RefreshSelectionStatus();
            RefreshTrainingStatus();
            RefreshDevelopmentStatus();
            RefreshContractStatus();
            RefreshMemoryStatus();
            RefreshPromiseStatus();
            RefreshRelationshipStatus();
            RefreshDecisionStatus();
            RefreshTransferWindowStatus();
            RefreshTransferBudgetStatus();
            RefreshTransferNeedStatus();
            RefreshShortlistTargetStatus();
            RefreshTransferProcessStatus();
            RefreshClubOfferStatus();
            RefreshContractProposalStatus();
            RefreshTacticStatus();
            RefreshSquadList();
            UpdatePrimaryHints(dueMatchCount: 0, canAdvance: world.Queries.GetTimeAdvanceEligibility().CanAdvance);
            UpdateJobOfferButtons(manager);
            UpdateTransferNeedButtons(manager);
            UpdateTrainingButtons(manager);
            UpdateTacticButtons(manager);
            return;
        }

        _seasonLabel.Text =
            $"Sezon #{season.SeasonId} ({season.Status}) — {season.ParticipantCount} takım, {season.FixtureCount} maç";

        var progress = competition.Queries.GetSeasonProgress(season.SeasonId);
        var progressText = progress is null
            ? "İlerleme: —"
            : $"İlerleme: {progress.AcceptedFixtureCount}/{progress.TotalFixtureCount} maç"
              + (progress.CanComplete ? " · kapatılabilir" : string.Empty)
              + (progress.CanArchive ? " · arşivlenebilir" : string.Empty);
        _progressLabel.Text = $"{progressText} · {periodText}";

        _blockerLabel.Text = _controller.FormatActiveBlockerSummary();
        RefreshSelectionStatus();
        RefreshTrainingStatus();
        RefreshDevelopmentStatus();
        RefreshContractStatus();
        RefreshMemoryStatus();
        RefreshPromiseStatus();
        RefreshRelationshipStatus();
        RefreshDecisionStatus();
        RefreshTransferWindowStatus();
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
        _suggestTargetButton.Disabled = !employed;
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
            return;
        }

        if (!training.HasPlan)
        {
            var injuryHint = training.InjuredSlotCount > 0
                ? $" · sakat {training.InjuredSlotCount} (uygun değil {training.UnavailableSlotCount})"
                : string.Empty;
            _trainingLabel.Text =
                $"Antrenman: plan yok — hafif/orta/yoğun uygula (maç gücünü etkiler){injuryHint}.";
            return;
        }

        var injuryText = training.InjuredSlotCount > 0
            ? $" · sakat {training.InjuredSlotCount} (uygun değil {training.UnavailableSlotCount})"
            : string.Empty;
        _trainingLabel.Text =
            $"Antrenman: {training.IntensityName}/{training.FocusName} · Dinlenme {training.RestApproachName}"
            + $" · XI yorgunluk {training.AverageFatigue} · fitness {training.AverageFitness}{injuryText}";
    }

    private void UpdateTrainingButtons(
        Application.ManagerCareer.Queries.ManagerCareerReadModel manager)
    {
        var employed = string.Equals(manager.EmploymentStatus, "Employed", StringComparison.Ordinal);
        _trainLowButton.Disabled = !employed;
        _trainMediumButton.Disabled = !employed;
        _trainHighButton.Disabled = !employed;
    }

    private void RefreshTacticStatus()
    {
        var tactic = _controller.Host.TeamPreparationModule.TacticQueries.GetManagedClubPlan();
        if (tactic.ClubId is null)
        {
            _tacticLabel.Text = "Taktik: işsiz — plan yok.";
            return;
        }

        if (string.Equals(tactic.FormationName, "yok", StringComparison.Ordinal))
        {
            _tacticLabel.Text = "Taktik: henüz yok — lig kur / formasyon seç.";
            return;
        }

        _tacticLabel.Text =
            $"Taktik: {tactic.FormationName} · {tactic.ApproachName}"
            + $" (maç gücünü etkiler)";
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
        if (pending.OpenCount == 0)
        {
            _decisionLabel.Text = "Kararlar: bekleyen zorunlu karar yok.";
            _grantDecisionButton.Disabled = true;
            _refuseDecisionButton.Disabled = true;
            _disciplineWarningButton.Disabled = true;
            _disciplineFineButton.Disabled = true;
            _disciplineSupportButton.Disabled = true;
            _boardCounterButton.Disabled = true;
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
                or Domain.Interaction.DecisionRequest.OptionCounterBoardDemand));
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

        var optionPreview = string.Join(
            " | ",
            options.Options.Select(o =>
                $"{o.DisplayText}{(o.IsEligible ? string.Empty : " [kapalı]")}"));
        var preview = string.Join(
            " · ",
            pending.OpenRequests.Select(d =>
                $"{d.KindName} oyuncu#{d.SubjectPlayerId} son:{d.DeadlineDayNumber}"));
        _decisionLabel.Text =
            $"Kararlar: {pending.OpenCount} açık — {preview}"
            + (string.IsNullOrWhiteSpace(optionPreview) ? string.Empty : $" · seçenekler: {optionPreview}");

        var awaiting = _controller.Host.InteractionModule.DialogueSessionStore.Sessions
            .Count(s => s.IsAwaitingPlayer);
        if (awaiting > 0)
        {
            _decisionLabel.Text += $" · diyalog:{awaiting}";
        }
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
    }

    private string GetClubDisplayNameSafe(long clubId) => _controller.GetClubDisplayName(clubId);

    private void RefreshSelectionStatus()
    {
        var currentDay = _controller.Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);

        if (pending is null)
        {
            _selectionLabel.Text = "Kadro onayı: vadesi gelmiş kendi maçın yok.";
            _approveSelectionButton.Disabled = true;
            return;
        }

        var opponent = _controller.GetClubDisplayName(pending.OpponentClubId);
        var venue = pending.IsHome ? "Ev" : "Dep";
        _selectionLabel.Text = pending.IsApproved
            ? $"Kadro onayı: hazır · fikstür #{pending.FixtureId} ({venue} vs {opponent})"
            : $"Kadro onayı: gerekli · fikstür #{pending.FixtureId} ({venue} vs {opponent}, {pending.ScheduledIsoDate})";
        _approveSelectionButton.Disabled = pending.IsApproved;
    }

    private void UpdatePrimaryHints(int dueMatchCount, bool canAdvance)
    {
        var currentDay = _controller.Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        var selectionBlocksPlay = pending is not null && !pending.IsApproved;

        _playButton.Disabled = dueMatchCount == 0 || selectionBlocksPlay;
        _playButton.Text = dueMatchCount == 0
            ? "Bugünün Maçlarını Oyna"
            : selectionBlocksPlay
                ? "Bugünün Maçlarını Oyna (önce kadro)"
                : $"Bugünün Maçlarını Oyna ({dueMatchCount})";

        _advanceDayButton.Disabled = !canAdvance;
        _advanceWeekButton.Disabled = !canAdvance;
    }

    private void RefreshSquadList()
    {
        _squadList.Clear();
        var manager = _controller.Host.ManagerModule.Queries.GetCareer();
        if (manager.EmployedClubId is not long clubId)
        {
            _squadStatusLabel.Text = "A takım: işsiz — kayıt yok.";
            return;
        }

        var rootSeed = _controller.Host.WorldModule.TimelineStore.Timeline.RootSeed;
        var squad = _controller.Host.TeamPreparationModule.SquadQueries.GetClubSquad(clubId, rootSeed);
        var persisted = _controller.Host.TeamPreparationModule.SquadStore.Get(
            new Domain.Shared.ClubId(clubId));
        var clubName = _controller.GetClubDisplayName(clubId);
        _squadStatusLabel.Text = persisted is null || persisted.Members.Count == 0
            ? "A takım: henüz yok — lig kur / gün ilerle / antrenman ile oluşur."
            : $"A takım: {persisted.Members.Count} üye · {clubName}";

        foreach (var player in squad.Take(11))
        {
            _squadList.AddItem($"{player.SquadNumber}. {player.DisplayName} ({player.Rating})");
        }

        if (squad.Count > 11)
        {
            _squadList.AddItem($"... +{squad.Count - 11} yedek/oyuncu ({clubName})");
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
        var season = _controller.Host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null || season.FixtureCount == 0)
        {
            _standingsLabel.Text = "Puan durumu: —";
            return;
        }

        var standings = _controller.Host.CompetitionModule.Queries.GetStandings(season.SeasonId);
        if (standings.Count == 0)
        {
            _standingsLabel.Text = "Puan durumu: henüz maç yok";
            return;
        }

        var preview = string.Join(
            " | ",
            standings.Take(5).Select((entry, index) =>
                $"{index + 1}. {_controller.GetClubDisplayName(entry.ClubId)} {entry.Points}p ({entry.Played}M)"));

        _standingsLabel.Text = $"Puan durumu (ilk 5): {preview}";
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
