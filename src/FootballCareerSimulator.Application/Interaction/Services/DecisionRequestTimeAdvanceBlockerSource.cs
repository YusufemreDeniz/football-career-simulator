using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Interaction;

namespace FootballCareerSimulator.Application.Interaction.Services;

public sealed class DecisionRequestTimeAdvanceBlockerSource : ITimeAdvanceBlockerSource
{
    public const string BlockerTypeCode = "PendingDecisionRequest";

    private readonly IDecisionRequestStore _store;

    public DecisionRequestTimeAdvanceBlockerSource(IDecisionRequestStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public string SourceContext => "Interaction";

    public IReadOnlyList<TimeAdvanceBlockerDescriptor> GetActiveBlockers()
    {
        var pending = _store.Requests.Count(r =>
            r.Status == DecisionRequestStatus.Open && r.IsHardBlocker);
        if (pending == 0)
        {
            return Array.Empty<TimeAdvanceBlockerDescriptor>();
        }

        return
        [
            new TimeAdvanceBlockerDescriptor(
                BlockerTypeCode,
                $"Bekleyen {pending} zorunlu karar var; önce kararları yanıtlayın.",
                IsHardBlocker: true),
        ];
    }
}
