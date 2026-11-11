using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;

namespace FootballCareerSimulator.Application.Transfer.Infrastructure;

public sealed class InMemoryTransferNeedStore : ITransferNeedStore
{
    private readonly Dictionary<long, TransferNeed> _byId = new();

    public IReadOnlyList<TransferNeed> Needs =>
        _byId.Values.OrderBy(n => n.NeedId.Value).ToArray();

    public TransferNeed? Get(TransferNeedId needId) =>
        _byId.TryGetValue(needId.Value, out var need) ? need : null;

    public IReadOnlyList<TransferNeed> GetForClub(ClubId clubId) =>
        _byId.Values
            .Where(n => n.ClubId.Value == clubId.Value)
            .OrderByDescending(n => n.Priority)
            .ThenBy(n => n.NeedId.Value)
            .ToArray();

    public void Upsert(TransferNeed need)
    {
        ArgumentNullException.ThrowIfNull(need);
        _byId[need.NeedId.Value] = need;
    }

    public void ReplaceAll(IEnumerable<TransferNeed> needs)
    {
        ArgumentNullException.ThrowIfNull(needs);
        _byId.Clear();
        foreach (var need in needs)
        {
            _byId[need.NeedId.Value] = need;
        }
    }
}
