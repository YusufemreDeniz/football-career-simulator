using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Ports;

public interface ITransferNeedStore
{
    IReadOnlyList<TransferNeed> Needs { get; }

    TransferNeed? Get(TransferNeedId needId);

    IReadOnlyList<TransferNeed> GetForClub(ClubId clubId);

    void Upsert(TransferNeed need);

    void ReplaceAll(IEnumerable<TransferNeed> needs);
}
