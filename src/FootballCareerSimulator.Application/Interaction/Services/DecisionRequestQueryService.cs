using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Application.Interaction.Services;

public sealed class DecisionRequestQueryService
{
    private readonly IDecisionRequestStore _store;

    public DecisionRequestQueryService(IDecisionRequestStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public PendingDecisionsReadModel GetPending(int take = 8)
    {
        if (take < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be positive.");
        }

        var open = _store.Requests
            .Where(r => r.Status == DecisionRequestStatus.Open)
            .OrderBy(r => r.DeadlineOn.DayNumber)
            .ThenBy(r => r.DecisionRequestId.Value)
            .ToArray();

        return new PendingDecisionsReadModel(
            open.Length,
            open.Take(take).Select(ToLine).ToArray());
    }

    private static DecisionRequestLineReadModel ToLine(DecisionRequest request) =>
        new(
            request.DecisionRequestId.Value,
            request.Kind switch
            {
                DecisionRequestKind.PlayingTimeRequest => "Forma süresi talebi",
                DecisionRequestKind.StartingOpportunityRequest => "İlk 11 fırsatı talebi",
                DecisionRequestKind.TransferRequest => "Transfer isteği",
                _ => request.Kind.ToString(),
            },
            request.SubjectPlayerId.Value,
            request.ClubId.Value,
            request.Status.ToString(),
            request.IsHardBlocker,
            request.OpenedOn.DayNumber,
            request.DeadlineOn.DayNumber,
            request.SelectedOptionCode);
}
