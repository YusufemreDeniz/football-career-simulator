using FootballCareerSimulator.Application.Interaction.Ports;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// Sınırlı Dialogue seçenek üretimi (ilk dilim: forma süresi talebi).
/// UI seçenek icat etmez; eligibility Application kurallarıyla belirlenir (D-114/D-126).
/// </summary>
public sealed class DialogueOptionGenerationService
{
    private readonly IDecisionRequestStore _decisions;
    private readonly IPromiseStore? _promises;

    public DialogueOptionGenerationService(
        IDecisionRequestStore decisions,
        IPromiseStore? promises = null)
    {
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        _promises = promises;
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
        var grantBlockedReason = FindGrantBlockReason(request);
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
            new DialogueOptionReadModel(
                DecisionRequest.OptionRefuse,
                SemanticIntentName: "RefusePlayingTimeRequest",
                DisplayText: "Talebi reddet",
                ToneCode: "Firm",
                RiskHint: "Trust düşebilir; Promise oluşmaz.",
                IsEligible: true,
                IneligibilityReason: null),
        ];
    }

    private string? FindGrantBlockReason(DecisionRequest request)
    {
        if (_promises is null)
        {
            return null;
        }

        var hasActive = _promises.Promises.Any(p =>
            p.IsActive
            && p.Kind == PromiseKind.PlayingTime
            && p.Promisee.Kind == ActorKind.Player
            && p.Promisee.Id == request.SubjectPlayerId.Value
            && p.ClubId == request.ClubId);

        return hasActive
            ? "Oyuncunun bu kulüpte zaten aktif forma süresi sözü var."
            : null;
    }

    private static string DialogueTypeName(DecisionRequestKind kind) =>
        kind switch
        {
            DecisionRequestKind.PlayingTimeRequest => "PlayingTimeRequest",
            _ => kind.ToString(),
        };
}
