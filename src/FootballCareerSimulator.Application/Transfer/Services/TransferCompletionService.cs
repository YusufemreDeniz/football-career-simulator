using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.Transfer.Ports;
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
    private readonly ContractRegistrationService _registration;
    private readonly ClubSquadService _clubSquad;
    private readonly IManagerCareerStore _managerCareerStore;

    public TransferCompletionService(
        ITransferProcessStore processStore,
        IPlayerContractProposalStore proposalStore,
        ContractRegistrationService registration,
        ClubSquadService clubSquad,
        IManagerCareerStore managerCareerStore)
    {
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _proposalStore = proposalStore ?? throw new ArgumentNullException(nameof(proposalStore));
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
        _clubSquad = clubSquad ?? throw new ArgumentNullException(nameof(clubSquad));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
    }

    public TransferProcess Complete(TransferProcessId processId, GameDate day)
    {
        var process = Require(processId);
        EnsureManagerOfBuyingClub(process.BuyingClubId);

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
        return Persist(process.Archive(day));
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

    private void EnsureManagerOfBuyingClub(ClubId buyingClubId)
    {
        if (_managerCareerStore.Career.ActiveEmployment is not { ClubId: var clubId }
            || clubId.Value != buyingClubId.Value)
        {
            throw new TransferInvariantViolationException(
                "Only the employed manager of the buying club can complete a transfer.");
        }
    }
}
