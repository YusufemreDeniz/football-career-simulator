using FootballCareerSimulator.Application.EventRuleEvaluation.Queries;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.Interaction.Services;

public sealed class DecisionRequestQueryService
{
    private readonly IDecisionRequestStore _store;
    private readonly IRelationshipStore? _relationships;
    private readonly IPromiseStore? _promises;
    private readonly IMemoryStore? _memories;
    private readonly IManagerCareerStore? _managerCareer;
    private readonly CausalityJournalQueryService? _causalityJournal;

    public DecisionRequestQueryService(
        IDecisionRequestStore store,
        IRelationshipStore? relationships = null,
        IPromiseStore? promises = null,
        IMemoryStore? memories = null,
        IManagerCareerStore? managerCareer = null,
        CausalityJournalQueryService? causalityJournal = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _relationships = relationships;
        _promises = promises;
        _memories = memories;
        _managerCareer = managerCareer;
        _causalityJournal = causalityJournal;
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

    /// <summary>
    /// Karar talebinin nedensellik kaydı için deterministik kimlik (Guid.NewGuid yerine; kayıt/yeniden oynatma güvenli).
    /// </summary>
    public static Guid CausalityEventIdFor(DecisionRequestId decisionRequestId)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes[..8], decisionRequestId.Value);
        // Sabit ad alanı imzası: "DRQ-CAUS" (Decision Request Causality).
        "DRQ-CAUS"u8.CopyTo(bytes[8..]);
        return new Guid(bytes);
    }

    public CausalityExplanation? ExplainStructuredCausality(DecisionRequestId decisionRequestId)
    {
        var request = _store.Get(decisionRequestId);
        if (request is null || !request.IsOpen)
        {
            return null;
        }

        var playerId = request.SubjectPlayerId.Value;
        var managerId = request.ManagerId.Value;
        var factors = new List<CausalFactor>();

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

        if (broken is not null)
        {
            factors.Add(new CausalFactor(
                "PromiseBroken",
                CausalFactorWeight.Primary,
                $"Verilen forma sözünün tutulmaması (#{broken.PromiseId.Value})",
                new ActorRef(ActorKind.Player, playerId),
                -15,
                broken.TerminalOn?.DayNumber ?? request.OpenedOn.DayNumber));
        }

        var relationship = _relationships?.FindPlayerToManager(playerId, managerId);
        if (relationship is { Status: RelationshipStatus.Active }
            && RelationshipDimensionBands.FromValue(relationship.Trust) == RelationshipDimensionBand.Low)
        {
            factors.Add(new CausalFactor(
                "LowTrust",
                broken is null ? CausalFactorWeight.Primary : CausalFactorWeight.Contributing,
                "Menajere duyulan genel güvenin düşük seviyede olması",
                new ActorRef(ActorKind.Player, playerId),
                -10,
                relationship.LastChangedOn.DayNumber));
        }

        var sittingOut = _memories is null
            ? 0
            : PostMatchPlayingTimeDemandTrigger.CountSittingOutEvents(
                _memories.Memories,
                request.SubjectPlayerId);

        if (sittingOut >= PostMatchPlayingTimeDemandTrigger.SittingOutDemandThreshold)
        {
            factors.Add(new CausalFactor(
                "SittingOutAccumulation",
                broken is null && factors.Count == 0 ? CausalFactorWeight.Primary : CausalFactorWeight.Contributing,
                $"Son {sittingOut} maçta yedek veya kadro dışı kalınması",
                new ActorRef(ActorKind.Player, playerId),
                -5,
                request.OpenedOn.DayNumber));
        }

        if (factors.Count == 0)
        {
            var fallback = BuildCausality(request);
            if (string.IsNullOrWhiteSpace(fallback))
            {
                return null;
            }

            factors.Add(new CausalFactor(
                "GeneralContext",
                CausalFactorWeight.Primary,
                fallback,
                new ActorRef(ActorKind.Player, playerId),
                null,
                request.OpenedOn.DayNumber));
        }

        var dimension = request.Kind switch
        {
            DecisionRequestKind.TransferRequest => CausalityDimension.TransferResult,
            DecisionRequestKind.PlayingTimeRequest => CausalityDimension.PromiseOutcome,
            DecisionRequestKind.StartingOpportunityRequest => CausalityDimension.PromiseOutcome,
            DecisionRequestKind.BoardDemandRequest => CausalityDimension.BoardConfidenceDelta,
            _ => CausalityDimension.RelationshipDelta,
        };

        // Kimlik karar talebinden deterministik türetilir; aynı talep için tekrar sorgu yeni kayıt üretmez.
        var targetEventId = CausalityEventIdFor(request.DecisionRequestId);
        var existing = _causalityJournal?.ExplainForPlayer(targetEventId);
        if (existing is not null)
        {
            return existing;
        }

        var record = new CausalityRecord(
            targetEventId,
            dimension,
            $"DecisionRequest.{request.Kind}",
            request.OpenedOn.DayNumber,
            factors);

        _causalityJournal?.RecordCausality(record, null, $"Rule.Decision.{request.Kind}");

        return _causalityJournal is not null
            ? _causalityJournal.ExplainForPlayer(targetEventId)
            : new CausalityExplanation(
                targetEventId,
                dimension,
                "Karar Nedenselliği",
                factors[0].SummaryKey,
                factors.Skip(1).Select(f => f.SummaryKey).ToArray(),
                Array.Empty<string>(),
                null,
                "Yüksek",
                new[] { $"Player #{playerId}" },
                request.OpenedOn.DayNumber);
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

        var sittingOut = _memories is null
            ? 0
            : PostMatchPlayingTimeDemandTrigger.CountSittingOutEvents(
                _memories.Memories,
                request.SubjectPlayerId);

        var boardReason = _managerCareer?.Career.ActiveEmployment?.LastAssessmentReasonCode;

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
            DecisionRequestKind.PlayingTimeRequest when sittingOut
                >= PostMatchPlayingTimeDemandTrigger.SittingOutDemandThreshold =>
                $"Son {sittingOut} maçta yedek/kadro dışı — forma istiyor",
            DecisionRequestKind.StartingOpportunityRequest when broken is not null =>
                $"Önceki İlk 11 sözü bozuldu (#{broken.PromiseId.Value})"
                + (memory is null ? string.Empty : $" · hatırlanıyor (etki {memory.CurrentInfluence})"),
            DecisionRequestKind.BoardDemandRequest when boardReason is not null =>
                ExplainBoardDemandReason(boardReason),
            DecisionRequestKind.DisciplineRequest =>
                "Kırmızı kart gördü — soyunma odasında konuşma şart",
            _ when memory is not null && memory.CurrentInfluence >= 40 =>
                $"Olumsuz hafıza etki {memory.CurrentInfluence}",
            _ => null,
        };
    }

    /// <summary>
    /// Mevcut board assessment reason code'larını oyuncu yüzüne çevirir; yeni formül yoktur.
    /// </summary>
    private static string ExplainBoardDemandReason(string reasonCode) =>
        reasonCode switch
        {
            "LossBehindExpectation" =>
                "Beklentinin altında mağlubiyet — yönetim masaya oturdu",
            "LossOnTrack" =>
                "Mağlubiyet — yönetim masaya oturdu",
            "DrawBehindExpectation" =>
                "Beklentinin altında beraberlik — yönetim masaya oturdu",
            _ => "Yönetim güveni incelemede — masaya oturdular",
        };

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
