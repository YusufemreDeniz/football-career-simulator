namespace FootballCareerSimulator.Application.Competition.Services;

using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;

/// <summary>
/// Aktif sezonda oynanmamış ve vadesi gelmiş maçlar varken zaman ilerletmeyi engeller.
/// </summary>
public sealed class UnplayedFixturesTimeAdvanceBlockerSource : ITimeAdvanceBlockerSource
{
    public const string BlockerTypeCode = "UnplayedFixturesDue";

    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly IWorldTimelineStore _timelineStore;

    public UnplayedFixturesTimeAdvanceBlockerSource(
        ILeagueCompetitionStore competitionStore,
        IWorldTimelineStore timelineStore)
    {
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public string SourceContext => "Competition";

    public IReadOnlyList<TimeAdvanceBlockerDescriptor> GetActiveBlockers()
    {
        var season = _competitionStore.League.CurrentSeason;
        if (season is null || season.Status is not SeasonStatus.Active)
        {
            return Array.Empty<TimeAdvanceBlockerDescriptor>();
        }

        var currentDay = _timelineStore.Timeline.CurrentDate.DayNumber;
        var dueUnplayedCount = season.Fixtures.Count(fixture =>
            fixture.Status is FixtureStatus.Planned
            && fixture.ScheduledDate.DayNumber <= currentDay);

        if (dueUnplayedCount == 0)
        {
            return Array.Empty<TimeAdvanceBlockerDescriptor>();
        }

        return
        [
            new TimeAdvanceBlockerDescriptor(
                BlockerTypeCode,
                BlockerTypeCode,
                IsHardBlocker: true),
        ];
    }
}
