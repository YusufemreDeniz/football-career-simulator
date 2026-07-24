using FootballCareerSimulator.Application.Discipline.Ports;
using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// Sınırlı Dialogue seçenek üretimi (PlayingTime / StartingOpportunity / Transfer / Discipline / BoardDemand).
/// </summary>
public sealed class DialogueOptionGenerationService
{
    private readonly IDecisionRequestStore _decisions;
    private readonly IPromiseStore? _promises;
    private readonly TransferNeedService? _transferNeeds;
    private readonly IDisciplinaryActionStore? _discipline;

    public DialogueOptionGenerationService(
        IDecisionRequestStore decisions,
        IPromiseStore? promises = null,
        TransferNeedService? transferNeeds = null,
        IDisciplinaryActionStore? discipline = null)
    {
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        _promises = promises;
        _transferNeeds = transferNeeds;
        _discipline = discipline;
    }

    public DialogueOptionsReadModel GetForDecision(DecisionRequestId decisionRequestId)
    {
        var request = _decisions.Get(decisionRequestId);
        if (request is null)
        {
            return new DialogueOptionsReadModel(
                decisionRequestId.Value,
                DialogueTypeName: "Unknown",
                DecisionIsOpen: false,
                Array.Empty<DialogueOptionReadModel>());
        }

        if (!request.IsOpen)
        {
            return new DialogueOptionsReadModel(
                request.DecisionRequestId.Value,
                DialogueTypeName(request.Kind),
                DecisionIsOpen: false,
                Array.Empty<DialogueOptionReadModel>());
        }

        var options = request.Kind switch
        {
            DecisionRequestKind.PlayingTimeRequest => BuildPlayingTimeOptions(request),
            DecisionRequestKind.StartingOpportunityRequest => BuildStartingOpportunityOptions(request),
            DecisionRequestKind.TransferRequest => BuildTransferOptions(request),
            DecisionRequestKind.DisciplineRequest => BuildDisciplineOptions(request),
            DecisionRequestKind.BoardDemandRequest => BuildBoardDemandOptions(),
            _ => Array.Empty<DialogueOptionReadModel>(),
        };

        return new DialogueOptionsReadModel(
            request.DecisionRequestId.Value,
            DialogueTypeName(request.Kind),
            DecisionIsOpen: true,
            options);
    }

    public void EnsureEligible(DecisionRequestId decisionRequestId, string optionCode)
    {
        if (string.IsNullOrWhiteSpace(optionCode))
        {
            throw new InteractionInvariantViolationException("Option code is required.");
        }

        var trimmed = optionCode.Trim();
        var snapshot = GetForDecision(decisionRequestId);
        if (!snapshot.DecisionIsOpen)
        {
            throw new InteractionInvariantViolationException(
                $"Decision request #{decisionRequestId.Value} is not open for dialogue options.");
        }

        var option = snapshot.Options.FirstOrDefault(o =>
            string.Equals(o.OptionCode, trimmed, StringComparison.Ordinal));
        if (option is null)
        {
            throw new InteractionInvariantViolationException(
                $"Option '{trimmed}' is not part of the generated dialogue set.");
        }

        if (!option.IsEligible)
        {
            throw new InteractionInvariantViolationException(
                option.IneligibilityReason
                ?? $"Option '{trimmed}' is not eligible.");
        }
    }

    private IReadOnlyList<DialogueOptionReadModel> BuildPlayingTimeOptions(DecisionRequest request)
    {
        var grantBlockedReason = FindActivePromiseBlockReason(request, PromiseKind.PlayingTime, "forma süresi");
        return
        [
            new DialogueOptionReadModel(
                DecisionRequest.OptionGrantPlayingTimePromise,
                SemanticIntentName: "GrantPlayingTimePromise",
                DisplayText: "Forma süresi sözü ver",
                ToneCode: "Supportive",
                RiskHint: "Aktif PlayingTime Promise oluşur; takip edilir.",
                IsEligible: grantBlockedReason is null,
                IneligibilityReason: grantBlockedReason),
            RefuseOption("RefusePlayingTimeRequest"),
        ];
    }

    private IReadOnlyList<DialogueOptionReadModel> BuildStartingOpportunityOptions(DecisionRequest request)
    {
        var grantBlockedReason = FindActivePromiseBlockReason(
            request,
            PromiseKind.StartingOpportunity,
            "ilk 11 fırsatı");
        return
        [
            new DialogueOptionReadModel(
                DecisionRequest.OptionGrantStartingOpportunityPromise,
                SemanticIntentName: "GrantStartingOpportunityPromise",
                DisplayText: "İlk 11 sözü ver",
                ToneCode: "Supportive",
                RiskHint: "Aktif StartingOpportunity Promise oluşur; takip edilir.",
                IsEligible: grantBlockedReason is null,
                IneligibilityReason: grantBlockedReason),
            RefuseOption("RefuseStartingOpportunityRequest"),
        ];
    }

    private IReadOnlyList<DialogueOptionReadModel> BuildTransferOptions(DecisionRequest request)
    {
        string? acknowledgeBlocked = null;
        if (_transferNeeds is not null
            && _transferNeeds.HasOpenPlayerExitRequest(request.ClubId, request.SubjectPlayerId))
        {
            acknowledgeBlocked = "Oyuncunun bu kulüpte zaten açık ayrılma/transfer ihtiyacı var.";
        }

        return
        [
            new DialogueOptionReadModel(
                DecisionRequest.OptionAcknowledgeTransferRequest,
                SemanticIntentName: "AcknowledgeTransferRequest",
                DisplayText: "Transfer isteğini kabul et",
                ToneCode: "Pragmatic",
                RiskHint: "Giden yön TransferNeed (PlayerExitRequest) oluşur.",
                IsEligible: acknowledgeBlocked is null,
                IneligibilityReason: acknowledgeBlocked),
            RefuseOption("RefuseTransferRequest"),
        ];
    }

    private IReadOnlyList<DialogueOptionReadModel> BuildDisciplineOptions(DecisionRequest request)
    {
        var fineBlocked = _discipline is not null
            && !_discipline.HasWarningForPlayerAtClub(request.SubjectPlayerId.Value, request.ClubId.Value)
            ? "Ceza için bu kulüpte önce uyarı kaydı gerekir."
            : null;

        return
        [
            new DialogueOptionReadModel(
                DecisionRequest.OptionIssueWarning,
                SemanticIntentName: "IssueWarning",
                DisplayText: "Uyarı ver",
                ToneCode: "Firm",
                RiskHint: "Disciplinary Warning kaydı; Trust↓ Respect↑.",
                IsEligible: true,
                IneligibilityReason: null),
            new DialogueOptionReadModel(
                DecisionRequest.OptionIssueFine,
                SemanticIntentName: "IssueFine",
                DisplayText: "Ceza uygula",
                ToneCode: "Strict",
                RiskHint: "Disciplinary Fine kaydı; Trust↓ Respect↑.",
                IsEligible: fineBlocked is null,
                IneligibilityReason: fineBlocked),
            new DialogueOptionReadModel(
                DecisionRequest.OptionOfferSupport,
                SemanticIntentName: "OfferSupport",
                DisplayText: "Destekle",
                ToneCode: "Supportive",
                RiskHint: "Disciplinary Support kaydı; Trust↑ Respect↓.",
                IsEligible: true,
                IneligibilityReason: null),
        ];
    }

    private static IReadOnlyList<DialogueOptionReadModel> BuildBoardDemandOptions() =>
    [
        new DialogueOptionReadModel(
            DecisionRequest.OptionAcceptBoardDemand,
            SemanticIntentName: "AcceptBoardDemand",
            DisplayText: "Yönetim talebini kabul et",
            ToneCode: "Compliant",
            RiskHint: "Board Confidence yükselir.",
            IsEligible: true,
            IneligibilityReason: null),
        new DialogueOptionReadModel(
            DecisionRequest.OptionCounterBoardDemand,
            SemanticIntentName: "CounterBoardDemand",
            DisplayText: "Karşı teklif sun",
            ToneCode: "Pragmatic",
            RiskHint: "Board Confidence hafif düşer.",
            IsEligible: true,
            IneligibilityReason: null),
        new DialogueOptionReadModel(
            DecisionRequest.OptionRefuse,
            SemanticIntentName: "RefuseBoardDemand",
            DisplayText: "Yönetim talebini reddet",
            ToneCode: "Firm",
            RiskHint: "Board Confidence belirgin düşer.",
            IsEligible: true,
            IneligibilityReason: null),
    ];

    private static DialogueOptionReadModel RefuseOption(string semanticIntentName) =>
        new(
            DecisionRequest.OptionRefuse,
            SemanticIntentName: semanticIntentName,
            DisplayText: "Talebi reddet",
            ToneCode: "Firm",
            RiskHint: "Trust düşebilir; Promise oluşmaz.",
            IsEligible: true,
            IneligibilityReason: null);

    private string? FindActivePromiseBlockReason(
        DecisionRequest request,
        PromiseKind kind,
        string label)
    {
        if (_promises is null)
        {
            return null;
        }

        var hasActive = _promises.Promises.Any(p =>
            p.IsActive
            && p.Kind == kind
            && p.Promisee.Kind == ActorKind.Player
            && p.Promisee.Id == request.SubjectPlayerId.Value
            && p.ClubId == request.ClubId);

        return hasActive
            ? $"Oyuncunun bu kulüpte zaten aktif {label} sözü var."
            : null;
    }

    private static string DialogueTypeName(DecisionRequestKind kind) =>
        kind switch
        {
            DecisionRequestKind.PlayingTimeRequest => "PlayingTimeRequest",
            DecisionRequestKind.StartingOpportunityRequest => "StartingOpportunityRequest",
            DecisionRequestKind.TransferRequest => "TransferRequest",
            DecisionRequestKind.DisciplineRequest => "DisciplineRequest",
            DecisionRequestKind.BoardDemandRequest => "BoardDemandRequest",
            _ => kind.ToString(),
        };
}
