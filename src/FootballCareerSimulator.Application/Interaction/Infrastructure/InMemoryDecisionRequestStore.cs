using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Application.Interaction.Infrastructure;

public sealed class InMemoryDecisionRequestStore : IDecisionRequestStore
{
    private readonly Dictionary<long, DecisionRequest> _requests = new();

    public IReadOnlyList<DecisionRequest> Requests =>
        _requests.Values.OrderBy(r => r.DecisionRequestId.Value).ToArray();

    public DecisionRequest? Get(DecisionRequestId id) =>
        _requests.TryGetValue(id.Value, out var request) ? request : null;

    public void Upsert(DecisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _requests[request.DecisionRequestId.Value] = request;
    }

    public void ReplaceAll(IEnumerable<DecisionRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        _requests.Clear();
        foreach (var request in requests)
        {
            Upsert(request);
        }
    }
}
