using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.Transfer.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Transfer Process: açılış + Sporting Approval. Müzakere / financial / completion yok.
/// </summary>
public sealed class TransferProcessService
{
    private readonly ITransferProcessStore _processStore;
    private readonly ITransferTargetStore _targetStore;
    private readonly ITransferNeedStore _needStore;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly ITransferWindowQuery _transferWindow;
    private readonly IClubOfferStore? _offerStore;
    private readonly IPlayerContractProposalStore? _proposalStore;
    private readonly ClubTransferBudgetService? _transferBudget;
    private readonly ClubWageBudgetService? _wageBudget;

    public TransferProcessService(
        ITransferProcessStore processStore,
        ITransferTargetStore targetStore,
        ITransferNeedStore needStore,
        IManagerCareerStore managerCareerStore,
        ITransferWindowQuery? transferWindow = null,
        IClubOfferStore? offerStore = null,
        ClubTransferBudgetService? transferBudget = null,
        IPlayerContractProposalStore? proposalStore = null,
        ClubWageBudgetService? wageBudget = null)
    {
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _targetStore = targetStore ?? throw new ArgumentNullException(nameof(targetStore));
        _needStore = needStore ?? throw new ArgumentNullException(nameof(needStore));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _transferWindow = transferWindow ?? AlwaysOpenTransferWindowQuery.Instance;
        _offerStore = offerStore;
        _transferBudget = transferBudget;
        _proposalStore = proposalStore;
        _wageBudget = wageBudget;
    }

    public TransferProcess OpenFromListedTarget(TransferTargetId targetId, GameDate day)
    {
        EnsureTransferWindowOpen();
        var target = _targetStore.Get(targetId)
            ?? throw new TransferInvariantViolationException($"Transfer target #{targetId.Value} not found.");
        if (!target.IsListed)
        {
            throw new TransferInvariantViolationException("Only listed targets can open a process.");
        }

        var need = _needStore.Get(target.NeedId)
            ?? throw new TransferInvariantViolationException($"Transfer need #{target.NeedId.Value} not found.");
        if (!need.IsOpen)
        {
            throw new TransferInvariantViolationException("Transfer need is closed.");
        }

        if (need.ClubId.Value != target.ClubId.Value)
        {
            throw new TransferInvariantViolationException("Need and target club mismatch.");
        }

        var existing = _processStore.GetForBuyingClub(target.ClubId)
            .FirstOrDefault(p => p.IsActive && p.TargetId.Value == targetId.Value);
        if (existing is not null)
        {
            return existing;
        }

        var sellingClubId = DecodeSyntheticClubId(target.PlayerId);
        var isFreeAgent = sellingClubId is null;
        if (!isFreeAgent && sellingClubId!.Value.Value == target.ClubId.Value)
        {
            throw new TransferInvariantViolationException(
                "Cannot open process for a player already at the buying club.");
        }

        var maxId = _processStore.Processes.Select(p => p.ProcessId.Value).DefaultIfEmpty(0).Max();
        var process = TransferProcess.OpenFromTarget(
            new TransferProcessId(maxId + 1),
            target.NeedId,
            target.TargetId,
            target.ClubId,
            target.PlayerId,
            sellingClubId,
            isFreeAgent,
            day);
        _processStore.Upsert(process);
        return process;
    }

    public TransferProcess OpenOldestListedTargetForClub(ClubId clubId, GameDate day)
    {
        var target = _targetStore.GetForClub(clubId)
            .Where(t => t.IsListed)
            .OrderBy(t => t.TargetId.Value)
            .FirstOrDefault()
            ?? throw new TransferInvariantViolationException("No listed transfer target for club.");

        return OpenFromListedTarget(target.TargetId, day);
    }

    public TransferProcess RequestSportingApproval(
        TransferProcessId processId,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        EnsureActor(Require(processId).BuyingClubId, actor);
        var updated = Require(processId).RequestSportingApproval();
        _processStore.Upsert(updated);
        return updated;
    }

    public TransferProcess GrantSportingApproval(
        TransferProcessId processId,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        EnsureActor(Require(processId).BuyingClubId, actor);
        var updated = Require(processId).GrantSportingApproval();
        _processStore.Upsert(updated);
        return updated;
    }

    public TransferProcess RejectSportingApproval(
        TransferProcessId processId,
        string reasonCode,
        GameDate day,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        EnsureActor(Require(processId).BuyingClubId, actor);
        var updated = Require(processId).RejectSportingApproval(reasonCode, day);
        _processStore.Upsert(updated);
        return updated;
    }

    public TransferProcess RequestFinancialApproval(
        TransferProcessId processId,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        EnsureActor(Require(processId).BuyingClubId, actor);
        var updated = Require(processId).RequestFinancialApproval();
        _processStore.Upsert(updated);
        return updated;
    }

    public TransferProcess GrantFinancialApproval(
        TransferProcessId processId,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        EnsureActor(Require(processId).BuyingClubId, actor);
        var updated = Require(processId).GrantFinancialApproval();
        _processStore.Upsert(updated);
        return updated;
    }

    public TransferProcess RejectFinancialApproval(
        TransferProcessId processId,
        string reasonCode,
        GameDate day,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        var process = Require(processId);
        EnsureActor(process.BuyingClubId, actor);
        ReleaseReservedFee(process);
        ReleaseReservedWage(process);
        var updated = process.RejectFinancialApproval(reasonCode, day);
        _processStore.Upsert(updated);
        return updated;
    }

    private void ReleaseReservedFee(TransferProcess process)
    {
        if (_transferBudget is null || _offerStore is null || process.IsFreeAgent)
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
            _transferBudget.Release(process.BuyingClubId, fee);
        }
        catch (ClubGovernanceInvariantViolationException ex)
        {
            throw new TransferInvariantViolationException(ex.Message);
        }
    }

    private void ReleaseReservedWage(TransferProcess process)
    {
        if (_wageBudget is null || _proposalStore is null)
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

    public TransferProcess Withdraw(TransferProcessId processId, GameDate day)
    {
        var process = Require(processId);
        var updated = process.Withdraw(day);
        _processStore.Upsert(updated);
        return updated;
    }

    public TransferProcess Fail(TransferProcessId processId, string reasonCode, GameDate day)
    {
        var process = Require(processId);
        var updated = process.Fail(reasonCode, day);
        _processStore.Upsert(updated);
        return updated;
    }

    public TransferProcess Archive(TransferProcessId processId, GameDate day)
    {
        var process = Require(processId);
        var updated = process.Archive(day);
        _processStore.Upsert(updated);
        return updated;
    }

    private TransferProcess Require(TransferProcessId processId) =>
        _processStore.Get(processId)
        ?? throw new TransferInvariantViolationException($"Transfer process #{processId.Value} not found.");

    private void EnsureActor(ClubId buyingClubId, TransferActingParty actor) =>
        TransferActorGuard.EnsureBuyingClubActor(
            _managerCareerStore,
            buyingClubId,
            actor,
            "Only the employed manager of the buying club can make sporting decisions.");

    private void EnsureTransferWindowOpen()
    {
        if (!_transferWindow.IsOpen)
        {
            throw new TransferInvariantViolationException(
                "Transfer window is closed; cannot open a new transfer process.");
        }
    }

    private static ClubId? DecodeSyntheticClubId(PlayerId playerId)
    {
        var value = playerId.Value;
        if (value <= 0)
        {
            return null;
        }

        var clubId = (value - 1) / 1000L;
        return clubId <= 0 ? null : new ClubId(clubId);
    }
}
