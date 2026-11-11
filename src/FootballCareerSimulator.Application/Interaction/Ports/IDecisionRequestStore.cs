using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Application.Interaction.Ports;

public interface IDecisionRequestStore
{
    IReadOnlyList<DecisionRequest> Requests { get; }

    DecisionRequest? Get(DecisionRequestId id);

    void Upsert(DecisionRequest request);

    void ReplaceAll(IEnumerable<DecisionRequest> requests);
}
