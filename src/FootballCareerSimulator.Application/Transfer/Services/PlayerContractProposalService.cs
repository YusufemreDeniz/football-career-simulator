using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.Transfer.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Player Contract Proposal iskeleti: teklif / karşı teklif / kabul / ret + maaş rezervasyonu.
/// </summary>
public sealed class PlayerContractProposalService
{
    private readonly IPlayerContractProposalStore _proposalStore;
    private readonly ITransferProcessStore _processStore;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly ITransferWindowQuery _transferWindow;
    private readonly ClubWageBudgetService? _wageBudget;

    public PlayerContractProposalService(
        IPlayerContractProposalStore proposalStore,
        ITransferProcessStore processStore,
        IManagerCareerStore managerCareerStore,
        ITransferWindowQuery? transferWindow = null,
        ClubWageBudgetService? wageBudget = null)
    {
        _proposalStore = proposalStore ?? throw new ArgumentNullException(nameof(proposalStore));
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _transferWindow = transferWindow ?? AlwaysOpenTransferWindowQuery.Instance;
        _wageBudget = wageBudget;
    }

    public PlayerContractProposal SubmitContractProposal(
        TransferProcessId processId,
        int weeklyWage,
        int contractYears,
        GameDate day,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        EnsureTransferWindowOpen();
        var process = RequireProcess(processId);
        EnsureActor(process.BuyingClubId, actor);

        if (process.Status is TransferProcessStatus.ClubAgreementReached
            or TransferProcessStatus.SportingApproved)
        {
            process = process.EnterPlayerNegotiation();
            _processStore.Upsert(process);
        }
        else if (process.Status != TransferProcessStatus.PlayerNegotiation)
        {
            throw new TransferInvariantViolationException(
                "Contract proposal requires club agreement (or sporting approval for free agents) "
                + "or an open player negotiation.");
        }

        if (_proposalStore.GetForProcess(processId).Any(p => p.IsPending))
        {
            throw new TransferInvariantViolationException(
                "A pending contract proposal already exists; accept, reject, or counter it first.");
        }

        var round = _proposalStore.GetForProcess(processId).Select(p => p.Round).DefaultIfEmpty(0).Max() + 1;
        var maxId = _proposalStore.Proposals.Select(p => p.ProposalId.Value).DefaultIfEmpty(0).Max();
        var proposal = PlayerContractProposal.Submit(
            new PlayerContractProposalId(maxId + 1),
            processId,
            round,
            weeklyWage,
            contractYears,
            day);
        _proposalStore.Upsert(proposal);
        return proposal;
    }

    public PlayerContractProposal AcceptPendingProposal(
        TransferProcessId processId,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        var process = RequireProcess(processId);
        EnsureActor(process.BuyingClubId, actor);
        if (!process.IsInPlayerNegotiation)
        {
            throw new TransferInvariantViolationException("No active player negotiation to accept.");
        }

        var pending = RequirePending(processId);
        ReserveWage(process.BuyingClubId, pending.WeeklyWage, pending.SubmittedOn);
        var accepted = pending.Accept();
        _proposalStore.Upsert(accepted);
        _processStore.Upsert(process.ReachPlayerAgreement());
        return accepted;
    }

    public PlayerContractProposal RejectPendingProposal(
        TransferProcessId processId,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        var process = RequireProcess(processId);
        EnsureActor(process.BuyingClubId, actor);
        if (!process.IsInPlayerNegotiation)
        {
            throw new TransferInvariantViolationException("No active player negotiation to reject.");
        }

        var pending = RequirePending(processId);
        var rejected = pending.Reject();
        _proposalStore.Upsert(rejected);
        return rejected;
    }

    public PlayerContractProposal CounterPendingProposal(
        TransferProcessId processId,
        int weeklyWage,
        int contractYears,
        GameDate day,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        EnsureTransferWindowOpen();
        var process = RequireProcess(processId);
        EnsureActor(process.BuyingClubId, actor);
        if (!process.IsInPlayerNegotiation)
        {
            throw new TransferInvariantViolationException("No active player negotiation to counter.");
        }

        var pending = RequirePending(processId);
        _proposalStore.Upsert(pending.Supersede());

        var round = pending.Round + 1;
        var maxId = _proposalStore.Proposals.Select(p => p.ProposalId.Value).DefaultIfEmpty(0).Max();
        var counter = PlayerContractProposal.Submit(
            new PlayerContractProposalId(maxId + 1),
            processId,
            round,
            weeklyWage,
            contractYears,
            day);
        _proposalStore.Upsert(counter);
        return counter;
    }

    private PlayerContractProposal RequirePending(TransferProcessId processId) =>
        _proposalStore.GetForProcess(processId).LastOrDefault(p => p.IsPending)
        ?? throw new TransferInvariantViolationException(
            $"No pending contract proposal for process #{processId.Value}.");

    private TransferProcess RequireProcess(TransferProcessId processId) =>
        _processStore.Get(processId)
        ?? throw new TransferInvariantViolationException($"Transfer process #{processId.Value} not found.");

    private void EnsureActor(ClubId buyingClubId, TransferActingParty actor) =>
        TransferActorGuard.EnsureBuyingClubActor(
            _managerCareerStore,
            buyingClubId,
            actor,
            "Only the employed manager of the buying club can manage contract proposals.");

    private void ReserveWage(ClubId clubId, int weeklyWage, GameDate day)
    {
        if (_wageBudget is null || weeklyWage <= 0)
        {
            return;
        }

        try
        {
            _wageBudget.Reserve(clubId, weeklyWage, day);
        }
        catch (ClubGovernanceInvariantViolationException ex)
        {
            throw new TransferInvariantViolationException(ex.Message);
        }
    }

    private void EnsureTransferWindowOpen()
    {
        if (!_transferWindow.IsOpen)
        {
            throw new TransferInvariantViolationException(
                "Transfer window is closed; cannot submit a contract proposal.");
        }
    }
}
