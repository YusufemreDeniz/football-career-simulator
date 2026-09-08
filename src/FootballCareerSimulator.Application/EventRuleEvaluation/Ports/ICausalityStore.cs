using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Ports;

/// <summary>
/// Nedensellik kayıtları için port sözleşmesi (docs/20 §6).
/// </summary>
public interface ICausalityStore
{
    void Add(CausalityRecord record, Guid? correlationId = null, string? ruleId = null);

    CausalityRecord? FindByTargetEventId(Guid targetEventId);

    (Guid? CorrelationId, string? RuleId) GetDiagnostics(Guid targetEventId);

    IReadOnlyList<CausalityRecord> GetRecent(int count = 50);

    void Clear();
}
