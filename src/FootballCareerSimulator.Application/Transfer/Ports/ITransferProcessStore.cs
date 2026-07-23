using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Ports;

public interface ITransferProcessStore
{
    IReadOnlyList<TransferProcess> Processes { get; }

    TransferProcess? Get(TransferProcessId processId);

    IReadOnlyList<TransferProcess> GetForBuyingClub(ClubId clubId);

    void Upsert(TransferProcess process);

    void ReplaceAll(IEnumerable<TransferProcess> processes);
}
