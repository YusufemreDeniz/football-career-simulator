using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// World &amp; Calendar bounded context'inin authoritative zaman aggregate'i
/// (docs/03_DOMAIN_MODEL.md Bölüm 7.1, docs/19_PRODUCTION_IMPLEMENTATION_PLAN.md Kart 2).
/// </summary>
public sealed class WorldTimeline
{
    private readonly List<WorldCalendarDomainEvent> _uncommittedEvents = new();

    public GameDate CurrentDate { get; private set; }

    public SimulationStepId LastCommittedStepId { get; private set; }

    public PlanningPeriod? ActivePlanningPeriod { get; private set; }

    public int RootSeed { get; }

    public string RngVersion { get; }

    public int RngDrawCount { get; private set; }

    public IReadOnlyList<WorldCalendarDomainEvent> UncommittedEvents => _uncommittedEvents;

    private WorldTimeline(GameDate startingDate, SimulationStepId lastCommittedStepId, int rootSeed, string rngVersion)
    {
        CurrentDate = startingDate;
        LastCommittedStepId = lastCommittedStepId;
        RootSeed = rootSeed;
        RngVersion = rngVersion;
    }

    public static WorldTimeline Create(GameDate startingDate, int rootSeed = 0, string rngVersion = "1") =>
        new(startingDate, SimulationStepId.Zero, rootSeed, rngVersion);

    public void RecordRngDraw() => RngDrawCount++;

    public WorldTimelineAdvancementResult AdvanceOneDay() => AdvanceTo(CurrentDate.NextDay());

    public WorldTimelineAdvancementResult AdvanceTo(GameDate targetDate)
    {
        if (!targetDate.IsAfter(CurrentDate))
        {
            throw new WorldCalendarInvariantViolationException(
                "Game date cannot move backwards or remain unchanged during advancement.");
        }

        var originalDate = CurrentDate;
        var previousDate = CurrentDate;
        var raisedEvents = new List<WorldCalendarDomainEvent>();
        var firstStepId = LastCommittedStepId.Next();
        var stepId = firstStepId;

        while (CurrentDate.IsBefore(targetDate))
        {
            var dayBeingProcessed = CurrentDate.NextDay();
            raisedEvents.Add(new GameDayStarted(stepId, dayBeingProcessed));

            CurrentDate = dayBeingProcessed;
            LastCommittedStepId = stepId;

            raisedEvents.Add(new GameDayCompleted(stepId, dayBeingProcessed));
            raisedEvents.Add(new GameTimeAdvanced(stepId, dayBeingProcessed, previousDate));

            previousDate = dayBeingProcessed;
            stepId = stepId.Next();
        }

        _uncommittedEvents.AddRange(raisedEvents);

        return new WorldTimelineAdvancementResult(
            PreviousDate: originalDate,
            NewDate: CurrentDate,
            FirstCommittedStepId: firstStepId,
            LastCommittedStepId: LastCommittedStepId,
            RaisedEvents: raisedEvents);
    }

    public PlanningPeriod OpenPlanningPeriod(
        PlanningPeriodId planningPeriodId,
        GameDate startDate,
        GameDate? expectedEndDate = null)
    {
        if (ActivePlanningPeriod is { IsActive: true })
        {
            throw new WorldCalendarInvariantViolationException(
                "A planning period is already active and must be completed before opening another.");
        }

        if (startDate.IsBefore(CurrentDate))
        {
            throw new WorldCalendarInvariantViolationException(
                "Planning period start date cannot be before the current game date.");
        }

        if (expectedEndDate is { } endDate && endDate.IsBefore(startDate))
        {
            throw new WorldCalendarInvariantViolationException(
                "Planning period expected end date cannot be before its start date.");
        }

        var stepId = LastCommittedStepId.Next();
        var period = new PlanningPeriod(
            planningPeriodId,
            startDate,
            expectedEndDate,
            PlanningPeriodStatus.Open,
            completedAt: null);

        ActivePlanningPeriod = period;
        LastCommittedStepId = stepId;

        var started = new PlanningPeriodStarted(stepId, CurrentDate, planningPeriodId, startDate);
        _uncommittedEvents.Add(started);

        return period;
    }

    public PlanningPeriod CompleteActivePlanningPeriod()
    {
        if (ActivePlanningPeriod is not { } period)
        {
            throw new WorldCalendarInvariantViolationException("No active planning period exists to complete.");
        }

        if (period.Status is PlanningPeriodStatus.Completed or PlanningPeriodStatus.Archived)
        {
            throw new WorldCalendarInvariantViolationException("Planning period has already been completed.");
        }

        var stepId = LastCommittedStepId.Next();
        var completedPeriod = period.WithStatus(PlanningPeriodStatus.Completed, CurrentDate);
        ActivePlanningPeriod = completedPeriod;
        LastCommittedStepId = stepId;

        var completed = new PlanningPeriodCompleted(stepId, CurrentDate, period.Id, CurrentDate);
        _uncommittedEvents.Add(completed);

        return completedPeriod;
    }

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
}
