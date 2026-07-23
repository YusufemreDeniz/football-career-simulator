using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.Transfer.Ports;
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

    public TransferProcessService(
        ITransferProcessStore processStore,
        ITransferTargetStore targetStore,
        ITransferNeedStore needStore,
        IManagerCareerStore managerCareerStore)
    {
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _targetStore = targetStore ?? throw new ArgumentNullException(nameof(targetStore));
        _needStore = needStore ?? throw new ArgumentNullException(nameof(needStore));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
    }

    public TransferProcess OpenFromListedTarget(TransferTargetId targetId, GameDate day)
    {
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

    public TransferProcess RequestSportingApproval(TransferProcessId processId)
    {
        EnsureManagerCanDecide(Require(processId).BuyingClubId);
        var updated = Require(processId).RequestSportingApproval();
        _processStore.Upsert(updated);
        return updated;
    }

    public TransferProcess GrantSportingApproval(TransferProcessId processId)
    {
        EnsureManagerCanDecide(Require(processId).BuyingClubId);
        var updated = Require(processId).GrantSportingApproval();
        _processStore.Upsert(updated);
        return updated;
    }

    public TransferProcess RejectSportingApproval(
        TransferProcessId processId,
        string reasonCode,
        GameDate day)
    {
        EnsureManagerCanDecide(Require(processId).BuyingClubId);
        var updated = Require(processId).RejectSportingApproval(reasonCode, day);
        _processStore.Upsert(updated);
        return updated;
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

    private void EnsureManagerCanDecide(ClubId buyingClubId)
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId }
            || clubId.Value != buyingClubId.Value)
        {
            throw new TransferInvariantViolationException(
                "Only the employed manager of the buying club can make sporting decisions.");
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
