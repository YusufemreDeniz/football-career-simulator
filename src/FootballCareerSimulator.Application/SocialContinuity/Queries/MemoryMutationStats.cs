namespace FootballCareerSimulator.Application.SocialContinuity.Queries;

/// <summary>
/// Memory candidate evaluation özeti: Create / Reinforce / Reject.
/// </summary>
public sealed record MemoryMutationStats(int Created, int Reinforced, int Rejected)
{
    public static MemoryMutationStats Empty { get; } = new(0, 0, 0);

    public int Applied => Created + Reinforced;

    public MemoryMutationStats AddDecision(MemoryCandidateDecision decision) =>
        decision switch
        {
            MemoryCandidateDecision.Created => new(Created + 1, Reinforced, Rejected),
            MemoryCandidateDecision.Reinforced => new(Created, Reinforced + 1, Rejected),
            MemoryCandidateDecision.Rejected => new(Created, Reinforced, Rejected + 1),
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, null),
        };
}
