using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.Interaction.Services;

public sealed class DecisionRequestQueryService
{
    private readonly IDecisionRequestStore _store;
    private readonly IRelationshipStore? _relationships;
    private readonly IPromiseStore? _promises;
    private readonly IMemoryStore? _memories;

    public DecisionRequestQueryService(
        IDecisionRequestStore store,
        IRelationshipStore? relationships = null,
        IPromiseStore? promises = null,
        IMemoryStore? memories = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _relationships = relationships;
        _promises = promises;
        _memories = memories;
    }

    public PendingDecisionsReadModel GetPending(int take = 8)
    {
        if (take < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be positive.");
        }

        // Hard blocker önce; Low Trust + Transfer (söz kırılması eskalasyonu) sonra; sonra son tarih.
        var open = _store.Requests
            .Where(r => r.Status == DecisionRequestStatus.Open)
            .OrderByDescending(r => r.IsHardBlocker)
            .ThenByDescending(IsPromiseExitPressure)
            .ThenBy(r => r.DeadlineOn.DayNumber)
            .ThenBy(r => r.DecisionRequestId.Value)
            .ToArray();

        return new PendingDecisionsReadModel(
            open.Length,
            open.Take(take).Select(ToLine).ToArray());
    }

    public string? ExplainCausality(DecisionRequestId decisionRequestId)
    {
        var request = _store.Get(decisionRequestId);
        if (request is null || !request.IsOpen)
        {
            return null;
        }

        return BuildCausality(request);
    }

    private bool IsPromiseExitPressure(DecisionRequest request)
    {
        if (request.Kind != DecisionRequestKind.TransferRequest || _relationships is null)
        {
            return false;
        }

        var relationship = _relationships.FindPlayerToManager(
            request.SubjectPlayerId.Value,
            request.ManagerId.Value);
        return relationship is { Status: RelationshipStatus.Active }
               && RelationshipDimensionBands.FromValue(relationship.Trust) == RelationshipDimensionBand.Low;
    }

    private string? BuildCausality(DecisionRequest request)
    {
        var playerId = request.SubjectPlayerId.Value;
        var managerId = request.ManagerId.Value;

        var broken = _promises?.Promises
            .Where(p =>
                p.Status == PromiseStatus.Broken
                && p.Promisee.Kind == ActorKind.Player
                && p.Promisee.Id == playerId
                && p.Promisor.Kind == ActorKind.Manager
                && p.Promisor.Id == managerId)
            .OrderByDescending(p => p.TerminalOn?.DayNumber ?? 0)
            .ThenByDescending(p => p.PromiseId.Value)
            .FirstOrDefault();

        var relationship = _relationships?.FindPlayerToManager(playerId, managerId);
        var trustLow = relationship is { Status: RelationshipStatus.Active }
                       && RelationshipDimensionBands.FromValue(relationship.Trust)
                           == RelationshipDimensionBand.Low;

        var memory = _memories?.Memories
            .Where(m =>
                m.Status == MemoryStatus.Active
                && m.RememberingActor.Kind == ActorKind.Player
                && m.RememberingActor.Id == playerId
                && m.Valence == MemoryValence.Negative
                && m.Category is MemoryCategory.Promise or MemoryCategory.Trust)
            .OrderByDescending(m => m.CurrentInfluence)
            .ThenByDescending(m => m.MemoryId.Value)
            .FirstOrDefault();

        return request.Kind switch
        {
            DecisionRequestKind.TransferRequest when trustLow && broken is not null =>
                $"Söz #{broken.PromiseId.Value} bozuldu · güven düşük"
                + (memory is null ? string.Empty : $" · hafıza etki {memory.CurrentInfluence}"),
            DecisionRequestKind.TransferRequest when trustLow =>
                "Güven düşük — oyuncu ayrılmak istiyor",
            DecisionRequestKind.PlayingTimeRequest when broken is not null =>
                $"Önceki forma sözü bozuldu (#{broken.PromiseId.Value})"
                + (memory is null ? string.Empty : $" · hatırlanıyor (etki {memory.CurrentInfluence})"),
            DecisionRequestKind.StartingOpportunityRequest when broken is not null =>
                $"Önceki İlk 11 sözü bozuldu (#{broken.PromiseId.Value})"
                + (memory is null ? string.Empty : $" · hatırlanıyor (etki {memory.CurrentInfluence})"),
            _ when memory is not null && memory.CurrentInfluence >= 40 =>
                $"Olumsuz hafıza etki {memory.CurrentInfluence} (oyuncu#{playerId})",
            _ => null,
        };
    }

    private static DecisionRequestLineReadModel ToLine(DecisionRequest request) =>
        new(
            request.DecisionRequestId.Value,
            request.Kind switch
            {
                DecisionRequestKind.PlayingTimeRequest => "Forma süresi talebi",
                DecisionRequestKind.StartingOpportunityRequest => "İlk 11 fırsatı talebi",
                DecisionRequestKind.TransferRequest => "Transfer isteği",
                DecisionRequestKind.DisciplineRequest => "Disiplin görüşmesi",
                DecisionRequestKind.BoardDemandRequest => "Yönetim talebi",
                DecisionRequestKind.PressQuestionRequest => "Kritik basın sorusu",
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
