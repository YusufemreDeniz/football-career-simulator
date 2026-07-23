using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Infrastructure;

public sealed class InMemoryPlayerContractProposalStore : IPlayerContractProposalStore
{
    private readonly Dictionary<long, PlayerContractProposal> _byId = new();

    public IReadOnlyList<PlayerContractProposal> Proposals =>
        _byId.Values.OrderBy(p => p.ProposalId.Value).ToArray();

    public PlayerContractProposal? Get(PlayerContractProposalId proposalId) =>
        _byId.TryGetValue(proposalId.Value, out var proposal) ? proposal : null;

    public IReadOnlyList<PlayerContractProposal> GetForProcess(TransferProcessId processId) =>
        _byId.Values
            .Where(p => p.ProcessId.Value == processId.Value)
            .OrderBy(p => p.Round)
            .ThenBy(p => p.ProposalId.Value)
            .ToArray();

    public void Upsert(PlayerContractProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        _byId[proposal.ProposalId.Value] = proposal;
    }

    public void ReplaceAll(IEnumerable<PlayerContractProposal> proposals)
    {
        ArgumentNullException.ThrowIfNull(proposals);
        _byId.Clear();
        foreach (var proposal in proposals)
        {
            _byId[proposal.ProposalId.Value] = proposal;
        }
    }
}
