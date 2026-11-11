using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Services;

internal static class TransferWageResolver
{
    public static int ResolveAcceptedWeeklyWage(
        IPlayerContractProposalStore proposalStore,
        TransferProcessId processId) =>
        proposalStore.GetForProcess(processId)
            .LastOrDefault(p => p.Status == PlayerContractProposalStatus.Accepted)
            ?.WeeklyWage
        ?? 0;
}
