namespace FootballCareerSimulator.Domain.ManagerCareer;

using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

public sealed class ManagerCareer
{
    private ManagerCareer(
        ManagerId managerId,
        string displayName,
        ClubEmployment? activeEmployment,
        ManagerEmploymentStatus employmentStatus,
        EmploymentEndReason? terminationReason,
        ClubId? lastClubId,
        FixtureId? dismissedDueToFixtureId,
        GameDate? dismissedAt)
    {
        ManagerId = managerId;
        DisplayName = displayName;
        ActiveEmployment = activeEmployment;
        EmploymentStatus = employmentStatus;
        TerminationReason = terminationReason;
        LastClubId = lastClubId;
        DismissedDueToFixtureId = dismissedDueToFixtureId;
        DismissedAt = dismissedAt;
    }

    public ManagerId ManagerId { get; }

    public string DisplayName { get; }

    public ClubEmployment? ActiveEmployment { get; }

    public ManagerEmploymentStatus EmploymentStatus { get; }

    public EmploymentEndReason? TerminationReason { get; }

    public ClubId? LastClubId { get; }

    public FixtureId? DismissedDueToFixtureId { get; }

    public GameDate? DismissedAt { get; }

    public bool IsEmployed =>
        EmploymentStatus == ManagerEmploymentStatus.Employed && ActiveEmployment is not null;

    public static ManagerCareer StartNewCareer(
        ManagerId managerId,
        string displayName,
        ClubId startingClubId,
        GameDate startedAt,
        SeasonExpectationTier seasonExpectation,
        int initialBoardConfidence = BoardConfidence.DefaultInitialValue)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ManagerCareerInvariantViolationException("Manager display name cannot be empty.");
        }

        return new ManagerCareer(
            managerId,
            displayName.Trim(),
            ClubEmployment.Create(
                startingClubId,
                startedAt,
                seasonExpectation,
                new BoardConfidence(initialBoardConfidence)),
            ManagerEmploymentStatus.Employed,
            terminationReason: null,
            lastClubId: startingClubId,
            dismissedDueToFixtureId: null,
            dismissedAt: null);
    }

    public static ManagerCareer StartNewCareerForClubStrength(
        ManagerId managerId,
        string displayName,
        ClubId startingClubId,
        GameDate startedAt,
        int clubSportiveStrength,
        int initialBoardConfidence = BoardConfidence.DefaultInitialValue) =>
        StartNewCareer(
            managerId,
            displayName,
            startingClubId,
            startedAt,
            SeasonExpectation.FromSportiveStrength(clubSportiveStrength),
            initialBoardConfidence);

    public static ManagerCareer Rehydrate(
        ManagerId managerId,
        string displayName,
        ClubEmployment? activeEmployment,
        ManagerEmploymentStatus employmentStatus,
        EmploymentEndReason? terminationReason,
        ClubId? lastClubId,
        FixtureId? dismissedDueToFixtureId,
        GameDate? dismissedAt)
    {
        if (employmentStatus == ManagerEmploymentStatus.Employed && activeEmployment is null)
        {
            throw new ManagerCareerInvariantViolationException(
                "Employed manager career requires active employment.");
        }

        if (employmentStatus == ManagerEmploymentStatus.Unemployed && activeEmployment is not null)
        {
            throw new ManagerCareerInvariantViolationException(
                "Unemployed manager career cannot keep active employment.");
        }

        return new ManagerCareer(
            managerId,
            displayName,
            activeEmployment,
            employmentStatus,
            terminationReason,
            lastClubId,
            dismissedDueToFixtureId,
            dismissedAt);
    }

    /// <summary>
    /// Geriye dönük rehydrate: istihdam varsa Employed kabul edilir.
    /// </summary>
    public static ManagerCareer Rehydrate(
        ManagerId managerId,
        string displayName,
        ClubEmployment? activeEmployment) =>
        activeEmployment is null
            ? Rehydrate(
                managerId,
                displayName,
                activeEmployment: null,
                ManagerEmploymentStatus.Unemployed,
                terminationReason: EmploymentEndReason.Dismissed,
                lastClubId: null,
                dismissedDueToFixtureId: null,
                dismissedAt: null)
            : Rehydrate(
                managerId,
                displayName,
                activeEmployment,
                ManagerEmploymentStatus.Employed,
                terminationReason: null,
                lastClubId: activeEmployment.ClubId,
                dismissedDueToFixtureId: null,
                dismissedAt: null);

    public BoardAssessmentResult ApplyMatchBoardAssessment(
        FixtureId fixtureId,
        MatchOutcomeForManagedClub matchOutcome,
        int leaguePosition,
        int leagueSize)
    {
        if (ActiveEmployment is null)
        {
            throw new ManagerCareerInvariantViolationException(
                "Board assessment requires active employment.");
        }

        if (ActiveEmployment.LastAssessedFixtureId == fixtureId)
        {
            return BoardAssessmentResult.AlreadyApplied(this, ActiveEmployment);
        }

        var meetsExpectation = SeasonExpectation.MeetsExpectation(
            ActiveEmployment.SeasonExpectation,
            leaguePosition,
            leagueSize);

        var (delta, reasonCode) = ComputeDelta(matchOutcome, meetsExpectation);
        var newConfidence = ActiveEmployment.BoardConfidence.Adjust(delta);
        var updatedEmployment = ActiveEmployment.WithBoardAssessment(fixtureId, newConfidence, reasonCode);
        var updatedCareer = new ManagerCareer(
            ManagerId,
            DisplayName,
            updatedEmployment,
            ManagerEmploymentStatus.Employed,
            terminationReason: null,
            lastClubId: updatedEmployment.ClubId,
            dismissedDueToFixtureId: null,
            dismissedAt: null);

        return BoardAssessmentResult.Applied(updatedCareer, updatedEmployment, delta, reasonCode);
    }

    /// <summary>
    /// Critical risk sonrası kovulma. Aynı fixture causation ikinci kez no-op.
    /// </summary>
    public DismissalResult DismissDueToBoardConfidence(FixtureId causationFixtureId, GameDate dismissedAt)
    {
        if (DismissedDueToFixtureId == causationFixtureId
            && EmploymentStatus == ManagerEmploymentStatus.Unemployed)
        {
            return DismissalResult.AlreadyApplied(this);
        }

        if (!IsEmployed || ActiveEmployment is null)
        {
            return DismissalResult.AlreadyApplied(this);
        }

        if (ActiveEmployment.RiskBand != EmploymentRiskBand.Critical)
        {
            throw new ManagerCareerInvariantViolationException(
                "Dismissal requires Critical employment risk band.");
        }

        var unemployed = new ManagerCareer(
            ManagerId,
            DisplayName,
            activeEmployment: null,
            ManagerEmploymentStatus.Unemployed,
            terminationReason: EmploymentEndReason.Dismissed,
            lastClubId: ActiveEmployment.ClubId,
            dismissedDueToFixtureId: causationFixtureId,
            dismissedAt: dismissedAt);

        return DismissalResult.Applied(unemployed, ActiveEmployment.ClubId, causationFixtureId);
    }

    private static (int Delta, string ReasonCode) ComputeDelta(
        MatchOutcomeForManagedClub outcome,
        bool meetsExpectation) =>
        (outcome, meetsExpectation) switch
        {
            (MatchOutcomeForManagedClub.Win, true) => (5, "WinOnTrack"),
            (MatchOutcomeForManagedClub.Win, false) => (3, "WinBehindExpectation"),
            (MatchOutcomeForManagedClub.Draw, true) => (0, "DrawOnTrack"),
            (MatchOutcomeForManagedClub.Draw, false) => (-2, "DrawBehindExpectation"),
            (MatchOutcomeForManagedClub.Loss, true) => (-3, "LossOnTrack"),
            (MatchOutcomeForManagedClub.Loss, false) => (-6, "LossBehindExpectation"),
            _ => (0, "Neutral"),
        };
}

public enum MatchOutcomeForManagedClub
{
    Win = 1,
    Draw = 2,
    Loss = 3,
}

public sealed record BoardAssessmentResult(
    bool WasApplied,
    bool WasAlreadyApplied,
    ManagerCareer Career,
    int ConfidenceDelta,
    int BoardConfidence,
    EmploymentRiskBand RiskBand,
    SeasonExpectationTier SeasonExpectation,
    string? ReasonCode)
{
    public static BoardAssessmentResult AlreadyApplied(ManagerCareer career, ClubEmployment employment) =>
        new(
            WasApplied: false,
            WasAlreadyApplied: true,
            career,
            ConfidenceDelta: 0,
            employment.BoardConfidence.Value,
            employment.RiskBand,
            employment.SeasonExpectation,
            employment.LastAssessmentReasonCode);

    public static BoardAssessmentResult Applied(
        ManagerCareer career,
        ClubEmployment employment,
        int delta,
        string reasonCode) =>
        new(
            WasApplied: true,
            WasAlreadyApplied: false,
            career,
            delta,
            employment.BoardConfidence.Value,
            employment.RiskBand,
            employment.SeasonExpectation,
            reasonCode);
}

public sealed record DismissalResult(
    bool WasApplied,
    bool WasAlreadyApplied,
    ManagerCareer Career,
    long? DismissedFromClubId,
    long? CausationFixtureId)
{
    public static DismissalResult AlreadyApplied(ManagerCareer career) =>
        new(false, true, career, career.LastClubId?.Value, career.DismissedDueToFixtureId?.Value);

    public static DismissalResult Applied(
        ManagerCareer career,
        ClubId fromClubId,
        FixtureId causationFixtureId) =>
        new(true, false, career, fromClubId.Value, causationFixtureId.Value);
}
