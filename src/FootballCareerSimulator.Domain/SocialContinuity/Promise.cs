using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.SocialContinuity;

/// <summary>
/// Sosyal Continuity söz kaydı. MVP iskelet: yalnızca Starting Opportunity (ilk 11 sayısı + deadline).
/// </summary>
public sealed class Promise
{
    public const int MinTargetStarts = 1;
    public const int MaxTargetStarts = 50;

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

    public int TargetStarts { get; }

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
        GameDate createdOn)
    {
        ValidateTargetStarts(targetStarts);
        if (deadlineOn.DayNumber <= createdOn.DayNumber)
        {
            throw new SocialContinuityInvariantViolationException(
                "Starting opportunity deadline must be after creation day.");
        }

        return new Promise(
            promiseId,
            PromiseKind.StartingOpportunity,
            new ActorRef(ActorKind.Manager, managerId.Value),
            new ActorRef(ActorKind.Player, playerId.Value),
            clubId,
            targetStarts,
            startsGiven: 0,
            deadlineOn,
            createdOn,
            PromiseStatus.Active,
            terminalOn: null,
            Array.Empty<long>());
    }

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

        ValidateTargetStarts(targetStarts);
        if (startsGiven < 0 || startsGiven > MaxTargetStarts)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Starts given must be between 0 and {MaxTargetStarts}.");
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

    public Promise RecordStartingAppearance(FixtureId fixtureId, GameDate day)
    {
        if (Status != PromiseStatus.Active || Kind != PromiseKind.StartingOpportunity)
        {
            return this;
        }

        if (_countedFixtureIds.Contains(fixtureId.Value))
        {
            return this;
        }

        var nextCounted = _countedFixtureIds.Append(fixtureId.Value).ToArray();
        var nextStarts = StartsGiven + 1;
        if (nextStarts >= TargetStarts)
        {
            return new Promise(
                PromiseId,
                Kind,
                Promisor,
                Promisee,
                ClubId,
                TargetStarts,
                nextStarts,
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
            nextStarts,
            DeadlineOn,
            CreatedOn,
            PromiseStatus.Active,
            terminalOn: null,
            nextCounted);
    }

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

    private static void ValidateTargetStarts(int targetStarts)
    {
        if (targetStarts is < MinTargetStarts or > MaxTargetStarts)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Target starts must be between {MinTargetStarts} and {MaxTargetStarts}.");
        }
    }
}
