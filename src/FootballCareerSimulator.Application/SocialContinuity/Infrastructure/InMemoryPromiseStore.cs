using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Infrastructure;

public sealed class InMemoryPromiseStore : IPromiseStore
{
    private readonly Dictionary<long, Promise> _byId = new();

    public IReadOnlyList<Promise> Promises =>
        _byId.Values.OrderBy(p => p.PromiseId.Value).ToArray();

    public Promise? Get(PromiseId promiseId) =>
        _byId.TryGetValue(promiseId.Value, out var promise) ? promise : null;

    public void Upsert(Promise promise)
    {
        ArgumentNullException.ThrowIfNull(promise);
        _byId[promise.PromiseId.Value] = promise;
    }

    public void ReplaceAll(IEnumerable<Promise> promises)
    {
        ArgumentNullException.ThrowIfNull(promises);
        _byId.Clear();
        foreach (var promise in promises)
        {
            _byId[promise.PromiseId.Value] = promise;
        }
    }
}
