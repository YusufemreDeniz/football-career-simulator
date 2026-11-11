using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.ManagerCareer;

public sealed record EmploymentHistoryEntry
{
    private EmploymentHistoryEntry(
        ClubId clubId,
        GameDate startedAt,
        GameDate endedAt,
        EmploymentEndReason endReason,
        int finalBoardConfidence,
        FixtureId? causationFixtureId,
        string? finalAssessmentReasonCode)
    {
        ClubId = clubId;
        StartedAt = startedAt;
        EndedAt = endedAt;
        EndReason = endReason;
        FinalBoardConfidence = finalBoardConfidence;
        CausationFixtureId = causationFixtureId;
        FinalAssessmentReasonCode = finalAssessmentReasonCode;
    }

    public ClubId ClubId { get; }
    public GameDate StartedAt { get; }
    public GameDate EndedAt { get; }
    public EmploymentEndReason EndReason { get; }
    public int FinalBoardConfidence { get; }
    public FixtureId? CausationFixtureId { get; }
    public string? FinalAssessmentReasonCode { get; }

    public static EmploymentHistoryEntry Complete(
        ClubEmployment employment,
        GameDate endedAt,
        EmploymentEndReason endReason,
        FixtureId? causationFixtureId = null)
    {
        ArgumentNullException.ThrowIfNull(employment);
        return Rehydrate(
            employment.ClubId,
            employment.StartedAt,
            endedAt,
            endReason,
            employment.BoardConfidence.Value,
            causationFixtureId,
            employment.LastAssessmentReasonCode);
    }

    public static EmploymentHistoryEntry Rehydrate(
        ClubId clubId,
        GameDate startedAt,
        GameDate endedAt,
        EmploymentEndReason endReason,
        int finalBoardConfidence,
        FixtureId? causationFixtureId,
        string? finalAssessmentReasonCode)
    {
        if (endedAt.DayNumber < startedAt.DayNumber)
        {
            throw new ManagerCareerInvariantViolationException(
                "Employment history end date cannot precede its start date.");
        }

        _ = new BoardConfidence(finalBoardConfidence);
        if (!Enum.IsDefined(endReason))
        {
            throw new ManagerCareerInvariantViolationException("Employment history end reason is invalid.");
        }

        return new EmploymentHistoryEntry(
            clubId,
            startedAt,
            endedAt,
            endReason,
            finalBoardConfidence,
            causationFixtureId,
            string.IsNullOrWhiteSpace(finalAssessmentReasonCode)
                ? null
                : finalAssessmentReasonCode.Trim());
    }
}
