using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Infrastructure;

public sealed class InMemoryTransferTargetStore : ITransferTargetStore
{
    private readonly Dictionary<long, TransferTarget> _byId = new();

    public IReadOnlyList<TransferTarget> Targets =>
        _byId.Values.OrderBy(t => t.TargetId.Value).ToArray();

    public TransferTarget? Get(TransferTargetId targetId) =>
        _byId.TryGetValue(targetId.Value, out var target) ? target : null;

    public IReadOnlyList<TransferTarget> GetForClub(ClubId clubId) =>
        _byId.Values
            .Where(t => t.ClubId.Value == clubId.Value)
            .OrderBy(t => t.TargetId.Value)
            .ToArray();

    public void Upsert(TransferTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _byId[target.TargetId.Value] = target;
    }

    public void ReplaceAll(IEnumerable<TransferTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        _byId.Clear();
        foreach (var target in targets)
        {
            _byId[target.TargetId.Value] = target;
        }
    }
}
