using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Transfer penceresi kapanınca müzakere süreçlerini expire eder;
/// FinancialApproved / CompletionPending ve diğer aktifleri sonraki pencereye taşır.
/// </summary>
public sealed class TransferWindowCloseService
{
    public const string WindowClosedReason = "TransferWindowClosed";

    private readonly ITransferProcessStore _processStore;
    private readonly IClubOfferStore _offerStore;
    private readonly IPlayerContractProposalStore _proposalStore;
    private readonly ClubTransferBudgetService? _transferBudget;

    public TransferWindowCloseService(
        ITransferProcessStore processStore,
        IClubOfferStore offerStore,
        IPlayerContractProposalStore proposalStore,
        ClubTransferBudgetService? transferBudget = null)
    {
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _offerStore = offerStore ?? throw new ArgumentNullException(nameof(offerStore));
        _proposalStore = proposalStore ?? throw new ArgumentNullException(nameof(proposalStore));
        _transferBudget = transferBudget;
    }

    public TransferWindowCloseOutcome ApplyWindowClosed(GameDate day)
    {
        var expired = 0;
        var carried = 0;

        foreach (var process in _processStore.Processes.Where(p => p.IsActive).ToArray())
        {
            if (TransferProcess.IsExpiredByTransferWindowClose(process.Status))
            {
                ReleaseReservedFee(process);
                SupersedePendingArtifacts(process.ProcessId);
                var terminal = process.Expire(WindowClosedReason, day);
                _processStore.Upsert(terminal);
                _processStore.Upsert(terminal.Archive(day));
                expired++;
                continue;
            }

            if (TransferProcess.IsCarriedAcrossTransferWindowClose(process.Status))
            {
                carried++;
            }
        }

        return new TransferWindowCloseOutcome(expired, carried);
    }

    private void ReleaseReservedFee(TransferProcess process)
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
            _transferBudget.Release(process.BuyingClubId, fee);
        }
        catch (ClubGovernanceInvariantViolationException)
        {
            // Expire must proceed even if reservation was already released.
        }
    }

    private void SupersedePendingArtifacts(TransferProcessId processId)
    {
        foreach (var offer in _offerStore.GetForProcess(processId).Where(o => o.IsPending))
        {
            _offerStore.Upsert(offer.Supersede());
        }

        foreach (var proposal in _proposalStore.GetForProcess(processId).Where(p => p.IsPending))
        {
            _proposalStore.Upsert(proposal.Supersede());
        }
    }
}

public sealed record TransferWindowCloseOutcome(int ExpiredCount, int CarriedCount);
