using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Sosyal Continuity söz kaydı.
/// MVP: Starting Opportunity (ilk 11) + Playing Time (maç günü kadrosu: XI ∪ yedek).
/// </summary>
public sealed class Promise
{
    public const int MinTargetCount = 1;
    public const int MaxTargetCount = 50;

    private readonly HashSet<long> _countedFixtureIds;

    private Promise(
        PromiseId promiseId,
        PromiseKind kind,
        ActorRef promisor,
        ActorRef promisee,
        ClubId clubId,
        int targetStarts,
        int startsGiven,
        GameDate deadlineOn,
        GameDate createdOn,
        PromiseStatus status,
        GameDate? terminalOn,
        IEnumerable<long> countedFixtureIds)
    {
        PromiseId = promiseId;
        Kind = kind;
        Promisor = promisor;
        Promisee = promisee;
        ClubId = clubId;
        TargetStarts = targetStarts;
        StartsGiven = startsGiven;
        DeadlineOn = deadlineOn;
        CreatedOn = createdOn;
        Status = status;
        TerminalOn = terminalOn;
        _countedFixtureIds = countedFixtureIds.ToHashSet();
    }

    public PromiseId PromiseId { get; }

    public PromiseKind Kind { get; }

    public ActorRef Promisor { get; }

    public ActorRef Promisee { get; }

    public ClubId ClubId { get; }

    /// <summary>Count-based hedef (ilk 11 veya maç günü görünümü).</summary>
    public int TargetStarts { get; }

    /// <summary>Count-based ilerleme.</summary>
    public int StartsGiven { get; }

    public GameDate DeadlineOn { get; }

    public GameDate CreatedOn { get; }

    public PromiseStatus Status { get; }

    public GameDate? TerminalOn { get; }

    public IReadOnlyCollection<long> CountedFixtureIds => _countedFixtureIds;

    public bool IsActive => Status == PromiseStatus.Active;

    public bool IsTerminal =>
        Status is PromiseStatus.Fulfilled or PromiseStatus.Broken or PromiseStatus.Archived;

    public static Promise CreateStartingOpportunity(
        PromiseId promiseId,
        ManagerId managerId,
        PlayerId playerId,
        ClubId clubId,
        int targetStarts,
        GameDate deadlineOn,
        GameDate createdOn) =>
        CreateCountBased(
            promiseId,
            PromiseKind.StartingOpportunity,
            managerId,
            playerId,
            clubId,
            targetStarts,
            deadlineOn,
            createdOn);

    public static Promise CreatePlayingTime(
        PromiseId promiseId,
        ManagerId managerId,
        PlayerId playerId,
        ClubId clubId,
        int targetAppearances,
        GameDate deadlineOn,
        GameDate createdOn) =>
        CreateCountBased(
            promiseId,
            PromiseKind.PlayingTime,
            managerId,
            playerId,
            clubId,
            targetAppearances,
            deadlineOn,
            createdOn);

    public static Promise Rehydrate(
        PromiseId promiseId,
        PromiseKind kind,
        ActorRef promisor,
        ActorRef promisee,
        ClubId clubId,
        int targetStarts,
        int startsGiven,
        GameDate deadlineOn,
        GameDate createdOn,
        PromiseStatus status,
        GameDate? terminalOn,
        IReadOnlyList<long> countedFixtureIds)
    {
        ArgumentNullException.ThrowIfNull(countedFixtureIds);
        if (!Enum.IsDefined(kind))
        {
            throw new SocialContinuityInvariantViolationException($"Unknown promise kind: {kind}.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new SocialContinuityInvariantViolationException($"Unknown promise status: {status}.");
        }

        ValidateTargetCount(targetStarts);
        if (startsGiven < 0 || startsGiven > MaxTargetCount)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Progress count must be between 0 and {MaxTargetCount}.");
        }

        if (status == PromiseStatus.Active && terminalOn is not null)
        {
            throw new SocialContinuityInvariantViolationException(
                "Active promise cannot have TerminalOn.");
        }

        if (status is PromiseStatus.Fulfilled or PromiseStatus.Broken or PromiseStatus.Archived
            && terminalOn is null)
        {
            throw new SocialContinuityInvariantViolationException(
                "Terminal promise requires TerminalOn.");
        }

        return new Promise(
            promiseId,
            kind,
            promisor,
            promisee,
            clubId,
            targetStarts,
            startsGiven,
            deadlineOn,
            createdOn,
            status,
            terminalOn,
            countedFixtureIds);
    }

    public Promise RecordStartingAppearance(FixtureId fixtureId, GameDate day) =>
        RecordProgress(fixtureId, day, PromiseKind.StartingOpportunity);

    public Promise RecordMatchAppearance(FixtureId fixtureId, GameDate day) =>
        RecordProgress(fixtureId, day, PromiseKind.PlayingTime);

    public Promise EvaluateDeadline(GameDate day)
    {
        if (Status != PromiseStatus.Active)
        {
            return this;
        }

        if (day.DayNumber < DeadlineOn.DayNumber)
        {
            return this;
        }

        var nextStatus = StartsGiven >= TargetStarts
            ? PromiseStatus.Fulfilled
            : PromiseStatus.Broken;

        return new Promise(
            PromiseId,
            Kind,
            Promisor,
            Promisee,
            ClubId,
            TargetStarts,
            StartsGiven,
            DeadlineOn,
            CreatedOn,
            nextStatus,
            day,
            _countedFixtureIds);
    }

    public Promise Archive(GameDate day)
    {
        if (Status == PromiseStatus.Archived)
        {
            return this;
        }

        if (Status is not (PromiseStatus.Fulfilled or PromiseStatus.Broken))
        {
            throw new SocialContinuityInvariantViolationException(
                $"Promise #{PromiseId.Value} is {Status} and cannot be archived.");
        }

        return new Promise(
            PromiseId,
            Kind,
            Promisor,
            Promisee,
            ClubId,
            TargetStarts,
            StartsGiven,
            DeadlineOn,
            CreatedOn,
            PromiseStatus.Archived,
            day,
            _countedFixtureIds);
    }

    private Promise RecordProgress(FixtureId fixtureId, GameDate day, PromiseKind expectedKind)
    {
        if (Status != PromiseStatus.Active || Kind != expectedKind)
        {
            return this;
        }

        if (_countedFixtureIds.Contains(fixtureId.Value))
        {
            return this;
        }

        var nextCounted = _countedFixtureIds.Append(fixtureId.Value).ToArray();
        var nextCount = StartsGiven + 1;
        if (nextCount >= TargetStarts)
        {
            return new Promise(
                PromiseId,
                Kind,
                Promisor,
                Promisee,
                ClubId,
                TargetStarts,
                nextCount,
                DeadlineOn,
                CreatedOn,
                PromiseStatus.Fulfilled,
                day,
                nextCounted);
        }

        return new Promise(
            PromiseId,
            Kind,
            Promisor,
            Promisee,
            ClubId,
            TargetStarts,
            nextCount,
            DeadlineOn,
            CreatedOn,
            PromiseStatus.Active,
            terminalOn: null,
            nextCounted);
    }

    private static Promise CreateCountBased(
        PromiseId promiseId,
        PromiseKind kind,
        ManagerId managerId,
        PlayerId playerId,
        ClubId clubId,
        int targetCount,
        GameDate deadlineOn,
        GameDate createdOn)
    {
        ValidateTargetCount(targetCount);
        if (deadlineOn.DayNumber <= createdOn.DayNumber)
        {
            throw new SocialContinuityInvariantViolationException(
                "Promise deadline must be after creation day.");
        }

        return new Promise(
            promiseId,
            kind,
            new ActorRef(ActorKind.Manager, managerId.Value),
            new ActorRef(ActorKind.Player, playerId.Value),
            clubId,
            targetCount,
            startsGiven: 0,
            deadlineOn,
            createdOn,
            PromiseStatus.Active,
            terminalOn: null,
            Array.Empty<long>());
    }

    private static void ValidateTargetCount(int targetCount)
    {
        if (targetCount is < MinTargetCount or > MaxTargetCount)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Target count must be between {MinTargetCount} and {MaxTargetCount}.");
        }
    }
}
