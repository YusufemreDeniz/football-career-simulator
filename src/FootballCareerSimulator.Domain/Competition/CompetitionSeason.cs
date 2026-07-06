namespace FootballCareerSimulator.Domain.Competition;

using FootballCareerSimulator.Domain.Competition.Events;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Competition bounded context season aggregate (docs/03_DOMAIN_MODEL.md Bölüm 7.2, 12.3).
/// </summary>
public sealed class CompetitionSeason
{
    private readonly List<SeasonParticipant> _participants = new();
    private readonly List<Fixture> _fixtures = new();
    private readonly List<CompetitionDomainEvent> _uncommittedEvents = new();

    private CompetitionSeason(
        CompetitionId competitionId,
        SeasonId seasonId,
        GameDate preseasonStartDate,
        SeasonStatus status,
        GameDate? activeStartedAt,
        GameDate? completedAt,
        GameDate? archivedAt,
        IEnumerable<SeasonParticipant> participants,
        IEnumerable<Fixture> fixtures)
    {
        CompetitionId = competitionId;
        SeasonId = seasonId;
        PreseasonStartDate = preseasonStartDate;
        Status = status;
        ActiveStartedAt = activeStartedAt;
        CompletedAt = completedAt;
        ArchivedAt = archivedAt;
        _participants.AddRange(participants);
        _fixtures.AddRange(fixtures);
    }

    public CompetitionId CompetitionId { get; }

    public SeasonId SeasonId { get; }

    public SeasonStatus Status { get; private set; }

    public GameDate PreseasonStartDate { get; }

    public GameDate? ActiveStartedAt { get; private set; }

    public GameDate? CompletedAt { get; private set; }

    public GameDate? ArchivedAt { get; private set; }

    public IReadOnlyList<SeasonParticipant> Participants => _participants;

    public IReadOnlyList<Fixture> Fixtures => _fixtures;

    public IReadOnlyList<CompetitionDomainEvent> UncommittedEvents => _uncommittedEvents;

    public static CompetitionSeason Create(
        CompetitionId competitionId,
        SeasonId seasonId,
        GameDate preseasonStartDate) =>
        new(
            competitionId,
            seasonId,
            preseasonStartDate,
            SeasonStatus.Preseason,
            activeStartedAt: null,
            completedAt: null,
            archivedAt: null,
            participants: Array.Empty<SeasonParticipant>(),
            fixtures: Array.Empty<Fixture>());

    public void RegisterParticipant(ClubId clubId)
    {
        if (Status is not SeasonStatus.Preseason)
        {
            throw new CompetitionInvariantViolationException(
                "Participants can only be registered while the season is in preseason.");
        }

        if (_participants.Any(participant => participant.ClubId == clubId))
        {
            throw new CompetitionInvariantViolationException(
                "The same club cannot be registered twice in the same season.");
        }

        if (_participants.Count >= CompetitionMvpConstraints.LeagueTeamCount)
        {
            throw new CompetitionInvariantViolationException(
                $"A league season cannot have more than {CompetitionMvpConstraints.LeagueTeamCount} participants.");
        }

        _participants.Add(new SeasonParticipant(clubId));
        _uncommittedEvents.Add(new SeasonParticipantRegistered(PreseasonStartDate, SeasonId, clubId));
    }

    public void StartActiveSeason(GameDate occurredAt)
    {
        if (Status is not SeasonStatus.Preseason)
        {
            throw new CompetitionInvariantViolationException(
                "Only a preseason season can transition to active.");
        }

        if (_participants.Count != CompetitionMvpConstraints.LeagueTeamCount)
        {
            throw new CompetitionInvariantViolationException(
                $"Active season requires exactly {CompetitionMvpConstraints.LeagueTeamCount} participants.");
        }

        Status = SeasonStatus.Active;
        ActiveStartedAt = occurredAt;
        _uncommittedEvents.Add(new SeasonStarted(occurredAt, CompetitionId, SeasonId));
    }

    public void PlanLeagueFixtures(
        GameDate firstMatchdayDate,
        FixtureId startingFixtureId,
        int daysBetweenRounds = CompetitionMvpConstraints.DefaultDaysBetweenRounds)
    {
        if (Status is not SeasonStatus.Active)
        {
            throw new CompetitionInvariantViolationException(
                "League fixtures can only be planned for an active season.");
        }

        if (_fixtures.Count > 0)
        {
            throw new CompetitionInvariantViolationException(
                "League fixtures have already been planned for this season.");
        }

        var generated = LeagueFixtureGenerator.GenerateDoubleRoundRobin(
            CompetitionId,
            SeasonId,
            _participants.Select(participant => participant.ClubId).ToArray(),
            firstMatchdayDate,
            daysBetweenRounds,
            startingFixtureId);

        _fixtures.AddRange(generated);
        _uncommittedEvents.Add(
            new LeagueFixturesPlanned(
                firstMatchdayDate,
                CompetitionId,
                SeasonId,
                generated.Count,
                firstMatchdayDate));
    }

    public void CompleteSeason(GameDate occurredAt)
    {
        if (Status is not SeasonStatus.Active)
        {
            throw new CompetitionInvariantViolationException(
                "Only an active season can be completed.");
        }

        Status = SeasonStatus.Completed;
        CompletedAt = occurredAt;
        _uncommittedEvents.Add(new SeasonCompleted(occurredAt, CompetitionId, SeasonId));
    }

    public void ArchiveSeason(GameDate occurredAt)
    {
        if (Status is not SeasonStatus.Completed)
        {
            throw new CompetitionInvariantViolationException(
                "Only a completed season can be archived.");
        }

        Status = SeasonStatus.Archived;
        ArchivedAt = occurredAt;
        _uncommittedEvents.Add(new SeasonArchived(occurredAt, CompetitionId, SeasonId));
    }

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    public static CompetitionSeason Rehydrate(
        CompetitionId competitionId,
        SeasonId seasonId,
        GameDate preseasonStartDate,
        SeasonStatus status,
        GameDate? activeStartedAt,
        GameDate? completedAt,
        GameDate? archivedAt,
        IEnumerable<SeasonParticipant> participants,
        IEnumerable<Fixture>? fixtures = null) =>
        new(
            competitionId,
            seasonId,
            preseasonStartDate,
            status,
            activeStartedAt,
            completedAt,
            archivedAt,
            participants,
            fixtures ?? Array.Empty<Fixture>());
}
