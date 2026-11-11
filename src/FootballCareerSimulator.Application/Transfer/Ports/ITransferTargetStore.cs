using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Ports;

public interface ITransferTargetStore
{
    IReadOnlyList<TransferTarget> Targets { get; }

    TransferTarget? Get(TransferTargetId targetId);

    IReadOnlyList<TransferTarget> GetForClub(ClubId clubId);

    void Upsert(TransferTarget target);

    void ReplaceAll(IEnumerable<TransferTarget> targets);
}
