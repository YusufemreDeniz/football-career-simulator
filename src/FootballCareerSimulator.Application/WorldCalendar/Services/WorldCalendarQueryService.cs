namespace FootballCareerSimulator.Application.WorldCalendar.Services;

using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Queries;
using FootballCareerSimulator.Domain.WorldCalendar;

public sealed class WorldCalendarQueryService
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly TimeAdvanceBlockerAggregator _blockerAggregator;

    public WorldCalendarQueryService(
        IWorldTimelineStore timelineStore,
        TimeAdvanceBlockerAggregator blockerAggregator)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _blockerAggregator = blockerAggregator ?? throw new ArgumentNullException(nameof(blockerAggregator));
    }

    public CurrentGameDateReadModel GetCurrentGameDate()
    {
        var current = _timelineStore.Timeline.CurrentDate;
        return new CurrentGameDateReadModel(
            current.DayNumber,
            current.ToIsoDateString(),
            current.Year,
            current.Month,
            current.Day);
    }

    public CurrentPlanningPeriodReadModel? GetCurrentPlanningPeriod()
    {
        var period = _timelineStore.Timeline.ActivePlanningPeriod;
        if (period is null || !period.IsActive)
        {
            return null;
        }

        return new CurrentPlanningPeriodReadModel(
            period.Id.Value,
            period.Status.ToString(),
            period.StartDate.DayNumber,
            period.StartDate.ToIsoDateString(),
            period.ExpectedEndDate?.DayNumber,
            period.ExpectedEndDate?.ToIsoDateString());
    }

    public TimeAdvanceEligibilityReadModel GetTimeAdvanceEligibility()
    {
        var blockers = _blockerAggregator
            .GetActiveBlockers()
            .Select(blocker => new TimeAdvanceBlockerReadModel(
                blocker.SourceContext,
                blocker.BlockerTypeCode,
                blocker.DescriptionCode,
                blocker.IsHardBlocker))
            .ToArray();

        return new TimeAdvanceEligibilityReadModel(
            CanAdvance: blockers.Length == 0,
            CurrentDayNumber: _timelineStore.Timeline.CurrentDate.DayNumber,
            Blockers: blockers);
    }

    public TransferWindowReadModel GetTransferWindow()
    {
        var window = _timelineStore.Timeline.TransferWindow;
        return new TransferWindowReadModel(
            window.IsOpen,
            window.IsOpen ? "Açık" : "Kapalı",
            window.OpenedOn?.DayNumber,
            window.ClosesOn?.DayNumber);
    }
}
