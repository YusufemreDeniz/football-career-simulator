using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Infrastructure.Career;

internal static class ManagerSnapshotMapper
{
    public static ManagerCareer ToDomain(
        long managerId,
        string displayName,
        long? employedClubId,
        int? employmentStartedDayNumber,
        GameDate fallbackStartDate,
        int? seasonExpectation = null,
        int? boardConfidence = null,
        int? riskBand = null,
        long? lastAssessedFixtureId = null,
        string? lastAssessmentReasonCode = null)
    {
        if (employedClubId is null || employmentStartedDayNumber is null)
        {
            return ManagerCareer.StartNewCareerForClubStrength(
                new ManagerId(managerId),
                displayName,
                new ClubId(1),
                fallbackStartDate,
                clubSportiveStrength: 50);
        }

        var expectation = seasonExpectation is int expectationValue
            ? (SeasonExpectationTier)expectationValue
            : SeasonExpectationTier.MidTable;
        var confidence = new BoardConfidence(boardConfidence ?? BoardConfidence.DefaultInitialValue);
        var band = riskBand is int riskValue
            ? (EmploymentRiskBand)riskValue
            : EmploymentRisk.FromConfidence(confidence.Value);
        var fixtureId = lastAssessedFixtureId is long fixture
            ? new FixtureId(fixture)
            : (FixtureId?)null;

        var employment = ClubEmployment.Rehydrate(
            new ClubId(employedClubId.Value),
            GameDate.FromDayNumber(employmentStartedDayNumber.Value),
            expectation,
            confidence,
            band,
            fixtureId,
            lastAssessmentReasonCode);

        return ManagerCareer.Rehydrate(new ManagerId(managerId), displayName, employment);
    }
}
