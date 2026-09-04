using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

public sealed class MatchSelectionQueryService
{
    private readonly IMatchSelectionStore _selectionStore;
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly IManagerCareerStore _managerCareerStore;

    public MatchSelectionQueryService(
        IMatchSelectionStore selectionStore,
        ILeagueCompetitionStore competitionStore,
        IManagerCareerStore managerCareerStore)
    {
        _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _managerCareerStore = managerCareerStore ?? throw new ArgumentNullException(nameof(managerCareerStore));
    }

    public MatchSelectionReadModel? Get(long fixtureId, long clubId)
    {
        var selection = _selectionStore.Get(new FixtureId(fixtureId), new ClubId(clubId));
        return selection is null
            ? null
            : new MatchSelectionReadModel(
                selection.FixtureId.Value,
                selection.ClubId.Value,
                selection.Status.ToString(),
                selection.StartingSlotIndices,
                selection.BenchSlotIndices);
    }

    public bool IsApproved(long fixtureId, long clubId) =>
        _selectionStore.Get(new FixtureId(fixtureId), new ClubId(clubId)) is not null;

    public ManagedFixtureSelectionStatusReadModel? GetNextDueManagedFixture(int currentDayNumber) =>
        GetNextManagedFixture(currentDayNumber, dueOnly: true);

    /// <summary>
    /// Returns the earliest still-planned managed fixture even when it is in the
    /// future. Pre-match planning screens use this; match execution keeps using
    /// <see cref="GetNextDueManagedFixture"/>.
    /// </summary>
    public ManagedFixtureSelectionStatusReadModel? GetNextPlannedManagedFixture(int currentDayNumber) =>
        GetNextManagedFixture(currentDayNumber, dueOnly: false);

    private ManagedFixtureSelectionStatusReadModel? GetNextManagedFixture(
        int currentDayNumber,
        bool dueOnly)
    {
        var employment = _managerCareerStore.Career.ActiveEmployment;
        if (employment is null)
        {
            return null;
        }

        var managedClubId = employment.ClubId;
        var season = _competitionStore.League.Seasons
            .OrderByDescending(candidate => candidate.SeasonId.Value)
            .FirstOrDefault(candidate =>
                candidate.Status is SeasonStatus.Active or SeasonStatus.Preseason);

        if (season is null)
        {
            return null;
        }

        var due = season.Fixtures
            .Where(fixture =>
                fixture.Status == FixtureStatus.Planned
                && (!dueOnly || fixture.ScheduledDate.DayNumber <= currentDayNumber)
                && (fixture.HomeClubId == managedClubId || fixture.AwayClubId == managedClubId))
            .OrderBy(fixture => fixture.ScheduledDate.DayNumber)
            .ThenBy(fixture => fixture.Id.Value)
            .FirstOrDefault();

        if (due is null)
        {
            return null;
        }

        var isHome = due.HomeClubId == managedClubId;
        var opponent = isHome ? due.AwayClubId : due.HomeClubId;

        return new ManagedFixtureSelectionStatusReadModel(
            due.Id.Value,
            season.SeasonId.Value,
            managedClubId.Value,
            opponent.Value,
            isHome,
            due.ScheduledDate.DayNumber,
            due.ScheduledDate.ToIsoDateString(),
            IsApproved(due.Id.Value, managedClubId.Value));
    }
}
