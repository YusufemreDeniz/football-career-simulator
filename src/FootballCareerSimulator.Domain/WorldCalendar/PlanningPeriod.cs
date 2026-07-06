namespace FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Aktif veya tamamlanmış bir planlama dönemi (docs/12_WORLD_SIMULATION.md Bölüm 10).
/// Durum geçişleri yalnızca <see cref="WorldTimeline"/> üzerinden yapılır.
/// </summary>
public sealed class PlanningPeriod
{
    public PlanningPeriodId Id { get; }
    public GameDate StartDate { get; }
    public GameDate? ExpectedEndDate { get; }
    public PlanningPeriodStatus Status { get; }
    public GameDate? CompletedAt { get; }

    internal PlanningPeriod(
        PlanningPeriodId id,
        GameDate startDate,
        GameDate? expectedEndDate,
        PlanningPeriodStatus status,
        GameDate? completedAt)
    {
        Id = id;
        StartDate = startDate;
        ExpectedEndDate = expectedEndDate;
        Status = status;
        CompletedAt = completedAt;
    }

    internal PlanningPeriod WithStatus(PlanningPeriodStatus status, GameDate? completedAt = null) =>
        new(Id, StartDate, ExpectedEndDate, status, completedAt ?? CompletedAt);

    internal bool IsActive => Status is not (PlanningPeriodStatus.Completed or PlanningPeriodStatus.Archived);
}
