using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.Discipline;

/// <summary>
/// Disiplin authoritative kaydı (iskelet): uyarı / ceza / destek.
/// Relationship state bu aggregate'te tutulmaz.
/// </summary>
public sealed class DisciplinaryAction
{
    private DisciplinaryAction(
        DisciplinaryActionId disciplinaryActionId,
        DisciplinaryActionKind kind,
        ManagerId managerId,
        PlayerId subjectPlayerId,
        ClubId clubId,
        DecisionRequestId? sourceDecisionRequestId,
        GameDate appliedOn)
    {
        DisciplinaryActionId = disciplinaryActionId;
        Kind = kind;
        ManagerId = managerId;
        SubjectPlayerId = subjectPlayerId;
        ClubId = clubId;
        SourceDecisionRequestId = sourceDecisionRequestId;
        AppliedOn = appliedOn;
    }

    public DisciplinaryActionId DisciplinaryActionId { get; }

    public DisciplinaryActionKind Kind { get; }

    public ManagerId ManagerId { get; }

    public PlayerId SubjectPlayerId { get; }

    public ClubId ClubId { get; }

    public DecisionRequestId? SourceDecisionRequestId { get; }

    public GameDate AppliedOn { get; }

    public static DisciplinaryAction Apply(
        DisciplinaryActionId disciplinaryActionId,
        DisciplinaryActionKind kind,
        ManagerId managerId,
        PlayerId subjectPlayerId,
        ClubId clubId,
        GameDate appliedOn,
        DecisionRequestId? sourceDecisionRequestId = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DisciplineInvariantViolationException($"Unknown disciplinary action kind: {kind}.");
        }

        return new DisciplinaryAction(
            disciplinaryActionId,
            kind,
            managerId,
            subjectPlayerId,
            clubId,
            sourceDecisionRequestId,
            appliedOn);
    }

    public static DisciplinaryAction Rehydrate(
        DisciplinaryActionId disciplinaryActionId,
        DisciplinaryActionKind kind,
        ManagerId managerId,
        PlayerId subjectPlayerId,
        ClubId clubId,
        DecisionRequestId? sourceDecisionRequestId,
        GameDate appliedOn)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DisciplineInvariantViolationException($"Unknown disciplinary action kind: {kind}.");
        }

        return new DisciplinaryAction(
            disciplinaryActionId,
            kind,
            managerId,
            subjectPlayerId,
            clubId,
            sourceDecisionRequestId,
            appliedOn);
    }
}
