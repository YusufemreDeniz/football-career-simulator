using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Ports;

public interface IPlayerContractProposalStore
{
    IReadOnlyList<PlayerContractProposal> Proposals { get; }

    PlayerContractProposal? Get(PlayerContractProposalId proposalId);

    IReadOnlyList<PlayerContractProposal> GetForProcess(TransferProcessId processId);

    void Upsert(PlayerContractProposal proposal);

    void ReplaceAll(IEnumerable<PlayerContractProposal> proposals);
}
