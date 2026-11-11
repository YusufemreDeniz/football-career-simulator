namespace FootballCareerSimulator.Domain.ManagerCareer;

using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

public sealed class ClubEmployment
{
    private ClubEmployment(
        ClubId clubId,
        GameDate startedAt,
        SeasonExpectationTier seasonExpectation,
        BoardConfidence boardConfidence,
        EmploymentRiskBand riskBand,
        FixtureId? lastAssessedFixtureId,
        string? lastAssessmentReasonCode)
    {
        ClubId = clubId;
        StartedAt = startedAt;
        SeasonExpectation = seasonExpectation;
        BoardConfidence = boardConfidence;
        RiskBand = riskBand;
        LastAssessedFixtureId = lastAssessedFixtureId;
        LastAssessmentReasonCode = lastAssessmentReasonCode;
    }

    public ClubId ClubId { get; }

    public GameDate StartedAt { get; }

    public SeasonExpectationTier SeasonExpectation { get; }

    public BoardConfidence BoardConfidence { get; }

    public EmploymentRiskBand RiskBand { get; }

    public FixtureId? LastAssessedFixtureId { get; }

    public string? LastAssessmentReasonCode { get; }

    public static ClubEmployment Create(
        ClubId clubId,
        GameDate startedAt,
        SeasonExpectationTier seasonExpectation,
        BoardConfidence? boardConfidence = null)
    {
        var confidence = boardConfidence ?? new BoardConfidence(BoardConfidence.DefaultInitialValue);
        return new ClubEmployment(
            clubId,
            startedAt,
            seasonExpectation,
            confidence,
            EmploymentRisk.FromConfidence(confidence.Value),
            lastAssessedFixtureId: null,
            lastAssessmentReasonCode: null);
    }

    public static ClubEmployment Rehydrate(
        ClubId clubId,
        GameDate startedAt,
        SeasonExpectationTier seasonExpectation,
        BoardConfidence boardConfidence,
        EmploymentRiskBand riskBand,
        FixtureId? lastAssessedFixtureId,
        string? lastAssessmentReasonCode) =>
        new(
            clubId,
            startedAt,
            seasonExpectation,
            boardConfidence,
            riskBand,
            lastAssessedFixtureId,
            lastAssessmentReasonCode);

    public ClubEmployment WithBoardAssessment(
        FixtureId fixtureId,
        BoardConfidence newConfidence,
        string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ManagerCareerInvariantViolationException("Assessment reason code cannot be empty.");
        }

        return new ClubEmployment(
            ClubId,
            StartedAt,
            SeasonExpectation,
            newConfidence,
            EmploymentRisk.FromConfidence(newConfidence.Value),
            fixtureId,
            reasonCode.Trim());
    }

    /// <summary>
    /// Maç dışı Board Confidence ayarı (ör. yönetim talebi yanıtı); son maç değerlendirme fikstürü korunur.
    /// </summary>
    public ClubEmployment WithBoardConfidenceAdjustment(
        BoardConfidence newConfidence,
        string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ManagerCareerInvariantViolationException("Assessment reason code cannot be empty.");
        }

        return new ClubEmployment(
            ClubId,
            StartedAt,
            SeasonExpectation,
            newConfidence,
            EmploymentRisk.FromConfidence(newConfidence.Value),
            LastAssessedFixtureId,
            reasonCode.Trim());
    }
}
