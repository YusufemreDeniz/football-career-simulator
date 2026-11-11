namespace FootballCareerSimulator.Application.Interaction.Queries;

/// <summary>
/// Dialogue Option sunum modeli: OptionCode = semantic intent (D-114).
/// Metin/ton domain sonucunu değiştirmez.
/// </summary>
public sealed record DialogueOptionReadModel(
    string OptionCode,
    string SemanticIntentName,
    string DisplayText,
    string ToneCode,
    string RiskHint,
    bool IsEligible,
    string? IneligibilityReason);

public sealed record DialogueOptionsReadModel(
    long DecisionRequestId,
    string DialogueTypeName,
    bool DecisionIsOpen,
    IReadOnlyList<DialogueOptionReadModel> Options);
