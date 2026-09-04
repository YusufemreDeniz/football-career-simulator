using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Infrastructure;

public sealed class InMemoryTransferProcessStore : ITransferProcessStore
{
    private readonly Dictionary<long, TransferProcess> _byId = new();

    public IReadOnlyList<TransferProcess> Processes =>
        _byId.Values.OrderBy(p => p.ProcessId.Value).ToArray();

    public TransferProcess? Get(TransferProcessId processId) =>
        _byId.TryGetValue(processId.Value, out var process) ? process : null;

    public IReadOnlyList<TransferProcess> GetForBuyingClub(ClubId clubId) =>
        _byId.Values
            .Where(p => p.BuyingClubId.Value == clubId.Value)
            .OrderBy(p => p.ProcessId.Value)
            .ToArray();

    public void Upsert(TransferProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);
        _byId[process.ProcessId.Value] = process;
    }

    public void ReplaceAll(IEnumerable<TransferProcess> processes)
    {
        ArgumentNullException.ThrowIfNull(processes);
        _byId.Clear();
        foreach (var process in processes)
        {
            _byId[process.ProcessId.Value] = process;
        }
    }
}
