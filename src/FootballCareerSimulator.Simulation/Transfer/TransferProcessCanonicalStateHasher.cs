using System.Text;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Simulation.Transfer;

public static class TransferProcessCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<TransferProcess> processes)
    {
        ArgumentNullException.ThrowIfNull(processes);

        var builder = new StringBuilder("TransferProcesses=");
        foreach (var process in processes.OrderBy(p => p.ProcessId.Value))
        {
            builder.Append("P=").Append(process.ProcessId.Value)
                .Append(";N=").Append(process.NeedId.Value)
                .Append(";T=").Append(process.TargetId.Value)
                .Append(";B=").Append(process.BuyingClubId.Value)
                .Append(";Y=").Append(process.PlayerId.Value)
                .Append(";S=").Append(process.SellingClubId?.Value.ToString() ?? "-")
                .Append(";F=").Append(process.IsFreeAgent ? 1 : 0)
                .Append(";U=").Append((int)process.Status)
                .Append(";R=").Append(process.FailureReasonCode ?? "-")
                .Append(";O=").Append(process.OpenedOn.DayNumber)
                .Append(";X=").Append(process.TerminalOn?.DayNumber.ToString() ?? "-")
                .Append('|');
        }

        return builder.ToString();
    }
}
