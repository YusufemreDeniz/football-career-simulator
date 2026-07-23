using System.Text;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Simulation.Transfer;

public static class PlayerContractProposalCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<PlayerContractProposal> proposals)
    {
        ArgumentNullException.ThrowIfNull(proposals);

        var builder = new StringBuilder("ContractProposals=");
        foreach (var proposal in proposals.OrderBy(p => p.ProposalId.Value))
        {
            builder.Append("P=").Append(proposal.ProposalId.Value)
                .Append(";Proc=").Append(proposal.ProcessId.Value)
                .Append(";R=").Append(proposal.Round)
                .Append(";W=").Append(proposal.WeeklyWage)
                .Append(";Y=").Append(proposal.ContractYears)
                .Append(";S=").Append((int)proposal.Status)
                .Append(";D=").Append(proposal.SubmittedOn.DayNumber)
                .Append('|');
        }

        return builder.ToString();
    }
}
