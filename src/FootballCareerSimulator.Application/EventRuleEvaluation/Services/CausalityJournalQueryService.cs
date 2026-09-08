using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Application.EventRuleEvaluation.Queries;
using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Services;

/// <summary>
/// Nedensellik sorgu ve açıklama servisi (docs/20 §6 / §29).
/// Çift projeksiyon üretir: oyuncu yüzü (semantik) ve teşhis izleme (teknik).
/// </summary>
public sealed class CausalityJournalQueryService
{
    private readonly ICausalityStore _store;

    public CausalityJournalQueryService(ICausalityStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public CausalityExplanation? ExplainForPlayer(Guid targetEventId)
    {
        var record = _store.FindByTargetEventId(targetEventId);
        if (record is null)
        {
            return null;
        }

        return FormatPlayerExplanation(record);
    }

    public CausalityDiagnosticTrace? GetDiagnosticTrace(Guid targetEventId)
    {
        var record = _store.FindByTargetEventId(targetEventId);
        if (record is null)
        {
            return null;
        }

        var (correlationId, ruleId) = _store.GetDiagnostics(targetEventId);
        return new CausalityDiagnosticTrace(
            record.TargetEventId,
            record.Dimension,
            ruleId,
            correlationId,
            record.Factors.Select(f => f.FactorCode).ToArray(),
            record.EvaluatedAtDayNumber);
    }

    public void RecordCausality(
        CausalityRecord record,
        Guid? correlationId = null,
        string? ruleId = null)
    {
        _store.Add(record, correlationId, ruleId);
    }

    private static CausalityExplanation FormatPlayerExplanation(CausalityRecord record)
    {
        var title = record.Dimension switch
        {
            CausalityDimension.RelationshipDelta => "İlişki / Güven Değişimi",
            CausalityDimension.PromiseOutcome => "Söz Sonucu",
            CausalityDimension.TransferResult => "Transfer Sonucu",
            CausalityDimension.BoardConfidenceDelta => "Yönetim Güveni Değişimi",
            _ => "Olay Nedenselliği",
        };

        var primary = record.PrimaryCause?.SummaryKey
            ?? "Belirgin tek bir ana neden bulunamadı; genel durum etkili oldu.";

        var supporting = record.ContributingCauses
            .Select(c => c.SummaryKey)
            .Distinct()
            .ToArray();

        var mitigating = record.MitigatingCauses
            .Select(c => c.SummaryKey)
            .Distinct()
            .ToArray();

        var historical = record.ContextualCauses
            .Select(c => c.SummaryKey)
            .FirstOrDefault();

        var confidence = record.Factors.Count > 0 ? "Yüksek" : "Orta";

        var actors = record.Factors
            .Where(f => f.SourceActor is not null)
            .Select(f => $"{f.SourceActor!.Value.Kind} #{f.SourceActor!.Value.Id}")
            .Distinct()
            .ToArray();

        return new CausalityExplanation(
            record.TargetEventId,
            record.Dimension,
            title,
            primary,
            supporting,
            mitigating,
            historical,
            confidence,
            actors,
            record.EvaluatedAtDayNumber);
    }
}
