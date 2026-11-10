using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private Button _transferTabDealBtn = null!;
    private Button _transferTabNeedsBtn = null!;
    private Control _transferDealHost = null!;
    private Control _transferNeedsHost = null!;
    private PanelContainer _scoutComparisonPanel = null!;
    private Label _scoutComparisonLabel = null!;

    private Control BuildTransferPage()
    {
        var page = PageRoot();
        page.SizeFlagsVertical = SizeFlags.ExpandFill;

        // ----------------------------------------------------
        // SINGLE COMPACT TOP BAR: RIGHT-PANEL TABS (LEFT) + BUDGET & WINDOW PILLS (RIGHT)
        // ----------------------------------------------------
        var topControlRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topControlRow.AddThemeConstantOverride("separation", 6);
        page.AddChild(topControlRow);

        var tabRow = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        tabRow.AddThemeConstantOverride("separation", 4);
        topControlRow.AddChild(tabRow);

        _transferTabDealBtn = new Button { Text = "PAZARLIK & SÖZLEŞME MASASI" };
        _transferTabDealBtn.Pressed += () => ShowTransferSubTab(0);
        CareerUiTheme.StyleTabButton(_transferTabDealBtn, selected: true);
        tabRow.AddChild(_transferTabDealBtn);

        _transferTabNeedsBtn = new Button { Text = "KADRO İHTİYACI & GİDENLER" };
        _transferTabNeedsBtn.Pressed += () => ShowTransferSubTab(1);
        CareerUiTheme.StyleTabButton(_transferTabNeedsBtn, selected: false);
        tabRow.AddChild(_transferTabNeedsBtn);

        _transferNextStepButton = AccentButton("Önerilen Adım");
        _transferNextStepButton.Visible = false;
        _transferNextStepButton.Pressed += OnTransferNextStepPressed;
        tabRow.AddChild(_transferNextStepButton);

        var topSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topControlRow.AddChild(topSpacer);

        var budgetRow = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        budgetRow.AddThemeConstantOverride("separation", 5);
        topControlRow.AddChild(budgetRow);

        _transferBudgetBadgePanel = new PanelContainer();
        _transferBudgetBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.24f));
        _transferBudgetBadgeLabel = new Label { Text = "Bütçe —" };
        CareerUiTheme.StyleTableText(_transferBudgetBadgeLabel);
        _transferBudgetBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Accent);
        _transferBudgetBadgePanel.AddChild(_transferBudgetBadgeLabel);
        budgetRow.AddChild(_transferBudgetBadgePanel);

        _wageBudgetBadgePanel = new PanelContainer();
        _wageBudgetBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data, 0.24f));
        _wageBudgetBadgeLabel = new Label { Text = "Maaş: —" };
        CareerUiTheme.StyleTableText(_wageBudgetBadgeLabel);
        _wageBudgetBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.Data);
        _wageBudgetBadgePanel.AddChild(_wageBudgetBadgeLabel);
        budgetRow.AddChild(_wageBudgetBadgePanel);

        _transferWindowBadgePanel = new PanelContainer();
        _transferWindowBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.24f));
        _transferWindowBadgeLabel = new Label { Text = "Pencere Açık" };
        CareerUiTheme.StyleTableText(_transferWindowBadgeLabel);
        _transferWindowBadgeLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _transferWindowBadgePanel.AddChild(_transferWindowBadgeLabel);
        budgetRow.AddChild(_transferWindowBadgePanel);

        // Welcome Banner (Visible after signing a new player)
        _transferWelcomePanel = new PanelContainer
        {
            Visible = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _transferWelcomePanel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.24f));
        var welcomeCard = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        welcomeCard.AddThemeConstantOverride("separation", 8);
        _transferWelcomeHeadlineLabel = new Label { Text = "YENİ TRANSFER" };
        CareerUiTheme.StyleHeadline(_transferWelcomeHeadlineLabel);
        _transferWelcomeHeadlineLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _transferWelcomeHeadlineLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(14));
        welcomeCard.AddChild(_transferWelcomeHeadlineLabel);
        _transferWelcomeMetaLabel = BodyLabel("TransferWelcomeMetaLabel", autowrap: true);
        _transferWelcomeMetaLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        welcomeCard.AddChild(_transferWelcomeMetaLabel);
        _transferWelcomePanel.AddChild(welcomeCard);
        page.AddChild(_transferWelcomePanel);

        // ====================================================
        // WIDESCREEN 2-COLUMN TRANSFER HUB
        // Left: Permanent Scouted Player Market & Shortlist
        // Right: Active Negotiation / Offer Desk OR Squad Needs & Outgoing
        // ====================================================
        var transferSplitRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        transferSplitRow.AddThemeConstantOverride("separation", 10);
        page.AddChild(transferSplitRow);

        // Dummy grid reference kept non-null for responsive layout hooks
        _transferBentoGrid = new GridContainer { Visible = false };
        page.AddChild(_transferBentoGrid);

        // ── LEFT COLUMN: SCOUTED PLAYER MARKET & SHORTLIST ─────────────────────
        var scoutNeonPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.3f,
        };
        scoutNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        transferSplitRow.AddChild(scoutNeonPanel);

        var scoutCard = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        scoutCard.AddThemeConstantOverride("separation", 6);
        scoutNeonPanel.AddChild(scoutCard);

        var scoutHeaderRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scoutHeaderRow.AddThemeConstantOverride("separation", 6);
        scoutCard.AddChild(scoutHeaderRow);
        scoutHeaderRow.AddChild(UiComponents.PageHeader("radar", "GÖZLEMCİ HAVUZU & TRANSFER PAZARI"));

        var scopeRow = ActionFlow();
        scoutCard.AddChild(scopeRow);

        _scopeAllButton = SecondaryButton("Tüm Dünya");
        _scopeAllButton.Pressed += () => SetScoutScope(TransferMarketScope.All);
        scopeRow.AddChild(_scopeAllButton);

        _scopeDomesticButton = SecondaryButton("Süper Lig");
        _scopeDomesticButton.Pressed += () => SetScoutScope(TransferMarketScope.Domestic);
        scopeRow.AddChild(_scopeDomesticButton);

        _scopeEuroButton = SecondaryButton("Avrupa Eliti");
        _scopeEuroButton.Pressed += () => SetScoutScope(TransferMarketScope.EuropeanElite);
        scopeRow.AddChild(_scopeEuroButton);

        _scopeFreeButton = SecondaryButton("Serbestler");
        _scopeFreeButton.Pressed += () => SetScoutScope(TransferMarketScope.FreeAgents);
        scopeRow.AddChild(_scopeFreeButton);

        _scoutCandidateList = new ItemList
        {
            Name = "ScoutCandidateList",
            CustomMinimumSize = new Vector2(0, 175),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_scoutCandidateList);
        _scoutCandidateList.ItemSelected += OnScoutCandidateSelected;
        scoutCard.AddChild(_scoutCandidateList);

        _scoutCandidateSummaryPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _scoutCandidateSummaryPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        var candidateSummary = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        candidateSummary.AddThemeConstantOverride("h_separation", 6);
        candidateSummary.AddThemeConstantOverride("v_separation", 4);
        _scoutMarketValueBadgeLabel = CreateTransferSummaryBadge(candidateSummary, "Değer: —", CareerUiTheme.Accent);
        _scoutWageExpectationBadgeLabel = CreateTransferSummaryBadge(candidateSummary, "Maaş: —", CareerUiTheme.Data);
        _scoutTransferChanceBadgeLabel = CreateTransferSummaryBadge(candidateSummary, "İhtimal: —", CareerUiTheme.ActionBright);
        _scoutCandidateSummaryPanel.AddChild(candidateSummary);
        scoutCard.AddChild(_scoutCandidateSummaryPanel);

        _scoutCandidateDetailLabel = BodyLabel("ScoutCandidateDetailLabel", muted: true, autowrap: true);
        _scoutCandidateDetailLabel.Text = "Aday seç — gözlemci raporu ve tahmini maliyet burada gösterilir.";
        scoutCard.AddChild(_scoutCandidateDetailLabel);

        _scoutComparisonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _scoutComparisonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());
        _scoutComparisonLabel = BodyLabel("ScoutComparisonLabel", muted: true, autowrap: true);
        _scoutComparisonLabel.Text = "3 Aday Kıyaslaması yükleniyor...";
        _scoutComparisonPanel.AddChild(_scoutComparisonLabel);
        scoutCard.AddChild(_scoutComparisonPanel);

        _shortlistTargetLabel = BodyLabel("ShortlistTargetLabel", autowrap: true);
        scoutCard.AddChild(_shortlistTargetLabel);

        var targetRow = ActionFlow();
        scoutCard.AddChild(targetRow);
        _suggestTargetButton = PrimaryButton("İzlemeye Al");
        _suggestTargetButton.Pressed += AddSelectedScoutCandidate;
        targetRow.AddChild(_suggestTargetButton);
        _openProcessButton = AccentButton("Görüşme Başlat");
        _openProcessButton.Pressed += () =>
        {
            Apply(_controller.OpenTransferProcessFromOldestTarget());
            ShowTransferSubTab(0);
        };
        targetRow.AddChild(_openProcessButton);
        _openDossierFromScoutButton = SecondaryButton("Oyuncu Profili");
        _openDossierFromScoutButton.Pressed += OnInspectCandidateDossierPressed;
        targetRow.AddChild(_openDossierFromScoutButton);
        _dropTargetButton = SecondaryButton("Listeden Çıkar");
        _dropTargetButton.Pressed += () => Apply(_controller.DropOldestListedTransferTarget());
        targetRow.AddChild(_dropTargetButton);

        // ── RIGHT COLUMN: NEGOTIATION DESK (TAB 0) & NEEDS/OUTGOING (TAB 1) ────
        var rightCol = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.15f,
        };
        rightCol.AddThemeConstantOverride("separation", 8);
        transferSplitRow.AddChild(rightCol);

        // TAB 0: AKTİF GÖRÜŞME, BONSERVİS VE SÖZLEŞME MASASI
        var dealHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        dealHost.AddThemeConstantOverride("separation", 6);
        rightCol.AddChild(dealHost);
        _transferDealHost = dealHost;

        var dealCard = AddCard(dealHost, "AKTİF TRANSFER GÖRÜŞMESİ", emphasized: true);
        _transferDeskLabel = BodyLabel("TransferDeskLabel", muted: true, autowrap: true);
        dealCard.AddChild(_transferDeskLabel);

        _transferProcessLabel = BodyLabel("TransferProcessLabel", autowrap: true);
        dealCard.AddChild(_transferProcessLabel);

        var dealRow = ActionFlow();
        dealCard.AddChild(dealRow);
        _advanceDealButton = PrimaryButton("Süreci İlerlet");
        _advanceDealButton.Pressed += () => Apply(_controller.AdvanceOldestTransferStep());
        dealRow.AddChild(_advanceDealButton);
        _completeTransferButton = AccentButton("Transferi İmzala");
        _completeTransferButton.Pressed += () =>
            CompleteTransferAndCelebrate(_controller.CompleteOldestFinanciallyApprovedProcess);
        dealRow.AddChild(_completeTransferButton);
        _withdrawProcessButton = DangerButton("Görüşmeden Çekil");
        _withdrawProcessButton.Pressed += () => Apply(_controller.WithdrawOldestActiveTransferProcess());
        dealRow.AddChild(_withdrawProcessButton);
        _transferNegotiationToggle = SecondaryButton("Teklif & Sözleşme Şartları");
        dealRow.AddChild(_transferNegotiationToggle);

        var sportingRow = ActionFlow();
        sportingRow.Visible = false;
        dealCard.AddChild(sportingRow);
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

        var offerCard = AddCard(dealHost, "KULÜP BONSERVİS TEKLİFİ");
        _clubOfferLabel = BodyLabel("ClubOfferLabel", autowrap: true);
        offerCard.AddChild(_clubOfferLabel);
        var feeRow = ActionFlow();
        offerCard.AddChild(feeRow);
        var feeLabel = BodyLabel("TransferFeeLabel", muted: true);
        feeLabel.Text = "Bonservis:";
        feeRow.AddChild(feeLabel);
        _transferFeeInput = new SpinBox
        {
            MinValue = 100_000,
            MaxValue = Domain.Transfer.ClubOffer.MaxFee,
            Step = 250_000,
            Value = 5_000_000,
            Suffix = " TRY",
            CustomMinimumSize = new Vector2(180, 32),
        };
        feeRow.AddChild(_transferFeeInput);
        var offerRow = ActionFlow();
        offerCard.AddChild(offerRow);
        _submitClubOfferButton = AccentButton("Teklif Sun");
        _submitClubOfferButton.Pressed += () =>
            Apply(_controller.SubmitClubOffer((int)_transferFeeInput.Value));
        offerRow.AddChild(_submitClubOfferButton);
        _acceptClubOfferButton = PrimaryButton("Kulüp Yanıtı");
        _acceptClubOfferButton.Pressed += () => Apply(_controller.ResolvePendingClubResponse());
        offerRow.AddChild(_acceptClubOfferButton);
        _counterClubOfferButton = SecondaryButton("Güncelle");
        _counterClubOfferButton.Pressed += () =>
            Apply(_controller.CounterPendingClubOffer((int)_transferFeeInput.Value));
        offerRow.AddChild(_counterClubOfferButton);
        _rejectClubOfferButton = DangerButton("Geri Çek");
        _rejectClubOfferButton.Pressed += () => Apply(_controller.RejectPendingClubOffer());
        offerRow.AddChild(_rejectClubOfferButton);

        var contractCard = AddCard(dealHost, "OYUNCU SÖZLEŞME TEKLİFİ");
        _contractProposalLabel = BodyLabel("ContractProposalLabel", autowrap: true);
        contractCard.AddChild(_contractProposalLabel);
        var contractTermsRow = ActionFlow();
        contractCard.AddChild(contractTermsRow);
        var wageLabel = BodyLabel("TransferWageLabel", muted: true);
        wageLabel.Text = "Haftalık:";
        contractTermsRow.AddChild(wageLabel);
        _transferWageInput = new SpinBox
        {
            MinValue = 1_000,
            MaxValue = Domain.Transfer.PlayerContractProposal.MaxWeeklyWage,
            Step = 500,
            Value = 25_000,
            Suffix = " TRY",
            CustomMinimumSize = new Vector2(160, 32),
        };
        contractTermsRow.AddChild(_transferWageInput);
        var yearsLabel = BodyLabel("ContractYearsLabel", muted: true);
        yearsLabel.Text = "Süre:";
        contractTermsRow.AddChild(yearsLabel);
        _contractYearsInput = new SpinBox
        {
            MinValue = Domain.Transfer.PlayerContractProposal.MinContractYears,
            MaxValue = Domain.Transfer.PlayerContractProposal.MaxContractYears,
            Step = 1,
            Value = 3,
            Suffix = " yıl",
            CustomMinimumSize = new Vector2(110, 32),
        };
        contractTermsRow.AddChild(_contractYearsInput);
        var proposalRow = ActionFlow();
        contractCard.AddChild(proposalRow);
        _submitContractProposalButton = AccentButton("Sözleşme Sun");
        _submitContractProposalButton.Pressed += () =>
            Apply(_controller.SubmitContractProposal(
                (int)_transferWageInput.Value,
                (int)_contractYearsInput.Value));
        proposalRow.AddChild(_submitContractProposalButton);
        _acceptContractProposalButton = PrimaryButton("Menajer Yanıtı");
        _acceptContractProposalButton.Pressed += () =>
            Apply(_controller.ResolvePendingContractResponse());
        proposalRow.AddChild(_acceptContractProposalButton);
        _counterContractProposalButton = SecondaryButton("Şartları Güncelle");
        _counterContractProposalButton.Pressed += () =>
            Apply(_controller.CounterPendingContractProposal(
                (int)_transferWageInput.Value,
                (int)_contractYearsInput.Value));
        proposalRow.AddChild(_counterContractProposalButton);
        _rejectContractProposalButton = DangerButton("Geri Çek");
        _rejectContractProposalButton.Pressed += () =>
            Apply(_controller.RejectPendingContractProposal());
        proposalRow.AddChild(_rejectContractProposalButton);

        var financeCard = AddCard(dealHost, "YÖNETİM & MALİ ONAY");
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
        _requestFinancialApprovalButton.Visible = false;
        _grantFinancialApprovalButton.Visible = false;
        _rejectFinancialApprovalButton.Visible = false;

        _transferNegotiationCards =
        [
            offerCard.GetParent<Control>(),
            contractCard.GetParent<Control>(),
            financeCard.GetParent<Control>(),
        ];
        // Show club offer & contract cards immediately so negotiation desk is ready to use
        offerCard.GetParent<Control>().Visible = true;
        contractCard.GetParent<Control>().Visible = true;
        financeCard.GetParent<Control>().Visible = false;

        _transferNegotiationToggle.Pressed += () =>
            SetTransferNegotiationExpanded(!_transferNegotiationCards[0].Visible);

        // TAB 1: KADRO İHTİYAÇLARI, BÜTÇE DETAYI VE GİDEN OYUNCULAR
        var needsHost = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Visible = false,
        };
        needsHost.AddThemeConstantOverride("separation", 8);
        rightCol.AddChild(needsHost);
        _transferNeedsHost = needsHost;

        var needsCard = AddCard(needsHost, "KADRO İHTİYAÇ ANALİZİ", emphasized: true);
        _scoutReportLabel = BodyLabel("ScoutReportLabel", autowrap: true);
        needsCard.AddChild(_scoutReportLabel);
        _transferNeedLabel = BodyLabel("TransferNeedLabel", autowrap: true);
        needsCard.AddChild(_transferNeedLabel);
        var needRow = ActionFlow();
        needsCard.AddChild(needRow);
        _refreshTransferNeedsButton = PrimaryButton("İhtiyaçları Tara");
        _refreshTransferNeedsButton.Pressed += () => Apply(_controller.RefreshTransferNeedSuggestions());
        needRow.AddChild(_refreshTransferNeedsButton);
        _declareTransferNeedButton = AccentButton("Mevki İhtiyacı Ekle");
        _declareTransferNeedButton.Pressed += () => Apply(_controller.DeclarePositionGapNeed());
        needRow.AddChild(_declareTransferNeedButton);
        _closeTransferNeedButton = SecondaryButton("İhtiyacı Kapat");
        _closeTransferNeedButton.Pressed += () => Apply(_controller.CloseOldestOpenTransferNeed());
        needRow.AddChild(_closeTransferNeedButton);

        var outgoingCard = AddCard(needsHost, "GİDENLER & KADRO KAPASİTESİ");
        _outgoingSaleLabel = BodyLabel("OutgoingSaleLabel", autowrap: true);
        outgoingCard.AddChild(_outgoingSaleLabel);
        var outgoingRow = ActionFlow();
        outgoingCard.AddChild(outgoingRow);
        var outgoingSell = PrimaryButton("Kenar Oyuncuyu Satışa Çıkar");
        outgoingSell.Pressed += () => Apply(_controller.SellFringePlayerFromManagedClub());
        outgoingRow.AddChild(outgoingSell);
        var outgoingRelease = SecondaryButton("Yer Aç (Serbest Bırak)");
        outgoingRelease.Pressed += () => Apply(_controller.ReleaseToFreeSquadCapacity());
        outgoingRow.AddChild(outgoingRelease);

        var budgetDetailCard = AddCard(needsHost, "BÜTÇE & PENCERE DETAYI");
        _transferScoutToggle = SecondaryButton("Raporu Göster / Gizle");
        _transferScoutToggle.Pressed += ToggleTransferScoutDetails;
        budgetDetailCard.AddChild(_transferScoutToggle);

        _transferScoutDetails = new VBoxContainer
        {
            Name = "TransferScoutDetails",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = true,
        };
        _transferScoutDetails.AddThemeConstantOverride("separation", 4);
        budgetDetailCard.AddChild(_transferScoutDetails);
        _transferScoutReportLabel = BodyLabel("TransferScoutReportLabel", autowrap: true);
        _transferScoutDetails.AddChild(_transferScoutReportLabel);
        _transferWindowLabel = BodyLabel("TransferWindowLabel", muted: true, autowrap: true);
        _transferScoutDetails.AddChild(_transferWindowLabel);
        _transferBudgetLabel = BodyLabel("TransferBudgetLabel", muted: true, autowrap: true);
        _transferScoutDetails.AddChild(_transferBudgetLabel);

        var windowRow = ActionFlow();
        windowRow.Visible = false;
        budgetDetailCard.AddChild(windowRow);
        _openTransferWindowButton = SecondaryButton("Pencere Aç");
        _openTransferWindowButton.Pressed += () => Apply(_controller.OpenTransferWindow());
        windowRow.AddChild(_openTransferWindowButton);
        _closeTransferWindowButton = SecondaryButton("Pencere Kapat");
        _closeTransferWindowButton.Pressed += () => Apply(_controller.CloseTransferWindow());
        windowRow.AddChild(_closeTransferWindowButton);

        ShowTransferSubTab(0);
        return page;
    }

    private void ShowTransferSubTab(int tabIndex)
    {
        if (_transferDealHost is not null) _transferDealHost.Visible = tabIndex == 0;
        if (_transferNeedsHost is not null) _transferNeedsHost.Visible = tabIndex == 1;

        if (_transferTabDealBtn is not null) CareerUiTheme.StyleTabButton(_transferTabDealBtn, tabIndex == 0);
        if (_transferTabNeedsBtn is not null) CareerUiTheme.StyleTabButton(_transferTabNeedsBtn, tabIndex == 1);
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
                ShowTransferSubTab(0);
                SetTransferNegotiationExpanded(true);
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionAdvanceProcess:
                Apply(_controller.AdvanceOldestTransferStep());
                ShowTransferSubTab(0);
                SetTransferNegotiationExpanded(true);
                return;
            case Application.CareerHub.Queries.OfficeNextStepGuide.ActionAnswerOffers:
                ShowTransferSubTab(0);
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

        _transferNegotiationCards[0].Visible = expanded;
        _transferNegotiationCards[1].Visible = expanded;
        _transferNegotiationCards[2].Visible = false;

        _transferNegotiationToggle.Text = expanded
            ? "Teklif Şartlarını Gizle"
            : "Teklif & Sözleşme Şartları";
    }
}
