using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.Transfer.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Transfer Completion process manager: Contract + Squad owner geçişleri.
/// İlişki / diyalog / medya / bütçe rezervasyonu yok.
/// </summary>
public sealed class TransferCompletionService
{
    private readonly ITransferProcessStore _processStore;
    private readonly IPlayerContractProposalStore _proposalStore;
    private readonly IClubOfferStore _offerStore;
    private readonly ContractRegistrationService _registration;
    private readonly ClubSquadService _clubSquad;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly ITransferWindowQuery _transferWindow;
    private readonly ClubTransferBudgetService? _transferBudget;

    public TransferCompletionService(
        ITransferProcessStore processStore,
        IPlayerContractProposalStore proposalStore,
        IClubOfferStore offerStore,
        ContractRegistrationService registration,
        ClubSquadService clubSquad,
        IManagerCareerStore managerCareerStore,
        ITransferWindowQuery? transferWindow = null,
        ClubTransferBudgetService? transferBudget = null)
    {
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _proposalStore = proposalStore ?? throw new ArgumentNullException(nameof(proposalStore));
        _offerStore = offerStore ?? throw new ArgumentNullException(nameof(offerStore));
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
        _clubSquad = clubSquad ?? throw new ArgumentNullException(nameof(clubSquad));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _transferWindow = transferWindow ?? AlwaysOpenTransferWindowQuery.Instance;
        _transferBudget = transferBudget;
    }

    public TransferProcess Complete(
        TransferProcessId processId,
        GameDate day,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        var process = Require(processId);
        EnsureActor(process.BuyingClubId, actor);

        if (process.Status is TransferProcessStatus.Completed or TransferProcessStatus.Archived)
        {
            return process.Status == TransferProcessStatus.Archived
                ? process
                : Persist(process.Archive(day));
        }

        if (process.Status is not (
            TransferProcessStatus.FinancialApproved
            or TransferProcessStatus.CompletionPending))
        {
            throw new TransferInvariantViolationException(
                "Completion requires financial approval or an open completion.");
        }

        // Normal completion requires an open window; in-progress completion may finish while closed.
        if (process.Status == TransferProcessStatus.FinancialApproved && !_transferWindow.IsOpen)
        {
            throw new TransferInvariantViolationException(
                "Transfer window is closed; cannot start transfer completion.");
        }

        var proposal = RequireAcceptedProposal(processId);
        process = Persist(process.StartCompletion());

        _registration.ActivateContractForTransfer(
            process.PlayerId,
            process.BuyingClubId,
            day,
            proposal.WeeklyWage,
            proposal.ContractYears);

        var clubIds = process.SellingClubId is { } selling
            ? new[] { process.BuyingClubId.Value, selling.Value }
            : new[] { process.BuyingClubId.Value };
        _clubSquad.SyncClubs(clubIds, day);

        process = Persist(process.MarkCompleted(day));
        ApplyReservedFee(process);
        return Persist(process.Archive(day));
    }

    private void ApplyReservedFee(TransferProcess process)
    {
        if (_transferBudget is null || process.IsFreeAgent)
        {
            return;
        }

        var fee = TransferBudgetFeeResolver.ResolveActiveFee(_offerStore, process.ProcessId);
        if (fee <= 0)
        {
            return;
        }

        try
        {
            _transferBudget.ApplyReservedSpend(process.BuyingClubId, fee);
        }
        catch (ClubGovernanceInvariantViolationException ex)
        {
            throw new TransferInvariantViolationException(ex.Message);
        }
    }

    private PlayerContractProposal RequireAcceptedProposal(TransferProcessId processId) =>
        _proposalStore.GetForProcess(processId)
            .LastOrDefault(p => p.Status == PlayerContractProposalStatus.Accepted)
        ?? throw new TransferInvariantViolationException(
            $"No accepted contract proposal for process #{processId.Value}.");

    private TransferProcess Persist(TransferProcess process)
    {
        _processStore.Upsert(process);
        return process;
    }

    private TransferProcess Require(TransferProcessId processId) =>
        _processStore.Get(processId)
        ?? throw new TransferInvariantViolationException($"Transfer process #{processId.Value} not found.");

    private void EnsureActor(ClubId buyingClubId, TransferActingParty actor) =>
        TransferActorGuard.EnsureBuyingClubActor(
            _managerCareerStore,
            buyingClubId,
            actor,
            "Only the employed manager of the buying club can complete a transfer.");
}
