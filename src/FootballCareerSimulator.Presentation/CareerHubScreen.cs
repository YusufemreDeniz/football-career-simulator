using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Application.PlayerCareer.Queries;
using FootballCareerSimulator.Simulation.DataPacks;
using FootballCareerSimulator.Simulation.Continental;
using Godot;
using static FootballCareerSimulator.Presentation.CareerHubCopyFormatting;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen : Control
{
    private readonly CareerSessionController _controller;
    private readonly ProceduralMatchAudioDirector? _audioDirector;

    private Label _dateLabel = null!;
    private Label _managerLabel = null!;
    private Label _seasonLabel = null!;
    private Label _progressLabel = null!;
    private Label _blockerLabel = null!;
    private Label _pulseLabel = null!;
    private PanelContainer _fanApprovalBadgePanel = null!;
    private Label _fanApprovalBadgeLabel = null!;
    private Label _fanFeedLabel = null!;
    private Label _selectionLabel = null!;
    private Label _trainingLabel = null!;
    private Label _matchTrainingPriorityLabel = null!;
    private readonly Dictionary<MatchTrainingPriority, Button> _matchTrainingPriorityButtons = [];
    private Label _prepBriefingLabel = null!;
    private Label _prepOpponentLabel = null!;
    private Label _prepMatchupLabel = null!;
    private Control _prepPitchHost = null!;
    private Label _prepPitchCaptionLabel = null!;
    private Button _prepApproveSelectionButton = null!;
    private Button _prepSwapSelectionButton = null!;
    private HBoxContainer _prepReadinessBar = null!;
    private PanelContainer _prepFitnessBadgePanel = null!;
    private Label _prepFitnessBadgeLabel = null!;
    private PanelContainer _prepSharpnessBadgePanel = null!;
    private Label _prepSharpnessBadgeLabel = null!;
    private PanelContainer _prepRiskBadgePanel = null!;
    private Label _prepRiskBadgeLabel = null!;
    private PanelContainer _prepApprovalBadgePanel = null!;
    private Label _prepApprovalBadgeLabel = null!;
    private PanelContainer _prepOpponentThreatBadgePanel = null!;
    private Label _prepOpponentThreatBadgeLabel = null!;
    private PanelContainer _prepMatchupSignalBadgePanel = null!;
    private Label _prepMatchupSignalBadgeLabel = null!;
    private Button _scenarioShockPressBtn = null!;
    private Button _scenarioParkTheBusBtn = null!;
    private Button _scenarioFastCounterBtn = null!;
    private Button _scenarioPatientControlBtn = null!;
    private readonly Dictionary<MatchTrainingPriority, PanelContainer> _matchPriorityCardPanels = [];
    private readonly Dictionary<MatchTrainingPriority, Label> _matchPriorityBonusLabels = [];
    private readonly Dictionary<MatchTrainingPriority, Label> _matchPriorityCostLabels = [];
    private readonly Dictionary<MatchTrainingPriority, PanelContainer> _matchPriorityRecBadges = [];
    private int? _prepSelectedSquadSlotIndex;
    private bool? _prepSelectedSquadPlayerIsStarter;
    private int? _clubPlanSelectedSlotIndex;
    private bool? _clubPlanSelectedIsStarter;
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
    private Label _nextMatchOpponentLabel = null!;
    private Label _nextMatchVenueLabel = null!;
    private PanelContainer _nextMatchVenuePill = null!;
    private Label _nextMatchCompetitionBadge = null!;
    private Label _nextMatchApprovalLabel = null!;
    private PanelContainer _nextMatchApprovalPill = null!;
    private VBoxContainer _miniStandingsContainer = null!;
    private Button _openFullStandingsButton = null!;
    private Label _todayTacticsSummaryLabel = null!;
    private Label _todayFitnessSummaryLabel = null!;
    private Button _openTacticsButton = null!;
    private Button _restEntireTeamButton = null!;
    private Button _lightRegenerationButton = null!;
    private Label _decisionLabel = null!;
    private PanelContainer _decisionCausalityPanel = null!;
    private Label _decisionCausalityLabel = null!;
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
    private PanelContainer _transferWelcomePanel = null!;
    private Label _transferWelcomeHeadlineLabel = null!;
    private Label _transferWelcomeMetaLabel = null!;
    private long? _newTransferPlayerId;
    private Label _transferScoutReportLabel = null!;
    private GridContainer _transferBentoGrid = null!;
    private PanelContainer _transferBudgetBadgePanel = null!;
    private Label _transferBudgetBadgeLabel = null!;
    private PanelContainer _wageBudgetBadgePanel = null!;
    private Label _wageBudgetBadgeLabel = null!;
    private PanelContainer _transferWindowBadgePanel = null!;
    private Label _transferWindowBadgeLabel = null!;
    private Button _openDossierFromScoutButton = null!;
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
    private TransferMarketScope _scoutMarketScope = TransferMarketScope.All;
    private Button _scopeAllButton = null!;
    private Button _scopeDomesticButton = null!;
    private Button _scopeEuroButton = null!;
    private Button _scopeFreeButton = null!;
    private ItemList _scoutCandidateList = null!;
    private Label _scoutCandidateDetailLabel = null!;
    private PanelContainer _scoutCandidateSummaryPanel = null!;
    private Label _scoutMarketValueBadgeLabel = null!;
    private Label _scoutWageExpectationBadgeLabel = null!;
    private Label _scoutTransferChanceBadgeLabel = null!;
    private IReadOnlyList<ScoutCandidateLine> _scoutCandidates = Array.Empty<ScoutCandidateLine>();
    private long? _selectedScoutPlayerId;
    private Label _shortlistTargetLabel = null!;
    private Label _transferProcessLabel = null!;
    private Label _outgoingSaleLabel = null!;
    private Button _advanceDealButton = null!;
    private Label _tacticLabel = null!;
    private Label _dualPhaseTacticLabel = null!;
    private Label _squadStatusLabel = null!;
    private Label _squadCapacityLabel = null!;
    private HBoxContainer _squadTelemetryRow = null!;
    private PanelContainer _squadOvrBadgePanel = null!;
    private Label _squadOvrBadgeLabel = null!;
    private PanelContainer _squadCapacityBadgePanel = null!;
    private Label _squadCapacityBadgeLabel = null!;
    private PanelContainer _squadAvgAgeBadgePanel = null!;
    private Label _squadAvgAgeBadgeLabel = null!;
    private PanelContainer _squadTotalValueBadgePanel = null!;
    private Label _squadTotalValueBadgeLabel = null!;
    private PanelContainer _squadWageBillBadgePanel = null!;
    private Label _squadWageBillBadgeLabel = null!;
    private HBoxContainer _starSpotlightDesk = null!;
    private Label _starSpotlightHeadline = null!;
    private readonly PanelContainer[] _starCards = new PanelContainer[3];
    private readonly Label[] _starPosBadges = new Label[3];
    private readonly Label[] _starRatingBadges = new Label[3];
    private readonly Label[] _starNameLabels = new Label[3];
    private readonly Label[] _starMetaLabels = new Label[3];
    private readonly Button[] _starInspectButtons = new Button[3];
    private readonly long?[] _starPlayerIds = new long?[3];
    private HBoxContainer _swotPanel = null!;
    private VBoxContainer _swotStrengthsVBox = null!;
    private VBoxContainer _swotWeaknessesVBox = null!;
    private Button _swotRestFatiguedButton = null!;
    private Button _swotViewAllSquadButton = null!;
    private HFlowContainer _squadFilterBar = null!;
    private Button _squadFilterAllBtn = null!;
    private Button _squadFilterGkBtn = null!;
    private Button _squadFilterDefBtn = null!;
    private Button _squadFilterMidBtn = null!;
    private Button _squadFilterFwdBtn = null!;
    private Button _squadFilterYouthBtn = null!;
    private Button _squadFilterRiskBtn = null!;
    private string _activeSquadCategoryFilter = "ALL";
    private Button _openSelectedPlayerDossierButton = null!;
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
    private ItemList _compactStandingsList = null!;
    private readonly List<long?> _compactStandingsClubIds = [];
    private Label _leagueBriefingLabel = null!;
    private Label _leagueStatisticsLabel = null!;
    private Label _managedLeagueStatisticsLabel = null!;
    private PanelContainer _leagueRoundBadgePanel = null!;
    private Label _leagueRoundBadgeLabel = null!;
    private PanelContainer _leagueMatchesBadgePanel = null!;
    private Label _leagueMatchesBadgeLabel = null!;
    private HBoxContainer _statLeadersDesk = null!;
    private Label _attackLeaderClubLabel = null!;
    private Label _attackLeaderStatLabel = null!;
    private Label _defenseLeaderClubLabel = null!;
    private Label _defenseLeaderStatLabel = null!;
    private Label _formLeaderClubLabel = null!;
    private Label _formLeaderStatLabel = null!;
    private bool _showingContinentalLeague;
    private Button _leagueCompButton = null!;
    private Button _euroCompButton = null!;
    private IReadOnlyList<ContinentalFixture> _currentContinentalFixtures = [];
    private long? _selectedWorldClubId;
    private IReadOnlyList<FixtureReadModel> _currentRoundFixtures = [];
    private Control _worldClubDossierCard = null!;
    private Label _worldClubHeadlineLabel = null!;
    private PanelContainer _worldClubRatingBadgePanel = null!;
    private Label _worldClubRatingBadgeLabel = null!;
    private Label _worldClubBadgeLabel = null!;
    private PanelContainer _worldClubThreatBadgePanel = null!;
    private Label _worldClubThreatBadgeLabel = null!;
    private Label _worldClubPerformanceLabel = null!;
    private Label _worldClubThreatLabel = null!;
    private Label _worldClubHeadToHeadLabel = null!;
    private Button _worldClubPrepButton = null!;
    private Button _worldClubTransferButton = null!;
    private Button _worldClubFixtureButton = null!;
    private Button _fixturePrevRoundButton = null!;
    private Button _fixtureNextRoundButton = null!;
    private PanelContainer _worldFixtureDetailCard = null!;
    private Label _worldFixtureHeadlineLabel = null!;
    private Label _worldFixtureDetailLabel = null!;
    private Button _worldFixturePrepButton = null!;
    private Button _worldFixtureHomeInspectButton = null!;
    private Button _worldFixtureAwayInspectButton = null!;
    private long? _selectedFixtureHomeClubId;
    private long? _selectedFixtureAwayClubId;
    private Label _statusLabel = null!;
    private Label _saveDeskLabel = null!;
    private Label _saveLastWriteBadgeLabel = null!;
    private Label _saveActiveSlotBadgeLabel = null!;
    private Label _saveWorldSeedBadgeLabel = null!;
    private Label _saveFileSizeBadgeLabel = null!;
    private GridContainer _govTelemetryBar = null!;
    private GridContainer _fileTopGrid = null!;
    private GridContainer _fileFinanceGrid = null!;
    private GridContainer _fileBottomGrid = null!;
    private GridContainer _fileLegacyStatsGrid = null!;
    private PanelContainer _boardConfidenceBadgePanel = null!;
    private Label _boardConfidenceBadgeLabel = null!;
    private PanelContainer _operatingBalanceBadgePanel = null!;
    private Label _operatingBalanceBadgeLabel = null!;
    private PanelContainer _stadiumAttendanceBadgePanel = null!;
    private Label _stadiumAttendanceBadgeLabel = null!;
    private PanelContainer _wageUtilizationBadgePanel = null!;
    private Label _wageUtilizationBadgeLabel = null!;
    private VBoxContainer _boardObjectivesContainer = null!;
    private Label _financeProjectionLabel = null!;
    private Label _realizedFinanceLabel = null!;
    private Label _boardOutcomeSummaryLabel = null!;
    private PanelContainer _boardFinanceStatusBadgePanel = null!;
    private Label _boardFinanceStatusBadgeLabel = null!;
    private PanelContainer _legacyReputationBadgePanel = null!;
    private Label _legacyReputationBadgeLabel = null!;
    private Label _legacyTenureLabel = null!;
    private Label _legacyStatMatchesLabel = null!;
    private Label _legacyStatWinRateLabel = null!;
    private Label _legacyStatYouthLabel = null!;
    private Label _legacyStatMilestoneLabel = null!;
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
    private VBoxContainer _clubOverviewPanel = null!;
    private VBoxContainer _clubAllPlayersPanel = null!;
    private VBoxContainer _clubPlanPanel = null!;
    private GridContainer _squadDepthGrid = null!;
    private VBoxContainer _clubPlanPitchHost = null!;
    private Label _clubOverviewHeadlineLabel = null!;
    private Label _clubDepthLabel = null!;
    private Label _clubPitchCaptionLabel = null!;
    private Label _clubPlanCaptionLabel = null!;
    private HBoxContainer _clubSignalRow = null!;
    private Button _clubTabOverviewButton = null!;
    private Button _clubTabAllButton = null!;
    private Button _clubTabPlanButton = null!;
    private Button _clubDeclareNeedButton = null!;
    private string? _squadListFilterCode;
    private enum ClubSubPage
    {
        Overview = 0,
        AllPlayers = 1,
        Plan = 2,
    }

    private ClubSubPage _clubSubPage = ClubSubPage.Overview;
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
    private SpinBox _transferFeeInput = null!;
    private Button _submitClubOfferButton = null!;
    private Button _acceptClubOfferButton = null!;
    private Button _rejectClubOfferButton = null!;
    private Button _counterClubOfferButton = null!;
    private Label _contractProposalLabel = null!;
    private SpinBox _transferWageInput = null!;
    private SpinBox _contractYearsInput = null!;
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
    private Button _formationPickerButton = null!;
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
    private Button _topContinueButton = null!;
    private PanelContainer _nextMatchPill = null!;
    private Label _nextMatchLabel = null!;
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
    private bool _compactLayout;
    private readonly Dictionary<HubPage, MobileFocusLayout> _mobileFocusLayouts = [];
    private readonly Dictionary<HubPage, int> _mobileFocusIndices = [];

    private sealed record MobileFocusSection(Button Button, Control[] Targets);

    private sealed record MobileFocusLayout(
        HBoxContainer Bar,
        MobileFocusSection[] Sections,
        Control[] CompactHiddenTargets);

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

    public CareerHubScreen(
        CareerSessionController controller,
        ProceduralMatchAudioDirector? audioDirector = null)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _audioDirector = audioDirector;
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
        if (what == NotificationWMGoBackRequest)
        {
            HandleHardwareBackRequest();
            return;
        }

        if (what == NotificationResized && _layoutBuilt)
        {
            ApplyResponsiveLayout();
        }
    }

    private void HandleHardwareBackRequest()
    {
        if (TryCloseFormationPicker())
        {
            return;
        }

        if (_playerDossierOverlay is { Visible: true })
        {
            ClosePlayerDossier();
            return;
        }

        if (_currentPage == HubPage.Club && _clubSubPage != ClubSubPage.Overview)
        {
            ShowClubSubPage(ClubSubPage.Overview);
            return;
        }

        if (_compactLayout
            && _mobileFocusIndices.GetValueOrDefault(_currentPage) > 0)
        {
            SelectMobileFocusSection(_currentPage, 0);
            return;
        }

        if (_currentPage != HubPage.Today)
        {
            ShowPage(HubPage.Today);
            return;
        }

        Callable.From(() => BackToMenuRequested?.Invoke()).CallDeferred();
    }

    private bool TryCloseFormationPicker()
    {
        foreach (var child in GetChildren())
        {
            if (child is FormationPickerModal modal)
            {
                modal.QueueFree();
                return true;
            }
        }

        return false;
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

    public void ShowPrepPage() => ShowPage(HubPage.Prep);

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
            if (_topContinueButton is not null)
            {
                _topContinueButton.Visible = false;
            }
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

        if (_topContinueButton is not null)
        {
            _topContinueButton.Text = "▶ " + step.ButtonLabel;
            _topContinueButton.Visible = _officeNextStepButton.Visible;
            _topContinueButton.TooltipText = step.ButtonLabel;
        }

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
                if (_officeNextStepTarget == HubPage.Today && _currentPage == HubPage.Today)
                {
                    if (_controller.HasDueContinentalMatch()
                        || _controller.GetNextDueManagedFixture(_controller.GetCurrentGameDate().DayNumber) is not null)
                    {
                        OnPlayMatches();
                    }
                    else if (_controller.CanAdvanceTime())
                    {
                        Apply(_controller.AdvanceToNextMeaningfulPoint());
                    }
                    return;
                }
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

        // Sabit kariyer kabuğu: broadcast tarzı top bar paneli + kulüp/menajer kimliği + sonraki maç + devam CTA'sı.
        var topBarPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        topBarPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.TopBarPanel());
        shell.AddChild(topBarPanel);

        _topBar = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        _topBar.AddThemeConstantOverride("separation", CareerUiTheme.SpaceM);
        topBarPanel.AddChild(_topBar);

        _clubCrest = new TextureRect
        {
            Name = "ClubCrest",
            CustomMinimumSize = new Vector2(48, 48),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = "Kulüp arması",
            Visible = false,
        };
        _topBar.AddChild(_clubCrest);

        var brandLockup = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        brandLockup.AddThemeConstantOverride("separation", 1);
        _topBar.AddChild(brandLockup);

        _brandLabel = new Label { Text = "FCS  /  MENAJER OFİSİ" };
        CareerUiTheme.StyleEyebrow(_brandLabel, CareerUiTheme.Accent);
        _brandLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        brandLockup.AddChild(_brandLabel);

        _managerLabel = BodyLabel("ManagerLabel", autowrap: true);
        CareerUiTheme.StyleHeadline(_managerLabel);
        _managerLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(20));
        brandLockup.AddChild(_managerLabel);

        _seasonLabel = BodyLabel("SeasonLabel", muted: true, autowrap: true);
        _seasonLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        brandLockup.AddChild(_seasonLabel);

        // Center: Sonraki Maç / Fikstür Teaser
        _nextMatchPill = new PanelContainer
        {
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            Visible = false,
        };
        _nextMatchPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePanel(CareerUiTheme.Data));
        _topBar.AddChild(_nextMatchPill);
        _nextMatchLabel = new Label
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        CareerUiTheme.StyleTableText(_nextMatchLabel);
        _nextMatchLabel.AddThemeColorOverride("font_color", CareerUiTheme.TextPrimary);
        _nextMatchPill.AddChild(_nextMatchLabel);

        // Right: Tarih + Ana Loop Continue CTA + Dosya
        var topBarRight = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            Alignment = BoxContainer.AlignmentMode.End,
        };
        topBarRight.AddThemeConstantOverride("separation", CareerUiTheme.SpaceS);
        _topBar.AddChild(topBarRight);

        _datePanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _datePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.PillPanel());
        topBarRight.AddChild(_datePanel);
        _dateLabel = BodyLabel("DateLabel");
        _dateLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _dateLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        _datePanel.AddChild(_dateLabel);

        _topContinueButton = new Button
        {
            Text = "▶ DEVAM",
            CustomMinimumSize = new Vector2(140, 42),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Visible = false,
        };
        CareerUiTheme.StyleAccentButton(_topContinueButton);
        _topContinueButton.Pressed += OnOfficeNextStepPressed;
        topBarRight.AddChild(_topContinueButton);

        _careerButton = SecondaryButton("DOSYA");
        _careerButton.CustomMinimumSize = new Vector2(58, 42);
        _careerButton.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
        _careerButton.TooltipText = "Kariyer dosyası, kayıt ve ana menü";
        _careerButton.Pressed += () => ShowPage(HubPage.File);
        topBarRight.AddChild(_careerButton);

        var careerMeta = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        careerMeta.AddThemeConstantOverride("separation", 2);
        shell.AddChild(careerMeta);
        _progressLabel = BodyLabel("ProgressLabel", muted: true, autowrap: true);
        _progressLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        careerMeta.AddChild(_progressLabel);
        careerMeta.Visible = false;

        // Konseptteki oyun kabuğu gibi ana bölümleri üstte, her sayfada sabit tut.
        // Böylece 844x390 yatay görünümde sol navigasyon içerik genişliğini tüketmez.
        BuildNavBar(shell);

        var screenHeading = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        screenHeading.AddThemeConstantOverride("separation", CareerUiTheme.SpaceM);
        shell.AddChild(screenHeading);
        var headingCopy = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headingCopy.AddThemeConstantOverride("separation", 2);
        screenHeading.AddChild(headingCopy);
        _pageTitleLabel = new Label();
        CareerUiTheme.StyleHeadline(_pageTitleLabel);
        _pageTitleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(24));
        headingCopy.AddChild(_pageTitleLabel);
        _pageSubtitleLabel = BodyLabel("PageSubtitle", muted: true, autowrap: true);
        _pageSubtitleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        // Referans ekranlardaki gibi başlığı tek başına bırak. Açıklama metni yerine
        // kartlar ve durum rozetleri ekranın ne yaptığını anlatıyor.
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
        _workspace.AddThemeConstantOverride("separation", CareerUiTheme.SpaceM);
        shell.AddChild(_workspace);

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
        _statusLabel.MouseFilter = MouseFilterEnum.Ignore;
        _statusPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
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
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        navPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.TopNavigationPanel());
        parent.AddChild(navPanel);

        // Referans tasarıma uygun: yatay HBox içinde sekmeler, seçili=yeşil dolgu
        var navRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        navRow.AddThemeConstantOverride("separation", CareerUiTheme.SeparationNav);
        navPanel.AddChild(navRow);

        // Sekme sırası ve hedef HubPage eşleşmesi:
        var navItems = new (HubPage Page, string Label)[]
        {
            (HubPage.Today, "ANA SAYFA"),
            (HubPage.Club, "KADRO"),
            (HubPage.Prep, "TAKTİK"),
            (HubPage.Transfer, "TRANSFER"),
            (HubPage.World, "TAKVİM"),
            (HubPage.File, "DAHA"),
        };

        _navButtons = new Button[6];
        foreach (var (page, label) in navItems)
        {
            var targetPage = page;
            var button = new Button
            {
                Text = label,
                ToggleMode = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            button.Pressed += () => ShowPage(targetPage);
            CareerUiTheme.StyleHubNavButton(button, selected: targetPage == HubPage.Today);
            navRow.AddChild(button);
            _navButtons[(int)targetPage] = button;
        }
    }

    private void ShowPage(HubPage page)
    {
        if (_layoutBuilt && page != _currentPage)
        {
            PlayInterfaceFeedback(MatchAudioCue.Button, vibrationDurationMs: 12);
        }
        ClosePlayerDossier();
        _currentPage = page;
        var titles = new[]
        {
            "Ana Sayfa",
            "Kadro",
            "Transfer",
            "Taktik",
            "Takvim",
            "Kulüp",
        };
        var subtitles = new[]
        {
            "Bugünün kritik kararları ve sıradaki hamle",
            "Oyuncular, mevkiler, sözler ve soyunma odası",
            "Gözlemcilik raporları, bütçe ve transfer masası",
            "14 diziliş, taktiksel senaryolar ve maç hazırlığı",
            "Puan durumu, haftanın fikstürü ve sezon akışı",
            "Yönetim hedefleri, muhasebe bütçesi ve menajer mirası",
        };
        _pageTitleLabel.Text = titles[(int)page];
        _pageSubtitleLabel.Text = subtitles[(int)page];
        _pageSubtitleLabel.Visible = false;
        for (var i = 0; i < _pages.Length; i++)
        {
            _pages[i].Visible = i == (int)page;
            if (_navButtons[i] is not null)
            {
                CareerUiTheme.StyleHubNavButton(_navButtons[i], selected: i == (int)page);
            }
        }

        if (page == HubPage.Club && _compactLayout && _clubSubPage == ClubSubPage.Overview)
        {
            ShowClubSubPage(ClubSubPage.AllPlayers);
        }

        ApplyMobileFocusLayout(page, _compactLayout);

        if (_topContinueButton is not null)
        {
            _topContinueButton.Visible = _officeNextStepButton is not null && _officeNextStepButton.Visible;
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
            safe.Bottom,
            mobileRuntime: OS.HasFeature("mobile") || OS.HasFeature("android"));
        _compactLayout = profile.IsCompact;
        _shellMargin.AddThemeConstantOverride("margin_left", profile.LeftMargin);
        _shellMargin.AddThemeConstantOverride("margin_right", profile.RightMargin);
        _shellMargin.AddThemeConstantOverride("margin_top", profile.TopMargin);
        _shellMargin.AddThemeConstantOverride("margin_bottom", profile.BottomMargin);
        _topBar.AddThemeConstantOverride("separation", profile.IsCompact ? 6 : 10);
        _brandLabel.Visible = !profile.IsCompact;
        _seasonLabel.Visible = !profile.IsCompact;
        _pageSubtitleLabel.Visible = false;
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
        if (_topContinueButton is not null)
        {
            _topContinueButton.CustomMinimumSize = new Vector2(profile.IsCompact ? 104 : 140, profile.TouchTargetHeight);
            _topContinueButton.AddThemeFontSizeOverride(
                "font_size",
                CareerUiTheme.FontSize(profile.IsCompact ? 12 : 14));
        }
        if (_nextMatchPill is not null)
        {
            _nextMatchPill.Visible = !profile.IsCompact && _controller.BuildNextMatchBriefing().HasMatch;
        }
        if (_prepPitchHost is not null)
        {
            _prepPitchHost.CustomMinimumSize = new Vector2(0, profile.IsCompact ? 340 : 400);
        }
        _statusLabel.CustomMinimumSize = new Vector2(0, profile.IsCompact ? 46 : 42);
        _standingsTable.CustomMinimumSize = new Vector2(0, 500);
        if (_transferBentoGrid is not null)
        {
            _transferBentoGrid.Columns = Size.X >= 1500f ? 3 : Size.X >= 900f ? 2 : 1;
        }

        // Dosya sayfasındaki uzun finans değerleri yatay HBox minimumlarını büyütüyordu.
        // Düşük ekran yüksekliğinde bento kartlarını tek sütuna indirerek içerik genişliğini
        // daima gerçek viewport içinde tut; rozet ve küçük istatistikler iki sütunda kalabilir.
        var compactFileLayout = profile.IsCompact || Size.Y < 800f || Size.X < 1100f;
        if (_govTelemetryBar is not null)
        {
            _govTelemetryBar.Columns = compactFileLayout ? 2 : 4;
        }
        if (_fileTopGrid is not null)
        {
            _fileTopGrid.Columns = compactFileLayout ? 1 : 2;
        }
        if (_fileFinanceGrid is not null)
        {
            _fileFinanceGrid.Columns = compactFileLayout ? 1 : 2;
        }
        if (_fileBottomGrid is not null)
        {
            _fileBottomGrid.Columns = compactFileLayout ? 1 : 2;
        }
        if (_fileLegacyStatsGrid is not null)
        {
            _fileLegacyStatsGrid.Columns = compactFileLayout ? 2 : 4;
        }

        _navButtons[(int)HubPage.Today].Text = profile.IsCompact ? "⌂ ANA" : "ANA SAYFA";
        _navButtons[(int)HubPage.Club].Text = profile.IsCompact ? "👥 KADRO" : "KADRO";
        _navButtons[(int)HubPage.Prep].Text = profile.IsCompact ? "🎯 TAKTİK" : "TAKTİK";
        _navButtons[(int)HubPage.Transfer].Text = profile.IsCompact ? "⇄ TRANSFER" : "TRANSFER";
        _navButtons[(int)HubPage.World].Text = profile.IsCompact ? "📅 TAKVİM" : "TAKVİM";
        _navButtons[(int)HubPage.File].Text = profile.IsCompact ? "••• DAHA" : "DAHA";

        _squadAvgAgeBadgePanel.Visible = !profile.IsCompact;
        _squadTotalValueBadgePanel.Visible = !profile.IsCompact;
        _squadWageBillBadgePanel.Visible = !profile.IsCompact;
        _standingsTable.Visible = !profile.IsCompact;
        _compactStandingsList.Visible = profile.IsCompact;

        foreach (var page in _mobileFocusLayouts.Keys)
        {
            ApplyMobileFocusLayout(page, profile.IsCompact);
        }

        if (profile.IsCompact && _currentPage == HubPage.Club && _clubSubPage == ClubSubPage.Overview)
        {
            ShowClubSubPage(ClubSubPage.AllPlayers);
        }

        RefreshSquadStarVisibility();
    }

    private VBoxContainer PageRoot()
    {
        var page = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        page.AddThemeConstantOverride("separation", 10);
        return page;
    }

    private static VBoxContainer AddCard(Control parent, string title, bool emphasized = false, bool expandVertical = false, string? icon = null)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = expandVertical ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin,
        };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.CardPanel(emphasized));
        parent.AddChild(panel);

        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = expandVertical ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin,
        };
        content.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        panel.AddChild(content);

        var (resolvedIcon, cleanTitle) = ExtractOrResolveCardIcon(title, icon);
        if (!string.IsNullOrEmpty(resolvedIcon))
        {
            content.AddChild(UiComponents.PageHeader(resolvedIcon, cleanTitle));
            content.AddChild(UiComponents.Divider(1));
        }
        else
        {
            content.AddChild(UiComponents.SectionHeader(title, withDivider: true));
        }
        return content;
    }

    private static (string? icon, string cleanTitle) ExtractOrResolveCardIcon(string title, string? customIcon)
    {
        if (!string.IsNullOrWhiteSpace(customIcon))
        {
            return (customIcon, title);
        }

        var trimmed = title.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return (null, title);
        }

        string[] commonEmojis = ["🔍", "⭐", "📋", "📊", "🏆", "🎯", "💰", "🎖️", "💼", "💾", "⚙️", "🤝", "📤", "👥", "⚡", "🛡️", "🔥", "⚽", "📅", "🏢"];
        foreach (var emoji in commonEmojis)
        {
            if (trimmed.StartsWith(emoji))
            {
                var rest = trimmed[emoji.Length..].TrimStart(' ', '·', '-');
                return (emoji, rest);
            }
        }

        if (trimmed.Contains("PUAN DURUMU")) return ("📋", trimmed);
        if (trimmed.Contains("FİKSTÜR")) return ("📅", trimmed);
        if (trimmed.Contains("İSTİHBARAT") || trimmed.Contains("RAKİP")) return ("🏢", trimmed);
        if (trimmed.Contains("HEDEF") || trimmed.Contains("BEKLENTİ")) return ("🎯", trimmed);
        if (trimmed.Contains("MUHASEBE") || trimmed.Contains("BÜTÇE") || trimmed.Contains("FİNANS") || trimmed.Contains("KASA")) return ("💰", trimmed);
        if (trimmed.Contains("MİRAS") || trimmed.Contains("PROFİL")) return ("🎖️", trimmed);
        if (trimmed.Contains("PİYASA") || trimmed.Contains("TEKLİF") || trimmed.Contains("SÖZLEŞME")) return ("💼", trimmed);
        if (trimmed.Contains("KAYIT") || trimmed.Contains("DOSYA")) return ("💾", trimmed);
        if (trimmed.Contains("AYAR") || trimmed.Contains("DENEYİM")) return ("⚙️", trimmed);
        if (trimmed.Contains("GÖRÜŞME")) return ("🤝", trimmed);
        if (trimmed.Contains("GİDEN")) return ("📤", trimmed);
        if (trimmed.Contains("ANTRENMAN") || trimmed.Contains("POTANSİYEL")) return ("⭐", trimmed);
        if (trimmed.Contains("KADRO") || trimmed.Contains("OYUNCU")) return ("👥", trimmed);
        if (trimmed.Contains("TAKTIK") || trimmed.Contains("TAKTİK") || trimmed.Contains("SAHA")) return ("📋", trimmed);
        if (trimmed.Contains("TRANSFER")) return ("🔄", trimmed);

        return (null, title);
    }

    private static HFlowContainer ActionFlow()
    {
        var flow = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        flow.AddThemeConstantOverride("h_separation", 8);
        flow.AddThemeConstantOverride("v_separation", 8);
        return flow;
    }

    private void RegisterMobileFocusLayout(
        VBoxContainer page,
        HubPage hubPage,
        Control[] compactHiddenTargets,
        params (string Label, Control[] Targets)[] definitions)
    {
        var bar = new HBoxContainer
        {
            Name = $"MobileFocusBar{hubPage}",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        bar.AddThemeConstantOverride("separation", CareerUiTheme.SpaceXs);
        page.AddChild(bar);
        page.MoveChild(bar, 0);

        var sections = new MobileFocusSection[definitions.Length];
        for (var i = 0; i < definitions.Length; i++)
        {
            var sectionIndex = i;
            var button = new Button
            {
                Text = definitions[i].Label,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 44),
            };
            button.Pressed += () => SelectMobileFocusSection(hubPage, sectionIndex);
            bar.AddChild(button);
            sections[i] = new MobileFocusSection(button, definitions[i].Targets);
        }

        _mobileFocusLayouts[hubPage] = new MobileFocusLayout(bar, sections, compactHiddenTargets);
        _mobileFocusIndices.TryAdd(hubPage, 0);
        ApplyMobileFocusLayout(hubPage, _compactLayout);
    }

    private void SelectMobileFocusSection(HubPage page, int sectionIndex)
    {
        if (!_mobileFocusLayouts.TryGetValue(page, out var layout)
            || sectionIndex < 0
            || sectionIndex >= layout.Sections.Length)
        {
            return;
        }

        _mobileFocusIndices[page] = sectionIndex;
        ApplyMobileFocusLayout(page, compact: true);
        PlayInterfaceFeedback(MatchAudioCue.Button, vibrationDurationMs: 10);
        Callable.From(() => _pageScroll.ScrollVertical = 0).CallDeferred();
    }

    private void ApplyMobileFocusLayout(HubPage page, bool compact)
    {
        if (!_mobileFocusLayouts.TryGetValue(page, out var layout))
        {
            return;
        }

        var selected = Math.Clamp(
            _mobileFocusIndices.GetValueOrDefault(page),
            0,
            Math.Max(0, layout.Sections.Length - 1));
        layout.Bar.Visible = compact;

        for (var i = 0; i < layout.Sections.Length; i++)
        {
            var active = !compact || i == selected;
            foreach (var target in layout.Sections[i].Targets)
            {
                target.Visible = active;
            }

            CareerUiTheme.StyleTabButton(layout.Sections[i].Button, selected: compact && i == selected);
        }

        foreach (var target in layout.CompactHiddenTargets)
        {
            target.Visible = !compact;
        }
    }

    private static Label SectionTitle(string text)
    {
        var label = new Label { Text = text };
        CareerUiTheme.StyleSection(label);
        return label;
    }

    private static Label TitleLabel(string name)
    {
        var label = new Label { Name = name };
        CareerUiTheme.StyleHeadline(label);
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

    private static string FormatFormPills(string form)
    {
        if (string.IsNullOrWhiteSpace(form) || form == "—")
        {
            return "—";
        }

        var results = form
            .Where(result => result is 'G' or 'B' or 'M' or 'W' or 'D' or 'L')
            .TakeLast(5)
            .Select(result => result switch
            {
                'G' or 'W' => $"🟢{result}",
                'B' or 'D' => $"⚪{result}",
                'M' or 'L' => $"🔴{result}",
                _ => result.ToString(),
            });
        return string.Join(" ", results);
    }

    private static void CreateStandingsLegendBadge(Control parent, string text, Color color)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(color, 0.18f));
        var label = new Label { Text = text };
        CareerUiTheme.StyleEyebrow(label, color);
        panel.AddChild(label);
        parent.AddChild(panel);
    }

    private static Color? StandingsZoneColor(int index, int teamCount) => index switch
    {
        0 => CareerUiTheme.Accent,
        <= 3 => CareerUiTheme.Data,
        _ when index >= Math.Max(0, teamCount - 3) => CareerUiTheme.DangerSoft,
        _ => null,
    };

    private static void ConfigureStandingsColumns(Tree table)
    {
        var titles = new[] { "#", "TAKIM", "O", "G", "B", "M", "A", "Y", "AV", "FORM", "P" };
        for (var column = 0; column < titles.Length; column++)
        {
            table.SetColumnTitle(column, titles[column]);
            table.SetColumnExpand(column, column == 1);
            table.SetColumnCustomMinimumWidth(
                column,
                column == 1 ? 175 : column == 9 ? 110 : column == 0 ? 48 : 32);
        }
    }

    private void ShowEmptyStandings(string message)
    {
        _standingsTable.Clear();
        var root = _standingsTable.CreateItem();
        var row = _standingsTable.CreateItem(root);
        row.SetText(1, message);
        row.SetCustomColor(1, CareerUiTheme.InkMuted);

        _compactStandingsList.Clear();
        _compactStandingsClubIds.Clear();
        _compactStandingsList.AddItem(message);
        _compactStandingsClubIds.Add(null);
        _compactStandingsList.SetItemCustomFgColor(0, CareerUiTheme.InkMuted);
    }

    private int AddCompactStanding(long? clubId, string text, Color color)
    {
        var index = _compactStandingsList.ItemCount;
        _compactStandingsList.AddItem(text);
        _compactStandingsClubIds.Add(clubId);
        _compactStandingsList.SetItemCustomFgColor(index, color);
        return index;
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

    private static Button AccentButton(string text)
    {
        var button = new Button { Text = text };
        CareerUiTheme.StyleAccentButton(button);
        return button;
    }

    private static Button DangerButton(string text)
    {
        var button = new Button { Text = text };
        CareerUiTheme.StyleDangerButton(button);
        return button;
    }

    private static Button GhostButton(string text)
    {
        var button = new Button { Text = text };
        CareerUiTheme.StyleGhostButton(button);
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
        _statusLabel.Text = CompactText(ToPlayerFacingText(message), maxLines: 3, maxCharacters: 300);
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

    private void OnPlayMatches()
    {
        PlayInterfaceFeedback(MatchAudioCue.Whistle, vibrationDurationMs: 24);
        var currentDay = _controller.GetCurrentGameDate().DayNumber;
        if (_controller.HasDueContinentalMatch())
        {
            Callable.From(() => MatchDayRequested?.Invoke()).CallDeferred();
            return;
        }

        var pending = _controller.GetNextDueManagedFixture(currentDay);
        if (pending is not null && !pending.IsApproved)
        {
            var approveResult = _controller.ApproveDefaultSelectionForNextDueMatch();
            if (!approveResult.Succeeded)
            {
                Apply(approveResult);
                return;
            }
        }
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

    private void CompleteTransferAndCelebrate(Func<UiActionResult> complete)
    {
        var beforePlayerIds = _controller.BuildPlayerManagementDigest().Players
            .Select(player => player.PlayerId)
            .ToHashSet();
        var result = complete();
        Apply(result);
        if (!result.Succeeded)
        {
            return;
        }

        var signed = _controller.BuildPlayerManagementDigest().Players
            .FirstOrDefault(player => !beforePlayerIds.Contains(player.PlayerId));
        if (signed is null)
        {
            return;
        }

        _newTransferPlayerId = signed.PlayerId;
        _audioDirector?.TryPlayCue(MatchAudioCue.ResultWin, signed.SquadNumber);
        _transferWelcomeHeadlineLabel.Text = $"✍ HOŞ GELDİN, {signed.DisplayName.ToUpperInvariant()}!";
        _transferWelcomeMetaLabel.Text =
            $"#{signed.SquadNumber} · {signed.PositionName} ({signed.PositionCode}) · {signed.Rating} GÜÇ\n"
            + "İmza tamamlandı; oyuncu A takım listesinde #YENİ rozetiyle işaretlendi.";
        _transferWelcomePanel.Visible = true;
        RefreshSquadList();
    }

    private void ApplyDecision(Func<UiActionResult> decide)
    {
        var result = decide();
        _audioDirector?.TryPlayCue(result.Succeeded ? MatchAudioCue.Button : MatchAudioCue.YellowCard);
        Apply(result);
    }

    private void PlayInterfaceFeedback(MatchAudioCue cue, int vibrationDurationMs)
    {
        _audioDirector?.TryPlayCue(cue, (int)_currentPage);
        var strength = GameExperienceSettingsStore.Current.EffectiveHapticsStrengthPercent;
        if (OS.HasFeature("mobile") && strength > 0)
        {
            Input.VibrateHandheld(Math.Max(1, vibrationDurationMs * strength / 100));
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

    private void OnSquadPlayerActivated(long index)
    {
        if (index < 0
            || index >= _playerManagementPlayers.Count)
        {
            return;
        }

        OpenPlayerDossier(_playerManagementPlayers[(int)index].PlayerId);
    }

    private void RefreshClubPitch(IReadOnlyList<PlayerManagementLine> players)
    {
        MatchScreenUi.ClearChildren(_clubPitchHost);
        var board = _controller.BuildSquadSelectionBoard();
        var tactic = _controller.GetManagedClubTacticPlan();
        var formation = tactic?.Formation ?? Formation.F442;
        if (board.HasMatch && board.StartingXi.Count == Domain.TeamPreparation.MatchSelection.StartingXiSize)
        {
            _clubPitchHost.AddChild(TacticalPitchBoardUi.BuildReadOnly(board.StartingXi, OnPitchPlayerSelected, formation));
            return;
        }

        var startingXi = players
            .Where(player => !player.IsInjured)
            .Take(Domain.TeamPreparation.MatchSelection.StartingXiSize)
            .Select(player => new Application.TeamPreparation.Queries.SquadSelectionPlayerDigest(
                player.SlotIndex,
                player.DisplayName,
                player.PositionCode,
                player.Rating,
                player.Fitness,
                player.Fatigue,
                true,
                true,
                player.PositionName))
            .ToArray();
        if (startingXi.Length != Domain.TeamPreparation.MatchSelection.StartingXiSize)
        {
            var empty = BodyLabel("ClubPitchEmpty", muted: true, autowrap: true);
            empty.Text = "Saha yerleşimi için yeterli müsait futbolcu yok.";
            _clubPitchHost.AddChild(empty);
            return;
        }

        _clubPitchHost.AddChild(TacticalPitchBoardUi.BuildReadOnly(startingXi, OnPitchPlayerSelected, formation));
    }

    private void RefreshClubPlanBoard()
    {
        MatchScreenUi.ClearChildren(_clubPlanPitchHost);
        RefreshSquadDepthChart();
        var board = _controller.BuildSquadSelectionBoard();
        var tactic = _controller.GetManagedClubTacticPlan();
        var formation = tactic?.Formation ?? Formation.F442;
        if (!board.HasMatch)
        {
            var empty = BodyLabel("ClubPlanEmpty", muted: true, autowrap: true);
            empty.Text = "Yaklaşan maç yok — Genel Bakış’taki kadro önizlemesine bak.";
            _clubPlanPitchHost.AddChild(empty);
            _clubPlanCaptionLabel.Text = "Kadro planı maç gününe bağlanır.";
            return;
        }

        var selectedStillExists = board.StartingXi.Concat(board.Bench)
            .Any(player => player.SlotIndex == _clubPlanSelectedSlotIndex);
        if (!selectedStillExists)
        {
            _clubPlanSelectedSlotIndex = null;
            _clubPlanSelectedIsStarter = null;
        }

        _clubPlanCaptionLabel.Text = board.IsApproved
            ? "✓ Onaylı XI ve yedekler — Takas için bir oyuncuya, ardından yedeğe dokun."
            : "⚠️ Taslak XI ve yedekler — Oyuncuları dokunarak takas et ve maça hazırla.";
        _clubPlanPitchHost.AddChild(
            TacticalPitchBoardUi.Build(
                board,
                selectedSlotIndex: _clubPlanSelectedSlotIndex,
                selectPlayer: OnClubPlanPlayerSelected,
                swapPlayers: (starter, bench) => Apply(_controller.SwapStarterWithBenchForNextDueMatch(starter, bench)),
                interactionEnabled: true,
                formation: formation));
    }

    private void RefreshSquadDepthChart()
    {
        MatchScreenUi.ClearChildren(_squadDepthGrid);
        var players = _controller.BuildPlayerManagementDigest().Players;
        var groups = new[]
        {
            (Code: "GK", Title: "🧤 KALECİ", Minimum: 2, Color: CareerUiTheme.Data),
            (Code: "DEF", Title: "🛡️ SAVUNMA", Minimum: 7, Color: CareerUiTheme.ActionBright),
            (Code: "MID", Title: "⚙️ ORTA SAHA", Minimum: 6, Color: CareerUiTheme.Accent),
            (Code: "FWD", Title: "⚡ HÜCUM", Minimum: 4, Color: CareerUiTheme.DangerSoft),
        };

        foreach (var group in groups)
        {
            var line = players
                .Where(player => PositionDepthGroup(player.PositionCode) == group.Code)
                .OrderByDescending(player => player.Rating)
                .ThenByDescending(player => player.Fitness)
                .ToArray();
            var sufficient = line.Length >= group.Minimum;
            var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            card.AddThemeStyleboxOverride(
                "panel",
                CareerUiTheme.BadgePillPanel(sufficient ? group.Color : CareerUiTheme.DangerSoft, 0.16f));
            _squadDepthGrid.AddChild(card);

            var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            body.AddThemeConstantOverride("separation", 5);
            card.AddChild(body);
            var title = new Label
            {
                Text = $"{group.Title} · {line.Length} OYUNCU · {(sufficient ? "✓ YETERLİ" : "⚠ EKSİK")}",
            };
            CareerUiTheme.StyleEyebrow(title, sufficient ? group.Color : CareerUiTheme.DangerSoft);
            body.AddChild(title);

            var starter = line.FirstOrDefault();
            var starterLabel = BodyLabel($"Depth{group.Code}Starter", autowrap: true);
            starterLabel.Text = starter is null
                ? "AS: Atanmış oyuncu yok"
                : $"AS: #{starter.SquadNumber} {starter.DisplayName} · {starter.PositionCode} · {starter.Rating} GÜÇ";
            body.AddChild(starterLabel);

            var alternatives = line.Skip(1).Take(4)
                .Select(player => $"#{player.SquadNumber} {player.DisplayName} ({player.PositionCode}/{player.Rating})")
                .ToArray();
            var benchLabel = BodyLabel($"Depth{group.Code}Bench", muted: true, autowrap: true);
            benchLabel.Text = alternatives.Length == 0
                ? "YEDEK: Alternatif yok"
                : $"YEDEK: {string.Join(" · ", alternatives)}";
            body.AddChild(benchLabel);
        }
    }

    private static string PositionDepthGroup(string positionCode) => positionCode switch
    {
        "KL" or "GK" => "GK",
        "CB" or "LB" or "RB" or "STP" or "SLB" or "SĞB" or "KB" => "DEF",
        "CM" or "DM" or "AM" or "MO" or "DOS" or "OOS" => "MID",
        _ => "FWD",
    };

    private void OnClubPlanPlayerSelected(Application.TeamPreparation.Queries.SquadSelectionPlayerDigest player)
    {
        if (_clubPlanSelectedSlotIndex == player.SlotIndex)
        {
            _clubPlanSelectedSlotIndex = null;
            _clubPlanSelectedIsStarter = null;
        }
        else if (_clubPlanSelectedSlotIndex is null || _clubPlanSelectedIsStarter == player.IsStarter)
        {
            _clubPlanSelectedSlotIndex = player.SlotIndex;
            _clubPlanSelectedIsStarter = player.IsStarter;
        }
        else
        {
            var starter = player.IsStarter ? player.SlotIndex : _clubPlanSelectedSlotIndex.Value;
            var bench = player.IsStarter ? _clubPlanSelectedSlotIndex.Value : player.SlotIndex;
            _clubPlanSelectedSlotIndex = null;
            _clubPlanSelectedIsStarter = null;
            Apply(_controller.SwapStarterWithBenchForNextDueMatch(starter, bench));
            return;
        }

        RefreshClubPlanBoard();
    }

    private void RefreshClubSignals(Application.CareerHub.Queries.SquadOverviewDigest overview)
    {
        MatchScreenUi.ClearChildren(_clubSignalRow);
        foreach (var signal in overview.Signals)
        {
            var code = signal.Code;
            var button = SecondaryButton(signal.Label);
            if (signal.IsActionable)
            {
                button.Pressed += () => OnClubSignalPressed(code);
            }

            _clubSignalRow.AddChild(button);
        }

        _clubDeclareNeedButton.Visible = overview.CanCreateTransferNeed;
    }

    private void OnClubSignalPressed(string code)
    {
        if (code is Application.CareerHub.Queries.SquadOverviewDigest.SignalDepth
            or Application.CareerHub.Queries.SquadOverviewDigest.SignalOverflow)
        {
            ShowPage(HubPage.Transfer);
            return;
        }

        if (code == Application.CareerHub.Queries.SquadOverviewDigest.SignalFatigue)
        {
            Apply(_controller.SetWeeklyTrainingFocus(TrainingFocus.Recovery));
            Apply(_controller.SetWeeklyTrainingRest(RestApproach.Heavy));
            ShowPage(HubPage.Prep);
            return;
        }

        _squadListFilterCode = code;
        ShowClubSubPage(ClubSubPage.AllPlayers);
        RefreshSquadList();
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
        var candidate = _scoutCandidates[(int)index];
        BindScoutCandidateDetail(candidate);
        _suggestTargetButton.Disabled = candidate.IsListedTarget;
        _suggestTargetButton.Visible = !_suggestTargetButton.Disabled;
    }

    private void BindScoutCandidateDetail(ScoutCandidateLine candidate)
    {
        _scoutCandidateDetailLabel.Text =
            $"{candidate.DisplayName} · {candidate.PositionCode} · {candidate.ClubName}\n"
            + $"Güç {candidate.EstimatedAbilityLow}-{candidate.EstimatedAbilityHigh} · Potansiyel {candidate.PotentialAbility} · {candidate.InterestLabel}";
        _scoutCandidateDetailLabel.TooltipText = candidate.ToDetailText();
        _scoutMarketValueBadgeLabel.Text = candidate.MarketValue == 0
            ? "💎 SERBEST"
            : $"💎 {candidate.MarketValue:N0} TRY";
        _scoutWageExpectationBadgeLabel.Text = $"💵 {candidate.SuggestedWeeklyWage:N0}/hf";
        _scoutTransferChanceBadgeLabel.Text = $"🎯 %{candidate.InterestPercent} · {candidate.InterestLabel}";
        _transferFeeInput.Value = candidate.SuggestedOpeningFee;
        _transferWageInput.Value = candidate.SuggestedWeeklyWage;
        _contractYearsInput.Value = candidate.Age switch
        {
            <= 23 => 5,
            <= 29 => 4,
            <= 32 => 3,
            _ => 2,
        };
    }

    private static Label CreateTransferSummaryBadge(Control parent, string text, Color color)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(color, 0.24f));
        var label = new Label { Text = text };
        CareerUiTheme.StyleEyebrow(label, color);
        panel.AddChild(label);
        parent.AddChild(panel);
        return label;
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

        if (selected is null)
        {
            _playerDetailLabel.Text = "Bir kez dokunarak oyuncuyu seç; dosyayı çift dokunuşla veya profil düğmesiyle aç.";
            _playerDetailLabel.TooltipText = string.Empty;
        }
        else
        {
            // Ana satır: kritik istatistikler tek bakışta görünür
            var fitness = selected.Fitness switch
            {
                >= 85 => "💪",
                >= 60 => "🟡",
                _ => "🔴"
            };
            var relIcon = selected.RelationshipState switch
            {
                "Güçlü" => "🟢",
                "Kırılgan" => "🔴",
                _ => "⚪"
            };
            var availability = selected.Availability == "Hazır" ? string.Empty : $" · ⚠ {selected.Availability}";
            var promiseFlag = selected.PromiseSummary == "aktif söz yok" ? string.Empty : " · 🤝 söz";
            var workload = string.IsNullOrWhiteSpace(selected.WorkloadHint)
                ? string.Empty
                : $" · {selected.WorkloadHint}";
            _playerDetailLabel.Text =
                $"{selected.DisplayName} · {selected.PositionCode} · GÜÇ {selected.Rating}"
                + $" · {fitness} Fit %{selected.Fitness}"
                + $" · {selected.FatigueRiskBand}"
                + $" · {relIcon} {selected.RelationshipState}"
                + availability
                + workload
                + promiseFlag
                + " — dosya için tekrar dokun.";
            _playerDetailLabel.TooltipText = selected.ToDetailText();
        }

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
        BeginSaveAction(_loadGameButton, "⏳ Yükleniyor...");
        Callable.From(() =>
        {
            var result = _controller.LoadGame();
            if (result.Succeeded && _controller.LastCareerResume is { } resume)
            {
                ApplyCareerResume(resume);
            }
            else
            {
                Apply(result);
            }
            EndSaveAction(_loadGameButton, "↻ Yeniden Yükle", result.Succeeded);
        }).CallDeferred();
    }

    private void OnSaveGamePressed()
    {
        BeginSaveAction(_saveGameButton, "⏳ Kaydediliyor...");
        Callable.From(() =>
        {
            var result = _controller.SaveGame();
            Apply(result);
            EndSaveAction(_saveGameButton, "⚡ Hızlı Kaydet", result.Succeeded);
        }).CallDeferred();
    }

    private static void BeginSaveAction(Button button, string busyText)
    {
        button.Disabled = true;
        button.Text = busyText;
        button.Modulate = new Color(1f, 1f, 1f, 0.55f);
    }

    private void EndSaveAction(Button button, string idleText, bool succeeded)
    {
        button.Disabled = false;
        button.Text = succeeded ? $"✓ {idleText}" : $"⚠ {idleText}";
        button.Modulate = Colors.White;
        if (!CareerUiTheme.ReducedMotion)
        {
            button.Scale = new Vector2(0.97f, 0.97f);
            CreateTween().TweenProperty(button, "scale", Vector2.One, 0.18f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
        }
        Callable.From(() => button.Text = idleText).CallDeferred();
    }

    private void RefreshUi()
    {
        var current = _controller.GetCurrentGameDate();
        var season = _controller.GetCurrentSeason();
        var manager = _controller.GetManagerCareer();
        var period = _controller.GetCurrentPlanningPeriod();

        _dateLabel.Text = $"{current.Day:D2}.{current.Month:D2}.{current.Year}";
        RefreshClubBranding();

        if (string.Equals(manager.EmploymentStatus, "Unemployed", StringComparison.Ordinal))
        {
            var lastClub = manager.LastClubId is long lastClubId
                ? _controller.GetClubDisplayName(lastClubId)
                : "—";
            var offerText = manager.PendingOfferClubId is long offerClubId
                ? $"Aktif teklif: {GetClubDisplayNameSafe(offerClubId)}"
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
            RefreshTodayStandingsWidget();
            RefreshTodayTacticsWidget();
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
            RefreshOutgoingSaleStatus();
            RefreshTacticStatus();
            RefreshSquadList();
            RefreshSaveDesk();
            UpdatePrimaryHints(dueMatchCount: 0, canAdvance: _controller.CanAdvanceTime());
            UpdateJobOfferButtons(manager);
            UpdateTransferNeedButtons(manager);
            UpdateTrainingButtons(manager);
            UpdateTacticButtons(manager);
            RefreshNavBadges();
            SanitizeScreenText(this);
            return;
        }

        var progress = _controller.GetSeasonProgress(season.SeasonId);
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
        RefreshTodayStandingsWidget();
        RefreshTodayTacticsWidget();
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
        RefreshOutgoingSaleStatus();
        RefreshTacticStatus();
        RefreshStandings();
        RefreshFixtureList();
        RefreshSquadList();
        RefreshSaveDesk();

        var dueCount = _controller.GetDueFixtureCount(season.SeasonId, current.DayNumber);

        UpdatePrimaryHints(dueCount, _controller.CanAdvanceTime());
        UpdateJobOfferButtons(manager);
        UpdateTransferNeedButtons(manager);
        UpdateTrainingButtons(manager);
        UpdateTacticButtons(manager);
        UpdateSaveDeskButtons();
        RefreshNavBadges();
        SanitizeScreenText(this);
        ApplyMobileFocusLayout(_currentPage, _compactLayout);
        RefreshSquadStarVisibility();
    }

    private void RefreshNavBadges()
    {
        if (_navButtons is null || _navButtons.Length < 6)
        {
            return;
        }

        // MERKEZ: bekleyen zorunlu karar sayısı
        var decisions = _controller.GetPendingDecisions(take: 5);
        var pendingCount = decisions.OpenCount;
        var todayLabel = _compactLayout ? "⌂ ANA" : "ANA SAYFA";
        _navButtons[(int)HubPage.Today].Text = pendingCount > 0 ? $"{todayLabel} {pendingCount}" : todayLabel;

        // KADRO: onay bekleyen maç varsa uyarı
        var currentDay = _controller.GetCurrentGameDate().DayNumber;
        var pendingMatch = _controller.GetNextDueManagedFixture(currentDay);
        _navButtons[(int)HubPage.Club].Text = pendingMatch is { IsApproved: false }
            ? (_compactLayout ? "👥 KADRO !" : "KADRO (!)")
            : (_compactLayout ? "👥 KADRO" : "KADRO");

        // TRANSFER: bekleyen teklif veya pencere açık
        var window = _controller.GetTransferWindow();
        var offers = _controller.GetManagedClubOffers();
        var proposals = _controller.GetManagedContractProposals();
        var needsOpen = _controller.GetManagedClubNeeds().OpenCount > 0;
        var transferPending = offers.PendingCount > 0 || proposals.PendingCount > 0 || needsOpen;
        _navButtons[(int)HubPage.Transfer].Text = transferPending
            ? (_compactLayout ? "⇄ TRANSFER !" : "TRANSFER (!)")
            : window.IsOpen
                ? (_compactLayout ? "⇄ TRANSFER ●" : "TRANSFER ●")
                : (_compactLayout ? "⇄ TRANSFER" : "TRANSFER");

        // TAKTİK: antrenman planı yoksa uyarı
        var training = _controller.GetTrainingSummary();
        _navButtons[(int)HubPage.Prep].Text = training.ClubId is not null && !training.HasPlan
            ? (_compactLayout ? "🎯 TAKTİK !" : "TAKTİK (!)")
            : (_compactLayout ? "🎯 TAKTİK" : "TAKTİK");

        _navButtons[(int)HubPage.World].Text = _compactLayout ? "📅 TAKVİM" : "TAKVİM";
        _navButtons[(int)HubPage.File].Text = _compactLayout ? "••• DAHA" : "DAHA";
    }

    private void RefreshClubBranding()
    {
        var manager = _controller.GetManagerCareer();
        var club = manager.EmployedClubId is long clubId
            ? _controller.GetClub(clubId)
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

        if (_legacyTenureLabel is not null)
        {
            _legacyTenureLabel.Text = $"{legacy.RecordLine}";
        }

        if (_legacyReputationBadgeLabel is not null)
        {
            var career = _controller.Host.ManagerModule.Store.Career;
            var rep = career.Reputation.Value;
            var repTitle = rep >= 80 ? "Elit Menajer" : (rep >= 60 ? "Kıdemli Menajer" : (rep >= 40 ? "Profesyonel" : "Çaylak"));
            _legacyReputationBadgeLabel.Text = $"⭐ İtibar: {rep} ({repTitle})";
        }

        if (_legacyStatMatchesLabel is not null)
        {
            _legacyStatMatchesLabel.Text = $"{legacy.RecordLine}";
        }

        if (_legacyStatWinRateLabel is not null)
        {
            var totalSeasons = legacy.Seasons.Count;
            _legacyStatWinRateLabel.Text = totalSeasons > 0 ? $"{totalSeasons} Sezon Geçmişi" : "İlk Sezon";
        }

        if (_legacyStatYouthLabel is not null)
        {
            _legacyStatYouthLabel.Text = $"{legacy.DevelopmentLine}";
        }

        if (_legacyStatMilestoneLabel is not null)
        {
            _legacyStatMilestoneLabel.Text = $"{legacy.NextMilestoneLine}";
        }

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
        _saveLastWriteBadgeLabel.Text = desk.SaveLastWriteUtc is DateTimeOffset stamp
            ? $"🕒 Son Kayıt: {stamp.ToLocalTime():g}"
            : "🕒 Son Kayıt: Henüz yok";
        _saveActiveSlotBadgeLabel.Text = $"📁 Aktif Slot: {desk.ActiveSlot}";
        _saveWorldSeedBadgeLabel.Text = $"🌍 Dünya Seed: {desk.WorldSeed}";
        _saveFileSizeBadgeLabel.Text = $"💽 Dosya Boyutu: {FormatFileSize(desk.SaveSizeBytes)}";
        UpdateSaveDeskButtons();
    }

    private static Label CreateSaveTelemetryBadge(Control parent, string text, Color color)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(color, 0.18f));
        var label = new Label { Text = text };
        CareerUiTheme.StyleEyebrow(label, color);
        panel.AddChild(label);
        parent.AddChild(panel);
        return label;
    }

    private static string FormatFileSize(long bytes) => bytes switch
    {
        <= 0 => "—",
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024d:0.0} KB",
        _ => $"{bytes / (1024d * 1024d):0.00} MB",
    };

    private void RefreshClubEconomy()
    {
        var economy = _controller.BuildClubEconomy();
        if (economy is null)
        {
            _clubEconomyLabel.Text = "Kulüp ekonomisi: aktif görev bulunmuyor.";
            if (_boardConfidenceBadgeLabel is not null) _boardConfidenceBadgeLabel.Text = "🏛️ Güven %—";
            if (_operatingBalanceBadgeLabel is not null) _operatingBalanceBadgeLabel.Text = "💰 Net —";
            if (_stadiumAttendanceBadgeLabel is not null) _stadiumAttendanceBadgeLabel.Text = "🏟️ %—";
            if (_wageUtilizationBadgeLabel is not null) _wageUtilizationBadgeLabel.Text = "💼 Maaş %—";
            return;
        }

        var balanceTone = economy.ProjectedOperatingBalance >= 0 ? "Fazla" : "Açık";
        var balanceColor = economy.ProjectedOperatingBalance >= 0 ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft;
        var realized = _controller.BuildRealizedClubFinance();

        // 1. Telemetry Bar
        if (_boardConfidenceBadgeLabel is not null && _boardConfidenceBadgePanel is not null)
        {
            var conf = economy.BoardConfidence ?? 50;
            var confColor = conf >= 70 ? CareerUiTheme.ActionBright : (conf >= 40 ? CareerUiTheme.ColorWarning : CareerUiTheme.DangerSoft);
            var confText = conf >= 75 ? "Çok Güçlü" : (conf >= 55 ? "İyi" : (conf >= 40 ? "Baskı Altında" : "Kritik"));
            _boardConfidenceBadgeLabel.Text = $"🏛️ Güven %{conf}";
            _boardConfidenceBadgeLabel.TooltipText = confText;
            _boardConfidenceBadgeLabel.AddThemeColorOverride("font_color", confColor);
            _boardConfidenceBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(confColor, 0.20f));
        }

        if (_operatingBalanceBadgeLabel is not null && _operatingBalanceBadgePanel is not null)
        {
            var balText = economy.ProjectedOperatingBalance >= 0 ? $"+{economy.ProjectedOperatingBalance:N0}" : $"-{Math.Abs(economy.ProjectedOperatingBalance):N0}";
            _operatingBalanceBadgeLabel.Text = $"💰 {balText} {economy.CurrencyCode}";
            _operatingBalanceBadgeLabel.TooltipText = balanceTone;
            _operatingBalanceBadgeLabel.AddThemeColorOverride("font_color", balanceColor);
            _operatingBalanceBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(balanceColor, 0.16f));
        }

        if (_stadiumAttendanceBadgeLabel is not null)
        {
            _stadiumAttendanceBadgeLabel.Text = $"🏟️ %{economy.AttendancePercent}";
            _stadiumAttendanceBadgeLabel.TooltipText = $"{economy.ProjectedAverageAttendance:N0}/{economy.StadiumCapacity:N0}";
        }

        if (_wageUtilizationBadgeLabel is not null)
        {
            _wageUtilizationBadgeLabel.Text = $"💼 Maaş %{economy.WageUtilizationPercent}";
            _wageUtilizationBadgeLabel.TooltipText = $"Boşluk {economy.WeeklyWageHeadroom:N0} {economy.CurrencyCode}/hf";
        }

        // 2. Board Objectives Desk
        if (_boardObjectivesContainer is not null)
        {
            MatchScreenUi.ClearChildren(_boardObjectivesContainer);

            var seasonExpLabel = new Label
            {
                Text = $"{economy.SeasonExpectation} · {economy.ClubName}",
            };
            CareerUiTheme.StyleHeadline(seasonExpLabel);
            seasonExpLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
            seasonExpLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
            _boardObjectivesContainer.AddChild(seasonExpLabel);

            foreach (var objective in economy.BoardObjectives)
            {
                var (statusText, statusColor) = objective.Status switch
                {
                    Application.ClubGovernance.Queries.BoardObjectiveStatus.Achieved => ("✓ Tamamlandı", CareerUiTheme.ActionBright),
                    Application.ClubGovernance.Queries.BoardObjectiveStatus.OnTrack => ("🟢 Yolunda", CareerUiTheme.ActionBright),
                    Application.ClubGovernance.Queries.BoardObjectiveStatus.AtRisk => ("⚠️ Riskte", CareerUiTheme.ColorWarning),
                    Application.ClubGovernance.Queries.BoardObjectiveStatus.OffTrack => ("🔴 Geride", CareerUiTheme.DangerSoft),
                    Application.ClubGovernance.Queries.BoardObjectiveStatus.Failed => ("✕ Başarısız", CareerUiTheme.DangerSoft),
                    _ => ("⚪ Başlamadı", CareerUiTheme.TextMuted),
                };

                var objPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                objPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BoardObjectiveCardPanel(statusColor));
                _boardObjectivesContainer.AddChild(objPanel);

                var objVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                objVBox.AddThemeConstantOverride("separation", 4);
                objPanel.AddChild(objVBox);

                var objTopRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                objVBox.AddChild(objTopRow);

                var titleLabel = new Label
                {
                    Text = objective.Title,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                CareerUiTheme.StyleHeadline(titleLabel);
                titleLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(13));
                objTopRow.AddChild(titleLabel);

                var badgePanel = new PanelContainer();
                badgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(statusColor, 0.20f));
                var badgeLabel = new Label { Text = statusText };
                CareerUiTheme.StyleTableText(badgeLabel);
                badgeLabel.AddThemeColorOverride("font_color", statusColor);
                badgePanel.AddChild(badgeLabel);
                objTopRow.AddChild(badgePanel);

                var progBar = new ProgressBar
                {
                    MinValue = 0,
                    MaxValue = 100,
                    Value = Math.Clamp(objective.ProgressPercent, 0, 100),
                    ShowPercentage = false,
                    CustomMinimumSize = new Vector2(0, 6),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                progBar.AddThemeStyleboxOverride("background", CareerUiTheme.ProgressBarBackground());
                progBar.AddThemeStyleboxOverride("fill", CareerUiTheme.ProgressBarFill(statusColor));
                objVBox.AddChild(progBar);

                var detailLabel = BodyLabel($"ObjectiveDetail_{objective.Code}", muted: true, autowrap: true);
                detailLabel.Text = $"{objective.Current} / {objective.Target} · %{objective.ProgressPercent}";
                detailLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(11));
                objVBox.AddChild(detailLabel);
            }
        }

        // 3. Finance Projection & Realized
        if (_financeProjectionLabel is not null)
        {
            _financeProjectionLabel.Text =
                $"Gelir  {economy.ProjectedOperatingRevenue:N0} · Gider  {economy.ProjectedOperatingCosts:N0}\n"
                + $"Net  {(economy.ProjectedOperatingBalance >= 0 ? "+" : "-")}{Math.Abs(economy.ProjectedOperatingBalance):N0} {economy.CurrencyCode}";
            _financeProjectionLabel.TooltipText =
                $"Maç günü {economy.ProjectedMatchdayRevenue:N0} · Sponsorluk {economy.ProjectedSponsorRevenue:N0} · "
                + $"Bordro {economy.ProjectedAnnualWageSpend:N0} · Operasyon {economy.ProjectedFootballOperationsCost:N0}";
        }

        if (_realizedFinanceLabel is not null && realized is not null)
        {
            _realizedFinanceLabel.Text =
                $"Kasa  {realized.Balance:N0} {realized.CurrencyCode}\n"
                + $"Gelir  {realized.Revenue:N0} · Gider  {realized.Expenses:N0} · Net  {(realized.OperatingResult >= 0 ? "+" : "")}{realized.OperatingResult:N0}";

            if (_boardOutcomeSummaryLabel is not null)
            {
                _boardOutcomeSummaryLabel.Text = $"Denetim Raporu: {realized.BoardOutcome.Summary}";
            }

            if (_boardFinanceStatusBadgeLabel is not null && _boardFinanceStatusBadgePanel is not null)
            {
                var finStatusText = FormatBoardFinancialStatus(realized.BoardOutcome.Status);
                var finStatusColor = realized.BoardOutcome.Status switch
                {
                    Application.ClubGovernance.Services.BoardFinancialStatus.Healthy => CareerUiTheme.ActionBright,
                    Application.ClubGovernance.Services.BoardFinancialStatus.Watch => CareerUiTheme.ColorWarning,
                    _ => CareerUiTheme.DangerSoft,
                };
                _boardFinanceStatusBadgeLabel.Text = finStatusText;
                _boardFinanceStatusBadgeLabel.AddThemeColorOverride("font_color", finStatusColor);
                _boardFinanceStatusBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(finStatusColor, 0.20f));
            }
        }
        else if (_realizedFinanceLabel is not null)
        {
            _realizedFinanceLabel.Text = "Kasa hareketi bekleniyor.";
            if (_boardOutcomeSummaryLabel is not null) _boardOutcomeSummaryLabel.Text = string.Empty;
        }

        // Keep _clubEconomyLabel updated for any legacy callers
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
            safe.Bottom,
            mobileRuntime: OS.HasFeature("mobile") || OS.HasFeature("android"));
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
        var timeline = _controller.GetTimeline();
        var startedAt = _controller.GetEmploymentStartedDayNumber()
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
        _loadGameButton.Text = exists ? "↻ Yeniden Yükle" : "↻ Yeniden Yükle (kayıt yok)";
    }

    private void RefreshTransferWindowStatus()
    {
        var window = _controller.GetTransferWindow();
        var openText = window.OpenedOnDayNumber is { } openDay
            ? GameDate.ToDisplayDateString(openDay)
            : null;
        var closeText = window.ClosesOnDayNumber is { } closeDay
            ? GameDate.ToDisplayDateString(closeDay)
            : null;
        _transferWindowLabel.Text = Application.Transfer.Queries.TransferFacingCopy.WindowLabel(
            window.IsOpen,
            openText,
            closeText);
        _openTransferWindowButton.Disabled = window.IsOpen;
        _closeTransferWindowButton.Disabled = !window.IsOpen;

        if (_transferWindowBadgeLabel is not null)
        {
            _transferWindowBadgeLabel.Text = window.IsOpen ? "🟢 Transfer Penceresi Açık" : "🔒 Pencere Kapalı";
            _transferWindowBadgeLabel.AddThemeColorOverride("font_color", window.IsOpen ? CareerUiTheme.ActionBright : CareerUiTheme.InkMuted);
            _transferWindowBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(window.IsOpen ? CareerUiTheme.ActionBright : CareerUiTheme.Stroke, 0.16f));
        }
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
        _transferScoutToggle.Text = visible ? "Pencere & bütçeyi gizle" : "Pencere & bütçe ayrıntısı";
    }

    private void RefreshTransferBudgetStatus()
    {
        var clubId = _controller.GetEmployedClubId();
        if (clubId is null)
        {
            _transferBudgetLabel.Text = "Transfer bütçesi: işsiz — kayıt yok.";
            if (_transferBudgetBadgeLabel is not null)
            {
                _transferBudgetBadgeLabel.Text = "💰 Bütçe: —";
                _wageBudgetBadgeLabel.Text = "💵 Maaş: —";
            }
            return;
        }

        var budget = _controller.GetTransferBudget(clubId.Value);
        var day = _controller.GetTimeline().CurrentDate;
        var wage = _controller.GetWageBudget(clubId.Value, day);
        var wageText = wage is null
            ? string.Empty
            : $" · Maaş: kullanılabilir {wage.Available:N0} / limit {wage.Limit:N0}"
              + $" (taahhüt {wage.Committed:N0}, rezerve {wage.Reserved:N0})";
        _transferBudgetLabel.Text =
            $"Transfer bütçesi: kullanılabilir {budget.Available:N0} / limit {budget.Limit:N0}"
            + $" (rezerve {budget.Reserved:N0}, harcanan {budget.Spent:N0})"
            + wageText;

        if (_transferBudgetBadgeLabel is not null)
        {
            _transferBudgetBadgeLabel.Text = $"💰 Transfer Bütçesi: {budget.Available:N0} TRY";
            _wageBudgetBadgeLabel.Text = wage is null ? "💵 Maaş: —" : $"💵 Maaş Bütçesi: {wage.Available:N0} TRY/hf";
        }
    }

    private void RefreshClubOfferStatus()
    {
        var offers = _controller.GetManagedClubOffers();
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
            $"Kulüp görüşmesi · tur {latest.Round} · {latest.OfferedFee:N0} TRY"
            + $" · {Application.Transfer.Queries.TransferFacingCopy.OfferStatusLabel(latest.StatusName, offers.PendingCount > 0)}"
            + (offers.PendingCount > 0 ? " · yanıt gerekli" : string.Empty);
        if (offers.PendingCount > 0)
        {
            _transferFeeInput.Value = latest.OfferedFee;
        }
    }

    private void RefreshContractProposalStatus()
    {
        var proposals = _controller.GetManagedContractProposals();
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
            $"Oyuncu görüşmesi · tur {latest.Round} · {latest.WeeklyWage:N0}/hafta"
            + $" · {latest.ContractYears} yıl · {Application.Transfer.Queries.TransferFacingCopy.ContractStatusLabel(latest.StatusName, proposals.PendingCount > 0)}"
            + (proposals.PendingCount > 0 ? " · yanıt gerekli" : string.Empty);
        if (proposals.PendingCount > 0)
        {
            _transferWageInput.Value = latest.WeeklyWage;
            _contractYearsInput.Value = latest.ContractYears;
        }
    }

    private void RefreshTransferProcessStatus()
    {
        var view = _controller.GetManagedClubProcesses();
        if (view.ClubId is null)
        {
            _transferProcessLabel.Text = "Aktif görüşme: kulüp görevi yok.";
            _advanceDealButton.Disabled = true;
            return;
        }

        if (view.ActiveCount == 0)
        {
            _transferProcessLabel.Text =
                "Aktif görüşme yok.\nÖnerilen listeden izlemeye al → Görüşme Başlat.";
            _advanceDealButton.Disabled = true;
            return;
        }

        var offers = _controller.GetManagedClubOffers();
        var proposals = _controller.GetManagedContractProposals();
        var preview = string.Join(
            "\n\n",
            view.ActiveProcesses.Take(2).Select(process =>
            {
                var tension = TransferTension(process.ProcessId, process.StatusCode);
                var hasPendingOffer = offers.PendingCount > 0
                    && offers.RecentOffers.Any(offer => offer.ProcessId == process.ProcessId);
                var hasPendingProposal = proposals.PendingCount > 0
                    && proposals.RecentProposals.Any(proposal => proposal.ProcessId == process.ProcessId);
                return Application.Transfer.Queries.TransferFacingCopy.DealLine(
                    _controller.GetPlayerDisplayName(process.PlayerId),
                    process.StatusCode,
                    tension,
                    hasPendingOffer,
                    hasPendingProposal)
                    + $"\n{TensionBar(tension)}";
            }));
        _transferProcessLabel.Text = $"{view.ActiveCount} aktif görüşme\n\n{preview}";
        _advanceDealButton.Disabled = false;
    }

    private int TransferTension(long processId, int statusCode)
    {
        var offerRounds = _controller.GetOfferRounds(processId)
            .Select(offer => offer.Round)
            .DefaultIfEmpty(0)
            .Max();
        var contractRounds = _controller.GetContractProposalRounds(processId)
            .Select(proposal => proposal.Round)
            .DefaultIfEmpty(0)
            .Max();
        var stagePressure = TransferStage(statusCode) * 8;
        return Math.Clamp(15 + stagePressure + (offerRounds + contractRounds) * 12, 15, 95);
    }

    private static int TransferStage(int statusCode) =>
        Application.Transfer.Queries.TransferFacingCopy.StageNumber(statusCode);

    private static string TensionBar(int tension)
    {
        var filled = Math.Clamp((int)Math.Round(tension / 20d), 1, 5);
        return new string('●', filled) + new string('○', 5 - filled);
    }

    private void RefreshOutgoingSaleStatus()
    {
        var capacity = _controller.BuildSquadCapacityDigest();
        if (!capacity.IsEmployed)
        {
            _outgoingSaleLabel.Text = "Giden oyuncu: kulüp görevi yok.";
            return;
        }

        var saleId = _controller.SuggestSaleCandidatePlayerId();
        var releaseId = _controller.SuggestReleaseCandidatePlayerId();
        var needs = _controller.GetManagedClubNeeds();
        var exitCount = needs.OpenNeeds.Count(need =>
            need.KindName.Contains("ayrılma", StringComparison.OrdinalIgnoreCase)
            || need.ReasonCode.StartsWith("PlayerExit:", StringComparison.Ordinal));

        var lines = new List<string>();
        if (saleId is long sid)
        {
            lines.Add(
                $"Satış adayı: {_controller.GetPlayerDisplayName(sid)}"
                + "\n· Kenar rotasyon / taşma / ayrılma isteği — Satışa Çıkar ile piyasaya sun.");
        }
        else
        {
            lines.Add("Satış adayı yok — kadro ince veya herkes kritik rolde.");
        }

        if (capacity.IsOverCapacity && releaseId is long rid)
        {
            var extras = capacity.OverflowPlayerIds.Count > 1
                ? $" (+{capacity.OverflowPlayerIds.Count - 1} diğer)"
                : string.Empty;
            lines.Add(
                $"Taşan sözleşme: {_controller.GetPlayerDisplayName(rid)}{extras}"
                + "\n· Yer Aç ile serbest bırakarak kapasite aç.");
        }
        else if (capacity.IsOverCapacity)
        {
            lines.Add($"Kadro taşması: {capacity.OverflowPlayerIds.Count} oyuncu — Yer Aç veya satış dene.");
        }

        if (exitCount > 0)
        {
            lines.Add($"{exitCount} açık ayrılma talebi — satış önceliği bu listeden gelir.");
        }

        _outgoingSaleLabel.Text = string.Join("\n\n", lines);
    }

    private void SetScoutScope(TransferMarketScope scope)
    {
        _scoutMarketScope = scope;
        RefreshShortlistTargetStatus();
    }

    private void UpdateScopeButtonStyles()
    {
        if (_scopeAllButton is null) return;
        _scopeAllButton.Text = _scoutMarketScope == TransferMarketScope.All ? "[🌍 TÜM DÜNYA]" : "🌍 Tüm Dünya";
        _scopeDomesticButton.Text = _scoutMarketScope == TransferMarketScope.Domestic ? "[🇹🇷 SÜPER LİG]" : "🇹🇷 Süper Lig";
        _scopeEuroButton.Text = _scoutMarketScope == TransferMarketScope.EuropeanElite ? "[⭐ AVRUPA ELİTİ]" : "⭐ Avrupa Eliti";
        _scopeFreeButton.Text = _scoutMarketScope == TransferMarketScope.FreeAgents ? "[🆓 SERBESTLER]" : "🆓 Serbestler";
    }

    private void RefreshShortlistTargetStatus()
    {
        UpdateScopeButtonStyles();
        var scout = _controller.BuildScoutTransferDigest(_scoutMarketScope);
        _scoutCandidates = scout.Candidates;
        _scoutReportLabel.Text = scout.HasClub
            ? $"{scout.Headline}\n{scout.NeedLine}"
            : scout.Headline;
        _scoutCandidateList.Clear();
        foreach (var candidate in scout.Candidates)
        {
            var value = candidate.MarketValue == 0
                ? "SERBEST"
                : candidate.MarketValue >= 1_000_000
                    ? $"{candidate.MarketValue / 1_000_000d:0.0}M"
                    : $"{candidate.MarketValue / 1_000d:0}K";
            var state = candidate.IsListedTarget ? " · ⭐" : candidate.IsShortlisted ? " · ✓" : string.Empty;
            _scoutCandidateList.AddItem(
                $"{candidate.PositionCode}  {candidate.DisplayName} · {candidate.EstimatedAbilityLow}-{candidate.EstimatedAbilityHigh} · {value}{state}");
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
            if (selected.candidate is not null)
            {
                BindScoutCandidateDetail(selected.candidate);
            }
        }

        var view = _controller.GetManagedClubShortlistTargets();
        if (view.ClubId is null)
        {
            _shortlistTargetLabel.Text = "Kısa liste/hedef: işsiz — kayıt yok.";
            return;
        }

        if (view.ActiveShortlistCount == 0 && view.ListedTargetCount == 0)
        {
            _shortlistTargetLabel.Text = "Kısa liste/hedef: boş — hedef öner (süreç açılmaz).";
            return;
        }

        var targetPreview = view.ListedTargets.Take(2)
            .Select(t => _controller.GetPlayerDisplayName(t.PlayerId))
            .ToArray();
        _shortlistTargetLabel.Text =
            $"Kısa liste {view.ActiveShortlistCount} · hedef {view.ListedTargetCount}"
            + (targetPreview.Length == 0 ? string.Empty : $" · {string.Join(" · ", targetPreview)}");
    }

    private void RefreshTransferNeedStatus()
    {
        var needs = _controller.GetManagedClubNeeds();
        if (needs.ClubId is null)
        {
            _transferNeedLabel.Text = "Transfer ihtiyacı: işsiz — kayıt yok.";
            return;
        }

        if (needs.OpenCount == 0)
        {
            _transferNeedLabel.Text = "Açık ihtiyaç yok — İhtiyaçları Tara veya İhtiyaç Oluştur.";
            return;
        }

        var preview = string.Join(
            "\n",
            needs.OpenNeeds.Take(3).Select(need =>
            {
                var reason = need.ReasonCode.StartsWith("ManualPositionGap:", StringComparison.Ordinal)
                    ? need.ReasonCode["ManualPositionGap:".Length..]
                    : need.ReasonCode.StartsWith("PlayerExit:", StringComparison.Ordinal)
                        ? _controller.GetPlayerDisplayName(
                            long.TryParse(need.ReasonCode["PlayerExit:".Length..], out var pid) ? pid : 0)
                        : need.ReasonCode;
                var reasonText = string.IsNullOrWhiteSpace(reason) || reason == "0" || reason == "ManualPositionGap"
                    ? string.Empty
                    : $" · {reason}";
                return $"· {need.KindName} (öncelik {need.Priority}){reasonText}";
            }));
        _transferNeedLabel.Text =
            $"{needs.OpenCount} açık ihtiyaç\n{preview}"
            + (needs.OpenCount > 3 ? "\n…" : string.Empty);
    }

    private void UpdateTransferNeedButtons(
        Application.ManagerCareer.Queries.ManagerCareerReadModel manager)
    {
        var employed = string.Equals(manager.EmploymentStatus, "Employed", StringComparison.Ordinal);
        var openCount = employed
            ? _controller.GetManagedClubNeeds().OpenCount
            : 0;
        var listedCount = employed
            ? _controller.GetManagedClubShortlistTargets().ListedTargetCount
            : 0;
        var canCreateNeed = employed && _controller.BuildSquadOverviewDigest().CanCreateTransferNeed;
        _refreshTransferNeedsButton.Disabled = !employed;
        _declareTransferNeedButton.Disabled = !employed;
        _closeTransferNeedButton.Disabled = !employed || openCount == 0;
        var windowOpen = _controller.IsTransferWindowOpen;
        _refreshTransferNeedsButton.Visible = employed && (windowOpen || openCount == 0);
        _declareTransferNeedButton.Visible = employed && (canCreateNeed || openCount == 0);
        _closeTransferNeedButton.Visible = !_closeTransferNeedButton.Disabled;
        var activeProcessCount = employed
            ? _controller.GetManagedClubProcesses().ActiveCount
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
            ? _controller.GetManagedClubProcesses().ActiveProcesses
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
            && _controller.GetManagedClubOffers().PendingCount > 0;
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
            && _controller.GetManagedContractProposals().PendingCount > 0;
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
        _requestFinancialApprovalButton.Visible = false;
        _grantFinancialApprovalButton.Visible = false;
        _rejectFinancialApprovalButton.Visible = false;
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
            _trainingLabel.Text = $"Plan seçilmedi{injuryHint}.";
            RefreshPreparationBriefing();
            return;
        }

        var injuryText = training.InjuredSlotCount > 0
            ? $" · sakat {training.InjuredSlotCount} (uygun değil {training.UnavailableSlotCount})"
            : string.Empty;
        _trainingLabel.Text =
            $"{FormatStoredIntensity(training.Intensity)} · {FormatStoredFocus(training.Focus)} · {FormatStoredRest(training.RestApproach)}"
            + $" · Fit %{training.AverageFitness}{injuryText}";
        RefreshPreparationBriefing();
    }

    private void RefreshMatchTrainingPriority()
    {
        var digest = _controller.BuildMatchTrainingPriorityDigest();
        var hasDueMatch = _controller.BuildNextMatchBriefing().HasMatch;
        var pendingPriority = _controller.PendingMatchTrainingPriority?.Priority;

        if (!digest.IsAvailable)
        {
            _matchTrainingPriorityLabel.Text = digest.Headline;
            foreach (var (priority, button) in _matchTrainingPriorityButtons)
            {
                button.Disabled = true;
                button.Text = "Seç";
                if (_matchPriorityRecBadges.TryGetValue(priority, out var recBadge))
                {
                    recBadge.Visible = false;
                }
                if (_matchPriorityCardPanels.TryGetValue(priority, out var panel))
                {
                    panel.AddThemeStyleboxOverride("panel", CareerUiTheme.MatchPriorityCardPanel());
                }
            }

            return;
        }

        var options = string.Join(
            "\n",
            digest.Options.Take(3).Select(option =>
                $"{(option.IsRecommended ? "★" : "·")} {option.Title}: "
                + $"{option.BoostLine} {option.RiskLine}"));
        _matchTrainingPriorityLabel.Text = CompactSummary(digest.Headline, digest.SquadStatusLine);
        _matchTrainingPriorityLabel.TooltipText = $"{digest.StaffFeedback}\n{options}";

        foreach (var option in digest.Options)
        {
            var isSelected = pendingPriority == option.Priority;
            var isRec = option.IsRecommended;

            if (_matchPriorityBonusLabels.TryGetValue(option.Priority, out var bonusLbl))
            {
                var sign = option.TemporaryMatchModifier > 0 ? "+" : string.Empty;
                bonusLbl.Text = $"{sign}{option.TemporaryMatchModifier} Maç Gücü";
                bonusLbl.AddThemeColorOverride("font_color", option.TemporaryMatchModifier >= 0 ? CareerUiTheme.ActionBright : CareerUiTheme.DangerSoft);
            }

            if (_matchPriorityCostLabels.TryGetValue(option.Priority, out var costLbl))
            {
                var fatigueDelta = option.ProjectedFatigueDelta;
                var fatigueText = fatigueDelta > 0
                    ? $"+{fatigueDelta} Yorgunluk"
                    : (fatigueDelta < 0 ? $"{fatigueDelta} Dinlenme" : "Yorgunluk Etkisiz");
                costLbl.Text = $"{fatigueText} · Sakatlık %{option.InjuryRiskDeltaPercent}";
            }

            if (_matchPriorityRecBadges.TryGetValue(option.Priority, out var recBadge))
            {
                recBadge.Visible = isRec;
            }

            if (_matchPriorityCardPanels.TryGetValue(option.Priority, out var panel))
            {
                panel.AddThemeStyleboxOverride("panel", CareerUiTheme.MatchPriorityCardPanel(isSelected, isRec));
            }

            if (_matchTrainingPriorityButtons.TryGetValue(option.Priority, out var button))
            {
                button.Disabled = !hasDueMatch;
                button.Text = isSelected ? "✓ Seçili" : "Seç";
                if (isSelected)
                {
                    CareerUiTheme.StylePrimaryButton(button);
                }
                else
                {
                    CareerUiTheme.StyleSecondaryButton(button);
                }
            }
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
        var tactic = _controller.GetManagedClubTacticPlan();
        if (tactic is null || tactic.ClubId is null)
        {
            _tacticLabel.Text = "Taktik: işsiz — plan yok.";
            if (_prepFitnessBadgeLabel is not null) _prepFitnessBadgeLabel.Text = "⚡ XI Zindelik: %—";
            if (_prepSharpnessBadgeLabel is not null) _prepSharpnessBadgeLabel.Text = "🎯 Maç Modifiyesi: —";
            if (_prepRiskBadgeLabel is not null) _prepRiskBadgeLabel.Text = "— Kadro Durumu";
            if (_prepApprovalBadgeLabel is not null) _prepApprovalBadgeLabel.Text = "— Onay";
            RefreshPreparationBriefing();
            RefreshMatchupPlan();
            RefreshPrepBoard();
            return;
        }

        if (string.Equals(tactic.FormationName, "yok", StringComparison.Ordinal))
        {
            _tacticLabel.Text = "Taktik: henüz yok — lig kur / formasyon seç.";
            if (_prepFitnessBadgeLabel is not null) _prepFitnessBadgeLabel.Text = "⚡ XI Zindelik: %—";
            if (_prepSharpnessBadgeLabel is not null) _prepSharpnessBadgeLabel.Text = "🎯 Maç Modifiyesi: —";
            if (_prepRiskBadgeLabel is not null) _prepRiskBadgeLabel.Text = "— Kadro Durumu";
            if (_prepApprovalBadgeLabel is not null) _prepApprovalBadgeLabel.Text = "— Onay";
            RefreshPreparationBriefing();
            RefreshMatchupPlan();
            RefreshPrepBoard();
            return;
        }

        var modifierLabel = _controller.GetManagedTacticModifierLabel();
        _tacticLabel.Text =
            $"{tactic.FormationName} · {tactic.ApproachName} · {tactic.PressingName} · {tactic.DefensiveLineName}";
        _tacticLabel.TooltipText = $"Maç etkisi: {modifierLabel}";
        var phase = _controller.BuildDualPhaseTacticDigest();
        _dualPhaseTacticLabel.Text = phase is null
            ? "Faz planı: klasik yerleşim kullanılıyor."
            : $"Faz planı: {phase.Headline}\n{phase.StaffNote}";

        // Zirve Zindelik (Peak Readiness) Telemetry Bar
        var board = _controller.BuildSquadSelectionBoard();
        if (_prepFitnessBadgeLabel is not null)
        {
            if (board.StartingXi.Count > 0)
            {
                var avgFit = (int)Math.Round(board.StartingXi.Average(p => p.Fitness));
                _prepFitnessBadgeLabel.Text = $"⚡ XI Zindelik: %{avgFit}";
            }
            else
            {
                _prepFitnessBadgeLabel.Text = "⚡ XI Zindelik: %—";
            }
        }

        if (_prepSharpnessBadgeLabel is not null)
        {
            _prepSharpnessBadgeLabel.Text = $"🎯 Maç Modifiyesi: {modifierLabel}";
        }

        if (_prepRiskBadgeLabel is not null && _prepRiskBadgePanel is not null)
        {
            var tiredCount = board.StartingXi.Concat(board.Bench).Count(p => p.Fitness < 70);
            var injuredCount = board.StartingXi.Concat(board.Bench).Count(p => !p.IsAvailable);
            if (injuredCount > 0 || tiredCount > 0)
            {
                _prepRiskBadgeLabel.Text = $"⚠️ {tiredCount} Yorgun · {injuredCount} Sakat";
                _prepRiskBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.DangerSoft);
                _prepRiskBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.DangerSoft, 0.20f));
            }
            else
            {
                _prepRiskBadgeLabel.Text = "✓ Kadro Zinde";
                _prepRiskBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Data);
                _prepRiskBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data, 0.20f));
            }
        }

        if (_prepApprovalBadgeLabel is not null && _prepApprovalBadgePanel is not null)
        {
            if (board.IsApproved)
            {
                _prepApprovalBadgeLabel.Text = "✓ Kadro Onaylı";
                _prepApprovalBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
                _prepApprovalBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
            }
            else
            {
                _prepApprovalBadgeLabel.Text = "⚠️ Onay Bekliyor";
                _prepApprovalBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ColorWarning);
                _prepApprovalBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning, 0.20f));
            }
        }

        RefreshPreparationBriefing();
        RefreshMatchupPlan();
        RefreshPrepBoard();
    }

    private void RefreshMatchupPlan()
    {
        if (_prepOpponentLabel is null || _prepMatchupLabel is null)
        {
            return;
        }

        var matchup = _controller.BuildMatchupPlan();
        var opponentDossier = _controller.BuildOpponentDossier();
        if (opponentDossier is null || matchup is null)
        {
            _prepOpponentLabel.Text = "Sıradaki maç planlanmadı veya rakip analizi bekleniyor.";
            _prepMatchupLabel.Text = "Eşleşme analizi için fikstür takvimi bekleniyor.";
            if (_prepOpponentThreatBadgeLabel is not null)
            {
                _prepOpponentThreatBadgeLabel.Text = "— Tehdit Belirsiz";
            }
            if (_prepMatchupSignalBadgeLabel is not null)
            {
                _prepMatchupSignalBadgeLabel.Text = "— Eşleşme Analizi";
            }
            return;
        }

        if (_prepOpponentThreatBadgeLabel is not null && _prepOpponentThreatBadgePanel is not null)
        {
            var (threatText, threatColor) = opponentDossier.ThreatKind switch
            {
                OpponentThreatKind.WinningStreak => ("🔥 Seri Galibiyet Tehdidi", CareerUiTheme.DangerSoft),
                OpponentThreatKind.ProductiveAttack => ("⚡ Üretken Hücum Hattı", CareerUiTheme.DangerSoft),
                OpponentThreatKind.SquadQuality => ("⭐ Yüksek Kadro Kalitesi", CareerUiTheme.DangerSoft),
                OpponentThreatKind.TopZoneTempo => ("🚀 Zirve Yarışı Temposu", CareerUiTheme.Accent),
                OpponentThreatKind.DefensiveResistance => ("🛡️ Dirençli Savunma Bloğu", CareerUiTheme.Accent),
                OpponentThreatKind.LosingStreak => ("📉 Kırılgan Form Grafiği", CareerUiTheme.ActionBright),
                _ => ("ℹ️ Dengeli Rakip Profili", CareerUiTheme.Data),
            };
            _prepOpponentThreatBadgeLabel.Text = threatText;
            _prepOpponentThreatBadgeLabel.AddThemeColorOverride("font_color", threatColor);
            _prepOpponentThreatBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(threatColor, 0.20f));
        }

        _prepOpponentLabel.Text = CompactSummary(
            opponentDossier.Headline,
            $"{opponentDossier.StandingLine} · {opponentDossier.FormLine}");
        _prepOpponentLabel.TooltipText = opponentDossier.ThreatLine;

        var (signalPrefix, signalBadgeText, signalColor) = matchup.Signal switch
        {
            Application.TeamPreparation.Queries.MatchupPlanSignal.Opportunity => (
                "✅ [FIRSAT / AVANTAJ]",
                "✅ Taktiksel Fırsat",
                CareerUiTheme.ActionBright),
            Application.TeamPreparation.Queries.MatchupPlanSignal.Risk => (
                "⚠️ [RİSK / TEHLİKE]",
                "⚠️ Taktiksel Risk",
                CareerUiTheme.DangerSoft),
            _ => (
                "ℹ️ [DENGELİ]",
                "ℹ️ Dengeli Eşleşme",
                CareerUiTheme.Data),
        };

        if (_prepMatchupSignalBadgeLabel is not null && _prepMatchupSignalBadgePanel is not null)
        {
            _prepMatchupSignalBadgeLabel.Text = signalBadgeText;
            _prepMatchupSignalBadgeLabel.AddThemeColorOverride("font_color", signalColor);
            _prepMatchupSignalBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(signalColor, 0.20f));
        }

        _prepMatchupLabel.Text = CompactSummary(matchup.BrandTitle, matchup.VerdictLine);
        _prepMatchupLabel.TooltipText = $"{signalPrefix} {matchup.SelectionLine}";
    }

    private void RefreshPrepBoard()
    {
        if (_prepPitchHost is null || _prepPitchCaptionLabel is null)
        {
            return;
        }

        MatchScreenUi.ClearChildren(_prepPitchHost);
        var board = _controller.BuildSquadSelectionBoard();
        var tactic = _controller.GetManagedClubTacticPlan();
        var formation = tactic?.Formation ?? Formation.F442;

        if (!board.HasMatch)
        {
            var empty = BodyLabel("PrepPitchEmpty", muted: true, autowrap: true);
            empty.Text = "Yaklaşan maç yok — lig fikstürü planlandığında taktik tahtası açılır.";
            _prepPitchHost.AddChild(empty);
            _prepPitchCaptionLabel.Text = "Maç takvimi bekleniyor.";
            if (_prepApproveSelectionButton is not null) _prepApproveSelectionButton.Disabled = true;
            if (_prepSwapSelectionButton is not null) _prepSwapSelectionButton.Disabled = true;
            return;
        }

        var selectedStillExists = board.StartingXi.Concat(board.Bench)
            .Any(player => player.SlotIndex == _prepSelectedSquadSlotIndex);
        if (!selectedStillExists)
        {
            _prepSelectedSquadSlotIndex = null;
            _prepSelectedSquadPlayerIsStarter = null;
        }

        _prepPitchCaptionLabel.Text = board.IsApproved
            ? "✓ Onaylı İlk 11 — Değişiklik için sahadan bir oyuncuya, ardından yedek kulübesine dokun."
            : "⚠️ Taslak İlk 11 — Oyuncuları dokunarak takas et ve 'Kadro Onayla' ile maça hazırla.";

        if (_prepApprovalBadgeLabel is not null && _prepApprovalBadgePanel is not null)
        {
            if (board.IsApproved)
            {
                _prepApprovalBadgeLabel.Text = "✓ Kadro Onaylı";
                _prepApprovalBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
                _prepApprovalBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.20f));
            }
            else
            {
                _prepApprovalBadgeLabel.Text = "⚠️ Onay Bekliyor";
                _prepApprovalBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ColorWarning);
                _prepApprovalBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning, 0.20f));
            }
        }

        if (_prepApproveSelectionButton is not null)
        {
            _prepApproveSelectionButton.Disabled = board.IsApproved;
            _prepApproveSelectionButton.Text = board.IsApproved ? "✓ Kadro Onaylandı" : "✓ Kadro Onayla";
            if (board.IsApproved)
            {
                CareerUiTheme.StyleSecondaryButton(_prepApproveSelectionButton);
            }
            else
            {
                CareerUiTheme.StyleAccentButton(_prepApproveSelectionButton);
            }
        }
        if (_prepSwapSelectionButton is not null)
        {
            _prepSwapSelectionButton.Disabled = false;
        }

        _prepPitchHost.AddChild(
            TacticalPitchBoardUi.Build(
                board,
                _prepSelectedSquadSlotIndex,
                selectPlayer: OnPrepPlayerSelected,
                swapPlayers: (starter, bench) => Apply(_controller.SwapStarterWithBenchForNextDueMatch(starter, bench)),
                interactionEnabled: true,
                formation: formation));
    }

    private void OnPrepPlayerSelected(Application.TeamPreparation.Queries.SquadSelectionPlayerDigest player)
    {
        if (_prepSelectedSquadSlotIndex == player.SlotIndex)
        {
            _prepSelectedSquadSlotIndex = null;
            _prepSelectedSquadPlayerIsStarter = null;
        }
        else if (_prepSelectedSquadSlotIndex is null || _prepSelectedSquadPlayerIsStarter == player.IsStarter)
        {
            _prepSelectedSquadSlotIndex = player.SlotIndex;
            _prepSelectedSquadPlayerIsStarter = player.IsStarter;
        }
        else
        {
            var starter = player.IsStarter ? player.SlotIndex : _prepSelectedSquadSlotIndex.Value;
            var bench = player.IsStarter ? _prepSelectedSquadSlotIndex.Value : player.SlotIndex;
            _prepSelectedSquadSlotIndex = null;
            _prepSelectedSquadPlayerIsStarter = null;
            Apply(_controller.SwapStarterWithBenchForNextDueMatch(starter, bench));
            return;
        }

        RefreshPrepBoard();
    }

    private void UpdateTacticButtons(
        Application.ManagerCareer.Queries.ManagerCareerReadModel manager)
    {
        var employed = string.Equals(manager.EmploymentStatus, "Employed", StringComparison.Ordinal);
        _formationPickerButton.Disabled = !employed;
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

        var tactic = _controller.GetManagedClubTacticPlan();
        if (_formationPickerButton is not null)
        {
            if (tactic is not null && tactic.ClubId is not null)
            {
                _formationPickerButton.Text = $"📋 Diziliş: {FormationPickerModal.FormatLabel(tactic.Formation)} (Tümü)";
            }
            else
            {
                _formationPickerButton.Text = "📋 Tüm Dizilişler (14)";
            }
        }
        _formation442Button.ButtonPressed = tactic is not null && tactic.ClubId is not null && tactic.Formation == Formation.F442;
        _formation433Button.ButtonPressed = tactic is not null && tactic.ClubId is not null && tactic.Formation == Formation.F433;
        _formation352Button.ButtonPressed = tactic is not null && tactic.ClubId is not null && tactic.Formation == Formation.F352;
        _approachBalancedButton.ButtonPressed =
            tactic is not null && tactic.ClubId is not null && tactic.Approach == TacticalApproach.Balanced;
        _approachAttackingButton.ButtonPressed =
            tactic is not null && tactic.ClubId is not null && tactic.Approach == TacticalApproach.Attacking;
        _approachDefensiveButton.ButtonPressed =
            tactic is not null && tactic.ClubId is not null && tactic.Approach == TacticalApproach.Defensive;
        _pressingLowButton.ButtonPressed =
            tactic is not null && tactic.ClubId is not null && tactic.Pressing == PressingIntensity.LowBlock;
        _pressingBalancedButton.ButtonPressed =
            tactic is not null && tactic.ClubId is not null && tactic.Pressing == PressingIntensity.Balanced;
        _pressingHighButton.ButtonPressed =
            tactic is not null && tactic.ClubId is not null && tactic.Pressing == PressingIntensity.HighPress;
        _lineDeepButton.ButtonPressed =
            tactic is not null && tactic.ClubId is not null && tactic.DefensiveLine == DefensiveLine.Deep;
        _lineStandardButton.ButtonPressed =
            tactic is not null && tactic.ClubId is not null && tactic.DefensiveLine == DefensiveLine.Standard;
        _lineHighButton.ButtonPressed =
            tactic is not null && tactic.ClubId is not null && tactic.DefensiveLine == DefensiveLine.High;

        var isShockPress = tactic is not null && tactic.ClubId is not null
            && tactic.Formation == Formation.F433
            && tactic.Approach == TacticalApproach.Attacking
            && tactic.Pressing == PressingIntensity.HighPress
            && tactic.DefensiveLine == DefensiveLine.High;

        var isParkTheBus = tactic is not null && tactic.ClubId is not null
            && tactic.Formation == Formation.F442
            && tactic.Approach == TacticalApproach.Defensive
            && tactic.Pressing == PressingIntensity.LowBlock
            && tactic.DefensiveLine == DefensiveLine.Deep;

        var isFastCounter = tactic is not null && tactic.ClubId is not null
            && tactic.Formation == Formation.F352
            && tactic.Approach == TacticalApproach.Balanced
            && tactic.Pressing == PressingIntensity.HighPress
            && tactic.DefensiveLine == DefensiveLine.Standard;

        var isPatientControl = tactic is not null && tactic.ClubId is not null
            && tactic.Formation == Formation.F433
            && tactic.Approach == TacticalApproach.Balanced
            && tactic.Pressing == PressingIntensity.Balanced
            && tactic.DefensiveLine == DefensiveLine.Standard;

        if (_scenarioShockPressBtn is not null)
        {
            _scenarioShockPressBtn.Disabled = !employed;
            if (isShockPress) CareerUiTheme.StylePrimaryButton(_scenarioShockPressBtn);
            else CareerUiTheme.StyleSecondaryButton(_scenarioShockPressBtn);
        }
        if (_scenarioParkTheBusBtn is not null)
        {
            _scenarioParkTheBusBtn.Disabled = !employed;
            if (isParkTheBus) CareerUiTheme.StylePrimaryButton(_scenarioParkTheBusBtn);
            else CareerUiTheme.StyleSecondaryButton(_scenarioParkTheBusBtn);
        }
        if (_scenarioFastCounterBtn is not null)
        {
            _scenarioFastCounterBtn.Disabled = !employed;
            if (isFastCounter) CareerUiTheme.StylePrimaryButton(_scenarioFastCounterBtn);
            else CareerUiTheme.StyleSecondaryButton(_scenarioFastCounterBtn);
        }
        if (_scenarioPatientControlBtn is not null)
        {
            _scenarioPatientControlBtn.Disabled = !employed;
            if (isPatientControl) CareerUiTheme.StylePrimaryButton(_scenarioPatientControlBtn);
            else CareerUiTheme.StyleSecondaryButton(_scenarioPatientControlBtn);
        }
    }

    private void RefreshDevelopmentStatus()
    {
        var development = _controller.GetManagedClubDevelopmentSummary();
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
        var contracts = _controller.GetManagedClubContractSummary();
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
        var memories = _controller.GetActiveMemoriesForManager(take: 5);

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
        var promises = _controller.GetActivePromisesForManager(take: 5);

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
        var relationships = _controller.GetActiveRelationshipsForManager(take: 5);

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
        var pending = _controller.GetPendingDecisions(take: 5);
        var desk = _controller.BuildDecisionDeskDigest();
        _deskLabel.Text = CompactSummary(desk.Headline, desk.SupportingLine ?? string.Empty);

        if (pending.OpenCount == 0)
        {
            _decisionCausalityPanel.Visible = false;
            _decisionLabel.Text = "Kararlar: masada bekleyen yok — Karar Masası kartından yanıtla.";
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
        _decisionCausalityPanel.Visible = true;
        _decisionCausalityLabel.Text = string.IsNullOrWhiteSpace(desk.CausalityLine)
            ? "❔ BU NEDEN OLDU? Kararın bağlamı henüz oluşmadı; talep türü ve son tarih üzerinden değerlendir."
            : $"❔ BU NEDEN OLDU?  {desk.CausalityLine}";
        var causalityColor = desk.IsHardBlocker ? CareerUiTheme.DangerSoft : CareerUiTheme.ColorWarning;
        _decisionCausalityPanel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.BadgePillPanel(causalityColor, 0.24f));
        _decisionCausalityLabel.AddThemeColorOverride("font_color", causalityColor);
        var options = _controller.GetDialogueOptionsForDecision(first.DecisionRequestId);
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
            _grantDecisionButton.Text = $"✓ {grant.DisplayText}";
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
            _refuseDecisionButton.Text = $"✕ {refuse.DisplayText}";
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
            $"Kararlar: {pending.OpenCount} açık — Karar Masası kartından yanıtla. {desk.SupportingLine}";

        var awaiting = _controller.GetAwaitingDecisionSessionCount();
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
            && _controller.HasSignableFreeAgent();
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
        var pulseLines = pulse.PulseLines.Take(2).ToArray();
        var pulseSupport = pulseLines.Length > 0
            ? string.Join(" · ", pulseLines)
            : string.Empty;
        _pulseLabel.Text = CompactSummary(pulse.Headline, pulseSupport);

        var fanPulse = _controller.BuildFanPulseDigest();
        var fanColor = fanPulse.ApprovalRating switch
        {
            >= 70 => CareerUiTheme.ActionBright,
            >= 40 => CareerUiTheme.ColorWarning,
            _ => CareerUiTheme.DangerSoft,
        };
        _fanApprovalBadgeLabel.Text = $"♥ %{fanPulse.ApprovalRating}  {fanPulse.Trend}";
        _fanApprovalBadgeLabel.AddThemeColorOverride("font_color", fanColor);
        _fanApprovalBadgePanel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.BadgePillPanel(fanColor, 0.20f));
        var visiblePosts = fanPulse.Posts.Take(_compactLayout ? 1 : 2);
        _fanFeedLabel.Text = fanPulse.Posts.Count == 0
            ? fanPulse.Headline
            : string.Join(
                "\n",
                visiblePosts.Select(post => $"{post.Handle}  {post.Message}  {post.Tag}"));
        var weekStory = _controller.BuildWeekStory();
        var weekMood = _controller.BuildWeekMood(weekStoryActive: weekStory.IsActive);
        if (weekStory.IsActive && !string.IsNullOrWhiteSpace(weekStory.StoryLine))
        {
            _weekStoryLabel.Text = $"📖 {weekStory.StoryLine}";
            _weekStoryLabel.Visible = true;
        }
        else
        {
            _weekStoryLabel.Visible = false;
            _weekStoryLabel.Text = string.Empty;
        }

        var recoveryPath = _controller.BuildInjuryRecoveryPath();
        if (recoveryPath.IsActive && !string.IsNullOrWhiteSpace(recoveryPath.Headline))
        {
            _recoveryPathLabel.Text = $"🏥 {recoveryPath.Headline}";
            _recoveryPathLabel.Visible = true;
        }
        else
        {
            _recoveryPathLabel.Visible = false;
            _recoveryPathLabel.Text = string.Empty;
        }


        var currentDay = _controller.GetCurrentGameDate().DayNumber;
        var pending = _controller.GetNextDueManagedFixture(currentDay);
        var hasDueContinental = _controller.HasDueContinentalMatch();
        var duePlayable = (pending is { IsApproved: true }) || hasDueContinental;
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
        var currentDay = _controller.GetCurrentGameDate().DayNumber;
        var pending = _controller.GetNextDueManagedFixture(currentDay);
        var hasDueEuro = _controller.HasDueContinentalMatch();

        var briefing = _controller.BuildNextMatchBriefing();
        _briefingLabel.Text = CompactSummary(
            briefing.HasMatch && !string.IsNullOrWhiteSpace(briefing.FixtureLine)
                ? briefing.FixtureLine
                : briefing.Headline,
            briefing.HasMatch ? briefing.Headline : string.Empty);

        if (_nextMatchPill is not null && _nextMatchLabel is not null)
        {
            if (briefing.HasMatch)
            {
                var matchLine = !string.IsNullOrWhiteSpace(briefing.FixtureLine)
                    ? briefing.FixtureLine
                    : briefing.Headline;
                _nextMatchLabel.Text = $"⚔ {matchLine}";
                _nextMatchPill.Visible = true;
                _nextMatchPill.TooltipText = briefing.Headline;
            }
            else
            {
                _nextMatchPill.Visible = false;
            }
        }

        // Populate Bento Hero Card details
        if (_nextMatchOpponentLabel is not null)
        {
            if (briefing.HasMatch)
            {
                if (hasDueEuro)
                {
                    _nextMatchCompetitionBadge.Text = "⭐ AVRUPA KUPASI";
                    _nextMatchCompetitionBadge.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
                    _nextMatchVenueLabel.Text = "AVRUPA GECESİ";
                    _nextMatchOpponentLabel.Text = briefing.Headline;
                    _nextMatchApprovalLabel.Text = "⭐ Avrupa Maçına Hazır";
                    _nextMatchApprovalLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
                    _nextMatchApprovalPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent));
                }
                else if (pending is not null)
                {
                    _nextMatchCompetitionBadge.Text = "🏆 LİG MAÇI";
                    _nextMatchCompetitionBadge.AddThemeColorOverride("font_color", CareerUiTheme.Data);
                    _nextMatchVenueLabel.Text = pending.IsHome ? "🏠 İÇ SAHA" : "✈️ DEPLASMAN";
                    var oppName = _controller.GetClubDisplayName(pending.OpponentClubId);
                    _nextMatchOpponentLabel.Text = $"vs {oppName}";

                    if (pending.IsApproved)
                    {
                        _nextMatchApprovalLabel.Text = "✓ Kadro Onaylandı";
                        _nextMatchApprovalLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
                        _nextMatchApprovalPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright));
                    }
                    else
                    {
                        _nextMatchApprovalLabel.Text = "⚠️ Kadro Onayı Bekliyor";
                        _nextMatchApprovalLabel.AddThemeColorOverride("font_color", CareerUiTheme.ColorWarning);
                        _nextMatchApprovalPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ColorWarning));
                    }
                }
                else
                {
                    _nextMatchCompetitionBadge.Text = "⚽ MÜSABAKA";
                    _nextMatchCompetitionBadge.AddThemeColorOverride("font_color", CareerUiTheme.Data);
                    _nextMatchVenueLabel.Text = "STADYUM";
                    _nextMatchOpponentLabel.Text = briefing.Headline;
                    _nextMatchApprovalLabel.Text = "Hazırlık";
                    _nextMatchApprovalLabel.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
                    _nextMatchApprovalPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Stroke));
                }
            }
            else
            {
                _nextMatchCompetitionBadge.Text = "HAZIRLIK DÖNEMİ";
                _nextMatchCompetitionBadge.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
                _nextMatchVenueLabel.Text = "—";
                _nextMatchOpponentLabel.Text = "Planlanmış Maç Yok";
                _nextMatchApprovalLabel.Text = "Serbest";
                _nextMatchApprovalLabel.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
                _nextMatchApprovalPill.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Stroke));
            }
        }

        RefreshTodayPulse();

        if (pending is null && !hasDueEuro)
        {
            _selectionLabel.Text = "Kadro: vadesi gelmiş maç yok.";
            _approveSelectionButton.Disabled = true;
            _swapSelectionButton.Disabled = true;
            return;
        }

        if (pending is not null)
        {
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
    }

    private void RefreshTodayStandingsWidget()
    {
        if (_miniStandingsContainer is null)
        {
            return;
        }

        foreach (var child in _miniStandingsContainer.GetChildren())
        {
            child.QueueFree();
        }

        var season = _controller.GetCurrentSeason();
        if (season is null)
        {
            var emptyLabel = BodyLabel("MiniStandingsEmpty", muted: true);
            emptyLabel.Text = "Aktif lig sezonu bulunmuyor.";
            _miniStandingsContainer.AddChild(emptyLabel);
            return;
        }

        var managedClubId = _controller.GetEmployedClubId();
        var strip = _controller.GetStandingsStrip(season.SeasonId, managedClubId, topCount: 5);
        if (strip.Entries.Count == 0)
        {
            var emptyLabel = BodyLabel("MiniStandingsEmpty", muted: true);
            emptyLabel.Text = "Henüz puan durumu oluşmadı.";
            _miniStandingsContainer.AddChild(emptyLabel);
            return;
        }

        var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headerRow.AddThemeConstantOverride("separation", 6);
        var hRank = BodyLabel("H_Rank", muted: true);
        hRank.Text = "#";
        hRank.CustomMinimumSize = new Vector2(24, 0);
        headerRow.AddChild(hRank);

        var hClub = BodyLabel("H_Club", muted: true);
        hClub.Text = "TAKIM";
        hClub.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        headerRow.AddChild(hClub);

        var hPlayed = BodyLabel("H_Played", muted: true);
        hPlayed.Text = "OM";
        hPlayed.CustomMinimumSize = new Vector2(28, 0);
        hPlayed.HorizontalAlignment = HorizontalAlignment.Right;
        headerRow.AddChild(hPlayed);

        var hPoints = BodyLabel("H_Points", muted: true);
        hPoints.Text = "P";
        hPoints.CustomMinimumSize = new Vector2(32, 0);
        hPoints.HorizontalAlignment = HorizontalAlignment.Right;
        headerRow.AddChild(hPoints);
        _miniStandingsContainer.AddChild(headerRow);

        foreach (var entry in strip.Entries)
        {
            var rowPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            rowPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.MiniStandingsRowPanel(entry.IsManaged));

            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 6);
            rowPanel.AddChild(row);

            var rankLabel = BodyLabel($"Rank_{entry.Rank}");
            rankLabel.Text = entry.Rank.ToString();
            rankLabel.CustomMinimumSize = new Vector2(24, 0);
            if (entry.IsManaged)
            {
                rankLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
                CareerUiTheme.ApplyDisplayFont(rankLabel);
            }
            row.AddChild(rankLabel);

            var clubLabel = BodyLabel($"Club_{entry.ClubId}");
            var name = _controller.GetClubDisplayName(entry.ClubId);
            clubLabel.Text = entry.IsManaged ? $"★ {name}" : name;
            clubLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            clubLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            if (entry.IsManaged)
            {
                clubLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
                CareerUiTheme.ApplyDisplayFont(clubLabel);
            }
            row.AddChild(clubLabel);

            var playedLabel = BodyLabel($"Played_{entry.Rank}", muted: !entry.IsManaged);
            playedLabel.Text = entry.Played.ToString();
            playedLabel.CustomMinimumSize = new Vector2(28, 0);
            playedLabel.HorizontalAlignment = HorizontalAlignment.Right;
            row.AddChild(playedLabel);

            var pointsLabel = BodyLabel($"Pts_{entry.Rank}");
            pointsLabel.Text = entry.Points.ToString();
            pointsLabel.CustomMinimumSize = new Vector2(32, 0);
            pointsLabel.HorizontalAlignment = HorizontalAlignment.Right;
            if (entry.IsManaged)
            {
                pointsLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
                CareerUiTheme.ApplyDisplayFont(pointsLabel);
            }
            row.AddChild(pointsLabel);

            _miniStandingsContainer.AddChild(rowPanel);
        }
    }

    private void RefreshTodayTacticsWidget()
    {
        if (_todayTacticsSummaryLabel is null || _todayFitnessSummaryLabel is null)
        {
            return;
        }

        var tactic = _controller.GetManagedClubTacticPlan();
        if (tactic is null || tactic.ClubId is null || string.Equals(tactic.FormationName, "yok", StringComparison.Ordinal))
        {
            _todayTacticsSummaryLabel.Text = "⚽ Taktik: Henüz belirlenmedi";
        }
        else
        {
            _todayTacticsSummaryLabel.Text = $"⚽ {tactic.FormationName} · {tactic.ApproachName} · {tactic.PressingName}";
        }

        var training = _controller.GetTrainingSummary();
        if (training.ClubId is null || !training.HasPlan)
        {
            _todayFitnessSummaryLabel.Text = "⚡ Kondisyon: Plan yok · Varsayılan değerler";
        }
        else
        {
            var injuryText = training.InjuredSlotCount > 0 ? $" · Sakat: {training.InjuredSlotCount}" : " · Sakat Yok";
            _todayFitnessSummaryLabel.Text = $"⚡ XI Kondisyon: %{training.AverageFitness} · Yorgunluk: %{training.AverageFatigue}{injuryText}";
        }
    }

    private void UpdatePrimaryHints(int dueMatchCount, bool canAdvance)
    {
        var currentDay = _controller.GetCurrentGameDate().DayNumber;
        var pending = _controller.GetNextDueManagedFixture(currentDay);
        var selectionBlocksPlay = pending is not null && !pending.IsApproved;

        if (_controller.HasDueContinentalMatch())
        {
            _playButton.Disabled = false;
            _playButton.Text = "⭐ Avrupa Maçına Çık";
            CareerUiTheme.StyleAccentButton(_playButton);
        }
        else
        {
            _playButton.Disabled = dueMatchCount == 0;
            var tension = _controller.GetPromiseTensionForNextDueMatch(currentDay);
            var atRisk = tension is
            {
                HasTension: true,
                ToneCode: Application.TeamPreparation.Services.PreMatchPromiseTensionQueryService.ToneAtRisk
            };

            _playButton.Text = dueMatchCount == 0
                ? "Maç Gününe Git"
                : selectionBlocksPlay
                    ? $"✓ Kadro Onayla & Maça Git ({dueMatchCount})"
                    : atRisk
                        ? $"Maç Gününe Git ({dueMatchCount}) · söz riski"
                        : $"Maç Gününe Git ({dueMatchCount})";
            CareerUiTheme.StylePrimaryButton(_playButton);
        }

        _advanceDayButton.Disabled = !canAdvance;
        _advanceWeekButton.Disabled = !canAdvance;
        if (canAdvance)
        {
            _advanceDayButton.Text = "1 Gün İlerlet";
            _advanceDayButton.TooltipText = string.Empty;
            _advanceWeekButton.Text = "7 Gün İlerlet";
            _advanceWeekButton.TooltipText = string.Empty;
        }
        else
        {
            var blocker = _controller.BuildTimeAdvanceBlockerDigest();
            _advanceDayButton.Text = "1 Gün İlerlet (kilitli)";
            _advanceDayButton.TooltipText = blocker.AdviceLine;
            _advanceWeekButton.Text = "7 Gün İlerlet (kilitli)";
            _advanceWeekButton.TooltipText = blocker.AdviceLine;
        }

        var canTransition = _controller.CanTransitionToNextSeason();
        _seasonTransitionButton.Disabled = !canTransition;
        var season = _controller.GetCurrentSeason();
        var progress = season is null
            ? null
            : _controller.GetSeasonProgress(season.SeasonId);
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
        var hasOpenDecision = _controller.GetPendingDecisions(take: 1).OpenCount > 0;
        var currentDay = _controller.GetCurrentGameDate().DayNumber;
        var pendingMatch = _controller.GetNextDueManagedFixture(currentDay);
        var hasContinentalMatch = _controller.HasDueContinentalMatch();
        var canAdvance = _controller.CanAdvanceTime();

        // Bu eylemlerin tamamı OfficeNextStep tarafından zaten kapsanır.
        _approveSelectionButton.Visible = false;
        _seasonTransitionButton.Visible = false;
        _advanceWeekButton.Visible = false;

        // Açık karar varken aynı kartta yalnızca gerçek diyalog seçenekleri kalır.
        _officeNextStepButton.Visible = _officeNextStepButton.Visible && !hasOpenDecision;
        _playButton.Visible =
            !hasOpenDecision
            && (pendingMatch is not null || hasContinentalMatch)
            && !_officeNextStepButton.Visible;
        _swapSelectionButton.Visible = !hasOpenDecision && pendingMatch is not null;
        _advanceDayButton.Visible =
            !hasOpenDecision
            && canAdvance
            && pendingMatch is null
            && !hasContinentalMatch
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
        var overview = _controller.BuildSquadOverviewDigest();
        _squadCapacityLabel.Text = overview.CapacityLine;
        _clubOverviewHeadlineLabel.Text = overview.Headline;
        _clubDepthLabel.Text = overview.DepthLine;
        _clubPitchCaptionLabel.Text = overview.PitchCaption;
        RefreshClubSignals(overview);

        var manager = _controller.GetManagerCareer();
        if (manager.EmployedClubId is not long clubId)
        {
            _squadStatusLabel.Text = "A takım: işsiz — kayıt yok.";
            _playerManagementPlayers = Array.Empty<PlayerManagementLine>();
            _playerManagementHeadlineLabel.Text = "Futbolcu yönetimi: kulüp görevi yok.";
            _selectedPlayerId = null;
            RefreshPlayerDetail();
            RefreshClubPitch(Array.Empty<PlayerManagementLine>());

            _squadOvrBadgeLabel.Text = "⭐ Güç —";
            _squadCapacityBadgeLabel.Text = "👥 Kadro: 0 / 25";
            _squadAvgAgeBadgeLabel.Text = "🎂 Yaş —";
            _squadTotalValueBadgeLabel.Text = "💰 Değer —";
            _squadWageBillBadgeLabel.Text = "💵 Maaş —";
            for (var i = 0; i < 3; i++)
            {
                _starCards[i].Visible = false;
            }
            return;
        }

        var persisted = _controller.GetPersistedSquad(clubId);
        var clubName = _controller.GetClubDisplayName(clubId);
        _squadStatusLabel.Text = persisted is null || persisted.Members.Count == 0
            ? "A takım: henüz yok — lig kur / gün ilerle / antrenman ile oluşur."
            : $"A takım: {persisted.Members.Count} üye · {clubName}";

        var management = _controller.BuildPlayerManagementDigest();

        // 1. Kadro Telemetrisi
        var squadCount = management.Players.Count;
        var avgRating = squadCount > 0 ? (int)Math.Round(management.Players.Average(p => p.Rating)) : 0;
        var avgAge = squadCount > 0 ? management.Players.Average(p => p.Age) : 0;
        var totalWage = management.Players.Sum(p => p.WeeklyWage ?? 0);
        var totalMarketValue = management.Players.Sum(p => (long)EstimateSquadPlayerMarketValue(p, avgRating));

        _squadOvrBadgeLabel.Text = $"⭐ Güç {avgRating}";
        _squadCapacityBadgeLabel.Text = $"👥 {squadCount}/{capacity.MaxMembers}";
        _squadAvgAgeBadgeLabel.Text = $"🎂 {avgAge:0.0}";
        _squadTotalValueBadgeLabel.Text = totalMarketValue >= 1_000_000
            ? $"💰 {(double)totalMarketValue / 1_000_000:0.0}M"
            : $"💰 {totalMarketValue:N0}";
        _squadWageBillBadgeLabel.Text = totalWage >= 1_000_000
            ? $"💵 {(double)totalWage / 1_000_000:0.00}M/hf"
            : $"💵 {totalWage:N0}/hf";

        // 2. Yıldız Oyuncu Vitrini (Top 3 Stars)
        var topStars = management.Players
            .OrderByDescending(p => p.Rating)
            .ThenByDescending(p => p.CurrentAbility)
            .ThenByDescending(p => p.PotentialAbility)
            .Take(3)
            .ToList();

        for (var i = 0; i < 3; i++)
        {
            if (i < topStars.Count)
            {
                var star = topStars[i];
                _starPlayerIds[i] = star.PlayerId;
                _starPosBadges[i].Text = $"[{star.PositionCode}]";
                _starPosBadges[i].AddThemeColorOverride("font_color", CareerUiTheme.PositionGroupColor(star.PositionCode));
                _starRatingBadges[i].Text = $"⭐ {star.Rating} GÜÇ";
                _starNameLabels[i].Text = star.DisplayName;
                var starVal = EstimateSquadPlayerMarketValue(star, avgRating);
                var valStr = starVal >= 1_000_000 ? $"{(double)starVal / 1_000_000:0.0}M TRY" : $"{starVal:N0} TRY";
                _starMetaLabels[i].Text = $"{star.Age} Yaş · %{star.Fitness} Fit · {valStr}";
                _starCards[i].Visible = !_compactLayout || i == 0;
            }
            else
            {
                _starPlayerIds[i] = null;
                _starCards[i].Visible = false;
            }
        }

        // 3. Kadro SWOT & Durum Raporu Masası
        PopulateSwotReport(management, overview, capacity, avgRating);

        // 4. Kadro Listesi Filtreleme ve Formatlama
        var visiblePlayers = FilterSquadPlayers(management.Players, _squadListFilterCode, _activeSquadCategoryFilter);
        _playerManagementPlayers = visiblePlayers;
        _playerManagementHeadlineLabel.Text = string.IsNullOrWhiteSpace(_squadListFilterCode)
            ? $"{visiblePlayers.Count} oyuncu"
            : $"{_squadListFilterCode} · {visiblePlayers.Count} oyuncu";

        foreach (var player in visiblePlayers)
        {
            var availabilityIndicator = player.Availability == "Hazır"
                ? string.Empty
                : $" · ⚠ {player.Availability}";

            var newBadge = player.PlayerId == _newTransferPlayerId ? " · YENİ" : string.Empty;
            var formattedLine = $"#{player.SquadNumber}  {player.PositionCode}  {player.DisplayName}  ·  ⭐ {player.Rating}  ·  %{player.Fitness}{newBadge}{availabilityIndicator}";
            _squadList.AddItem(formattedLine);
            if (player.PlayerId == _newTransferPlayerId)
            {
                _squadList.SetItemCustomFgColor(_squadList.ItemCount - 1, CareerUiTheme.ActionBright);
            }
        }

        if (capacity.IsOverCapacity
            && (_squadListFilterCode is null
                || _squadListFilterCode == Application.CareerHub.Queries.SquadOverviewDigest.SignalOverflow))
        {
            foreach (var id in capacity.OverflowPlayerIds)
            {
                _squadList.AddItem($"[kadro dışı sözleşme] {_controller.GetPlayerDisplayName(id)}");
            }
        }

        if (_selectedPlayerId is not long selectedId
            || !visiblePlayers.Any(player => player.PlayerId == selectedId))
        {
            _selectedPlayerId = visiblePlayers.FirstOrDefault()?.PlayerId;
        }

        if (_selectedPlayerId is long activePlayerId)
        {
            var selectedIndex = visiblePlayers
                .Select((player, index) => (player, index))
                .FirstOrDefault(item => item.player.PlayerId == activePlayerId).index;
            if (selectedIndex >= 0 && selectedIndex < visiblePlayers.Count)
            {
                _squadList.Select(selectedIndex);
            }
        }

        RefreshPlayerDetail();
        RefreshClubPitch(management.Players);
        if (_clubSubPage == ClubSubPage.Plan)
        {
            RefreshClubPlanBoard();
        }

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
    }

    private void RefreshSquadStarVisibility()
    {
        for (var i = 0; i < _starCards.Length; i++)
        {
            if (_starCards[i] is not null)
            {
                _starCards[i].Visible = _starPlayerIds[i].HasValue && (!_compactLayout || i == 0);
            }
        }
    }

    private void PopulateSwotReport(
        PlayerManagementDigest management,
        SquadOverviewDigest overview,
        Application.TeamPreparation.Queries.SquadCapacityDigest capacity,
        int avgRating)
    {
        while (_swotStrengthsVBox.GetChildCount() > 1)
        {
            var child = _swotStrengthsVBox.GetChild(1);
            _swotStrengthsVBox.RemoveChild(child);
            child.QueueFree();
        }

        while (_swotWeaknessesVBox.GetChildCount() > 1)
        {
            var child = _swotWeaknessesVBox.GetChild(1);
            _swotWeaknessesVBox.RemoveChild(child);
            child.QueueFree();
        }

        if (avgRating >= 75)
        {
            AddSwotItem(_swotStrengthsVBox, $"✓ Kadro reyting ortalaması ({avgRating}) lig standartlarında üst düzey rekabetçi seviyede.", CareerUiTheme.ActionBright);
        }
        else
        {
            AddSwotItem(_swotStrengthsVBox, $"✓ Kadro genelinde yüksek dinamizm ve gelişim potansiyeli hakim ({avgRating} OVR).", CareerUiTheme.ActionBright);
        }

        var youngPlayers = management.Players.Count(p => p.Age <= 23);
        if (youngPlayers > 0)
        {
            AddSwotItem(_swotStrengthsVBox, $"✓ As takımda {youngPlayers} adet 23 yaş ve altı gelecek vadeden genç yetenek mevcut.", CareerUiTheme.ActionBright);
        }

        var topGk = management.Players.Where(p => p.PositionCode is "KL" or "GK").OrderByDescending(p => p.Rating).FirstOrDefault();
        if (topGk is not null && topGk.Rating >= 72)
        {
            AddSwotItem(_swotStrengthsVBox, $"✓ Kalede {topGk.DisplayName} ({topGk.Rating} GÜÇ) ile güven veren 1. kaleci hattı.", CareerUiTheme.ActionBright);
        }

        if (management.Players.Count is >= 20 and <= 25)
        {
            AddSwotItem(_swotStrengthsVBox, $"✓ Kadro büyüklüğü ({management.Players.Count}/25) ideal operasyonel rotasyon dengesinde.", CareerUiTheme.ActionBright);
        }

        var hasWeakness = false;
        if (overview.Signals.Any(s => s.Code == SquadOverviewDigest.SignalDepth))
        {
            AddSwotItem(_swotWeaknessesVBox, "✕ Mevki derinliğinde boşluk tespit edildi: Rotasyon alternatifleri kısıtlı.", CareerUiTheme.DangerSoft);
            hasWeakness = true;
        }

        var fatCount = management.Players.Count(p => p.HasFatigueRisk);
        if (fatCount > 0)
        {
            AddSwotItem(_swotWeaknessesVBox, $"⚠️ {fatCount} as oyuncuda aşırı maç yorgunluğu tespit edildi (Dinlendirme önerilir).", CareerUiTheme.ColorWarning);
            hasWeakness = true;
        }

        var promCount = management.Players.Count(p => p.HasPromiseRisk);
        if (promCount > 0)
        {
            AddSwotItem(_swotWeaknessesVBox, $"⚠️ {promCount} oyuncu ile verilen söz risk altında (Moral/ilişki kaybı riski).", CareerUiTheme.ColorWarning);
            hasWeakness = true;
        }

        var injCount = management.Players.Count(p => p.IsInjured);
        if (injCount > 0)
        {
            AddSwotItem(_swotWeaknessesVBox, $"🩹 {injCount} oyuncu sakatlığı nedeniyle maç kadrosunda yer alamıyor.", CareerUiTheme.DangerSoft);
            hasWeakness = true;
        }

        if (capacity.IsOverCapacity)
        {
            AddSwotItem(_swotWeaknessesVBox, $"⚠️ {capacity.OverflowPlayerIds.Count} oyuncu sözleşme kotası fazlası (Kadro dışı).", CareerUiTheme.ColorWarning);
            hasWeakness = true;
        }

        if (!hasWeakness)
        {
            AddSwotItem(_swotWeaknessesVBox, "✓ Şu anda kadroda kritik bir derinlik açığı veya acil risk bulunmuyor.", CareerUiTheme.InkMuted);
        }
    }

    private static void AddSwotItem(VBoxContainer parent, string text, Color color)
    {
        var lbl = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(lbl);
        lbl.AddThemeColorOverride("font_color", color);
        lbl.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(12));
        parent.AddChild(lbl);
    }

    private static IReadOnlyList<PlayerManagementLine> FilterSquadPlayers(
        IReadOnlyList<PlayerManagementLine> players,
        string? filterCode,
        string categoryFilter = "ALL")
    {
        var filtered = players;
        if (!string.IsNullOrWhiteSpace(filterCode)
            && filterCode is not (Application.CareerHub.Queries.SquadOverviewDigest.SignalDepth
                or Application.CareerHub.Queries.SquadOverviewDigest.SignalOverflow
                or "calm"))
        {
            filtered = filterCode switch
            {
                Application.CareerHub.Queries.SquadOverviewDigest.SignalInjured => filtered
                    .Where(player => player.IsInjured)
                    .ToArray(),
                Application.CareerHub.Queries.SquadOverviewDigest.SignalFatigue => filtered
                    .Where(player => player.HasFatigueRisk)
                    .ToArray(),
                Application.CareerHub.Queries.SquadOverviewDigest.SignalPromise => filtered
                    .Where(player => player.HasPromiseRisk)
                    .ToArray(),
                _ => filtered,
            };
        }

        return categoryFilter switch
        {
            "GK" => filtered.Where(p => p.PositionCode is "KL" or "GK").ToArray(),
            "DEF" => filtered.Where(p => p.PositionCode is "CB" or "LB" or "RB" or "STP" or "SLB" or "SĞB" or "KB").ToArray(),
            "MID" => filtered.Where(p => p.PositionCode is "CM" or "DM" or "AM" or "MO" or "DOS" or "OOS").ToArray(),
            "FWD" => filtered.Where(p => p.PositionCode is "ST" or "CF" or "LW" or "RW" or "SLK" or "SĞK" or "SNT" or "FOR").ToArray(),
            "YOUTH" => filtered.Where(p => p.Age <= 21).ToArray(),
            "RISK" => filtered.Where(p => p.IsInjured || p.HasFatigueRisk || p.HasPromiseRisk).ToArray(),
            _ => filtered,
        };
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
        var managedClubId = _controller.GetEmployedClubId();

        if (_showingContinentalLeague)
        {
            if (_roundSelector.MaxValue != 6)
            {
                _roundSelector.MaxValue = 6;
            }

            var round = (int)_roundSelector.Value;
            var euroFixtures = _controller.GetContinentalFixtures(round);
            _currentContinentalFixtures = euroFixtures;

            int? managedIndex = null;
            for (var i = 0; i < euroFixtures.Count; i++)
            {
                var f = euroFixtures[i];
                var isManaged = managedClubId.HasValue && (f.HomeClubId == managedClubId.Value || f.AwayClubId == managedClubId.Value);
                if (isManaged && managedIndex is null)
                {
                    managedIndex = i;
                }
                var prefix = isManaged ? "⭐ " : "   ";
                var fixtureLine = f.IsPlayed
                    ? $"{prefix}[E] {f.HomeClubName,-14}  {f.HomeScore} - {f.AwayScore}  {f.AwayClubName,14} [D] · {f.StageName} (Gün {f.ScheduledDayNumber})"
                    : $"{prefix}[E] {f.HomeClubName,-14}    vs    {f.AwayClubName,14} [D] · {f.StageName} (Gün {f.ScheduledDayNumber})";
                _fixtureList.AddItem(fixtureLine);
            }

            if (euroFixtures.Count > 0)
            {
                var selectIndex = managedIndex ?? 0;
                _fixtureList.Select(selectIndex);
                UpdateFixtureDetail(selectIndex);
            }
            else
            {
                _worldFixtureDetailCard.Visible = false;
            }
            return;
        }

        if (_roundSelector.MaxValue != CompetitionMvpConstraints.MaxLeagueFixtureRound)
        {
            _roundSelector.MaxValue = CompetitionMvpConstraints.MaxLeagueFixtureRound;
        }

        var season = _controller.GetCurrentSeason();
        if (season is null || season.FixtureCount == 0)
        {
            _currentRoundFixtures = [];
            _worldFixtureDetailCard.Visible = false;
            return;
        }

        var roundNum = (int)_roundSelector.Value;
        var fixtures = _controller.GetFixturesByRound(season.SeasonId, roundNum);
        _currentRoundFixtures = fixtures;

        int? managedFixtureIndex = null;

        for (var i = 0; i < fixtures.Count; i++)
        {
            var fixture = fixtures[i];
            var home = _controller.GetClubDisplayName(fixture.HomeClubId);
            var away = _controller.GetClubDisplayName(fixture.AwayClubId);
            var isManaged = managedClubId.HasValue && (fixture.HomeClubId == managedClubId.Value || fixture.AwayClubId == managedClubId.Value);
            if (isManaged && managedFixtureIndex is null)
            {
                managedFixtureIndex = i;
            }
            var prefix = isManaged ? "⭐ " : "   ";
            var isPlayed = fixture.HomeGoals.HasValue && fixture.AwayGoals.HasValue;
            var fixtureLine = isPlayed
                ? $"{prefix}[E] {home,-14}  {fixture.HomeGoals} - {fixture.AwayGoals}  {away,14} [D] · {fixture.ScheduledIsoDate} · Oynandı"
                : $"{prefix}[E] {home,-14}    vs    {away,14} [D] · {fixture.ScheduledIsoDate} · Planlandı";
            _fixtureList.AddItem(fixtureLine);
        }

        if (fixtures.Count > 0)
        {
            var selectIndex = managedFixtureIndex ?? 0;
            _fixtureList.Select(selectIndex);
            UpdateFixtureDetail(selectIndex);
        }
        else
        {
            _worldFixtureDetailCard.Visible = false;
        }
    }

    private void OnFixtureSelected(long index)
    {
        UpdateFixtureDetail((int)index);
    }

    private void UpdateFixtureDetail(int index)
    {
        var managedClubId = _controller.GetEmployedClubId();

        if (_showingContinentalLeague)
        {
            if (index < 0 || index >= _currentContinentalFixtures.Count)
            {
                _worldFixtureDetailCard.Visible = false;
                return;
            }

            var f = _currentContinentalFixtures[index];
            _selectedFixtureHomeClubId = f.HomeClubId;
            _selectedFixtureAwayClubId = f.AwayClubId;

            _worldFixtureHeadlineLabel.Text = $"{f.HomeClubName} vs {f.AwayClubName}";
            var isManagedFixture = managedClubId.HasValue
                && (f.HomeClubId == managedClubId.Value || f.AwayClubId == managedClubId.Value);
            var isHome = managedClubId.HasValue && f.HomeClubId == managedClubId.Value;

            var statusText = f.IsPlayed
                ? $"Skor: {f.HomeScore} - {f.AwayScore} · Oynandı (Sonuç Tescillendi)"
                : "Planlandı (Avrupa Maç Günü — Salı/Çarşamba)";

            var roleText = isManagedFixture
                ? (isHome ? " · [Kulübünüz Ev Sahibi]" : " · [Kulübünüz Deplasman]")
                : string.Empty;

            _worldFixtureDetailLabel.Text = $"⭐ {f.StageName} · Takvim Günü: {f.ScheduledDayNumber}{roleText}\n{statusText}\nGalibiyet Primi: +3.500.000 TRY · Beraberlik Primi: +1.200.000 TRY";

            _worldFixturePrepButton.Visible = isManagedFixture && !f.IsPlayed;
            _worldFixtureHomeInspectButton.Text = $"🏠 {f.HomeClubName} İncele";
            _worldFixtureAwayInspectButton.Text = $"✈️ {f.AwayClubName} İncele";
            _worldFixtureDetailCard.Visible = true;
            return;
        }

        if (index < 0 || index >= _currentRoundFixtures.Count)
        {
            _worldFixtureDetailCard.Visible = false;
            return;
        }

        var fixture = _currentRoundFixtures[index];
        _selectedFixtureHomeClubId = fixture.HomeClubId;
        _selectedFixtureAwayClubId = fixture.AwayClubId;

        var homeName = _controller.GetClubDisplayName(fixture.HomeClubId);
        var awayName = _controller.GetClubDisplayName(fixture.AwayClubId);
        _worldFixtureHeadlineLabel.Text = $"{homeName} vs {awayName}";

        var isManaged = managedClubId.HasValue
            && (fixture.HomeClubId == managedClubId.Value || fixture.AwayClubId == managedClubId.Value);

        var isHomeClub = managedClubId.HasValue && fixture.HomeClubId == managedClubId.Value;
        var domStatusText = fixture.Status switch
        {
            nameof(FixtureStatus.ResultAccepted) when fixture.HomeGoals.HasValue && fixture.AwayGoals.HasValue
                => $"Skor: {fixture.HomeGoals} - {fixture.AwayGoals} · Oynandı (Sonuç Tescillendi)",
            nameof(FixtureStatus.Planned) => "Planlandı (Maç saati bekleniyor)",
            _ => fixture.Status,
        };

        var domRoleText = isManaged
            ? (isHomeClub ? " · [Kulübünüz Ev Sahibi]" : " · [Kulübünüz Deplasman]")
            : string.Empty;

        _worldFixtureDetailLabel.Text = $"Hafta {fixture.Round} · Tarih: {fixture.ScheduledIsoDate}{domRoleText}\n{domStatusText}";

        _worldFixturePrepButton.Visible = isManaged && fixture.Status == nameof(FixtureStatus.Planned);
        _worldFixtureHomeInspectButton.Text = $"🏠 {homeName} İncele";
        _worldFixtureAwayInspectButton.Text = $"✈️ {awayName} İncele";
        _worldFixtureDetailCard.Visible = true;
    }

    private void OnWorldClubFixtureJumpPressed()
    {
        if (!_selectedWorldClubId.HasValue)
        {
            return;
        }

        if (_showingContinentalLeague)
        {
            var nextEuro = _controller.GetContinentalFixtures()
                .FirstOrDefault(f => !f.IsPlayed && (f.HomeClubId == _selectedWorldClubId.Value || f.AwayClubId == _selectedWorldClubId.Value));
            if (nextEuro is not null)
            {
                _roundSelector.Value = nextEuro.RoundNumber;
                RefreshFixtureList();
            }
            return;
        }

        var season = _controller.GetCurrentSeason();
        if (season is null)
        {
            return;
        }

        var clubId = _selectedWorldClubId.Value;
        var clubName = _controller.GetClubDisplayName(clubId);
        var allFixtures = _controller.GetSeasonFixtures(season.SeasonId);
        var clubFixtures = allFixtures
            .Where(f => f.HomeClubId == clubId || f.AwayClubId == clubId)
            .OrderBy(f => f.Round)
            .ToList();

        if (clubFixtures.Count == 0)
        {
            PulseStatus($"{clubName} için fikstür bulunamadı.");
            return;
        }

        var targetFixture = clubFixtures.FirstOrDefault(f => f.Status == nameof(FixtureStatus.Planned))
            ?? clubFixtures.LastOrDefault(f => f.Status == nameof(FixtureStatus.ResultAccepted))
            ?? clubFixtures[0];

        _roundSelector.Value = targetFixture.Round;
        RefreshFixtureList();

        for (var i = 0; i < _currentRoundFixtures.Count; i++)
        {
            var f = _currentRoundFixtures[i];
            if (f.HomeClubId == clubId || f.AwayClubId == clubId)
            {
                _fixtureList.Select(i);
                UpdateFixtureDetail(i);
                break;
            }
        }

        PulseStatus($"{clubName} takımının {targetFixture.Round}. hafta maçına gidildi.");
    }

    private void OnStandingsClubSelected()
    {
        var selected = _standingsTable.GetSelected();
        if (selected is null)
        {
            return;
        }

        var meta = selected.GetMetadata(0);
        if (meta.VariantType != Variant.Type.Nil)
        {
            var clubId = meta.AsInt64();
            InspectClub(clubId);
        }
    }

    private void OnCompactStandingsClubSelected(long index)
    {
        if (index < 0
            || index >= _compactStandingsClubIds.Count
            || _compactStandingsClubIds[(int)index] is not long clubId)
        {
            return;
        }

        InspectClub(clubId);
        _mobileFocusIndices[HubPage.World] = 2;
        ApplyMobileFocusLayout(HubPage.World, compact: true);
        Callable.From(() => _pageScroll.ScrollVertical = 0).CallDeferred();
    }

    private void InspectClub(long clubId)
    {
        _selectedWorldClubId = clubId;

        if (EuropeanElite202627DataPack.TryGetClub(clubId, out var euroElite))
        {
            var stars = string.Join(", ", euroElite.Players.OrderByDescending(p => p.CurrentAbility ?? 75).Take(4).Select(p => $"{p.DisplayName} ({p.CurrentAbility ?? 75})"));
            _worldClubHeadlineLabel.Text = $"{euroElite.OfficialName} ({euroElite.ShortCode}) · {euroElite.Country} · Güç: ⭐ {euroElite.SportiveStrength} / 100";
            _worldClubBadgeLabel.Text = $"⭐ AVRUPA DEVİ ({euroElite.LeagueName})";
            _worldClubBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);

            _worldClubRatingBadgeLabel.Text = $"⭐ Güç: {euroElite.SportiveStrength} / 100";
            _worldClubRatingBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.RatingBadgePanel(euroElite.SportiveStrength));
            _worldClubThreatBadgeLabel.Text = $"🌍 {euroElite.Country} ({euroElite.LeagueName})";
            CareerUiTheme.StyleEyebrow(_worldClubThreatBadgeLabel, CareerUiTheme.Accent);
            _worldClubThreatBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.20f));
            _worldClubThreatBadgePanel.Visible = true;

            _worldClubPerformanceLabel.Text = $"🌍 AVRUPA KULÜP DOSYASI\nÜlke / Lig: {euroElite.Country} ({euroElite.LeagueName})\nTransfer Bütçe Limiti: {euroElite.TransferBudgetLimit:N0} TRY\nSportif Güç: ⭐ {euroElite.SportiveStrength} / 100\nÖne Çıkan Yıldızlar: {stars}";

            _worldClubThreatLabel.Text = $"⚔️ KADRO VE TEHDİT ANALİZİ\nAvrupa Elit Kulübü ({euroElite.Players.Count} Kayıtlı Yıldız Oyuncu)\nKulüp Kodu: {euroElite.ShortCode}\nUluslararası Güç Puanı: ⭐ {euroElite.SportiveStrength} / 100";

            var euroEngine = _controller.GetOrInitContinentalTournament();
            var managedId = _controller.GetEmployedClubId();
            var euroH2H = euroEngine.AllFixtures
                .Where(f => (f.HomeClubId == managedId && f.AwayClubId == clubId) || (f.HomeClubId == clubId && f.AwayClubId == managedId))
                .ToList();
            var h2hLines = new List<string>();
            foreach (var f in euroH2H)
            {
                var played = f.IsPlayed ? $"{f.HomeScore} - {f.AwayScore} (Oynandı)" : "Henüz oynanmadı";
                h2hLines.Add($"⭐ {f.StageName}: {f.HomeClubName} vs {f.AwayClubName} · {played}");
            }

            _worldClubHeadToHeadLabel.Text = h2hLines.Count > 0
                ? "🤝 AVRUPA MÜSABAKALARI\n" + string.Join("\n", h2hLines)
                : "🤝 AVRUPA MÜSABAKALARI\nBu sezon henüz bu takımla eşleşme kaydı bulunmuyor.";

            _worldClubPrepButton.Visible = false;
            _worldClubTransferButton.Visible = true;
            _worldClubFixtureButton.Visible = false;
            return;
        }

        var club = _controller.GetClub(clubId);
        var clubName = club?.DisplayName ?? _controller.GetClubDisplayName(clubId);

        var season = _controller.GetCurrentSeason();
        var standings = season is not null ? _controller.GetStandings(season.SeasonId) : [];
        var standingRank = standings.Select((e, i) => (Entry: e, Rank: i + 1)).FirstOrDefault(x => x.Entry.ClubId == clubId);

        var stats = _controller.BuildLeagueStatisticsDigest();
        var teamStats = stats.Teams.FirstOrDefault(t => t.ClubId == clubId);
        var dossier = _controller.BuildClubDossier(clubId);

        var currentDay = _controller.GetCurrentGameDate().DayNumber;
        var nextManaged = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextPlannedManagedFixture(currentDay);
        var isNextOpponent = nextManaged is not null && nextManaged.OpponentClubId == clubId;
        var managedClubId = _controller.GetEmployedClubId();
        var isManagedClub = managedClubId.HasValue && managedClubId.Value == clubId;

        _worldClubHeadlineLabel.Text = $"{clubName} · Kadro Gücü: ⭐ {club?.SportiveStrength ?? 50} / 100";

        var strength = club?.SportiveStrength ?? 50;
        _worldClubRatingBadgeLabel.Text = $"⭐ Güç: {strength} / 100";
        _worldClubRatingBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.RatingBadgePanel(strength));

        if (dossier is not null)
        {
            var (threatText, threatColor) = dossier.ThreatKind switch
            {
                OpponentThreatKind.WinningStreak => ("🔥 Galibiyet Serisi", CareerUiTheme.Accent),
                OpponentThreatKind.ProductiveAttack => ("⚡ Golcü Hücum", CareerUiTheme.ActionBright),
                OpponentThreatKind.SquadQuality => ("⭐ Yüksek Kadro Kalitesi", CareerUiTheme.Accent),
                OpponentThreatKind.TopZoneTempo => ("🚀 Yüksek Tempo", CareerUiTheme.ActionBright),
                OpponentThreatKind.DefensiveResistance => ("🛡️ Sert Savunma", CareerUiTheme.Data),
                OpponentThreatKind.LosingStreak => ("⚠️ Düşüş Formunda", CareerUiTheme.ColorWarning),
                _ => ("⚖️ Dengeli Eşleşme", CareerUiTheme.Stroke),
            };
            _worldClubThreatBadgeLabel.Text = threatText;
            CareerUiTheme.StyleEyebrow(_worldClubThreatBadgeLabel, threatColor);
            _worldClubThreatBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(threatColor, 0.20f));
            _worldClubThreatBadgePanel.Visible = true;
        }
        else
        {
            _worldClubThreatBadgePanel.Visible = false;
        }

        if (isNextOpponent)
        {
            _worldClubBadgeLabel.Text = "🎯 SIRADAKİ RAKİBİNİZ (Yaklaşan Müsabaka — Taktik Hazırlığı Yapabilirsiniz)";
            _worldClubBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        }
        else if (isManagedClub)
        {
            _worldClubBadgeLabel.Text = "🛡️ ÇALIŞTIRDIĞINIZ KULÜP";
            _worldClubBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Data);
        }
        else if (standingRank.Rank == 1)
        {
            _worldClubBadgeLabel.Text = "🏆 LİG LİDERİ";
            _worldClubBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        }
        else if (standingRank.Rank > 0)
        {
            _worldClubBadgeLabel.Text = $"Lig Derecesi: {standingRank.Rank}. Sıra ({standingRank.Entry.Points} Puan · Averaj {standingRank.Entry.GoalDifference:+0;-#;0})";
            _worldClubBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
        }
        else
        {
            _worldClubBadgeLabel.Text = "Kulüp Bilgisi";
            _worldClubBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.InkMuted);
        }

        var standingLine = dossier?.StandingLine ?? "Lig verisi bekleniyor.";
        var formLine = dossier?.FormLine ?? "Form verisi bekleniyor.";
        var splitsLine = teamStats is not null && (teamStats.HomePlayed > 0 || teamStats.AwayPlayed > 0)
            ? $"İç Saha: {teamStats.HomePoints} Puan ({teamStats.HomePlayed} Maç) · Deplasman: {teamStats.AwayPoints} Puan ({teamStats.AwayPlayed} Maç)"
            : string.Empty;

        _worldClubPerformanceLabel.Text = string.IsNullOrWhiteSpace(splitsLine)
            ? $"📊 LİG & FORM PERFORMANSI\n{standingLine}\n{formLine}"
            : $"📊 LİG & FORM PERFORMANSI\n{standingLine}\n{formLine}\n{splitsLine}";

        var strengthLine = isManagedClub
            ? $"Güç: Çalıştırdığınız kulüp ({club?.SportiveStrength ?? 50} puan)."
            : dossier?.StrengthLine ?? "Kadro dengeli.";
        var threatLine = dossier?.ThreatLine ?? "Tehdit verisi yok.";
        _worldClubThreatLabel.Text = $"⚔️ GÖRELİ GÜÇ & OYUN KİMLİĞİ\n{strengthLine}\n{threatLine}";

        if (isManagedClub)
        {
            _worldClubHeadToHeadLabel.Text = "🤝 BU SEZONKİ KARŞILAŞMALAR\nKendi kulübünüzü inceliyorsunuz. Kadro derinliği ve taktik planı için KULÜP ve TAKTİK sekmelerini kullanabilirsiniz.";
        }
        else
        {
            var headToHeadLines = new List<string>();
            if (season is not null && managedClubId.HasValue)
            {
                var allFixtures = _controller.GetSeasonFixtures(season.SeasonId);
                var h2h = allFixtures
                    .Where(f => (f.HomeClubId == managedClubId.Value && f.AwayClubId == clubId)
                             || (f.HomeClubId == clubId && f.AwayClubId == managedClubId.Value))
                    .OrderBy(f => f.Round)
                    .ToList();
                foreach (var f in h2h)
                {
                    var home = _controller.GetClubDisplayName(f.HomeClubId);
                    var away = _controller.GetClubDisplayName(f.AwayClubId);
                    if (f.HomeGoals is int h && f.AwayGoals is int a)
                    {
                        headToHeadLines.Add($"Hafta {f.Round}: {home} {h} - {a} {away} (Oynandı)");
                    }
                    else
                    {
                        headToHeadLines.Add($"Hafta {f.Round}: {home} vs {away} · {f.ScheduledIsoDate} (Planlandı)");
                    }
                }
            }

            _worldClubHeadToHeadLabel.Text = headToHeadLines.Count > 0
                ? "🤝 BU SEZONKİ KARŞILAŞMALAR\n" + string.Join("\n", headToHeadLines)
                : "🤝 BU SEZONKİ KARŞILAŞMALAR\nBu sezon henüz karşılaşma kaydı bulunmuyor.";
        }

        _worldClubPrepButton.Visible = isNextOpponent;
        _worldClubTransferButton.Visible = !isManagedClub;
        _worldClubFixtureButton.Visible = true;
    }

    private void UpdateCompetitionTabButtons()
    {
        if (_leagueCompButton is null || _euroCompButton is null)
        {
            return;
        }

        if (_showingContinentalLeague)
        {
            CareerUiTheme.StyleSecondaryButton(_leagueCompButton);
            CareerUiTheme.StyleAccentButton(_euroCompButton);
        }
        else
        {
            CareerUiTheme.StyleAccentButton(_leagueCompButton);
            CareerUiTheme.StyleSecondaryButton(_euroCompButton);
        }
    }

    private void RefreshContinentalStandings()
    {
        var digest = _controller.BuildContinentalTournamentDigest();
        _leagueBriefingLabel.Text = $"⭐ {digest.TournamentTitle} · {digest.StageName}\nÖdül Havuzu: {digest.PrizeSummaryText}";
        _leagueStatisticsLabel.Text = "Gruplar: 4 Grup (A, B, C, D) · Her grupta 1 Süper Lig + 3 Avrupa Devi (Real Madrid, Man City, Bayern, PSG...)\nİlk 2 sırayı alan takımlar Çeyrek Final'e yükselir.\nGalibiyet Primi: +3.500.000 TRY · Beraberlik Primi: +1.200.000 TRY";

        _leagueRoundBadgeLabel.Text = $"⭐ {digest.StageName}";
        _leagueMatchesBadgeLabel.Text = "⚽ 4 Grup · 16 Kulüp";
        _attackLeaderClubLabel.Text = "Avrupa Eliti";
        _attackLeaderStatLabel.Text = "Şampiyonluk Mücadelesi";
        _defenseLeaderClubLabel.Text = "Grup Etabı";
        _defenseLeaderStatLabel.Text = "İlk 2 Tur Atlar";
        _formLeaderClubLabel.Text = "Galibiyet Primi";
        _formLeaderStatLabel.Text = "+3.500.000 TRY";

        var managedClubId = _controller.GetEmployedClubId();
        var managedEntry = digest.AllGroups
            .SelectMany(g => g.Standings)
            .FirstOrDefault(s => s.ClubId == managedClubId);

        _managedLeagueStatisticsLabel.Text = managedEntry is not null
            ? $"Kulübünüz {managedEntry.ClubName}: {managedEntry.Points} Puan ({managedEntry.Won}G {managedEntry.Drawn}B {managedEntry.Lost}M · Averaj {managedEntry.GoalDifference:+0;-#;0}) · {(managedEntry.Rank <= 2 ? "🏆 İlk İkide (Üst Tur Hattı)" : "Grup Etabı Mücadelesi")}"
            : "Kulübümüz Avrupa Şampiyonlar Kupası'nda mücadele ediyor.";

        _standingsTable.Clear();
        _compactStandingsList.Clear();
        _compactStandingsClubIds.Clear();
        var root = _standingsTable.CreateItem();
        TreeItem? rowToSelect = null;
        var compactRowToSelect = -1;
        var preferredClubId = _selectedWorldClubId ?? managedClubId;

        foreach (var group in digest.AllGroups)
        {
            AddCompactStanding(null, $"GRUP {group.GroupCode}", CareerUiTheme.Accent);
            var headerRow = _standingsTable.CreateItem(root);
            headerRow.SetText(0, $"GRUP {group.GroupCode}");
            headerRow.SetCustomColor(0, CareerUiTheme.Accent);
            headerRow.SetText(1, "── Avrupa Şampiyonlar Kupası ──");
            headerRow.SetCustomColor(1, CareerUiTheme.Accent);
            for (var col = 2; col < 11; col++)
            {
                headerRow.SetText(col, "·");
                headerRow.SetTextAlignment(col, HorizontalAlignment.Center);
            }

            for (var index = 0; index < group.Standings.Count; index++)
            {
                var entry = group.Standings[index];
                var row = _standingsTable.CreateItem(root);
                row.SetMetadata(0, entry.ClubId);

                if (entry.ClubId == preferredClubId)
                {
                    rowToSelect = row;
                }

                var compactIndex = AddCompactStanding(
                    entry.ClubId,
                    $"{index + 1}. {entry.ClubName}   {entry.Played}M · {entry.GoalDifference:+0;-#;0} · {entry.Points}P",
                    managedClubId == entry.ClubId
                        ? CareerUiTheme.ActionBright
                        : index < 2
                            ? CareerUiTheme.Accent
                            : CareerUiTheme.Ink);
                if (entry.ClubId == preferredClubId)
                {
                    compactRowToSelect = compactIndex;
                }

                var rankText = index < 2 ? $"⭐ {index + 1}" : $"   {index + 1}";
                var clubDisplayName = managedClubId == entry.ClubId ? $"🛡️ {entry.ClubName}" : entry.ClubName;

                var values = new[]
                {
                    rankText,
                    clubDisplayName,
                    entry.Played.ToString(),
                    entry.Won.ToString(),
                    entry.Drawn.ToString(),
                    entry.Lost.ToString(),
                    entry.GoalsFor.ToString(),
                    entry.GoalsAgainst.ToString(),
                    entry.GoalDifference > 0 ? $"+{entry.GoalDifference}" : entry.GoalDifference.ToString(),
                    "—",
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

                var rankColor = index < 2 ? CareerUiTheme.Accent : CareerUiTheme.InkMuted;
                row.SetCustomColor(0, rankColor);
                row.SetCustomColor(9, CareerUiTheme.Data);
                row.SetCustomColor(10, CareerUiTheme.Accent);

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
                                0.20f));
                    }
                    row.SetCustomColor(1, CareerUiTheme.ActionBright);
                }
            }
        }

        rowToSelect?.Select(0);
        if (compactRowToSelect >= 0)
        {
            _compactStandingsList.Select(compactRowToSelect);
        }
        if (preferredClubId.HasValue)
        {
            InspectClub(preferredClubId.Value);
        }
    }

    private void RefreshStandings()
    {
        UpdateCompetitionTabButtons();
        if (_showingContinentalLeague)
        {
            RefreshContinentalStandings();
            return;
        }

        _leagueBriefingLabel.Text = _controller.BuildLeagueWorldBriefing().ToDisplayText();
        var statistics = _controller.BuildLeagueStatisticsDigest();
        _leagueStatisticsLabel.Text = $"{statistics.Headline}\n{statistics.LeadersLine}";
        _managedLeagueStatisticsLabel.Text = statistics.ManagedClubLine;
        _standingsTable.Clear();
        _compactStandingsList.Clear();
        _compactStandingsClubIds.Clear();

        var season = _controller.GetCurrentSeason();
        if (season is null || season.FixtureCount == 0)
        {
            ShowEmptyStandings("Henüz fikstür oluşturulmadı");
            return;
        }

        var roundNum = (int)_roundSelector.Value;
        _leagueRoundBadgeLabel.Text = $"📅 Hafta {roundNum} / {CompetitionMvpConstraints.MaxLeagueFixtureRound}";

        var allFixtures = _controller.GetSeasonFixtures(season.SeasonId);
        var playedMatches = allFixtures.Count(f => f.HomeGoals.HasValue);
        var totalGoals = allFixtures.Where(f => f.HomeGoals.HasValue).Sum(f => f.HomeGoals!.Value + f.AwayGoals!.Value);
        _leagueMatchesBadgeLabel.Text = playedMatches > 0
            ? $"⚽ {playedMatches} Maç · {(double)totalGoals / playedMatches:0.00} Gol/Maç"
            : "⚽ 0 Maç · — Gol/Maç";

        var managedClubId = _controller.GetEmployedClubId();
        var standings = _controller.GetStandings(season.SeasonId);
        if (standings.Count == 0)
        {
            ShowEmptyStandings("Henüz puan durumu oluşmadı");
            return;
        }

        var attackEntry = standings.Where(e => e.Played > 0).OrderByDescending(e => e.GoalsFor).ThenByDescending(e => e.Points).FirstOrDefault();
        var defenseEntry = standings.Where(e => e.Played > 0).OrderBy(e => e.GoalsAgainst).ThenByDescending(e => e.Points).FirstOrDefault();
        var formTeam = statistics.Teams.OrderByDescending(t => t.LastFivePoints).ThenByDescending(t => standings.FirstOrDefault(e => e.ClubId == t.ClubId)?.GoalDifference ?? 0).FirstOrDefault();

        if (attackEntry is not null)
        {
            _attackLeaderClubLabel.Text = _controller.GetClubDisplayName(attackEntry.ClubId);
            _attackLeaderStatLabel.Text = $"{attackEntry.GoalsFor} Gol (Maç Başı {(double)attackEntry.GoalsFor / Math.Max(1, attackEntry.Played):0.00})";
        }
        else
        {
            _attackLeaderClubLabel.Text = "—";
            _attackLeaderStatLabel.Text = "— Gol";
        }

        if (defenseEntry is not null)
        {
            _defenseLeaderClubLabel.Text = _controller.GetClubDisplayName(defenseEntry.ClubId);
            _defenseLeaderStatLabel.Text = $"{defenseEntry.GoalsAgainst} Gol Yenilen ({defenseEntry.Played} Maç)";
        }
        else
        {
            _defenseLeaderClubLabel.Text = "—";
            _defenseLeaderStatLabel.Text = "— Gol Yenilen";
        }

        if (formTeam is not null && formTeam.LastFiveForm != "—")
        {
            _formLeaderClubLabel.Text = formTeam.ClubName;
            _formLeaderStatLabel.Text = $"{formTeam.LastFivePoints}/15 Puan · {FormatFormPills(formTeam.LastFiveForm)}";
        }
        else
        {
            _formLeaderClubLabel.Text = "—";
            _formLeaderStatLabel.Text = "—/15 Puan";
        }

        TreeItem? rowToSelect = null;
        var compactRowToSelect = -1;
        var currentDay = _controller.GetCurrentGameDate().DayNumber;
        var nextManaged = _controller.Host.TeamPreparationModule.SelectionQueries
            .GetNextPlannedManagedFixture(currentDay);

        var preferredClubId = _selectedWorldClubId
            ?? nextManaged?.OpponentClubId
            ?? managedClubId
            ?? standings[0].ClubId;

        var root = _standingsTable.CreateItem();
        for (var index = 0; index < standings.Count; index++)
        {
            var entry = standings[index];
            var row = _standingsTable.CreateItem(root);
            row.SetMetadata(0, entry.ClubId);

            if (entry.ClubId == preferredClubId)
            {
                rowToSelect = row;
            }

            var zoneColor = StandingsZoneColor(index, standings.Count);
            var rankDisplay = index switch
            {
                0 => "▌🏆 1",
                <= 3 => $"▌⭐ {index + 1}",
                _ when index >= standings.Count - 3 => $"▌🔻 {index + 1}",
                _ => (index + 1).ToString(),
            };

            var clubNameText = _controller.GetClubDisplayName(entry.ClubId);
            var clubDisplay = managedClubId == entry.ClubId ? $"🛡️ {clubNameText}" : clubNameText;

            var compactIndex = AddCompactStanding(
                entry.ClubId,
                $"{index + 1}. {clubDisplay}   {entry.Played}M · {entry.GoalDifference:+0;-#;0} · {entry.Points}P",
                managedClubId == entry.ClubId
                    ? CareerUiTheme.ActionBright
                    : zoneColor ?? CareerUiTheme.Ink);
            if (entry.ClubId == preferredClubId)
            {
                compactRowToSelect = compactIndex;
            }

            var values = new[]
            {
                rankDisplay,
                clubDisplay,
                entry.Played.ToString(),
                entry.Won.ToString(),
                entry.Drawn.ToString(),
                entry.Lost.ToString(),
                entry.GoalsFor.ToString(),
                entry.GoalsAgainst.ToString(),
                entry.GoalDifference > 0 ? $"+{entry.GoalDifference}" : entry.GoalDifference.ToString(),
                FormatFormPills(statistics.GetForm(entry.ClubId)),
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
                >= 15 => CareerUiTheme.DangerSoft,
                _ => CareerUiTheme.InkMuted,
            };
            row.SetCustomColor(0, rankColor);
            row.SetCustomColor(1, managedClubId == entry.ClubId ? CareerUiTheme.ActionBright : CareerUiTheme.Ink);
            row.SetCustomColor(9, CareerUiTheme.Ink);
            row.SetCustomColor(10, CareerUiTheme.Accent);

            if (zoneColor is Color zone)
            {
                row.SetCustomBgColor(0, new Color(zone.R, zone.G, zone.B, 0.30f));
            }

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
                            0.20f));
                }
                if (zoneColor is Color managedZone)
                {
                    row.SetCustomBgColor(0, new Color(managedZone.R, managedZone.G, managedZone.B, 0.38f));
                }
            }
        }

        rowToSelect?.Select(0);
        if (compactRowToSelect >= 0)
        {
            _compactStandingsList.Select(compactRowToSelect);
        }
        InspectClub(preferredClubId);
    }

}
