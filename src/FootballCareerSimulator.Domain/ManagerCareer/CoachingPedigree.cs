using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.ManagerCareer;

/// <summary>
/// Emekli olan futbolcunun teknik direktörlük soy ağacı ve antrenörlük kökeni (Yenilik 8: Coaching Lineage).
/// </summary>
public sealed class CoachingPedigree
{
    public CoachingPedigree(
        ManagerId managerId,
        long originPlayerId,
        string fullName,
        ManagerId? mentoredByManagerId,
        string? mentoredByManagerName,
        Formation favoredFormation,
        int leadershipReputation,
        GameDate transitionedOn)
    {
        if (managerId.Value <= 0)
        {
            throw new ManagerCareerInvariantViolationException("Manager ID must be positive.");
        }

        if (originPlayerId <= 0)
        {
            throw new ManagerCareerInvariantViolationException("Origin player ID must be positive.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ManagerCareerInvariantViolationException("Full name is required.");
        }

        ManagerId = managerId;
        OriginPlayerId = originPlayerId;
        FullName = fullName;
        MentoredByManagerId = mentoredByManagerId;
        MentoredByManagerName = mentoredByManagerName;
        FavoredFormation = favoredFormation;
        LeadershipReputation = Math.Clamp(leadershipReputation, 0, 100);
        TransitionedOn = transitionedOn;
    }

    public ManagerId ManagerId { get; }

    public long OriginPlayerId { get; }

    public string FullName { get; }

    public ManagerId? MentoredByManagerId { get; }

    public string? MentoredByManagerName { get; }

    public Formation FavoredFormation { get; }

    public int LeadershipReputation { get; }

    public GameDate TransitionedOn { get; }

    public bool HasMentor => MentoredByManagerId != null;
}
