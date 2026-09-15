using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;
using Godot;

namespace FootballCareerSimulator.Presentation;

public partial class CareerHubScreen
{
    private Control BuildTransferPage()
    {
        var page = PageRoot();

        // Ana overview kart — güçlü neon
        var overviewNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        overviewNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel(strongGlow: true));
        page.AddChild(overviewNeonPanel);
        var overviewCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        overviewCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        overviewNeonPanel.AddChild(overviewCard);
        overviewCard.AddChild(UiComponents.PageHeader("🔄", "TRANSFER"));
        overviewCard.AddChild(UiComponents.Divider(1));

        _transferWelcomePanel = new PanelContainer
        {
            Visible = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _transferWelcomePanel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.28f));
        var welcomeCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        welcomeCard.AddThemeConstantOverride("separation", 4);
        _transferWelcomeHeadlineLabel = new Label { Text = "✍ HOŞ GELDİN" };
        CareerUiTheme.StyleHeadline(_transferWelcomeHeadlineLabel);
        _transferWelcomeHeadlineLabel.AddThemeColorOverride("font_color", CareerUiTheme.ActionBright);
        _transferWelcomeHeadlineLabel.AddThemeFontSizeOverride("font_size", CareerUiTheme.FontSize(20));
        welcomeCard.AddChild(_transferWelcomeHeadlineLabel);
        _transferWelcomeMetaLabel = BodyLabel("TransferWelcomeMetaLabel", autowrap: true);
        welcomeCard.AddChild(_transferWelcomeMetaLabel);
        _transferWelcomePanel.AddChild(welcomeCard);
        overviewCard.AddChild(_transferWelcomePanel);

        var budgetRow = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        budgetRow.AddThemeConstantOverride("separation", 8);
        overviewCard.AddChild(budgetRow);

        _transferBudgetBadgePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _transferBudgetBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Accent, 0.28f));
        _transferBudgetBadgeLabel = new Label { Text = "💰 Bütçe —" };
        CareerUiTheme.StyleEyebrow(_transferBudgetBadgeLabel, CareerUiTheme.Accent);
        _transferBudgetBadgePanel.AddChild(_transferBudgetBadgeLabel);
        budgetRow.AddChild(_transferBudgetBadgePanel);

        _wageBudgetBadgePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _wageBudgetBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.Data, 0.28f));
        _wageBudgetBadgeLabel = new Label { Text = "💵 Maaş: —" };
        CareerUiTheme.StyleEyebrow(_wageBudgetBadgeLabel, CareerUiTheme.Data);
        _wageBudgetBadgePanel.AddChild(_wageBudgetBadgeLabel);
        budgetRow.AddChild(_wageBudgetBadgePanel);

        _transferWindowBadgePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _transferWindowBadgePanel.AddThemeStyleboxOverride("panel", CareerUiTheme.BadgePillPanel(CareerUiTheme.ActionBright, 0.32f));
        _transferWindowBadgeLabel = new Label { Text = "🟢 Pencere Açık" };
        CareerUiTheme.StyleEyebrow(_transferWindowBadgeLabel, CareerUiTheme.ActionBright);
        _transferWindowBadgePanel.AddChild(_transferWindowBadgeLabel);
        budgetRow.AddChild(_transferWindowBadgePanel);

        _transferDeskLabel = BodyLabel("TransferDeskLabel", autowrap: true);
        overviewCard.AddChild(_transferDeskLabel);

        _transferNextStepButton = PrimaryButton("Sıradaki Adım");
        _transferNextStepButton.Visible = false;
        _transferNextStepButton.Pressed += OnTransferNextStepPressed;
        overviewCard.AddChild(_transferNextStepButton);

        _transferScoutToggle = SecondaryButton("Bütçe Detayı");
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
        windowRow.Visible = false;
        overviewCard.AddChild(windowRow);
        _openTransferWindowButton = SecondaryButton("Pencere Aç");
        _openTransferWindowButton.Pressed += () => Apply(_controller.OpenTransferWindow());
        windowRow.AddChild(_openTransferWindowButton);
        _closeTransferWindowButton = SecondaryButton("Pencere Kapat");
        _closeTransferWindowButton.Pressed += () => Apply(_controller.CloseTransferWindow());
        windowRow.AddChild(_closeTransferWindowButton);

        _transferBentoGrid = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _transferBentoGrid.AddThemeConstantOverride("h_separation", CareerUiTheme.SpaceM);
        _transferBentoGrid.AddThemeConstantOverride("v_separation", CareerUiTheme.SpaceM);
        page.AddChild(_transferBentoGrid);

        // 1 — Kadro İhtiyaçları
        var needsNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        needsNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        _transferBentoGrid.AddChild(needsNeonPanel);
        var needsCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        needsCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        needsNeonPanel.AddChild(needsCard);
        needsCard.AddChild(UiComponents.PageHeader("🎯", "İHTİYAÇ"));
        needsCard.AddChild(UiComponents.Divider(1));

        _scoutReportLabel = BodyLabel("ScoutReportLabel", autowrap: true);
        needsCard.AddChild(_scoutReportLabel);
        _transferNeedLabel = BodyLabel("TransferNeedLabel", autowrap: true);
        needsCard.AddChild(_transferNeedLabel);
        var needRow = ActionFlow();
        needsCard.AddChild(needRow);
        _refreshTransferNeedsButton = SecondaryButton("Tara");
        _refreshTransferNeedsButton.Pressed += () => Apply(_controller.RefreshTransferNeedSuggestions());
        needRow.AddChild(_refreshTransferNeedsButton);
        _declareTransferNeedButton = PrimaryButton("Ekle");
        _declareTransferNeedButton.Pressed += () => Apply(_controller.DeclarePositionGapNeed());
        needRow.AddChild(_declareTransferNeedButton);
        _closeTransferNeedButton = SecondaryButton("Kapat");
        _closeTransferNeedButton.Pressed += () => Apply(_controller.CloseOldestOpenTransferNeed());
        needRow.AddChild(_closeTransferNeedButton);

        // 2 — Önerilen / İzlenen
        var scoutNeonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scoutNeonPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        _transferBentoGrid.AddChild(scoutNeonPanel);
        var scoutCard = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scoutCard.AddThemeConstantOverride("separation", CareerUiTheme.SeparationCard);
        scoutNeonPanel.AddChild(scoutCard);
        scoutCard.AddChild(UiComponents.PageHeader("🔭", "OYUNCULAR"));
        scoutCard.AddChild(UiComponents.Divider(1));

        var scopeRow = ActionFlow();
        scoutCard.AddChild(scopeRow);

        _scopeAllButton = SecondaryButton("🌍 Tüm Dünya");
        _scopeAllButton.Pressed += () => SetScoutScope(TransferMarketScope.All);
        scopeRow.AddChild(_scopeAllButton);

        _scopeDomesticButton = SecondaryButton("🇹🇷 Süper Lig");
        _scopeDomesticButton.Pressed += () => SetScoutScope(TransferMarketScope.Domestic);
        scopeRow.AddChild(_scopeDomesticButton);

        _scopeEuroButton = SecondaryButton("⭐ Avrupa Eliti");
        _scopeEuroButton.Pressed += () => SetScoutScope(TransferMarketScope.EuropeanElite);
        scopeRow.AddChild(_scopeEuroButton);

        _scopeFreeButton = SecondaryButton("🆓 Serbestler");
        _scopeFreeButton.Pressed += () => SetScoutScope(TransferMarketScope.FreeAgents);
        scopeRow.AddChild(_scopeFreeButton);

        _scoutCandidateList = new ItemList
        {
            Name = "ScoutCandidateList",
            CustomMinimumSize = new Vector2(0, 260),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        CareerUiTheme.StyleList(_scoutCandidateList);
        _scoutCandidateList.ItemSelected += OnScoutCandidateSelected;
        scoutCard.AddChild(_scoutCandidateList);
        _scoutCandidateDetailLabel = BodyLabel("ScoutCandidateDetailLabel", muted: true, autowrap: true);
        _scoutCandidateDetailLabel.Text = "Aday seç — neden önerildiğini ve maliyeti gör.";
        scoutCard.AddChild(_scoutCandidateDetailLabel);
        _scoutCandidateSummaryPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _scoutCandidateSummaryPanel.AddThemeStyleboxOverride("panel", CareerUiTheme.NeonGlowPanel());
        var candidateSummary = new GridContainer { Columns = 1, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        candidateSummary.AddThemeConstantOverride("v_separation", 6);
        _scoutMarketValueBadgeLabel = CreateTransferSummaryBadge(candidateSummary, "💎 Piyasa Değeri: —", CareerUiTheme.Accent);
        _scoutWageExpectationBadgeLabel = CreateTransferSummaryBadge(candidateSummary, "💵 Maaş Beklentisi: —", CareerUiTheme.Data);
        _scoutTransferChanceBadgeLabel = CreateTransferSummaryBadge(candidateSummary, "🎯 Transfer İhtimali: —", CareerUiTheme.ActionBright);
        _scoutCandidateSummaryPanel.AddChild(candidateSummary);
        scoutCard.AddChild(_scoutCandidateSummaryPanel);
        _shortlistTargetLabel = BodyLabel("ShortlistTargetLabel", autowrap: true);
        scoutCard.AddChild(_shortlistTargetLabel);
        var targetRow = ActionFlow();
        scoutCard.AddChild(targetRow);
        _suggestTargetButton = PrimaryButton("İzlemeye Al");
        _suggestTargetButton.Pressed += AddSelectedScoutCandidate;
        targetRow.AddChild(_suggestTargetButton);
        _dropTargetButton = SecondaryButton("İzlemeyi Bırak");
        _dropTargetButton.Pressed += () => Apply(_controller.DropOldestListedTransferTarget());
        targetRow.AddChild(_dropTargetButton);
        _openProcessButton = AccentButton("Görüşme Başlat");
        _openProcessButton.Pressed += () => Apply(_controller.OpenTransferProcessFromOldestTarget());
        targetRow.AddChild(_openProcessButton);
        _openDossierFromScoutButton = SecondaryButton("Profil");
        _openDossierFromScoutButton.Pressed += OnInspectCandidateDossierPressed;
        targetRow.AddChild(_openDossierFromScoutButton);

        // 3 — Aktif Görüşmeler
        var dealCard = AddCard(_transferBentoGrid, "GÖRÜŞME", emphasized: true, expandVertical: true);
        _transferProcessLabel = BodyLabel("TransferProcessLabel", autowrap: true);
        dealCard.AddChild(_transferProcessLabel);
        var dealRow = ActionFlow();
        dealCard.AddChild(dealRow);
        _advanceDealButton = PrimaryButton("İlerle");
        _advanceDealButton.Pressed += () => Apply(_controller.AdvanceOldestTransferStep());
        dealRow.AddChild(_advanceDealButton);
        _withdrawProcessButton = SecondaryButton("Vazgeç");
        _withdrawProcessButton.Pressed += () => Apply(_controller.WithdrawOldestActiveTransferProcess());
        dealRow.AddChild(_withdrawProcessButton);
        _completeTransferButton = AccentButton("İmzala");
        _completeTransferButton.Pressed += () =>
            CompleteTransferAndCelebrate(_controller.CompleteOldestFinanciallyApprovedProcess);
        dealRow.AddChild(_completeTransferButton);

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

        var offerCard = AddCard(page, "Kulüp teklifi");
        _clubOfferLabel = BodyLabel("ClubOfferLabel", autowrap: true);
        offerCard.AddChild(_clubOfferLabel);
        var feeRow = ActionFlow();
        offerCard.AddChild(feeRow);
        var feeLabel = BodyLabel("TransferFeeLabel", muted: true);
        feeLabel.Text = "Bonservis";
        feeRow.AddChild(feeLabel);
        _transferFeeInput = new SpinBox
        {
            MinValue = 100_000,
            MaxValue = Domain.Transfer.ClubOffer.MaxFee,
            Step = 250_000,
            Value = 5_000_000,
            Suffix = " TRY",
            CustomMinimumSize = new Vector2(260, 48),
        };
        feeRow.AddChild(_transferFeeInput);
        var offerRow = ActionFlow();
        offerCard.AddChild(offerRow);
        _submitClubOfferButton = AccentButton("Teklif Sun");
        _submitClubOfferButton.Pressed += () =>
            Apply(_controller.SubmitClubOffer((int)_transferFeeInput.Value));
        offerRow.AddChild(_submitClubOfferButton);
        _acceptClubOfferButton = PrimaryButton("Kulüp Yanıtını Al");
        _acceptClubOfferButton.Pressed += () => Apply(_controller.ResolvePendingClubResponse());
        offerRow.AddChild(_acceptClubOfferButton);
        _rejectClubOfferButton = DangerButton("Teklifi Geri Çek");
        _rejectClubOfferButton.Pressed += () => Apply(_controller.RejectPendingClubOffer());
        offerRow.AddChild(_rejectClubOfferButton);
        _counterClubOfferButton = SecondaryButton("Teklifi Güncelle");
        _counterClubOfferButton.Pressed += () =>
            Apply(_controller.CounterPendingClubOffer((int)_transferFeeInput.Value));
        offerRow.AddChild(_counterClubOfferButton);

        var contractCard = AddCard(page, "Oyuncu sözleşmesi");
        _contractProposalLabel = BodyLabel("ContractProposalLabel", autowrap: true);
        contractCard.AddChild(_contractProposalLabel);
        var contractTermsRow = ActionFlow();
        contractCard.AddChild(contractTermsRow);
        var wageLabel = BodyLabel("TransferWageLabel", muted: true);
        wageLabel.Text = "Haftalık maaş";
        contractTermsRow.AddChild(wageLabel);
        _transferWageInput = new SpinBox
        {
            MinValue = 1_000,
            MaxValue = Domain.Transfer.PlayerContractProposal.MaxWeeklyWage,
            Step = 500,
            Value = 25_000,
            Suffix = " TRY",
            CustomMinimumSize = new Vector2(220, 48),
        };
        contractTermsRow.AddChild(_transferWageInput);
        var yearsLabel = BodyLabel("ContractYearsLabel", muted: true);
        yearsLabel.Text = "Süre";
        contractTermsRow.AddChild(yearsLabel);
        _contractYearsInput = new SpinBox
        {
            MinValue = Domain.Transfer.PlayerContractProposal.MinContractYears,
            MaxValue = Domain.Transfer.PlayerContractProposal.MaxContractYears,
            Step = 1,
            Value = 3,
            Suffix = " yıl",
            CustomMinimumSize = new Vector2(150, 48),
        };
        contractTermsRow.AddChild(_contractYearsInput);
        var proposalRow = ActionFlow();
        contractCard.AddChild(proposalRow);
        _submitContractProposalButton = AccentButton("Sözleşme Teklif Et");
        _submitContractProposalButton.Pressed += () =>
            Apply(_controller.SubmitContractProposal(
                (int)_transferWageInput.Value,
                (int)_contractYearsInput.Value));
        proposalRow.AddChild(_submitContractProposalButton);
        _acceptContractProposalButton = PrimaryButton("Menajer Yanıtını Al");
        _acceptContractProposalButton.Pressed += () =>
            Apply(_controller.ResolvePendingContractResponse());
        proposalRow.AddChild(_acceptContractProposalButton);
        _rejectContractProposalButton = DangerButton("Teklifi Geri Çek");
        _rejectContractProposalButton.Pressed += () =>
            Apply(_controller.RejectPendingContractProposal());
        proposalRow.AddChild(_rejectContractProposalButton);
        _counterContractProposalButton = SecondaryButton("Şartları Güncelle");
        _counterContractProposalButton.Pressed += () =>
            Apply(_controller.CounterPendingContractProposal(
                (int)_transferWageInput.Value,
                (int)_contractYearsInput.Value));
        proposalRow.AddChild(_counterContractProposalButton);

        var financeCard = AddCard(page, "Yönetim / imza");
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

        _transferNegotiationToggle = SecondaryButton("Teklif ayrıntılarını aç");
        dealCard.AddChild(_transferNegotiationToggle);
        _transferNegotiationCards =
        [
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

        // 4 — Giden Oyuncular
        var outgoingCard = AddCard(page, "GİDENLER");
        _outgoingSaleLabel = BodyLabel("OutgoingSaleLabel", autowrap: true);
        outgoingCard.AddChild(_outgoingSaleLabel);
        var outgoingRow = ActionFlow();
        outgoingCard.AddChild(outgoingRow);
        var outgoingSell = PrimaryButton("Satışa Çıkar");
        outgoingSell.Pressed += () => Apply(_controller.SellFringePlayerFromManagedClub());
        outgoingRow.AddChild(outgoingSell);
        var outgoingRelease = SecondaryButton("Yer Aç (serbest bırak)");
        outgoingRelease.Pressed += () => Apply(_controller.ReleaseToFreeSquadCapacity());
        outgoingRow.AddChild(outgoingRelease);

        RegisterMobileFocusLayout(
            page,
            HubPage.Transfer,
            Array.Empty<Control>(),
            ("ÖZET", new Control[] { overviewNeonPanel }),
            ("İHTİYAÇ", new Control[] { needsNeonPanel }),
            ("OYUNCU", new Control[] { scoutNeonPanel }),
            ("GÖRÜŞME", new Control[] { dealCard.GetParent<Control>() }),
            ("GİDEN", new Control[] { outgoingCard.GetParent<Control>() }));

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
            ? "Teklif ayrıntılarını gizle"
            : "Teklif ayrıntılarını düzenle";
    }

}
