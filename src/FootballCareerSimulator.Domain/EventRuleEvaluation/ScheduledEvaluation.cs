namespace FootballCareerSimulator.Domain.EventRuleEvaluation;

/// <summary>
/// Gelecek değerlendirme kaydı (docs/04 §14) — Domain Event değildir; business deadline'ın kopyası değildir.
/// </summary>
public sealed class ScheduledEvaluation
{
    private ScheduledEvaluation(
        ScheduledEvaluationId id,
        string evaluationTypeCode,
        int dueDayNumber,
        Guid? sourceEventId,
        ScheduledEvaluationStatus status)
    {
        Id = id;
        EvaluationTypeCode = evaluationTypeCode;
        DueDayNumber = dueDayNumber;
        SourceEventId = sourceEventId;
        Status = status;
    }

    public ScheduledEvaluationId Id { get; }

    public string EvaluationTypeCode { get; }

    public int DueDayNumber { get; }

    public Guid? SourceEventId { get; }

    public ScheduledEvaluationStatus Status { get; private set; }

    public static ScheduledEvaluation CreatePending(
        ScheduledEvaluationId id,
        string evaluationTypeCode,
        int dueDayNumber,
        Guid? sourceEventId)
    {
        if (string.IsNullOrWhiteSpace(evaluationTypeCode))
        {
            throw new ArgumentException("Evaluation type code is required.", nameof(evaluationTypeCode));
        }

        return new ScheduledEvaluation(
            id,
            evaluationTypeCode,
            dueDayNumber,
            sourceEventId,
            ScheduledEvaluationStatus.Pending);
    }

    public static ScheduledEvaluation Rehydrate(
        ScheduledEvaluationId id,
        string evaluationTypeCode,
        int dueDayNumber,
        Guid? sourceEventId,
        ScheduledEvaluationStatus status)
    {
        if (string.IsNullOrWhiteSpace(evaluationTypeCode))
        {
            throw new ArgumentException("Evaluation type code is required.", nameof(evaluationTypeCode));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown scheduled evaluation status.");
        }

        return new ScheduledEvaluation(id, evaluationTypeCode, dueDayNumber, sourceEventId, status);
    }

    public void MarkCompleted()
    {
        if (Status != ScheduledEvaluationStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Scheduled evaluation {Id.Value} cannot complete from {Status}.");
        }

        Status = ScheduledEvaluationStatus.Completed;
    }
}
