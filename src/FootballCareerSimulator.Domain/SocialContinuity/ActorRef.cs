namespace FootballCareerSimulator.Domain.SocialContinuity;

public readonly record struct ActorRef
{
    public ActorRef(ActorKind kind, long id)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new SocialContinuityInvariantViolationException($"Unknown actor kind: {kind}.");
        }

        if (id <= 0)
        {
            throw new SocialContinuityInvariantViolationException("Actor id must be positive.");
        }

        Kind = kind;
        Id = id;
    }

    public ActorKind Kind { get; }

    public long Id { get; }
}
