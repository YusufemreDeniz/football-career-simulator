namespace FootballCareerSimulator.Domain.ManagerCareer;

using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

public sealed class ManagerCareer
{
    private ManagerCareer(ManagerId managerId, string displayName, ClubEmployment? activeEmployment)
    {
        ManagerId = managerId;
        DisplayName = displayName;
        ActiveEmployment = activeEmployment;
    }

    public ManagerId ManagerId { get; }

    public string DisplayName { get; }

    public ClubEmployment? ActiveEmployment { get; }

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
                new BoardConfidence(initialBoardConfidence)));
    }

    /// <summary>
    /// Kulüp gücünden beklenti türeten kolay başlatıcı.
    /// </summary>
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
        ClubEmployment? activeEmployment) =>
        new(managerId, displayName, activeEmployment);

    /// <summary>
    /// Managed kulüp maçı sonrası board assessment. Aynı fixture ikinci kez uygulanmaz.
    /// </summary>
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
        var updatedCareer = new ManagerCareer(ManagerId, DisplayName, updatedEmployment);

        return BoardAssessmentResult.Applied(updatedCareer, updatedEmployment, delta, reasonCode);
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
