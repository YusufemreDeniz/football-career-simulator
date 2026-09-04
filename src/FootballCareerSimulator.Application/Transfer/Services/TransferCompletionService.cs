using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Application.Transfer.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
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
    private readonly ClubWageBudgetService? _wageBudget;
    private readonly PromiseInvalidationService? _promiseInvalidation;
    private readonly TransferMemoryService? _transferMemory;
    private readonly ClubHistoryMemoryService? _clubHistoryMemory;
    private readonly RelationshipEvaluationService? _relationships;
    private readonly ITrainingPhysicalStateStore? _trainingStore;
    private readonly IClubSquadStore? _squadStore;

    public TransferCompletionService(
        ITransferProcessStore processStore,
        IPlayerContractProposalStore proposalStore,
        IClubOfferStore offerStore,
        ContractRegistrationService registration,
        ClubSquadService clubSquad,
        IManagerCareerStore managerCareerStore,
        ITransferWindowQuery? transferWindow = null,
        ClubTransferBudgetService? transferBudget = null,
        ClubWageBudgetService? wageBudget = null,
        PromiseInvalidationService? promiseInvalidation = null,
        TransferMemoryService? transferMemory = null,
        ClubHistoryMemoryService? clubHistoryMemory = null,
        RelationshipEvaluationService? relationships = null,
        ITrainingPhysicalStateStore? trainingStore = null,
        IClubSquadStore? squadStore = null)
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
        _wageBudget = wageBudget;
        _promiseInvalidation = promiseInvalidation;
        _transferMemory = transferMemory;
        _clubHistoryMemory = clubHistoryMemory;
        _relationships = relationships;
        _trainingStore = trainingStore;
        _squadStore = squadStore;
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

        _promiseInvalidation?.InvalidateForPlayerLeaving(process.PlayerId, day);
        _relationships?.MarkDormantForPlayerLeaving(process.PlayerId, day);

        var carriedPhysical = CapturePhysicalBeforeSquadSync(process);
        var clubIds = process.SellingClubId is { } selling
            ? new[] { process.BuyingClubId.Value, selling.Value }
            : new[] { process.BuyingClubId.Value };
        _clubSquad.SyncClubs(clubIds, day);
        RemountPhysicalAfterTransfer(process, carriedPhysical);

        process = Persist(process.MarkCompleted(day));
        _transferMemory?.RecordCompleted(process, day, ResolveInvolvedManager(process));
        _clubHistoryMemory?.RecordPlayerTransferClubs(process, day);
        ApplyReservedFee(process);
        ReleaseReservedWage(process);
        return Persist(process.Archive(day));
    }

    private ActorRef? ResolveInvolvedManager(TransferProcess process)
    {
        var career = _managerCareerStore.Career;
        if (!career.IsEmployed || career.ActiveEmployment is null)
        {
            return null;
        }

        var clubId = career.ActiveEmployment.ClubId;
        if (clubId == process.BuyingClubId
            || (process.SellingClubId is { } selling && clubId == selling))
        {
            return new ActorRef(ActorKind.Manager, career.ManagerId.Value);
        }

        return null;
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

    private void ReleaseReservedWage(TransferProcess process)
    {
        if (_wageBudget is null)
        {
            return;
        }

        var wage = TransferWageResolver.ResolveAcceptedWeeklyWage(_proposalStore, process.ProcessId);
        if (wage <= 0)
        {
            return;
        }

        try
        {
            _wageBudget.Release(process.BuyingClubId, wage);
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

    private (ClubId ClubId, int SlotIndex, PlayerPhysicalState State)? CapturePhysicalBeforeSquadSync(
        TransferProcess process)
    {
        if (_trainingStore is null || _squadStore is null || process.SellingClubId is not { } selling)
        {
            return null;
        }

        var member = _squadStore.Get(selling)?.Members.FirstOrDefault(m => m.PlayerId == process.PlayerId);
        if (member is null)
        {
            return null;
        }

        var state = _trainingStore.GetPhysical(selling, member.SlotIndex);
        return state is null ? null : (selling, member.SlotIndex, state);
    }

    private void RemountPhysicalAfterTransfer(
        TransferProcess process,
        (ClubId ClubId, int SlotIndex, PlayerPhysicalState State)? carried)
    {
        if (_trainingStore is null || _squadStore is null)
        {
            return;
        }

        if (carried is { } left)
        {
            var sellerStates = _trainingStore.PhysicalStates
                .Where(state => state.ClubId == left.ClubId)
                .Select(state => state.SlotIndex == left.SlotIndex
                    ? PlayerPhysicalState.CreateRested(left.ClubId, left.SlotIndex)
                    : state)
                .ToArray();
            _trainingStore.ReplacePhysicalStatesForClub(left.ClubId, sellerStates);
        }

        var buyerMember = _squadStore.Get(process.BuyingClubId)?
            .Members.FirstOrDefault(m => m.PlayerId == process.PlayerId);
        if (buyerMember is null)
        {
            return;
        }

        var remounted = carried is { } source
            ? source.State.Relocate(process.BuyingClubId, buyerMember.SlotIndex)
            : PlayerPhysicalState.CreateRested(process.BuyingClubId, buyerMember.SlotIndex);
        var buyerStates = _trainingStore.PhysicalStates
            .Where(state => state.ClubId == process.BuyingClubId && state.SlotIndex != buyerMember.SlotIndex)
            .Append(remounted)
            .ToArray();
        _trainingStore.ReplacePhysicalStatesForClub(process.BuyingClubId, buyerStates);
    }
}
